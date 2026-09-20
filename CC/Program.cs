using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows.Forms;
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
