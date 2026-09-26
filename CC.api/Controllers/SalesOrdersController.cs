using CC.api.Models;
using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CC.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesOrdersController : ControllerBase
    {
        private readonly CrmDbContext _context;

        public SalesOrdersController(CrmDbContext context)
        {
            _context = context;
        }

        // GET: api/salesorders?page=1&pageSize=10&search=...&statusId=...
        [HttpGet]
        public async Task<ActionResult<PagedResult<SalesOrderListDto>>> GetOrders(
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromQuery] string? search = null,
            [FromQuery] int? statusId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            pageSize = 10;
            page = Math.Max(1, page);

            IQueryable<SalesOrder> query = _context.SalesOrders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Status)
                .Include(o => o.Payments)
                .Where(o => o.Customer != null && o.Customer.CompanyId == companyId);

            if (statusId.HasValue && statusId.Value >= 0)
            {
                query = query.Where(o => o.StatusId == statusId.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(o =>
                    (o.Customer != null && (o.Customer.FirstName.ToLower().Contains(s) || o.Customer.LastName.ToLower().Contains(s))) ||
                    o.Flavor.ToLower().Contains(s) ||
                    o.DesignTheme.ToLower().Contains(s) ||
                    o.OrderId.ToString().Contains(s));
            }

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new SalesOrderListDto
                {
                    OrderId = o.OrderId,
                    CustomerId = o.CustomerId,
                    CustomerName = o.Customer != null ? (o.Customer.FirstName + " " + o.Customer.LastName).Trim() : "Unknown",
                    CustomerPhone = o.Customer != null ? o.Customer.Phone : string.Empty,
                    OrderDate = o.OrderDate,
                    DeliveryDate = o.DeliveryDate,
                    CakeSize = o.CakeSize,
                    Flavor = o.Flavor,
                    DesignTheme = o.DesignTheme,
                    TotalAmount = o.TotalAmount,
                    AmountPaid = o.Payments.Sum(p => p.Amount),
                    StatusId = o.StatusId,
                    StatusName = o.Status != null ? o.Status.StatusName : "Unknown"
                })
                .ToListAsync();

            return Ok(new PagedResult<SalesOrderListDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        // GET: api/salesorders/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<SalesOrderDetailDto>> GetOrder(
            int id,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var order = await _context.SalesOrders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.Status)
                .Include(o => o.DeliveryAddress)
                .Include(o => o.OrderDetails)
                .Include(o => o.Payments)
                    .ThenInclude(p => p.Status)
                .Include(o => o.Payments)
                    .ThenInclude(p => p.Method)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.Customer != null && o.Customer.CompanyId == companyId);

            if (order == null)
                return NotFound($"Sales Order #{id} not found.");

            var dto = new SalesOrderDetailDto
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer != null ? (order.Customer.FirstName + " " + order.Customer.LastName).Trim() : "Unknown",
                CustomerPhone = order.Customer != null ? order.Customer.Phone : string.Empty,
                CustomerEmail = order.Customer != null ? order.Customer.Email : string.Empty,
                OrderDate = order.OrderDate,
                DeliveryDate = order.DeliveryDate,
                CakeSize = order.CakeSize,
                Flavor = order.Flavor,
                DesignTheme = order.DesignTheme,
                Notes = order.Notes,
                TotalAmount = order.TotalAmount,
                AmountPaid = order.Payments.Sum(p => p.Amount),
                BalanceDue = order.TotalAmount - order.Payments.Sum(p => p.Amount),
                StatusId = order.StatusId,
                StatusName = order.Status != null ? order.Status.StatusName : "Unknown",
                DeliveryAddress = order.DeliveryAddress != null
                    ? $"{order.DeliveryAddress.AddressLine1}, {order.DeliveryAddress.City}, {order.DeliveryAddress.State}".Trim(',', ' ')
                    : "Pickup / None",
                Items = order.OrderDetails.Select(d => new SalesOrderItemDto
                {
                    OrderDetailId = d.OrderDetailId,
                    ItemDescription = d.ItemDescription,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    TotalPrice = d.Quantity * d.UnitPrice
                }).ToList(),
                Payments = order.Payments.Select(p => new OrderPaymentDto
                {
                    PaymentId = p.PaymentId,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    MethodName = p.Method != null ? p.Method.MethodName : "Cash",
                    StatusName = p.Status != null ? p.Status.StatusName : "Completed",
                    TransactionReference = p.TransactionReference
                }).ToList()
            };

            return Ok(dto);
        }

        // POST: api/salesorders
        [HttpPost]
        public async Task<ActionResult<SalesOrderDetailDto>> CreateOrder(
            [FromBody] CreateSalesOrderRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromHeader(Name = "X-User-Id")] int userId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            if (userId <= 0)
                return BadRequest("A valid user ID is required in X-User-Id header.");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.CustomerId == request.CustomerId && c.CompanyId == companyId);
            if (customer == null)
                return BadRequest($"Customer #{request.CustomerId} does not exist in this company.");

            decimal totalAmount = request.TotalAmount;
            if (request.Items != null && request.Items.Count > 0 && totalAmount <= 0)
            {
                totalAmount = request.Items.Sum(i => i.Quantity * i.UnitPrice);
            }

            var order = new SalesOrder
            {
                CustomerId = request.CustomerId,
                CreatedByUserId = userId,
                DeliveryAddressId = request.DeliveryAddressId,
                StatusId = request.StatusId >= 0 ? request.StatusId : 0, // Default Pending (0)
                OrderDate = DateTime.UtcNow,
                DeliveryDate = request.DeliveryDate,
                CakeSize = request.CakeSize?.Trim() ?? string.Empty,
                Flavor = request.Flavor?.Trim() ?? string.Empty,
                DesignTheme = request.DesignTheme?.Trim() ?? string.Empty,
                Notes = request.Notes?.Trim() ?? string.Empty,
                TotalAmount = totalAmount
            };

            if (request.Items != null && request.Items.Count > 0)
            {
                foreach (var item in request.Items)
                {
                    order.OrderDetails.Add(new SalesOrderDetail
                    {
                        ItemDescription = item.ItemDescription,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice
                    });
                }
            }

            _context.SalesOrders.Add(order);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOrder), new { id = order.OrderId }, new SalesOrderDetailDto
            {
                OrderId = order.OrderId,
                CustomerId = order.CustomerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
                OrderDate = order.OrderDate,
                DeliveryDate = order.DeliveryDate,
                CakeSize = order.CakeSize,
                Flavor = order.Flavor,
                DesignTheme = order.DesignTheme,
                TotalAmount = order.TotalAmount,
                StatusId = order.StatusId,
                StatusName = "Pending"
            });
        }

        // PUT: api/salesorders/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateOrder(
            int id,
            [FromBody] UpdateSalesOrderRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var order = await _context.SalesOrders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.Customer != null && o.Customer.CompanyId == companyId);

            if (order == null)
                return NotFound($"Sales Order #{id} not found.");

            if (request.DeliveryDate.HasValue) order.DeliveryDate = request.DeliveryDate.Value;
            if (request.CakeSize != null) order.CakeSize = request.CakeSize.Trim();
            if (request.Flavor != null) order.Flavor = request.Flavor.Trim();
            if (request.DesignTheme != null) order.DesignTheme = request.DesignTheme.Trim();
            if (request.Notes != null) order.Notes = request.Notes.Trim();
            if (request.TotalAmount.HasValue && request.TotalAmount.Value >= 0) order.TotalAmount = request.TotalAmount.Value;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/salesorders/1/status
        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateOrderStatus(
            int id,
            [FromBody] UpdateOrderStatusRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var order = await _context.SalesOrders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.Customer != null && o.Customer.CompanyId == companyId);

            if (order == null)
                return NotFound($"Sales Order #{id} not found.");

            var statusExists = await _context.OrderStatuses.AnyAsync(s => s.StatusId == request.StatusId);
            if (!statusExists)
                return BadRequest($"StatusId #{request.StatusId} is not a valid OrderStatus.");

            order.StatusId = request.StatusId;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/salesorders/1 (Cancels order)
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> CancelOrder(
            int id,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var order = await _context.SalesOrders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.OrderId == id && o.Customer != null && o.Customer.CompanyId == companyId);

            if (order == null)
                return NotFound($"Sales Order #{id} not found.");

            order.StatusId = 5; // Cancelled
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }

    public class SalesOrderListDto
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string CakeSize { get; set; } = string.Empty;
        public string Flavor { get; set; } = string.Empty;
        public string DesignTheme { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue => Math.Max(0, TotalAmount - AmountPaid);
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
    }

    public class SalesOrderDetailDto
    {
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string CakeSize { get; set; } = string.Empty;
        public string Flavor { get; set; } = string.Empty;
        public string DesignTheme { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public List<SalesOrderItemDto> Items { get; set; } = new();
        public List<OrderPaymentDto> Payments { get; set; } = new();
    }

    public class SalesOrderItemDto
    {
        public int OrderDetailId { get; set; }
        public string ItemDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class OrderPaymentDto
    {
        public int PaymentId { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public string MethodName { get; set; } = string.Empty;
        public string StatusName { get; set; } = string.Empty;
        public string TransactionReference { get; set; } = string.Empty;
    }

    public class CreateSalesOrderRequest
    {
        public int CustomerId { get; set; }
        public int? DeliveryAddressId { get; set; }
        public DateTime? DeliveryDate { get; set; }
        public string CakeSize { get; set; } = string.Empty;
        public string Flavor { get; set; } = string.Empty;
        public string DesignTheme { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal TotalAmount { get; set; }
        public int StatusId { get; set; } = 0;
        public List<CreateSalesOrderItemRequest>? Items { get; set; }
    }

    public class CreateSalesOrderItemRequest
    {
        public string ItemDescription { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
    }

    public class UpdateSalesOrderRequest
    {
        public DateTime? DeliveryDate { get; set; }
        public string? CakeSize { get; set; }
        public string? Flavor { get; set; }
        public string? DesignTheme { get; set; }
        public string? Notes { get; set; }
        public decimal? TotalAmount { get; set; }
    }

    public class UpdateOrderStatusRequest
    {
        public int StatusId { get; set; }
    }
}
