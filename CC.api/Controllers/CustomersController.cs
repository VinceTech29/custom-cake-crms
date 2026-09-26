using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CC.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly CrmDbContext _context;

        public CustomersController(CrmDbContext context)
        {
            _context = context;
        }

        // GET: api/customers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerDto>>> GetCustomers(
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required.");

            var customers = await _context.Customers
                .AsNoTracking()
                .Where(c => c.CompanyId == companyId)
                .OrderByDescending(c => c.RegisteredDate)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CompanyId = c.CompanyId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    AddressId = c.AddressId,
                    RegisteredDate = c.RegisteredDate,
                    CreatedByUserId = c.CreatedByUserId,
                    OrdersCount = c.Orders.Count
                })
                .ToListAsync();

            return Ok(customers);
        }

        // GET: api/customers/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CustomerDto>> GetCustomer(
            int id,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required.");

            var customer = await _context.Customers
                .AsNoTracking()
                .Where(c => c.CustomerId == id && c.CompanyId == companyId)
                .Select(c => new CustomerDto
                {
                    CustomerId = c.CustomerId,
                    CompanyId = c.CompanyId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    AddressId = c.AddressId,
                    RegisteredDate = c.RegisteredDate,
                    CreatedByUserId = c.CreatedByUserId,
                    OrdersCount = c.Orders.Count
                })
                .FirstOrDefaultAsync();

            if (customer == null)
                return NotFound("Customer not found.");

            return Ok(customer);
        }

        // POST: api/customers
        [HttpPost]
        public async Task<ActionResult<CustomerDto>> CreateCustomer(
            [FromBody] CreateCustomerRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromHeader(Name = "X-User-Id")] int userId)
        {
            if (userId <= 0)
                return BadRequest("A valid user ID is required.");

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest("First name is required.");

            var user = await _context.AppUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return BadRequest("User does not exist.");

            var customerCompanyId = user.CompanyId;

            if (customerCompanyId <= 0)
                return BadRequest("User is not assigned to a company.");

            var companyExists = await _context.Companies
                .AnyAsync(c => c.CompanyId == customerCompanyId);

            if (!companyExists)
                return BadRequest("User's company does not exist.");

            var customer = new Customer
            {
                CompanyId = customerCompanyId,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName?.Trim() ?? string.Empty,
                Email = request.Email?.Trim() ?? string.Empty,
                Phone = request.Phone?.Trim() ?? string.Empty,
                AddressId = request.AddressId,
                RegisteredDate = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var result = new CustomerDto
            {
                CustomerId = customer.CustomerId,
                CompanyId = customer.CompanyId,
                FirstName = customer.FirstName,
                LastName = customer.LastName,
                Email = customer.Email,
                Phone = customer.Phone,
                AddressId = customer.AddressId,
                RegisteredDate = customer.RegisteredDate,
                CreatedByUserId = customer.CreatedByUserId,
                OrdersCount = 0
            };

            return CreatedAtAction(
                nameof(GetCustomer),
                new { id = customer.CustomerId },
                result);
        }

        // PUT: api/customers/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCustomer(
            int id,
            [FromBody] UpdateCustomerRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required.");

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest("First name is required.");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == id &&
                    c.CompanyId == companyId);

            if (customer == null)
                return NotFound("Customer not found.");

            customer.FirstName = request.FirstName.Trim();
            customer.LastName = request.LastName?.Trim() ?? string.Empty;
            customer.Email = request.Email?.Trim() ?? string.Empty;
            customer.Phone = request.Phone?.Trim() ?? string.Empty;
            customer.AddressId = request.AddressId;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/customers/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCustomer(
            int id,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required.");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c =>
                    c.CustomerId == id &&
                    c.CompanyId == companyId);

            if (customer == null)
                return NotFound("Customer not found.");

            // Prevent deleting customers that already have orders/follow-ups
            var hasOrders = await _context.SalesOrders
                .AnyAsync(o => o.CustomerId == id);

            var hasFollowUps = await _context.CustomerFollowUps
                .AnyAsync(f => f.CustomerId == id);

            if (hasOrders || hasFollowUps)
            {
                return BadRequest(
                    "This customer cannot be deleted because they have existing records.");
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public int CompanyId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int? AddressId { get; set; }
        public DateTime RegisteredDate { get; set; }
        public int CreatedByUserId { get; set; }
        public int OrdersCount { get; set; }
    }

    public class CreateCustomerRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? AddressId { get; set; }
    }

    public class UpdateCustomerRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? AddressId { get; set; }
    }
}
