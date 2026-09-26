using System.Security.Cryptography;
using System.Text;
using CC.api.Models;
using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CC.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly CrmDbContext _context;
        private readonly MasterCrmDbContext _masterContext;

        public UsersController(CrmDbContext context, MasterCrmDbContext masterContext)
        {
            _context = context;
            _masterContext = masterContext;
        }

        // GET: api/users?page=1&pageSize=10&search=...&roleId=...&isActive=...
        [HttpGet]
        public async Task<ActionResult<PagedResult<UserDto>>> GetUsers(
            [FromHeader(Name = "X-Company-Id")] int companyId,
            [FromQuery] string? search = null,
            [FromQuery] int? roleId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            pageSize = 10;
            page = Math.Max(1, page);

            IQueryable<SystemUser> query = _context.AppUsers
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.CompanyId == companyId);

            if (roleId.HasValue && roleId.Value > 0)
            {
                query = query.Where(u => u.RoleId == roleId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim().ToLower();
                query = query.Where(u =>
                    u.Username.ToLower().Contains(s) ||
                    u.FirstName.ToLower().Contains(s) ||
                    u.LastName.ToLower().Contains(s) ||
                    u.Email.ToLower().Contains(s));
            }

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    CompanyId = u.CompanyId,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null ? u.Role.RoleName : "User",
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Phone = u.Phone,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    LastLoginDate = u.LastLoginDate
                })
                .ToListAsync();

            return Ok(new PagedResult<UserDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount
            });
        }

        // GET: api/users/1
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserDto>> GetUser(
            int id,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var user = await _context.AppUsers
                .AsNoTracking()
                .Include(u => u.Role)
                .Where(u => u.UserId == id && u.CompanyId == companyId)
                .Select(u => new UserDto
                {
                    UserId = u.UserId,
                    CompanyId = u.CompanyId,
                    RoleId = u.RoleId,
                    RoleName = u.Role != null ? u.Role.RoleName : "User",
                    Username = u.Username,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Phone = u.Phone,
                    IsActive = u.IsActive,
                    CreatedDate = u.CreatedDate,
                    LastLoginDate = u.LastLoginDate
                })
                .FirstOrDefaultAsync();

            if (user == null)
                return NotFound($"User #{id} not found.");

            return Ok(user);
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<UserDto>> CreateUser(
            [FromBody] CreateUserRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest("Username is required.");

            if (string.IsNullOrWhiteSpace(request.FirstName))
                return BadRequest("First name is required.");

            // Check username uniqueness within company
            var exists = await _context.AppUsers
                .AnyAsync(u => u.CompanyId == companyId && u.Username.ToLower() == request.Username.Trim().ToLower());
            if (exists)
                return BadRequest($"Username '{request.Username}' is already taken in this company.");

            // Enforce Subscription Plan seat limit (MaxUsers)
            var activeSub = await _masterContext.Subscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Where(s => s.CompanyId == companyId && s.StatusId == 1 && s.EndDate >= DateTime.UtcNow)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            if (activeSub?.Plan != null && activeSub.Plan.MaxUsers > 0)
            {
                int activeCount = await _context.AppUsers
                    .CountAsync(u => u.CompanyId == companyId && u.IsActive);

                if (activeCount >= activeSub.Plan.MaxUsers)
                {
                    return BadRequest($"Cannot add user. The active subscription plan '{activeSub.Plan.PlanName}' seat limit of {activeSub.Plan.MaxUsers} user(s) has been reached. Please upgrade your subscription to add more team members.");
                }
            }

            string rawPassword = string.IsNullOrWhiteSpace(request.Password) ? "admin123" : request.Password;
            string passwordHash = ComputeSha256(rawPassword);

            var user = new SystemUser
            {
                CompanyId = companyId,
                RoleId = request.RoleId > 0 ? request.RoleId : 4, // Default to Staff
                Username = request.Username.Trim(),
                PasswordHash = passwordHash,
                Email = request.Email?.Trim() ?? string.Empty,
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName?.Trim() ?? string.Empty,
                Phone = request.Phone?.Trim() ?? string.Empty,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            _context.AppUsers.Add(user);
            await _context.SaveChangesAsync();

            var role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.RoleId == user.RoleId);

            var result = new UserDto
            {
                UserId = user.UserId,
                CompanyId = user.CompanyId,
                RoleId = user.RoleId,
                RoleName = role?.RoleName ?? "Staff",
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.Phone,
                IsActive = user.IsActive,
                CreatedDate = user.CreatedDate,
                LastLoginDate = null
            };

            return CreatedAtAction(nameof(GetUser), new { id = user.UserId }, result);
        }

        // PUT: api/users/1
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateUser(
            int id,
            [FromBody] UpdateUserRequest request,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == id && u.CompanyId == companyId);
            if (user == null)
                return NotFound($"User #{id} not found.");

            // Check if activating from inactive -> seat limit check
            if (request.IsActive.HasValue && request.IsActive.Value && !user.IsActive)
            {
                var activeSub = await _masterContext.Subscriptions
                    .AsNoTracking()
                    .Include(s => s.Plan)
                    .Where(s => s.CompanyId == companyId && s.StatusId == 1 && s.EndDate >= DateTime.UtcNow)
                    .OrderByDescending(s => s.SubscriptionId)
                    .FirstOrDefaultAsync();

                if (activeSub?.Plan != null && activeSub.Plan.MaxUsers > 0)
                {
                    int activeCount = await _context.AppUsers
                        .CountAsync(u => u.CompanyId == companyId && u.IsActive);

                    if (activeCount >= activeSub.Plan.MaxUsers)
                    {
                        return BadRequest($"Cannot activate user. The active subscription plan '{activeSub.Plan.PlanName}' seat limit of {activeSub.Plan.MaxUsers} user(s) has been reached.");
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(request.FirstName)) user.FirstName = request.FirstName.Trim();
            if (request.LastName != null) user.LastName = request.LastName.Trim();
            if (request.Email != null) user.Email = request.Email.Trim();
            if (request.Phone != null) user.Phone = request.Phone.Trim();
            if (request.RoleId.HasValue && request.RoleId.Value > 0) user.RoleId = request.RoleId.Value;
            if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                user.PasswordHash = ComputeSha256(request.Password);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/users/1
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeactivateUser(
            int id,
            [FromHeader(Name = "X-Company-Id")] int companyId)
        {
            if (companyId <= 0)
                return BadRequest("A valid company ID is required in X-Company-Id header.");

            var user = await _context.AppUsers.FirstOrDefaultAsync(u => u.UserId == id && u.CompanyId == companyId);
            if (user == null)
                return NotFound($"User #{id} not found.");

            user.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        private static string ComputeSha256(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
    }

    public class UserDto
    {
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastLoginDate { get; set; }
    }

    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int RoleId { get; set; } = 4;
    }

    public class UpdateUserRequest
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
        public string? Password { get; set; }
    }
}
