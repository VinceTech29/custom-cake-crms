using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CC.domain.Entities;
using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CC.Services
{
    public record DashboardMetrics(
        int OpenInquiriesCount,
        int OrdersInProgressCount,
        int OverdueFollowupsCount,
        int FollowupsDueCount,
        int ProcessingOrdersCount,
        int ReadyForPickupCount,
        int TotalCustomersCount,
        decimal TotalRevenue
    );

    public record TransactionRecord(
        int Id,
        string ReferenceNo,
        DateTime Date,
        string Type, // "Order" or "Payment"
        string CustomerName,
        string Details,
        decimal Amount,
        string PaymentMethod,
        string Status
    );

    public record ReportSummaryMetrics(
        decimal TotalRevenue,
        int TotalTransactions,
        decimal DailyRevenue,
        int DailyCount,
        decimal MonthlyRevenue,
        int MonthlyCount,
        decimal AnnualRevenue = 0,
        int AnnualCount = 0
    );

    public record UserSummaryMetrics(
        int TotalActive,
        int AdminCount,
        int ManagerCount,
        int StaffCount
    );

    public record SubscriptionInfo(
        string PlanName,
        string Status,
        decimal Price,
        string BillingCycle,
        DateTime RenewalDate,
        string PaymentMethod,
        int UsedSeats,
        int MaxSeats
    );

    public record BillingHistoryItem(
        DateTime Date,
        string Description,
        decimal Amount,
        string Status
    );

    /// <summary>
    /// Centralized, database-driven service for CRM data operations using EF Core short-lived contexts.
    /// Operates against SQL Server LocalDB database 'CustomCakeCRM'.
    /// </summary>
    public static class CrmDataService
    {
        public const string ConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=CustomCakeCRM;Trusted_Connection=True;TrustServerCertificate=True;";
        public const int DefaultCompanyId = 2;
        public const int DefaultUserId = 4; // Staff user

        public static CrmDbContext CreateDbContext()
        {
            var options = new DbContextOptionsBuilder<CrmDbContext>()
                .UseSqlServer(ConnectionString)
                .Options;

            return new CrmDbContext(options);
        }

        public static async Task EnsureDatabaseReadyAsync()
        {
            try
            {
                await using var context = CreateDbContext();

                // 1. Seed Lookups if missing
                if (!await context.OrderStatuses.AnyAsync())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT OrderStatuses ON;
                        INSERT INTO OrderStatuses (StatusId, StatusName) VALUES
                            (0, 'Pending'), (1, 'Confirmed'), (2, 'Processing'),
                            (3, 'Completed'), (4, 'Ready'), (5, 'Cancelled');
                        SET IDENTITY_INSERT OrderStatuses OFF;
                    ");
                }

                if (!await context.FollowUpStatuses.AnyAsync())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT FollowUpStatuses ON;
                        INSERT INTO FollowUpStatuses (StatusId, StatusName) VALUES
                            (0, 'Pending'), (1, 'Completed'), (2, 'Cancelled'), (3, 'Overdue');
                        SET IDENTITY_INSERT FollowUpStatuses OFF;
                    ");
                }

                if (!await context.PaymentMethods.AnyAsync())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT PaymentMethods ON;
                        INSERT INTO PaymentMethods (MethodId, MethodName) VALUES
                            (0, 'Cash'), (1, 'GCash'), (2, 'Bank Transfer'), (3, 'Credit Card'), (4, 'Other');
                        SET IDENTITY_INSERT PaymentMethods OFF;
                    ");
                }

                if (!await context.PaymentStatuses.AnyAsync())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT PaymentStatuses ON;
                        INSERT INTO PaymentStatuses (StatusId, StatusName) VALUES
                            (0, 'Pending'), (1, 'Completed'), (2, 'Failed'), (3, 'Refunded');
                        SET IDENTITY_INSERT PaymentStatuses OFF;
                    ");
                }

                // Roles
                if (!await context.Roles.AnyAsync())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT Roles ON;
                        INSERT INTO Roles (RoleId, RoleName) VALUES
                            (1, 'SuperAdmin'), (2, 'Business Admin'), (3, 'Manager'), (4, 'Staff');
                        SET IDENTITY_INSERT Roles OFF;
                    ");
                }

                // Subscription Plans
                if (!await context.SubscriptionPlans.AnyAsync())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT SubscriptionPlans ON;
                        INSERT INTO SubscriptionPlans (PlanId, PlanName, Price, DurationDays, MaxUsers) VALUES
                            (1, 'Starter Plan', 4399, 365, 3),
                            (2, 'Pro Plan', 9599, 365, 10),
                            (3, 'Enterprise Plan', 19999, 365, 50);
                        SET IDENTITY_INSERT SubscriptionPlans OFF;
                    ");
                }

                // Subscription Statuses
                if (!await context.SubscriptionStatuses.AnyAsync())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT SubscriptionStatuses ON;
                        INSERT INTO SubscriptionStatuses (StatusId, StatusName) VALUES
                            (1, 'Active'), (2, 'Expired'), (3, 'Cancelled');
                        SET IDENTITY_INSERT SubscriptionStatuses OFF;
                    ");
                }

                // Active Company Subscription
                if (!await context.Subscriptions.AnyAsync(s => s.CompanyId == DefaultCompanyId))
                {
                    context.Subscriptions.Add(new Subscription
                    {
                        CompanyId = DefaultCompanyId,
                        PlanId = 2, // Pro Plan
                        StatusId = 1, // Active
                        StartDate = new DateTime(2026, 1, 1),
                        EndDate = new DateTime(2027, 1, 1)
                    });
                    await context.SaveChangesAsync();
                }

                // Shop Staff & Admin Users matching media_1789390916454.png
                int userCount = await context.AppUsers.CountAsync(u => u.CompanyId == DefaultCompanyId);
                if (userCount < 5)
                {
                    var existingEmails = await context.AppUsers.Select(u => u.Email.ToLower()).ToListAsync();

                    var defaultUsers = new List<SystemUser>
                    {
                        new SystemUser
                        {
                            CompanyId = DefaultCompanyId,
                            RoleId = 2, // Business Admin
                            Username = "admin",
                            Email = "lea.abad@cakeshop.ph",
                            FirstName = "Lea",
                            LastName = "Abad",
                            Phone = "+63 917 111 2233",
                            IsActive = true,
                            PasswordHash = "admin123",
                            CreatedDate = new DateTime(2026, 1, 1),
                            LastLoginDate = new DateTime(2026, 8, 24)
                        },
                        new SystemUser
                        {
                            CompanyId = DefaultCompanyId,
                            RoleId = 3, // Manager
                            Username = "mark.perez",
                            Email = "mark.perez@cakeshop.ph",
                            FirstName = "Mark",
                            LastName = "Perez",
                            Phone = "+63 918 222 3344",
                            IsActive = true,
                            PasswordHash = "admin123",
                            CreatedDate = new DateTime(2026, 1, 15),
                            LastLoginDate = new DateTime(2026, 8, 23)
                        },
                        new SystemUser
                        {
                            CompanyId = DefaultCompanyId,
                            RoleId = 4, // Staff
                            Username = "carrie.ngo",
                            Email = "carrie.ngo@cakeshop.ph",
                            FirstName = "Carrie",
                            LastName = "Ngo",
                            Phone = "+63 919 333 4455",
                            IsActive = true,
                            PasswordHash = "admin123",
                            CreatedDate = new DateTime(2026, 2, 1),
                            LastLoginDate = new DateTime(2026, 8, 24)
                        },
                        new SystemUser
                        {
                            CompanyId = DefaultCompanyId,
                            RoleId = 4, // Staff
                            Username = "dana.santos",
                            Email = "dana.santos@cakeshop.ph",
                            FirstName = "Dana",
                            LastName = "Santos",
                            Phone = "+63 920 444 5566",
                            IsActive = true,
                            PasswordHash = "admin123",
                            CreatedDate = new DateTime(2026, 2, 10),
                            LastLoginDate = new DateTime(2026, 8, 22)
                        },
                        new SystemUser
                        {
                            CompanyId = DefaultCompanyId,
                            RoleId = 4, // Staff
                            Username = "jose.uy",
                            Email = "jose.uy@cakeshop.ph",
                            FirstName = "Jose",
                            LastName = "Uy",
                            Phone = "+63 921 555 6677",
                            IsActive = false,
                            PasswordHash = "admin123",
                            CreatedDate = new DateTime(2026, 3, 1),
                            LastLoginDate = new DateTime(2026, 7, 10)
                        }
                    };

                    foreach (var u in defaultUsers)
                    {
                        if (!existingEmails.Contains(u.Email.ToLower()))
                        {
                            context.AppUsers.Add(u);
                        }
                    }
                    await context.SaveChangesAsync();
                }

                // Operational data (Customers, Orders, Inquiries, Follow-ups, Payments)
                // is NOT seeded — users create all records via the CRUD UI.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrmDataService.EnsureDatabaseReadyAsync] {ex.Message}");
            }
        }

        // =========================================================
        // CUSTOMERS CRUD
        // =========================================================

        public static async Task<List<Customer>> GetCustomersAsync(string? searchQuery = null)
        {
            await using var context = CreateDbContext();
            IQueryable<Customer> query = context.Customers
                .AsNoTracking()
                .Include(c => c.Orders);

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string s = searchQuery.Trim().ToLower();
                query = query.Where(c =>
                    c.FirstName.ToLower().Contains(s) ||
                    c.LastName.ToLower().Contains(s) ||
                    c.Email.ToLower().Contains(s) ||
                    c.Phone.ToLower().Contains(s));
            }

            return await query
                .OrderBy(c => c.FirstName)
                .ThenBy(c => c.LastName)
                .ToListAsync();
        }

        public static async Task<Customer?> GetCustomerByIdAsync(int customerId)
        {
            await using var context = CreateDbContext();
            return await context.Customers
                .AsNoTracking()
                .Include(c => c.Orders)
                .Include(c => c.Inquiries)
                .Include(c => c.FollowUps)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId);
        }

        public static async Task<Customer> CreateCustomerAsync(Customer customer)
        {
            await using var context = CreateDbContext();
            if (customer.CompanyId <= 0) customer.CompanyId = DefaultCompanyId;
            if (customer.CreatedByUserId <= 0) customer.CreatedByUserId = DefaultUserId;
            if (customer.RegisteredDate == default) customer.RegisteredDate = DateTime.UtcNow;

            context.Customers.Add(customer);
            await context.SaveChangesAsync();
            return customer;
        }

        public static async Task<Customer> UpdateCustomerAsync(Customer customer)
        {
            await using var context = CreateDbContext();
            var existing = await context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId);
            if (existing == null) throw new InvalidOperationException($"Customer #{customer.CustomerId} not found in database.");

            existing.FirstName = customer.FirstName;
            existing.LastName = customer.LastName;
            existing.Email = customer.Email;
            existing.Phone = customer.Phone;
            existing.AddressText = customer.AddressText;
            existing.CakePreferences = customer.CakePreferences;
            existing.Notes = customer.Notes;

            await context.SaveChangesAsync();
            return existing;
        }

        public static async Task<bool> DeleteCustomerAsync(int customerId)
        {
            await using var context = CreateDbContext();
            var existing = await context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId);
            if (existing == null) return false;

            context.Customers.Remove(existing);
            await context.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // SALES ORDERS CRUD
        // =========================================================

        public static async Task<List<SalesOrder>> GetOrdersAsync(string? statusFilter = null, string? searchQuery = null)
        {
            await using var context = CreateDbContext();
            IQueryable<SalesOrder> query = context.SalesOrders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .Include(o => o.Payments)
                    .ThenInclude(p => p.Method);

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                int targetStatus = statusFilter switch
                {
                    "Pending" => 0,
                    "Confirmed" => 1,
                    "Processing" => 2,
                    "Completed" => 3,
                    "Ready" => 4,
                    "Cancelled" => 5,
                    _ => -1
                };

                if (targetStatus >= 0)
                {
                    query = query.Where(o => o.StatusId == targetStatus);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string s = searchQuery.Trim().ToLower();
                query = query.Where(o =>
                    o.OrderId.ToString().Contains(s) ||
                    (o.DesignTheme != null && o.DesignTheme.ToLower().Contains(s)) ||
                    (o.CakeSize != null && o.CakeSize.ToLower().Contains(s)) ||
                    (o.Flavor != null && o.Flavor.ToLower().Contains(s)) ||
                    (o.Customer != null && (o.Customer.FirstName.ToLower().Contains(s) || o.Customer.LastName.ToLower().Contains(s))));
            }

            return await query
                .OrderByDescending(o => o.OrderId)
                .ToListAsync();
        }

        public static async Task<SalesOrder?> GetOrderByIdAsync(int orderId)
        {
            await using var context = CreateDbContext();
            return await context.SalesOrders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);
        }

        public static async Task<SalesOrder> CreateOrderAsync(SalesOrder order)
        {
            await using var context = CreateDbContext();
            if (order.CreatedByUserId <= 0) order.CreatedByUserId = DefaultUserId;
            if (order.OrderDate == default) order.OrderDate = DateTime.UtcNow;

            if (order.OrderDetails.Count == 0 && order.TotalAmount > 0)
            {
                order.OrderDetails.Add(new SalesOrderDetail
                {
                    ItemDescription = string.IsNullOrWhiteSpace(order.DesignTheme) ? "Custom Cake" : order.DesignTheme,
                    Quantity = 1,
                    UnitPrice = order.TotalAmount
                });
            }

            context.SalesOrders.Add(order);
            await context.SaveChangesAsync();
            return order;
        }

        public static async Task<SalesOrder> UpdateOrderAsync(SalesOrder order)
        {
            await using var context = CreateDbContext();
            var existing = await context.SalesOrders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

            if (existing == null) throw new InvalidOperationException($"Order #{order.OrderId} not found in database.");

            int oldStatus = existing.StatusId;
            int newStatus = order.StatusId;

            // Enforce forward-only status lifecycle rule
            if (oldStatus != newStatus)
            {
                if (!OrderStatusWorkflow.CanTransition(oldStatus, newStatus))
                {
                    throw new InvalidOperationException($"Invalid order status transition from '{OrderStatusWorkflow.GetStatusName(oldStatus)}' to '{OrderStatusWorkflow.GetStatusName(newStatus)}'. The order status lifecycle is forward-only.");
                }
            }

            existing.CustomerId = order.CustomerId;
            existing.StatusId = order.StatusId;
            existing.CakeSize = order.CakeSize;
            existing.Flavor = order.Flavor;
            existing.DesignTheme = order.DesignTheme;
            existing.TotalAmount = order.TotalAmount;
            existing.DeliveryDate = order.DeliveryDate;
            existing.Notes = order.Notes;

            if (existing.OrderDetails.Any())
            {
                var d = existing.OrderDetails.First();
                d.UnitPrice = order.TotalAmount;
                d.ItemDescription = string.IsNullOrWhiteSpace(order.DesignTheme) ? $"{order.CakeSize} - {order.Flavor}" : order.DesignTheme;
            }

            await context.SaveChangesAsync();

            if (oldStatus != newStatus)
            {
                await LogAuditAsync("ORDER_STATUS_CHANGED", $"Order #{order.OrderId} status updated from '{OrderStatusWorkflow.GetStatusName(oldStatus)}' to '{OrderStatusWorkflow.GetStatusName(newStatus)}'.");
            }

            return existing;
        }

        public static async Task<bool> DeleteOrderAsync(int orderId)
        {
            await using var context = CreateDbContext();
            var existing = await context.SalesOrders
                .Include(o => o.OrderDetails)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (existing == null) return false;

            context.SalesOrders.Remove(existing);
            await context.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // INQUIRIES CRUD
        // =========================================================

        public static async Task<List<CustomerInquiry>> GetInquiriesAsync(string? statusFilter = null, string? searchQuery = null)
        {
            await using var context = CreateDbContext();
            IQueryable<CustomerInquiry> query = context.CustomerInquiries
                .AsNoTracking()
                .Include(i => i.Customer);

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(i => i.Status == statusFilter);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string s = searchQuery.Trim().ToLower();
                query = query.Where(i =>
                    i.InquiryCode.ToLower().Contains(s) ||
                    i.CakeType.ToLower().Contains(s) ||
                    i.AssignedTo.ToLower().Contains(s) ||
                    (i.Customer != null && (i.Customer.FirstName.ToLower().Contains(s) || i.Customer.LastName.ToLower().Contains(s))));
            }

            return await query
                .OrderByDescending(i => i.InquiryId)
                .ToListAsync();
        }

        public static async Task<CustomerInquiry?> GetInquiryByIdAsync(int inquiryId)
        {
            await using var context = CreateDbContext();
            return await context.CustomerInquiries
                .AsNoTracking()
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.InquiryId == inquiryId);
        }

        public static async Task<CustomerInquiry> CreateInquiryAsync(CustomerInquiry inquiry)
        {
            await using var context = CreateDbContext();
            if (string.IsNullOrWhiteSpace(inquiry.InquiryCode))
            {
                int count = await context.CustomerInquiries.CountAsync() + 1;
                inquiry.InquiryCode = $"INQ-2026-{count:D4}";
            }
            if (inquiry.CreatedAt == default) inquiry.CreatedAt = DateTime.UtcNow;

            context.CustomerInquiries.Add(inquiry);
            await context.SaveChangesAsync();
            return inquiry;
        }

        public static async Task LogAuditAsync(string actionType, string description, int? userId = null)
        {
            try
            {
                await using var context = CreateDbContext();
                int uid = userId ?? SessionService.CurrentUser?.UserId ?? DefaultUserId;
                context.SystemAuditLogs.Add(new SystemAuditLog
                {
                    UserId = uid,
                    ActionType = actionType,
                    ActionDescription = description,
                    Timestamp = DateTime.UtcNow
                });
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Audit log warning: {ex.Message}");
            }
        }

        public static async Task<CustomerInquiry> UpdateInquiryAsync(CustomerInquiry inquiry)
        {
            await using var context = CreateDbContext();
            var existing = await context.CustomerInquiries.FirstOrDefaultAsync(i => i.InquiryId == inquiry.InquiryId);
            if (existing == null) throw new InvalidOperationException($"Inquiry #{inquiry.InquiryId} not found in database.");

            string oldStatus = existing.Status;
            string newStatus = inquiry.Status;

            // Enforce forward-only status lifecycle rule
            if (!string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                if (!InquiryStatusWorkflow.CanTransition(oldStatus, newStatus))
                {
                    throw new InvalidOperationException($"Invalid status transition from '{oldStatus}' to '{newStatus}'. The inquiry lifecycle is forward-only.");
                }
            }

            existing.CustomerId = inquiry.CustomerId;
            existing.CakeType = inquiry.CakeType;
            existing.EventDate = inquiry.EventDate;
            existing.AssignedTo = inquiry.AssignedTo;
            existing.EstimatedBudget = inquiry.EstimatedBudget;
            existing.Status = inquiry.Status;
            existing.Notes = inquiry.Notes;

            await context.SaveChangesAsync();

            if (!string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                await LogAuditAsync("INQUIRY_STATUS_CHANGED", $"Inquiry {existing.InquiryCode} status updated from '{oldStatus}' to '{newStatus}'.");
            }

            return existing;
        }

        public static async Task<bool> UpdateInquiryStatusAsync(int inquiryId, string newStatus)
        {
            await using var context = CreateDbContext();
            var existing = await context.CustomerInquiries.FirstOrDefaultAsync(i => i.InquiryId == inquiryId);
            if (existing == null) return false;

            string oldStatus = existing.Status;
            if (!string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                if (!InquiryStatusWorkflow.CanTransition(oldStatus, newStatus))
                {
                    throw new InvalidOperationException($"Invalid status transition from '{oldStatus}' to '{newStatus}'. The inquiry lifecycle is forward-only.");
                }
            }

            existing.Status = newStatus;
            await context.SaveChangesAsync();

            if (!string.Equals(oldStatus, newStatus, StringComparison.OrdinalIgnoreCase))
            {
                await LogAuditAsync("INQUIRY_STATUS_CHANGED", $"Inquiry {existing.InquiryCode} status updated from '{oldStatus}' to '{newStatus}'.");
            }

            return true;
        }

        public static async Task<bool> DeleteInquiryAsync(int inquiryId)
        {
            await using var context = CreateDbContext();
            var existing = await context.CustomerInquiries.FirstOrDefaultAsync(i => i.InquiryId == inquiryId);
            if (existing == null) return false;

            context.CustomerInquiries.Remove(existing);
            await context.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // FOLLOW-UPS CRUD
        // =========================================================

        public static async Task<List<CustomerFollowUp>> GetFollowUpsAsync(string? statusFilter = null, string? searchQuery = null)
        {
            await using var context = CreateDbContext();
            IQueryable<CustomerFollowUp> query = context.CustomerFollowUps
                .AsNoTracking()
                .Include(f => f.Customer);

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                int targetStatus = statusFilter switch
                {
                    "Pending" => 0,
                    "Completed" => 1,
                    "Cancelled" => 2,
                    "Overdue" => 3,
                    _ => -1
                };

                if (targetStatus >= 0)
                {
                    query = query.Where(f => f.StatusId == targetStatus);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string s = searchQuery.Trim().ToLower();
                query = query.Where(f =>
                    f.FollowUpId.ToString().Contains(s) ||
                    (f.Notes != null && f.Notes.ToLower().Contains(s)) ||
                    (f.Customer != null && (f.Customer.FirstName.ToLower().Contains(s) || f.Customer.LastName.ToLower().Contains(s))));
            }

            return await query
                .OrderByDescending(f => f.FollowUpDate)
                .ToListAsync();
        }

        public static async Task<CustomerFollowUp> CreateFollowUpAsync(CustomerFollowUp followUp)
        {
            await using var context = CreateDbContext();
            if (followUp.StaffUserId <= 0) followUp.StaffUserId = DefaultUserId;

            context.CustomerFollowUps.Add(followUp);
            await context.SaveChangesAsync();
            return followUp;
        }

        public static async Task<CustomerFollowUp> UpdateFollowUpAsync(CustomerFollowUp followUp)
        {
            await using var context = CreateDbContext();
            var existing = await context.CustomerFollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUp.FollowUpId);
            if (existing == null) throw new InvalidOperationException($"Follow-up #{followUp.FollowUpId} not found in database.");

            int oldStatus = existing.StatusId;
            int newStatus = followUp.StatusId;

            // Enforce forward-only status lifecycle rule
            if (oldStatus != newStatus)
            {
                if (!FollowUpStatusWorkflow.CanTransition(oldStatus, newStatus))
                {
                    throw new InvalidOperationException($"Invalid follow-up status transition from '{FollowUpStatusWorkflow.GetStatusName(oldStatus)}' to '{FollowUpStatusWorkflow.GetStatusName(newStatus)}'. The follow-up status lifecycle is forward-only.");
                }
            }

            existing.CustomerId = followUp.CustomerId;
            existing.FollowUpDate = followUp.FollowUpDate;
            existing.NextFollowUpDate = followUp.NextFollowUpDate;
            existing.StatusId = followUp.StatusId;
            existing.Notes = followUp.Notes;

            await context.SaveChangesAsync();

            if (oldStatus != newStatus)
            {
                await LogAuditAsync("FOLLOWUP_STATUS_CHANGED", $"Follow-up #FOL-{followUp.FollowUpId} status updated from '{FollowUpStatusWorkflow.GetStatusName(oldStatus)}' to '{FollowUpStatusWorkflow.GetStatusName(newStatus)}'.");
            }

            return existing;
        }

        public static async Task<bool> DeleteFollowUpAsync(int followUpId)
        {
            await using var context = CreateDbContext();
            var existing = await context.CustomerFollowUps.FirstOrDefaultAsync(f => f.FollowUpId == followUpId);
            if (existing == null) return false;

            context.CustomerFollowUps.Remove(existing);
            await context.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // PAYMENTS CRUD
        // =========================================================

        public static async Task<List<Payment>> GetPaymentsAsync(string? statusFilter = null, string? searchQuery = null)
        {
            await using var context = CreateDbContext();
            IQueryable<Payment> query = context.Payments
                .AsNoTracking()
                .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer);

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                int targetStatus = statusFilter switch
                {
                    "Pending" => 0,
                    "Completed" => 1,
                    "Failed" => 2,
                    "Refunded" => 3,
                    _ => -1
                };

                if (targetStatus >= 0)
                {
                    query = query.Where(p => p.StatusId == targetStatus);
                }
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string s = searchQuery.Trim().ToLower();
                query = query.Where(p =>
                    p.PaymentId.ToString().Contains(s) ||
                    p.TransactionReference.ToLower().Contains(s) ||
                    (p.Order != null && p.Order.Customer != null &&
                        (p.Order.Customer.FirstName.ToLower().Contains(s) || p.Order.Customer.LastName.ToLower().Contains(s))));
            }

            return await query
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
        }

        public static async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            await using var context = CreateDbContext();
            if (payment.ProcessedByUserId <= 0) payment.ProcessedByUserId = DefaultUserId;
            if (payment.PaymentDate == default) payment.PaymentDate = DateTime.UtcNow;

            context.Payments.Add(payment);
            await context.SaveChangesAsync();
            return payment;
        }

        public static async Task<Payment> UpdatePaymentAsync(Payment payment)
        {
            await using var context = CreateDbContext();
            var existing = await context.Payments.FirstOrDefaultAsync(p => p.PaymentId == payment.PaymentId);
            if (existing == null) throw new InvalidOperationException($"Payment #{payment.PaymentId} not found in database.");

            int oldStatus = existing.StatusId;
            int newStatus = payment.StatusId;

            // Enforce forward-only status lifecycle rule
            if (oldStatus != newStatus)
            {
                if (!PaymentStatusWorkflow.CanTransition(oldStatus, newStatus))
                {
                    throw new InvalidOperationException($"Invalid payment status transition from '{PaymentStatusWorkflow.GetStatusName(oldStatus)}' to '{PaymentStatusWorkflow.GetStatusName(newStatus)}'. The payment status lifecycle is forward-only.");
                }
            }

            existing.OrderId = payment.OrderId;
            existing.MethodId = payment.MethodId;
            existing.StatusId = payment.StatusId;
            existing.Amount = payment.Amount;
            existing.PaymentDate = payment.PaymentDate;
            existing.TransactionReference = payment.TransactionReference;

            await context.SaveChangesAsync();

            if (oldStatus != newStatus)
            {
                await LogAuditAsync("PAYMENT_STATUS_CHANGED", $"Payment #{payment.PaymentId} status updated from '{PaymentStatusWorkflow.GetStatusName(oldStatus)}' to '{PaymentStatusWorkflow.GetStatusName(newStatus)}'.");
            }

            return existing;
        }

        public static async Task<bool> DeletePaymentAsync(int paymentId)
        {
            await using var context = CreateDbContext();
            var existing = await context.Payments.FirstOrDefaultAsync(p => p.PaymentId == paymentId);
            if (existing == null) return false;

            context.Payments.Remove(existing);
            await context.SaveChangesAsync();
            return true;
        }

        // =========================================================
        // DASHBOARD METRICS (Dynamic Live Aggregate Queries)
        // =========================================================

        public static async Task<DashboardMetrics> GetDashboardMetricsAsync()
        {
            await using var context = CreateDbContext();

            // Open inquiries: status is New or In Progress or Quoted
            int openInquiries = await context.CustomerInquiries
                .CountAsync(i => i.Status == "New" || i.Status == "In Progress" || i.Status == "Quoted");

            // Orders in progress: StatusId == 2 (Processing) or 1 (Confirmed)
            int ordersInProgress = await context.SalesOrders
                .CountAsync(o => o.StatusId == 1 || o.StatusId == 2);

            // Processing orders specifically: StatusId == 2
            int processingOrders = await context.SalesOrders
                .CountAsync(o => o.StatusId == 2);

            // Ready for pickup: StatusId == 4
            int readyOrders = await context.SalesOrders
                .CountAsync(o => o.StatusId == 4);

            // Follow-ups due: StatusId == 0 (Pending)
            int followupsDue = await context.CustomerFollowUps
                .CountAsync(f => f.StatusId == 0);

            // Overdue follow-ups: StatusId == 0 and FollowUpDate < Today
            var today = DateTime.Today;
            int overdueFollowups = await context.CustomerFollowUps
                .CountAsync(f => f.StatusId == 0 && f.FollowUpDate < today);

            // Total Customers
            int totalCustomers = await context.Customers.CountAsync();

            // Total Revenue: sum of completed payments or completed orders
            decimal totalRevenue = await context.Payments
                .Where(p => p.StatusId == 1)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            if (totalRevenue == 0)
            {
                totalRevenue = await context.SalesOrders
                    .Where(o => o.StatusId == 3)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            }

            return new DashboardMetrics(
                OpenInquiriesCount: openInquiries,
                OrdersInProgressCount: ordersInProgress,
                OverdueFollowupsCount: overdueFollowups,
                FollowupsDueCount: followupsDue,
                ProcessingOrdersCount: processingOrders,
                ReadyForPickupCount: readyOrders,
                TotalCustomersCount: totalCustomers,
                TotalRevenue: totalRevenue
            );
        }

        // =========================================================
        // MANAGER OVERALL TRANSACTIONS & REPORTS
        // =========================================================

        public static async Task<List<TransactionRecord>> GetOverallTransactionsAsync(
            string period = "All", // "All", "Daily", "Monthly"
            DateTime? filterDate = null,
            string? typeFilter = null, // "All", "Orders", "Payments"
            string? searchQuery = null)
        {
            await using var context = CreateDbContext();
            var target = filterDate ?? DateTime.Today;
            var list = new List<TransactionRecord>();

            // 1. Fetch Orders
            if (string.IsNullOrEmpty(typeFilter) || typeFilter == "All" || typeFilter == "Orders")
            {
                var orderQuery = context.SalesOrders
                    .Include(o => o.Customer)
                    .Include(o => o.Status)
                    .AsNoTracking();

                var orders = await orderQuery.ToListAsync();

                foreach (var ord in orders)
                {
                    string custName = ord.Customer != null
                        ? $"{ord.Customer.FirstName} {ord.Customer.LastName}".Trim()
                        : "Walk-in Customer";

                    string details = !string.IsNullOrWhiteSpace(ord.CakeSize) || !string.IsNullOrWhiteSpace(ord.Flavor)
                        ? $"{ord.CakeSize} - {ord.Flavor}".Trim('-', ' ')
                        : ord.DesignTheme;

                    list.Add(new TransactionRecord(
                        Id: ord.OrderId,
                        ReferenceNo: $"#ORD-{ord.OrderId:D4}",
                        Date: ord.OrderDate,
                        Type: "Order",
                        CustomerName: custName,
                        Details: details,
                        Amount: ord.TotalAmount,
                        PaymentMethod: "N/A",
                        Status: ord.Status?.StatusName ?? "Pending"
                    ));
                }
            }

            // 2. Fetch Payments
            if (string.IsNullOrEmpty(typeFilter) || typeFilter == "All" || typeFilter == "Payments")
            {
                var paymentQuery = context.Payments
                    .Include(p => p.Order)
                        .ThenInclude(o => o!.Customer)
                    .Include(p => p.Method)
                    .Include(p => p.Status)
                    .AsNoTracking();

                var payments = await paymentQuery.ToListAsync();

                foreach (var pay in payments)
                {
                    string custName = pay.Order?.Customer != null
                        ? $"{pay.Order.Customer.FirstName} {pay.Order.Customer.LastName}".Trim()
                        : "Walk-in Customer";

                    string refNo = !string.IsNullOrWhiteSpace(pay.TransactionReference)
                        ? pay.TransactionReference
                        : $"#PAY-{pay.PaymentId:D4}";

                    list.Add(new TransactionRecord(
                        Id: pay.PaymentId,
                        ReferenceNo: refNo,
                        Date: pay.PaymentDate,
                        Type: "Payment",
                        CustomerName: custName,
                        Details: pay.Order != null ? $"Order #ORD-{pay.Order.OrderId:D4}" : "Direct Collection",
                        Amount: pay.Amount,
                        PaymentMethod: pay.Method?.MethodName ?? "Cash",
                        Status: pay.Status?.StatusName ?? "Completed"
                    ));
                }
            }

            // 3. Filter by Period
            if (string.Equals(period, "Daily", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(t => t.Date.Date == target.Date).ToList();
            }
            else if (string.Equals(period, "Monthly", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(t => t.Date.Year == target.Year && t.Date.Month == target.Month).ToList();
            }
            else if (string.Equals(period, "Annually", StringComparison.OrdinalIgnoreCase))
            {
                list = list.Where(t => t.Date.Year == target.Year).ToList();
            }

            // 4. Filter by Search Query
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var q = searchQuery.Trim().ToLowerInvariant();
                list = list.Where(t =>
                    t.ReferenceNo.ToLowerInvariant().Contains(q) ||
                    t.CustomerName.ToLowerInvariant().Contains(q) ||
                    t.Details.ToLowerInvariant().Contains(q) ||
                    t.PaymentMethod.ToLowerInvariant().Contains(q) ||
                    t.Status.ToLowerInvariant().Contains(q) ||
                    t.Type.ToLowerInvariant().Contains(q)
                ).ToList();
            }

            // Sort newest first
            return list.OrderByDescending(t => t.Date).ToList();
        }

        public static async Task<ReportSummaryMetrics> GetReportSummaryMetricsAsync(DateTime? targetDate = null)
        {
            var date = targetDate ?? DateTime.Today;
            await using var context = CreateDbContext();

            // Total revenue (completed payments or active orders)
            decimal totalPaymentRevenue = await context.Payments
                .Where(p => p.StatusId == 1) // Completed
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            if (totalPaymentRevenue == 0)
            {
                totalPaymentRevenue = await context.SalesOrders
                    .Where(o => o.StatusId != 5) // Not Cancelled
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            }

            int totalPayments = await context.Payments.CountAsync();
            int totalOrders = await context.SalesOrders.CountAsync();

            // Daily metrics
            decimal dailyRevenue = await context.Payments
                .Where(p => p.StatusId == 1 && p.PaymentDate.Date == date.Date)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            if (dailyRevenue == 0)
            {
                dailyRevenue = await context.SalesOrders
                    .Where(o => o.StatusId != 5 && o.OrderDate.Date == date.Date)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            }

            int dailyPayments = await context.Payments.CountAsync(p => p.PaymentDate.Date == date.Date);
            int dailyOrders = await context.SalesOrders.CountAsync(o => o.OrderDate.Date == date.Date);

            // Monthly metrics
            decimal monthlyRevenue = await context.Payments
                .Where(p => p.StatusId == 1 && p.PaymentDate.Year == date.Year && p.PaymentDate.Month == date.Month)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            if (monthlyRevenue == 0)
            {
                monthlyRevenue = await context.SalesOrders
                    .Where(o => o.StatusId != 5 && o.OrderDate.Year == date.Year && o.OrderDate.Month == date.Month)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            }

            int monthlyPayments = await context.Payments.CountAsync(p => p.PaymentDate.Year == date.Year && p.PaymentDate.Month == date.Month);
            int monthlyOrders = await context.SalesOrders.CountAsync(o => o.OrderDate.Year == date.Year && o.OrderDate.Month == date.Month);

            // Annual metrics
            decimal annualRevenue = await context.Payments
                .Where(p => p.StatusId == 1 && p.PaymentDate.Year == date.Year)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            if (annualRevenue == 0)
            {
                annualRevenue = await context.SalesOrders
                    .Where(o => o.StatusId != 5 && o.OrderDate.Year == date.Year)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            }

            int annualPayments = await context.Payments.CountAsync(p => p.PaymentDate.Year == date.Year);
            int annualOrders = await context.SalesOrders.CountAsync(o => o.OrderDate.Year == date.Year);

            return new ReportSummaryMetrics(
                TotalRevenue: totalPaymentRevenue,
                TotalTransactions: totalPayments + totalOrders,
                DailyRevenue: dailyRevenue,
                DailyCount: dailyPayments + dailyOrders,
                MonthlyRevenue: monthlyRevenue,
                MonthlyCount: monthlyPayments + monthlyOrders,
                AnnualRevenue: annualRevenue,
                AnnualCount: annualPayments + annualOrders
            );
        }

        // =========================================================
        // ADMIN USER MANAGEMENT CRUD
        // =========================================================

        public static async Task<List<Role>> GetRolesAsync()
        {
            await using var context = CreateDbContext();
            return await context.Roles
                .AsNoTracking()
                .OrderBy(r => r.RoleId)
                .ToListAsync();
        }

        public static async Task<List<SystemUser>> GetUsersAsync(
            string? searchQuery = null,
            string? roleFilter = null,
            string? statusFilter = null)
        {
            await using var context = CreateDbContext();
            IQueryable<SystemUser> query = context.AppUsers
                .Include(u => u.Role)
                .Where(u => u.CompanyId == DefaultCompanyId)
                .AsNoTracking();

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var q = searchQuery.Trim().ToLowerInvariant();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(q) ||
                    u.LastName.ToLower().Contains(q) ||
                    u.Username.ToLower().Contains(q) ||
                    u.Email.ToLower().Contains(q) ||
                    (u.Role != null && u.Role.RoleName.ToLower().Contains(q)));
            }

            if (!string.IsNullOrWhiteSpace(roleFilter) && roleFilter != "All Roles")
            {
                query = query.Where(u => u.Role != null && u.Role.RoleName == roleFilter);
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All Status")
            {
                bool activeOnly = statusFilter.Equals("Active", StringComparison.OrdinalIgnoreCase);
                query = query.Where(u => u.IsActive == activeOnly);
            }

            return await query
                .OrderBy(u => u.RoleId)
                .ThenBy(u => u.LastName)
                .ToListAsync();
        }

        public static async Task<UserSummaryMetrics> GetUserSummaryMetricsAsync()
        {
            await using var context = CreateDbContext();
            var users = await context.AppUsers
                .Include(u => u.Role)
                .Where(u => u.CompanyId == DefaultCompanyId)
                .AsNoTracking()
                .ToListAsync();

            int totalActive = users.Count(u => u.IsActive);
            int adminCount = users.Count(u => u.IsActive && u.Role != null && (u.Role.RoleName == "Business Admin" || u.Role.RoleName == "Admin" || u.Role.RoleName == "SuperAdmin"));
            int managerCount = users.Count(u => u.IsActive && u.Role != null && u.Role.RoleName == "Manager");
            int staffCount = users.Count(u => u.IsActive && u.Role != null && u.Role.RoleName == "Staff");

            return new UserSummaryMetrics(totalActive, adminCount, managerCount, staffCount);
        }

        public static async Task<SystemUser> CreateUserAsync(SystemUser user, string? password = null)
        {
            await using var context = CreateDbContext();
            if (user.CompanyId <= 0) user.CompanyId = DefaultCompanyId;
            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = string.IsNullOrWhiteSpace(password) ? "admin123" : password;
            }
            if (user.CreatedDate == default) user.CreatedDate = DateTime.UtcNow;

            context.AppUsers.Add(user);
            await context.SaveChangesAsync();
            return user;
        }

        public static async Task<SystemUser> UpdateUserAsync(SystemUser user, string? newPassword = null)
        {
            await using var context = CreateDbContext();
            var existing = await context.AppUsers.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            if (existing == null) throw new InvalidOperationException($"User #{user.UserId} not found in database.");

            existing.FirstName = user.FirstName;
            existing.LastName = user.LastName;
            existing.Username = user.Username;
            existing.Email = user.Email;
            existing.Phone = user.Phone;
            existing.RoleId = user.RoleId;
            existing.IsActive = user.IsActive;

            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                existing.PasswordHash = newPassword;
            }

            await context.SaveChangesAsync();
            return existing;
        }

        public static async Task ToggleUserStatusAsync(int userId, bool isActive)
        {
            await using var context = CreateDbContext();
            var existing = await context.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            if (existing != null)
            {
                existing.IsActive = isActive;
                await context.SaveChangesAsync();
            }
        }

        // =========================================================
        // ADMIN SUBSCRIPTION MANAGEMENT
        // =========================================================

        public static async Task<SubscriptionInfo> GetSubscriptionInfoAsync()
        {
            await using var context = CreateDbContext();
            var sub = await context.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.Status)
                .Where(s => s.CompanyId == DefaultCompanyId)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            int usedSeats = await context.AppUsers
                .CountAsync(u => u.CompanyId == DefaultCompanyId && u.IsActive);

            if (sub != null && sub.Plan != null)
            {
                return new SubscriptionInfo(
                    PlanName: sub.Plan.PlanName,
                    Status: sub.Status?.StatusName ?? "Active",
                    Price: sub.Plan.Price,
                    BillingCycle: "year",
                    RenewalDate: sub.EndDate,
                    PaymentMethod: "Visa ending in 4242",
                    UsedSeats: usedSeats,
                    MaxSeats: sub.Plan.MaxUsers
                );
            }

            // Fallback default
            return new SubscriptionInfo(
                PlanName: "Pro Plan",
                Status: "Active",
                Price: 9599m,
                BillingCycle: "year",
                RenewalDate: DateTime.Today.AddMonths(11),
                PaymentMethod: "Visa ending in 4242",
                UsedSeats: usedSeats > 0 ? usedSeats : 5,
                MaxSeats: 10
            );
        }

        public static async Task<List<BillingHistoryItem>> GetBillingHistoryAsync()
        {
            // Real billing records can also be dynamically backed or supplemented
            await Task.Yield();
            return new List<BillingHistoryItem>
            {
                new(new DateTime(2025, 8, 15), "Pro Plan - Annual Subscription", 9599.00m, "Paid"),
                new(new DateTime(2024, 8, 15), "Pro Plan - Annual Subscription", 9599.00m, "Paid"),
                new(new DateTime(2023, 8, 15), "Starter Plan - Annual Subscription", 4399.00m, "Paid")
            };
        }

        public static async Task RenewSubscriptionAsync()
        {
            await using var context = CreateDbContext();
            var sub = await context.Subscriptions
                .Where(s => s.CompanyId == DefaultCompanyId)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            if (sub != null)
            {
                sub.EndDate = sub.EndDate.AddYears(1);
                await context.SaveChangesAsync();
            }
        }

        public static async Task UpgradeDowngradePlanAsync(string targetPlanName)
        {
            await using var context = CreateDbContext();
            var plan = await context.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.PlanName.ToLower() == targetPlanName.ToLower());

            if (plan != null)
            {
                var sub = await context.Subscriptions
                    .Where(s => s.CompanyId == DefaultCompanyId)
                    .OrderByDescending(s => s.SubscriptionId)
                    .FirstOrDefaultAsync();

                if (sub != null)
                {
                    sub.PlanId = plan.PlanId;
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}
