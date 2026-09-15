using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Forms.Manager.Reports;
using CC.Forms.Staff.Customers;
using CC.Forms.Staff.FollowUps;
using CC.Forms.Staff.Inquiries;
using CC.Forms.Staff.Orders;
using CC.Forms.Staff.Payments;
using CC.Services;

namespace CC.Forms.Manager
{
    /// <summary>
    /// Manager Dashboard form matching the Sweet Story CRM design mockup.
    /// Provides full parity with Staff modules plus Reports & Analytics with transaction export.
    /// </summary>
    public class ManagerDashboardForm : CC.Forms.Shared.DashboardShell
    {
        private string _currentView = "Dashboard";

        public ManagerDashboardForm() : base("Dashboard")
        {
            PageTitle = "Dashboard";

            // Add Reports navigation item to sidebar (Bar Chart icon \uE9F9)
            SidebarCtrl.AddNavItem("Reports", "\uE9F9");

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
            }
        }

        private async void ShowDashboard()
        {
            _currentView = "Dashboard";
            MainPanel.Controls.Clear();

            // Dynamic live metrics calculation from database
            var metrics = await CrmDataService.GetDashboardMetricsAsync();
            if (_currentView != "Dashboard") return;
            int openInquiriesCount = metrics.OpenInquiriesCount;
            int ordersInProgressCount = metrics.OrdersInProgressCount;
            int overdueFollowupsCount = metrics.OverdueFollowupsCount;
            int followupsDueCount = metrics.FollowupsDueCount;
            int processingOrdersCount = metrics.ProcessingOrdersCount;
            int readyForPickupCount = metrics.ReadyForPickupCount;

            // Page Heading Block (calibrated: text-2xl, subtitle text-sm)
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
                Text = "Manager Dashboard",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            var lblSubtitle = new Label
            {
                Text = $"Executive Overview \u00B7 {DateTime.Now:MMM d, yyyy}",
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
            layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F)); // Left Column (Main Card)
            layoutTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F)); // Right Column (Card Stack)
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

                // Border
                using (var pen = new Pen(UITheme.BorderColor, 1.2f))
                {
                    e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, leftCard.Width - 1, leftCard.Height - 1), 14);
                }

                // Title "Open inquiries"
                using (var titleFont = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold))
                using (var titleBrush = new SolidBrush(UITheme.TextMuted))
                {
                    e.Graphics.DrawString("Open inquiries", titleFont, titleBrush, 24, 24);
                }

                // Large Stat Number
                using (var statFont = new Font(UITheme.FontSerif, 42F, FontStyle.Bold))
                using (var statBrush = new SolidBrush(UITheme.TextDark))
                {
                    e.Graphics.DrawString(openInquiriesCount.ToString(), statFont, statBrush, 20, 52);
                }

                // Green Status Pill "~ Needs handling"
                UITheme.DrawStatusPill(e.Graphics, new Rectangle(24, 138, 120, 26), "~ Needs handling",
                    UITheme.StatusGreenBg, UITheme.StatusGreenFg);

                // Trend line chart graphic (Gold/Tan #C7963C)
                using (var chartPen = new Pen(UITheme.UpgradeGold, 2.2f))
                {
                    Point[] points =
                    {
                        new Point(leftCard.Width - 140, 110),
                        new Point(leftCard.Width - 124, 86),
                        new Point(leftCard.Width - 108, 104),
                        new Point(leftCard.Width - 92, 68),
                        new Point(leftCard.Width - 76, 92),
                        new Point(leftCard.Width - 60, 54),
                        new Point(leftCard.Width - 44, 110)
                    };
                    e.Graphics.DrawLines(chartPen, points);
                }

                // Horizontal Divider
                using (var divPen = new Pen(UITheme.BorderColor, 1f))
                {
                    e.Graphics.DrawLine(divPen, 24, 195, leftCard.Width - 24, 195);
                }

                // Bottom Stats Row
                // Stat 1: Orders in progress
                using (var labelFont = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold))
                using (var labelBrush = new SolidBrush(UITheme.TextMuted))
                {
                    e.Graphics.DrawString("Orders in progress", labelFont, labelBrush, 24, 212);
                }
                using (var numFont = new Font(UITheme.FontSerif, 18F, FontStyle.Bold))
                using (var numBrush = new SolidBrush(UITheme.TextDark))
                {
                    e.Graphics.DrawString(ordersInProgressCount.ToString(), numFont, numBrush, 24, 234);
                }

                // Stat 2: Overdue follow-ups
                using (var labelFont = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold))
                using (var labelBrush = new SolidBrush(UITheme.TextMuted))
                {
                    e.Graphics.DrawString("Overdue follow-ups", labelFont, labelBrush, 170, 212);
                }
                using (var numFont = new Font(UITheme.FontSerif, 18F, FontStyle.Bold))
                using (var numBrush = new SolidBrush(overdueFollowupsCount > 0 ? UITheme.StatusRedFg : UITheme.TextDark))
                {
                    e.Graphics.DrawString(overdueFollowupsCount.ToString(), numFont, numBrush, 170, 234);
                }
            };

            leftCard.Resize += (s, e) => leftCard.Invalidate();
            leftCard.Click += (s, e) => { SidebarCtrl?.SetActiveItem("Inquiries"); OnNavigationRequested("Inquiries"); };

            // ---- RIGHT COLUMN: STACK OF 3 STAT CARDS ----
            var rightStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var cardFollowups = CreateStatCard("Follow-ups due", followupsDueCount.ToString(), "~ Schedule pending", UITheme.StatusGreenBg, UITheme.StatusGreenFg);
            cardFollowups.Click += (s, e) => { SidebarCtrl?.SetActiveItem("Follow-ups"); OnNavigationRequested("Follow-ups"); };

            var cardProcessing = CreateStatCard("Processing orders", processingOrdersCount.ToString(), "~ In the oven", UITheme.StatusGreenBg, UITheme.StatusGreenFg);
            cardProcessing.Click += (s, e) => { SidebarCtrl?.SetActiveItem("Orders"); OnNavigationRequested("Orders"); };

            var cardReady = CreateStatCard("Ready for pickup", readyForPickupCount.ToString(), "~ Notify customers", UITheme.StatusGreenBg, UITheme.StatusGreenFg);
            cardReady.Click += (s, e) => { SidebarCtrl?.SetActiveItem("Orders"); OnNavigationRequested("Orders"); };

            rightStack.Controls.Add(cardFollowups);
            rightStack.Controls.Add(cardProcessing);
            rightStack.Controls.Add(cardReady);

            layoutTable.Controls.Add(leftCard, 0, 0);
            layoutTable.Controls.Add(rightStack, 1, 0);

            MainPanel.Controls.Add(layoutTable);
            MainPanel.Controls.Add(topPanel);
        }

        private Panel CreateStatCard(string title, string countStr, string pillText, Color pillBg, Color pillFg)
        {
            var card = new Panel
            {
                Height = 92,
                Width = 380,
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

                // Title
                using (var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold))
                using (var brush = new SolidBrush(UITheme.TextMuted))
                {
                    e.Graphics.DrawString(title, font, brush, 18, 14);
                }

                // Count Number
                using (var font = new Font(UITheme.FontSerif, 24F, FontStyle.Bold))
                using (var brush = new SolidBrush(UITheme.TextDark))
                {
                    e.Graphics.DrawString(countStr, font, brush, 16, 38);
                }

                // Top-right Pill Badge
                int pillWidth = 126;
                UITheme.DrawStatusPill(e.Graphics, new Rectangle(card.Width - pillWidth - 16, 14, pillWidth, 24),
                    pillText, pillBg, pillFg);
            };

            card.Resize += (s, e) => card.Invalidate();

            return card;
        }
    }
}
