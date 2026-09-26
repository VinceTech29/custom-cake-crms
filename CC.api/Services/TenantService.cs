using System;
using System.Collections.Concurrent;
using System.Linq;
using CC.infrastructure.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CC.api.Services
{
    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MasterCrmDbContext _masterDbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TenantService> _logger;

        // Cache connection strings by CompanyId so we don't query Master DB on every single request
        private static readonly ConcurrentDictionary<int, string> _connectionCache = new();

        public TenantService(
            IHttpContextAccessor httpContextAccessor,
            MasterCrmDbContext masterDbContext,
            IConfiguration configuration,
            ILogger<TenantService> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _masterDbContext = masterDbContext;
            _configuration = configuration;
            _logger = logger;
        }

        public int? GetCurrentCompanyId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return null;

            // Read X-Company-Id from request headers
            if (httpContext.Request.Headers.TryGetValue("X-Company-Id", out var companyIdHeader))
            {
                if (int.TryParse(companyIdHeader.FirstOrDefault(), out var companyId) && companyId > 0)
                {
                    return companyId;
                }
            }

            // Also check query string as fallback (e.g. for testing / swagger)
            if (httpContext.Request.Query.TryGetValue("companyId", out var queryCompanyId))
            {
                if (int.TryParse(queryCompanyId.FirstOrDefault(), out var companyId) && companyId > 0)
                {
                    return companyId;
                }
            }

            return null;
        }

        public string GetTenantConnectionString()
        {
            var companyId = GetCurrentCompanyId();

            // If no companyId is passed (e.g., health check, Swagger UI, design time), return default connection string
            if (!companyId.HasValue || companyId.Value <= 0)
            {
                var defaultConn = _configuration.GetConnectionString("LocalCrm");
                if (string.IsNullOrEmpty(defaultConn))
                {
                    throw new InvalidOperationException("Default CRM connection string 'LocalCrm' was not found in configuration.");
                }
                return defaultConn;
            }

            // Check cache
            if (_connectionCache.TryGetValue(companyId.Value, out var cachedConn))
            {
                return cachedConn;
            }

            // Look up the active tenant database from Master CRM
            var tenantDb = _masterDbContext.CompanyDatabases
                .AsNoTracking()
                .FirstOrDefault(cd => cd.CompanyId == companyId.Value && cd.IsActive);

            if (tenantDb == null)
            {
                _logger.LogWarning("No active database mapping found in MSME_MasterCRM for CompanyId {CompanyId}", companyId.Value);
                throw new InvalidOperationException($"No active tenant database found for Company ID {companyId.Value}.");
            }

            // Construct tenant connection string
            var connStr = $"Server={tenantDb.ServerName};Database={tenantDb.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";
            _connectionCache[companyId.Value] = connStr;

            _logger.LogInformation("Resolved tenant database '{DatabaseName}' on '{ServerName}' for CompanyId {CompanyId}",
                tenantDb.DatabaseName, tenantDb.ServerName, companyId.Value);

            return connStr;
        }
    }
}
