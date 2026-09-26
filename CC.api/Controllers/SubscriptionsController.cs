using CC.api.Models;
using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CC.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly MasterCrmDbContext _context;

        public SubscriptionsController(MasterCrmDbContext context)
        {
            _context = context;
        }

        // GET: api/subscriptions?page=1&pageSize=10&search=...&status=...
        [HttpGet]
        public async Task<ActionResult<PagedResult<CompanySubscriptionItemDto>>> GetSubscriptions(
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            pageSize = 10;
            page = Math.Max(1, page);

            var companies = await _context.Companies.AsNoTracking().ToListAsync();
            var dbs = await _context.CompanyDatabases.AsNoTracking().Where(d => d.IsActive).ToListAsync();
            var subs = await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Include(s => s.Status)
                .ToListAsync();

            var list = new List<CompanySubscriptionItemDto>();
            var now = DateTime.UtcNow;

            foreach (var c in companies)
            {
                var sub = subs.Where(s => s.CompanyId == c.CompanyId).OrderByDescending(s => s.SubscriptionId).FirstOrDefault();
                var db = dbs.FirstOrDefault(d => d.CompanyId == c.CompanyId);

                string statusLabel = "No Plan";
                if (sub != null)
                {
                    if (sub.EndDate < now)
                    {
                        statusLabel = "Expired";
                    }
                    else if ((sub.EndDate - now).TotalDays <= 30)
                    {
                        statusLabel = "Expiring";
                    }
                    else
                    {
                        statusLabel = "Active";
                    }
                }

                list.Add(new CompanySubscriptionItemDto
                {
                    CompanyId = c.CompanyId,
                    CompanyCode = c.CompanyCode,
                    CompanyName = c.CompanyName,
                    DatabaseName = db?.DatabaseName ?? "N/A",
                    SubscriptionId = sub?.SubscriptionId ?? 0,
                    PlanId = sub?.PlanId ?? 0,
                    PlanName = sub?.Plan?.PlanName ?? "No Plan",
                    Price = sub?.Plan?.Price ?? 0m,
                    DurationDays = sub?.Plan?.DurationDays ?? 365,
                    MaxUsers = sub?.Plan?.MaxUsers ?? 0,
                    StatusId = sub?.StatusId ?? 2,
                    StatusName = statusLabel,
                    StartDate = sub?.StartDate ?? c.CreatedDate,
                    EndDate = sub?.EndDate ?? c.CreatedDate.AddYears(1)
                });
            }

            // Filtering
            IEnumerable<CompanySubscriptionItemDto> query = list;

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(x =>
                    x.CompanyName.ToLower().Contains(s) ||
                    x.CompanyCode.ToLower().Contains(s) ||
                    x.PlanName.ToLower().Contains(s));
            }

            if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
            {
                string st = status.Trim().ToLower();
                query = query.Where(x => x.StatusName.ToLower() == st);
            }

            var orderedList = query.OrderBy(x => x.CompanyId).ToList();
            int totalCount = orderedList.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = orderedList
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return Ok(new PagedResult<CompanySubscriptionItemDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        // GET: api/subscriptions/company/1
        [HttpGet("company/{companyId:int}")]
        public async Task<ActionResult<CompanySubscriptionItemDto>> GetCompanySubscription(int companyId)
        {
            var company = await _context.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null)
                return NotFound($"Company #{companyId} not found.");

            var db = await _context.CompanyDatabases
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.CompanyId == companyId && d.IsActive);

            var sub = await _context.Subscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Include(s => s.Status)
                .Where(s => s.CompanyId == companyId)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            var now = DateTime.UtcNow;
            string statusLabel = "No Plan";
            if (sub != null)
            {
                if (sub.EndDate < now) statusLabel = "Expired";
                else if ((sub.EndDate - now).TotalDays <= 30) statusLabel = "Expiring";
                else statusLabel = "Active";
            }

            var dto = new CompanySubscriptionItemDto
            {
                CompanyId = company.CompanyId,
                CompanyCode = company.CompanyCode,
                CompanyName = company.CompanyName,
                DatabaseName = db?.DatabaseName ?? "N/A",
                SubscriptionId = sub?.SubscriptionId ?? 0,
                PlanId = sub?.PlanId ?? 0,
                PlanName = sub?.Plan?.PlanName ?? "No Plan",
                Price = sub?.Plan?.Price ?? 0m,
                DurationDays = sub?.Plan?.DurationDays ?? 365,
                MaxUsers = sub?.Plan?.MaxUsers ?? 0,
                StatusId = sub?.StatusId ?? 2,
                StatusName = statusLabel,
                StartDate = sub?.StartDate ?? company.CreatedDate,
                EndDate = sub?.EndDate ?? company.CreatedDate.AddYears(1)
            };

            return Ok(dto);
        }

        // POST: api/subscriptions/assign
        [HttpPost("assign")]
        public async Task<IActionResult> AssignSubscription([FromBody] AssignSubscriptionRequest request)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == request.CompanyId);
            if (company == null)
                return NotFound($"Company #{request.CompanyId} not found.");

            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == request.PlanId);
            if (plan == null)
                return NotFound($"Plan #{request.PlanId} not found.");

            var existing = await _context.Subscriptions
                .Where(s => s.CompanyId == request.CompanyId)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            var startDate = request.StartDate ?? DateTime.UtcNow;
            var endDate = request.EndDate ?? startDate.AddDays(plan.DurationDays > 0 ? plan.DurationDays : 365);

            if (existing != null)
            {
                existing.PlanId = request.PlanId;
                existing.StatusId = request.StatusId > 0 ? request.StatusId : 1; // Active
                existing.StartDate = startDate;
                existing.EndDate = endDate;
            }
            else
            {
                _context.Subscriptions.Add(new Subscription
                {
                    CompanyId = request.CompanyId,
                    PlanId = request.PlanId,
                    StatusId = request.StatusId > 0 ? request.StatusId : 1,
                    StartDate = startDate,
                    EndDate = endDate
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Plan '{plan.PlanName}' successfully assigned to '{company.CompanyName}'." });
        }

        // POST: api/subscriptions/1/renew
        [HttpPost("{companyId:int}/renew")]
        public async Task<IActionResult> RenewSubscription(int companyId)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);
            if (company == null)
                return NotFound($"Company #{companyId} not found.");

            var current = await _context.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.CompanyId == companyId)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            if (current == null || current.Plan == null)
            {
                return BadRequest($"Company '{company.CompanyName}' does not have an active or previous subscription to renew.");
            }

            int days = current.Plan.DurationDays > 0 ? current.Plan.DurationDays : 365;
            var baseDate = current.EndDate > DateTime.UtcNow ? current.EndDate : DateTime.UtcNow;

            current.EndDate = baseDate.AddDays(days);
            current.StatusId = 1; // Active

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Subscription renewed successfully until {current.EndDate:yyyy-MM-dd}." });
        }
    }

    public class CompanySubscriptionItemDto
    {
        public int CompanyId { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public int SubscriptionId { get; set; }
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int MaxUsers { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class AssignSubscriptionRequest
    {
        public int CompanyId { get; set; }
        public int PlanId { get; set; }
        public int StatusId { get; set; } = 1;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
    }
}
