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
    public class SeedResult
    {
        public bool Success { get; set; }
        public bool AlreadySeeded { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, int> Counts { get; set; } = new();
    }

    public static class DatabaseSeeder
    {
        public const string DemoTag = "[DEMO-DATA]";

        private static readonly string[] FirstNames = new[]
        {
            "Maria", "Juan", "Liza", "Joshua", "Kathryn", "Daniel", "Bea", "Alden", "Maine", "Nadine",
            "James", "Marian", "Dingdong", "Carlo", "Angel", "Coco", "Sarah", "Matteo", "Judy", "Ryan",
            "Piolo", "Anne", "Erwan", "Solenn", "Jericho", "Heart", "Richard", "Derek", "Ellen", "Kim",
            "Gerald", "Enrique", "Janella", "Elisse", "McCoy", "Andrea", "Seth", "Francine", "Kyle", "Belle",
            "Kristine", "Oyo", "Jennylyn", "Dennis", "Iza", "Ben", "Carla", "Tom", "Maja", "Rambo"
        };

        private static readonly string[] LastNames = new[]
        {
            "Santos", "Dela Cruz", "Reyes", "Ramos", "Mendoza", "Garcia", "Bautista", "Ocampo", "Fernandez", "Aquino",
            "Tan", "Lim", "Sy", "Go", "Yap", "Villanueva", "Torres", "Morales", "Castillo", "Cruz",
            "Alvarez", "Perez", "Tolentino", "Salazar", "Mercado", "Navarro", "Gomez", "Pascual", "Soriano", "Rivera",
            "Aguilar", "Santiago", "Valdez", "David", "Velasco", "Manalo", "Del Rosario", "Corpuz", "Cabrera", "Domingo"
        };

        private static readonly string[] DavaoStreetAddresses = new[]
        {
            "Blk 12 Lot 5, Ciudad Esperanza, Cabantian",
            "Unit 4B, Abreeza Residences, J.P. Laurel Ave, Bajada",
            "124 Macopa St., Juna Subdivision, Matina",
            "77 Ruby St., Marfori Heights",
            "Phase 3, Ladislawa Village, Buhangin",
            "88 Roxas Ave, Poblacion District",
            "23 Eco West Drive, Ecoland Phase 2",
            "15 Gladiola St., Mintal",
            "Lot 9, Palm Village, Lanang",
            "302 Toril Executive Heights, Toril",
            "45 Dahlia St., Buhangin",
            "18 Mahogany St., Woodridge Park, Maa",
            "Unit 12A, One Oasis Condominiums, Ecoland",
            "56 Mango St., Nova Tierra Village, Lanang",
            "82 Diamond St., Marfori Heights",
            "104 Sampaguita St., Bangkal",
            "Lot 3 Blk 8, Las Terrazas, Maa",
            "215 C.M. Recto St., Poblacion",
            "34 Acacia St., Juna Subdivision, Matina",
            "67 Ilang-Ilang St., Rivera Village, Bajada",
            "Unit 7, Camella Homes, Communal, Buhangin",
            "19 Rosal St., Davao Executive Homes, Matina",
            "508 Villa Josefina Resort Village, Dumoy",
            "73 Orchard Lane, Robinsons Highlands, Buhangin",
            "14 Rosalina Village, Puan",
            "Unit 201, Linmarr Towers, Porras St., Bo. Obrero",
            "88 Damosa Gateway, Lanang",
            "112 Tulip Drive, Matina",
            "41 Santol St., Cabantian",
            "95 Bougainvillea St., Central Park, Bangkal"
        };

        private static readonly string[] CakeFlavors = new[]
        {
            "Ube Macapuno Deluxe",
            "Belgian Dark Chocolate Ganache",
            "Red Velvet Cream Cheese",
            "Mango Bravo Chiffon",
            "Mocha Crunch Caramel",
            "Salted Caramel Vanilla",
            "Strawberry Shortcake",
            "Pandan Coconut Delight",
            "Matcha Green Tea White Chocolate",
            "Classic Carrot Cake with Walnuts",
            "Black Forest Cherry",
            "Biscoff Cookie Butter Chiffon"
        };

        private static readonly (string Size, decimal BasePrice)[] CakeSizes = new[]
        {
            ("4-inch Bento Duo", 950m),
            ("6-inch Petite Round (1 Tier)", 1250m),
            ("8-inch Signature Round (1 Tier)", 1850m),
            ("10-inch Celebration Round (1 Tier)", 2850m),
            ("8-inch Heart Shaped", 2150m),
            ("6-inch + 8-inch Celebration (2 Tier)", 4200m),
            ("8-inch + 10-inch Grand (2 Tier)", 5600m),
            ("6-inch + 8-inch + 10-inch Royal Wedding (3 Tier)", 8500m),
            ("9-inch x 13-inch Quarter Sheet", 3400m)
        };

        private static readonly string[] DesignThemes = new[]
        {
            "Minimalist Pastel Ribbon & Pearls",
            "Enchanted Floral Garden with Gold Leaf",
            "Dinosaur Safari Adventure Birthday",
            "Boho Chic Dried Palms & Macrame",
            "Princess Royal Tiara & Pastel Castle",
            "Modern Palette Knife Textured Buttercream",
            "Vintage Victorian Lambeth Ruffles",
            "Rustic Semi-Naked with Fresh Berries",
            "Silver Jubilee Geometric Elegance",
            "Celestial Galaxy & Constellations",
            "Under the Sea Mermaid Fantasy",
            "Classic Corporate Recognition Ribbon"
        };

        private static readonly string[] CakeShapes = new[]
        {
            "Round", "Heart", "Square", "Tiered", "Hexagon"
        };

        private static readonly string[] ColorThemes = new[]
        {
            "Pastel Pink & Rose Gold",
            "Sage Green & Ivory",
            "Navy Blue & Gold Leaf",
            "Lavender & Lilac Shimmer",
            "Earthy Warm Terracotta & Beige",
            "Classic Monochrome Black & White",
            "Baby Blue & Cloud White",
            "Burgundy & Dusty Rose"
        };

        private static readonly string[] CakeMessages = new[]
        {
            "Happy 18th Birthday Chloe!",
            "Happy 1st Birthday Baby Liam!",
            "Happy 50th Golden Anniversary Mom & Dad!",
            "Congratulations on Passing the Board Exam!",
            "Happy Sweet 16 Sophia!",
            "Happy 60th Birthday Nanay!",
            "Welcome to the World Baby Lucas!",
            "Happy Birthday to the Best Boss Ever!",
            "Forever & Always - Ken & Sarah",
            "Congratulations Class of 2026!",
            "Happy 7th Birthday Superhero Joaquin!",
            "Best Wishes on Your New Journey!"
        };

        private static readonly string[] FollowUpTemplates = new[]
        {
            "Sent updated quotation with fondant floral styling and edible gold leaf options.",
            "Called customer to confirm event venue address and delivery window for reception.",
            "Confirmed 50% reservation downpayment received via GCash; forwarded specs to head baker.",
            "Followed up regarding flavor tasting appointment schedule at the boutique shop.",
            "Post-event customer satisfaction follow-up: customer gave 5-star review on Ube Macapuno cake.",
            "Discussed custom dietary requests: client confirmed low-sugar cream cheese frosting preference.",
            "Sent swatch comparisons for dusty rose and sage green buttercream color theme.",
            "Sent final balance reminder 3 days prior to scheduled pickup.",
            "Followed up on inquiry quotation sent via email last week.",
            "Customer requested to add a set of 12 matching character cupcakes to the existing order."
        };

        private static readonly string[] InquiryTypes = new[]
        {
            "3-Tier Floral Wedding Cake",
            "1st Birthday Fondant Themed Cake",
            "Debutante Chandelier Cake",
            "Company 10th Anniversary Sheet Cake",
            "Christening 2-Tier Pastel Cake",
            "Bridal Shower Heart Cake",
            "Retirement Recognition Celebration Cake"
        };

        private static readonly string[] InquiryStatuses = new[]
        {
            "New", "In Progress", "Quoted", "Confirmed", "Declined"
        };

        /// <summary>
        /// Seeds approximately 200 Customers, 200 SalesOrders, OrderDetails, Customizations,
        /// 200 Payments, 200 Follow-ups, and Inquiries into the tenant database idempotently.
        /// </summary>
        public static async Task<SeedResult> SeedDemoDataAsync(int companyId = 2, bool force = false)
        {
            await using var context = CrmDataService.CreateDbContext(companyId);

            // Ensure database tables and schema baseline exist
            await context.Database.EnsureCreatedAsync();
            await CrmDataService.SeedTenantBaselineAsync(context);

            // Resolve company name and code from Master DB
            string companyName = "Tenant Cake Shop";
            string companyCode = $"TC{companyId}";
            try
            {
                await using var masterCtx = CrmDataService.CreateMasterDbContext();
                var mComp = await masterCtx.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.CompanyId == companyId);
                if (mComp != null)
                {
                    if (!string.IsNullOrWhiteSpace(mComp.CompanyName)) companyName = mComp.CompanyName.Trim();
                    if (!string.IsNullOrWhiteSpace(mComp.CompanyCode)) companyCode = mComp.CompanyCode.Trim();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SeedDemoDataAsync Master DB lookup] {ex.Message}");
            }

            // 1. Idempotency Check: check if demo customers already exist
            int existingDemoCustomers = await context.Customers
                .CountAsync(c => c.CompanyId == companyId && c.Notes != null && c.Notes.Contains(DemoTag));

            if (existingDemoCustomers >= 180 && !force)
            {
                var existingCounts = await GetCurrentCountsAsync(context);
                return new SeedResult
                {
                    Success = true,
                    AlreadySeeded = true,
                    Message = $"Database for '{companyName}' (Company ID {companyId}) already contains {existingDemoCustomers} demo customers (Total: {existingCounts["Customers"]} customers, {existingCounts["SalesOrders"]} orders, {existingCounts["Payments"]} payments). Idempotent seeding skipped.",
                    Counts = existingCounts
                };
            }

            // Ensure company exists in tenant DB
            var company = await context.Companies.FindAsync(companyId);
            if (company == null)
            {
                try
                {
                    await context.Database.ExecuteSqlRawAsync(@"
                        SET IDENTITY_INSERT Companies ON;
                        INSERT INTO Companies (CompanyId, CompanyCode, CompanyName, IsActive, CreatedDate)
                        VALUES ({0}, {1}, {2}, 1, GETUTCDATE());
                        SET IDENTITY_INSERT Companies OFF;
                    ", companyId, companyCode, companyName);
                }
                catch
                {
                    try
                    {
                        company = new Company
                        {
                            CompanyId = companyId,
                            CompanyCode = companyCode,
                            CompanyName = companyName,
                            IsActive = true
                        };
                        context.Companies.Add(company);
                        await context.SaveChangesAsync();
                    }
                    catch { }
                }
            }

            // Ensure branches exist for this tenant
            await CrmDataService.EnsureTenantBranchBaselineAsync(context, companyId);
            var branches = await context.Branches.Where(b => b.CompanyId == companyId && b.IsActive).ToListAsync();
            if (branches.Count < 2)
            {
                try
                {
                    var subCap = await CrmDataService.GetBranchCapabilityAsync(companyId);
                    if (subCap.AllowBranching && subCap.MaxBranches >= 2)
                    {
                        var branch2 = new Branch
                        {
                            CompanyId = companyId,
                            BranchName = $"{companyName} - North Hub",
                            BranchCode = $"{companyCode}-NH",
                            Address = "J.P. Laurel Ave, Bajada, Davao City",
                            ContactPhone = "+63 917 555 0199",
                            ContactEmail = $"north@{companyCode.ToLower().Replace(" ", "").Replace("-", "")}.ph",
                            ManagerName = "Operations Lead",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        };
                        context.Branches.Add(branch2);
                        await context.SaveChangesAsync();
                        branches = await context.Branches.Where(b => b.CompanyId == companyId && b.IsActive).ToListAsync();
                    }
                }
                catch { }
            }
            var branchIds = branches.Select(b => b.BranchId).ToList();

            // Ensure staff and admin users exist for this tenant
            var existingUsers = await context.AppUsers.Where(u => u.CompanyId == companyId).ToListAsync();
            if (existingUsers.Count < 3)
            {
                int? defBranchId = branchIds.Count > 0 ? branchIds[0] : (int?)null;
                int? secondBranchId = branchIds.Count > 1 ? branchIds[1] : defBranchId;
                string baseUser = companyCode.ToLower().Replace(" ", "").Replace("-", "");

                var usersToSeed = new List<SystemUser>
                {
                    new SystemUser
                    {
                        CompanyId = companyId,
                        BranchId = defBranchId,
                        RoleId = 2, // Business Admin
                        Username = $"{baseUser}.admin",
                        Email = $"admin@{baseUser}.ph",
                        FirstName = companyName.Split(' ').FirstOrDefault() ?? "Admin",
                        LastName = "Admin",
                        Phone = "+63 917 111 8899",
                        IsActive = true,
                        PasswordHash = "admin123",
                        CreatedDate = DateTime.UtcNow.AddDays(-90),
                        LastLoginDate = DateTime.UtcNow
                    },
                    new SystemUser
                    {
                        CompanyId = companyId,
                        BranchId = defBranchId,
                        RoleId = 3, // Manager
                        Username = $"{baseUser}.manager",
                        Email = $"manager@{baseUser}.ph",
                        FirstName = "Operations",
                        LastName = "Manager",
                        Phone = "+63 918 222 8899",
                        IsActive = true,
                        PasswordHash = "admin123",
                        CreatedDate = DateTime.UtcNow.AddDays(-80),
                        LastLoginDate = DateTime.UtcNow
                    },
                    new SystemUser
                    {
                        CompanyId = companyId,
                        BranchId = secondBranchId,
                        RoleId = 4, // Staff
                        Username = $"{baseUser}.staff",
                        Email = $"staff@{baseUser}.ph",
                        FirstName = "Baking",
                        LastName = "Staff",
                        Phone = "+63 919 333 8899",
                        IsActive = true,
                        PasswordHash = "admin123",
                        CreatedDate = DateTime.UtcNow.AddDays(-70),
                        LastLoginDate = DateTime.UtcNow
                    }
                };

                foreach (var u in usersToSeed)
                {
                    if (!existingUsers.Any(eu => eu.Username.ToLower() == u.Username.ToLower() || eu.Email.ToLower() == u.Email.ToLower()))
                    {
                        context.AppUsers.Add(u);
                    }
                }
                await context.SaveChangesAsync();
            }

            // Fetch available staff/admin user IDs for realistic assignment
            var userIds = await context.AppUsers
                .Where(u => u.CompanyId == companyId)
                .Select(u => u.UserId)
                .ToListAsync();

            if (!userIds.Any())
            {
                userIds = new List<int> { CrmDataService.DefaultUserId };
            }

            var staffUserIds = await context.AppUsers
                .Where(u => u.CompanyId == companyId && u.RoleId == 4)
                .Select(u => u.UserId)
                .ToListAsync();

            if (!staffUserIds.Any()) staffUserIds = userIds;

            var staffUsers = await context.AppUsers
                .Where(u => userIds.Contains(u.UserId))
                .Select(u => new { u.UserId, Name = u.FirstName + " " + u.LastName })
                .ToListAsync();

            var rnd = new Random(42 + companyId); // Deterministic seed per company for reproducible data distribution
            var baseDate = DateTime.Today;

            // 2. Create Addresses (30 realistic Davao addresses)
            var existingAddresses = await context.Addresses.ToListAsync();
            var addresses = new List<Address>(existingAddresses);
            if (addresses.Count < 30)
            {
                for (int i = addresses.Count; i < DavaoStreetAddresses.Length; i++)
                {
                    var addr = new Address
                    {
                        AddressLine1 = DavaoStreetAddresses[i],
                        AddressLine2 = i % 3 == 0 ? $"Floor {rnd.Next(1, 10)}" : null,
                        City = "Davao City",
                        State = "Davao del Sur",
                        PostalCode = "8000",
                        Country = "Philippines"
                    };
                    context.Addresses.Add(addr);
                    addresses.Add(addr);
                }
                await context.SaveChangesAsync();
            }

            // 3. Create 200 Customers
            var customers = new List<Customer>();
            int custIdx = 1;
            for (int f = 0; f < FirstNames.Length; f++)
            {
                for (int l = 0; l < LastNames.Length; l++)
                {
                    if (customers.Count >= 200) break;

                    string fn = FirstNames[(f + custIdx) % FirstNames.Length];
                    string ln = LastNames[(l + custIdx * 2) % LastNames.Length];
                    string email = $"{fn.ToLower()}.{ln.ToLower()}{custIdx}@{companyCode.ToLower().Replace(" ", "").Replace("-", "")}.ph";

                    // Spread registration across past 90 days
                    int daysAgo = rnd.Next(0, 91);
                    var regDate = baseDate.AddDays(-daysAgo).AddHours(rnd.Next(8, 18)).AddMinutes(rnd.Next(0, 60));
                    int assignedUserId = staffUserIds[rnd.Next(staffUserIds.Count)];
                    var addr = addresses[rnd.Next(addresses.Count)];
                    int? assignedBranchId = branchIds.Count > 0 ? branchIds[rnd.Next(branchIds.Count)] : (int?)null;

                    var cust = new Customer
                    {
                        CompanyId = companyId,
                        BranchId = assignedBranchId,
                        FirstName = fn,
                        LastName = ln,
                        Email = email,
                        Phone = $"0917{rnd.Next(1000000, 9999999)}",
                        AddressId = addr.AddressId,
                        AddressText = addr.AddressLine1 + ", Davao City",
                        RegisteredDate = regDate,
                        CreatedByUserId = assignedUserId,
                        CakePreferences = $"{CakeFlavors[rnd.Next(CakeFlavors.Length)]}; {ColorThemes[rnd.Next(ColorThemes.Length)]}",
                        Notes = $"Regular client. {DemoTag}"
                    };

                    customers.Add(cust);
                    custIdx++;
                }
            }

            context.Customers.AddRange(customers);
            await context.SaveChangesAsync();

            // 4. Create 200 SalesOrders with realistic distribution across customers
            // Repeat customer logic: 5 VIP customers get 4 orders, 10 customers get 3 orders, 25 get 2 orders, remainder get 1 order
            var customerOrderTargets = new List<(Customer Customer, int OrderCount)>();
            int orderAcc = 0;
            for (int i = 0; i < customers.Count; i++)
            {
                int count = 1;
                if (i < 5) count = 4;        // Top 5 VIPs
                else if (i < 15) count = 3;  // Top 10 repeat
                else if (i < 40) count = 2;  // 25 repeat
                else count = 1;

                if (orderAcc + count > 200)
                {
                    count = Math.Max(0, 200 - orderAcc);
                }
                customerOrderTargets.Add((customers[i], count));
                orderAcc += count;
                if (orderAcc >= 200) break;
            }

            // Fill remaining if needed to reach exactly 200
            while (orderAcc < 200)
            {
                var target = customerOrderTargets[rnd.Next(customerOrderTargets.Count)];
                var idx = customerOrderTargets.IndexOf(target);
                customerOrderTargets[idx] = (target.Customer, target.OrderCount + 1);
                orderAcc++;
            }

            int orderNumber = 100;
            var createdOrders = new List<SalesOrder>();

            foreach (var (customer, targetCount) in customerOrderTargets)
            {
                for (int c = 0; c < targetCount; c++)
                {
                    orderNumber++;
                    int daysAgo = rnd.Next(0, 91);
                    var orderDate = baseDate.AddDays(-daysAgo).AddHours(rnd.Next(9, 17)).AddMinutes(rnd.Next(0, 59));
                    int deliveryLeadDays = rnd.Next(2, 6);
                    var deliveryDate = orderDate.AddDays(deliveryLeadDays);

                    // Realistic Status based on age of order:
                    // Recent (0-2 days ago): Pending (0) or Confirmed (1)
                    // Mid (2-5 days ago): Processing (2) or Ready (4)
                    // Older (5-90 days ago): Completed (3) [90%] or Cancelled (5) [10%]
                    int statusId;
                    if (daysAgo <= 2)
                    {
                        statusId = rnd.Next(2) == 0 ? 0 : 1; // Pending or Confirmed
                    }
                    else if (daysAgo <= 6)
                    {
                        statusId = rnd.Next(2) == 0 ? 2 : 4; // Processing or Ready
                    }
                    else
                    {
                        statusId = rnd.Next(10) == 0 ? 5 : 3; // 10% Cancelled, 90% Completed
                    }

                    var (cakeSize, basePrice) = CakeSizes[rnd.Next(CakeSizes.Length)];
                    string flavor = CakeFlavors[rnd.Next(CakeFlavors.Length)];
                    string theme = DesignThemes[rnd.Next(DesignThemes.Length)];
                    decimal totalAmount = basePrice + (rnd.Next(0, 4) * 250m);

                    int? assignedBranchId = customer.BranchId ?? (branchIds.Count > 0 ? branchIds[rnd.Next(branchIds.Count)] : (int?)null);

                    var order = new SalesOrder
                    {
                        CustomerId = customer.CustomerId,
                        CreatedByUserId = customer.CreatedByUserId,
                        DeliveryAddressId = customer.AddressId,
                        BranchId = assignedBranchId,
                        StatusId = statusId,
                        OrderDate = orderDate,
                        DeliveryDate = deliveryDate,
                        CakeSize = cakeSize,
                        Flavor = flavor,
                        DesignTheme = theme,
                        Notes = $"Order #{orderNumber:D4} for {customer.FirstName}. {DemoTag}",
                        TotalAmount = totalAmount
                    };

                    createdOrders.Add(order);
                }
            }

            context.SalesOrders.AddRange(createdOrders);
            await context.SaveChangesAsync();

            // 5. Create SalesOrderDetails & CakeCustomizations for each order
            foreach (var order in createdOrders)
            {
                var detail = new SalesOrderDetail
                {
                    OrderId = order.OrderId,
                    ItemDescription = $"{order.CakeSize} - {order.Flavor}",
                    Quantity = 1,
                    UnitPrice = order.TotalAmount
                };
                context.SalesOrderDetails.Add(detail);
                await context.SaveChangesAsync(); // Save detail to generate OrderDetailId

                var customization = new CakeCustomization
                {
                    OrderDetailId = detail.OrderDetailId,
                    Flavor = order.Flavor,
                    Shape = CakeShapes[rnd.Next(CakeShapes.Length)],
                    ColorTheme = ColorThemes[rnd.Next(ColorThemes.Length)],
                    MessageOnCake = CakeMessages[rnd.Next(CakeMessages.Length)],
                    ReferenceImageUrl = null
                };
                context.CakeCustomizations.Add(customization);
            }
            await context.SaveChangesAsync();

            // 6. Create ~200 Payments mapped to orders
            var payments = new List<Payment>();
            int payIdx = 1000;
            foreach (var order in createdOrders)
            {
                payIdx++;
                int methodId = rnd.Next(0, 4); // 0: Cash, 1: GCash, 2: Bank Transfer, 3: Credit Card
                string prefix = methodId switch
                {
                    0 => "CASH-OR",
                    1 => "GCASH",
                    2 => "BDO-TXN",
                    _ => "CC-AUTH"
                };
                string refNo = $"{prefix}-2026-{payIdx:D5}";

                int paymentStatusId;
                decimal paymentAmount;

                switch (order.StatusId)
                {
                    case 3: // Completed
                    case 4: // Ready
                        paymentStatusId = 1; // Completed
                        paymentAmount = order.TotalAmount;
                        break;
                    case 1: // Confirmed
                    case 2: // Processing
                        paymentStatusId = 1; // Completed
                        paymentAmount = Math.Round(order.TotalAmount * (rnd.Next(2) == 0 ? 0.5m : 1.0m), 2);
                        break;
                    case 0: // Pending
                        paymentStatusId = rnd.Next(2) == 0 ? 0 : 1; // Pending or Downpayment completed
                        paymentAmount = paymentStatusId == 0 ? order.TotalAmount : Math.Round(order.TotalAmount * 0.5m, 2);
                        break;
                    case 5: // Cancelled
                    default:
                        paymentStatusId = rnd.Next(2) == 0 ? 3 : 2; // Refunded or Failed
                        paymentAmount = order.TotalAmount;
                        break;
                }

                var paymentDate = order.OrderDate.AddHours(rnd.Next(1, 24));
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    MethodId = methodId,
                    StatusId = paymentStatusId,
                    ProcessedByUserId = order.CreatedByUserId,
                    Amount = paymentAmount,
                    PaymentDate = paymentDate,
                    TransactionReference = refNo,
                    BranchId = order.BranchId
                };

                payments.Add(payment);
            }

            context.Payments.AddRange(payments);
            await context.SaveChangesAsync();

            // 7. Create 200 CustomerFollowUps across customers
            var followUps = new List<CustomerFollowUp>();
            for (int i = 0; i < 200; i++)
            {
                var cust = customers[i % customers.Count];
                int staffId = staffUserIds[rnd.Next(staffUserIds.Count)];
                int daysAgo = rnd.Next(0, 91);
                var fDate = baseDate.AddDays(-daysAgo).AddHours(rnd.Next(9, 17));

                int statusId;
                int roll = rnd.Next(100);
                if (roll < 20) statusId = 0; // Pending
                else if (roll < 30) statusId = 3; // Overdue
                else if (roll < 35) statusId = 2; // Cancelled
                else statusId = 1; // Completed

                DateTime? nextDate = statusId == 0 || statusId == 3
                    ? baseDate.AddDays(rnd.Next(1, 7))
                    : null;

                var fUp = new CustomerFollowUp
                {
                    CustomerId = cust.CustomerId,
                    StaffUserId = staffId,
                    StatusId = statusId,
                    FollowUpDate = fDate,
                    Notes = $"{FollowUpTemplates[rnd.Next(FollowUpTemplates.Length)]} {DemoTag}",
                    NextFollowUpDate = nextDate,
                    BranchId = cust.BranchId
                };

                followUps.Add(fUp);
            }

            context.CustomerFollowUps.AddRange(followUps);
            await context.SaveChangesAsync();

            // 8. Create ~75 CustomerInquiries
            var inquiries = new List<CustomerInquiry>();
            for (int i = 1; i <= 75; i++)
            {
                var cust = customers[rnd.Next(customers.Count)];
                var staff = staffUsers[rnd.Next(staffUsers.Count)];
                int daysAgo = rnd.Next(0, 60);
                var createdDate = baseDate.AddDays(-daysAgo).AddHours(rnd.Next(8, 17));
                var eventDate = baseDate.AddDays(rnd.Next(3, 45));

                string status = InquiryStatuses[rnd.Next(InquiryStatuses.Length)];
                decimal budget = rnd.Next(15, 95) * 100m;

                var inq = new CustomerInquiry
                {
                    InquiryCode = $"INQ-2026-{i:D4}",
                    CustomerId = cust.CustomerId,
                    CakeType = InquiryTypes[rnd.Next(InquiryTypes.Length)],
                    EventDate = eventDate,
                    AssignedTo = staff.Name,
                    EstimatedBudget = budget,
                    Status = status,
                    Notes = $"Client inquiry for upcoming celebration. {DemoTag}",
                    CreatedAt = createdDate,
                    BranchId = cust.BranchId
                };

                inquiries.Add(inq);
            }

            context.CustomerInquiries.AddRange(inquiries);
            await context.SaveChangesAsync();

            // 9. Add ReportLogs and SystemAuditLogs
            var reportLogs = new List<ReportLog>
            {
                new ReportLog { GeneratedByUserId = userIds[0], ReportType = "Monthly Revenue Summary", GeneratedDate = baseDate.AddDays(-5) },
                new ReportLog { GeneratedByUserId = userIds[0], ReportType = "Customer Order Frequency Analytics", GeneratedDate = baseDate.AddDays(-12) },
                new ReportLog { GeneratedByUserId = userIds[0], ReportType = "Payment Gateway Reconciliation", GeneratedDate = baseDate.AddDays(-20) },
                new ReportLog { GeneratedByUserId = userIds[0], ReportType = "Weekly Operational Cake Orders Pipeline", GeneratedDate = baseDate.AddDays(-2) }
            };
            context.ReportLogs.AddRange(reportLogs);

            var auditLogs = new List<SystemAuditLog>
            {
                new SystemAuditLog { UserId = userIds[0], ActionType = "Data Seeding", ActionDescription = "Generated 200 realistic demo records for BI dashboards", Timestamp = DateTime.UtcNow, IPAddress = "127.0.0.1" },
                new SystemAuditLog { UserId = userIds[0], ActionType = "Report Export", ActionDescription = "Exported Monthly Sales Report to CSV", Timestamp = baseDate.AddDays(-5).AddHours(14), IPAddress = "192.168.1.102" },
                new SystemAuditLog { UserId = userIds[0], ActionType = "Order Status Update", ActionDescription = "Batch updated orders to Completed status", Timestamp = baseDate.AddDays(-1).AddHours(17), IPAddress = "192.168.1.105" }
            };
            context.SystemAuditLogs.AddRange(auditLogs);
            await context.SaveChangesAsync();

            var finalCounts = await GetCurrentCountsAsync(context);

            return new SeedResult
            {
                Success = true,
                AlreadySeeded = false,
                Message = $"Successfully seeded realistic demo data into '{companyName}' (Company ID: {companyId}): {customers.Count} Customers, {createdOrders.Count} SalesOrders, {createdOrders.Count} Customizations, {payments.Count} Payments, {followUps.Count} Follow-ups, {inquiries.Count} Inquiries.",
                Counts = finalCounts
            };
        }

        /// <summary>
        /// Seeds 200 demo entries into all active tenant companies in the Master database.
        /// </summary>
        public static async Task<List<SeedResult>> SeedAllTenantsDemoDataAsync(bool force = false)
        {
            var results = new List<SeedResult>();
            try
            {
                await using var masterContext = CrmDataService.CreateMasterDbContext();
                var companies = await masterContext.Companies
                    .AsNoTracking()
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.CompanyId)
                    .ToListAsync();

                foreach (var company in companies)
                {
                    try
                    {
                        var res = await SeedDemoDataAsync(company.CompanyId, force);
                        results.Add(res);
                        Console.WriteLine($"[SEEDING] {company.CompanyName} (ID: {company.CompanyId}): {res.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SEEDING ERROR] Company '{company.CompanyName}' (ID: {company.CompanyId}): {ex.Message}");
                        results.Add(new SeedResult
                        {
                            Success = false,
                            Message = $"Error seeding {company.CompanyName} (ID {company.CompanyId}): {ex.Message}"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SEED ALL ERROR] {ex.Message}");
            }

            return results;
        }

        public static async Task<Dictionary<string, int>> GetCurrentCountsAsync(CrmDbContext context)
        {
            return new Dictionary<string, int>
            {
                ["Customers"] = await context.Customers.CountAsync(),
                ["SalesOrders"] = await context.SalesOrders.CountAsync(),
                ["SalesOrderDetails"] = await context.SalesOrderDetails.CountAsync(),
                ["CakeCustomizations"] = await context.CakeCustomizations.CountAsync(),
                ["Payments"] = await context.Payments.CountAsync(),
                ["CustomerFollowUps"] = await context.CustomerFollowUps.CountAsync(),
                ["CustomerInquiries"] = await context.CustomerInquiries.CountAsync(),
                ["Addresses"] = await context.Addresses.CountAsync(),
                ["ReportLogs"] = await context.ReportLogs.CountAsync(),
                ["SystemAuditLogs"] = await context.SystemAuditLogs.CountAsync()
            };
        }

        public static async Task PrintDatabaseCountsAsync(int companyId = 2)
        {
            string compName = await CrmDataService.GetCompanyNameAsync(companyId) ?? $"Company {companyId}";
            await using var context = CrmDataService.CreateDbContext(companyId);
            var counts = await GetCurrentCountsAsync(context);
            Console.WriteLine("==================================================");
            Console.WriteLine($" DATABASE COUNTS FOR: {compName.ToUpperInvariant()} (ID: {companyId})");
            Console.WriteLine("==================================================");
            foreach (var kvp in counts)
            {
                Console.WriteLine($" {kvp.Key.PadRight(22)}: {kvp.Value,6} records");
            }
            Console.WriteLine("==================================================");

            var metrics = await CrmDataService.GetDashboardMetricsAsync(companyId);
            Console.WriteLine(" DASHBOARD LIVE METRICS:");
            Console.WriteLine($" Open Inquiries        : {metrics.OpenInquiriesCount}");
            Console.WriteLine($" Orders In Progress    : {metrics.OrdersInProgressCount}");
            Console.WriteLine($" Processing Orders     : {metrics.ProcessingOrdersCount}");
            Console.WriteLine($" Ready For Pickup      : {metrics.ReadyForPickupCount}");
            Console.WriteLine($" Follow-ups Due        : {metrics.FollowupsDueCount}");
            Console.WriteLine($" Overdue Follow-ups    : {metrics.OverdueFollowupsCount}");
            Console.WriteLine($" Total Customers       : {metrics.TotalCustomersCount}");
            Console.WriteLine($" Total Revenue (PHP)   : PHP {metrics.TotalRevenue:N2}");
            Console.WriteLine("==================================================");
        }

        public static async Task PrintAllDatabaseCountsAsync()
        {
            try
            {
                await using var masterContext = CrmDataService.CreateMasterDbContext();
                var companies = await masterContext.Companies
                    .AsNoTracking()
                    .OrderBy(c => c.CompanyId)
                    .ToListAsync();

                foreach (var company in companies)
                {
                    try
                    {
                        await PrintDatabaseCountsAsync(company.CompanyId);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Could not load counts for '{company.CompanyName}' (ID {company.CompanyId}): {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PRINT ALL COUNTS ERROR] {ex.Message}");
            }
        }
    }
}
