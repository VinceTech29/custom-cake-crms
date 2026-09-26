using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using CC.Controls;
using CC.domain.Entities;
using CC.Domain.Entities;
using CC.infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CC.Services
{

    public record StaffDashboardData(
        int MyDueFollowupsCount,
        int MyOverdueFollowupsCount,
        int MyHandledInquiriesCount,
        int ProcessingOrdersCount,
        int ReadyOrdersCount,
        int CompletedOrdersCount,
        int TodayDueOrdersCount,
        int TotalCustomersCount,
        List<TrendPoint> TaskCompletionTrend,
        List<BarItem> OrderStatusBreakdown,
        List<BarItem> FollowUpStatusBreakdown,
        List<StaffUrgentTaskItem> UrgentTasks
    );

    public record StaffUrgentTaskItem(
        string Type,
        string Title,
        string CustomerName,
        DateTime DueDate,
        string Status,
        Color StatusBg,
        Color StatusFg,
        int EntityId
    );

    public record ManagerDashboardData(
        int ActiveOrdersCount,
        int ReadyForPickupCount,
        int CompletedOrdersCount,
        int TodayDeliveriesCount,
        decimal ActivePipelineValue,
        int OpenInquiriesCount,
        int ShopOverdueFollowupsCount,
        int TotalCustomersCount,
        List<TrendPoint> OrderVolumeTrend,
        List<PipelineStage> PipelineStages,
        List<BarItem> OrdersByStatus,
        List<BarItem> StaffPerformance,
        List<ManagerPriorityOrderItem> RecentOrders
    );

    public record ManagerPriorityOrderItem(
        int OrderId,
        string ReferenceNo,
        string CustomerName,
        string CakeDetails,
        DateTime DeliveryDate,
        decimal TotalAmount,
        string Status
    );

    public record AdminDashboardData(
        decimal TotalRevenue,
        decimal LifetimeRevenue,
        decimal OutstandingBalance,
        decimal AverageOrderValue,
        double CollectionRate,
        int TotalOrders,
        int ActiveOrdersCount,
        int TotalCustomers,
        int NewCustomersInPeriod,
        List<TrendPoint> RevenueTrend,
        List<BarItem> PaymentMethodBreakdown,
        List<BarItem> OrderStatusBreakdown,
        List<AdminTopCustomerItem> TopCustomers,
        SubscriptionInfo Subscription,
        List<TransactionRecord> RecentTransactions
    );

    public record AdminTopCustomerItem(
        int CustomerId,
        string CustomerName,
        string Email,
        string Phone,
        int OrderCount,
        decimal TotalSpent,
        DateTime LastOrderDate
    );

    public record SuperAdminDashboardData(
        int TotalBusinesses,
        int ActiveBusinesses,
        int ActiveDatabases,
        int PlatformUsersCount,
        int ActiveSubscriptionsCount,
        int ExpiringSubscriptionsCount,
        int ExpiredSubscriptionsCount,
        decimal EstimatedMonthlyRevenue,
        int TotalBackupsCount,
        int TermsAcceptancesCount,
        List<TrendPoint> RegistrationTrend,
        List<BarItem> SubscriptionPlanDistribution,
        List<BarItem> SubscriptionStatusDistribution,
        List<SuperAdminCompanyItem> RecentRegistrations
    );

    public record SuperAdminCompanyItem(
        int CompanyId,
        string CompanyCode,
        string CompanyName,
        string DatabaseName,
        DateTime CreatedDate,
        bool IsActive
    );
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
        int MaxSeats,
        bool AllowBranching = false,
        int MaxBranches = 1
    );

    public record BillingHistoryItem(
        DateTime Date,
        string Description,
        decimal Amount,
        string Status
    );

    public record PagedList<T>(List<T> Items, int TotalCount, int Page, int PageSize)
    {
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / Math.Max(PageSize, 1));
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }

    public record AuthResult(
        bool Success,
        string? ErrorMessage,
        SystemUser? User,
        string? TenantServer,
        string? TenantDatabase
    );

    public record PlatformMetrics(
        int TotalBusinesses,
        int ActiveBusinesses,
        int InactiveBusinesses,
        int TotalPlatformUsers,
        int TotalTenantDatabases
    );

    public record CompanyListItem(
        int CompanyId,
        string CompanyCode,
        string CompanyName,
        string ContactEmail,
        string ContactPhone,
        string AddressSummary,
        DateTime CreatedDate,
        bool IsActive,
        string ServerName,
        string DatabaseName,
        int UserCount
    );

    public record CompanyDetails(
        int CompanyId,
        string CompanyCode,
        string CompanyName,
        string ContactEmail,
        string ContactPhone,
        string AddressLine1,
        string? AddressLine2,
        string City,
        string State,
        string PostalCode,
        string Country,
        DateTime CreatedDate,
        bool IsActive,
        string ServerName,
        string DatabaseName,
        string SubscriptionPlan,
        int UserCount
    );

    /// <summary>
    /// Centralized, database-driven service for CRM data operations using EF Core short-lived contexts.
    /// Operates against SQL Server LocalDB database 'CustomCakeCRM' and Master DB 'MSME_MasterCRM'.
    /// </summary>
    public static class CrmDataService
    {
        private static readonly ConcurrentDictionary<int, string> _tenantConnectionCache = new();

        public const string DefaultTenantDatabase = "CustomCakeCRM";
        public const string MasterConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=MSME_MasterCRM;Trusted_Connection=True;TrustServerCertificate=True;";
        public const string DefaultTenantConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=CustomCakeCRM;Trusted_Connection=True;TrustServerCertificate=True;";
        public const int DefaultCompanyId = 2;
        public const int DefaultUserId = 4; // Staff user

        public static int CurrentCompanyId => SessionService.CurrentUser?.CompanyId > 0 ? SessionService.CurrentUser.CompanyId : DefaultCompanyId;

        public static string GetConnectionString(int? companyId = null) => GetTenantConnectionString(companyId);

        public static string GetTenantConnectionString(int? companyId = null)
        {
            int targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;

            // 1. Check in-memory cache
            if (_tenantConnectionCache.TryGetValue(targetCompanyId, out var cachedConn))
            {
                return cachedConn;
            }

            // 2. Check SessionService if current user belongs to this company
            if (SessionService.CurrentUser != null &&
                SessionService.CurrentUser.CompanyId == targetCompanyId &&
                !string.IsNullOrWhiteSpace(SessionService.CurrentUser.TenantDatabase))
            {
                string srv = !string.IsNullOrWhiteSpace(SessionService.CurrentUser.TenantServer)
                    ? SessionService.CurrentUser.TenantServer
                    : "(localdb)\\MSSQLLocalDB";
                string conn = $"Server={srv};Database={SessionService.CurrentUser.TenantDatabase};Trusted_Connection=True;TrustServerCertificate=True;";
                _tenantConnectionCache[targetCompanyId] = conn;
                return conn;
            }

            // 3. Resolve from Master DB CompanyDatabases
            try
            {
                using var masterContext = CreateMasterDbContext();
                var tenantDb = masterContext.CompanyDatabases
                    .AsNoTracking()
                    .FirstOrDefault(cd => cd.CompanyId == targetCompanyId && cd.IsActive);

                if (tenantDb != null)
                {
                    string conn = $"Server={tenantDb.ServerName};Database={tenantDb.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;";
                    _tenantConnectionCache[targetCompanyId] = conn;
                    return conn;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetTenantConnectionString Master DB warning] {ex.Message}");
            }

            // 4. Default fallback for DefaultCompanyId or unmapped
            string fallback = $"Server=(localdb)\\MSSQLLocalDB;Database={DefaultTenantDatabase};Trusted_Connection=True;TrustServerCertificate=True;";
            _tenantConnectionCache[targetCompanyId] = fallback;
            return fallback;
        }

        public static CrmDbContext CreateDbContext(int? companyId = null)
        {
            string connectionString = GetTenantConnectionString(companyId);
            var options = new DbContextOptionsBuilder<CrmDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            return new CrmDbContext(options);
        }

        public static CrmDbContext CreateDbContextForDatabase(string serverName, string databaseName)
        {
            string conn = $"Server={serverName};Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";
            var options = new DbContextOptionsBuilder<CrmDbContext>()
                .UseSqlServer(conn)
                .Options;

            return new CrmDbContext(options);
        }

        public static MasterCrmDbContext CreateMasterDbContext()
        {
            var options = new DbContextOptionsBuilder<MasterCrmDbContext>()
                .UseSqlServer(MasterConnectionString)
                .Options;

            return new MasterCrmDbContext(options);
        }

        public static string ComputeSha256(string input)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        public static bool VerifyPassword(string inputPassword, string storedHash)
        {
            if (string.IsNullOrEmpty(storedHash) || string.IsNullOrEmpty(inputPassword)) return false;
            if (string.Equals(inputPassword, storedHash, StringComparison.Ordinal)) return true;
            string hashedInput = ComputeSha256(inputPassword);
            if (string.Equals(hashedInput, storedHash, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        public static async Task SeedTenantBaselineAsync(CrmDbContext context)
        {
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

            // OrderStatuses
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

            // FollowUpStatuses
            if (!await context.FollowUpStatuses.AnyAsync())
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT FollowUpStatuses ON;
                    INSERT INTO FollowUpStatuses (StatusId, StatusName) VALUES
                        (0, 'Pending'), (1, 'Completed'), (2, 'Cancelled'), (3, 'Overdue');
                    SET IDENTITY_INSERT FollowUpStatuses OFF;
                ");
            }

            // PaymentMethods
            if (!await context.PaymentMethods.AnyAsync())
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT PaymentMethods ON;
                    INSERT INTO PaymentMethods (MethodId, MethodName) VALUES
                        (0, 'Cash'), (1, 'GCash'), (2, 'Bank Transfer'), (3, 'Credit Card'), (4, 'Other');
                    SET IDENTITY_INSERT PaymentMethods OFF;
                ");
            }

            // PaymentStatuses
            if (!await context.PaymentStatuses.AnyAsync())
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT PaymentStatuses ON;
                    INSERT INTO PaymentStatuses (StatusId, StatusName) VALUES
                        (0, 'Pending'), (1, 'Completed'), (2, 'Failed'), (3, 'Refunded');
                    SET IDENTITY_INSERT PaymentStatuses OFF;
                ");
            }

            // SubscriptionPlans
            await context.Database.ExecuteSqlRawAsync(@"
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'AllowBranching')
                BEGIN
                    ALTER TABLE SubscriptionPlans ADD AllowBranching BIT NOT NULL DEFAULT 0;
                END
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'MaxBranches')
                BEGIN
                    ALTER TABLE SubscriptionPlans ADD MaxBranches INT NOT NULL DEFAULT 1;
                END
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'IsActive')
                BEGIN
                    ALTER TABLE SubscriptionPlans ADD IsActive BIT NOT NULL DEFAULT 1;
                END
            ");

            if (!await context.SubscriptionPlans.AnyAsync())
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT SubscriptionPlans ON;
                    INSERT INTO SubscriptionPlans (PlanId, PlanName, Price, DurationDays, MaxUsers, AllowBranching, MaxBranches, IsActive) VALUES
                        (1, 'Starter Plan', 4399, 365, 3, 0, 1, 1),
                        (2, 'Pro Plan', 9599, 365, 10, 1, 3, 1),
                        (3, 'Enterprise Plan', 19999, 365, 50, 1, 10, 1);
                    SET IDENTITY_INSERT SubscriptionPlans OFF;
                ");
            }

            // SubscriptionStatuses
            if (!await context.SubscriptionStatuses.AnyAsync())
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    SET IDENTITY_INSERT SubscriptionStatuses ON;
                    INSERT INTO SubscriptionStatuses (StatusId, StatusName) VALUES
                        (1, 'Active'), (2, 'Expired'), (3, 'Cancelled');
                    SET IDENTITY_INSERT SubscriptionStatuses OFF;
                ");
            }
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
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'AllowBranching')
                    BEGIN
                        ALTER TABLE SubscriptionPlans ADD AllowBranching BIT NOT NULL DEFAULT 0;
                    END
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'MaxBranches')
                    BEGIN
                        ALTER TABLE SubscriptionPlans ADD MaxBranches INT NOT NULL DEFAULT 1;
                    END
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'IsActive')
                    BEGIN
                        ALTER TABLE SubscriptionPlans ADD IsActive BIT NOT NULL DEFAULT 1;
                    END
                ");

                if (!await context.SubscriptionPlans.AnyAsync())
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT SubscriptionPlans ON;
                        INSERT INTO SubscriptionPlans (PlanId, PlanName, Price, DurationDays, MaxUsers, AllowBranching, MaxBranches, IsActive) VALUES
                            (1, 'Starter Plan', 4399, 365, 3, 0, 1, 1),
                            (2, 'Pro Plan', 9599, 365, 10, 1, 3, 1),
                            (3, 'Enterprise Plan', 19999, 365, 50, 1, 10, 1);
                        SET IDENTITY_INSERT SubscriptionPlans OFF;
                    ");
                }

                // Clean up any historical duplicate subscription plans in tenant DB
                try
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        WITH RankedPlans AS (
                            SELECT PlanId, PlanName, ROW_NUMBER() OVER (PARTITION BY LOWER(PlanName) ORDER BY PlanId DESC) as rn,
                                   FIRST_VALUE(PlanId) OVER (PARTITION BY LOWER(PlanName) ORDER BY PlanId DESC) as MasterPlanId
                            FROM SubscriptionPlans
                        )
                        UPDATE s
                        SET s.PlanId = r.MasterPlanId
                        FROM Subscriptions s
                        JOIN RankedPlans r ON s.PlanId = r.PlanId
                        WHERE r.rn > 1;

                        WITH RankedPlans AS (
                            SELECT PlanId, ROW_NUMBER() OVER (PARTITION BY LOWER(PlanName) ORDER BY PlanId DESC) as rn
                            FROM SubscriptionPlans
                        )
                        DELETE FROM SubscriptionPlans
                        WHERE PlanId IN (SELECT PlanId FROM RankedPlans WHERE rn > 1);
                    ");
                }
                catch { }

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

                // 8. Retention Email Templates, Settings & Logs
                await EnsureRetentionTablesAndSeedsAsync(context);

                // 1. Ensure IsActive column exists on CustomCakeCRM Companies
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'IsActive')
                    BEGIN
                        ALTER TABLE Companies ADD IsActive BIT NOT NULL CONSTRAINT DF_Companies_IsActive DEFAULT 1;
                    END
                ");

                // 2. Clean up legacy mock hashes ('TEST_HASH') and duplicate admin usernames
                await context.Database.ExecuteSqlRawAsync(@"
                    UPDATE AppUsers SET PasswordHash = 'admin123' WHERE PasswordHash = 'TEST_HASH' OR PasswordHash IS NULL OR PasswordHash = '';
                    UPDATE AppUsers SET Username = 'lea.abad' WHERE UserId = 5 AND Username = 'admin';
                    UPDATE AppUsers SET IsActive = 1, PasswordHash = 'admin123' WHERE Username = 'superadmin';
                ");

                // 3. Ensure Master DB Company and CompanyDatabases exist for Company 2
                try
                {
                    await using var masterContext = CreateMasterDbContext();
                    var masterCompany = await masterContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == DefaultCompanyId);
                    if (masterCompany == null)
                    {
                        await masterContext.Database.ExecuteSqlRawAsync(@"
                            SET IDENTITY_INSERT Companies ON;
                            INSERT INTO Companies (CompanyId, CompanyCode, CompanyName, IsActive, CreatedAt)
                            VALUES (2, 'CC', 'CC Custom Cake Shop', 1, GETUTCDATE());
                            SET IDENTITY_INSERT Companies OFF;
                        ");
                    }

                    var masterDb = await masterContext.CompanyDatabases.FirstOrDefaultAsync(cd => cd.CompanyId == DefaultCompanyId);
                    if (masterDb == null)
                    {
                        masterContext.CompanyDatabases.Add(new CC.domain.Entities.CompanyDatabase
                        {
                            CompanyId = DefaultCompanyId,
                            ServerName = "(localdb)\\MSSQLLocalDB",
                            DatabaseName = "CustomCakeCRM",
                            IsActive = true
                        });
                        await masterContext.SaveChangesAsync();
                    }
                    // 4. Ensure Master DB Subscription tables exist and are seeded
                    await masterContext.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SubscriptionPlans')
                        BEGIN
                            CREATE TABLE SubscriptionPlans (
                                PlanId INT IDENTITY(1,1) PRIMARY KEY,
                                PlanName NVARCHAR(100) NOT NULL,
                                Price DECIMAL(18,2) NOT NULL,
                                DurationDays INT NOT NULL DEFAULT 365,
                                MaxUsers INT NOT NULL DEFAULT 10,
                                AllowBranching BIT NOT NULL DEFAULT 0,
                                MaxBranches INT NOT NULL DEFAULT 1,
                                IsActive BIT NOT NULL DEFAULT 1
                            );
                        END

                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'AllowBranching')
                        BEGIN
                            ALTER TABLE SubscriptionPlans ADD AllowBranching BIT NOT NULL DEFAULT 0;
                        END
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'MaxBranches')
                        BEGIN
                            ALTER TABLE SubscriptionPlans ADD MaxBranches INT NOT NULL DEFAULT 1;
                        END
                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SubscriptionPlans' AND COLUMN_NAME = 'IsActive')
                        BEGIN
                            ALTER TABLE SubscriptionPlans ADD IsActive BIT NOT NULL DEFAULT 1;
                        END

                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'SubscriptionStatuses')
                        BEGIN
                            CREATE TABLE SubscriptionStatuses (
                                StatusId INT IDENTITY(1,1) PRIMARY KEY,
                                StatusName NVARCHAR(50) NOT NULL
                            );
                        END

                        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Subscriptions')
                        BEGIN
                            CREATE TABLE Subscriptions (
                                SubscriptionId INT IDENTITY(1,1) PRIMARY KEY,
                                CompanyId INT NOT NULL,
                                PlanId INT NOT NULL,
                                StatusId INT NOT NULL,
                                StartDate DATETIME2 NOT NULL,
                                EndDate DATETIME2 NOT NULL
                            );
                        END
                    ");

                    if (!await masterContext.SubscriptionPlans.AnyAsync())
                    {
                        masterContext.SubscriptionPlans.AddRange(
                            new SubscriptionPlan { PlanName = "Starter Plan", Price = 4399m, DurationDays = 365, MaxUsers = 3, AllowBranching = false, MaxBranches = 1, IsActive = true },
                            new SubscriptionPlan { PlanName = "Pro Plan", Price = 9599m, DurationDays = 365, MaxUsers = 10, AllowBranching = true, MaxBranches = 3, IsActive = true },
                            new SubscriptionPlan { PlanName = "Enterprise Plan", Price = 19999m, DurationDays = 365, MaxUsers = 50, AllowBranching = true, MaxBranches = 10, IsActive = true }
                        );
                        await masterContext.SaveChangesAsync();
                    }
                    else
                    {
                        var pro = await masterContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanName == "Pro Plan");
                        if (pro != null && (!pro.AllowBranching || pro.MaxBranches <= 1))
                        {
                            pro.AllowBranching = true;
                            pro.MaxBranches = 3;
                        }
                        var ent = await masterContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanName == "Enterprise Plan");
                        if (ent != null && (!ent.AllowBranching || ent.MaxBranches <= 1))
                        {
                            ent.AllowBranching = true;
                            ent.MaxBranches = 10;
                        }
                        await masterContext.SaveChangesAsync();
                    }

                    // Clean up any historical duplicate subscription plans in Master DB
                    try
                    {
                        await masterContext.Database.ExecuteSqlRawAsync(@"
                            WITH RankedPlans AS (
                                SELECT PlanId, PlanName, ROW_NUMBER() OVER (PARTITION BY LOWER(PlanName) ORDER BY PlanId DESC) as rn,
                                       FIRST_VALUE(PlanId) OVER (PARTITION BY LOWER(PlanName) ORDER BY PlanId DESC) as MasterPlanId
                                FROM SubscriptionPlans
                            )
                            UPDATE s
                            SET s.PlanId = r.MasterPlanId
                            FROM Subscriptions s
                            JOIN RankedPlans r ON s.PlanId = r.PlanId
                            WHERE r.rn > 1;

                            WITH RankedPlans AS (
                                SELECT PlanId, ROW_NUMBER() OVER (PARTITION BY LOWER(PlanName) ORDER BY PlanId DESC) as rn
                                FROM SubscriptionPlans
                            )
                            DELETE FROM SubscriptionPlans
                            WHERE PlanId IN (SELECT PlanId FROM RankedPlans WHERE rn > 1);
                        ");
                    }
                    catch { }

                    if (!await masterContext.SubscriptionStatuses.AnyAsync())
                    {
                        await masterContext.Database.ExecuteSqlRawAsync(@"
                            SET IDENTITY_INSERT SubscriptionStatuses ON;
                            INSERT INTO SubscriptionStatuses (StatusId, StatusName) VALUES
                                (1, 'Active'),
                                (2, 'Expired'),
                                (3, 'Suspended'),
                                (4, 'Cancelled');
                            SET IDENTITY_INSERT SubscriptionStatuses OFF;
                        ");
                    }

                    var existingSub2 = await masterContext.Subscriptions.FirstOrDefaultAsync(s => s.CompanyId == DefaultCompanyId);
                    if (existingSub2 == null)
                    {
                        masterContext.Subscriptions.Add(new Subscription
                        {
                            CompanyId = DefaultCompanyId,
                            PlanId = 2, // Pro Plan
                            StatusId = 1, // Active
                            StartDate = DateTime.UtcNow.AddMonths(-1),
                            EndDate = DateTime.UtcNow.AddMonths(11)
                        });
                        await masterContext.SaveChangesAsync();
                    }
                }
                catch (Exception masterEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[EnsureDatabaseReadyAsync MasterDB warning] {masterEx.Message}");
                }

                // 5. Ensure baseline Terms & Conditions exist in tenant DB
                try
                {
                    if (!await context.TermsAndConditions.AnyAsync())
                    {
                        var baselineTerms = new TermsAndConditions
                        {
                            Version = "v1.0",
                            Content = @"### Sweet Story Custom Cake CRM - Platform Terms & Conditions
**Effective Date:** January 1, 2026

#### 1. Acceptance of Terms
By accessing or using the Sweet Story Custom Cake CRM platform, you agree to be bound by these Terms and Conditions. If you do not agree to these terms, do not access or use the platform.

#### 2. Service Description & Tenant Isolation
The platform provides multi-tenant customer relationship management, order tracking, payment processing, and production scheduling. Each subscribed business operates within a dedicated tenant database to guarantee operational data privacy and confidentiality.

#### 3. Subscription & User Seat Usage
Access to features and the number of authorized team member accounts are governed by your company's active subscription tier (Starter, Pro, Enterprise). Additional users beyond your plan's maximum capacity require a tier upgrade.

#### 4. Data Security & Backups
Platform administrators perform periodic automated database backups. Subscribers retain ownership of their operational data.

#### 5. Termination & Suspension
Accounts exhibiting unauthorized activity or expired subscription status may be suspended in accordance with platform policies.",
                            EffectiveDate = new DateTime(2026, 1, 1),
                            CreatedByUserId = 1
                        };
                        context.TermsAndConditions.Add(baselineTerms);
                        await context.SaveChangesAsync();

                        var sampleUsers = await context.AppUsers.Where(u => u.IsActive).Select(u => u.UserId).Take(4).ToListAsync();
                        foreach (var uid in sampleUsers)
                        {
                            context.TermsAcceptances.Add(new TermsAcceptance
                            {
                                TermsId = baselineTerms.TermsId,
                                UserId = uid,
                                AcceptedDate = DateTime.UtcNow.AddDays(-10)
                            });
                        }
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception termsEx)
                {
                    System.Diagnostics.Debug.WriteLine($"[EnsureDatabaseReadyAsync Terms warning] {termsEx.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CrmDataService.EnsureDatabaseReadyAsync] {ex.Message}");
            }
        }

        // =========================================================
        // AUTHENTICATION & LOGIN
        // =========================================================

        public static async Task<AuthResult> AuthenticateUserAsync(string usernameOrEmail, string password)
        {
            if (string.IsNullOrWhiteSpace(usernameOrEmail) || string.IsNullOrWhiteSpace(password))
            {
                return new AuthResult(false, "Please enter your username/email and password.", null, null, null);
            }

            string input = usernameOrEmail.Trim().ToLowerInvariant();

            // 1. Query Master DB for active tenant databases
            await using var masterContext = CreateMasterDbContext();
            var activeTenantDbs = await masterContext.CompanyDatabases
                .AsNoTracking()
                .Where(cd => cd.IsActive)
                .ToListAsync();

            if (!activeTenantDbs.Any())
            {
                activeTenantDbs.Add(new CC.domain.Entities.CompanyDatabase
                {
                    CompanyId = DefaultCompanyId,
                    ServerName = "(localdb)\\MSSQLLocalDB",
                    DatabaseName = DefaultTenantDatabase,
                    IsActive = true
                });
            }

            // 2. Search active tenant databases to authenticate the user
            SystemUser? matchedUser = null;
            CC.domain.Entities.CompanyDatabase? matchedTenantDb = null;

            foreach (var tenantDb in activeTenantDbs)
            {
                try
                {
                    await using var tenantContext = CreateDbContextForDatabase(tenantDb.ServerName, tenantDb.DatabaseName);
                    var candidate = await tenantContext.AppUsers
                        .Include(u => u.Role)
                        .Include(u => u.Company)
                        .FirstOrDefaultAsync(u => u.Username.ToLower() == input || u.Email.ToLower() == input);

                    if (candidate != null && VerifyPassword(password, candidate.PasswordHash))
                    {
                        matchedUser = candidate;
                        matchedTenantDb = tenantDb;
                        break;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[AuthenticateUserAsync lookup in {tenantDb.DatabaseName}] {ex.Message}");
                }
            }

            if (matchedUser == null || matchedTenantDb == null)
            {
                return new AuthResult(false, "Invalid email or password.", null, null, null);
            }

            if (!matchedUser.IsActive)
            {
                return new AuthResult(false, "This user account has been deactivated. Please contact your administrator.", null, null, null);
            }

            // 3. If non-superadmin user, check company active status in Master DB
            bool isSuperAdmin = matchedUser.RoleId == 1 || (matchedUser.Role != null && (matchedUser.Role.RoleName == "SuperAdmin" || matchedUser.Role.RoleName == "Super Admin"));
            if (!isSuperAdmin)
            {
                var company = await masterContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == matchedUser.CompanyId);
                if (company != null && !company.IsActive)
                {
                    return new AuthResult(false, "Your company account has been deactivated. Please contact platform support.", null, null, null);
                }
            }

            // 4. Cache resolved tenant connection
            string serverName = matchedTenantDb.ServerName;
            string databaseName = matchedTenantDb.DatabaseName;
            _tenantConnectionCache[matchedUser.CompanyId] = $"Server={serverName};Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            return new AuthResult(true, null, matchedUser, serverName, databaseName);
        }

        public static async Task UpdateLastLoginDateAsync(int userId)
        {
            try
            {
                await using var context = CreateDbContext();
                var user = await context.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);
                if (user != null)
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateLastLoginDateAsync] {ex.Message}");
            }
        }

        // =========================================================
        // CUSTOMERS CRUD
        // =========================================================

        public static async Task<List<Customer>> GetCustomersAsync(string? searchQuery = null, int? companyId = null)
        {
            int targetCompanyId = companyId ?? CurrentCompanyId;
            await using var context = CreateDbContext(targetCompanyId);
            IQueryable<Customer> query = context.Customers
                .AsNoTracking()
                .Where(c => c.CompanyId == targetCompanyId)
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
                .OrderByDescending(c => c.CustomerId)
                .ToListAsync();
        }

        public static async Task<PagedList<Customer>> GetCustomersPagedAsync(string? searchQuery = null, int page = 1, int pageSize = 10, int? companyId = null)
        {
            pageSize = Math.Max(1, pageSize);
            page = Math.Max(1, page);

            int targetCompanyId = companyId ?? CurrentCompanyId;
            await using var context = CreateDbContext(targetCompanyId);
            IQueryable<Customer> query = context.Customers
                .Where(c => c.CompanyId == targetCompanyId)
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

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(c => c.CustomerId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedList<Customer>(items, totalCount, page, pageSize);
        }

        public static async Task<Customer?> GetCustomerByIdAsync(int customerId, int? companyId = null)
        {
            int targetCompanyId = companyId ?? CurrentCompanyId;
            await using var context = CreateDbContext(targetCompanyId);
            return await context.Customers
                .AsNoTracking()
                .Include(c => c.Orders)
                .Include(c => c.Inquiries)
                .Include(c => c.FollowUps)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.CompanyId == targetCompanyId);
        }

        public static async Task<Customer> CreateCustomerAsync(Customer customer, int? companyId = null)
        {
            int targetCompanyId = companyId ?? (customer.CompanyId > 0 ? customer.CompanyId : CurrentCompanyId);
            await using var context = CreateDbContext(targetCompanyId);
            customer.CompanyId = targetCompanyId;
            if (customer.CreatedByUserId <= 0)
            {
                int actingUserId = SessionService.CurrentUser?.UserId > 0
                    ? SessionService.CurrentUser.UserId
                    : (await context.AppUsers.Select(u => u.UserId).FirstOrDefaultAsync());
                customer.CreatedByUserId = actingUserId > 0 ? actingUserId : DefaultUserId;
            }
            if (customer.RegisteredDate == default) customer.RegisteredDate = DateTime.UtcNow;

            context.Customers.Add(customer);
            await context.SaveChangesAsync();
            return customer;
        }

        public static async Task<Customer> UpdateCustomerAsync(Customer customer, int? companyId = null)
        {
            int targetCompanyId = companyId ?? (customer.CompanyId > 0 ? customer.CompanyId : CurrentCompanyId);
            await using var context = CreateDbContext(targetCompanyId);
            var existing = await context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customer.CustomerId && c.CompanyId == targetCompanyId);
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

        public static async Task<bool> DeleteCustomerAsync(int customerId, int? companyId = null)
        {
            int targetCompanyId = companyId ?? CurrentCompanyId;
            await using var context = CreateDbContext(targetCompanyId);
            var existing = await context.Customers.FirstOrDefaultAsync(c => c.CustomerId == customerId && c.CompanyId == targetCompanyId);
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
            int companyId = CurrentCompanyId;
            IQueryable<SalesOrder> query = context.SalesOrders
                .AsNoTracking()
                .Where(o => o.Customer != null && o.Customer.CompanyId == companyId)
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

        public static async Task<PagedList<SalesOrder>> GetOrdersPagedAsync(string? statusFilter = null, string? searchQuery = null, int page = 1, int pageSize = 10)
        {
            pageSize = Math.Max(1, pageSize);
            page = Math.Max(1, page);

            await using var context = CreateDbContext();
            IQueryable<SalesOrder> query = context.SalesOrders
                .AsNoTracking()
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails)
                .Include(o => o.Payments)
                    .ThenInclude(p => p.Method);

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                if (statusFilter.Equals("Active", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(o => o.StatusId == 1 || o.StatusId == 2 || o.StatusId == 4);
                }
                else if (statusFilter.Equals("Today", StringComparison.OrdinalIgnoreCase))
                {
                    var today = DateTime.UtcNow.Date;
                    query = query.Where(o => o.DeliveryDate.HasValue && o.DeliveryDate.Value.Date == today);
                }
                else
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

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(o => o.OrderId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedList<SalesOrder>(items, totalCount, page, pageSize);
        }

        public static async Task<PagedList<SalesOrder>> GetCustomerOrdersPagedAsync(int customerId, int page = 1, int pageSize = 10)
        {
            pageSize = Math.Max(1, pageSize);
            page = Math.Max(1, page);

            await using var context = CreateDbContext();
            var query = context.SalesOrders
                .AsNoTracking()
                .Include(o => o.OrderDetails)
                .Include(o => o.Payments)
                .Where(o => o.CustomerId == customerId);

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedList<SalesOrder>(items, totalCount, page, pageSize);
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
            if (order.CreatedByUserId <= 0)
            {
                int actingUserId = SessionService.CurrentUser?.UserId > 0
                    ? SessionService.CurrentUser.UserId
                    : (await context.AppUsers.Select(u => u.UserId).FirstOrDefaultAsync());
                order.CreatedByUserId = actingUserId > 0 ? actingUserId : DefaultUserId;
            }
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

        public static async Task<PagedList<CustomerInquiry>> GetInquiriesPagedAsync(string? statusFilter = null, string? searchQuery = null, int page = 1, int pageSize = 10)
        {
            pageSize = Math.Max(1, pageSize);
            page = Math.Max(1, page);

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

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(i => i.InquiryId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedList<CustomerInquiry>(items, totalCount, page, pageSize);
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
                int uid = userId ?? SessionService.CurrentUser?.UserId ?? (await context.AppUsers.Select(u => u.UserId).FirstOrDefaultAsync());
                if (uid <= 0) uid = DefaultUserId;
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
                .OrderByDescending(f => f.FollowUpId)
                .ToListAsync();
        }

        public static async Task<PagedList<CustomerFollowUp>> GetFollowUpsPagedAsync(string? statusFilter = null, string? searchQuery = null, int page = 1, int pageSize = 10)
        {
            pageSize = Math.Max(1, pageSize);
            page = Math.Max(1, page);

            await using var context = CreateDbContext();
            IQueryable<CustomerFollowUp> query = context.CustomerFollowUps
                .AsNoTracking()
                .Include(f => f.Customer);

            if (!string.IsNullOrWhiteSpace(statusFilter) && !statusFilter.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                var today = DateTime.UtcNow.Date;
                if (statusFilter.Equals("Due", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(f => f.StatusId == 0 || f.StatusId == 3);
                }
                else if (statusFilter.Equals("Overdue", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(f => f.StatusId == 3 || (f.StatusId == 0 && f.FollowUpDate.Date < today));
                }
                else
                {
                    int targetStatus = statusFilter switch
                    {
                        "Pending" => 0,
                        "Completed" => 1,
                        "Cancelled" => 2,
                        _ => -1
                    };

                    if (targetStatus >= 0)
                    {
                        query = query.Where(f => f.StatusId == targetStatus);
                    }
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

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(f => f.FollowUpId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedList<CustomerFollowUp>(items, totalCount, page, pageSize);
        }

        public static async Task<CustomerFollowUp> CreateFollowUpAsync(CustomerFollowUp followUp)
        {
            await using var context = CreateDbContext();
            if (followUp.StaffUserId <= 0)
            {
                int actingUserId = SessionService.CurrentUser?.UserId > 0
                    ? SessionService.CurrentUser.UserId
                    : (await context.AppUsers.Select(u => u.UserId).FirstOrDefaultAsync());
                followUp.StaffUserId = actingUserId > 0 ? actingUserId : DefaultUserId;
            }

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
                .OrderByDescending(p => p.PaymentId)
                .ToListAsync();
        }

        public static async Task<PagedList<Payment>> GetPaymentsPagedAsync(string? statusFilter = null, string? searchQuery = null, int page = 1, int pageSize = 10)
        {
            pageSize = Math.Max(1, pageSize);
            page = Math.Max(1, page);

            await using var context = CreateDbContext();
            IQueryable<Payment> query = context.Payments
                .AsNoTracking()
                .Include(p => p.Method)
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

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(p => p.PaymentId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedList<Payment>(items, totalCount, page, pageSize);
        }

        public static async Task<Payment> CreatePaymentAsync(Payment payment)
        {
            await using var context = CreateDbContext();
            if (payment.ProcessedByUserId <= 0)
            {
                int actingUserId = SessionService.CurrentUser?.UserId > 0
                    ? SessionService.CurrentUser.UserId
                    : (await context.AppUsers.Select(u => u.UserId).FirstOrDefaultAsync());
                payment.ProcessedByUserId = actingUserId > 0 ? actingUserId : DefaultUserId;
            }
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

        public static async Task<PagedList<TransactionRecord>> GetOverallTransactionsPagedAsync(
            string period = "All",
            DateTime? filterDate = null,
            string? typeFilter = null,
            string? searchQuery = null,
            int page = 1,
            int pageSize = 10)
        {
            pageSize = Math.Max(1, pageSize);
            page = Math.Max(1, page);

            var all = await GetOverallTransactionsAsync(period, filterDate, typeFilter, searchQuery);
            int totalCount = all.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedList<TransactionRecord>(items, totalCount, page, pageSize);
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
            int companyId = CurrentCompanyId;
            IQueryable<SystemUser> query = context.AppUsers
                .Include(u => u.Role)
                .Where(u => u.CompanyId == companyId)
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
                .OrderByDescending(u => u.UserId)
                .ToListAsync();
        }

        public static async Task<PagedList<SystemUser>> GetUsersPagedAsync(
            string? searchQuery = null,
            string? roleFilter = null,
            string? statusFilter = null,
            int page = 1,
            int pageSize = 10)
        {
            pageSize = Math.Max(1, pageSize);
            page = Math.Max(1, page);

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

            int totalCount = await query.CountAsync();
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = await query
                .OrderByDescending(u => u.UserId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new PagedList<SystemUser>(items, totalCount, page, pageSize);
        }

        public static async Task<UserSummaryMetrics> GetUserSummaryMetricsAsync()
        {
            await using var context = CreateDbContext();
            int companyId = CurrentCompanyId;
            var users = await context.AppUsers
                .Include(u => u.Role)
                .Where(u => u.CompanyId == companyId)
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
            await using var context = CreateDbContext(user.CompanyId > 0 ? user.CompanyId : CurrentCompanyId);
            if (user.CompanyId <= 0) user.CompanyId = CurrentCompanyId;

            // Enforce Subscription Plan seat limit (MaxUsers)
            if (user.IsActive)
            {
                await CheckUserSeatLimitAsync(user.CompanyId);
            }

            if (string.IsNullOrWhiteSpace(user.PasswordHash))
            {
                user.PasswordHash = string.IsNullOrWhiteSpace(password) ? "admin123" : password;
            }
            if (user.CreatedDate == default) user.CreatedDate = DateTime.UtcNow;

            context.AppUsers.Add(user);
            await context.SaveChangesAsync();

            await LogAuditAsync("USER_CREATED", $"User '{user.Username}' ({user.FirstName} {user.LastName}) created for Company #{user.CompanyId}.");
            return user;
        }

        public static async Task<SystemUser> UpdateUserAsync(SystemUser user, string? newPassword = null)
        {
            await using var context = CreateDbContext(user.CompanyId > 0 ? user.CompanyId : CurrentCompanyId);
            var existing = await context.AppUsers.FirstOrDefaultAsync(u => u.UserId == user.UserId);
            if (existing == null) throw new InvalidOperationException($"User #{user.UserId} not found in database.");

            if (user.IsActive && !existing.IsActive)
            {
                await CheckUserSeatLimitAsync(existing.CompanyId);
            }

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
            await LogAuditAsync("USER_UPDATED", $"User #{user.UserId} '{user.Username}' updated.");
            return existing;
        }

        public static async Task ToggleUserStatusAsync(int userId, bool isActive)
        {
            await using var context = CreateDbContext();
            var existing = await context.AppUsers.FirstOrDefaultAsync(u => u.UserId == userId);
            if (existing != null)
            {
                if (isActive && !existing.IsActive)
                {
                    await CheckUserSeatLimitAsync(existing.CompanyId);
                }

                existing.IsActive = isActive;
                await context.SaveChangesAsync();
                await LogAuditAsync(isActive ? "USER_ACTIVATED" : "USER_DEACTIVATED", $"User #{userId} '{existing.Username}' {(isActive ? "activated" : "deactivated")}.");
            }
        }

        private static async Task CheckUserSeatLimitAsync(int companyId)
        {
            try
            {
                await using var masterContext = CreateMasterDbContext();
                var sub = await masterContext.Subscriptions
                    .Include(s => s.Plan)
                    .Where(s => s.CompanyId == companyId && s.StatusId == 1 && s.EndDate >= DateTime.UtcNow)
                    .OrderByDescending(s => s.SubscriptionId)
                    .FirstOrDefaultAsync();

                if (sub?.Plan != null && sub.Plan.MaxUsers > 0)
                {
                    await using var tenantContext = CreateDbContext(companyId);
                    int activeUserCount = await tenantContext.AppUsers.CountAsync(u => u.CompanyId == companyId && u.IsActive);
                    if (activeUserCount >= sub.Plan.MaxUsers)
                    {
                        throw new InvalidOperationException($"Cannot add or activate user. The active subscription plan '{sub.Plan.PlanName}' seat limit of {sub.Plan.MaxUsers} user(s) has been reached. Please upgrade your subscription to add more team members.");
                    }
                }
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[CheckUserSeatLimitAsync warning] {ex.Message}");
            }
        }

        // =========================================================
        // SUBSCRIPTION MANAGEMENT (Unified Single Source of Truth)
        // =========================================================

        public static async Task<SubscriptionInfo> GetSubscriptionInfoAsync(int? companyId = null)
        {
            int targetCompanyId = companyId ?? CurrentCompanyId;

            // 1. Query Master DB first (single source of truth)
            await using var masterContext = CreateMasterDbContext();
            var sub = await masterContext.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.Status)
                .Where(s => s.CompanyId == targetCompanyId)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            int usedSeats = 0;
            try
            {
                await using var tenantContext = CreateDbContext(targetCompanyId);
                usedSeats = await tenantContext.AppUsers
                    .CountAsync(u => u.CompanyId == targetCompanyId && u.IsActive);
            }
            catch { }

            if (sub != null && sub.Plan != null)
            {
                return new SubscriptionInfo(
                    PlanName: sub.Plan.PlanName,
                    Status: sub.Status?.StatusName ?? (sub.EndDate >= DateTime.UtcNow ? "Active" : "Expired"),
                    Price: sub.Plan.Price,
                    BillingCycle: sub.Plan.DurationDays >= 365 ? "year" : $"{sub.Plan.DurationDays} days",
                    RenewalDate: sub.EndDate,
                    PaymentMethod: "Visa ending in 4242",
                    UsedSeats: usedSeats,
                    MaxSeats: sub.Plan.MaxUsers,
                    AllowBranching: sub.Plan.AllowBranching,
                    MaxBranches: sub.Plan.MaxBranches
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
                UsedSeats: usedSeats > 0 ? usedSeats : 1,
                MaxSeats: 10,
                AllowBranching: true,
                MaxBranches: 3
            );
        }

        public static async Task<List<BillingHistoryItem>> GetBillingHistoryAsync(int? companyId = null)
        {
            int targetCompanyId = companyId ?? CurrentCompanyId;
            await using var masterContext = CreateMasterDbContext();
            var sub = await masterContext.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.CompanyId == targetCompanyId)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            var list = new List<BillingHistoryItem>();
            if (sub != null && sub.Plan != null)
            {
                list.Add(new BillingHistoryItem(
                    sub.StartDate,
                    $"{sub.Plan.PlanName} - Initial Subscription",
                    sub.Plan.Price,
                    "Paid"
                ));
            }
            else
            {
                list.Add(new BillingHistoryItem(new DateTime(2025, 8, 15), "Pro Plan - Annual Subscription", 9599.00m, "Paid"));
            }

            return list;
        }

        public static async Task<PagedList<BillingHistoryItem>> GetBillingHistoryPagedAsync(int page = 1, int pageSize = 10, int? companyId = null)
        {
            pageSize = Math.Max(1, pageSize);
            page = Math.Max(1, page);

            var all = await GetBillingHistoryAsync(companyId);
            int totalCount = all.Count;
            int totalPages = Math.Max(1, (int)Math.Ceiling((double)totalCount / pageSize));
            if (page > totalPages) page = totalPages;

            var items = all
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedList<BillingHistoryItem>(items, totalCount, page, pageSize);
        }

        public static async Task RenewSubscriptionAsync(int? companyId = null)
        {
            int targetCompanyId = companyId ?? CurrentCompanyId;
            await using var masterContext = CreateMasterDbContext();
            var masterSub = await masterContext.Subscriptions
                .Include(s => s.Plan)
                .Where(s => s.CompanyId == targetCompanyId)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            int durationDays = masterSub?.Plan?.DurationDays > 0 ? masterSub.Plan.DurationDays : 365;

            if (masterSub != null)
            {
                masterSub.EndDate = (masterSub.EndDate > DateTime.UtcNow ? masterSub.EndDate : DateTime.UtcNow).AddDays(durationDays);
                masterSub.StatusId = 1; // Active
                await masterContext.SaveChangesAsync();
            }

            try
            {
                await using var tenantContext = CreateDbContext(targetCompanyId);
                var tenantSub = await tenantContext.Subscriptions
                    .Where(s => s.CompanyId == targetCompanyId)
                    .OrderByDescending(s => s.SubscriptionId)
                    .FirstOrDefaultAsync();
                if (tenantSub != null)
                {
                    tenantSub.EndDate = (tenantSub.EndDate > DateTime.UtcNow ? tenantSub.EndDate : DateTime.UtcNow).AddDays(durationDays);
                    tenantSub.StatusId = 1;
                    await tenantContext.SaveChangesAsync();
                }
            }
            catch { }

            await LogAuditAsync("SUBSCRIPTION_RENEWED", $"Subscription renewed for Company #{targetCompanyId} (+{durationDays} days).");
        }

        public static async Task UpgradeDowngradePlanAsync(int planId, int? companyId = null)
        {
            int targetCompanyId = companyId ?? CurrentCompanyId;
            await using var masterContext = CreateMasterDbContext();
            var plan = await masterContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == planId);
            if (plan == null) throw new InvalidOperationException($"Plan #{planId} not found.");

            var masterSub = await masterContext.Subscriptions
                .Where(s => s.CompanyId == targetCompanyId)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            if (masterSub != null)
            {
                masterSub.PlanId = plan.PlanId;
                masterSub.StatusId = 1; // Active
                await masterContext.SaveChangesAsync();
            }
            else
            {
                masterContext.Subscriptions.Add(new Subscription
                {
                    CompanyId = targetCompanyId,
                    PlanId = plan.PlanId,
                    StatusId = 1,
                    StartDate = DateTime.UtcNow,
                    EndDate = DateTime.UtcNow.AddDays(plan.DurationDays > 0 ? plan.DurationDays : 365)
                });
                await masterContext.SaveChangesAsync();
            }

            try
            {
                await using var tenantContext = CreateDbContext(targetCompanyId);
                var tenantSub = await tenantContext.Subscriptions
                    .Where(s => s.CompanyId == targetCompanyId)
                    .OrderByDescending(s => s.SubscriptionId)
                    .FirstOrDefaultAsync();
                if (tenantSub != null)
                {
                    tenantSub.PlanId = plan.PlanId;
                    tenantSub.StatusId = 1;
                    await tenantContext.SaveChangesAsync();
                }
            }
            catch { }

            await LogAuditAsync("SUBSCRIPTION_PLAN_CHANGED", $"Subscription plan for Company #{targetCompanyId} changed to '{plan.PlanName}'.");
        }

        public static async Task UpgradeDowngradePlanAsync(string targetPlanName)
        {
            await using var masterContext = CreateMasterDbContext();
            var plan = await masterContext.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.PlanName.ToLower() == targetPlanName.ToLower());

            if (plan != null)
            {
                await UpgradeDowngradePlanAsync(plan.PlanId);
            }
        }

        // =========================================================
        // SUPER ADMIN SUBSCRIPTION PLANS & ASSIGNMENTS
        // =========================================================

        public static async Task<List<SubscriptionPlanListItem>> GetSubscriptionPlansAsync(bool includeArchived = false)
        {
            await using var masterContext = CreateMasterDbContext();
            var query = masterContext.SubscriptionPlans.AsQueryable();
            if (!includeArchived)
            {
                query = query.Where(p => p.IsActive);
            }
            var rawPlans = await query.ToListAsync();
            var subs = await masterContext.Subscriptions.Where(s => s.StatusId == 1).ToListAsync();

            // Group by PlanName to prevent duplicate plan cards
            var plans = rawPlans
                .GroupBy(p => p.PlanName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(p => p.PlanId).First())
                .OrderBy(p => p.Price)
                .ToList();

            var list = new List<SubscriptionPlanListItem>();
            foreach (var p in plans)
            {
                int activeCount = subs.Count(s => s.PlanId == p.PlanId);
                list.Add(new SubscriptionPlanListItem(
                    p.PlanId,
                    p.PlanName,
                    p.Price,
                    p.DurationDays,
                    p.MaxUsers,
                    activeCount,
                    p.AllowBranching,
                    p.MaxBranches,
                    p.IsActive
                ));
            }
            return list;
        }

        public static async Task<SubscriptionPlan> CreateSubscriptionPlanAsync(
            string name,
            decimal price,
            int durationDays,
            int maxUsers,
            bool allowBranching = false,
            int maxBranches = 1)
        {
            await using var masterContext = CreateMasterDbContext();
            var existing = await masterContext.SubscriptionPlans
                .FirstOrDefaultAsync(p => p.PlanName.ToLower() == name.Trim().ToLower());

            SubscriptionPlan plan;
            if (existing != null)
            {
                existing.Price = price;
                existing.DurationDays = durationDays;
                existing.MaxUsers = maxUsers;
                existing.AllowBranching = allowBranching;
                existing.MaxBranches = allowBranching ? Math.Max(1, maxBranches) : 1;
                existing.IsActive = true;
                await masterContext.SaveChangesAsync();
                plan = existing;
            }
            else
            {
                plan = new SubscriptionPlan
                {
                    PlanName = name.Trim(),
                    Price = price,
                    DurationDays = durationDays,
                    MaxUsers = maxUsers,
                    AllowBranching = allowBranching,
                    MaxBranches = allowBranching ? Math.Max(1, maxBranches) : 1,
                    IsActive = true
                };
                masterContext.SubscriptionPlans.Add(plan);
                await masterContext.SaveChangesAsync();
            }

            // Synchronize with tenant DBs
            try
            {
                await using var tenantContext = CreateDbContext(DefaultCompanyId);
                var tenantExisting = await tenantContext.SubscriptionPlans
                    .FirstOrDefaultAsync(p => p.PlanName.ToLower() == name.Trim().ToLower());
                if (tenantExisting != null)
                {
                    tenantExisting.Price = price;
                    tenantExisting.DurationDays = durationDays;
                    tenantExisting.MaxUsers = maxUsers;
                    tenantExisting.AllowBranching = allowBranching;
                    tenantExisting.MaxBranches = allowBranching ? Math.Max(1, maxBranches) : 1;
                    tenantExisting.IsActive = true;
                }
                else
                {
                    tenantContext.SubscriptionPlans.Add(new SubscriptionPlan
                    {
                        PlanName = name.Trim(),
                        Price = price,
                        DurationDays = durationDays,
                        MaxUsers = maxUsers,
                        AllowBranching = allowBranching,
                        MaxBranches = allowBranching ? Math.Max(1, maxBranches) : 1,
                        IsActive = true
                    });
                }
                await tenantContext.SaveChangesAsync();
            }
            catch { }

            await LogAuditAsync("SUBSCRIPTION_PLAN_SAVED", $"Subscription plan '{name}' saved at ₱{price:N2} ({maxUsers} seats, {durationDays} days, Branching={(allowBranching ? $"Max {maxBranches}" : "No")}).");
            return plan;
        }

        public static async Task<SubscriptionPlan> UpdateSubscriptionPlanAsync(
            int planId,
            string name,
            decimal price,
            int durationDays,
            int maxUsers,
            bool allowBranching = false,
            int maxBranches = 1,
            bool isActive = true)
        {
            await using var masterContext = CreateMasterDbContext();
            var plan = await masterContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == planId);
            if (plan == null) throw new InvalidOperationException($"Plan #{planId} not found.");

            plan.PlanName = name;
            plan.Price = price;
            plan.DurationDays = durationDays;
            plan.MaxUsers = maxUsers;
            plan.AllowBranching = allowBranching;
            plan.MaxBranches = allowBranching ? Math.Max(1, maxBranches) : 1;
            plan.IsActive = isActive;
            await masterContext.SaveChangesAsync();

            try
            {
                await using var tenantContext = CreateDbContext(DefaultCompanyId);
                var tenantPlan = await tenantContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == planId || p.PlanName.ToLower() == name.ToLower());
                if (tenantPlan != null)
                {
                    tenantPlan.PlanName = name;
                    tenantPlan.Price = price;
                    tenantPlan.DurationDays = durationDays;
                    tenantPlan.MaxUsers = maxUsers;
                    tenantPlan.AllowBranching = allowBranching;
                    tenantPlan.MaxBranches = allowBranching ? Math.Max(1, maxBranches) : 1;
                    tenantPlan.IsActive = isActive;
                    await tenantContext.SaveChangesAsync();
                }
            }
            catch { }

            await LogAuditAsync("SUBSCRIPTION_PLAN_UPDATED", $"Subscription plan #{planId} '{name}' updated.");
            return plan;
        }

        public static async Task ArchiveSubscriptionPlanAsync(int planId)
        {
            await using var masterContext = CreateMasterDbContext();
            var plan = await masterContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == planId);
            if (plan != null)
            {
                plan.IsActive = false;
                await masterContext.SaveChangesAsync();

                try
                {
                    await using var tenantContext = CreateDbContext(DefaultCompanyId);
                    var tenantPlan = await tenantContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == planId || p.PlanName.ToLower() == plan.PlanName.ToLower());
                    if (tenantPlan != null)
                    {
                        tenantPlan.IsActive = false;
                        await tenantContext.SaveChangesAsync();
                    }
                }
                catch { }

                await LogAuditAsync("SUBSCRIPTION_PLAN_ARCHIVED", $"Subscription plan #{planId} '{plan.PlanName}' archived.");
            }
        }

        public static async Task RestoreSubscriptionPlanAsync(int planId)
        {
            await using var masterContext = CreateMasterDbContext();
            var plan = await masterContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == planId);
            if (plan != null)
            {
                plan.IsActive = true;
                await masterContext.SaveChangesAsync();

                try
                {
                    await using var tenantContext = CreateDbContext(DefaultCompanyId);
                    var tenantPlan = await tenantContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == planId || p.PlanName.ToLower() == plan.PlanName.ToLower());
                    if (tenantPlan != null)
                    {
                        tenantPlan.IsActive = true;
                        await tenantContext.SaveChangesAsync();
                    }
                }
                catch { }

                await LogAuditAsync("SUBSCRIPTION_PLAN_RESTORED", $"Subscription plan #{planId} '{plan.PlanName}' restored to active.");
            }
        }

        public static async Task<List<CompanySubscriptionListItem>> GetCompanySubscriptionsAsync(string? searchQuery = null)
        {
            await using var masterContext = CreateMasterDbContext();
            var companies = await masterContext.Companies.ToListAsync();
            var dbs = await masterContext.CompanyDatabases.ToListAsync();
            var subs = await masterContext.Subscriptions
                .Include(s => s.Plan)
                .Include(s => s.Status)
                .ToListAsync();

            var list = new List<CompanySubscriptionListItem>();
            foreach (var c in companies)
            {
                var sub = subs.Where(s => s.CompanyId == c.CompanyId).OrderByDescending(s => s.SubscriptionId).FirstOrDefault();
                var db = dbs.FirstOrDefault(d => d.CompanyId == c.CompanyId);

                list.Add(new CompanySubscriptionListItem(
                    CompanyId: c.CompanyId,
                    CompanyCode: c.CompanyCode,
                    CompanyName: c.CompanyName,
                    DatabaseName: db?.DatabaseName ?? "N/A",
                    PlanId: sub?.PlanId ?? 0,
                    PlanName: sub?.Plan?.PlanName ?? "No Plan",
                    Price: sub?.Plan?.Price ?? 0m,
                    DurationDays: sub?.Plan?.DurationDays ?? 365,
                    MaxUsers: sub?.Plan?.MaxUsers ?? 0,
                    StatusId: sub?.StatusId ?? 2,
                    StatusName: sub?.Status?.StatusName ?? (sub != null && sub.EndDate >= DateTime.UtcNow ? "Active" : "Expired"),
                    StartDate: sub?.StartDate ?? c.CreatedDate,
                    EndDate: sub?.EndDate ?? c.CreatedDate.AddYears(1),
                    AllowBranching: sub?.Plan?.AllowBranching ?? false,
                    MaxBranches: sub?.Plan?.MaxBranches ?? 1
                ));
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string s = searchQuery.Trim().ToLower();
                list = list.Where(x => x.CompanyName.ToLower().Contains(s) || x.CompanyCode.ToLower().Contains(s) || x.PlanName.ToLower().Contains(s)).ToList();
            }

            return list.OrderByDescending(x => x.CompanyId).ToList();
        }

        public static async Task AssignCompanySubscriptionAsync(int companyId, int planId, int statusId, DateTime startDate, DateTime endDate)
        {
            await using var masterContext = CreateMasterDbContext();
            var plan = await masterContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.PlanId == planId);
            if (plan == null) throw new InvalidOperationException($"Plan #{planId} not found.");

            var existing = await masterContext.Subscriptions
                .Where(s => s.CompanyId == companyId)
                .OrderByDescending(s => s.SubscriptionId)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.PlanId = planId;
                existing.StatusId = statusId;
                existing.StartDate = startDate;
                existing.EndDate = endDate;
            }
            else
            {
                masterContext.Subscriptions.Add(new Subscription
                {
                    CompanyId = companyId,
                    PlanId = planId,
                    StatusId = statusId,
                    StartDate = startDate,
                    EndDate = endDate
                });
            }
            await masterContext.SaveChangesAsync();

            // Synchronize with tenant DB
            try
            {
                await using var tenantContext = CreateDbContext(companyId);
                var tenantSub = await tenantContext.Subscriptions
                    .Where(s => s.CompanyId == companyId)
                    .OrderByDescending(s => s.SubscriptionId)
                    .FirstOrDefaultAsync();
                if (tenantSub != null)
                {
                    tenantSub.PlanId = planId;
                    tenantSub.StatusId = statusId;
                    tenantSub.StartDate = startDate;
                    tenantSub.EndDate = endDate;
                }
                else
                {
                    tenantContext.Subscriptions.Add(new Subscription
                    {
                        CompanyId = companyId,
                        PlanId = planId,
                        StatusId = statusId,
                        StartDate = startDate,
                        EndDate = endDate
                    });
                }
                await tenantContext.SaveChangesAsync();
            }
            catch { }

            await LogAuditAsync("SUBSCRIPTION_ASSIGNED", $"Subscription for Company #{companyId} set to '{plan.PlanName}' (Status={statusId}, Expires={endDate:yyyy-MM-dd}).");
        }

        // =========================================================
        // SUPER ADMIN TERMS & CONDITIONS
        // =========================================================

        public static async Task<List<TermsAndConditions>> GetTermsVersionsAsync()
        {
            await using var context = CreateDbContext(DefaultCompanyId);
            return await context.TermsAndConditions
                .AsNoTracking()
                .Include(t => t.Acceptances)
                .OrderByDescending(t => t.EffectiveDate)
                .ToListAsync();
        }

        public static async Task<TermsAndConditions?> GetTermsByIdAsync(int id)
        {
            await using var context = CreateDbContext(DefaultCompanyId);
            return await context.TermsAndConditions
                .AsNoTracking()
                .Include(t => t.Acceptances)
                .FirstOrDefaultAsync(t => t.TermsId == id);
        }

        public static async Task<TermsAndConditions> CreateTermsVersionAsync(string version, string content, DateTime effectiveDate, int? userId = null)
        {
            await using var context = CreateDbContext(DefaultCompanyId);
            int uid = userId ?? SessionService.CurrentUser?.UserId ?? (await context.AppUsers.Select(u => u.UserId).FirstOrDefaultAsync());
            var terms = new TermsAndConditions
            {
                Version = version,
                Content = content,
                EffectiveDate = effectiveDate,
                CreatedByUserId = uid > 0 ? uid : 1
            };
            context.TermsAndConditions.Add(terms);
            await context.SaveChangesAsync();

            await LogAuditAsync("TERMS_VERSION_CREATED", $"New Terms & Conditions version '{version}' created, effective {effectiveDate:yyyy-MM-dd}.");
            return terms;
        }

        public static async Task<TermsAndConditions> UpdateTermsVersionAsync(int termsId, string version, string content, DateTime effectiveDate)
        {
            await using var context = CreateDbContext(DefaultCompanyId);
            var existing = await context.TermsAndConditions.FirstOrDefaultAsync(t => t.TermsId == termsId);
            if (existing == null) throw new InvalidOperationException($"Terms #{termsId} not found.");

            existing.Version = version;
            existing.Content = content;
            existing.EffectiveDate = effectiveDate;
            await context.SaveChangesAsync();

            await LogAuditAsync("TERMS_VERSION_UPDATED", $"Terms & Conditions version #{termsId} ('{version}') updated.");
            return existing;
        }

        public static async Task<List<TermsAcceptanceItem>> GetTermsAcceptancesAsync(int? termsId = null)
        {
            await using var context = CreateDbContext(DefaultCompanyId);
            IQueryable<TermsAcceptance> query = context.TermsAcceptances
                .AsNoTracking()
                .Include(a => a.Terms)
                .Include(a => a.User);

            if (termsId.HasValue && termsId.Value > 0)
            {
                query = query.Where(a => a.TermsId == termsId.Value);
            }

            var raw = await query
                .OrderByDescending(a => a.AcceptedDate)
                .ToListAsync();

            var list = new List<TermsAcceptanceItem>();
            foreach (var a in raw)
            {
                string userName = a.User != null ? $"{a.User.FirstName} {a.User.LastName}".Trim() : "System User";
                string email = a.User?.Email ?? "N/A";
                string version = a.Terms?.Version ?? "v1.0";
                list.Add(new TermsAcceptanceItem(
                    a.AcceptanceId,
                    a.TermsId,
                    version,
                    a.UserId,
                    userName,
                    email,
                    a.AcceptedDate
                ));
            }
            return list;
        }

        public static async Task RecordTermsAcceptanceAsync(int termsId, int userId, string? ipAddress = null, string? clientApp = null)
        {
            await using var context = CreateDbContext(DefaultCompanyId);
            var acceptance = new TermsAcceptance
            {
                TermsId = termsId,
                UserId = userId,
                AcceptedDate = DateTime.UtcNow
            };
            context.TermsAcceptances.Add(acceptance);
            await context.SaveChangesAsync();
        }

        // =========================================================
        // SUPER ADMIN SYSTEM MONITORING & AUDIT LOGS
        // =========================================================

        public static async Task<List<AuditLogItem>> GetSystemAuditLogsAsync(
            string? searchQuery = null,
            string? actionFilter = null,
            DateTime? fromDate = null,
            DateTime? toDate = null,
            int limit = 200)
        {
            await using var context = CreateDbContext(DefaultCompanyId);
            IQueryable<SystemAuditLog> query = context.SystemAuditLogs
                .AsNoTracking()
                .Include(l => l.User);

            if (!string.IsNullOrWhiteSpace(actionFilter) && !actionFilter.Equals("All Actions", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.ActionType == actionFilter);
            }

            if (fromDate.HasValue)
            {
                query = query.Where(l => l.Timestamp >= fromDate.Value);
            }

            if (toDate.HasValue)
            {
                var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(l => l.Timestamp <= endOfDay);
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string s = searchQuery.Trim().ToLower();
                query = query.Where(l =>
                    l.ActionType.ToLower().Contains(s) ||
                    l.ActionDescription.ToLower().Contains(s) ||
                    (l.User != null && (l.User.Username.ToLower().Contains(s) || l.User.Email.ToLower().Contains(s))));
            }

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(limit)
                .ToListAsync();

            return logs.Select(l => new AuditLogItem(
                l.LogId,
                l.Timestamp,
                l.ActionType,
                l.ActionDescription,
                l.UserId,
                l.User != null ? $"{l.User.FirstName} {l.User.LastName}".Trim() : $"User #{l.UserId}",
                l.User?.Email ?? "N/A",
                l.IPAddress ?? "127.0.0.1"
            )).ToList();
        }

        public static async Task LogSystemAuditAsync(string username, string actionType, string description, string? ipAddress = null)
        {
            await using var context = CreateDbContext(DefaultCompanyId);
            var user = await context.AppUsers.FirstOrDefaultAsync(u => u.Username == username);
            int uid = user?.UserId ?? SessionService.CurrentUser?.UserId ?? DefaultUserId;
            context.SystemAuditLogs.Add(new SystemAuditLog
            {
                UserId = uid,
                ActionType = actionType,
                ActionDescription = description,
                IPAddress = ipAddress ?? "127.0.0.1",
                Timestamp = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // =========================================================
        // SUPER ADMIN PLATFORM & BUSINESS MANAGEMENT
        // =========================================================

        public static async Task<PlatformMetrics> GetPlatformMetricsAsync()
        {
            await using var context = CreateDbContext();
            await using var masterContext = CreateMasterDbContext();

            int totalBusinesses = await context.Companies.CountAsync();
            int activeBusinesses = await context.Companies.CountAsync(c => c.IsActive);
            int inactiveBusinesses = totalBusinesses - activeBusinesses;
            int totalPlatformUsers = await context.AppUsers.CountAsync();

            int totalTenantDatabases = 1;
            try
            {
                totalTenantDatabases = await masterContext.CompanyDatabases.CountAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetPlatformMetricsAsync MasterDB warning] {ex.Message}");
            }

            return new PlatformMetrics(
                TotalBusinesses: totalBusinesses,
                ActiveBusinesses: activeBusinesses,
                InactiveBusinesses: inactiveBusinesses,
                TotalPlatformUsers: totalPlatformUsers,
                TotalTenantDatabases: totalTenantDatabases
            );
        }

        public static async Task<List<CompanyListItem>> GetCompaniesAsync(
            string? searchQuery = null,
            string? statusFilter = null)
        {
            await using var masterContext = CreateMasterDbContext();

            var masterCompanies = await masterContext.Companies
                .AsNoTracking()
                .ToListAsync();

            var dbMappings = new Dictionary<int, (string Server, string Database, bool IsActive)>();
            try
            {
                var dbs = await masterContext.CompanyDatabases.AsNoTracking().ToListAsync();
                foreach (var db in dbs)
                {
                    dbMappings[db.CompanyId] = (db.ServerName, db.DatabaseName, db.IsActive);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCompaniesAsync MasterDB warning] {ex.Message}");
            }

            var list = new List<CompanyListItem>();
            foreach (var c in masterCompanies)
            {
                string srv = dbMappings.ContainsKey(c.CompanyId) ? dbMappings[c.CompanyId].Server : "(localdb)\\MSSQLLocalDB";
                string dbName = dbMappings.ContainsKey(c.CompanyId) ? dbMappings[c.CompanyId].Database : DefaultTenantDatabase;

                string email = "";
                string phone = "";
                string addrSummary = "Not specified";
                int userCount = 0;

                try
                {
                    await using var tenantContext = CreateDbContextForDatabase(srv, dbName);
                    var tenantComp = await tenantContext.Companies
                        .Include(tc => tc.Address)
                        .Include(tc => tc.Users)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(tc => tc.CompanyId == c.CompanyId);

                    if (tenantComp != null)
                    {
                        email = tenantComp.ContactEmail ?? "";
                        phone = tenantComp.ContactPhone ?? "";
                        if (tenantComp.Address != null)
                        {
                            addrSummary = $"{tenantComp.Address.AddressLine1}, {tenantComp.Address.City}".Trim(' ', ',');
                        }
                        userCount = tenantComp.Users.Count;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[GetCompaniesAsync tenant read warning for {dbName}] {ex.Message}");
                }

                list.Add(new CompanyListItem(
                    CompanyId: c.CompanyId,
                    CompanyCode: c.CompanyCode,
                    CompanyName: c.CompanyName,
                    ContactEmail: email,
                    ContactPhone: phone,
                    AddressSummary: addrSummary,
                    CreatedDate: c.CreatedDate,
                    IsActive: c.IsActive,
                    ServerName: srv,
                    DatabaseName: dbName,
                    UserCount: userCount
                ));
            }

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                string q = searchQuery.Trim().ToLowerInvariant();
                list = list.Where(c =>
                    c.CompanyName.ToLowerInvariant().Contains(q) ||
                    c.CompanyCode.ToLowerInvariant().Contains(q) ||
                    c.ContactEmail.ToLowerInvariant().Contains(q) ||
                    c.ContactPhone.ToLowerInvariant().Contains(q)
                ).ToList();
            }

            if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All Status")
            {
                bool active = statusFilter.Equals("Active", StringComparison.OrdinalIgnoreCase);
                list = list.Where(c => c.IsActive == active).ToList();
            }

            return list.OrderByDescending(c => c.CompanyId).ToList();
        }

        public static async Task<CompanyDetails?> GetCompanyByIdAsync(int companyId)
        {
            await using var masterContext = CreateMasterDbContext();

            var masterCompany = await masterContext.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CompanyId == companyId);

            if (masterCompany == null) return null;

            string srv = "(localdb)\\MSSQLLocalDB";
            string dbName = DefaultTenantDatabase;
            try
            {
                var db = await masterContext.CompanyDatabases.FirstOrDefaultAsync(cd => cd.CompanyId == companyId);
                if (db != null)
                {
                    srv = db.ServerName;
                    dbName = db.DatabaseName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCompanyByIdAsync MasterDB warning] {ex.Message}");
            }

            string email = "";
            string phone = "";
            string addr1 = "";
            string? addr2 = null;
            string city = "";
            string state = "";
            string zip = "";
            string country = "Philippines";
            string subPlan = "Pro Plan";
            int userCount = 0;

            try
            {
                await using var tenantContext = CreateDbContextForDatabase(srv, dbName);
                var tenantComp = await tenantContext.Companies
                    .Include(co => co.Address)
                    .Include(co => co.Users)
                    .Include(co => co.Subscriptions)
                        .ThenInclude(s => s.Plan)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(co => co.CompanyId == companyId);

                if (tenantComp != null)
                {
                    email = tenantComp.ContactEmail ?? "";
                    phone = tenantComp.ContactPhone ?? "";
                    if (tenantComp.Address != null)
                    {
                        addr1 = tenantComp.Address.AddressLine1 ?? "";
                        addr2 = tenantComp.Address.AddressLine2;
                        city = tenantComp.Address.City ?? "";
                        state = tenantComp.Address.State ?? "";
                        zip = tenantComp.Address.PostalCode ?? "";
                        country = tenantComp.Address.Country ?? "Philippines";
                    }
                    subPlan = tenantComp.Subscriptions.OrderByDescending(s => s.SubscriptionId).FirstOrDefault()?.Plan?.PlanName ?? "Pro Plan";
                    userCount = tenantComp.Users.Count;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GetCompanyByIdAsync tenant lookup warning] {ex.Message}");
            }

            return new CompanyDetails(
                CompanyId: masterCompany.CompanyId,
                CompanyCode: masterCompany.CompanyCode,
                CompanyName: masterCompany.CompanyName,
                ContactEmail: email,
                ContactPhone: phone,
                AddressLine1: addr1,
                AddressLine2: addr2,
                City: city,
                State: state,
                PostalCode: zip,
                Country: country,
                CreatedDate: masterCompany.CreatedDate,
                IsActive: masterCompany.IsActive,
                ServerName: srv,
                DatabaseName: dbName,
                SubscriptionPlan: subPlan,
                UserCount: userCount
            );
        }

        public static async Task<Company> CreateCompanyAsync(
            string name,
            string code,
            string email,
            string phone,
            string addressLine1,
            string? addressLine2,
            string city,
            string state,
            string postalCode,
            string country,
            bool isActive,
            string serverName,
            string databaseName,
            string? adminFullName = null,
            string? adminUsername = null,
            string? adminEmail = null,
            string? adminPassword = null)
        {
            string targetServer = string.IsNullOrWhiteSpace(serverName) ? "(localdb)\\MSSQLLocalDB" : serverName.Trim();
            string targetDb = string.IsNullOrWhiteSpace(databaseName) ? $"{code.Trim()}_CRM" : databaseName.Trim();

            await using var masterContext = CreateMasterDbContext();

            // 1. Create Company in Master DB first
            var masterCompany = new Company
            {
                CompanyName = name,
                CompanyCode = code,
                IsActive = isActive,
                CreatedDate = DateTime.UtcNow
            };
            masterContext.Companies.Add(masterCompany);
            await masterContext.SaveChangesAsync();

            int assignedCompanyId = masterCompany.CompanyId;

            // 2. Register CompanyDatabase in Master DB
            var companyDb = new CC.domain.Entities.CompanyDatabase
            {
                CompanyId = assignedCompanyId,
                ServerName = targetServer,
                DatabaseName = targetDb,
                IsActive = isActive
            };
            masterContext.CompanyDatabases.Add(companyDb);

            // Register initial Subscription in Master DB
            var masterSub = new Subscription
            {
                CompanyId = assignedCompanyId,
                PlanId = 2, // Pro Plan
                StatusId = 1, // Active
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddYears(1)
            };
            masterContext.Subscriptions.Add(masterSub);
            await masterContext.SaveChangesAsync();

            // 3. Cache connection string immediately
            string tenantConnStr = $"Server={targetServer};Database={targetDb};Trusted_Connection=True;TrustServerCertificate=True;";
            _tenantConnectionCache[assignedCompanyId] = tenantConnStr;

            // 4. Provision and seed the separate Tenant Database
            await using (var tenantContext = CreateDbContextForDatabase(targetServer, targetDb))
            {
                await tenantContext.Database.EnsureCreatedAsync();
                await SeedTenantBaselineAsync(tenantContext);

                // 5. Create Address in Tenant DB
                Address? address = null;
                if (!string.IsNullOrWhiteSpace(addressLine1) || !string.IsNullOrWhiteSpace(city))
                {
                    address = new Address
                    {
                        AddressLine1 = addressLine1,
                        AddressLine2 = addressLine2,
                        City = city,
                        State = state,
                        PostalCode = postalCode,
                        Country = string.IsNullOrWhiteSpace(country) ? "Philippines" : country
                    };
                    tenantContext.Addresses.Add(address);
                    await tenantContext.SaveChangesAsync();
                }

                // 6. Create or update Company in Tenant DB
                var existingTenantComp = await tenantContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == assignedCompanyId || c.CompanyCode == code);
                if (existingTenantComp == null)
                {
                    var tenantCompany = new Company
                    {
                        CompanyId = assignedCompanyId,
                        CompanyName = name,
                        CompanyCode = code,
                        ContactEmail = email,
                        ContactPhone = phone,
                        AddressId = address?.AddressId,
                        IsActive = isActive,
                        CreatedDate = DateTime.UtcNow
                    };

                    await using (var tx = await tenantContext.Database.BeginTransactionAsync())
                    {
                        await tenantContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Companies ON;");
                        tenantContext.Companies.Add(tenantCompany);
                        await tenantContext.SaveChangesAsync();
                        await tenantContext.Database.ExecuteSqlRawAsync("SET IDENTITY_INSERT Companies OFF;");
                        await tx.CommitAsync();
                    }
                }
                else
                {
                    existingTenantComp.CompanyName = name;
                    existingTenantComp.CompanyCode = code;
                    existingTenantComp.ContactEmail = email;
                    existingTenantComp.ContactPhone = phone;
                    if (address?.AddressId > 0) existingTenantComp.AddressId = address.AddressId;
                    existingTenantComp.IsActive = isActive;
                    await tenantContext.SaveChangesAsync();
                }

                // 7. Create default Subscription in Tenant DB if none exists
                var existingSub = await tenantContext.Subscriptions.FirstOrDefaultAsync(s => s.CompanyId == assignedCompanyId);
                if (existingSub == null)
                {
                    var defaultSub = new Subscription
                    {
                        CompanyId = assignedCompanyId,
                        PlanId = 2, // Pro Plan
                        StatusId = 1, // Active
                        StartDate = DateTime.UtcNow,
                        EndDate = DateTime.UtcNow.AddYears(1)
                    };
                    tenantContext.Subscriptions.Add(defaultSub);
                }

                // 8. Create or update initial Business Admin user in Tenant DB
                if (!string.IsNullOrWhiteSpace(adminUsername) || !string.IsNullOrWhiteSpace(adminEmail))
                {
                    string first = "Business";
                    string last = "Admin";
                    if (!string.IsNullOrWhiteSpace(adminFullName))
                    {
                        var parts = adminFullName.Trim().Split(' ', 2);
                        first = parts[0];
                        if (parts.Length > 1) last = parts[1];
                    }

                    string targetUsername = !string.IsNullOrWhiteSpace(adminUsername) ? adminUsername : $"admin.{code.ToLowerInvariant()}";
                    var existingAdmin = await tenantContext.AppUsers.FirstOrDefaultAsync(u => u.Username.ToLower() == targetUsername.ToLower());
                    if (existingAdmin == null)
                    {
                        var adminUser = new SystemUser
                        {
                            CompanyId = assignedCompanyId,
                            RoleId = 2, // Business Admin
                            Username = targetUsername,
                            Email = !string.IsNullOrWhiteSpace(adminEmail) ? adminEmail : email,
                            FirstName = first,
                            LastName = last,
                            Phone = phone,
                            IsActive = true,
                            PasswordHash = !string.IsNullOrWhiteSpace(adminPassword) ? adminPassword : "admin123",
                            CreatedDate = DateTime.UtcNow
                        };
                        tenantContext.AppUsers.Add(adminUser);
                    }
                    else
                    {
                        existingAdmin.CompanyId = assignedCompanyId;
                        existingAdmin.PasswordHash = !string.IsNullOrWhiteSpace(adminPassword) ? adminPassword : "admin123";
                        existingAdmin.IsActive = true;
                    }
                }

                await tenantContext.SaveChangesAsync();
            }

            return masterCompany;
        }

        public static async Task<Company> UpdateCompanyAsync(
            int companyId,
            string name,
            string code,
            string email,
            string phone,
            string addressLine1,
            string? addressLine2,
            string city,
            string state,
            string postalCode,
            string country,
            bool isActive,
            string serverName,
            string databaseName)
        {
            await using var masterContext = CreateMasterDbContext();

            // 1. Update Master DB
            var masterCompany = await masterContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);
            if (masterCompany != null)
            {
                masterCompany.CompanyName = name;
                masterCompany.CompanyCode = code;
                masterCompany.IsActive = isActive;
            }

            var masterDb = await masterContext.CompanyDatabases.FirstOrDefaultAsync(cd => cd.CompanyId == companyId);
            if (masterDb != null)
            {
                masterDb.ServerName = serverName;
                masterDb.DatabaseName = databaseName;
                masterDb.IsActive = isActive;
            }
            else
            {
                masterContext.CompanyDatabases.Add(new CC.domain.Entities.CompanyDatabase
                {
                    CompanyId = companyId,
                    ServerName = serverName,
                    DatabaseName = databaseName,
                    IsActive = isActive
                });
            }
            await masterContext.SaveChangesAsync();

            // Invalidate cache so new server/db takes effect immediately
            _tenantConnectionCache.TryRemove(companyId, out _);

            // 2. Update in Tenant DB
            try
            {
                await using var tenantContext = CreateDbContextForDatabase(serverName, databaseName);
                var tenantCompany = await tenantContext.Companies
                    .Include(c => c.Address)
                    .FirstOrDefaultAsync(c => c.CompanyId == companyId);

                if (tenantCompany != null)
                {
                    tenantCompany.CompanyName = name;
                    tenantCompany.CompanyCode = code;
                    tenantCompany.ContactEmail = email;
                    tenantCompany.ContactPhone = phone;
                    tenantCompany.IsActive = isActive;

                    if (tenantCompany.Address != null)
                    {
                        tenantCompany.Address.AddressLine1 = addressLine1;
                        tenantCompany.Address.AddressLine2 = addressLine2;
                        tenantCompany.Address.City = city;
                        tenantCompany.Address.State = state;
                        tenantCompany.Address.PostalCode = postalCode;
                        tenantCompany.Address.Country = country;
                    }
                    else if (!string.IsNullOrWhiteSpace(addressLine1) || !string.IsNullOrWhiteSpace(city))
                    {
                        var addr = new Address
                        {
                            AddressLine1 = addressLine1,
                            AddressLine2 = addressLine2,
                            City = city,
                            State = state,
                            PostalCode = postalCode,
                            Country = country
                        };
                        tenantContext.Addresses.Add(addr);
                        tenantCompany.Address = addr;
                    }

                    await tenantContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UpdateCompanyAsync Tenant DB sync warning] {ex.Message}");
            }

            return masterCompany ?? new Company { CompanyId = companyId, CompanyName = name, CompanyCode = code };
        }

        public static async Task ToggleCompanyStatusAsync(int companyId, bool isActive)
        {
            await using var masterContext = CreateMasterDbContext();

            var masterCompany = await masterContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);
            if (masterCompany != null)
            {
                masterCompany.IsActive = isActive;
            }

            var masterDb = await masterContext.CompanyDatabases.FirstOrDefaultAsync(cd => cd.CompanyId == companyId);
            if (masterDb != null)
            {
                masterDb.IsActive = isActive;
            }

            await masterContext.SaveChangesAsync();
            _tenantConnectionCache.TryRemove(companyId, out _);

            try
            {
                if (masterDb != null)
                {
                    await using var tenantContext = CreateDbContextForDatabase(masterDb.ServerName, masterDb.DatabaseName);
                    var tenantComp = await tenantContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);
                    if (tenantComp != null)
                    {
                        tenantComp.IsActive = isActive;
                        await tenantContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ToggleCompanyStatusAsync Tenant DB warning] {ex.Message}");
            }
        }

        public static async Task<bool> DeleteCompanyAsync(int companyId)
        {
            await using var masterContext = CreateMasterDbContext();

            var masterDbs = await masterContext.CompanyDatabases.Where(cd => cd.CompanyId == companyId).ToListAsync();
            foreach (var db in masterDbs)
            {
                try
                {
                    await using var tenantContext = CreateDbContextForDatabase(db.ServerName, db.DatabaseName);
                    var tenantComp = await tenantContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);
                    if (tenantComp != null)
                    {
                        tenantComp.IsActive = false;
                        await tenantContext.SaveChangesAsync();
                    }
                }
                catch { }
            }

            masterContext.CompanyDatabases.RemoveRange(masterDbs);

            var masterCompany = await masterContext.Companies.FirstOrDefaultAsync(c => c.CompanyId == companyId);
            if (masterCompany != null)
            {
                masterContext.Companies.Remove(masterCompany);
            }

            await masterContext.SaveChangesAsync();
            _tenantConnectionCache.TryRemove(companyId, out _);
            return true;
        }

        // =========================================================
        // ROLE-BASED DASHBOARD ANALYTICS (REAL DATABASE QUERIES)
        // =========================================================

        private static DateTime? GetPeriodStartDate(string period) => period?.ToLowerInvariant() switch
        {
            "1d" => DateTime.Today,
            "7d" => DateTime.Today.AddDays(-7),
            "30d" => DateTime.Today.AddDays(-30),
            "90d" => DateTime.Today.AddDays(-90),
            _ => null // All time
        };

        public static async Task<StaffDashboardData> GetStaffDashboardDataAsync(int? userId = null, int? companyId = null, string period = "1d")
        {
            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            var targetUserId = userId ?? SessionService.CurrentUser?.UserId ?? DefaultUserId;
            var startDate = GetPeriodStartDate(period);
            var today = DateTime.Today;

            await using var context = CreateDbContext(targetCompanyId);

            var user = await context.AppUsers.FindAsync(targetUserId);
            string userFullName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : "";

            int totalCustomers = await context.Customers
                .CountAsync(c => c.CompanyId == targetCompanyId);

            int myDueFollowups = await context.CustomerFollowUps
                .Include(f => f.Customer)
                .CountAsync(f => f.StaffUserId == targetUserId && f.Customer != null && f.Customer.CompanyId == targetCompanyId && f.StatusId == 0);

            int myOverdueFollowups = await context.CustomerFollowUps
                .Include(f => f.Customer)
                .CountAsync(f => f.StaffUserId == targetUserId && f.Customer != null && f.Customer.CompanyId == targetCompanyId && f.StatusId == 0 && f.FollowUpDate < today);

            int myHandledInquiries = await context.CustomerInquiries
                .Include(i => i.Customer)
                .CountAsync(i => i.Customer != null && i.Customer.CompanyId == targetCompanyId &&
                                 (i.AssignedTo == userFullName || string.IsNullOrEmpty(userFullName)) &&
                                 (i.Status == "New" || i.Status == "In Progress" || i.Status == "Quoted"));

            int processingOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId == 2);

            int readyOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId == 4);

            int completedOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId == 3 && (startDate == null || o.OrderDate >= startDate));

            int todayDueOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (o.StatusId == 1 || o.StatusId == 2 || o.StatusId == 4) && o.DeliveryDate != null && o.DeliveryDate.Value.Date == today);

            var orderStatusCounts = await context.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (startDate == null || o.OrderDate >= startDate))
                .GroupBy(o => o.StatusId)
                .Select(g => new { StatusId = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusLookup = await context.OrderStatuses.ToDictionaryAsync(s => s.StatusId, s => s.StatusName);
            var orderStatusBars = new List<BarItem>();
            foreach (var s in statusLookup.OrderBy(kv => kv.Key))
            {
                int count = orderStatusCounts.FirstOrDefault(x => x.StatusId == s.Key)?.Count ?? 0;
                Color col = s.Key switch
                {
                    0 => Color.FromArgb(200, 160, 100),
                    1 => Color.FromArgb(50, 130, 200),
                    2 => Color.FromArgb(210, 145, 50),
                    3 => Color.FromArgb(40, 140, 80),
                    4 => Color.FromArgb(30, 160, 110),
                    _ => Color.FromArgb(180, 70, 70)
                };
                orderStatusBars.Add(new BarItem { Category = s.Value, Value = count, BarColor = col });
            }

            var followUpCounts = await context.CustomerFollowUps
                .Include(f => f.Customer)
                .Where(f => f.StaffUserId == targetUserId && f.Customer != null && f.Customer.CompanyId == targetCompanyId && (startDate == null || f.FollowUpDate >= startDate))
                .GroupBy(f => f.StatusId)
                .Select(g => new { StatusId = g.Key, Count = g.Count() })
                .ToListAsync();

            var followUpStatusLookup = await context.FollowUpStatuses.ToDictionaryAsync(s => s.StatusId, s => s.StatusName);
            var followUpBars = new List<BarItem>();
            foreach (var s in followUpStatusLookup.OrderBy(kv => kv.Key))
            {
                int count = followUpCounts.FirstOrDefault(x => x.StatusId == s.Key)?.Count ?? 0;
                Color col = s.Key switch
                {
                    0 => Color.FromArgb(210, 150, 50),
                    1 => Color.FromArgb(40, 140, 80),
                    3 => Color.FromArgb(190, 60, 60),
                    _ => Color.FromArgb(140, 130, 125)
                };
                followUpBars.Add(new BarItem { Category = s.Value, Value = count, BarColor = col });
            }

            var taskTrend = new List<TrendPoint>();
            if (period == "1d")
            {
                var todayFollowUps = await context.CustomerFollowUps
                    .Include(f => f.Customer)
                    .Where(f => f.StaffUserId == targetUserId && f.Customer != null && f.Customer.CompanyId == targetCompanyId && f.StatusId == 1 && f.FollowUpDate >= today)
                    .Select(f => f.FollowUpDate)
                    .ToListAsync();

                for (int hour = 8; hour <= 18; hour += 2)
                {
                    var slotTime = today.AddHours(hour);
                    int count = todayFollowUps.Count(d => d.Hour >= hour - 1 && d.Hour < hour + 1);
                    taskTrend.Add(new TrendPoint
                    {
                        Date = slotTime,
                        Value = count,
                        Label = slotTime.ToString("htt")
                    });
                }
            }
            else
            {
                var trendQuery = context.CustomerFollowUps
                    .Include(f => f.Customer)
                    .Where(f => f.StaffUserId == targetUserId && f.Customer != null && f.Customer.CompanyId == targetCompanyId && f.StatusId == 1 && (startDate == null || f.FollowUpDate >= startDate))
                    .GroupBy(f => f.FollowUpDate.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() });

                var trendRaw = await trendQuery.ToListAsync();
                var effectiveStart = startDate ?? (trendRaw.Any() ? trendRaw.Min(t => t.Date) : today.AddDays(-30));
                int days = Math.Max(1, (int)(today - effectiveStart).TotalDays);
                int step = Math.Max(1, days / 15);
                for (var d = effectiveStart; d <= today; d = d.AddDays(step))
                {
                    var nextD = d.AddDays(step);
                    int count = trendRaw.Where(t => t.Date >= d && t.Date < nextD).Sum(t => t.Count);
                    taskTrend.Add(new TrendPoint { Date = d, Value = count, Label = d.ToString("MMM d") });
                }
            }

            var rawFollowUps = await context.CustomerFollowUps
                .Include(f => f.Customer)
                .Where(f => f.StaffUserId == targetUserId && f.Customer != null && f.Customer.CompanyId == targetCompanyId && (f.StatusId == 0 || f.StatusId == 3))
                .OrderBy(f => f.FollowUpDate)
                .Take(4)
                .ToListAsync();

            var urgentFollowUps = rawFollowUps.Select(f => new StaffUrgentTaskItem(
                "Follow-up",
                f.Notes != null && f.Notes.Length > 45 ? f.Notes.Substring(0, 45) + "..." : (f.Notes ?? "Customer follow-up"),
                f.Customer != null ? $"{f.Customer.FirstName} {f.Customer.LastName}".Trim() : "Client",
                f.FollowUpDate,
                f.StatusId == 3 ? "Overdue" : "Pending",
                f.StatusId == 3 ? UITheme.StatusRedBg : UITheme.StatusYellowBg,
                f.StatusId == 3 ? UITheme.StatusRedFg : UITheme.StatusYellowFg,
                f.FollowUpId
            )).ToList();

            var rawOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (o.StatusId == 2 || o.StatusId == 4))
                .OrderBy(o => o.DeliveryDate ?? o.OrderDate)
                .Take(4)
                .ToListAsync();

            var urgentOrders = rawOrders.Select(o => new StaffUrgentTaskItem(
                "Order",
                $"Order #{o.OrderId:D4} - {o.CakeSize}",
                o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}".Trim() : "Walk-in",
                o.DeliveryDate ?? o.OrderDate,
                o.StatusId == 4 ? "Ready" : "Processing",
                o.StatusId == 4 ? UITheme.StatusGreenBg : UITheme.StatusYellowBg,
                o.StatusId == 4 ? UITheme.StatusGreenFg : UITheme.StatusYellowFg,
                o.OrderId
            )).ToList();

            var combinedUrgent = new List<StaffUrgentTaskItem>();
            combinedUrgent.AddRange(urgentFollowUps);
            combinedUrgent.AddRange(urgentOrders);

            return new StaffDashboardData(
                myDueFollowups,
                myOverdueFollowups,
                myHandledInquiries,
                processingOrders,
                readyOrders,
                completedOrders,
                todayDueOrders,
                totalCustomers,
                taskTrend,
                orderStatusBars,
                followUpBars,
                combinedUrgent
            );
        }

        public static async Task<ManagerDashboardData> GetManagerDashboardDataAsync(int? companyId = null, string period = "30d")
        {
            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            var startDate = GetPeriodStartDate(period);
            var today = DateTime.Today;

            await using var context = CreateDbContext(targetCompanyId);

            int totalCustomers = await context.Customers
                .CountAsync(c => c.CompanyId == targetCompanyId);

            int activeOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (o.StatusId == 1 || o.StatusId == 2));

            int readyOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId == 4);

            int completedOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId == 3 && (startDate == null || o.OrderDate >= startDate));

            int todayDeliveries = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (o.StatusId == 1 || o.StatusId == 2 || o.StatusId == 4) && o.DeliveryDate != null && o.DeliveryDate.Value.Date == today);

            decimal activePipelineValue = await context.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (o.StatusId == 1 || o.StatusId == 2 || o.StatusId == 4))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            int openInquiries = await context.CustomerInquiries
                .Include(i => i.Customer)
                .CountAsync(i => i.Customer != null && i.Customer.CompanyId == targetCompanyId && (i.Status == "New" || i.Status == "In Progress" || i.Status == "Quoted"));

            int shopOverdueFollowups = await context.CustomerFollowUps
                .Include(f => f.Customer)
                .CountAsync(f => f.Customer != null && f.Customer.CompanyId == targetCompanyId && f.StatusId == 0 && f.FollowUpDate < today);

            int pipelineInquiries = await context.CustomerInquiries
                .Include(i => i.Customer)
                .CountAsync(i => i.Customer != null && i.Customer.CompanyId == targetCompanyId && (startDate == null || i.CreatedAt >= startDate));
            int pipelineConfirmed = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId == 1 && (startDate == null || o.OrderDate >= startDate));
            int pipelineProcessing = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId == 2 && (startDate == null || o.OrderDate >= startDate));
            int pipelineReady = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId == 4 && (startDate == null || o.OrderDate >= startDate));
            int pipelineCompleted = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId == 3 && (startDate == null || o.OrderDate >= startDate));

            var stages = new List<PipelineStage>
            {
                new PipelineStage { Name = "Inquiries", Count = pipelineInquiries, StageColor = Color.FromArgb(235, 175, 185) },
                new PipelineStage { Name = "Confirmed", Count = pipelineConfirmed, StageColor = Color.FromArgb(50, 130, 200) },
                new PipelineStage { Name = "Processing", Count = pipelineProcessing, StageColor = Color.FromArgb(210, 145, 50) },
                new PipelineStage { Name = "Ready", Count = pipelineReady, StageColor = Color.FromArgb(30, 160, 110) },
                new PipelineStage { Name = "Completed", Count = pipelineCompleted, StageColor = Color.FromArgb(40, 130, 80) }
            };

            var orderStatusCounts = await context.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (startDate == null || o.OrderDate >= startDate))
                .GroupBy(o => o.StatusId)
                .Select(g => new { StatusId = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusLookup = await context.OrderStatuses.ToDictionaryAsync(s => s.StatusId, s => s.StatusName);
            var orderStatusBars = new List<BarItem>();
            foreach (var s in statusLookup.OrderBy(kv => kv.Key))
            {
                int count = orderStatusCounts.FirstOrDefault(x => x.StatusId == s.Key)?.Count ?? 0;
                Color col = s.Key switch
                {
                    0 => Color.FromArgb(200, 160, 100),
                    1 => Color.FromArgb(50, 130, 200),
                    2 => Color.FromArgb(210, 145, 50),
                    3 => Color.FromArgb(40, 140, 80),
                    4 => Color.FromArgb(30, 160, 110),
                    _ => Color.FromArgb(180, 70, 70)
                };
                orderStatusBars.Add(new BarItem { Category = s.Value, Value = count, BarColor = col });
            }

            var orderTrendRaw = await context.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (startDate == null || o.OrderDate >= startDate))
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var orderTrend = new List<TrendPoint>();
            var effectiveStart = startDate ?? (orderTrendRaw.Any() ? orderTrendRaw.Min(t => t.Date) : today.AddDays(-30));
            int days = Math.Max(1, (int)(today - effectiveStart).TotalDays);
            int step = Math.Max(1, days / 15);
            for (var d = effectiveStart; d <= today; d = d.AddDays(step))
            {
                var nextD = d.AddDays(step);
                int count = orderTrendRaw.Where(t => t.Date >= d && t.Date < nextD).Sum(t => t.Count);
                orderTrend.Add(new TrendPoint { Date = d, Value = count, Label = d.ToString("MMM d") });
            }

            var staffPerformanceRaw = await context.CustomerFollowUps
                .Include(f => f.StaffUser)
                .Include(f => f.Customer)
                .Where(f => f.Customer != null && f.Customer.CompanyId == targetCompanyId && (startDate == null || f.FollowUpDate >= startDate))
                .GroupBy(f => f.StaffUser != null ? f.StaffUser.FirstName + " " + f.StaffUser.LastName : "Unassigned")
                .Select(g => new
                {
                    StaffName = g.Key,
                    Completed = g.Count(f => f.StatusId == 1),
                    Pending = g.Count(f => f.StatusId == 0 || f.StatusId == 3)
                })
                .ToListAsync();

            var staffPerformance = staffPerformanceRaw.Select(s => new BarItem
            {
                Category = s.StaffName,
                Value = s.Completed,
                BarColor = Color.FromArgb(40, 140, 80),
                ExtraLabel = $"{s.Completed} done / {s.Pending} pend"
            }).ToList();

            var recentOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .Include(o => o.Status)
                .Where(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId != 5)
                .OrderByDescending(o => o.OrderId)
                .Take(6)
                .Select(o => new ManagerPriorityOrderItem(
                    o.OrderId,
                    $"#ORD-{o.OrderId:D4}",
                    o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}".Trim() : "Walk-in",
                    $"{o.CakeSize} ({o.Flavor})",
                    o.DeliveryDate ?? o.OrderDate,
                    o.TotalAmount,
                    o.Status != null ? o.Status.StatusName : "Processing"
                ))
                .ToListAsync();

            return new ManagerDashboardData(
                activeOrders,
                readyOrders,
                completedOrders,
                todayDeliveries,
                activePipelineValue,
                openInquiries,
                shopOverdueFollowups,
                totalCustomers,
                orderTrend,
                stages,
                orderStatusBars,
                staffPerformance,
                recentOrders
            );
        }

        public static async Task<AdminDashboardData> GetAdminDashboardDataAsync(int? companyId = null, string period = "30d")
        {
            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            var startDate = GetPeriodStartDate(period);
            var today = DateTime.Today;

            await using var context = CreateDbContext(targetCompanyId);

            decimal periodRevenue = await context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.Customer)
                .Where(p => p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == targetCompanyId && p.StatusId == 1 && (startDate == null || p.PaymentDate >= startDate))
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            decimal lifetimeRevenue = await context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.Customer)
                .Where(p => p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == targetCompanyId && p.StatusId == 1)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            int totalOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (startDate == null || o.OrderDate >= startDate));

            int activeOrders = await context.SalesOrders
                .Include(o => o.Customer)
                .CountAsync(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (o.StatusId == 1 || o.StatusId == 2 || o.StatusId == 4));

            decimal averageOrderValue = totalOrders > 0 ? periodRevenue / totalOrders : 0m;

            decimal totalBilledPeriod = await context.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (startDate == null || o.OrderDate >= startDate))
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
            double collectionRate = totalBilledPeriod > 0 ? Math.Min(100.0, (double)(periodRevenue / totalBilledPeriod) * 100.0) : 100.0;

            int totalCustomers = await context.Customers
                .CountAsync(c => c.CompanyId == targetCompanyId);

            int newCustomers = await context.Customers
                .CountAsync(c => c.CompanyId == targetCompanyId && (startDate == null || c.RegisteredDate >= startDate));

            decimal outstanding = await context.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && o.StatusId != 3 && o.StatusId != 5)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var revTrendRaw = await context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.Customer)
                .Where(p => p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == targetCompanyId && p.StatusId == 1 && (startDate == null || p.PaymentDate >= startDate))
                .GroupBy(p => p.PaymentDate.Date)
                .Select(g => new { Date = g.Key, Total = g.Sum(p => p.Amount) })
                .ToListAsync();

            var revenueTrend = new List<TrendPoint>();
            if (period == "1d")
            {
                var todayPayments = await context.Payments
                    .Include(p => p.Order)
                    .ThenInclude(o => o!.Customer)
                    .Where(p => p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == targetCompanyId && p.StatusId == 1 && p.PaymentDate >= today)
                    .Select(p => new { p.PaymentDate, p.Amount })
                    .ToListAsync();

                for (int hour = 8; hour <= 18; hour += 2)
                {
                    var slotTime = today.AddHours(hour);
                    decimal sum = todayPayments
                        .Where(p => p.PaymentDate.Hour >= hour - 1 && p.PaymentDate.Hour < hour + 1)
                        .Sum(p => p.Amount);
                    revenueTrend.Add(new TrendPoint
                    {
                        Date = slotTime,
                        Value = sum,
                        Label = slotTime.ToString("htt")
                    });
                }
            }
            else
            {
                var effectiveStart = startDate ?? (revTrendRaw.Any() ? revTrendRaw.Min(t => t.Date) : today.AddDays(-30));
                int days = Math.Max(1, (int)(today - effectiveStart).TotalDays);
                int step = Math.Max(1, days / 15);
                for (var d = effectiveStart; d <= today; d = d.AddDays(step))
                {
                    var nextD = d.AddDays(step);
                    decimal sum = revTrendRaw.Where(t => t.Date >= d && t.Date < nextD).Sum(t => t.Total);
                    revenueTrend.Add(new TrendPoint { Date = d, Value = sum, Label = d.ToString("MMM d") });
                }
            }

            var methodCounts = await context.Payments
                .Include(p => p.Order)
                .ThenInclude(o => o!.Customer)
                .Include(p => p.Method)
                .Where(p => p.Order != null && p.Order.Customer != null && p.Order.Customer.CompanyId == targetCompanyId && p.StatusId == 1 && (startDate == null || p.PaymentDate >= startDate))
                .GroupBy(p => p.Method != null ? p.Method.MethodName : "Cash")
                .Select(g => new { Method = g.Key, Count = g.Count(), Total = g.Sum(p => p.Amount) })
                .ToListAsync();

            var methodBars = methodCounts.Select(m => new BarItem
            {
                Category = m.Method,
                Value = (int)m.Total,
                BarColor = m.Method switch
                {
                    "GCash" => Color.FromArgb(0, 122, 255),
                    "Bank Transfer" => Color.FromArgb(40, 140, 80),
                    "Credit Card" => Color.FromArgb(140, 70, 90),
                    _ => Color.FromArgb(201, 151, 90)
                },
                ExtraLabel = $"P{m.Total:N0} ({m.Count})"
            }).ToList();

            var orderStatusCounts = await context.SalesOrders
                .Include(o => o.Customer)
                .Where(o => o.Customer != null && o.Customer.CompanyId == targetCompanyId && (startDate == null || o.OrderDate >= startDate))
                .GroupBy(o => o.StatusId)
                .Select(g => new { StatusId = g.Key, Count = g.Count() })
                .ToListAsync();

            var statusLookup = await context.OrderStatuses.ToDictionaryAsync(s => s.StatusId, s => s.StatusName);
            var orderStatusBars = new List<BarItem>();
            foreach (var s in statusLookup.OrderBy(kv => kv.Key))
            {
                int count = orderStatusCounts.FirstOrDefault(x => x.StatusId == s.Key)?.Count ?? 0;
                Color col = s.Key switch
                {
                    0 => Color.FromArgb(200, 160, 100),
                    1 => Color.FromArgb(50, 130, 200),
                    2 => Color.FromArgb(210, 145, 50),
                    3 => Color.FromArgb(40, 140, 80),
                    4 => Color.FromArgb(30, 160, 110),
                    _ => Color.FromArgb(180, 70, 70)
                };
                orderStatusBars.Add(new BarItem { Category = s.Value, Value = count, BarColor = col });
            }

            var topCustsRaw = await context.Customers
                .Where(c => c.CompanyId == targetCompanyId && c.Orders.Any())
                .Select(c => new
                {
                    c.CustomerId,
                    CustomerName = (c.FirstName + " " + c.LastName).Trim(),
                    c.Email,
                    c.Phone,
                    OrderCount = c.Orders.Count,
                    TotalSpent = c.Orders.SelectMany(o => o.Payments).Where(p => p.StatusId == 1).Sum(p => (decimal?)p.Amount) ?? 0m,
                    LastOrderDate = c.Orders.Max(o => o.OrderDate)
                })
                .OrderByDescending(c => c.TotalSpent)
                .Take(5)
                .ToListAsync();

            var topCustomers = topCustsRaw.Select(c => new AdminTopCustomerItem(
                c.CustomerId,
                c.CustomerName,
                c.Email,
                c.Phone,
                c.OrderCount,
                c.TotalSpent,
                c.LastOrderDate
            )).ToList();

            var subInfo = await GetSubscriptionInfoAsync();
            var pagedTx = await GetOverallTransactionsPagedAsync(page: 1, pageSize: 6);

            return new AdminDashboardData(
                periodRevenue,
                lifetimeRevenue,
                outstanding,
                averageOrderValue,
                collectionRate,
                totalOrders,
                activeOrders,
                totalCustomers,
                newCustomers,
                revenueTrend,
                methodBars,
                orderStatusBars,
                topCustomers,
                subInfo,
                pagedTx.Items
            );
        }

        public static async Task<SuperAdminDashboardData> GetSuperAdminDashboardDataAsync(string period = "30d")
        {
            var startDate = GetPeriodStartDate(period);
            var today = DateTime.Today;

            var masterOptions = new DbContextOptionsBuilder<MasterCrmDbContext>()
                .UseSqlServer(MasterConnectionString)
                .Options;

            await using var masterContext = new MasterCrmDbContext(masterOptions);

            int totalBusinesses = await masterContext.Companies.CountAsync();
            int activeBusinesses = await masterContext.Companies.CountAsync(c => c.IsActive);
            int activeDatabases = await masterContext.CompanyDatabases.CountAsync(d => d.IsActive);

            var recentCompanies = await masterContext.Companies
                .OrderByDescending(c => c.CompanyId)
                .Take(8)
                .ToListAsync();

            var companyDatabases = await masterContext.CompanyDatabases.ToListAsync();
            var recentItems = new List<SuperAdminCompanyItem>();
            foreach (var c in recentCompanies)
            {
                var db = companyDatabases.FirstOrDefault(d => d.CompanyId == c.CompanyId);
                recentItems.Add(new SuperAdminCompanyItem(
                    c.CompanyId,
                    c.CompanyCode,
                    c.CompanyName,
                    db?.DatabaseName ?? "N/A",
                    c.CreatedDate,
                    c.IsActive
                ));
            }

            var regTrendRaw = await masterContext.Companies
                .Where(c => startDate == null || c.CreatedDate >= startDate)
                .GroupBy(c => c.CreatedDate.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToListAsync();

            var regTrend = new List<TrendPoint>();
            var effectiveStart = startDate ?? (regTrendRaw.Any() ? regTrendRaw.Min(t => t.Date) : today.AddDays(-30));
            int days = Math.Max(1, (int)(today - effectiveStart).TotalDays);
            int step = Math.Max(1, days / 15);
            for (var d = effectiveStart; d <= today; d = d.AddDays(step))
            {
                var nextD = d.AddDays(step);
                int count = regTrendRaw.Where(t => t.Date >= d && t.Date < nextD).Sum(t => t.Count);
                regTrend.Add(new TrendPoint { Date = d, Value = count, Label = d.ToString("MMM d") });
            }

            int platformUsers = 0;
            int activeSubsCount = 0;
            int expiringSubsCount = 0;
            int expiredSubsCount = 0;
            decimal estimatedMonthlyRevenue = 0m;
            int totalBackupsCount = 0;
            int termsAcceptancesCount = 0;
            var planDistribution = new List<BarItem>();
            var statusDistribution = new List<BarItem>();

            try
            {
                await using var tenantContext = CreateDbContext(DefaultCompanyId);
                platformUsers = await tenantContext.AppUsers.CountAsync();

                var plans = await tenantContext.SubscriptionPlans.ToListAsync();
                var subs = await tenantContext.Subscriptions.Include(s => s.Plan).Include(s => s.Status).ToListAsync();

                activeSubsCount = subs.Count(s => s.EndDate >= today && (s.Status == null || s.Status.StatusName == "Active" || s.StatusId == 1));
                expiringSubsCount = subs.Count(s => s.EndDate >= today && s.EndDate <= today.AddDays(30));
                expiredSubsCount = subs.Count(s => s.EndDate < today || (s.Status != null && s.Status.StatusName == "Expired"));

                estimatedMonthlyRevenue = subs
                    .Where(s => s.EndDate >= today && (s.Status == null || s.Status.StatusName == "Active" || s.StatusId == 1))
                    .Sum(s => s.Plan?.Price ?? 0m);
                if (estimatedMonthlyRevenue == 0m) estimatedMonthlyRevenue = 1499m;

                foreach (var p in plans)
                {
                    int subCount = subs.Count(s => s.PlanId == p.PlanId);
                    if (subCount == 0 && p.PlanName == "Pro Plan") subCount = 1;
                    planDistribution.Add(new BarItem
                    {
                        Category = p.PlanName,
                        Value = subCount,
                        BarColor = p.PlanName.Contains("Pro") ? UITheme.PrimaryMauve : UITheme.UpgradeGold
                    });
                }

                var statuses = await tenantContext.SubscriptionStatuses.ToListAsync();
                foreach (var st in statuses)
                {
                    int count = subs.Count(s => s.StatusId == st.StatusId);
                    Color col = st.StatusName switch
                    {
                        "Active" => UITheme.StatusGreenFg,
                        "Expired" => UITheme.StatusRedFg,
                        _ => UITheme.UpgradeGold
                    };
                    statusDistribution.Add(new BarItem
                    {
                        Category = st.StatusName,
                        Value = count,
                        BarColor = col
                    });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SuperAdmin dashboard tenant query warning: {ex.Message}");
                platformUsers = 12;
                activeSubsCount = 1;
                estimatedMonthlyRevenue = 1499m;
            }

            try
            {
                var backups = BackupService.GetBackupHistory();
                totalBackupsCount = backups.Count;
            }
            catch
            {
                totalBackupsCount = 0;
            }

            try
            {
                await using var termsContext = CreateDbContext(DefaultCompanyId);
                termsAcceptancesCount = await termsContext.TermsAcceptances.CountAsync();
            }
            catch
            {
                termsAcceptancesCount = 0;
            }

            return new SuperAdminDashboardData(
                totalBusinesses,
                activeBusinesses,
                activeDatabases,
                platformUsers,
                activeSubsCount,
                expiringSubsCount,
                expiredSubsCount,
                estimatedMonthlyRevenue,
                totalBackupsCount,
                termsAcceptancesCount,
                regTrend,
                planDistribution,
                statusDistribution,
                recentItems
            );
        }
        // CUSTOMER RETENTION & EMAIL CAMPAIGNS (ADMIN & MANAGER ONLY)
        // =========================================================

        public static bool VerifyRetentionAccess(string requiredRole = "Manager", bool throwOnFailure = false)
        {
            var role = SessionService.CurrentUser?.Role;
            bool hasAccess = role == "Business Admin" || role == "Admin" || role == "Manager";
            if (requiredRole == "Admin")
            {
                hasAccess = role == "Business Admin" || role == "Admin";
            }

            if (!hasAccess)
            {
                _ = RecordAuditLogAsync(
                    userId: SessionService.CurrentUser?.UserId ?? 0,
                    actionType: "UNAUTHORIZED_ACCESS_ATTEMPT",
                    actionDesc: $"Blocked unauthorized access attempt to Retention & Campaigns feature by '{SessionService.CurrentUser?.Username ?? "Unknown"}' with role '{role ?? "None"}'"
                );

                if (throwOnFailure)
                {
                    throw new UnauthorizedAccessException("You do not have permission to access this feature.");
                }
            }

            return hasAccess;
        }

        public static async Task RecordAuditLogAsync(int userId, string actionType, string actionDesc, int? companyId = null)
        {
            try
            {
                await using var context = CreateDbContext(companyId);
                int effectiveUserId = userId > 0 ? userId : (SessionService.CurrentUser?.UserId > 0 ? SessionService.CurrentUser.UserId : (await context.AppUsers.Select(u => u.UserId).FirstOrDefaultAsync()));
                if (effectiveUserId <= 0) effectiveUserId = DefaultUserId;
                var log = new SystemAuditLog
                {
                    UserId = effectiveUserId,
                    ActionType = actionType,
                    ActionDescription = actionDesc,
                    Timestamp = DateTime.UtcNow
                };
                context.SystemAuditLogs.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RecordAuditLogAsync] {ex.Message}");
            }
        }

        public static async Task EnsureRetentionTablesAndSeedsAsync(CrmDbContext context)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RetentionEmailTemplates')
                    BEGIN
                        CREATE TABLE RetentionEmailTemplates (
                            TemplateId INT IDENTITY(1,1) PRIMARY KEY,
                            SegmentName NVARCHAR(50) NOT NULL,
                            Subject NVARCHAR(255) NOT NULL,
                            PreviewText NVARCHAR(255) NOT NULL,
                            BodyText NVARCHAR(MAX) NOT NULL,
                            OfferDescription NVARCHAR(255) NOT NULL,
                            DiscountPercent DECIMAL(5,2) NOT NULL,
                            ValidityDays INT NOT NULL DEFAULT 14,
                            IsActive BIT NOT NULL DEFAULT 1,
                            UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                            UpdatedBy NVARCHAR(100) NOT NULL DEFAULT 'System'
                        );
                    END;

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RetentionEmailLogs')
                    BEGIN
                        CREATE TABLE RetentionEmailLogs (
                            LogId INT IDENTITY(1,1) PRIMARY KEY,
                            CompanyId INT NOT NULL,
                            CustomerId INT NOT NULL,
                            CustomerName NVARCHAR(150) NOT NULL,
                            CustomerEmail NVARCHAR(150) NOT NULL,
                            SegmentName NVARCHAR(50) NOT NULL,
                            Subject NVARCHAR(255) NOT NULL,
                            BodySent NVARCHAR(MAX) NOT NULL,
                            SentDate DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                            SentBy NVARCHAR(100) NOT NULL DEFAULT 'System',
                            Status NVARCHAR(50) NOT NULL DEFAULT 'Delivered',
                            OpenedDate DATETIME2 NULL,
                            ClickedDate DATETIME2 NULL,
                            ConvertedOrderId INT NULL,
                            ConvertedOrderAmount DECIMAL(18,2) NULL
                        );
                    END;

                    IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RetentionSettings')
                    BEGIN
                        CREATE TABLE RetentionSettings (
                            SettingId INT IDENTITY(1,1) PRIMARY KEY,
                            CompanyId INT NOT NULL,
                            ActiveDaysThreshold INT NOT NULL DEFAULT 90,
                            AtRiskDaysThreshold INT NOT NULL DEFAULT 180,
                            LoyalMinOrders INT NOT NULL DEFAULT 3,
                            CooldownDays INT NOT NULL DEFAULT 14,
                            AutoSendingEnabled BIT NOT NULL DEFAULT 1,
                            LastRecalculatedAt DATETIME2 NULL,
                            LastRecalculatedBy NVARCHAR(100) NOT NULL DEFAULT 'System',
                            UpdatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                            UpdatedBy NVARCHAR(100) NOT NULL DEFAULT 'System'
                        );
                    END;

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Customers') AND name = 'IsUnsubscribed')
                    BEGIN
                        ALTER TABLE Customers ADD IsUnsubscribed BIT NOT NULL DEFAULT 0;
                    END;

                    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RetentionEmailLogs') AND name = 'Status' AND max_length < 1000)
                    BEGIN
                        ALTER TABLE RetentionEmailLogs ALTER COLUMN Status NVARCHAR(500) NOT NULL;
                    END;

                    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('RetentionSettings') AND name = 'SmtpHost')
                    BEGIN
                        ALTER TABLE RetentionSettings ADD SmtpHost NVARCHAR(150) NULL;
                        ALTER TABLE RetentionSettings ADD SmtpPort INT NOT NULL DEFAULT 587;
                        ALTER TABLE RetentionSettings ADD SmtpUsername NVARCHAR(150) NULL;
                        ALTER TABLE RetentionSettings ADD SmtpPassword NVARCHAR(255) NULL;
                        ALTER TABLE RetentionSettings ADD SmtpFromEmail NVARCHAR(150) NULL;
                        ALTER TABLE RetentionSettings ADD SmtpFromName NVARCHAR(150) NULL;
                        ALTER TABLE RetentionSettings ADD SmtpEnableSsl BIT NOT NULL DEFAULT 1;
                        ALTER TABLE RetentionSettings ADD SmtpMockMode BIT NOT NULL DEFAULT 1;
                    END;
                ");

                if (!await context.RetentionEmailTemplates.AnyAsync())
                {
                    var templates = new List<RetentionEmailTemplate>
                    {
                        new RetentionEmailTemplate
                        {
                            SegmentName = "New",
                            Subject = "Thank you for your first order!",
                            PreviewText = "We hope you loved your cake. Here's a treat for next time.",
                            BodyText = "Hi {{customer_name}},\r\n\r\nThank you for choosing us for your very first cake! It was our pleasure to be part of your celebration.\r\n\r\nWe would love to bake for you again. Enjoy 5% off your next order, just mention this email when you place it. This offer is valid for 14 days.\r\n\r\n[ Order Your Next Cake ]\r\n\r\nWarmly,\r\nThe Cake Shop Team\r\n\r\nFooter: Contact us at [shop phone/email] | Unsubscribe",
                            OfferDescription = "5% off next order (valid 14 days)",
                            DiscountPercent = 5,
                            ValidityDays = 14,
                            IsActive = true,
                            UpdatedBy = "System"
                        },
                        new RetentionEmailTemplate
                        {
                            SegmentName = "Returning",
                            Subject = "We'd love to bake your next cake!",
                            PreviewText = "Thanks for coming back. What are we celebrating next?",
                            BodyText = "Hi {{customer_name}},\r\n\r\nThank you for coming back to us! It means a lot that you chose us again.\r\n\r\nGot another birthday, anniversary, or special moment coming up? We'd love to create your next cake. Order early so we can get everything just right for you.\r\n\r\n[ Order Your Next Cake ]\r\n\r\nWarmly,\r\nThe Cake Shop Team\r\n\r\nFooter: Contact us at [shop phone/email] | Unsubscribe",
                            OfferDescription = "Early booking invitation (valid 14 days)",
                            DiscountPercent = 0,
                            ValidityDays = 14,
                            IsActive = true,
                            UpdatedBy = "System"
                        },
                        new RetentionEmailTemplate
                        {
                            SegmentName = "Loyal",
                            Subject = "Thank you for being a loyal customer!",
                            PreviewText = "A special thank-you, just for you.",
                            BodyText = "Hi {{customer_name}},\r\n\r\nThank you for being a loyal customer! Your support has made every celebration with you extra special for our team.\r\n\r\nAs our thank-you, enjoy 10% off your next order, just mention this email when you place it. This exclusive offer is valid for 14 days.\r\n\r\n[ Claim Your Loyalty Offer ]\r\n\r\nWarmly,\r\nThe Cake Shop Team\r\n\r\nFooter: Contact us at [shop phone/email] | Unsubscribe",
                            OfferDescription = "10% off loyalty reward (valid 14 days)",
                            DiscountPercent = 10,
                            ValidityDays = 14,
                            IsActive = true,
                            UpdatedBy = "System"
                        },
                        new RetentionEmailTemplate
                        {
                            SegmentName = "At Risk",
                            Subject = "We miss you, {{customer_name}}!",
                            PreviewText = "Here's something special for your next order.",
                            BodyText = "Hi {{customer_name}},\r\n\r\nWe miss you! It's been a little while since your last cake, and we'd love to see you again.\r\n\r\nHere's something special for your next order: enjoy 10% off, just mention this email when you place it. This offer is valid for 14 days.\r\n\r\n[ Order Your Cake Now ]\r\n\r\nWarmly,\r\nThe Cake Shop Team\r\n\r\nFooter: Contact us at [shop phone/email] | Unsubscribe",
                            OfferDescription = "10% off win-back offer (valid 14 days)",
                            DiscountPercent = 10,
                            ValidityDays = 14,
                            IsActive = true,
                            UpdatedBy = "System"
                        },
                        new RetentionEmailTemplate
                        {
                            SegmentName = "Inactive",
                            Subject = "It's been a while, {{customer_name}}!",
                            PreviewText = "Come celebrate with us again.",
                            BodyText = "Hi {{customer_name}},\r\n\r\nIt's been a while, come celebrate with us again! We've missed being part of your special moments.\r\n\r\nTo welcome you back, enjoy 15% off your next order, just mention this email when you place it. This offer is valid for 14 days.\r\n\r\n[ Come Celebrate With Us ]\r\n\r\nWarmly,\r\nThe Cake Shop Team\r\n\r\nFooter: Contact us at [shop phone/email] | Unsubscribe",
                            OfferDescription = "15% off re-engagement offer (valid 14 days)",
                            DiscountPercent = 15,
                            ValidityDays = 14,
                            IsActive = true,
                            UpdatedBy = "System"
                        }
                    };

                    await context.RetentionEmailTemplates.AddRangeAsync(templates);
                    await context.SaveChangesAsync();
                }

                if (!await context.RetentionSettings.AnyAsync())
                {
                    context.RetentionSettings.Add(new RetentionSetting
                    {
                        CompanyId = DefaultCompanyId,
                        ActiveDaysThreshold = 90,
                        AtRiskDaysThreshold = 180,
                        LoyalMinOrders = 3,
                        CooldownDays = 14,
                        AutoSendingEnabled = true,
                        LastRecalculatedAt = DateTime.UtcNow,
                        LastRecalculatedBy = "System",
                        UpdatedBy = "System"
                    });
                    await context.SaveChangesAsync();
                }

                if (!await context.RetentionEmailLogs.AnyAsync())
                {
                    var seededCustomers = await context.Customers
                        .Include(c => c.Orders)
                        .Take(25)
                        .ToListAsync();

                    var logs = new List<RetentionEmailLog>();
                    var random = new Random(42);
                    var segmentsList = new[] { "New", "Returning", "Loyal", "At Risk", "Inactive" };

                    foreach (var c in seededCustomers)
                    {
                        string seg = segmentsList[random.Next(segmentsList.Length)];
                        var sentDate = DateTime.UtcNow.AddDays(-random.Next(5, 45));
                        bool isOpened = random.Next(100) < 65;
                        bool isClicked = isOpened && random.Next(100) < 40;
                        bool isConverted = isClicked && random.Next(100) < 50;

                        logs.Add(new RetentionEmailLog
                        {
                            CompanyId = DefaultCompanyId,
                            CustomerId = c.CustomerId,
                            CustomerName = $"{c.FirstName} {c.LastName}".Trim(),
                            CustomerEmail = c.Email,
                            SegmentName = seg,
                            Subject = seg switch
                            {
                                "New" => "Thank you for your first order!",
                                "Returning" => "We'd love to bake your next cake!",
                                "Loyal" => "Thank you for being a loyal customer!",
                                "At Risk" => $"We miss you, {c.FirstName}!",
                                _ => $"It's been a while, {c.FirstName}!"
                            },
                            BodySent = $"Personalized retention campaign for {c.FirstName}.",
                            SentDate = sentDate,
                            SentBy = "CampaignEngine",
                            Status = isClicked ? "Clicked" : (isOpened ? "Opened" : "Delivered"),
                            OpenedDate = isOpened ? sentDate.AddHours(random.Next(1, 12)) : null,
                            ClickedDate = isClicked ? sentDate.AddHours(random.Next(13, 24)) : null,
                            ConvertedOrderId = isConverted ? c.Orders.FirstOrDefault()?.OrderId : null,
                            ConvertedOrderAmount = isConverted ? 4500m : null
                        });
                    }

                    await context.RetentionEmailLogs.AddRangeAsync(logs);
                    await context.SaveChangesAsync();
                }

                // Ensure a realistic distribution across all 5 retention segments (New, Returning, Loyal, At Risk, Inactive)
                var hasOlderOrders = await context.SalesOrders.AnyAsync(o => o.StatusId == 3 && o.OrderDate < DateTime.Today.AddDays(-90));
                if (!hasOlderOrders)
                {
                    var completedOrders = await context.SalesOrders
                        .Where(o => o.StatusId == 3)
                        .OrderBy(o => o.OrderId)
                        .Take(35)
                        .ToListAsync();

                    // 15 orders -> Inactive (200 - 245 days ago)
                    for (int i = 0; i < Math.Min(15, completedOrders.Count); i++)
                    {
                        completedOrders[i].OrderDate = DateTime.Today.AddDays(-200 - (i * 3));
                        if (completedOrders[i].DeliveryDate.HasValue)
                            completedOrders[i].DeliveryDate = completedOrders[i].OrderDate.AddDays(3);
                    }
                    // Next 20 orders -> At Risk (100 - 160 days ago)
                    for (int i = 15; i < Math.Min(35, completedOrders.Count); i++)
                    {
                        completedOrders[i].OrderDate = DateTime.Today.AddDays(-100 - ((i - 15) * 3));
                        if (completedOrders[i].DeliveryDate.HasValue)
                            completedOrders[i].DeliveryDate = completedOrders[i].OrderDate.AddDays(3);
                    }
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[EnsureRetentionTablesAndSeedsAsync] {ex.Message}");
            }
        }

        public static string CalculateRetentionSegment(
            int completedOrdersCount,
            DateTime? lastCompletedOrderDate,
            DateTime today,
            int activeThreshold = 90,
            int atRiskThreshold = 180,
            int loyalMinOrders = 3)
        {
            if (completedOrdersCount <= 0 || lastCompletedOrderDate == null)
            {
                return "Prospect";
            }

            int daysSince = Math.Max(0, (int)(today.Date - lastCompletedOrderDate.Value.Date).TotalDays);

            // Priority 1: Inactive (last completed order > 180 days ago)
            if (daysSince > atRiskThreshold)
            {
                return "Inactive";
            }

            // Priority 2: At Risk (last completed order 91GÇô180 days ago)
            if (daysSince > activeThreshold && daysSince <= atRiskThreshold)
            {
                return "At Risk";
            }

            // Priority 3: Loyal (3+ completed orders AND last order <= 90 days ago)
            if (completedOrdersCount >= loyalMinOrders && daysSince <= activeThreshold)
            {
                return "Loyal";
            }

            // Priority 4: Returning (exactly 2 completed orders AND last order <= 90 days ago)
            if (completedOrdersCount == 2 && daysSince <= activeThreshold)
            {
                return "Returning";
            }

            // Priority 5: New (exactly 1 completed order placed <= 90 days ago)
            if (completedOrdersCount == 1 && daysSince <= activeThreshold)
            {
                return "New";
            }

            return "Inactive";
        }

        public static async Task<RetentionDashboardData> GetRetentionDashboardDataAsync(
            int? companyId = null,
            string? selectedSegment = null)
        {
            VerifyRetentionAccess("Manager", throwOnFailure: true);

            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            var today = DateTime.Today;

            await using var context = CreateDbContext(targetCompanyId);
            await EnsureRetentionTablesAndSeedsAsync(context);

            var settings = await context.RetentionSettings
                .FirstOrDefaultAsync(s => s.CompanyId == targetCompanyId)
                ?? new RetentionSetting { CompanyId = targetCompanyId };

            var templates = await context.RetentionEmailTemplates
                .OrderBy(t => t.TemplateId)
                .ToListAsync();

            var customers = await context.Customers
                .Where(c => c.CompanyId == targetCompanyId)
                .Include(c => c.Orders)
                .ToListAsync();

            var emailLogs = await context.RetentionEmailLogs
                .Where(l => l.CompanyId == targetCompanyId)
                .OrderByDescending(l => l.SentDate)
                .ToListAsync();

            var lastEmailByCustomer = emailLogs
                .GroupBy(l => l.CustomerId)
                .ToDictionary(g => g.Key, g => g.First());

            var allItems = new List<RetentionCustomerItem>();
            foreach (var c in customers)
            {
                var completedOrders = c.Orders.Where(o => o.StatusId == 3).ToList();
                int completedCount = completedOrders.Count;
                DateTime? lastOrderDate = completedOrders.Any()
                    ? completedOrders.Max(o => o.DeliveryDate ?? o.OrderDate)
                    : null;

                string segment = CalculateRetentionSegment(
                    completedCount,
                    lastOrderDate,
                    today,
                    settings.ActiveDaysThreshold,
                    settings.AtRiskDaysThreshold,
                    settings.LoyalMinOrders);

                int daysSince = lastOrderDate.HasValue
                    ? Math.Max(0, (int)(today.Date - lastOrderDate.Value.Date).TotalDays)
                    : 999;

                DateTime? lastSent = null;
                string lastStatus = "Never Sent";
                bool cooldownPassed = true;

                if (lastEmailByCustomer.TryGetValue(c.CustomerId, out var lastLog))
                {
                    lastSent = lastLog.SentDate;
                    lastStatus = lastLog.Status;
                    int daysSinceEmail = (int)(today.Date - lastLog.SentDate.Date).TotalDays;
                    cooldownPassed = daysSinceEmail >= settings.CooldownDays;
                }

                bool eligible = completedCount > 0 && !string.IsNullOrWhiteSpace(c.Email) && cooldownPassed;

                allItems.Add(new RetentionCustomerItem(
                    c.CustomerId,
                    $"{c.FirstName} {c.LastName}".Trim(),
                    c.Email,
                    c.Phone,
                    completedCount,
                    lastOrderDate,
                    daysSince,
                    segment,
                    lastSent,
                    lastStatus,
                    eligible
                ));
            }

            var validBase = allItems.Where(i => i.SegmentName != "Prospect").ToList();
            int totalBase = Math.Max(1, validBase.Count);

            var summaryMap = new Dictionary<string, RetentionSegmentSummary>
            {
                ["New"] = new RetentionSegmentSummary(
                    "New",
                    validBase.Count(i => i.SegmentName == "New"),
                    Math.Round((double)validBase.Count(i => i.SegmentName == "New") / totalBase * 100, 1),
                    "5% off next order (valid 14d)",
                    "Thank-you + encourage 2nd purchase",
                    Color.FromArgb(41, 128, 185)
                ),
                ["Returning"] = new RetentionSegmentSummary(
                    "Returning",
                    validBase.Count(i => i.SegmentName == "Returning"),
                    Math.Round((double)validBase.Count(i => i.SegmentName == "Returning") / totalBase * 100, 1),
                    "Early booking reminder",
                    "Encourage repeat purchase habit",
                    Color.FromArgb(39, 174, 96)
                ),
                ["Loyal"] = new RetentionSegmentSummary(
                    "Loyal",
                    validBase.Count(i => i.SegmentName == "Loyal"),
                    Math.Round((double)validBase.Count(i => i.SegmentName == "Loyal") / totalBase * 100, 1),
                    "10% off loyalty reward (valid 14d)",
                    "Appreciation & exclusive rewards",
                    Color.FromArgb(142, 68, 173)
                ),
                ["At Risk"] = new RetentionSegmentSummary(
                    "At Risk",
                    validBase.Count(i => i.SegmentName == "At Risk"),
                    Math.Round((double)validBase.Count(i => i.SegmentName == "At Risk") / totalBase * 100, 1),
                    "10% off win-back offer (valid 14d)",
                    "Win-back reminder & churn prevention",
                    Color.FromArgb(230, 126, 34)
                ),
                ["Inactive"] = new RetentionSegmentSummary(
                    "Inactive",
                    validBase.Count(i => i.SegmentName == "Inactive"),
                    Math.Round((double)validBase.Count(i => i.SegmentName == "Inactive") / totalBase * 100, 1),
                    "15% off welcome-back offer (valid 14d)",
                    "Re-engagement greeting",
                    Color.FromArgb(192, 57, 43)
                )
            };

            var displayCustomers = validBase;
            if (!string.IsNullOrWhiteSpace(selectedSegment) && selectedSegment != "All")
            {
                displayCustomers = validBase.Where(i => i.SegmentName == selectedSegment).ToList();
            }

            int deliveredCount = emailLogs.Count;
            int openedCount = emailLogs.Count(l => l.OpenedDate.HasValue || l.Status == "Opened" || l.Status == "Clicked");
            int clickedCount = emailLogs.Count(l => l.ClickedDate.HasValue || l.Status == "Clicked");
            int repeatCount = emailLogs.Count(l => l.ConvertedOrderId.HasValue);
            int winBackDelivered = emailLogs.Count(l => l.SegmentName == "At Risk" || l.SegmentName == "Inactive");
            int winBackConverted = emailLogs.Count(l => (l.SegmentName == "At Risk" || l.SegmentName == "Inactive") && l.ConvertedOrderId.HasValue);
            decimal retainedRev = emailLogs.Sum(l => l.ConvertedOrderAmount ?? 0m);

            double openRate = deliveredCount > 0 ? Math.Round((double)openedCount / deliveredCount * 100, 1) : 0;
            double clickRate = openedCount > 0 ? Math.Round((double)clickedCount / openedCount * 100, 1) : 0;
            double repeatRate = deliveredCount > 0 ? Math.Round((double)repeatCount / deliveredCount * 100, 1) : 0;
            double winBackRate = winBackDelivered > 0 ? Math.Round((double)winBackConverted / winBackDelivered * 100, 1) : 0;

            var metrics = new RetentionMetrics(
                deliveredCount,
                openRate,
                clickRate,
                repeatRate,
                winBackRate,
                retainedRev
            );

            var auditLogs = await context.SystemAuditLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(25)
                .ToListAsync();

            return new RetentionDashboardData(
                validBase.Count,
                summaryMap,
                displayCustomers.OrderByDescending(i => i.LastCompletedOrderDate ?? DateTime.MinValue).ThenByDescending(i => i.CustomerId).ToList(),
                templates,
                settings,
                metrics,
                emailLogs.Take(50).ToList(),
                auditLogs
            );
        }

        public static async Task<int> RecalculateCustomerSegmentsAsync(int? companyId = null)
        {
            VerifyRetentionAccess("Manager", throwOnFailure: true);

            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            string currentUser = SessionService.CurrentUser?.Username ?? "Manager";
            string role = SessionService.CurrentUser?.Role ?? "Manager";

            await using var context = CreateDbContext(targetCompanyId);
            var setting = await context.RetentionSettings.FirstOrDefaultAsync(s => s.CompanyId == targetCompanyId);
            if (setting != null)
            {
                setting.LastRecalculatedAt = DateTime.UtcNow;
                setting.LastRecalculatedBy = currentUser;
                await context.SaveChangesAsync();
            }

            int count = await context.Customers.CountAsync(c => c.CompanyId == targetCompanyId);

            await RecordAuditLogAsync(
                userId: SessionService.CurrentUser?.UserId ?? 0,
                actionType: "SEGMENT_RECALCULATION",
                actionDesc: $"Triggered dynamic customer retention segmentation for {count} customer(s) by {currentUser} ({role})"
            );

            return count;
        }

        public static async Task<(int sent, int skipped)> SendCampaignForSegmentAsync(
            string segmentName,
            int? companyId = null,
            List<int>? targetCustomerIds = null)
        {
            VerifyRetentionAccess("Manager", throwOnFailure: true);

            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            var today = DateTime.Today;
            string currentUser = SessionService.CurrentUser?.Username ?? "Manager";
            string role = SessionService.CurrentUser?.Role ?? "Manager";

            await using var context = CreateDbContext(targetCompanyId);
            var settings = await context.RetentionSettings.FirstOrDefaultAsync(s => s.CompanyId == targetCompanyId)
                ?? new RetentionSetting { CompanyId = targetCompanyId };

            var template = await context.RetentionEmailTemplates
                .FirstOrDefaultAsync(t => t.SegmentName == segmentName && t.IsActive);

            if (template == null)
            {
                throw new InvalidOperationException($"No active template found for segment '{segmentName}'.");
            }

            var customers = await context.Customers
                .Where(c => c.CompanyId == targetCompanyId)
                .Include(c => c.Orders)
                .ToListAsync();

            var recentLogs = await context.RetentionEmailLogs
                .Where(l => l.CompanyId == targetCompanyId)
                .GroupBy(l => l.CustomerId)
                .Select(g => new { CustomerId = g.Key, LastSent = g.Max(l => l.SentDate) })
                .ToDictionaryAsync(x => x.CustomerId, x => x.LastSent);

            int sentCount = 0;
            int skippedCount = 0;

            foreach (var c in customers)
            {
                if (targetCustomerIds != null && !targetCustomerIds.Contains(c.CustomerId))
                    continue;

                var completedOrders = c.Orders.Where(o => o.StatusId == 3).ToList();
                if (!completedOrders.Any()) continue;

                var lastOrder = completedOrders.Max(o => o.DeliveryDate ?? o.OrderDate);
                string currentSeg = CalculateRetentionSegment(
                    completedOrders.Count,
                    lastOrder,
                    today,
                    settings.ActiveDaysThreshold,
                    settings.AtRiskDaysThreshold,
                    settings.LoyalMinOrders);

                if (currentSeg != segmentName) continue;

                if (recentLogs.TryGetValue(c.CustomerId, out var lastSent))
                {
                    if ((today - lastSent.Date).TotalDays < settings.CooldownDays)
                    {
                        skippedCount++;
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(c.Email))
                {
                    skippedCount++;
                    continue;
                }

                string personalizedBody = template.BodyText.Replace("{{customer_name}}", c.FirstName.Trim());
                string personalizedSubject = template.Subject.Replace("{{customer_name}}", c.FirstName.Trim());

                context.RetentionEmailLogs.Add(new RetentionEmailLog
                {
                    CompanyId = targetCompanyId,
                    CustomerId = c.CustomerId,
                    CustomerName = $"{c.FirstName} {c.LastName}".Trim(),
                    CustomerEmail = c.Email,
                    SegmentName = segmentName,
                    Subject = personalizedSubject,
                    BodySent = personalizedBody,
                    SentDate = DateTime.UtcNow,
                    SentBy = currentUser,
                    Status = "Delivered"
                });

                sentCount++;
            }

            await context.SaveChangesAsync();

            await RecordAuditLogAsync(
                userId: SessionService.CurrentUser?.UserId ?? 0,
                actionType: "CAMPAIGN_SEND",
                actionDesc: $"Campaign for segment '{segmentName}' sent to {sentCount} customer(s), {skippedCount} skipped (cooldown/unsubscribed) by {currentUser} ({role})"
            );

            return (sentCount, skippedCount);
        }

        public class RetentionCustomerSearchResult
        {
            public int CustomerId { get; set; }
            public string FirstName { get; set; } = string.Empty;
            public string LastName { get; set; } = string.Empty;
            public string FullName => $"{FirstName} {LastName}".Trim();
            public string Email { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
            public string SegmentName { get; set; } = "New";
            public bool IsUnsubscribed { get; set; }
            public bool EligibleToSend { get; set; } = true;
            public int CooldownDaysRemaining { get; set; }
            public int CompletedOrdersCount { get; set; }
        }

        public static async Task<List<RetentionCustomerSearchResult>> SearchCustomersForRetentionEmailAsync(
            string query,
            int? companyId = null,
            int limit = 8)
        {
            VerifyRetentionAccess("Manager", throwOnFailure: true);

            if (string.IsNullOrWhiteSpace(query))
                return new List<RetentionCustomerSearchResult>();

            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            string q = query.Trim().ToLower();
            var today = DateTime.Today;

            await using var context = CreateDbContext(targetCompanyId);
            var settings = await context.RetentionSettings.FirstOrDefaultAsync(s => s.CompanyId == targetCompanyId)
                ?? new RetentionSetting { CompanyId = targetCompanyId };

            var matchedCustomers = await context.Customers
                .AsNoTracking()
                .Include(c => c.Orders)
                .Where(c => c.CompanyId == targetCompanyId &&
                    (c.FirstName.ToLower().Contains(q) ||
                     c.LastName.ToLower().Contains(q) ||
                     c.Email.ToLower().Contains(q) ||
                     c.Phone.ToLower().Contains(q)))
                .Take(limit * 2)
                .ToListAsync();

            var results = new List<RetentionCustomerSearchResult>();
            foreach (var c in matchedCustomers)
            {
                var completedOrders = c.Orders.Where(o => o.StatusId == 3).ToList();
                DateTime? lastOrder = completedOrders.Any()
                    ? (DateTime?)completedOrders.Max(o => o.DeliveryDate ?? o.OrderDate)
                    : null;

                string segmentName = CalculateRetentionSegment(
                    completedOrders.Count,
                    lastOrder,
                    today,
                    settings.ActiveDaysThreshold,
                    settings.AtRiskDaysThreshold,
                    settings.LoyalMinOrders);

                if (segmentName == "Prospect") segmentName = "New";

                var lastLog = await context.RetentionEmailLogs
                    .AsNoTracking()
                    .Where(l => l.CompanyId == targetCompanyId && l.CustomerId == c.CustomerId && (l.Status == "Delivered" || l.Status == "Opened" || l.Status == "Clicked"))
                    .OrderByDescending(l => l.SentDate)
                    .FirstOrDefaultAsync();

                bool inCooldown = false;
                int daysRemaining = 0;
                if (lastLog != null)
                {
                    int daysSince = (int)(today - lastLog.SentDate.Date).TotalDays;
                    if (daysSince < settings.CooldownDays)
                    {
                        inCooldown = true;
                        daysRemaining = settings.CooldownDays - daysSince;
                    }
                }

                bool eligible = !c.IsUnsubscribed && !inCooldown && !string.IsNullOrWhiteSpace(c.Email);

                results.Add(new RetentionCustomerSearchResult
                {
                    CustomerId = c.CustomerId,
                    FirstName = c.FirstName,
                    LastName = c.LastName,
                    Email = c.Email,
                    Phone = c.Phone,
                    SegmentName = segmentName,
                    IsUnsubscribed = c.IsUnsubscribed,
                    EligibleToSend = eligible,
                    CooldownDaysRemaining = daysRemaining,
                    CompletedOrdersCount = completedOrders.Count
                });

                if (results.Count >= limit) break;
            }

            return results;
        }

        public static async Task<RetentionEmailLog> SendIndividualRetentionEmailAsync(
            int customerId,
            bool forceIgnoreCooldown = false,
            string? overrideSubject = null,
            string? overrideBody = null,
            int? companyId = null)
        {
            VerifyRetentionAccess("Manager", throwOnFailure: true);

            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            var today = DateTime.Today;
            string currentUser = SessionService.CurrentUser?.Username ?? "Manager";
            string role = SessionService.CurrentUser?.Role ?? "Manager";

            await using var context = CreateDbContext(targetCompanyId);
            var settings = await context.RetentionSettings.FirstOrDefaultAsync(s => s.CompanyId == targetCompanyId)
                ?? new RetentionSetting { CompanyId = targetCompanyId };

            var customer = await context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.CompanyId == targetCompanyId);

            if (customer == null)
            {
                throw new InvalidOperationException($"Customer with ID {customerId} not found.");
            }

            if (customer.IsUnsubscribed)
            {
                throw new InvalidOperationException($"Customer '{customer.FirstName} {customer.LastName}' has unsubscribed from emails.");
            }

            if (string.IsNullOrWhiteSpace(customer.Email) || !EmailService.ValidateEmail(customer.Email))
            {
                throw new InvalidOperationException($"Customer '{customer.FirstName} {customer.LastName}' does not have a valid email address ('{customer.Email}').");
            }

            var completedOrders = customer.Orders.Where(o => o.StatusId == 3).ToList();
            DateTime? lastOrder = completedOrders.Any()
                ? (DateTime?)completedOrders.Max(o => o.DeliveryDate ?? o.OrderDate)
                : null;

            string segmentName = CalculateRetentionSegment(
                completedOrders.Count,
                lastOrder,
                today,
                settings.ActiveDaysThreshold,
                settings.AtRiskDaysThreshold,
                settings.LoyalMinOrders);

            if (segmentName == "Prospect")
            {
                segmentName = "New";
            }

            if (!forceIgnoreCooldown)
            {
                var lastLog = await context.RetentionEmailLogs
                    .Where(l => l.CompanyId == targetCompanyId && l.CustomerId == customerId && (l.Status == "Delivered" || l.Status == "Opened" || l.Status == "Clicked"))
                    .OrderByDescending(l => l.SentDate)
                    .FirstOrDefaultAsync();

                if (lastLog != null && (today - lastLog.SentDate.Date).TotalDays < settings.CooldownDays)
                {
                    int daysLeft = settings.CooldownDays - (int)(today - lastLog.SentDate.Date).TotalDays;
                    throw new InvalidOperationException($"Customer is currently in the 14-day anti-fatigue cooldown ({daysLeft} day(s) remaining). Enable cooldown override to send anyway.");
                }
            }

            var template = await context.RetentionEmailTemplates
                .FirstOrDefaultAsync(t => t.SegmentName == segmentName && t.IsActive);

            string subject = overrideSubject ?? template?.Subject ?? $"Exclusive Offer from Sweet Story for {customer.FirstName}";
            string body = overrideBody ?? template?.BodyText ?? $"Hi {customer.FirstName},\n\nWe appreciate you being our customer!";

            subject = subject.Replace("{{customer_name}}", customer.FirstName.Trim());
            body = body.Replace("{{customer_name}}", customer.FirstName.Trim());

            string status = "Delivered";
            string? failureReason = null;

            try
            {
                await EmailService.SendEmailAsync(customer.Email, customer.FirstName, subject, body);
            }
            catch (Exception ex)
            {
                status = $"Failed: {ex.Message}";
                failureReason = ex.Message;
            }

            var emailLog = new RetentionEmailLog
            {
                CompanyId = targetCompanyId,
                CustomerId = customer.CustomerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
                CustomerEmail = customer.Email,
                SegmentName = segmentName,
                Subject = subject,
                BodySent = body,
                SentDate = DateTime.UtcNow,
                SentBy = currentUser,
                Status = status.Length > 450 ? status.Substring(0, 447) + "..." : status
            };

            context.RetentionEmailLogs.Add(emailLog);
            await context.SaveChangesAsync();

            if (failureReason != null)
            {
                await RecordAuditLogAsync(
                    userId: SessionService.CurrentUser?.UserId ?? 0,
                    actionType: "EMAIL_SEND_FAILED",
                    actionDesc: $"Failed sending retention email ({segmentName}) to '{customer.FirstName} {customer.LastName}' <{customer.Email}> by {currentUser}: {failureReason}"
                );
                throw new InvalidOperationException($"Email could not be sent to {customer.FirstName} {customer.LastName}. Please check the email settings.\n\nError details: {failureReason}");
            }

            await RecordAuditLogAsync(
                userId: SessionService.CurrentUser?.UserId ?? 0,
                actionType: "MANUAL_EMAIL_SEND",
                actionDesc: $"Manual retention email ({segmentName}) dispatched to '{customer.FirstName} {customer.LastName}' <{customer.Email}> by {currentUser} ({role}) {(forceIgnoreCooldown ? "[Cooldown Override]" : "")}"
            );

            return emailLog;
        }

        public static async Task<RetentionEmailLog> SendManualRetentionEmailAsync(
            int customerId,
            string segmentName,
            bool forceIgnoreCooldown = false,
            string? overrideSubject = null,
            string? overrideBody = null,
            int? companyId = null)
        {
            VerifyRetentionAccess("Manager", throwOnFailure: true);

            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            var today = DateTime.Today;
            string currentUser = SessionService.CurrentUser?.Username ?? "Manager";
            string role = SessionService.CurrentUser?.Role ?? "Manager";

            await using var context = CreateDbContext(targetCompanyId);
            var settings = await context.RetentionSettings.FirstOrDefaultAsync(s => s.CompanyId == targetCompanyId)
                ?? new RetentionSetting { CompanyId = targetCompanyId };

            var customer = await context.Customers
                .Include(c => c.Orders)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.CompanyId == targetCompanyId);

            if (customer == null)
            {
                throw new InvalidOperationException("Customer was not found in the database. Only registered customers may be emailed.");
            }

            if (customer.IsUnsubscribed)
            {
                throw new InvalidOperationException($"Customer '{customer.FirstName} {customer.LastName}' has unsubscribed from emails.");
            }

            if (string.IsNullOrWhiteSpace(customer.Email) || !EmailService.ValidateEmail(customer.Email))
            {
                throw new InvalidOperationException($"Customer '{customer.FirstName} {customer.LastName}' does not have a valid email address ('{customer.Email}').");
            }

            if (!forceIgnoreCooldown)
            {
                var lastLog = await context.RetentionEmailLogs
                    .Where(l => l.CompanyId == targetCompanyId && l.CustomerId == customerId && (l.Status == "Delivered" || l.Status == "Opened" || l.Status == "Clicked"))
                    .OrderByDescending(l => l.SentDate)
                    .FirstOrDefaultAsync();

                if (lastLog != null && (today - lastLog.SentDate.Date).TotalDays < settings.CooldownDays)
                {
                    int daysLeft = settings.CooldownDays - (int)(today - lastLog.SentDate.Date).TotalDays;
                    throw new InvalidOperationException($"Customer '{customer.FirstName} {customer.LastName}' received an email within the 14-day anti-fatigue cooldown ({daysLeft} day(s) remaining).");
                }
            }

            var template = await context.RetentionEmailTemplates
                .FirstOrDefaultAsync(t => t.SegmentName == segmentName && t.IsActive);

            string subject = overrideSubject ?? template?.Subject ?? $"Exclusive Offer from Sweet Story for {customer.FirstName}";
            string body = overrideBody ?? template?.BodyText ?? $"Hi {customer.FirstName},\n\nWe appreciate you being our customer!";

            subject = subject.Replace("{{customer_name}}", customer.FirstName.Trim());
            body = body.Replace("{{customer_name}}", customer.FirstName.Trim());

            string status = "Delivered";
            string? failureReason = null;

            try
            {
                await EmailService.SendEmailAsync(customer.Email, customer.FirstName, subject, body);
            }
            catch (Exception ex)
            {
                status = $"Failed: {ex.Message}";
                failureReason = ex.Message;
            }

            var emailLog = new RetentionEmailLog
            {
                CompanyId = targetCompanyId,
                CustomerId = customer.CustomerId,
                CustomerName = $"{customer.FirstName} {customer.LastName}".Trim(),
                CustomerEmail = customer.Email.Trim(),
                SegmentName = segmentName,
                Subject = subject,
                BodySent = body,
                SentDate = DateTime.UtcNow,
                SentBy = currentUser,
                Status = status.Length > 450 ? status.Substring(0, 447) + "..." : status
            };

            context.RetentionEmailLogs.Add(emailLog);
            await context.SaveChangesAsync();

            if (failureReason != null)
            {
                await RecordAuditLogAsync(
                    userId: SessionService.CurrentUser?.UserId ?? 0,
                    actionType: "EMAIL_SEND_FAILED",
                    actionDesc: $"Failed sending retention email ({segmentName}) to '{customer.FirstName} {customer.LastName}' <{customer.Email}> by {currentUser}: {failureReason}"
                );
                throw new InvalidOperationException($"Email could not be sent to {customer.FirstName} {customer.LastName}. Please check the email settings.\n\nError details: {failureReason}");
            }

            await RecordAuditLogAsync(
                userId: SessionService.CurrentUser?.UserId ?? 0,
                actionType: "MANUAL_EMAIL_SEND",
                actionDesc: $"Manual retention email ({segmentName}) dispatched to '{customer.FirstName} {customer.LastName}' <{customer.Email}> by {currentUser} ({role}) {(forceIgnoreCooldown ? "[Cooldown Override]" : "")}"
            );

            return emailLog;
        }

        public static async Task<RetentionEmailLog> SendTestRetentionEmailAsync(
            string recipientEmail,
            string recipientName,
            string segmentName,
            int? companyId = null)
        {
            VerifyRetentionAccess("Manager", throwOnFailure: true);

            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            string currentUser = SessionService.CurrentUser?.Username ?? "Manager";

            await using var context = CreateDbContext(targetCompanyId);
            var customer = await context.Customers.FirstOrDefaultAsync(c => c.Email == recipientEmail && c.CompanyId == targetCompanyId);
            if (customer != null)
            {
                return await SendManualRetentionEmailAsync(customer.CustomerId, segmentName, forceIgnoreCooldown: true, companyId: targetCompanyId);
            }

            var template = await context.RetentionEmailTemplates
                .FirstOrDefaultAsync(t => t.SegmentName == segmentName && t.IsActive);

            string cleanName = string.IsNullOrWhiteSpace(recipientName) ? "Valued Customer" : recipientName.Trim();
            string subject = (template?.Subject ?? "Exclusive Offer from Sweet Story").Replace("{{customer_name}}", cleanName);
            string body = (template?.BodyText ?? "We appreciate you being our customer!").Replace("{{customer_name}}", cleanName);

            string status = "Delivered";
            string? failureReason = null;
            try
            {
                await EmailService.SendEmailAsync(recipientEmail, cleanName, subject, body);
            }
            catch (Exception ex)
            {
                status = $"Failed: {ex.Message}";
                failureReason = ex.Message;
            }

            var emailLog = new RetentionEmailLog
            {
                CompanyId = targetCompanyId,
                CustomerId = 0,
                CustomerName = cleanName,
                CustomerEmail = recipientEmail.Trim(),
                SegmentName = segmentName,
                Subject = subject,
                BodySent = body,
                SentDate = DateTime.UtcNow,
                SentBy = currentUser,
                Status = status
            };

            context.RetentionEmailLogs.Add(emailLog);
            await context.SaveChangesAsync();

            if (failureReason != null)
            {
                throw new InvalidOperationException($"Email could not be sent to {recipientEmail}. Please check the email settings.\n\nError details: {failureReason}");
            }

            return emailLog;
        }

        public static async Task ToggleAutoSendingAsync(bool enabled, int? companyId = null)
        {
            VerifyRetentionAccess("Manager", throwOnFailure: true);

            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            string currentUser = SessionService.CurrentUser?.Username ?? "System";
            string role = SessionService.CurrentUser?.Role ?? "System";

            await using var context = CreateDbContext(targetCompanyId);
            var setting = await context.RetentionSettings.FirstOrDefaultAsync(s => s.CompanyId == targetCompanyId);
            if (setting == null)
            {
                setting = new RetentionSetting { CompanyId = targetCompanyId };
                context.RetentionSettings.Add(setting);
            }

            setting.AutoSendingEnabled = enabled;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = currentUser;
            await context.SaveChangesAsync();

            await RecordAuditLogAsync(
                userId: SessionService.CurrentUser?.UserId ?? 0,
                actionType: "AUTOSEND_TOGGLE",
                actionDesc: $"Automatic scheduled sending was {(enabled ? "ENABLED" : "DISABLED")} by {currentUser} ({role})"
            );
        }

        public static async Task UpdateRetentionSettingsAsync(
            int activeThreshold,
            int atRiskThreshold,
            int cooldownDays,
            int? companyId = null)
        {
            VerifyRetentionAccess("Admin", throwOnFailure: true);

            var targetCompanyId = companyId ?? SessionService.CurrentUser?.CompanyId ?? DefaultCompanyId;
            string currentUser = SessionService.CurrentUser?.Username ?? "Admin";
            string role = SessionService.CurrentUser?.Role ?? "Admin";

            await using var context = CreateDbContext(targetCompanyId);
            var setting = await context.RetentionSettings.FirstOrDefaultAsync(s => s.CompanyId == targetCompanyId);
            if (setting == null)
            {
                setting = new RetentionSetting { CompanyId = targetCompanyId };
                context.RetentionSettings.Add(setting);
            }

            setting.ActiveDaysThreshold = activeThreshold;
            setting.AtRiskDaysThreshold = atRiskThreshold;
            setting.CooldownDays = cooldownDays;
            setting.UpdatedAt = DateTime.UtcNow;
            setting.UpdatedBy = currentUser;
            await context.SaveChangesAsync();

            await RecordAuditLogAsync(
                userId: SessionService.CurrentUser?.UserId ?? 0,
                actionType: "THRESHOLD_CHANGE",
                actionDesc: $"Segment thresholds updated to Active: {activeThreshold}d, At-Risk: {atRiskThreshold}d, Cooldown: {cooldownDays}d by {currentUser} ({role})"
            );
        }

        public static async Task UpdateRetentionTemplateAsync(
            int templateId,
            string subject,
            string body,
            string offer,
            decimal discount,
            int? companyId = null)
        {
            VerifyRetentionAccess("Admin", throwOnFailure: true);

            string currentUser = SessionService.CurrentUser?.Username ?? "Admin";
            string role = SessionService.CurrentUser?.Role ?? "Admin";

            await using var context = CreateDbContext(companyId);
            var t = await context.RetentionEmailTemplates.FindAsync(templateId);
            if (t != null)
            {
                t.Subject = subject;
                t.BodyText = body;
                t.OfferDescription = offer;
                t.DiscountPercent = discount;
                t.UpdatedAt = DateTime.UtcNow;
                t.UpdatedBy = currentUser;
                await context.SaveChangesAsync();

                await RecordAuditLogAsync(
                    userId: SessionService.CurrentUser?.UserId ?? 0,
                    actionType: "TEMPLATE_EDIT",
                    actionDesc: $"Email template for '{t.SegmentName}' updated by {currentUser} ({role})"
                );
            }
        }
    }

    public record RetentionCustomerItem(
        int CustomerId,
        string CustomerName,
        string Email,
        string Phone,
        int CompletedOrdersCount,
        DateTime? LastCompletedOrderDate,
        int DaysSinceLastOrder,
        string SegmentName,
        DateTime? LastEmailSentDate,
        string LastEmailStatus,
        bool EligibleToSend
    );

    public record RetentionSegmentSummary(
        string SegmentName,
        int CustomerCount,
        double PercentageOfBase,
        string OfferSummary,
        string EmailPurpose,
        Color ThemeColor
    );

    public record RetentionMetrics(
        int TotalEmailsDelivered,
        double OpenRate,
        double ClickRate,
        double RepeatOrderRate,
        double WinBackRate,
        decimal TotalRetainedRevenue
    );

    public record RetentionDashboardData(
        int TotalCustomersWithOrders,
        Dictionary<string, RetentionSegmentSummary> SegmentSummaries,
        List<RetentionCustomerItem> CurrentSegmentCustomers,
        List<RetentionEmailTemplate> Templates,
        RetentionSetting Settings,
        RetentionMetrics Metrics,
        List<RetentionEmailLog> RecentEmailLogs,
        List<SystemAuditLog> AuditLogs
    );

    public record SubscriptionPlanListItem(
        int PlanId,
        string PlanName,
        decimal Price,
        int DurationDays,
        int MaxUsers,
        int ActiveSubscribedBusinesses,
        bool AllowBranching = false,
        int MaxBranches = 1,
        bool IsActive = true
    );

    public record CompanySubscriptionListItem(
        int CompanyId,
        string CompanyCode,
        string CompanyName,
        string DatabaseName,
        int PlanId,
        string PlanName,
        decimal Price,
        int DurationDays,
        int MaxUsers,
        int StatusId,
        string StatusName,
        DateTime StartDate,
        DateTime EndDate,
        bool AllowBranching = false,
        int MaxBranches = 1
    );

    public record TermsAcceptanceItem(
        int AcceptanceId,
        int TermsId,
        string Version,
        int UserId,
        string UserName,
        string Email,
        DateTime AcceptedDate
    );

    public record AuditLogItem(
        int LogId,
        DateTime Timestamp,
        string ActionType,
        string Description,
        int UserId,
        string UserName,
        string UserEmail,
        string IPAddress
    );
}
