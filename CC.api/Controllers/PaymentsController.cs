using CC.api.Models;
using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CC.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly CrmDbContext _context;

        public PaymentsController(CrmDbContext context)
        {
            _context = context;
        }

        // GET: api/payments?page=1&pageSize=10&search=...&statusId=...&methodId=...
        [HttpGet]
        public async Task<ActionResult<PagedResult<PaymentDto>>> GetPayments(
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromQuery] string? search = null,
            [FromQuery] int? orderId = null,
            [FromQuery] int? statusId = null,
            [FromQuery] int? methodId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            pageSize = 10;
            page = Math.Max(1, page);

            IQueryable<Payment> query = _context.Payments
                .AsNoTracking()
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer)
                .Include(p => p.Method)
                .Include(p => p.Status)
                .Include(p => p.ProcessedByUser)
                .Where(p => p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == companyId);

            if (orderId.HasValue && orderId.Value > 0)
            {
                query = query.Where(p => p.OrderId == orderId.Value);
            }

            if (statusId.HasValue && statusId.Value >= 0)
            {
                query = query.Where(p => p.StatusId == statusId.Value);
            }

            if (methodId.HasValue && methodId.Value > 0)
            {
                query = query.Where(p => p.MethodId == methodId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(p =>
                    p.TransactionReference.ToLower().Contains(s) ||
                    (p.Order != null && p.Order.Customer != null && (p.Order.Customer.FirstName.ToLower().Contains(s) || p.Order.Customer.LastName.ToLower().Contains(s))) ||
                    p.OrderId.ToString().Contains(s));
            }

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(p => p.PaymentDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PaymentDto
                {
                    PaymentId = p.PaymentId,
                    OrderId = p.OrderId,
                    CustomerName = p.Order != null && p.Order.Customer != null
                        ? (p.Order.Customer.FirstName + " " + p.Order.Customer.LastName).Trim()
                        : "Unknown",
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    MethodId = p.MethodId,
                    MethodName = p.Method != null ? p.Method.MethodName : "Cash",
                    StatusId = p.StatusId,
                    StatusName = p.Status != null ? p.Status.StatusName : "Completed",
                    TransactionReference = p.TransactionReference,
                    ProcessedBy = p.ProcessedByUser != null ? p.ProcessedByUser.Username : "System"
                })
                .ToListAsync();

            return Ok(new PagedResult<PaymentDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        // GET: api/payments/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<PaymentDto>> GetPayment(
            int id,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var payment = await _context.Payments
                .AsNoTracking()
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer)
                .Include(p => p.Method)
                .Include(p => p.Status)
                .Include(p => p.ProcessedByUser)
                .FirstOrDefaultAsync(p => p.PaymentId == id && p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == companyId);

            if (payment == null)
                return NotFound($"Payment #{id} not found.");

            var dto = new PaymentDto
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                CustomerName = payment.Order?.Customer != null
                    ? $"{payment.Order.Customer.FirstName} {payment.Order.Customer.LastName}".Trim()
                    : "Unknown",
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                MethodId = payment.MethodId,
                MethodName = payment.Method?.MethodName ?? "Cash",
                StatusId = payment.StatusId,
                StatusName = payment.Status?.StatusName ?? "Completed",
                TransactionReference = payment.TransactionReference,
                ProcessedBy = payment.ProcessedByUser?.Username ?? "System"
            };

            return Ok(dto);
        }

        // POST: api/payments
        [HttpPost]
        public async Task<ActionResult<PaymentDto>> CreatePayment(
            [FromBody] CreatePaymentRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromHeader(Name = "X-User-Id")] int userId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            if (userId <= 0)
                return BadRequest("A valid user ID is required in X-User-Id header.");

            if (request.Amount <= 0)
                return BadRequest("Payment amount must be greater than zero.");

            var order = await _context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId && o.Customer != null && o.Customer.CompanyId == companyId);

            if (order == null)
                return BadRequest($"Sales Order #{request.OrderId} does not exist in this company.");

            var payment = new Payment
            {
                OrderId = request.OrderId,
                MethodId = request.MethodId > 0 ? request.MethodId : 1, // Default Cash
                StatusId = request.StatusId >= 0 ? request.StatusId : 1, // Default Completed/Paid
                ProcessedByUserId = userId,
                Amount = request.Amount,
                PaymentDate = DateTime.UtcNow,
                TransactionReference = request.TransactionReference?.Trim() ?? $"PAY-{DateTime.UtcNow:yyyyMMddHHmmss}"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            var method = await _context.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(m => m.MethodId == payment.MethodId);
            var status = await _context.PaymentStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.StatusId == payment.StatusId);

            var result = new PaymentDto
            {
                PaymentId = payment.PaymentId,
                OrderId = payment.OrderId,
                CustomerName = $"{order.Customer?.FirstName} {order.Customer?.LastName}".Trim(),
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                MethodId = payment.MethodId,
                MethodName = method?.MethodName ?? "Cash",
                StatusId = payment.StatusId,
                StatusName = status?.StatusName ?? "Completed",
                TransactionReference = payment.TransactionReference,
                ProcessedBy = $"User #{userId}"
            };

            return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentId }, result);
        }

        // PUT: api/payments/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePayment(
            int id,
            [FromBody] UpdatePaymentRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer)
                .FirstOrDefaultAsync(p => p.PaymentId == id && p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == companyId);

            if (payment == null)
                return NotFound($"Payment #{id} not found.");

            if (request.MethodId.HasValue && request.MethodId.Value > 0) payment.MethodId = request.MethodId.Value;
            if (request.StatusId.HasValue && request.StatusId.Value >= 0) payment.StatusId = request.StatusId.Value;
            if (!string.IsNullOrWhiteSpace(request.TransactionReference)) payment.TransactionReference = request.TransactionReference.Trim();

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/payments/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePayment(
            int id,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var payment = await _context.Payments
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer)
                .FirstOrDefaultAsync(p => p.PaymentId == id && p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == companyId);

            if (payment == null)
                return NotFound($"Payment #{id} not found.");

            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class PaymentDto
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public int MethodId { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
        public string ProcessedBy { get; set; } = string.Empty;
    }

    public class CreatePaymentRequest
    {
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public int MethodId { get; set; } = 1;
        public int StatusId { get; set; } = 1;
        public string? TransactionReference { get; set; }
    }

    public class UpdatePaymentRequest
    {
        public int? MethodId { get; set; }
        public int? StatusId { get; set; }
        public string? TransactionReference { get; set; }
    }
}
