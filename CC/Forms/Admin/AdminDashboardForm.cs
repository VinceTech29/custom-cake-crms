using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
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
    /// Role-specific Business Admin Dashboard:
    /// Focuses on overall business performance: total revenue collected, outstanding balances,
    /// revenue trends, payment methods, order status distribution, top spending customers, and subscription information.
    /// </summary>
    public class AdminDashboardForm : CC.Forms.Shared.DashboardShell
    {
        private string _activePeriod = "30d";
        private Panel _contentScrollPanel = null!;
        public Task InitializationTask { get; private set; } = Task.CompletedTask;

        public AdminDashboardForm() : base("Business Admin")
        {
            PageTitle = "Business Admin Dashboard";
            SidebarCtrl.AddNavItem("Reports", "\uE9F9");
            SidebarCtrl.AddNavItem("Retention & Campaigns", "\uE715");
            SidebarCtrl.AddNavItem("User Management", "\uE716");
            SidebarCtrl.AddNavItem("Subscription", "\uE8C7");
            SidebarCtrl.SetActiveItem("Dashboard");
            BuildDashboardShell();
            InitializationTask = LoadDashboardDataAsync();
        }

        protected override void OnNavigationRequested(string key)
        {
            base.OnNavigationRequested(key);

            switch (key)
            {
                case "Dashboard":
                    BuildDashboardShell();
                    _ = LoadDashboardDataAsync();
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
                case "Retention & Campaigns":
                    ViewHost.ShowFormInPanel(MainPanel, new CC.Forms.Retention.RetentionCampaignsForm());
                    break;
                case "User Management":
                    ViewHost.ShowFormInPanel(MainPanel, new UserListForm());
                    break;
                case "Subscription":
                    ViewHost.ShowFormInPanel(MainPanel, new SubscriptionForm());
                    break;
            }
        }

        private void BuildDashboardShell()
        {
            ViewHost.ClearHostedForm(MainPanel);
            MainPanel.Controls.Clear();

            if (_contentScrollPanel == null)
            {
                _contentScrollPanel = new Panel
                {
                    Dock = DockStyle.Fill,
                    AutoScroll = true,
                    BackColor = Color.Transparent
                };
            }

            _contentScrollPanel.Visible = true;
            MainPanel.Controls.Add(_contentScrollPanel);
            _contentScrollPanel.BringToFront();
        }

        private async Task LoadDashboardDataAsync()
        {
            if (IsDisposed) return;
            try
            {
                var data = await CrmDataService.GetAdminDashboardDataAsync(
                    companyId: SessionService.CurrentUser?.CompanyId,
                    period: _activePeriod);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header + Period
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // KPI Cards
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Charts
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Top Customers & Subscription

            // 1. TOP HEADER & PERIOD FILTER
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
                Dock = DockStyle.Left,
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
                Text = $"Executive Financial & CRM Intelligence \u00B7 {DateTime.Now:MMM d, yyyy}",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            var periodSelector = new PeriodSelectorControl(PeriodSelectorControl.AdminPeriods)
            {
                Dock = DockStyle.Right,
                SelectedPeriod = _activePeriod
            };
            periodSelector.PeriodChanged += (s, newPeriod) =>
            {
                _activePeriod = newPeriod;
                _ = LoadDashboardDataAsync();
            };

            topPanel.Controls.Add(periodSelector);
            topPanel.Controls.Add(titleStack);
            rootLayout.Controls.Add(topPanel, 0, 0);

            // 2. 8 FINANCIAL & CRM KPI CARDS (4x2 GRID)
            var kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 2,
                Height = 290,
                Margin = new Padding(0, 0, 0, 18),
                BackColor = Color.Transparent
            };
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            kpiTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Row 1: Financial Health & Unit Economics
            var cardRevenue = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Revenue Collected",
                Value = $"P{data.TotalRevenue:N0}",
                Subtitle = $"Lifetime: P{data.LifetimeRevenue:N0}",
                Margin = new Padding(0, 0, 6, 6)
            };
            cardRevenue.SetBadge("Collected", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardRevenue.CardClicked += (s, e) => Navigate("Payments");

            var cardOutstanding = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Outstanding Balance",
                Value = $"P{data.OutstandingBalance:N0}",
                Subtitle = "Uncollected on active orders",
                Margin = new Padding(6, 0, 6, 6)
            };
            cardOutstanding.SetBadge("Receivables", UITheme.StatusYellowFg, UITheme.StatusYellowBg);
            cardOutstanding.CardClicked += (s, e) => Navigate("Payments");

            var cardAov = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Average Order Value",
                Value = $"P{data.AverageOrderValue:N0}",
                Subtitle = "Avg spend per placed order",
                Margin = new Padding(6, 0, 6, 6)
            };
            cardAov.SetBadge("Unit Econ", UITheme.PrimaryMauve, Color.FromArgb(245, 235, 240));
            cardAov.CardClicked += (s, e) => Navigate("Orders");

            var cardCollectionRate = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Collection Rate",
                Value = $"{data.CollectionRate:0.#}%",
                Subtitle = "Collected vs billed order total",
                Margin = new Padding(6, 0, 0, 6)
            };
            cardCollectionRate.SetBadge("Efficiency", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardCollectionRate.CardClicked += (s, e) => Navigate("Payments");

            // Row 2: Customer Base, Orders, Production & Seats
            var cardCustomers = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Customer Base",
                Value = data.TotalCustomers.ToString(),
                Subtitle = $"+{data.NewCustomersInPeriod} new registered",
                Margin = new Padding(0, 6, 6, 0)
            };
            cardCustomers.SetBadge("CRM", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardCustomers.CardClicked += (s, e) => Navigate("Customers");

            var cardOrders = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Total Sales Orders",
                Value = data.TotalOrders.ToString(),
                Subtitle = "Placed within timeframe",
                Margin = new Padding(6, 6, 6, 0)
            };
            cardOrders.SetBadge("Orders", UITheme.StatusBlueFg, UITheme.StatusBlueBg);
            cardOrders.CardClicked += (s, e) => Navigate("Orders");

            var cardActiveOrders = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Active In-Production",
                Value = data.ActiveOrdersCount.ToString(),
                Subtitle = "Orders currently in kitchen",
                Margin = new Padding(6, 6, 6, 0)
            };
            cardActiveOrders.SetBadge("Kitchen", UITheme.StatusYellowFg, UITheme.StatusYellowBg);
            cardActiveOrders.CardClicked += (s, e) => Navigate("Orders");

            var cardSeats = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Plan Seats Used",
                Value = $"{data.Subscription.UsedSeats} / {data.Subscription.MaxSeats}",
                Subtitle = $"{data.Subscription.PlanName} tier",
                Margin = new Padding(6, 6, 0, 0)
            };
            cardSeats.SetBadge("License", UITheme.PrimaryMauve, Color.FromArgb(245, 235, 240));
            cardSeats.CardClicked += (s, e) => Navigate("Subscription");

            kpiTable.Controls.Add(cardRevenue, 0, 0);
            kpiTable.Controls.Add(cardOutstanding, 1, 0);
            kpiTable.Controls.Add(cardAov, 2, 0);
            kpiTable.Controls.Add(cardCollectionRate, 3, 0);
            kpiTable.Controls.Add(cardCustomers, 0, 1);
            kpiTable.Controls.Add(cardOrders, 1, 1);
            kpiTable.Controls.Add(cardActiveOrders, 2, 1);
            kpiTable.Controls.Add(cardSeats, 3, 1);
            rootLayout.Controls.Add(kpiTable, 0, 1);

            // 3. CHARTS ROW (Revenue Trend + Payment Methods Breakdown)
            var chartsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Height = 280,
                Margin = new Padding(0, 0, 0, 18),
                BackColor = Color.Transparent
            };
            chartsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
            chartsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));

            var revTrendChart = new DashboardTrendChart
            {
                Dock = DockStyle.Fill,
                ChartTitle = "Revenue Growth Trend",
                ChartSubtitle = "Daily collected cashflow across payment methods",
                IsCurrency = true,
                Margin = new Padding(0, 0, 10, 0)
            };
            revTrendChart.SetData(data.RevenueTrend, isCurrency: true);

            var methodChart = new DashboardBarChart
            {
                Dock = DockStyle.Fill,
                ChartTitle = "Payment Channels",
                ChartSubtitle = "Revenue breakdown by payment method",
                Margin = new Padding(10, 0, 0, 0)
            };
            methodChart.SetData(data.PaymentMethodBreakdown);
            methodChart.CategoryClicked += (s, cat) => Navigate("Payments");

            chartsTable.Controls.Add(revTrendChart, 0, 0);
            chartsTable.Controls.Add(methodChart, 1, 0);
            rootLayout.Controls.Add(chartsTable, 0, 2);

            // 4. BOTTOM ROW: TOP CUSTOMERS TABLE + SUBSCRIPTION WIDGET
            var bottomTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Height = 360,
                Margin = new Padding(0, 0, 0, 24),
                BackColor = Color.Transparent
            };
            bottomTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            bottomTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));

            var topCustomersPanel = BuildTopCustomersPanel(data);
            var subscriptionPanel = BuildSubscriptionWidget(data.Subscription);

            bottomTable.Controls.Add(topCustomersPanel, 0, 0);
            bottomTable.Controls.Add(subscriptionPanel, 1, 0);
            rootLayout.Controls.Add(bottomTable, 0, 3);

            if (IsDisposed) return;
            _contentScrollPanel.SuspendLayout();
            _contentScrollPanel.Controls.Clear();
            _contentScrollPanel.Controls.Add(rootLayout);
            _contentScrollPanel.ResumeLayout(true);
            }
            catch (Exception ex)
            {
                var errLbl = new Label
                {
                    Text = $"Error loading admin dashboard: {ex.Message}\n{ex.StackTrace}",
                    ForeColor = Color.Red,
                    Dock = DockStyle.Fill,
                    Font = new Font(UITheme.FontSans, 9F)
                };
                _contentScrollPanel.Controls.Clear();
                _contentScrollPanel.Controls.Add(errLbl);
                Console.WriteLine($"[ADMIN DASHBOARD ERROR] {ex}");
            }
        }

        private Panel BuildTopCustomersPanel(AdminDashboardData data)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = new Padding(0, 0, 10, 0)
            };
            card.ApplyRoundedRegion(16);

            card.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 16);
            };

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(22, 10, 22, 6)
            };

            var headerLeft = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = "Top Spending Customers",
                Font = new Font(UITheme.FontSerif, 12.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };

            var lblSubtitle = new Label
            {
                Text = $"{data.TopCustomers.Count} highest lifetime-value customer accounts",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            headerLeft.Controls.Add(lblTitle);
            headerLeft.Controls.Add(lblSubtitle);

            var btnViewAll = new Label
            {
                Text = "View All \u2192",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Dock = DockStyle.Right,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 16, 0, 0)
            };
            btnViewAll.Click += (s, e) => Navigate("Customers");

            headerPanel.Controls.Add(btnViewAll);
            headerPanel.Controls.Add(headerLeft);
            card.Controls.Add(headerPanel);

            if (data.TopCustomers == null || data.TopCustomers.Count == 0)
            {
                var emptyLbl = new Label
                {
                    Text = "No customer spend data available for this period.",
                    Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Italic),
                    ForeColor = UITheme.TextMuted,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                card.Controls.Add(emptyLbl);
                return card;
            }

            var grid = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyTableStyle(grid);
            grid.RowTemplate.Height = 50;
            grid.ColumnHeadersHeight = 42;

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 150, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrders", HeaderText = "ORDERS", Width = 80, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotalSpent", HeaderText = "TOTAL SPENT", Width = 125, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLastOrder", HeaderText = "LAST ORDER", Width = 115, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAction", HeaderText = "", Width = 75, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

            foreach (var c in data.TopCustomers)
            {
                int rowIdx = grid.Rows.Add(
                    c.CustomerName,
                    c.OrderCount,
                    $"P{c.TotalSpent:N2}",
                    c.LastOrderDate.ToString("MMM d, yyyy"),
                    "View \u2192");
                grid.Rows[rowIdx].Tag = c;
            }

            grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics == null) return;
                var c = grid.Rows[e.RowIndex].Tag as AdminTopCustomerItem;
                if (c == null) return;

                e.PaintBackground(e.CellBounds, true);
                var g = e.Graphics;
                int px5 = 20;
                int cellY = e.CellBounds.Top + (e.CellBounds.Height - 20) / 2;

                if (e.ColumnIndex == 0) // CUSTOMER
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                    g.DrawString(c.CustomerName, font, Brushes.Black, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 1) // ORDERS
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                    using var brush = new SolidBrush(UITheme.TextDark);
                    g.DrawString(c.OrderCount.ToString(), font, brush, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 2) // TOTAL SPENT
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                    g.DrawString($"\u20B1{c.TotalSpent:N2}", font, Brushes.Black, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 3) // LAST ORDER
                {
                    using var font = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                    using var brush = new SolidBrush(UITheme.TextMuted);
                    g.DrawString(c.LastOrderDate.ToString("MMM d, yyyy"), font, brush, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 4) // ACTION
                {
                    UITheme.DrawActionLink(g, e.CellBounds, "View \u2192");
                    e.Handled = true;
                }
            };

            grid.CellClick += (s, e) => Navigate("Customers");

            grid.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 4)
                    grid.Cursor = Cursors.Hand;
                else
                    grid.Cursor = Cursors.Default;
            };

            grid.ClearSelection();
            card.Controls.Add(grid);
            grid.BringToFront();
            return card;
        }

        private Panel BuildSubscriptionWidget(SubscriptionInfo sub)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(22, 18, 22, 18),
                Margin = new Padding(10, 0, 0, 0)
            };
            card.ApplyRoundedRegion(16);

            card.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 16);
            };

            var title = new Label
            {
                Text = "Plan & Team Licenses",
                Font = new Font(UITheme.FontSerif, 12F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Top,
                Height = 28
            };

            var planLabel = new Label
            {
                Text = sub.PlanName,
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Dock = DockStyle.Top,
                Height = 36
            };

            var statusBadge = new Label
            {
                Text = $"Status: {sub.Status} \u00B7 Renewal: {sub.RenewalDate:MMM d, yyyy}",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 22
            };

            var seatsLabel = new Label
            {
                Text = $"Active Seats: {sub.UsedSeats} / {sub.MaxSeats} users",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Top,
                Height = 26
            };

            // Seat Progress Track
            var trackPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 12,
                Margin = new Padding(0, 4, 0, 16)
            };
            trackPanel.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = new Rectangle(0, 0, trackPanel.Width - 1, trackPanel.Height - 1);
                using var bg = new SolidBrush(Color.FromArgb(245, 241, 237));
                g.FillRoundedRectangle(bg, rect, 6);

                float ratio = Math.Clamp((float)sub.UsedSeats / Math.Max(1, sub.MaxSeats), 0f, 1f);
                int filledW = (int)(ratio * rect.Width);
                if (filledW > 0)
                {
                    using var fill = new SolidBrush(UITheme.UpgradeGold);
                    g.FillRoundedRectangle(fill, new Rectangle(0, 0, filledW, rect.Height), 6);
                }
            };

            var btnManage = new Button
            {
                Text = "Manage Subscription \u2192",
                Dock = DockStyle.Bottom,
                Height = 38,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.TableHeaderTan,
                ForeColor = UITheme.TextDark,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnManage.FlatAppearance.BorderSize = 0;
            btnManage.Click += (s, e) => Navigate("Subscription");

            card.Controls.Add(btnManage);
            card.Controls.Add(trackPanel);
            card.Controls.Add(seatsLabel);
            card.Controls.Add(statusBadge);
            card.Controls.Add(planLabel);
            card.Controls.Add(title);

            return card;
        }
    }
}
