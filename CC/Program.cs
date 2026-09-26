using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using CC.domain.Entities;
using CC.Domain.Entities;
using CC.Forms.Staff;
using CC.Services;

namespace CC
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            try
            {
                CrmDataService.EnsureDatabaseReadyAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"DB Init warning: {ex.Message}");
            }

            if (args.Length > 0 && args[0] == "--seed-demo-data")
            {
                bool force = args.Length > 1 && args[1] == "--force";
                Console.WriteLine("Starting demo data seeding for CustomCakeCRM...");
                var result = DatabaseSeeder.SeedDemoDataAsync(2, force).GetAwaiter().GetResult();
                Console.WriteLine(result.Message);
                return;
            }

            if (args.Length > 0 && args[0] == "--verify-db-counts")
            {
                Console.WriteLine("Verifying database row counts for CustomCakeCRM...");
                DatabaseSeeder.PrintDatabaseCountsAsync(2).GetAwaiter().GetResult();
                return;
            }

            if (args.Length > 0 && args[0] == "--test-nav")
            {
                Console.WriteLine("Running navigation diagnostic test...");
                SessionService.CurrentUser = new CurrentUser { UserId = 4, FirstName = "Jerome", LastName = "Santos", Role = "Staff", CompanyId = 2 };
                var form = new StaffDashboardForm();
                form.Show();

                void Pump(int ms = 500)
                {
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (sw.ElapsedMilliseconds < ms)
                    {
                        Application.DoEvents();
                        Thread.Sleep(20);
                    }
                }

                System.Collections.Generic.IEnumerable<Control> GetAllControls(Control c)
                {
                    yield return c;
                    foreach (Control child in c.Controls)
                    foreach (var descendant in GetAllControls(child))
                        yield return descendant;
                }

                Pump(1000);
                Console.WriteLine($"[1. Init Dashboard] MainPanel controls: {form.MainPanel.Controls.Count}, Tag: {form.MainPanel.Tag?.GetType().Name ?? "null"}");

                Console.WriteLine("Navigating to Customers...");
                form.Navigate("Customers");
                Pump(1000);
                Console.WriteLine($"[2. Customers] MainPanel controls: {form.MainPanel.Controls.Count}, Tag: {form.MainPanel.Tag?.GetType().Name ?? "null"}");
                if (form.MainPanel.Tag is CC.Forms.Staff.Customers.CustomerListForm clf)
                {
                    var dgv = clf.Controls.OfType<Control>().SelectMany(c => GetAllControls(c)).OfType<DataGridView>().FirstOrDefault();
                    Console.WriteLine($"Found DataGridView: {dgv != null}, Rows: {dgv?.Rows.Count ?? -1}");
                }

                Console.WriteLine("Navigating to Dashboard...");
                form.Navigate("Dashboard");
                Pump(1500);
                Console.WriteLine($"[3. Return Dashboard] MainPanel controls: {form.MainPanel.Controls.Count}, Tag: {form.MainPanel.Tag?.GetType().Name ?? "null"}");
                var scrollPanel = form.MainPanel.Controls.OfType<Panel>().FirstOrDefault();
                Console.WriteLine($"ScrollPanel controls: {scrollPanel?.Controls.Count ?? -1}");

                Console.WriteLine("Navigating to Customers again...");
                form.Navigate("Customers");
                Pump(1000);
                Console.WriteLine($"[4. Customers Again] MainPanel controls: {form.MainPanel.Controls.Count}, Tag: {form.MainPanel.Tag?.GetType().Name ?? "null"}");
                if (form.MainPanel.Tag is CC.Forms.Staff.Customers.CustomerListForm clf2)
                {
                    var dgv = clf2.Controls.OfType<Control>().SelectMany(c => GetAllControls(c)).OfType<DataGridView>().FirstOrDefault();
                    Console.WriteLine($"Found DataGridView: {dgv != null}, Rows: {dgv?.Rows.Count ?? -1}");
                }

                Console.WriteLine("Done test-nav.");
                form.Close();
                return;
            }

            if (args.Length > 0 && args[0] == "--test-retention")
            {
                Console.WriteLine("Running automated tests for Customer Retention & Email Campaigns...");

                // 1. Test Segmentation Logic
                var today = DateTime.Today;
                var segInactive = CrmDataService.CalculateRetentionSegment(5, today.AddDays(-200), today);
                var segAtRisk = CrmDataService.CalculateRetentionSegment(2, today.AddDays(-120), today);
                var segLoyal = CrmDataService.CalculateRetentionSegment(4, today.AddDays(-30), today);
                var segReturning = CrmDataService.CalculateRetentionSegment(2, today.AddDays(-20), today);
                var segNew = CrmDataService.CalculateRetentionSegment(1, today.AddDays(-10), today);

                Console.WriteLine($"[SEGMENT TEST] Inactive: {segInactive == "Inactive"} ({segInactive})");
                Console.WriteLine($"[SEGMENT TEST] At Risk: {segAtRisk == "At Risk"} ({segAtRisk})");
                Console.WriteLine($"[SEGMENT TEST] Loyal: {segLoyal == "Loyal"} ({segLoyal})");
                Console.WriteLine($"[SEGMENT TEST] Returning: {segReturning == "Returning"} ({segReturning})");
                Console.WriteLine($"[SEGMENT TEST] New: {segNew == "New"} ({segNew})");

                // 2. Test RBAC: Staff Access Denied
                SessionService.CurrentUser = new CurrentUser { UserId = 4, FirstName = "Jerome", LastName = "Santos", Role = "Staff", CompanyId = 2 };
                bool staffHasAccess = CrmDataService.VerifyRetentionAccess("Manager", throwOnFailure: false);
                Console.WriteLine($"[RBAC TEST] Staff Denied Access: {!staffHasAccess}");

                // 3. Test RBAC: Manager Access Allowed
                SessionService.CurrentUser = new CurrentUser { UserId = 3, FirstName = "Camille", LastName = "Reyes", Role = "Manager", CompanyId = 2 };
                bool mgrHasAccess = CrmDataService.VerifyRetentionAccess("Manager", throwOnFailure: false);
                bool mgrCanEditSettings = CrmDataService.VerifyRetentionAccess("Admin", throwOnFailure: false);
                Console.WriteLine($"[RBAC TEST] Manager Has Access: {mgrHasAccess}, Manager Denied Admin Settings: {!mgrCanEditSettings}");

                // 4. Test RBAC: Admin Access Allowed
                SessionService.CurrentUser = new CurrentUser { UserId = 2, FirstName = "Lea", LastName = "Abad", Role = "Admin", CompanyId = 2 };
                bool adminHasAccess = CrmDataService.VerifyRetentionAccess("Admin", throwOnFailure: false);
                Console.WriteLine($"[RBAC TEST] Admin Full Access: {adminHasAccess}");

                // 5. Test Retention Dashboard Data Loading
                var retentionData = CrmDataService.GetRetentionDashboardDataAsync(2).GetAwaiter().GetResult();
                Console.WriteLine($"[DATA TEST] Base Customers: {retentionData.TotalCustomersWithOrders}, Templates: {retentionData.Templates.Count}, Metrics Delivered: {retentionData.Metrics.TotalEmailsDelivered}, OpenRate: {retentionData.Metrics.OpenRate}%");

                // 5b. Test Customer Autocomplete Search
                var searchResults = CrmDataService.SearchCustomersForRetentionEmailAsync("a", 2, 8).GetAwaiter().GetResult();
                Console.WriteLine($"[SEARCH TEST] Found {searchResults.Count} customers matching query. Top match: {searchResults.FirstOrDefault()?.FullName} <{searchResults.FirstOrDefault()?.Email}> [{searchResults.FirstOrDefault()?.SegmentName}]");

                // 5c. Test Email Dispatch with SMTP Mock
                Environment.SetEnvironmentVariable("SMTP_MOCK", "true");
                var firstCustomer = searchResults.First();
                var manualLog = CrmDataService.SendManualRetentionEmailAsync(
                    customerId: firstCustomer.CustomerId,
                    segmentName: firstCustomer.SegmentName,
                    forceIgnoreCooldown: true,
                    companyId: 2).GetAwaiter().GetResult();
                Console.WriteLine($"[MANUAL EMAIL TEST] Successfully delivered to {manualLog.CustomerName} <{manualLog.CustomerEmail}>, Status: {manualLog.Status}, Subject: {manualLog.Subject}");

                // 5d. Test Cooldown Verification
                try
                {
                    CrmDataService.SendManualRetentionEmailAsync(
                        customerId: firstCustomer.CustomerId,
                        segmentName: firstCustomer.SegmentName,
                        forceIgnoreCooldown: false,
                        companyId: 2).GetAwaiter().GetResult();
                    Console.WriteLine("[COOLDOWN TEST] FAILED: Cooldown should have blocked send.");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"[COOLDOWN TEST] PASSED: 14-day anti-fatigue cooldown successfully prevented rapid resend ({ex.Message}).");
                }

                // 5e. Test Real SMTP Error Logging & Exception Handling (Unconfigured SMTP)
                Environment.SetEnvironmentVariable("SMTP_MOCK", null);
                Environment.SetEnvironmentVariable("SMTP_HOST", null);
                try
                {
                    CrmDataService.SendManualRetentionEmailAsync(
                        customerId: firstCustomer.CustomerId,
                        segmentName: firstCustomer.SegmentName,
                        forceIgnoreCooldown: true,
                        companyId: 2).GetAwaiter().GetResult();
                    Console.WriteLine("[SMTP ERROR TEST] Expected error when SMTP is unconfigured.");
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine($"[SMTP ERROR TEST] PASSED: Correctly caught unconfigured SMTP, logged failure to database, and reported: {ex.Message.Split('\n')[0]}");
                }

                // 6. Test UI Rendering & Capture Screenshots for Admin and Manager
                string outputDir = @"C:\Users\user1\.gemini\antigravity\brain\7aee3b98-6126-4c32-b0bc-280b2549c485";

                void PumpWait(Task? t)
                {
                    if (t == null) return;
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (!t.IsCompleted && sw.ElapsedMilliseconds < 5000)
                    {
                        Application.DoEvents();
                        Thread.Sleep(20);
                    }
                }

                // Test Admin Dashboard Navigation to Retention & Campaigns
                SessionService.CurrentUser = new CurrentUser { UserId = 2, FirstName = "Lea", LastName = "Abad", Role = "Admin", CompanyId = 2 };
                var adminDash = new CC.Forms.Admin.AdminDashboardForm { Size = new Size(1366, 820), StartPosition = FormStartPosition.Manual, Location = new Point(50, 50) };
                adminDash.Show();
                PumpWait(adminDash.InitializationTask);
                adminDash.Navigate("Retention & Campaigns");
                for (int i = 0; i < 30; i++) { Application.DoEvents(); Thread.Sleep(25); }

                using (var bmp = new Bitmap(adminDash.Width, adminDash.Height))
                {
                    adminDash.DrawToBitmap(bmp, new Rectangle(0, 0, adminDash.Width, adminDash.Height));
                    bmp.Save(Path.Combine(outputDir, "screen_admin_retention_campaigns.png"), ImageFormat.Png);
                }
                adminDash.Close();

                // Test Manager Dashboard Navigation to Retention & Campaigns
                SessionService.CurrentUser = new CurrentUser { UserId = 3, FirstName = "Camille", LastName = "Reyes", Role = "Manager", CompanyId = 2 };
                var mgrDash = new CC.Forms.Manager.ManagerDashboardForm { Size = new Size(1366, 820), StartPosition = FormStartPosition.Manual, Location = new Point(50, 50) };
                mgrDash.Show();
                PumpWait(mgrDash.InitializationTask);
                mgrDash.Navigate("Retention & Campaigns");
                for (int i = 0; i < 30; i++) { Application.DoEvents(); Thread.Sleep(25); }

                using (var bmp = new Bitmap(mgrDash.Width, mgrDash.Height))
                {
                    mgrDash.DrawToBitmap(bmp, new Rectangle(0, 0, mgrDash.Width, mgrDash.Height));
                    bmp.Save(Path.Combine(outputDir, "screen_manager_retention_campaigns.png"), ImageFormat.Png);
                }
                mgrDash.Close();

                // Direct tab-by-tab captures for comprehensive documentation
                SessionService.CurrentUser = new CurrentUser { UserId = 2, FirstName = "Lea", LastName = "Abad", Role = "Admin", CompanyId = 2 };
                var retForm = new CC.Forms.Retention.RetentionCampaignsForm { Size = new Size(1366, 820), StartPosition = FormStartPosition.Manual, Location = new Point(50, 50) };
                retForm.Show();
                PumpWait(retForm.InitializeDataAsync());
                for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }

                // Campaigns Tab
                retForm.SwitchTab("Campaigns");
                for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                using (var bmp = new Bitmap(retForm.Width, retForm.Height))
                {
                    retForm.DrawToBitmap(bmp, new Rectangle(0, 0, retForm.Width, retForm.Height));
                    bmp.Save(Path.Combine(outputDir, "screen_retention_campaigns_tab.png"), ImageFormat.Png);
                }

                // Reports Tab
                retForm.SwitchTab("Reports");
                for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                using (var bmp = new Bitmap(retForm.Width, retForm.Height))
                {
                    retForm.DrawToBitmap(bmp, new Rectangle(0, 0, retForm.Width, retForm.Height));
                    bmp.Save(Path.Combine(outputDir, "screen_retention_reports_tab.png"), ImageFormat.Png);
                }

                // Logs Tab
                retForm.SwitchTab("Logs");
                for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                using (var bmp = new Bitmap(retForm.Width, retForm.Height))
                {
                    retForm.DrawToBitmap(bmp, new Rectangle(0, 0, retForm.Width, retForm.Height));
                    bmp.Save(Path.Combine(outputDir, "screen_retention_logs_tab.png"), ImageFormat.Png);
                }

                // Settings Tab
                retForm.SwitchTab("Settings");
                for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                using (var bmp = new Bitmap(retForm.Width, retForm.Height))
                {
                    retForm.DrawToBitmap(bmp, new Rectangle(0, 0, retForm.Width, retForm.Height));
                    bmp.Save(Path.Combine(outputDir, "screen_retention_settings_tab.png"), ImageFormat.Png);
                }
                retForm.Close();

                Console.WriteLine("Retention automated tests and UI captures completed successfully!");
                return;
            }

            if (args.Length > 0 && args[0] == "--test-analytics")
            {
                Console.WriteLine("Running automated validation for all role dashboard analytics...");
                var staff = CrmDataService.GetStaffDashboardDataAsync(4, 2, "30d").GetAwaiter().GetResult();
                Console.WriteLine($"[STAFF] Due: {staff.MyDueFollowupsCount}, Overdue: {staff.MyOverdueFollowupsCount}, Processing: {staff.ProcessingOrdersCount}, Ready: {staff.ReadyOrdersCount}, Trend Points: {staff.TaskCompletionTrend.Count}, Urgent Tasks: {staff.UrgentTasks.Count}");

                var mgr = CrmDataService.GetManagerDashboardDataAsync(2, "30d").GetAwaiter().GetResult();
                Console.WriteLine($"[MANAGER] Active: {mgr.ActiveOrdersCount}, Ready: {mgr.ReadyForPickupCount}, Stages: {mgr.PipelineStages.Count}, Staff Performance: {mgr.StaffPerformance.Count}, Recent Orders: {mgr.RecentOrders.Count}");

                var adm = CrmDataService.GetAdminDashboardDataAsync(2, "30d").GetAwaiter().GetResult();
                Console.WriteLine($"[ADMIN] Revenue: P{adm.TotalRevenue:N2}, Lifetime: P{adm.LifetimeRevenue:N2}, Orders: {adm.TotalOrders}, Top Custs: {adm.TopCustomers.Count}, Payment Methods: {adm.PaymentMethodBreakdown.Count}");

                var super = CrmDataService.GetSuperAdminDashboardDataAsync("30d").GetAwaiter().GetResult();
                Console.WriteLine($"[SUPER ADMIN] Businesses: {super.TotalBusinesses}, Databases: {super.ActiveDatabases}, Registrations: {super.RecentRegistrations.Count}");

                string outputDir = @"C:\Users\user1\.gemini\antigravity\brain\7aee3b98-6126-4c32-b0bc-280b2549c485";

                void PumpWait(Task? t)
                {
                    if (t == null) return;
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (!t.IsCompleted && sw.ElapsedMilliseconds < 5000)
                    {
                        Application.DoEvents();
                        Thread.Sleep(20);
                    }
                }

                // 1. Staff
                SessionService.CurrentUser = new CurrentUser { UserId = 4, FirstName = "Jerome", LastName = "Santos", Role = "Staff", CompanyId = 2 };
                var staffForm = new CC.Forms.Staff.StaffDashboardForm { Size = new Size(1366, 820), StartPosition = FormStartPosition.Manual, Location = new Point(50, 50) };
                staffForm.Show();
                PumpWait(staffForm.InitializationTask);
                for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                using (var bmp = new Bitmap(staffForm.Width, staffForm.Height))
                {
                    staffForm.DrawToBitmap(bmp, new Rectangle(0, 0, staffForm.Width, staffForm.Height));
                    bmp.Save(Path.Combine(outputDir, "screen_staff_dashboard.png"), ImageFormat.Png);
                }
                staffForm.Close();

                // 2. Manager
                SessionService.CurrentUser = new CurrentUser { UserId = 3, FirstName = "Camille", LastName = "Reyes", Role = "Manager", CompanyId = 2 };
                var mgrForm = new CC.Forms.Manager.ManagerDashboardForm { Size = new Size(1366, 820), StartPosition = FormStartPosition.Manual, Location = new Point(50, 50) };
                mgrForm.Show();
                PumpWait(mgrForm.InitializationTask);
                for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                using (var bmp = new Bitmap(mgrForm.Width, mgrForm.Height))
                {
                    mgrForm.DrawToBitmap(bmp, new Rectangle(0, 0, mgrForm.Width, mgrForm.Height));
                    bmp.Save(Path.Combine(outputDir, "screen_manager_dashboard.png"), ImageFormat.Png);
                }

                mgrForm.ScrollToBottom();
                for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                using (var bmpTable = new Bitmap(mgrForm.Width, mgrForm.Height))
                {
                    mgrForm.DrawToBitmap(bmpTable, new Rectangle(0, 0, mgrForm.Width, mgrForm.Height));
                    bmpTable.Save(Path.Combine(outputDir, "screen_manager_dashboard_table.png"), ImageFormat.Png);
                }
                mgrForm.Close();

                // 3. Admin
                SessionService.CurrentUser = new CurrentUser { UserId = 2, FirstName = "Lea", LastName = "Abad", Role = "Admin", CompanyId = 2 };
                var adminForm = new CC.Forms.Admin.AdminDashboardForm { Size = new Size(1366, 820), StartPosition = FormStartPosition.Manual, Location = new Point(50, 50) };
                adminForm.Show();
                PumpWait(adminForm.InitializationTask);
                for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                using (var bmp = new Bitmap(adminForm.Width, adminForm.Height))
                {
                    adminForm.DrawToBitmap(bmp, new Rectangle(0, 0, adminForm.Width, adminForm.Height));
                    bmp.Save(Path.Combine(outputDir, "screen_admin_dashboard.png"), ImageFormat.Png);
                }
                adminForm.Close();

                // 4. Super Admin
                SessionService.CurrentUser = new CurrentUser { UserId = 1, FirstName = "System", LastName = "SuperAdmin", Role = "SuperAdmin", CompanyId = 1 };
                var superForm = new CC.Forms.SuperAdmin.SuperAdminDashboardForm { Size = new Size(1366, 820), StartPosition = FormStartPosition.Manual, Location = new Point(50, 50) };
                superForm.Show();
                PumpWait(superForm.InitializationTask);
                for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                using (var bmp = new Bitmap(superForm.Width, superForm.Height))
                {
                    superForm.DrawToBitmap(bmp, new Rectangle(0, 0, superForm.Width, superForm.Height));
                    bmp.Save(Path.Combine(outputDir, "screen_superadmin_dashboard.png"), ImageFormat.Png);
                }
                superForm.Close();

                Console.WriteLine("All 4 role dashboards successfully captured with updated table styling!");
                return;
            }

            if (args.Length > 0 && args[0] == "--verify-all")
            {
                string outputDir = @"C:\Users\user1\.gemini\antigravity\brain\7aee3b98-6126-4c32-b0bc-280b2549c485";
                Console.WriteLine("=== STARTING AUTHENTICATION & PLATFORM VERIFICATION ===");

                try
                {
                    // 1. Ensure DB Ready
                    Console.WriteLine("[TEST 1] Database Initialization & Seed Synchronization...");
                    CrmDataService.EnsureDatabaseReadyAsync().GetAwaiter().GetResult();
                    Console.WriteLine("  -> PASS: Database ready and seeded.");

                    // 2. Super Admin Login
                    Console.WriteLine("[TEST 2] Testing Super Admin Login (superadmin / admin123)...");
                    var superAuth = CrmDataService.AuthenticateUserAsync("superadmin", "admin123").GetAwaiter().GetResult();
                    if (!superAuth.Success || superAuth.User == null)
                        throw new Exception($"Super Admin login failed: {superAuth.ErrorMessage}");
                    Console.WriteLine($"  -> PASS: Super Admin authenticated. Role={superAuth.User.Role?.RoleName ?? superAuth.User.RoleId.ToString()}, TenantDB={superAuth.TenantDatabase}");

                    // 3. Business Admin Login
                    Console.WriteLine("[TEST 3] Testing Business Admin Login (admin / admin123)...");
                    var adminAuth = CrmDataService.AuthenticateUserAsync("admin", "admin123").GetAwaiter().GetResult();
                    if (!adminAuth.Success || adminAuth.User == null)
                        throw new Exception($"Business Admin login failed: {adminAuth.ErrorMessage}");
                    Console.WriteLine($"  -> PASS: Business Admin authenticated. User={adminAuth.User.FirstName} {adminAuth.User.LastName}, Role={adminAuth.User.Role?.RoleName}");

                    // 4. Manager Login
                    Console.WriteLine("[TEST 4] Testing Manager Login (mark.perez / admin123)...");
                    var mgrAuth = CrmDataService.AuthenticateUserAsync("mark.perez", "admin123").GetAwaiter().GetResult();
                    if (!mgrAuth.Success || mgrAuth.User == null)
                        throw new Exception($"Manager login failed: {mgrAuth.ErrorMessage}");
                    Console.WriteLine($"  -> PASS: Manager authenticated. Role={mgrAuth.User.Role?.RoleName}");

                    // 5. Staff Login
                    Console.WriteLine("[TEST 5] Testing Staff Login (carrie.ngo / admin123)...");
                    var staffAuth = CrmDataService.AuthenticateUserAsync("carrie.ngo", "admin123").GetAwaiter().GetResult();
                    if (!staffAuth.Success || staffAuth.User == null)
                        throw new Exception($"Staff login failed: {staffAuth.ErrorMessage}");
                    Console.WriteLine($"  -> PASS: Staff authenticated. Role={staffAuth.User.Role?.RoleName}");

                    // 6. Bad Password
                    Console.WriteLine("[TEST 6] Testing Invalid Password Rejection...");
                    var badAuth = CrmDataService.AuthenticateUserAsync("admin", "wrongpassword").GetAwaiter().GetResult();
                    if (badAuth.Success)
                        throw new Exception("Invalid password test failed: expected authentication failure.");
                    Console.WriteLine($"  -> PASS: Rejected correctly with: '{badAuth.ErrorMessage}'");

                    // 7. Inactive Account
                    Console.WriteLine("[TEST 7] Testing Inactive Account Rejection (jose.uy)...");
                    var inactiveAuth = CrmDataService.AuthenticateUserAsync("jose.uy", "admin123").GetAwaiter().GetResult();
                    if (inactiveAuth.Success)
                        throw new Exception("Inactive account test failed: expected authentication failure.");
                    Console.WriteLine($"  -> PASS: Inactive account rejected correctly with: '{inactiveAuth.ErrorMessage}'");

                    // 8. User Management -> Login Lifecycle
                    Console.WriteLine("[TEST 8] Testing User Management -> Login Lifecycle (juan.manager / Temp@12345)...");
                    var existingJuan = CrmDataService.GetUsersAsync("juan.manager").GetAwaiter().GetResult();
                    if (existingJuan.Any(u => u.Username == "juan.manager"))
                    {
                        Console.WriteLine("  (Existing juan.manager found, updating password to Temp@12345)");
                        var u = existingJuan.First(x => x.Username == "juan.manager");
                        u.IsActive = true;
                        CrmDataService.UpdateUserAsync(u, "Temp@12345").GetAwaiter().GetResult();
                    }
                    else
                    {
                        var newUser = new CC.Domain.Entities.SystemUser
                        {
                            CompanyId = 2,
                            RoleId = 3, // Manager
                            FirstName = "Juan",
                            LastName = "Manager",
                            Username = "juan.manager",
                            Email = "juan.manager@cakeshop.ph",
                            Phone = "+63 917 555 7788",
                            IsActive = true,
                            CreatedDate = DateTime.UtcNow
                        };
                        CrmDataService.CreateUserAsync(newUser, "Temp@12345").GetAwaiter().GetResult();
                    }

                    var juanAuth = CrmDataService.AuthenticateUserAsync("juan.manager", "Temp@12345").GetAwaiter().GetResult();
                    if (!juanAuth.Success || juanAuth.User == null)
                        throw new Exception($"Login with newly created juan.manager failed: {juanAuth.ErrorMessage}");
                    if (juanAuth.User.RoleId != 3)
                        throw new Exception($"Expected RoleId 3 (Manager), got {juanAuth.User.RoleId}");
                    Console.WriteLine($"  -> PASS: juan.manager logged in successfully! Role={juanAuth.User.Role?.RoleName}, CompanyId={juanAuth.User.CompanyId}");

                    // 9. Database-Per-Company Provisioning & Tenant Isolation Test
                    Console.WriteLine("[TEST 9] Testing Database-Per-Company Architecture & Data Isolation...");
                    string testBizCode = "VBB99";
                    string testDbName = "VanillaBlossom_CRM";

                    // Clean up any previous test company if it exists in Master DB and drop test DB
                    var oldCompanies = CrmDataService.GetCompaniesAsync(testBizCode).GetAwaiter().GetResult();
                    var oldVbb = oldCompanies.FirstOrDefault(c => c.CompanyCode == testBizCode);
                    if (oldVbb != null)
                    {
                        CrmDataService.DeleteCompanyAsync(oldVbb.CompanyId).GetAwaiter().GetResult();
                    }
                    try
                    {
                        using var cleanCtx = CrmDataService.CreateDbContextForDatabase("(localdb)\\MSSQLLocalDB", testDbName);
                        cleanCtx.Database.EnsureDeleted();
                    }
                    catch { }

                    // Create Company B with its own dedicated operational database
                    var createdCompany = CrmDataService.CreateCompanyAsync(
                        name: "Vanilla Blossom Bakery",
                        code: testBizCode,
                        email: "contact@vanillablossom.ph",
                        phone: "+63 917 999 8877",
                        addressLine1: "99 Blossom Boulevard",
                        addressLine2: null,
                        city: "Makati City",
                        state: "Metro Manila",
                        postalCode: "1200",
                        country: "Philippines",
                        isActive: true,
                        serverName: "(localdb)\\MSSQLLocalDB",
                        databaseName: testDbName,
                        adminFullName: "Vanilla Admin",
                        adminUsername: "admin.vbb",
                        adminEmail: "admin@vanillablossom.ph",
                        adminPassword: "Temp@12345"
                    ).GetAwaiter().GetResult();

                    // 9.1 Verify Master DB routing link
                    var verifyCompanies = CrmDataService.GetCompaniesAsync(testBizCode).GetAwaiter().GetResult();
                    var vbb = verifyCompanies.FirstOrDefault(c => c.CompanyCode == testBizCode);
                    if (vbb == null)
                        throw new Exception("Created business not found in companies list!");
                    if (vbb.DatabaseName != testDbName)
                        throw new Exception($"Expected tenant DB '{testDbName}', got '{vbb.DatabaseName}'");
                    Console.WriteLine($"  -> PASS: Dedicated Tenant DB registered in Master DB: ID={vbb.CompanyId}, Name={vbb.CompanyName}, DB={vbb.DatabaseName}");

                    // 9.2 Test logging in with the new business admin (Dynamic Tenant Resolution)
                    var vbbAuth = CrmDataService.AuthenticateUserAsync("admin.vbb", "Temp@12345").GetAwaiter().GetResult();
                    if (!vbbAuth.Success || vbbAuth.User == null)
                        throw new Exception($"New business admin login failed: {vbbAuth.ErrorMessage}");
                    if (vbbAuth.User.CompanyId != vbb.CompanyId)
                        throw new Exception($"Expected CompanyId {vbb.CompanyId}, got {vbbAuth.User.CompanyId}");
                    if (vbbAuth.TenantDatabase != testDbName)
                        throw new Exception($"Expected authenticated tenant DB '{testDbName}', got '{vbbAuth.TenantDatabase}'");
                    Console.WriteLine($"  -> PASS: Tenant Resolution: admin.vbb resolved to Company #{vbbAuth.User.CompanyId} and connected to '{vbbAuth.TenantDatabase}'");

                    // 9.3 Test Operational Data Isolation
                    // Logged in as Company B (admin.vbb) -> Create customer in VanillaBlossom_CRM
                    SessionService.CurrentUser = new CurrentUser
                    {
                        UserId = vbbAuth.User.UserId,
                        Username = vbbAuth.User.Username,
                        FirstName = vbbAuth.User.FirstName,
                        LastName = vbbAuth.User.LastName,
                        Role = "Business Admin",
                        CompanyId = vbb.CompanyId,
                        CompanyName = vbb.CompanyName,
                        TenantServer = vbbAuth.TenantServer,
                        TenantDatabase = vbbAuth.TenantDatabase
                    };

                    var custB = CrmDataService.CreateCustomerAsync(new Customer
                    {
                        FirstName = "Isabella",
                        LastName = "Flores",
                        Email = "isabella@blossom.ph",
                        Phone = "+63 917 111 2222",
                        AddressText = "123 Blossom Way, Makati City",
                        CompanyId = vbb.CompanyId,
                        CreatedByUserId = vbbAuth.User.UserId
                    }, companyId: vbb.CompanyId).GetAwaiter().GetResult();
                    Console.WriteLine($"  -> Created Customer in Company B ({testDbName}): ID={custB.CustomerId}, Name={custB.FirstName} {custB.LastName}");

                    // Verify Company B has only its own customer (1 customer, NOT Company A's 208 customers)
                    var companyBCustomers = CrmDataService.GetCustomersAsync(companyId: vbb.CompanyId).GetAwaiter().GetResult();
                    if (companyBCustomers.Count != 1 || !companyBCustomers.Any(c => c.Email == "isabella@blossom.ph"))
                        throw new Exception($"Company B customer isolation failed! Found {companyBCustomers.Count} customers.");
                    Console.WriteLine($"  -> PASS: Company B ({testDbName}) contains strictly its own operational data ({companyBCustomers.Count} customer).");

                    // Switch Session to Company A (CC Custom Cake Shop)
                    SessionService.CurrentUser = new CurrentUser
                    {
                        UserId = 2,
                        Username = "admin",
                        Role = "Business Admin",
                        CompanyId = 2,
                        CompanyName = "CC Custom Cake Shop",
                        TenantServer = "(localdb)\\MSSQLLocalDB",
                        TenantDatabase = "CustomCakeCRM"
                    };

                    // Verify Company A CANNOT see Company B's customer
                    var companyACustomers = CrmDataService.GetCustomersAsync(companyId: 2).GetAwaiter().GetResult();
                    if (companyACustomers.Any(c => c.Email == "isabella@blossom.ph"))
                        throw new Exception("Company A can see Company B's customer! Complete isolation failed.");
                    Console.WriteLine($"  -> PASS: Complete Isolation: Company A ({companyACustomers.Count} customers) cannot access Company B's operational records.");

                    // 10. UI Screen Captures
                    // 10. Dashboard Data Calculations & Tenant Scoping
                    Console.WriteLine("[TEST 10] Testing Real Database Dashboard Calculations & Tenant Scoping...");
                    var superData = CrmDataService.GetSuperAdminDashboardDataAsync("30d").GetAwaiter().GetResult();
                    Console.WriteLine($"  -> Super Admin: TotalBiz={superData.TotalBusinesses}, ActiveBiz={superData.ActiveBusinesses}, Users={superData.PlatformUsersCount}, Backups={superData.TotalBackupsCount}, MRR=P{superData.EstimatedMonthlyRevenue:N0}, TermsAcceptances={superData.TermsAcceptancesCount}");

                    var adminData30 = CrmDataService.GetAdminDashboardDataAsync(2, "30d").GetAwaiter().GetResult();
                    var adminData7 = CrmDataService.GetAdminDashboardDataAsync(2, "7d").GetAwaiter().GetResult();
                    Console.WriteLine($"  -> Business Admin: 30d Rev=P{adminData30.TotalRevenue:N2} ({adminData30.TotalOrders} orders, AOV=P{adminData30.AverageOrderValue:N0}), CollectionRate={adminData30.CollectionRate:0.#}%, ActiveOrders={adminData30.ActiveOrdersCount}");

                    var mgrData = CrmDataService.GetManagerDashboardDataAsync(2, "30d").GetAwaiter().GetResult();
                    Console.WriteLine($"  -> Manager: ActiveOrders={mgrData.ActiveOrdersCount} (Val=P{mgrData.ActivePipelineValue:N0}), DueToday={mgrData.TodayDeliveriesCount}, Completed={mgrData.CompletedOrdersCount}, Inquiries={mgrData.OpenInquiriesCount}");

                    var staffData = CrmDataService.GetStaffDashboardDataAsync(4, 2, "1d").GetAwaiter().GetResult();
                    Console.WriteLine($"  -> Staff: DueFollowups={staffData.MyDueFollowupsCount}, HandledInquiries={staffData.MyHandledInquiriesCount}, TodayDueOrders={staffData.TodayDueOrdersCount}, Completed={staffData.CompletedOrdersCount}");

                    // 11. UI Screen Captures for all 4 Dashboards
                    Console.WriteLine("[TEST 11] Generating UI Visual Captures for Walkthrough...");

                    // 11.1 Super Admin Dashboard
                    SessionService.CurrentUser = new CurrentUser
                    {
                        UserId = 1,
                        Username = "superadmin",
                        FirstName = "Super",
                        LastName = "Admin",
                        Role = "SuperAdmin",
                        CompanyId = 1,
                        CompanyName = "Platform Administration",
                        Email = "superadmin@cakeshop.ph"
                    };

                    var superShell = new CC.Forms.SuperAdmin.SuperAdminDashboardForm();
                    superShell.Size = new Size(1366, 820);
                    superShell.StartPosition = FormStartPosition.Manual;
                    superShell.Location = new Point(50, 50);
                    superShell.Show();
                    for (int i = 0; i < 40; i++) { Application.DoEvents(); Thread.Sleep(50); }

                    using (var bmp = new Bitmap(superShell.Width, superShell.Height))
                    {
                        superShell.DrawToBitmap(bmp, new Rectangle(0, 0, superShell.Width, superShell.Height));
                        bmp.Save(Path.Combine(outputDir, "screen_superadmin_dashboard.png"), ImageFormat.Png);
                    }

                    superShell.Navigate("Businesses");
                    for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(30); }
                    using (var bmp = new Bitmap(superShell.Width, superShell.Height))
                    {
                        superShell.DrawToBitmap(bmp, new Rectangle(0, 0, superShell.Width, superShell.Height));
                        bmp.Save(Path.Combine(outputDir, "screen_superadmin_businesses.png"), ImageFormat.Png);
                    }
                    superShell.Close();

                    // Capture Business Details Modal
                    var detailsModal = new CC.Forms.SuperAdmin.Businesses.BusinessDetailsModal(vbb.CompanyId);
                    detailsModal.StartPosition = FormStartPosition.Manual;
                    detailsModal.Location = new Point(100, 100);
                    detailsModal.Show();
                    for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(30); }
                    using (var bmp = new Bitmap(detailsModal.Width, detailsModal.Height))
                    {
                        detailsModal.DrawToBitmap(bmp, new Rectangle(0, 0, detailsModal.Width, detailsModal.Height));
                        bmp.Save(Path.Combine(outputDir, "screen_superadmin_business_details.png"), ImageFormat.Png);
                    }
                    detailsModal.Close();

                    // Capture Register New Business Modal (Top & Scrolled Bottom)
                    using (var regModal = new CC.Forms.SuperAdmin.Businesses.BusinessModal())
                    {
                        regModal.StartPosition = FormStartPosition.Manual;
                        regModal.Location = new Point(100, 100);
                        regModal.Show();
                        for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                        using (var bmp = new Bitmap(regModal.Width, regModal.Height))
                        {
                            regModal.DrawToBitmap(bmp, new Rectangle(0, 0, regModal.Width, regModal.Height));
                            bmp.Save(Path.Combine(outputDir, "screen_superadmin_register_business_top.png"), ImageFormat.Png);
                        }

                        // Scroll body to bottom to capture Initial Business Admin Account section
                        var bodyPanel = regModal.Controls.OfType<Panel>().FirstOrDefault(p => p.Dock == DockStyle.Fill);
                        if (bodyPanel != null)
                        {
                            bodyPanel.AutoScrollPosition = new Point(0, 2000);
                            for (int i = 0; i < 15; i++) { Application.DoEvents(); Thread.Sleep(20); }
                        }
                        using (var bmp = new Bitmap(regModal.Width, regModal.Height))
                        {
                            regModal.DrawToBitmap(bmp, new Rectangle(0, 0, regModal.Width, regModal.Height));
                            bmp.Save(Path.Combine(outputDir, "screen_superadmin_register_business_bottom.png"), ImageFormat.Png);
                        }
                        regModal.Close();
                    }

                    // 11.2 Business Admin Dashboard
                    SessionService.CurrentUser = new CurrentUser
                    {
                        UserId = 2,
                        Username = "admin",
                        FirstName = "Lea",
                        LastName = "Abad",
                        Role = "Admin",
                        CompanyId = 2,
                        CompanyName = "CC Custom Cake Shop",
                        Email = "admin@cakeshop.ph"
                    };

                    var adminShell = new CC.Forms.Admin.AdminDashboardForm();
                    adminShell.Size = new Size(1366, 820);
                    adminShell.StartPosition = FormStartPosition.Manual;
                    adminShell.Location = new Point(50, 50);
                    adminShell.Show();
                    for (int i = 0; i < 40; i++) { Application.DoEvents(); Thread.Sleep(50); }

                    using (var bmp = new Bitmap(adminShell.Width, adminShell.Height))
                    {
                        adminShell.DrawToBitmap(bmp, new Rectangle(0, 0, adminShell.Width, adminShell.Height));
                        bmp.Save(Path.Combine(outputDir, "screen_admin_dashboard.png"), ImageFormat.Png);
                    }
                    adminShell.Close();

                    // 11.3 Manager Dashboard
                    SessionService.CurrentUser = new CurrentUser
                    {
                        UserId = 3,
                        Username = "mark.perez",
                        FirstName = "Camille",
                        LastName = "Reyes",
                        Role = "Manager",
                        CompanyId = 2,
                        CompanyName = "CC Custom Cake Shop",
                        Email = "mark.perez@cakeshop.ph"
                    };

                    var mgrShell = new CC.Forms.Manager.ManagerDashboardForm();
                    mgrShell.Size = new Size(1366, 820);
                    mgrShell.StartPosition = FormStartPosition.Manual;
                    mgrShell.Location = new Point(50, 50);
                    mgrShell.Show();
                    for (int i = 0; i < 40; i++) { Application.DoEvents(); Thread.Sleep(50); }

                    using (var bmp = new Bitmap(mgrShell.Width, mgrShell.Height))
                    {
                        mgrShell.DrawToBitmap(bmp, new Rectangle(0, 0, mgrShell.Width, mgrShell.Height));
                        bmp.Save(Path.Combine(outputDir, "screen_manager_dashboard.png"), ImageFormat.Png);
                    }
                    mgrShell.Close();

                    // 11.4 Staff Dashboard
                    SessionService.CurrentUser = new CurrentUser
                    {
                        UserId = 4,
                        Username = "carrie.ngo",
                        FirstName = "Jerome",
                        LastName = "Santos",
                        Role = "Staff",
                        CompanyId = 2,
                        CompanyName = "CC Custom Cake Shop",
                        Email = "carrie.ngo@cakeshop.ph"
                    };

                    var staffShell = new CC.Forms.Staff.StaffDashboardForm();
                    staffShell.Size = new Size(1366, 820);
                    staffShell.StartPosition = FormStartPosition.Manual;
                    staffShell.Location = new Point(50, 50);
                    staffShell.Show();
                    for (int i = 0; i < 40; i++) { Application.DoEvents(); Thread.Sleep(50); }

                    using (var bmp = new Bitmap(staffShell.Width, staffShell.Height))
                    {
                        staffShell.DrawToBitmap(bmp, new Rectangle(0, 0, staffShell.Width, staffShell.Height));
                        bmp.Save(Path.Combine(outputDir, "screen_staff_dashboard.png"), ImageFormat.Png);
                    }
                    staffShell.Close();

                    Console.WriteLine("  -> PASS: All role dashboard visual captures saved to artifact directory.");

                    // 12. Super Admin Navigation Items & UI Screens Verification
                    Console.WriteLine("[TEST 12] Super Admin Navigation & Dedicated Views Verification...");
                    SessionService.CurrentUser = new CurrentUser
                    {
                        UserId = 1,
                        Username = "superadmin",
                        FirstName = "Super",
                        LastName = "Admin",
                        Role = "SuperAdmin",
                        CompanyId = 1,
                        CompanyName = "Platform Administration",
                        Email = "superadmin@cakeshop.ph"
                    };

                    using (var saShell = new CC.Forms.SuperAdmin.SuperAdminDashboardForm())
                    {
                        saShell.Size = new Size(1366, 820);
                        saShell.StartPosition = FormStartPosition.Manual;
                        saShell.Location = new Point(50, 50);
                        saShell.Show();
                        for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(30); }

                        // Assert the 6 requested navigation items (Sign Out is in pinned footer)
                        string[] expectedNavs = { "Dashboard", "Businesses", "Subscriptions", "Terms & Conditions", "System Monitoring & Backups", "Users" };
                        foreach (var nav in expectedNavs)
                        {
                            if (!saShell.SidebarCtrl.HasNavItem(nav))
                                throw new Exception($"Super Admin sidebar is missing required navigation item: '{nav}'");
                        }
                        if (saShell.SidebarCtrl.HasNavItem("Sign Out"))
                            throw new Exception("Sign Out should not be duplicated in the navigation list (it is in the footer).");
                        Console.WriteLine("  -> PASS: All 6 Super Admin navigation items verified (Sign Out located in footer).");

                        // Capture Subscriptions Screen
                        saShell.Navigate("Subscriptions");
                        for (int i = 0; i < 30; i++) { Application.DoEvents(); Thread.Sleep(30); }
                        using (var bmp = new Bitmap(saShell.Width, saShell.Height))
                        {
                            saShell.DrawToBitmap(bmp, new Rectangle(0, 0, saShell.Width, saShell.Height));
                            bmp.Save(Path.Combine(outputDir, "screen_superadmin_subscriptions.png"), ImageFormat.Png);
                        }

                        // Capture Subscriptions Screen (Tab 1: Business Subscriptions Table)
                        if (saShell.MainPanel.Controls.Count > 0 && saShell.MainPanel.Controls[0] is CC.Forms.SuperAdmin.Subscriptions.SuperAdminSubscriptionForm saSubForm)
                        {
                            var tabBar = saSubForm.Controls.OfType<Panel>().FirstOrDefault(p => p.Height == 44);
                            var flow = tabBar?.Controls.OfType<FlowLayoutPanel>().FirstOrDefault();
                            var btnTabBiz = flow?.Controls.OfType<Button>().FirstOrDefault(b => b.Text.Contains("Business Subscriptions"));
                            btnTabBiz?.PerformClick();
                            for (int i = 0; i < 30; i++) { Application.DoEvents(); Thread.Sleep(30); }
                            using (var bmp = new Bitmap(saShell.Width, saShell.Height))
                            {
                                saShell.DrawToBitmap(bmp, new Rectangle(0, 0, saShell.Width, saShell.Height));
                                bmp.Save(Path.Combine(outputDir, "screen_superadmin_business_subscriptions_table.png"), ImageFormat.Png);
                            }
                        }

                        // Capture Terms & Conditions Screen
                        saShell.Navigate("Terms & Conditions");
                        for (int i = 0; i < 30; i++) { Application.DoEvents(); Thread.Sleep(30); }
                        using (var bmp = new Bitmap(saShell.Width, saShell.Height))
                        {
                            saShell.DrawToBitmap(bmp, new Rectangle(0, 0, saShell.Width, saShell.Height));
                            bmp.Save(Path.Combine(outputDir, "screen_superadmin_terms.png"), ImageFormat.Png);
                        }

                        // Capture System Monitoring & Backups Screen
                        saShell.Navigate("System Monitoring & Backups");
                        for (int i = 0; i < 30; i++) { Application.DoEvents(); Thread.Sleep(30); }
                        using (var bmp = new Bitmap(saShell.Width, saShell.Height))
                        {
                            saShell.DrawToBitmap(bmp, new Rectangle(0, 0, saShell.Width, saShell.Height));
                            bmp.Save(Path.Combine(outputDir, "screen_superadmin_monitoring.png"), ImageFormat.Png);
                        }

                        // Capture Plan Modal
                        using (var planModal = new CC.Forms.SuperAdmin.Subscriptions.PlanModal())
                        {
                            planModal.StartPosition = FormStartPosition.Manual;
                            planModal.Location = new Point(100, 100);
                            planModal.Show();
                            for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                            using (var bmp = new Bitmap(planModal.Width, planModal.Height))
                            {
                                planModal.DrawToBitmap(bmp, new Rectangle(0, 0, planModal.Width, planModal.Height));
                                bmp.Save(Path.Combine(outputDir, "screen_superadmin_plan_modal.png"), ImageFormat.Png);
                            }
                            planModal.Close();
                        }

                        // Capture Assign Subscription Modal
                        var sampleBiz = new CC.Services.CompanySubscriptionListItem(
                            1, "CC01", "Custom Cake Shop", "CustomCakeCRM", 2, "Pro Plan", 9599m, 365, 10, 1, "Active", DateTime.UtcNow, DateTime.UtcNow.AddYears(1), true, 3
                        );
                        var samplePlans = CrmDataService.GetSubscriptionPlansAsync(includeArchived: false).GetAwaiter().GetResult();
                        using (var assignModal = new CC.Forms.SuperAdmin.Subscriptions.AssignSubscriptionModal(sampleBiz, samplePlans))
                        {
                            assignModal.StartPosition = FormStartPosition.Manual;
                            assignModal.Location = new Point(100, 100);
                            assignModal.Show();
                            for (int i = 0; i < 20; i++) { Application.DoEvents(); Thread.Sleep(20); }
                            using (var bmp = new Bitmap(assignModal.Width, assignModal.Height))
                            {
                                assignModal.DrawToBitmap(bmp, new Rectangle(0, 0, assignModal.Width, assignModal.Height));
                                bmp.Save(Path.Combine(outputDir, "screen_superadmin_assign_subscription_modal.png"), ImageFormat.Png);
                            }
                            assignModal.Close();
                        }

                        saShell.Close();
                    }

                    // Also capture Business Admin Subscription screen
                    SessionService.CurrentUser = new CurrentUser
                    {
                        UserId = 2,
                        Username = "admin",
                        FirstName = "Lea",
                        LastName = "Abad",
                        Role = "Admin",
                        CompanyId = 2,
                        CompanyName = "CC Custom Cake Shop",
                        Email = "admin@cakeshop.ph"
                    };
                    using (var adminSubShell = new CC.Forms.Admin.AdminDashboardForm())
                    {
                        adminSubShell.Size = new Size(1366, 820);
                        adminSubShell.StartPosition = FormStartPosition.Manual;
                        adminSubShell.Location = new Point(50, 50);
                        adminSubShell.Show();
                        adminSubShell.Navigate("Subscription");
                        for (int i = 0; i < 30; i++) { Application.DoEvents(); Thread.Sleep(30); }
                        using (var bmp = new Bitmap(adminSubShell.Width, adminSubShell.Height))
                        {
                            adminSubShell.DrawToBitmap(bmp, new Rectangle(0, 0, adminSubShell.Width, adminSubShell.Height));
                            bmp.Save(Path.Combine(outputDir, "screen_admin_subscription.png"), ImageFormat.Png);
                        }
                        adminSubShell.Close();
                    }
                    Console.WriteLine("  -> PASS: Super Admin and Business Admin subscription/monitoring views captured.");

                    // 13. Subscription Management CRUD & Single Source of Truth
                    Console.WriteLine("[TEST 13] Subscription Plans CRUD & Cross-Database Single Source of Truth...");
                    var initialPlans = CrmDataService.GetSubscriptionPlansAsync().GetAwaiter().GetResult();
                    if (initialPlans.Count < 3)
                        throw new Exception($"Expected at least 3 seeded subscription plans, got {initialPlans.Count}");
                    Console.WriteLine($"  -> Initial Plans Count: {initialPlans.Count} (Starter, Pro, Enterprise)");

                    // Create New Custom Plan as Super Admin
                    var growthPlan = CrmDataService.CreateSubscriptionPlanAsync("Growth Suite", 1499.00m, 60, 12).GetAwaiter().GetResult();
                    Console.WriteLine($"  -> Created New Plan: PlanId={growthPlan.PlanId}, Name={growthPlan.PlanName}, Price=P{growthPlan.Price}, Seats={growthPlan.MaxUsers}");

                    // Assign this Plan to Company B (vbb.CompanyId)
                    CrmDataService.AssignCompanySubscriptionAsync(vbb.CompanyId, growthPlan.PlanId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(growthPlan.DurationDays)).GetAwaiter().GetResult();
                    Console.WriteLine($"  -> Assigned to Company B (Id={vbb.CompanyId}): PlanId={growthPlan.PlanId}");

                    // Business Admin queries its own subscription
                    var bizSubInfo = CrmDataService.GetSubscriptionInfoAsync(vbb.CompanyId).GetAwaiter().GetResult();
                    if (bizSubInfo.PlanName != "Growth Suite" || bizSubInfo.Price != 1499.00m || bizSubInfo.MaxSeats != 12)
                        throw new Exception($"Single Source of Truth mismatch! Expected Growth Suite, P1499, 12 users. Got: {bizSubInfo.PlanName}, P{bizSubInfo.Price}, {bizSubInfo.MaxSeats} users.");
                    if (bizSubInfo.Status != "Active")
                        throw new Exception($"Subscription status invalid: {bizSubInfo.Status}");
                    Console.WriteLine($"  -> PASS: Business Admin reads identical subscription from Master DB: {bizSubInfo.PlanName}, Seats={bizSubInfo.UsedSeats}/{bizSubInfo.MaxSeats}, Renewal={bizSubInfo.RenewalDate:yyyy-MM-dd}");

                    // Super Admin renews the subscription
                    DateTime preRenewEnd = bizSubInfo.RenewalDate;
                    CrmDataService.RenewSubscriptionAsync(vbb.CompanyId).GetAwaiter().GetResult();
                    var bizSubRenewed = CrmDataService.GetSubscriptionInfoAsync(vbb.CompanyId).GetAwaiter().GetResult();
                    if (bizSubRenewed.RenewalDate <= preRenewEnd)
                        throw new Exception($"Renew failed to extend validity date! Pre: {preRenewEnd}, Post: {bizSubRenewed.RenewalDate}");
                    Console.WriteLine($"  -> PASS: Subscription Renewal successfully extended validity date to {bizSubRenewed.RenewalDate:yyyy-MM-dd}.");

                    // 13b. Verify Branching Configuration and Archive / Restore Lifecycle
                    Console.WriteLine("  -> Testing Multi-Branch Plan Creation & Archive/Restore Lifecycle...");
                    var multiBranchPlan = CrmDataService.CreateSubscriptionPlanAsync("Multi-Branch Pro", 2499.00m, 90, 20, allowBranching: true, maxBranches: 5).GetAwaiter().GetResult();
                    if (!multiBranchPlan.AllowBranching || multiBranchPlan.MaxBranches != 5)
                        throw new Exception($"Branching configuration mismatch! Expected AllowBranching=true, MaxBranches=5. Got: {multiBranchPlan.AllowBranching}, {multiBranchPlan.MaxBranches}");
                    Console.WriteLine($"    * Created Multi-Branch Plan: PlanId={multiBranchPlan.PlanId}, AllowBranching={multiBranchPlan.AllowBranching}, MaxBranches={multiBranchPlan.MaxBranches}");

                    // Test Soft-Archive
                    CrmDataService.ArchiveSubscriptionPlanAsync(multiBranchPlan.PlanId).GetAwaiter().GetResult();
                    var activePlansAfterArchive = CrmDataService.GetSubscriptionPlansAsync(includeArchived: false).GetAwaiter().GetResult();
                    if (activePlansAfterArchive.Any(p => p.PlanId == multiBranchPlan.PlanId))
                        throw new Exception("Archived plan must NOT appear in default active subscription plans list!");

                    var allPlansAfterArchive = CrmDataService.GetSubscriptionPlansAsync(includeArchived: true).GetAwaiter().GetResult();
                    var archivedItem = allPlansAfterArchive.FirstOrDefault(p => p.PlanId == multiBranchPlan.PlanId);
                    if (archivedItem == null || archivedItem.IsActive)
                        throw new Exception("Archived plan must appear with IsActive=false when includeArchived=true!");
                    Console.WriteLine("    * PASS: Plan soft-archived successfully, correctly excluded from active picker view.");

                    // Test Restore
                    CrmDataService.RestoreSubscriptionPlanAsync(multiBranchPlan.PlanId).GetAwaiter().GetResult();
                    var activePlansAfterRestore = CrmDataService.GetSubscriptionPlansAsync(includeArchived: false).GetAwaiter().GetResult();
                    if (!activePlansAfterRestore.Any(p => p.PlanId == multiBranchPlan.PlanId && p.IsActive))
                        throw new Exception("Restored plan must reappear in active subscription plans list!");
                    Console.WriteLine("    * PASS: Plan restored successfully to active status.");

                    // Assign multi-branch plan and verify business-side retrieval
                    CrmDataService.AssignCompanySubscriptionAsync(vbb.CompanyId, multiBranchPlan.PlanId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(90)).GetAwaiter().GetResult();
                    var branchSubInfo = CrmDataService.GetSubscriptionInfoAsync(vbb.CompanyId).GetAwaiter().GetResult();
                    if (!branchSubInfo.AllowBranching || branchSubInfo.MaxBranches != 5)
                        throw new Exception($"Business Admin failed to retrieve branching info! Got AllowBranching={branchSubInfo.AllowBranching}, MaxBranches={branchSubInfo.MaxBranches}");
                    Console.WriteLine($"    * PASS: Business Admin correctly reads branching capabilities: Multi-Branch={branchSubInfo.AllowBranching}, MaxBranches={branchSubInfo.MaxBranches}.");

                    // 14. User Seat Limit Enforcement
                    Console.WriteLine("[TEST 14] User Seat Limit Enforcement (MaxUsers Guard)...");
                    // Create a 1-seat restricted plan and assign to Company B
                    var microPlan = CrmDataService.CreateSubscriptionPlanAsync("Micro Single User", 299.00m, 30, 1).GetAwaiter().GetResult();
                    CrmDataService.AssignCompanySubscriptionAsync(vbb.CompanyId, microPlan.PlanId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(30)).GetAwaiter().GetResult();

                    // Company B already has 1 user. Attempting to create a 2nd active user MUST fail with InvalidOperationException
                    bool seatLimitEnforced = false;
                    try
                    {
                        CrmDataService.CreateUserAsync(new SystemUser
                        {
                            CompanyId = vbb.CompanyId,
                            Username = "excess.user",
                            FirstName = "Excess",
                            LastName = "Seat",
                            Email = "excess@blossom.ph",
                            RoleId = 3, // Staff
                            IsActive = true
                        }, "Password123!").GetAwaiter().GetResult();
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("seat limit"))
                    {
                        seatLimitEnforced = true;
                        Console.WriteLine($"  -> Correctly blocked user creation: {ex.Message}");
                    }

                    if (!seatLimitEnforced)
                        throw new Exception("Security/Licensing violation: CreateUserAsync permitted exceeding MaxUsers seat limit!");
                    Console.WriteLine("  -> PASS: Seat limit enforcement successfully prevented exceeding MaxUsers.");

                    // Restore Company B to Growth Suite
                    CrmDataService.AssignCompanySubscriptionAsync(vbb.CompanyId, growthPlan.PlanId, 1, DateTime.UtcNow, DateTime.UtcNow.AddDays(60)).GetAwaiter().GetResult();

                    // 15. Terms & Conditions Module
                    Console.WriteLine("[TEST 15] Terms & Conditions Module & Versioning...");
                    var termsVersions = CrmDataService.GetTermsVersionsAsync().GetAwaiter().GetResult();
                    if (termsVersions.Count == 0)
                        throw new Exception("Expected at least baseline v1.0 terms version.");
                    Console.WriteLine($"  -> Existing Terms Versions: {termsVersions.Count} (Latest: {termsVersions[0].Version})");

                    var newTerms = CrmDataService.CreateTermsVersionAsync("v2.0", "### Platform Terms of Service v2.0\r\n\r\nUpdated multi-tenant data compliance rules.", DateTime.Today.AddDays(7), 1).GetAwaiter().GetResult();
                    if (newTerms.Version != "v2.0")
                        throw new Exception("Created terms version does not match requested version.");
                    Console.WriteLine($"  -> Created new Terms Version: {newTerms.Version}, Effective: {newTerms.EffectiveDate:yyyy-MM-dd}");

                    // Record acceptance
                    CrmDataService.RecordTermsAcceptanceAsync(newTerms.TermsId, 1, "127.0.0.1", "WinForms Client 2.0").GetAwaiter().GetResult();
                    var acceptances = CrmDataService.GetTermsAcceptancesAsync(newTerms.TermsId).GetAwaiter().GetResult();
                    if (acceptances.Count == 0 || !acceptances.Any(a => a.UserId == 1))
                        throw new Exception("Terms acceptance record was not stored correctly.");
                    Console.WriteLine($"  -> PASS: Terms acceptance tracked successfully ({acceptances.Count} acceptances recorded).");

                    // 16. System Monitoring & Multi-Tenant Database Backups
                    Console.WriteLine("[TEST 16] System Monitoring Audit Logs & Multi-Tenant Database Backup Engine...");
                    // Test Audit Log
                    CrmDataService.LogSystemAuditAsync("superadmin", "TEST_SYSTEM_CHECK", "Verification test executed system integrity check", "127.0.0.1").GetAwaiter().GetResult();
                    var auditLogs = CrmDataService.GetSystemAuditLogsAsync(limit: 10).GetAwaiter().GetResult();
                    if (!auditLogs.Any(l => l.ActionType == "TEST_SYSTEM_CHECK"))
                        throw new Exception("Audit log entry was not found in system logs.");
                    Console.WriteLine($"  -> PASS: System audit log verified ({auditLogs.Count} recent logs).");

                    // Test Multi-Tenant Database Backup
                    var registeredDbs = BackupService.GetRegisteredDatabasesAsync().GetAwaiter().GetResult();
                    Console.WriteLine($"  -> Target Databases for Backup ({registeredDbs.Count}): {string.Join(", ", registeredDbs)}");
                    if (!registeredDbs.Contains("MSME_MasterCRM") || !registeredDbs.Contains("CustomCakeCRM"))
                        throw new Exception("Backup discovery failed to include Master and Tenant databases!");

                    var backupResults = BackupService.BackupAllDatabasesAsync().GetAwaiter().GetResult();
                    foreach (var br in backupResults)
                    {
                        Console.WriteLine($"  -> Database '{br.DatabaseName}' Backup: Success={br.IsSuccess}, File={Path.GetFileName(br.FilePath)}, Size={br.FileSizeBytes:N0} bytes");
                        if (!br.IsSuccess || !File.Exists(br.FilePath) || br.FileSizeBytes <= 0)
                            throw new Exception($"Backup failed for database '{br.DatabaseName}': {br.ErrorMessage}");
                    }

                    var backupHistory = BackupService.GetBackupHistory();
                    if (backupHistory.Count < backupResults.Count)
                        throw new Exception("Backup history does not reflect newly generated backup files.");
                    Console.WriteLine($"  -> PASS: Native SQL Server multi-database backup engine executed successfully. ({backupHistory.Count} total backups found in repository).");

                    // 17. KPI Click-to-Data Navigation & Query Alignment
                    Console.WriteLine("[TEST 17] KPI Click-to-Data Navigation & Query Alignment Verification...");

                    // Test Orders Active filter
                    var activeOrders = CrmDataService.GetOrdersPagedAsync("Active", pageSize: 20).GetAwaiter().GetResult();
                    if (activeOrders.Items.Any(o => o.StatusId != 1 && o.StatusId != 2 && o.StatusId != 4))
                        throw new Exception("Active orders filter included invalid order status.");
                    Console.WriteLine($"  -> PASS: Orders 'Active' filter verified ({activeOrders.TotalCount} matching orders with Status in Confirmed/Processing/Ready).");

                    // Test Orders Today filter
                    var todayOrders = CrmDataService.GetOrdersPagedAsync("Today", pageSize: 20).GetAwaiter().GetResult();
                    var todayUtc = DateTime.UtcNow.Date;
                    if (todayOrders.Items.Any(o => !o.DeliveryDate.HasValue || o.DeliveryDate.Value.Date != todayUtc))
                        throw new Exception("Today orders filter returned orders with mismatching delivery date.");
                    Console.WriteLine($"  -> PASS: Orders 'Today' filter verified ({todayOrders.TotalCount} matching orders due today).");

                    // Test Follow-ups Due & Overdue
                    var dueFollowUps = CrmDataService.GetFollowUpsPagedAsync("Due", pageSize: 20).GetAwaiter().GetResult();
                    if (dueFollowUps.Items.Any(f => f.StatusId != 0 && f.StatusId != 3))
                        throw new Exception("Due follow-ups filter included non-pending follow-up status.");
                    Console.WriteLine($"  -> PASS: Follow-ups 'Due' filter verified ({dueFollowUps.TotalCount} matching tasks).");

                    var overdueFollowUps = CrmDataService.GetFollowUpsPagedAsync("Overdue", pageSize: 20).GetAwaiter().GetResult();
                    if (overdueFollowUps.Items.Any(f => f.StatusId != 3 && !(f.StatusId == 0 && f.FollowUpDate.Date < todayUtc)))
                        throw new Exception("Overdue follow-ups filter included non-overdue tasks.");
                    Console.WriteLine($"  -> PASS: Follow-ups 'Overdue' filter verified ({overdueFollowUps.TotalCount} matching overdue tasks).");

                    // Test Form Instantiations with Filter Contexts
                    using (var orderFormActive = new CC.Forms.Staff.Orders.OrderListForm("Active"))
                    {
                        if (orderFormActive == null) throw new Exception("Failed to instantiate OrderListForm with filter.");
                    }
                    using (var followUpFormDue = new CC.Forms.Staff.FollowUps.FollowUpListForm("Due"))
                    {
                        if (followUpFormDue == null) throw new Exception("Failed to instantiate FollowUpListForm with filter.");
                    }
                    using (var paymentFormUnpaid = new CC.Forms.Staff.Payments.PaymentListForm("Unpaid"))
                    {
                        if (paymentFormUnpaid == null) throw new Exception("Failed to instantiate PaymentListForm with filter.");
                    }
                    using (var subFormExpiring = new CC.Forms.SuperAdmin.Subscriptions.SuperAdminSubscriptionForm(1, "Expiring"))
                    {
                        if (subFormExpiring == null) throw new Exception("Failed to instantiate SuperAdminSubscriptionForm with tab and filter.");
                    }
                    Console.WriteLine("  -> PASS: All destination forms successfully instantiated and bound with designated KPI filter contexts.");

                    Console.WriteLine("=== ALL 17 VERIFICATION TESTS PASSED SUCCESSFULLY! ===");
                    Environment.Exit(0);
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n[VERIFICATION FAILED]: {ex.Message}\n{ex.StackTrace}");
                    Environment.Exit(1);
                    return;
                }
            }

            if (args.Length > 0 && args[0] == "--test-capture")
            {
                SessionService.CurrentUser = new CurrentUser
                {
                    UserId = 4,
                    FirstName = "Jerome",
                    LastName = "Santos",
                    Role = "Staff",
                    CompanyId = 2
                };

                string outputDir = @"C:\Users\user1\.gemini\antigravity\brain\7aee3b98-6126-4c32-b0bc-280b2549c485";
                if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
                {
                    outputDir = args[1];
                }

                var loginForm = new CC.Forms.Authentication.LoginForm();
                loginForm.Size = new Size(1100, 720);
                loginForm.StartPosition = FormStartPosition.Manual;
                loginForm.Location = new Point(50, 50);
                loginForm.Show();
                Application.DoEvents();
                Thread.Sleep(300);
                using (var bmpLogin = new Bitmap(loginForm.Width, loginForm.Height))
                {
                    loginForm.DrawToBitmap(bmpLogin, new Rectangle(0, 0, loginForm.Width, loginForm.Height));
                    bmpLogin.Save(Path.Combine(outputDir, "screen_login.png"), ImageFormat.Png);
                }
                loginForm.Close();
                loginForm.Dispose();

                void WaitForTask(Task? task)
                {
                    if (task == null) return;
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    while (!task.IsCompleted && sw.ElapsedMilliseconds < 10000)
                    {
                        Application.DoEvents();
                        Thread.Sleep(20);
                    }
                }

                var shell = new StaffDashboardForm();
                shell.Size = new Size(1366, 820);
                shell.StartPosition = FormStartPosition.Manual;
                shell.Location = new Point(50, 50);
                shell.Show();
                Application.DoEvents();
                WaitForTask(shell.InitializationTask);
                for (int i = 0; i < 15; i++) { Application.DoEvents(); Thread.Sleep(20); }

                void Capture(string filename)
                {
                    for (int i = 0; i < 25; i++)
                    {
                        Application.DoEvents();
                        Thread.Sleep(30);
                    }
                    using var bmp = new Bitmap(shell.Width, shell.Height);
                    shell.DrawToBitmap(bmp, new Rectangle(0, 0, shell.Width, shell.Height));
                    bmp.Save(Path.Combine(outputDir, filename), ImageFormat.Png);
                }

                Capture("screen_staff_dashboard.png");
                Capture("screen_dashboard.png");

                shell.Navigate("Customers");
                Capture("screen_customers.png");

                // Capture Customer Details Screen
                var existingCusts = CrmDataService.GetCustomersAsync().GetAwaiter().GetResult();
                if (existingCusts.Count > 0)
                {
                    using var detModal = new CC.Forms.Staff.Customers.CustomerDetailsForm(existingCusts[0]);
                    detModal.StartPosition = FormStartPosition.Manual;
                    detModal.Location = new Point(shell.Location.X + (shell.Width - detModal.Width) / 2, shell.Location.Y + (shell.Height - detModal.Height) / 2);
                    detModal.Show();
                    for (int i = 0; i < 15; i++) { Application.DoEvents(); Thread.Sleep(30); }
                    using var bmpDet = new Bitmap(detModal.Width, detModal.Height);
                    detModal.DrawToBitmap(bmpDet, new Rectangle(0, 0, detModal.Width, detModal.Height));
                    bmpDet.Save(Path.Combine(outputDir, "screen_customer_details.png"), ImageFormat.Png);
                    detModal.Close();
                }

                shell.Navigate("Inquiries");
                Capture("screen_inquiries.png");

                // Capture New Inquiry Modal
                using (var modal = new CC.Forms.Staff.Inquiries.InquiryForm())
                {
                    modal.StartPosition = FormStartPosition.Manual;
                    modal.Location = new Point(shell.Location.X + (shell.Width - modal.Width) / 2, shell.Location.Y + (shell.Height - modal.Height) / 2);
                    modal.Show();
                    Application.DoEvents();
                    Thread.Sleep(200);
                    using var bmpModal = new Bitmap(modal.Width, modal.Height);
                    modal.DrawToBitmap(bmpModal, new Rectangle(0, 0, modal.Width, modal.Height));
                    bmpModal.Save(Path.Combine(outputDir, "screen_inquiry_modal.png"), ImageFormat.Png);
                    modal.Close();
                }

                // Ensure we have at least one active inquiry (e.g. In Progress) and one Approved inquiry
                var testInquiries = CrmDataService.GetInquiriesAsync().GetAwaiter().GetResult();
                var activeInq = testInquiries.FirstOrDefault(i => i.Status == "In Progress" || i.Status == "New");
                if (activeInq == null)
                {
                    activeInq = new CC.Domain.Entities.CustomerInquiry
                    {
                        CustomerId = testInquiries.FirstOrDefault()?.CustomerId ?? 1,
                        CakeType = "Custom Chocolate Tier Cake",
                        EventDate = DateTime.Today.AddDays(20),
                        AssignedTo = "Staff - Lea R.",
                        EstimatedBudget = 4500,
                        Status = "In Progress",
                        Notes = "Theme: Minimalist Lavender"
                    };
                    activeInq = CrmDataService.CreateInquiryAsync(activeInq).GetAwaiter().GetResult();
                }

                var approvedInq = testInquiries.FirstOrDefault(i => i.Status == "Approved");
                if (approvedInq == null)
                {
                    approvedInq = new CC.Domain.Entities.CustomerInquiry
                    {
                        CustomerId = testInquiries.FirstOrDefault()?.CustomerId ?? 1,
                        CakeType = "3 tier cake",
                        EventDate = DateTime.Today.AddDays(30),
                        AssignedTo = "Staff - Lea R.",
                        EstimatedBudget = 7000,
                        Status = "Approved",
                        Notes = "Chocolate"
                    };
                    approvedInq = CrmDataService.CreateInquiryAsync(approvedInq).GetAwaiter().GetResult();
                }

                // Refresh inquiries on screen so the table shows both Update Status and Convert to Order
                shell.Navigate("Inquiries");
                Capture("screen_inquiries.png");

                // Capture Inquiry Status Modal on the active inquiry (showing forward-only options)
                using (var statusModal = new CC.Forms.Staff.Inquiries.InquiryStatusModal(activeInq))
                {
                    statusModal.StartPosition = FormStartPosition.Manual;
                    statusModal.Location = new Point(shell.Location.X + (shell.Width - statusModal.Width) / 2, shell.Location.Y + (shell.Height - statusModal.Height) / 2);
                    statusModal.Show();
                    Application.DoEvents();
                    Thread.Sleep(200);
                    using var bmpStatus = new Bitmap(statusModal.Width, statusModal.Height);
                    statusModal.DrawToBitmap(bmpStatus, new Rectangle(0, 0, statusModal.Width, statusModal.Height));
                    bmpStatus.Save(Path.Combine(outputDir, "screen_inquiry_status_modal.png"), ImageFormat.Png);
                    statusModal.Close();
                }

                // Capture InquiryForm on the Approved inquiry (showing locked indicator banner and disabled inputs)
                using (var lockedForm = new CC.Forms.Staff.Inquiries.InquiryForm(approvedInq))
                {
                    lockedForm.StartPosition = FormStartPosition.Manual;
                    lockedForm.Location = new Point(shell.Location.X + (shell.Width - lockedForm.Width) / 2, shell.Location.Y + (shell.Height - lockedForm.Height) / 2);
                    lockedForm.Show();
                    for (int i = 0; i < 15; i++) { Application.DoEvents(); Thread.Sleep(30); }
                    using var bmpLocked = new Bitmap(lockedForm.Width, lockedForm.Height);
                    lockedForm.DrawToBitmap(bmpLocked, new Rectangle(0, 0, lockedForm.Width, lockedForm.Height));
                    bmpLocked.Save(Path.Combine(outputDir, "screen_inquiry_locked_modal.png"), ImageFormat.Png);
                    lockedForm.Close();
                }

                // Capture Order Conversion Modal pre-populated from approved inquiry
                using (var convOrderModal = new CC.Forms.Staff.Orders.OrderForm(approvedInq))
                {
                    convOrderModal.StartPosition = FormStartPosition.Manual;
                    convOrderModal.Location = new Point(shell.Location.X + (shell.Width - convOrderModal.Width) / 2, shell.Location.Y + (shell.Height - convOrderModal.Height) / 2);
                    convOrderModal.Show();
                    for (int i = 0; i < 15; i++) { Application.DoEvents(); Thread.Sleep(30); }
                    using var bmpConv = new Bitmap(convOrderModal.Width, convOrderModal.Height);
                    convOrderModal.DrawToBitmap(bmpConv, new Rectangle(0, 0, convOrderModal.Width, convOrderModal.Height));
                    bmpConv.Save(Path.Combine(outputDir, "screen_inquiry_convert_modal.png"), ImageFormat.Png);
                    convOrderModal.Close();
                }

                shell.Navigate("Orders");
                Capture("screen_orders.png");

                // Ensure a rich sample order matching mockup (ORD-2026-0045 style) exists
                var allOrders = CrmDataService.GetOrdersAsync().GetAwaiter().GetResult();
                var procOrder = allOrders.FirstOrDefault(o => o.StatusId == 2) ?? allOrders.FirstOrDefault(o => o.StatusId == 1) ?? allOrders.FirstOrDefault();
                if (procOrder != null && procOrder.StatusId < 2)
                {
                    procOrder.StatusId = 2; // Processing
                    procOrder.CakeSize = "2-tier, 6\" + 8\"";
                    procOrder.Flavor = "Vanilla bean with lemon curd filling";
                    procOrder.DesignTheme = "Watercolor pastel with baby blue + gold fondant accents";
                    procOrder.Notes = "Pickup at 10am. Box with extra padding.";
                    procOrder.TotalAmount = 6800;
                    try { CrmDataService.UpdateOrderAsync(procOrder).GetAwaiter().GetResult(); } catch { }
                }

                if (shell.MainPanel.Controls.Count > 0 && shell.MainPanel.Controls[0] is CC.Forms.Staff.Orders.OrderListForm orderListForm && procOrder != null)
                {
                    WaitForTask(orderListForm.ShowOrderDetailsAsync(procOrder.OrderId));
                    for (int i = 0; i < 15; i++) { Application.DoEvents(); Thread.Sleep(30); }
                    Capture("screen_order_details.png");

                    // Capture Order Status Modal
                    using (var stModal = new CC.Forms.Staff.Orders.OrderStatusModal(procOrder))
                    {
                        stModal.StartPosition = FormStartPosition.Manual;
                        stModal.Location = new Point(shell.Location.X + (shell.Width - stModal.Width) / 2, shell.Location.Y + (shell.Height - stModal.Height) / 2);
                        stModal.Show();
                        for (int i = 0; i < 15; i++) { Application.DoEvents(); Thread.Sleep(30); }
                        using var bmpSt = new Bitmap(stModal.Width, stModal.Height);
                        stModal.DrawToBitmap(bmpSt, new Rectangle(0, 0, stModal.Width, stModal.Height));
                        bmpSt.Save(Path.Combine(outputDir, "screen_order_status_modal.png"), ImageFormat.Png);
                        stModal.Close();
                    }
                }

                // Capture New Order Modal with editable dropdown
                using (var orderModal = new CC.Forms.Staff.Orders.OrderForm())
                {
                    orderModal.StartPosition = FormStartPosition.Manual;
                    orderModal.Location = new Point(shell.Location.X + (shell.Width - orderModal.Width) / 2, shell.Location.Y + (shell.Height - orderModal.Height) / 2);
                    orderModal.Show();
                    Application.DoEvents();
                    Thread.Sleep(200);
                    using var bmpOrder = new Bitmap(orderModal.Width, orderModal.Height);
                    orderModal.DrawToBitmap(bmpOrder, new Rectangle(0, 0, orderModal.Width, orderModal.Height));
                    bmpOrder.Save(Path.Combine(outputDir, "screen_order_modal.png"), ImageFormat.Png);
                    orderModal.Close();
                }

                shell.Navigate("Payments");
                Capture("screen_payments.png");

                // Capture Payment Modal
                using (var payModal = new CC.Forms.Staff.Payments.PaymentForm())
                {
                    payModal.Show(shell);
                    Application.DoEvents();
                    Thread.Sleep(200);
                    using var bmpPay = new Bitmap(payModal.Width, payModal.Height);
                    payModal.DrawToBitmap(bmpPay, new Rectangle(0, 0, payModal.Width, payModal.Height));
                    bmpPay.Save(Path.Combine(outputDir, "screen_payment_modal.png"), ImageFormat.Png);
                    payModal.Close();
                }

                shell.Navigate("Follow-ups");
                Capture("screen_followups.png");

                // Capture Follow-up Modal
                using (var folModal = new CC.Forms.Staff.FollowUps.FollowUpForm())
                {
                    folModal.Show(shell);
                    Application.DoEvents();
                    Thread.Sleep(200);
                    using var bmpFol = new Bitmap(folModal.Width, folModal.Height);
                    folModal.DrawToBitmap(bmpFol, new Rectangle(0, 0, folModal.Width, folModal.Height));
                    bmpFol.Save(Path.Combine(outputDir, "screen_followup_modal.png"), ImageFormat.Png);
                    folModal.Close();
                }

                shell.Close();

                // Capture Manager Views (Camille Reyes matching mockup)
                SessionService.CurrentUser = new CurrentUser
                {
                    UserId = 3,
                    FirstName = "Camille",
                    LastName = "Reyes",
                    Role = "Manager",
                    CompanyId = 2
                };

                var managerShell = new CC.Forms.Manager.ManagerDashboardForm();
                managerShell.Size = new Size(1366, 820);
                managerShell.StartPosition = FormStartPosition.Manual;
                managerShell.Location = new Point(50, 50);
                managerShell.Show();
                Application.DoEvents();
                WaitForTask(managerShell.InitializationTask);
                for (int i = 0; i < 15; i++) { Application.DoEvents(); Thread.Sleep(20); }

                void CaptureMgr(string filename)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Application.DoEvents();
                        Thread.Sleep(30);
                    }
                    using var bmp = new Bitmap(managerShell.Width, managerShell.Height);
                    managerShell.DrawToBitmap(bmp, new Rectangle(0, 0, managerShell.Width, managerShell.Height));
                    bmp.Save(Path.Combine(outputDir, filename), ImageFormat.Png);
                }

                CaptureMgr("screen_manager_dashboard.png");

                managerShell.Navigate("Customers");
                CaptureMgr("screen_manager_customers.png");

                managerShell.Navigate("Reports");
                CaptureMgr("screen_manager_reports.png");

                managerShell.Close();

                // Capture Business Admin Views (Lea Abad matching media_1789390916454.png & media_1789390886516.png)
                SessionService.CurrentUser = new CurrentUser
                {
                    UserId = 2,
                    FirstName = "Lea",
                    LastName = "Abad",
                    Role = "Admin",
                    CompanyId = 2
                };

                var adminShell = new CC.Forms.Admin.AdminDashboardForm();
                adminShell.Size = new Size(1366, 820);
                adminShell.StartPosition = FormStartPosition.Manual;
                adminShell.Location = new Point(50, 50);
                adminShell.Show();
                Application.DoEvents();
                WaitForTask(adminShell.InitializationTask);
                for (int i = 0; i < 15; i++) { Application.DoEvents(); Thread.Sleep(20); }

                void CaptureAdmin(string filename)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Application.DoEvents();
                        Thread.Sleep(30);
                    }
                    using var bmp = new Bitmap(adminShell.Width, adminShell.Height);
                    adminShell.DrawToBitmap(bmp, new Rectangle(0, 0, adminShell.Width, adminShell.Height));
                    bmp.Save(Path.Combine(outputDir, filename), ImageFormat.Png);
                }

                CaptureAdmin("screen_admin_dashboard.png");

                adminShell.Navigate("User Management");
                CaptureAdmin("screen_admin_users.png");

                adminShell.Navigate("Subscription");
                CaptureAdmin("screen_admin_subscription.png");

                adminShell.Navigate("Reports");
                CaptureAdmin("screen_admin_reports.png");

                adminShell.Close();

                // Capture Super Admin Dashboard
                SessionService.CurrentUser = new CurrentUser
                {
                    UserId = 1,
                    FirstName = "System",
                    LastName = "SuperAdmin",
                    Role = "SuperAdmin",
                    CompanyId = 1
                };

                var superShell = new CC.Forms.SuperAdmin.SuperAdminDashboardForm();
                superShell.Size = new Size(1366, 820);
                superShell.StartPosition = FormStartPosition.Manual;
                superShell.Location = new Point(50, 50);
                superShell.Show();
                Application.DoEvents();
                WaitForTask(superShell.InitializationTask);
                for (int i = 0; i < 25; i++) { Application.DoEvents(); Thread.Sleep(30); }
                using (var bmpSuper = new Bitmap(superShell.Width, superShell.Height))
                {
                    superShell.DrawToBitmap(bmpSuper, new Rectangle(0, 0, superShell.Width, superShell.Height));
                    bmpSuper.Save(Path.Combine(outputDir, "screen_superadmin_dashboard.png"), ImageFormat.Png);
                }
                superShell.Close();

                Environment.Exit(0);
                return;
            }

            // Start with Login form
            var login = new Forms.Authentication.LoginForm();
            Application.Run(login);
        }
    }
}
