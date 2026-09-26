using CC.api.Models;
using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CC.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SystemAuditLogsController : ControllerBase
    {
        private readonly CrmDbContext _context;

        public SystemAuditLogsController(CrmDbContext context)
        {
            _context = context;
        }

        // GET: api/systemauditlogs?page=1&pageSize=10&search=...&actionType=...
        [HttpGet]
        public async Task<ActionResult<PagedResult<SystemAuditLogDto>>> GetLogs(
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromQuery] string? search = null,
            [FromQuery] string? actionType = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            pageSize = 10;
            page = Math.Max(1, page);

            IQueryable<SystemAuditLog> query = _context.SystemAuditLogs
                .AsNoTracking()
                .Include(l => l.User)
                .Where(l => l.User == null || l.User.CompanyId == companyId);

            if (!string.IsNullOrWhiteSpace(actionType))
            {
                string at = actionType.Trim().ToLower();
                query = query.Where(l => l.ActionType.ToLower() == at);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(l =>
                    l.ActionType.ToLower().Contains(s) ||
                    l.ActionDescription.ToLower().Contains(s) ||
                    (l.User != null && l.User.Username.ToLower().Contains(s)));
            }

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(l => l.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new SystemAuditLogDto
                {
                    LogId = l.LogId,
                    UserId = l.UserId,
                    Username = l.User != null ? l.User.Username : "System",
                    ActionType = l.ActionType,
                    ActionDescription = l.ActionDescription,
                    Timestamp = l.Timestamp,
                    IPAddress = l.IPAddress ?? "127.0.0.1"
                })
                .ToListAsync();

            return Ok(new PagedResult<SystemAuditLogDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        // POST: api/systemauditlogs
        [HttpPost]
        public async Task<ActionResult<SystemAuditLogDto>> CreateLog(
            [FromBody] CreateAuditLogRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromHeader(Name = "X-User-Id")] int userId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            int logUserId = request.UserId > 0 ? request.UserId : userId;
            if (logUserId <= 0)
            {
                // Fall back to first available user or keep 0
                var firstUser = await _context.AppUsers.FirstOrDefaultAsync(u => u.CompanyId == companyId);
                if (firstUser != null) logUserId = firstUser.UserId;
            }

            var log = new SystemAuditLog
            {
                UserId = logUserId,
                ActionType = string.IsNullOrWhiteSpace(request.ActionType) ? "GENERAL" : request.ActionType.Trim().ToUpperInvariant(),
                ActionDescription = request.ActionDescription?.Trim() ?? string.Empty,
                Timestamp = DateTime.UtcNow,
                IPAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
            };

            _context.SystemAuditLogs.Add(log);
            await _context.SaveChangesAsync();

            var user = await _context.AppUsers.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == logUserId);

            var result = new SystemAuditLogDto
            {
                LogId = log.LogId,
                UserId = log.UserId,
                Username = user?.Username ?? "System",
                ActionType = log.ActionType,
                ActionDescription = log.ActionDescription,
                Timestamp = log.Timestamp,
                IPAddress = log.IPAddress
            };

            return Ok(result);
        }
    }

    public class SystemAuditLogDto
    {
        public int LogId { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string ActionDescription { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string IPAddress { get; set; } = string.Empty;
    }

    public class CreateAuditLogRequest
    {
        public int UserId { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public string ActionDescription { get; set; } = string.Empty;
    }
}
