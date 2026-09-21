using CC.api.Models;
using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CC.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomerFollowUpsController : ControllerBase
    {
        private readonly CrmDbContext _context;

        public CustomerFollowUpsController(CrmDbContext context)
        {
            _context = context;
        }

        // GET: api/customerfollowups?page=1&pageSize=10&search=...&statusId=...&staffUserId=...
        [HttpGet]
        public async Task<ActionResult<PagedResult<FollowUpDto>>> GetFollowUps(
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromQuery] string? search = null,
            [FromQuery] int? statusId = null,
            [FromQuery] int? staffUserId = null,
            [FromQuery] int? customerId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            pageSize = 10;
            page = Math.Max(1, page);

            IQueryable<CustomerFollowUp> query = _context.CustomerFollowUps
                .AsNoTracking()
                .Include(f => f.Customer)
                .Include(f => f.StaffUser)
                .Include(f => f.Status)
                .Where(f => f.Customer != null && f.Customer.CompanyId == companyId);

            if (statusId.HasValue && statusId.Value >= 0)
            {
                query = query.Where(f => f.StatusId == statusId.Value);
            }

            if (staffUserId.HasValue && staffUserId.Value > 0)
            {
                query = query.Where(f => f.StaffUserId == staffUserId.Value);
            }

            if (customerId.HasValue && customerId.Value > 0)
            {
                query = query.Where(f => f.CustomerId == customerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(f =>
                    (f.Customer != null && (f.Customer.FirstName.ToLower().Contains(s) || f.Customer.LastName.ToLower().Contains(s))) ||
                    f.Notes.ToLower().Contains(s) ||
                    (f.StaffUser != null && f.StaffUser.Username.ToLower().Contains(s)));
            }

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderBy(f => f.FollowUpDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FollowUpDto
                {
                    FollowUpId = f.FollowUpId,
                    CustomerId = f.CustomerId,
                    CustomerName = f.Customer != null ? (f.Customer.FirstName + " " + f.Customer.LastName).Trim() : "Unknown",
                    CustomerPhone = f.Customer != null ? f.Customer.Phone : string.Empty,
                    StaffUserId = f.StaffUserId,
                    StaffUserName = f.StaffUser != null ? f.StaffUser.Username : "Unassigned",
                    StatusId = f.StatusId,
                    StatusName = f.Status != null ? f.Status.StatusName : "Pending",
                    FollowUpDate = f.FollowUpDate,
                    NextFollowUpDate = f.NextFollowUpDate,
                    Notes = f.Notes
                })
                .ToListAsync();

            return Ok(new PagedResult<FollowUpDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        // GET: api/customerfollowups/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<FollowUpDto>> GetFollowUp(
            int id,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var followUp = await _context.CustomerFollowUps
                .AsNoTracking()
                .Include(f => f.Customer)
                .Include(f => f.StaffUser)
                .Include(f => f.Status)
                .FirstOrDefaultAsync(f => f.FollowUpId == id && f.Customer != null && f.Customer.CompanyId == companyId);

            if (followUp == null)
                return NotFound($"Follow-up #{id} not found.");

            var dto = new FollowUpDto
            {
                FollowUpId = followUp.FollowUpId,
                CustomerId = followUp.CustomerId,
                CustomerName = followUp.Customer != null ? $"{followUp.Customer.FirstName} {followUp.Customer.LastName}".Trim() : "Unknown",
                CustomerPhone = followUp.Customer?.Phone ?? string.Empty,
                StaffUserId = followUp.StaffUserId,
                StaffUserName = followUp.StaffUser?.Username ?? "Unassigned",
                StatusId = followUp.StatusId,
                StatusName = followUp.Status?.StatusName ?? "Pending",
                FollowUpDate = followUp.FollowUpDate,
                NextFollowUpDate = followUp.NextFollowUpDate,
                Notes = followUp.Notes
            };

            return Ok(dto);
        }

        // POST: api/customerfollowups
        [HttpPost]
        public async Task<ActionResult<FollowUpDto>> CreateFollowUp(
            [FromBody] CreateFollowUpRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromHeader(Name = "X-User-Id")] int userId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId && c.CompanyId == companyId);
            if (customer == null)
                return BadRequest($"Customer #{request.CustomerId} does not exist in this company.");

            int staffId = request.StaffUserId > 0 ? request.StaffUserId : userId;

            var followUp = new CustomerFollowUp
            {
                CustomerId = request.CustomerId,
                StaffUserId = staffId,
                StatusId = request.StatusId >= 0 ? request.StatusId : 0, // Default Pending
                FollowUpDate = request.FollowUpDate ?? DateTime.UtcNow,
                NextFollowUpDate = request.NextFollowUpDate,
                Notes = request.Notes?.Trim() ?? string.Empty
            };

            _context.CustomerFollowUps.Add(followUp);
            await _context.SaveChangesAsync();

            var result = new FollowUpDto
            {
                FollowUpId = followUp.FollowUpId,
                CustomerId = followUp.CustomerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
                CustomerPhone = customer.Phone,
                StaffUserId = staffId,
                StaffUserName = $"User #{staffId}",
                StatusId = followUp.StatusId,
                StatusName = "Pending",
                FollowUpDate = followUp.FollowUpDate,
                NextFollowUpDate = followUp.NextFollowUpDate,
                Notes = followUp.Notes
            };

            return CreatedAtAction(nameof(GetFollowUp), new { id = followUp.FollowUpId }, result);
        }

        // PUT: api/customerfollowups/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateFollowUp(
            int id,
            [FromBody] UpdateFollowUpRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var followUp = await _context.CustomerFollowUps
                .Include(f => f.Customer)
                .FirstOrDefaultAsync(f => f.FollowUpId == id && f.Customer != null && f.Customer.CompanyId == companyId);

            if (followUp == null)
                return NotFound($"Follow-up #{id} not found.");

            if (request.StatusId.HasValue && request.StatusId.Value >= 0) followUp.StatusId = request.StatusId.Value;
            if (request.StaffUserId.HasValue && request.StaffUserId.Value > 0) followUp.StaffUserId = request.StaffUserId.Value;
            if (request.FollowUpDate.HasValue) followUp.FollowUpDate = request.FollowUpDate.Value;
            if (request.NextFollowUpDate.HasValue) followUp.NextFollowUpDate = request.NextFollowUpDate.Value;
            if (request.Notes != null) followUp.Notes = request.Notes.Trim();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/customerfollowups/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteFollowUp(
            int id,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var followUp = await _context.CustomerFollowUps
                .Include(f => f.Customer)
                .FirstOrDefaultAsync(f => f.FollowUpId == id && f.Customer != null && f.Customer.CompanyId == companyId);

            if (followUp == null)
                return NotFound($"Follow-up #{id} not found.");

            _context.CustomerFollowUps.Remove(followUp);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class FollowUpDto
    {
        public int FollowUpId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int StaffUserId { get; set; }
        public string StaffUserName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime FollowUpDate { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class CreateFollowUpRequest
    {
        public int CustomerId { get; set; }
        public int StaffUserId { get; set; }
        public int StatusId { get; set; } = 0;
        public DateTime? FollowUpDate { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateFollowUpRequest
    {
        public int? StatusId { get; set; }
        public int? StaffUserId { get; set; }
        public DateTime? FollowUpDate { get; set; }
        public DateTime? NextFollowUpDate { get; set; }
        public string? Notes { get; set; }
    }
}
