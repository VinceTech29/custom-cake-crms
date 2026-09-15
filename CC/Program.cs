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

                var shell = new StaffDashboardForm();
                shell.Size = new Size(1366, 820);
                shell.StartPosition = FormStartPosition.Manual;
                shell.Location = new Point(50, 50);
                shell.Show();
                Application.DoEvents();
                Thread.Sleep(300);

                void Capture(string filename)
                {
                    Application.DoEvents();
                    Thread.Sleep(200);
                    using var bmp = new Bitmap(shell.Width, shell.Height);
                    shell.DrawToBitmap(bmp, new Rectangle(0, 0, shell.Width, shell.Height));
                    bmp.Save(Path.Combine(outputDir, filename), ImageFormat.Png);
                }

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
                    orderListForm.ShowOrderDetailsAsync(procOrder.OrderId).GetAwaiter().GetResult();
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
                Thread.Sleep(300);

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
                Thread.Sleep(300);

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

                adminShell.Navigate("User Management");
                CaptureAdmin("screen_admin_users.png");

                adminShell.Navigate("Subscription");
                CaptureAdmin("screen_admin_subscription.png");

                adminShell.Navigate("Reports");
                CaptureAdmin("screen_admin_reports.png");

                adminShell.Close();
                Environment.Exit(0);
                return;
            }

            // Start with Login form
            var login = new Forms.Authentication.LoginForm();
            Application.Run(login);
        }
    }
}
