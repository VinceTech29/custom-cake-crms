using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Admin.Subscription;
using CC.Forms.Admin.Users;
using CC.Forms.Authentication;
using CC.Forms.Manager.Reports;
using CC.Forms.Staff.Customers;
using CC.Forms.Staff.FollowUps;
using CC.Forms.Staff.Inquiries;
using CC.Forms.Staff.Orders;
using CC.Forms.Staff.Payments;
using CC.Services;

namespace CC.Forms.Admin
{
    /// <summary>
    /// Business Admin / Owner Dashboard form:
    /// - Complete supervision of CRM modules: Customers, Inquiries, Orders, Payments, Follow-ups
    /// - Reports & Analytics (Daily, Monthly, Annually with CSV export)
    /// - User Management (Staff, Managers, Business Admin roles & access)
    /// - Subscription Management (Plans, seat usage, billing history, upgrade/downgrade)
    /// </summary>
    public class AdminDashboardForm : CC.Forms.Shared.DashboardShell
    {
        private string _currentView = "Dashboard";

        public AdminDashboardForm() : base("Dashboard")
        {
            PageTitle = "Dashboard";

            // Add specialized Business Admin navigation items to sidebar
            SidebarCtrl.AddNavItem("Reports", "\uE9F9");
            SidebarCtrl.AddNavItem("User Management", "\uE713");
            SidebarCtrl.AddNavItem("Subscription", "\uE734");

            ShowDashboard();
        }

        protected override void OnNavigationRequested(string key)
        {
            base.OnNavigationRequested(key);
            _currentView = key;

            switch (key)
            {
                case "Dashboard":
                    ShowDashboard();
                    break;
                case "Customers":
                    ViewHost.ShowFormInPanel(MainPanel, new CustomerListForm());
                    break;
                case "Inquiries":
                    ViewHost.ShowFormInPanel(MainPanel, new InquiryListForm());
                    break;
                case "Orders":
                    ViewHost.ShowFormInPanel(MainPanel, new OrderListForm());
                    break;
                case "Payments":
                    ViewHost.ShowFormInPanel(MainPanel, new PaymentListForm());
                    break;
                case "Follow-ups":
                    ViewHost.ShowFormInPanel(MainPanel, new FollowUpListForm());
                    break;
                case "Reports":
                    ViewHost.ShowFormInPanel(MainPanel, new ReportListForm());
                    break;
                case "User Management":
                    ViewHost.ShowFormInPanel(MainPanel, new UserListForm());
                    break;
                case "Subscription":
                    ViewHost.ShowFormInPanel(MainPanel, new SubscriptionForm());
                    break;
            }
        }

        private async void ShowDashboard()
        {
            _currentView = "Dashboard";
            MainPanel.Controls.Clear();

            // Dynamic live metrics calculation from database
            var metrics = await CrmDataService.GetDashboardMetricsAsync();
            var userMetrics = await CrmDataService.GetUserSummaryMetricsAsync();
            var subInfo = await CrmDataService.GetSubscriptionInfoAsync();

            if (_currentView != "Dashboard") return;

            int openInquiriesCount = metrics.OpenInquiriesCount;
            int ordersInProgressCount = metrics.OrdersInProgressCount;
            int overdueFollowupsCount = metrics.OverdueFollowupsCount;
            int followupsDueCount = metrics.FollowupsDueCount;
            int processingOrdersCount = metrics.ProcessingOrdersCount;
            int readyForPickupCount = metrics.ReadyForPickupCount;

            // Page Heading Block
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(0, 0, 0, 14),
                BackColor = Color.Transparent
            };

            var titleStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = "Business Admin Dashboard",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            var lblSubtitle = new Label
            {
                Text = $"Executive & Business Oversight \u00B7 {DateTime.Now:MMM d, yyyy}",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);
            topPanel.Controls.Add(titleStack);

            // Main 2-Column Responsive Layout Table
            var layoutTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F)); // Left Column
            layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F)); // Right Column
            layoutTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // ---- LEFT COLUMN: MAIN SUMMARY CARD ----
            var leftCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 310,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 16, 0),
                Cursor = Cursors.Hand
            };
            leftCard.ApplyRoundedRegion(14);

            leftCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (var pen = new Pen(UITheme.BorderColor, 1.2f))
                {
                    e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, leftCard.Width - 1, leftCard.Height - 1), 14);
                }

                using (var titleFont = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold))
                using (var titleBrush = new SolidBrush(UITheme.TextMuted))
                {
                    e.Graphics.DrawString("Open inquiries", titleFont, titleBrush, 24, 24);
                }

                using (var numFont = new Font(UITheme.FontSerif, 54F, FontStyle.Bold))
                using (var numBrush = new SolidBrush(UITheme.TextDark))
                {
                    e.Graphics.DrawString(openInquiriesCount.ToString(), numFont, numBrush, 20, 52);
                }

                using (var subFont = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular))
                using (var subBrush = new SolidBrush(Color.FromArgb(140, 130, 125)))
                {
                    e.Graphics.DrawString("Active inquiries awaiting quote or response", subFont, subBrush, 24, 142);
                }

                using (var divPen = new Pen(Color.FromArgb(240, 235, 230), 1f))
                {
                    e.Graphics.DrawLine(divPen, 24, 185, leftCard.Width - 24, 185);
                }

                // Team & Subscription quick indicators on bottom
                using (var teamFont = new Font(UITheme.FontSans, 9F, FontStyle.Bold))
                using (var teamBrush = new SolidBrush(UITheme.TextDark))
                {
                    e.Graphics.DrawString($"Active Team: {userMetrics.TotalActive} members", teamFont, teamBrush, 24, 210);
                    e.Graphics.DrawString($"Subscription: {subInfo.PlanName} ({subInfo.UsedSeats}/{subInfo.MaxSeats} seats)", teamFont, teamBrush, 24, 235);
                }

                // View link
                using (var linkFont = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold))
                using (var linkBrush = new SolidBrush(UITheme.PrimaryMauve))
                {
                    string actionText = "Manage Inquiries \u2192";
                    var size = e.Graphics.MeasureString(actionText, linkFont);
                    e.Graphics.DrawString(actionText, linkFont, linkBrush, leftCard.Width - size.Width - 24, 235);
                }
            };

            leftCard.Click += (s, e) =>
            {
                Navigate("Inquiries");
            };

            // ---- RIGHT COLUMN: 3 STAT CARDS ----
            var rightFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            var cardOrders = CreateStatCard("Orders in progress", ordersInProgressCount.ToString(),
                $"{processingOrdersCount} baking \u00B7 {readyForPickupCount} ready for pickup",
                "View all orders \u2192", () =>
                {
                    Navigate("Orders");
                });

            var cardFollowups = CreateStatCard("Follow-ups due", followupsDueCount.ToString(),
                $"{overdueFollowupsCount} overdue require attention",
                "View follow-ups \u2192", () =>
                {
                    Navigate("Follow-ups");
                });

            var cardUsers = CreateStatCard("Team Members", userMetrics.TotalActive.ToString(),
                $"{userMetrics.AdminCount} Admin \u00B7 {userMetrics.ManagerCount} Manager \u00B7 {userMetrics.StaffCount} Staff",
                "User Management \u2192", () =>
                {
                    Navigate("User Management");
                });

            rightFlow.Controls.Add(cardOrders);
            rightFlow.Controls.Add(cardFollowups);
            rightFlow.Controls.Add(cardUsers);

            layoutTable.Controls.Add(leftCard, 0, 0);
            layoutTable.Controls.Add(rightFlow, 1, 0);

            MainPanel.Controls.Add(layoutTable);
            MainPanel.Controls.Add(topPanel);
        }

        private Panel CreateStatCard(string title, string count, string subtitle, string actionText, Action onClick)
        {
            var card = new Panel
            {
                Width = 380,
                Height = 98,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 14),
                Cursor = Cursors.Hand
            };
            card.ApplyRoundedRegion(12);

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (var pen = new Pen(UITheme.BorderColor, 1.2f))
                {
                    e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
                }

                using (var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold))
                using (var brush = new SolidBrush(UITheme.TextMuted))
                {
                    e.Graphics.DrawString(title, font, brush, 16, 14);
                }

                using (var font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold))
                using (var brush = new SolidBrush(UITheme.TextDark))
                {
                    e.Graphics.DrawString(count, font, brush, 14, 32);
                }

                using (var font = new Font(UITheme.FontSans, 8F, FontStyle.Regular))
                using (var brush = new SolidBrush(Color.FromArgb(140, 130, 125)))
                {
                    e.Graphics.DrawString(subtitle, font, brush, 16, 70);
                }

                using (var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold))
                using (var brush = new SolidBrush(UITheme.PrimaryMauve))
                {
                    var sz = e.Graphics.MeasureString(actionText, font);
                    e.Graphics.DrawString(actionText, font, brush, card.Width - sz.Width - 16, 70);
                }
            };

            card.Click += (s, e) => onClick();
            return card;
        }
    }
}
