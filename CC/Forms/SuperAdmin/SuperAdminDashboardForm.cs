using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Admin.Users;
using CC.Forms.Authentication;
using CC.Forms.SuperAdmin.Businesses;
using CC.Services;

namespace CC.Forms.SuperAdmin
{
    /// <summary>
    /// Role-specific Super Admin Dashboard:
    /// Focuses on multi-tenant platform-level management using MSME_MasterCRM database:
    /// registered businesses, active tenant database mappings, subscription plan distribution,
    /// cross-tenant user statistics, and tenant registration trends with period filtering.
    /// </summary>
    public class SuperAdminDashboardForm : CC.Forms.Shared.DashboardShell
    {
        private string _activePeriod = "30d";
        private Panel _contentScrollPanel = null!;
        public Task InitializationTask { get; private set; } = Task.CompletedTask;

        public SuperAdminDashboardForm() : base("Super Admin Dashboard")
        {
            PageTitle = "Platform Dashboard";

            // Configure Super Admin specialized navigation items
            SidebarCtrl.ResetNavItems(new[]
            {
                ("Dashboard", "\uE80F"),
                ("Businesses", "\uE716"),
                ("Subscriptions", "\uE8C7"),
                ("Terms & Conditions", "\uE8D7"),
                ("System Monitoring & Backups", "\uE770"),
                ("Users", "\uE77B")
            });
            SidebarCtrl.SetActiveItem("Dashboard");

            BuildDashboardShell();
            InitializationTask = LoadDashboardDataAsync();
        }

        protected override void OnNavigationRequested(string key, object? filterContext)
        {
            base.OnNavigationRequested(key, filterContext);

            string? filterStr = filterContext as string;

            switch (key)
            {
                case "Dashboard":
                    BuildDashboardShell();
                    _ = LoadDashboardDataAsync();
                    break;
                case "Businesses":
                    ViewHost.ShowFormInPanel(MainPanel, new BusinessListForm());
                    break;
                case "Subscriptions":
                    int targetTab = filterStr == "Plans" ? 0 : (string.IsNullOrWhiteSpace(filterStr) ? 0 : 1);
                    string? statusFilter = (filterStr == "Active" || filterStr == "Expiring") ? filterStr : null;
                    ViewHost.ShowFormInPanel(MainPanel, new CC.Forms.SuperAdmin.Subscriptions.SuperAdminSubscriptionForm(targetTab, statusFilter));
                    break;
                case "Terms & Conditions":
                    ViewHost.ShowFormInPanel(MainPanel, new CC.Forms.SuperAdmin.Terms.TermsAndConditionsForm());
                    break;
                case "System Monitoring & Backups":
                    ViewHost.ShowFormInPanel(MainPanel, new CC.Forms.SuperAdmin.Monitoring.SystemMonitoringBackupsForm());
                    break;
                case "Users":
                    ViewHost.ShowFormInPanel(MainPanel, new UserListForm());
                    break;
                case "Sign Out":
                    OnSignOutRequested();
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
        }

        private async Task LoadDashboardDataAsync()
        {
            _contentScrollPanel.Controls.Clear();

            var data = await CrmDataService.GetSuperAdminDashboardDataAsync(_activePeriod);

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 16, 24)
            };
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // 1. TOP HEADER & PERIOD FILTER
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };

            var titleStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = "Platform Administration",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };

            var lblSubtitle = new Label
            {
                Text = "Supervise multi-tenant businesses, system users, database routing, and platform health",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            var periodSelector = new PeriodSelectorControl(new[] { "7d", "30d", "90d", "all" })
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

            // 2. 8 KPI STAT CARDS (4x2 GRID)
            var kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 290,
                ColumnCount = 4,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 20)
            };
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            kpiTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            // Row 1: Platform Infrastructure, Users & System Health
            var cardBusinesses = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Total Businesses",
                Value = data.TotalBusinesses.ToString(),
                Subtitle = $"Active: {data.ActiveBusinesses} tenants",
                Margin = new Padding(0, 0, 6, 6)
            };
            cardBusinesses.SetBadge("Master CRM", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardBusinesses.CardClicked += (s, e) => Navigate("Businesses");

            var cardUsers = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Platform Users",
                Value = data.PlatformUsersCount.ToString(),
                Subtitle = "Across all registered tenants",
                Margin = new Padding(6, 0, 6, 6)
            };
            cardUsers.SetBadge("Access", Color.FromArgb(50, 130, 200), Color.FromArgb(235, 243, 250));
            cardUsers.CardClicked += (s, e) => Navigate("Users");

            var cardDatabases = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Tenant Databases",
                Value = data.ActiveDatabases.ToString(),
                Subtitle = "Dedicated SQL DB instances",
                Margin = new Padding(6, 0, 6, 6)
            };
            cardDatabases.SetBadge("Multi-Tenant", UITheme.UpgradeGold, Color.FromArgb(253, 248, 238));
            cardDatabases.CardClicked += (s, e) => Navigate("System Monitoring & Backups");

            var cardBackups = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Database Backups",
                Value = $"{data.TotalBackupsCount} Files",
                Subtitle = "Native SQL multi-tenant archives",
                Margin = new Padding(6, 0, 0, 6)
            };
            cardBackups.SetBadge("System Health", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardBackups.CardClicked += (s, e) => Navigate("System Monitoring & Backups");

            // Row 2: Subscriptions, Commercial Revenue & Compliance
            var cardSubs = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Active Subscriptions",
                Value = data.ActiveSubscriptionsCount.ToString(),
                Subtitle = "Active tenant accounts",
                Margin = new Padding(0, 6, 6, 0)
            };
            cardSubs.SetBadge("Platform", UITheme.PrimaryMauve, Color.FromArgb(245, 235, 240));
            cardSubs.CardClicked += (s, e) => Navigate("Subscriptions", "Active");

            var cardExpiring = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Expiring Plans (30d)",
                Value = data.ExpiringSubscriptionsCount.ToString(),
                Subtitle = $"{data.ExpiredSubscriptionsCount} expired accounts",
                Margin = new Padding(6, 6, 6, 0)
            };
            cardExpiring.SetBadge("Renewal Alert", UITheme.StatusYellowFg, UITheme.StatusYellowBg);
            cardExpiring.CardClicked += (s, e) => Navigate("Subscriptions", "Expiring");

            var cardMrr = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Est. Monthly Revenue",
                Value = $"P{data.EstimatedMonthlyRevenue:N0}",
                Subtitle = "Active subscription MRR",
                Margin = new Padding(6, 6, 6, 0)
            };
            cardMrr.SetBadge("MRR", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardMrr.CardClicked += (s, e) => Navigate("Subscriptions", "Plans");

            var cardTerms = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Terms Compliance",
                Value = $"{data.TermsAcceptancesCount} Signed",
                Subtitle = "User acceptances logged",
                Margin = new Padding(6, 6, 0, 0)
            };
            cardTerms.SetBadge("Compliance", UITheme.PrimaryMauve, Color.FromArgb(245, 235, 240));
            cardTerms.CardClicked += (s, e) => Navigate("Terms & Conditions");

            kpiTable.Controls.Add(cardBusinesses, 0, 0);
            kpiTable.Controls.Add(cardUsers, 1, 0);
            kpiTable.Controls.Add(cardDatabases, 2, 0);
            kpiTable.Controls.Add(cardBackups, 3, 0);
            kpiTable.Controls.Add(cardSubs, 0, 1);
            kpiTable.Controls.Add(cardExpiring, 1, 1);
            kpiTable.Controls.Add(cardMrr, 2, 1);
            kpiTable.Controls.Add(cardTerms, 3, 1);
            rootLayout.Controls.Add(kpiTable, 0, 1);

            // 3. ANALYTICS CHARTS (2 Columns: Registration Trend & Subscription Plans)
            var chartsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 310,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 20)
            };
            chartsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
            chartsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));

            var trendChart = new DashboardTrendChart
            {
                Dock = DockStyle.Fill,
                ChartTitle = "Business Registration Trends",
                ChartSubtitle = "New business onboarding over selected period",
                IsCurrency = false,
                Margin = new Padding(0, 0, 10, 0)
            };
            trendChart.SetData(data.RegistrationTrend);

            var planChart = new DashboardBarChart
            {
                Dock = DockStyle.Fill,
                ChartTitle = "Subscription Plan Distribution",
                ChartSubtitle = "Platform tenant tier breakdown",
                Margin = new Padding(10, 0, 0, 0)
            };
            planChart.SetData(data.SubscriptionPlanDistribution);

            chartsTable.Controls.Add(trendChart, 0, 0);
            chartsTable.Controls.Add(planChart, 1, 0);
            rootLayout.Controls.Add(chartsTable, 0, 2);

            // 4. TENANT BUSINESS REGISTRY TABLE
            var registryPanel = BuildTenantRegistryPanel(data);
            rootLayout.Controls.Add(registryPanel, 0, 3);

            _contentScrollPanel.Controls.Add(rootLayout);
        }

        private Panel BuildTenantRegistryPanel(SuperAdminDashboardData data)
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                Height = 320,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = new Padding(0, 0, 0, 24)
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
                Text = "Registered Tenant Businesses & Database Allocations",
                Font = new Font(UITheme.FontSerif, 12.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };

            var lblSubtitle = new Label
            {
                Text = $"{data.RecentRegistrations.Count} dedicated tenant database instances active on platform",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            headerLeft.Controls.Add(lblTitle);
            headerLeft.Controls.Add(lblSubtitle);

            var linkManage = new Label
            {
                Text = "Manage All Businesses \u2192",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Dock = DockStyle.Right,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Padding = new Padding(0, 14, 0, 0)
            };
            linkManage.Click += (s, e) => Navigate("Businesses");

            var badgePlatform = new Label
            {
                Text = "Database-per-Tenant",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = UITheme.StatusGreenFg,
                BackColor = UITheme.StatusGreenBg,
                Dock = DockStyle.Right,
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                Margin = new Padding(0, 14, 16, 0)
            };
            badgePlatform.Paint += (s, e) =>
            {
                using var borderPen = new Pen(Color.FromArgb(40, UITheme.StatusGreenFg), 1f);
                e.Graphics.DrawRoundedRectangle(borderPen, new Rectangle(0, 0, badgePlatform.Width - 1, badgePlatform.Height - 1), 10);
            };

            headerPanel.Controls.Add(linkManage);
            headerPanel.Controls.Add(badgePlatform);
            headerPanel.Controls.Add(headerLeft);
            card.Controls.Add(headerPanel);

            if (data.RecentRegistrations == null || data.RecentRegistrations.Count == 0)
            {
                var emptyLbl = new Label
                {
                    Text = "No registered tenant businesses found in Master CRM.",
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

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCode", HeaderText = "COMPANY CODE", Width = 140, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "COMPANY NAME", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 200, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDb", HeaderText = "TENANT DATABASE", Width = 200, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "REGISTERED DATE", Width = 150, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS", Width = 125, SortMode = DataGridViewColumnSortMode.NotSortable });

            foreach (var item in data.RecentRegistrations)
            {
                int rowIdx = grid.Rows.Add(
                    item.CompanyCode,
                    item.CompanyName,
                    item.DatabaseName,
                    item.CreatedDate.ToString("MMM d, yyyy"),
                    item.IsActive ? "Active" : "Inactive");
                grid.Rows[rowIdx].Tag = item;
            }

            grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics == null) return;
                var item = grid.Rows[e.RowIndex].Tag as SuperAdminCompanyItem;
                if (item == null) return;

                e.PaintBackground(e.CellBounds, true);
                var g = e.Graphics;
                int px5 = 20;
                int cellY = e.CellBounds.Top + (e.CellBounds.Height - 20) / 2;

                if (e.ColumnIndex == 0) // CODE
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                    using var brush = new SolidBrush(UITheme.PrimaryMauve);
                    g.DrawString(item.CompanyCode, font, brush, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 1) // NAME
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                    g.DrawString(item.CompanyName, font, Brushes.Black, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 2) // DATABASE
                {
                    using var font = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                    using var brush = new SolidBrush(UITheme.TextDark);
                    g.DrawString(item.DatabaseName, font, brush, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 3) // DATE
                {
                    using var font = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                    using var brush = new SolidBrush(UITheme.TextMuted);
                    g.DrawString(item.CreatedDate.ToString("MMM d, yyyy"), font, brush, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 4) // STATUS
                {
                    Color bg = item.IsActive ? UITheme.StatusGreenBg : UITheme.StatusRedBg;
                    Color fg = item.IsActive ? UITheme.StatusGreenFg : UITheme.StatusRedFg;
                    UITheme.DrawStatusBadge(g, e.CellBounds, item.IsActive ? "Active" : "Inactive", bg, fg, showDot: true);
                    e.Handled = true;
                }
            };

            grid.CellClick += (s, e) => Navigate("Businesses");

            grid.ClearSelection();
            card.Controls.Add(grid);
            grid.BringToFront();
            return card;
        }
    }
}
