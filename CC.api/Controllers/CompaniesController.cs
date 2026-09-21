using CC.api.Models;
using CC.domain.Entities;
using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CC.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompaniesController : ControllerBase
    {
        private readonly MasterCrmDbContext _context;

        public CompaniesController(MasterCrmDbContext context)
        {
            _context = context;
        }

        // GET: api/companies?page=1&pageSize=10&search=...
        [HttpGet]
        public async Task<ActionResult<PagedResult<CompanyDto>>> GetCompanies(
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            pageSize = 10;
            page = Math.Max(1, page);

            IQueryable<Company> query = _context.Companies.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(c =>
                    c.CompanyName.ToLower().Contains(s) ||
                    c.CompanyCode.ToLower().Contains(s));
            }

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var companies = await query
                .OrderBy(c => c.CompanyId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var companyIds = companies.Select(c => c.CompanyId).ToList();

            var databases = await _context.CompanyDatabases
                .AsNoTracking()
                .Where(cd => companyIds.Contains(cd.CompanyId) && cd.IsActive)
                .ToListAsync();

            var activeSubs = await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Where(s => companyIds.Contains(s.CompanyId) && s.StatusId == 1 && s.EndDate >= DateTime.UtcNow)
                .ToListAsync();

            var items = companies.Select(c =>
            {
                var db = databases.FirstOrDefault(d => d.CompanyId == c.CompanyId);
                var sub = activeSubs.FirstOrDefault(s => s.CompanyId == c.CompanyId);
                return new CompanyDto
                {
                    CompanyId = c.CompanyId,
                    CompanyCode = c.CompanyCode,
                    CompanyName = c.CompanyName,
                    IsActive = c.IsActive,
                    CreatedDate = c.CreatedDate,
                    DatabaseName = db?.DatabaseName ?? "N/A",
                    ServerName = db?.ServerName ?? "N/A",
                    ActivePlanName = sub?.Plan?.PlanName ?? "None"
                };
            }).ToList();

            return Ok(new PagedResult<CompanyDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        // GET: api/companies/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CompanyDetailsDto>> GetCompany(int id)
        {
            var company = await _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyId == id);

            if (company == null)
            {
                return NotFound($"Company #{id} not found.");
            }

            var databases = await _context.CompanyDatabases
                .AsNoTracking()
                .Where(cd => cd.CompanyId == id)
                .ToListAsync();

            var activeSub = await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Include(s => s.Status)
                .Where(s => s.CompanyId == id)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            var dto = new CompanyDetailsDto
            {
                CompanyId = company.CompanyId,
                CompanyCode = company.CompanyCode,
                CompanyName = company.CompanyName,
                IsActive = company.IsActive,
                CreatedDate = company.CreatedDate,
                Databases = databases.Select(d => new CompanyDatabaseDto
                {
                    CompanyDatabaseId = d.CompanyDatabaseId,
                    ServerName = d.ServerName,
                    DatabaseName = d.DatabaseName,
                    IsActive = d.IsActive
                }).ToList(),
                CurrentSubscription = activeSub == null ? null : new CompanySubscriptionSummaryDto
                {
                    SubscriptionId = activeSub.SubscriptionId,
                    PlanName = activeSub.Plan?.PlanName ?? "Unknown",
                    Price = activeSub.Plan?.Price ?? 0,
                    StatusName = activeSub.Status?.StatusName ?? "Active",
                    StartDate = activeSub.StartDate,
                    EndDate = activeSub.EndDate
                }
            };

            return Ok(dto);
        }

        // POST: api/companies
        [HttpPost]
        public async Task<ActionResult<CompanyDto>> CreateCompany([FromBody] CreateCompanyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CompanyCode))
                return BadRequest("Company code is required.");

            if (string.IsNullOrWhiteSpace(request.CompanyName))
                return BadRequest("Company name is required.");

            var codeExists = await _context.Companies
                .AnyAsync(c => c.CompanyCode.ToLower() == request.CompanyCode.Trim().ToLower());

            if (codeExists)
                return BadRequest($"Company code '{request.CompanyCode}' is already registered.");

            var company = new Company
            {
                CompanyCode = request.CompanyCode.Trim().ToUpperInvariant(),
                CompanyName = request.CompanyName.Trim(),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.Companies.Add(company);
            await _context.SaveChangesAsync();

            string dbName = string.IsNullOrWhiteSpace(request.DatabaseName)
                ? $"{request.CompanyCode.Trim()}_CRM"
                : request.DatabaseName.Trim();

            string serverName = string.IsNullOrWhiteSpace(request.ServerName)
                ? "."
                : request.ServerName.Trim();

            var companyDb = new CompanyDatabase
            {
                CompanyId = company.CompanyId,
                ServerName = serverName,
                DatabaseName = dbName,
                IsActive = true
            };

            _context.CompanyDatabases.Add(companyDb);
            await _context.SaveChangesAsync();

            var result = new CompanyDto
            {
                CompanyId = company.CompanyId,
                CompanyCode = company.CompanyCode,
                CompanyName = company.CompanyName,
                IsActive = company.IsActive,
                CreatedDate = company.CreatedDate,
                DatabaseName = companyDb.DatabaseName,
                ServerName = companyDb.ServerName,
                ActivePlanName = "None"
            };

            return CreatedAtAction(nameof(GetCompany), new { id = company.CompanyId }, result);
        }

        // PUT: api/companies/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCompany(int id, [FromBody] UpdateCompanyRequest request)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company == null)
                return NotFound($"Company #{id} not found.");

            if (string.IsNullOrWhiteSpace(request.CompanyName))
                return BadRequest("Company name is required.");

            company.CompanyName = request.CompanyName.Trim();
            if (request.IsActive.HasValue)
            {
                company.IsActive = request.IsActive.Value;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/companies/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeactivateCompany(int id)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == id);
            if (company == null)
                return NotFound($"Company #{id} not found.");

            company.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class CompanyDto
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public string DatabaseName { get; set; } = string.Empty;
        public string ServerName { get; set; } = string.Empty;
        public string ActivePlanName { get; set; } = string.Empty;
    }

    public class CompanyDetailsDto
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public List<CompanyDatabaseDto> Databases { get; set; } = new();
        public CompanySubscriptionSummaryDto? CurrentSubscription { get; set; }
    }

    public class CompanyDatabaseDto
    {
        public int CompanyDatabaseId { get; set; }
        public string ServerName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class CompanySubscriptionSummaryDto
    {
        public int SubscriptionId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CreateCompanyRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string? DatabaseName { get; set; }
        public string? ServerName { get; set; }
    }

    public class UpdateCompanyRequest
    {
        public string CompanyName { get; set; } = string.Empty;
        public bool? IsActive { get; set; }
    }
}
