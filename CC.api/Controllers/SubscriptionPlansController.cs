using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CC.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SubscriptionPlansController : ControllerBase
    {
        private readonly MasterCrmDbContext _context;

        public SubscriptionPlansController(MasterCrmDbContext context)
        {
            _context = context;
        }

        // GET: api/subscriptionplans
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubscriptionPlanDto>>> GetPlans()
        {
            var plans = await _context.SubscriptionPlans.AsNoTracking().ToListAsync();
            var activeSubs = await _context.Subscriptions
                .AsNoTracking()
                .Where(s => s.StatusId == 1 && s.EndDate >= DateTime.UtcNow)
                .ToListAsync();

            var list = plans.Select(p => new SubscriptionPlanDto
            {
                PlanId = p.PlanId,
                PlanName = p.PlanName,
                Price = p.Price,
                DurationDays = p.DurationDays,
                MaxUsers = p.MaxUsers,
                ActiveSubscribersCount = activeSubs.Count(s => s.PlanId == p.PlanId)
            }).OrderBy(p => p.Price).ToList();

            return Ok(list);
        }

        // GET: api/subscriptionplans/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SubscriptionPlanDto>> GetPlan(int id)
        {
            var plan = await _context.SubscriptionPlans.AsNoTracking().FirstOrDefaultAsync(p => p.PlanId == id);
            if (plan == null)
                return NotFound($"Subscription Plan #{id} not found.");

            int activeCount = await _context.Subscriptions
                .AsNoTracking()
                .CountAsync(s => s.PlanId == id && s.StatusId == 1 && s.EndDate >= DateTime.UtcNow);

            var dto = new SubscriptionPlanDto
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Price = plan.Price,
                DurationDays = plan.DurationDays,
                MaxUsers = plan.MaxUsers,
                ActiveSubscribersCount = activeCount
            };

            return Ok(dto);
        }

        // POST: api/subscriptionplans
        [HttpPost]
        public async Task<ActionResult<SubscriptionPlanDto>> CreatePlan([FromBody] CreateSubscriptionPlanRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PlanName))
                return BadRequest("Plan name is required.");

            if (request.Price < 0)
                return BadRequest("Plan price cannot be negative.");

            if (request.DurationDays <= 0)
                return BadRequest("Duration days must be at least 1.");

            if (request.MaxUsers <= 0)
                return BadRequest("Max users must be at least 1.");

            var plan = new SubscriptionPlan
            {
                PlanName = request.PlanName.Trim(),
                Price = request.Price,
                DurationDays = request.DurationDays,
                MaxUsers = request.MaxUsers
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            var result = new SubscriptionPlanDto
            {
                PlanId = plan.PlanId,
                PlanName = plan.PlanName,
                Price = plan.Price,
                DurationDays = plan.DurationDays,
                MaxUsers = plan.MaxUsers,
                ActiveSubscribersCount = 0
            };

            return CreatedAtAction(nameof(GetPlan), new { id = plan.PlanId }, result);
        }

        // PUT: api/subscriptionplans/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] UpdateSubscriptionPlanRequest request)
        {
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == id);
            if (plan == null)
                return NotFound($"Subscription Plan #{id} not found.");

            if (!string.IsNullOrWhiteSpace(request.PlanName)) plan.PlanName = request.PlanName.Trim();
            if (request.Price.HasValue && request.Price.Value >= 0) plan.Price = request.Price.Value;
            if (request.DurationDays.HasValue && request.DurationDays.Value > 0) plan.DurationDays = request.DurationDays.Value;
            if (request.MaxUsers.HasValue && request.MaxUsers.Value > 0) plan.MaxUsers = request.MaxUsers.Value;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/subscriptionplans/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _context.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == id);
            if (plan == null)
                return NotFound($"Subscription Plan #{id} not found.");

            var hasActiveSubs = await _context.Subscriptions
                .AnyAsync(s => s.PlanId == id && s.StatusId == 1 && s.EndDate >= DateTime.UtcNow);

            if (hasActiveSubs)
            {
                return BadRequest("Cannot delete a plan that has active subscribers.");
            }

            _context.SubscriptionPlans.Remove(plan);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class SubscriptionPlanDto
    {
        public int PlanId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationDays { get; set; }
        public int MaxUsers { get; set; }
        public int ActiveSubscribersCount { get; set; }
    }

    public class CreateSubscriptionPlanRequest
    {
        public string PlanName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationDays { get; set; } = 365;
        public int MaxUsers { get; set; } = 10;
    }

    public class UpdateSubscriptionPlanRequest
    {
        public string? PlanName { get; set; }
        public decimal? Price { get; set; }
        public int? DurationDays { get; set; }
        public int? MaxUsers { get; set; }
    }
}
