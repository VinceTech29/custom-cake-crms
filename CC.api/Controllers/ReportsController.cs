using CC.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CC.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly CrmDbContext _context;

        public ReportsController(CrmDbContext context)
        {
            _context = context;
        }

        // GET: api/reports/summary
        [HttpGet("summary")]
        public async Task<ActionResult<CrmSummaryReportDto>> GetSummaryReport(
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var ordersQuery = _context.SalesOrders
                .AsNoTracking()
                .Where(o => o.Customer != null && o.Customer.CompanyId == companyId);

            int totalOrders = await ordersQuery.CountAsync();
            int completedOrders = await ordersQuery.CountAsync(o => o.StatusId == 3);
            int pendingOrders = await ordersQuery.CountAsync(o => o.StatusId == 0 || o.StatusId == 1 || o.StatusId == 2);

            decimal totalRevenue = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == companyId && p.StatusId == 1)
                .SumAsync(p => p.Amount);

            decimal totalOrderValue = await ordersQuery.SumAsync(o => o.TotalAmount);
            decimal averageOrderValue = totalOrders > 0 ? Math.Round(totalOrderValue / totalOrders, 2) : 0m;

            int totalCustomers = await _context.Customers
                .AsNoTracking()
                .CountAsync(c => c.CompanyId == companyId);

            int pendingFollowUps = await _context.CustomerFollowUps
                .AsNoTracking()
                .CountAsync(f => f.Customer != null && f.Customer.CompanyId == companyId && (f.StatusId == 0 || f.StatusId == 3));

            return Ok(new CrmSummaryReportDto
            {
                TotalOrders = totalOrders,
                CompletedOrders = completedOrders,
                PendingOrders = pendingOrders,
                TotalRevenue = totalRevenue,
                AverageOrderValue = averageOrderValue,
                TotalCustomers = totalCustomers,
                PendingFollowUps = pendingFollowUps
            });
        }

        // GET: api/reports/revenue-trend?months=6
        [HttpGet("revenue-trend")]
        public async Task<ActionResult<IEnumerable<MonthlyRevenueDto>>> GetRevenueTrend(
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromQuery] int months = 6)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            months = Math.Clamp(months, 1, 24);
            var startDate = DateTime.UtcNow.AddMonths(-months + 1);
            var startOfMonth = new DateTime(startDate.Year, startDate.Month, 1);

            var payments = await _context.Payments
                .AsNoTracking()
                .Where(p => p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == companyId
                            && p.StatusId == 1 && p.PaymentDate >= startOfMonth)
                .Select(p => new { p.PaymentDate, p.Amount })
                .ToListAsync();

            var result = new List<MonthlyRevenueDto>();
            for (int i = 0; i < months; i++)
            {
                var targetMonth = startOfMonth.AddMonths(i);
                var monthlyTotal = payments
                    .Where(p => p.PaymentDate.Year == targetMonth.Year && p.PaymentDate.Month == targetMonth.Month)
                    .Sum(p => p.Amount);

                result.Add(new MonthlyRevenueDto
                {
                    Year = targetMonth.Year,
                    Month = targetMonth.Month,
                    MonthName = targetMonth.ToString("MMM yyyy"),
                    Revenue = monthlyTotal
                });
            }

            return Ok(result);
        }

        // GET: api/reports/top-customers?top=5
        [HttpGet("top-customers")]
        public async Task<ActionResult<IEnumerable<TopCustomerDto>>> GetTopCustomers(
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromQuery] int top = 5)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            top = Math.Clamp(top, 1, 50);

            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId)
                .Select(c => new TopCustomerDto
                {
                    CustomerId = c.CustomerId,
                    CustomerName = (c.FirstName + " " + c.LastName).Trim(),
                    Phone = c.Phone,
                    Email = c.Email,
                    TotalOrders = c.Orders.Count,
                    TotalSpend = c.Orders.SelectMany(o => o.Payments.Where(p => p.StatusId == 1)).Sum(p => p.Amount)
                })
                .OrderByDescending(c => c.TotalSpend)
                .Take(top)
                .ToListAsync();

            return Ok(customers);
        }
    }

    public class CrmSummaryReportDto
    {
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalCustomers { get; set; }
        public int PendingFollowUps { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class TopCustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpend { get; set; }
    }
}
