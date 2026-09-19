using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.SuperAdmin
{
    /// <summary>
    /// Role-specific Super Admin Dashboard:
    /// Focuses on multi-tenant platform-level management using MSME_MasterCRM database:
    /// registered businesses, active tenant database mappings, subscription plan distribution,
    /// cross-tenant user statistics, and tenant registration trends with period filtering.
    /// </summary>
    public class SuperAdminDashboardForm : Form
    {
        private string _activePeriod = "30d";
        private Panel _contentScrollPanel = null!;

        public Task InitializationTask { get; private set; } = Task.CompletedTask;

        public SuperAdminDashboardForm()
        {
            Text = "Super Admin Platform Dashboard";
            Width = 1366;
            Height = 820;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(1040, 680);
            BackColor = UITheme.CreamBackground;

            if (UITheme.AppIcon != null)
                Icon = UITheme.AppIcon;

            BuildShellLayout();
            InitializationTask = LoadDashboardDataAsync();
        }

        private void BuildShellLayout()
        {
            Controls.Clear();

            // 1. TOP APP BAR
            var topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                BackColor = Color.White,
                Padding = new Padding(32, 0, 32, 0)
            };
            topBar.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawLine(pen, 0, topBar.Height - 1, topBar.Width, topBar.Height - 1);
            };

            var logoStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = new Padding(0, 14, 0, 0)
            };

            var logoBadge = new Panel { Size = new Size(36, 36), Margin = new Padding(0, 0, 12, 0) };
            logoBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                if (UITheme.LogoImage != null)
                {
                    UITheme.DrawLogo(e.Graphics, new Rectangle(2, 2, 32, 32));
                }
                else
                {
                    using var b = new SolidBrush(UITheme.PrimaryMauve);
                    e.Graphics.FillRoundedRectangle(b, new Rectangle(0, 0, 36, 36), 8);
                }
            };

            var brandTitle = new Label
            {
                Text = "Custom Cake CRMS \u00B7 Master Platform",
                Font = new Font(UITheme.FontSerif, 12F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 6, 0, 0)
            };

            logoStack.Controls.Add(logoBadge);
            logoStack.Controls.Add(brandTitle);
            topBar.Controls.Add(logoStack);

            var userStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 14, 0, 0)
            };

            var btnLogout = new Button
            {
                Text = "Sign Out",
                Height = 36,
                Width = 100,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.TableHeaderTan,
                ForeColor = UITheme.TextDark,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.Click += (s, e) => Close();

            userStack.Controls.Add(btnLogout);
            topBar.Controls.Add(userStack);
            Controls.Add(topBar);

            // 2. MAIN SCROLL PANEL
            _contentScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = UITheme.CreamBackground,
                Padding = new Padding(40, 24, 40, 32)
            };

            Controls.Add(_contentScrollPanel);
            _contentScrollPanel.BringToFront();
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
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header + Period
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // KPI Cards
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Charts
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Tenant Registry Table

            // 1. TOP HEADER & PERIOD FILTER
            var headerPanel = new Panel
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
                Text = "Platform Master Dashboard",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            var lblSubtitle = new Label
            {
                Text = $"Multi-Tenant Health & Cross-Shop Subscriptions \u00B7 {DateTime.Now:MMM d, yyyy}",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            var periodSelector = new PeriodSelectorControl
            {
                Dock = DockStyle.Right,
                SelectedPeriod = _activePeriod
            };
            periodSelector.PeriodChanged += (s, newPeriod) =>
            {
                _activePeriod = newPeriod;
                _ = LoadDashboardDataAsync();
            };

            headerPanel.Controls.Add(periodSelector);
            headerPanel.Controls.Add(titleStack);
            rootLayout.Controls.Add(headerPanel, 0, 0);

            // 2. 4 PLATFORM KPI CARDS
            var kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 1,
                Height = 145,
                Margin = new Padding(0, 0, 0, 18),
                BackColor = Color.Transparent
            };
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            var cardBusinesses = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Registered Businesses",
                Value = data.TotalBusinesses.ToString(),
                Subtitle = $"{data.ActiveBusinesses} active tenant shops",
                Margin = new Padding(0, 0, 8, 0)
            };
            cardBusinesses.SetBadge("Master CRM", UITheme.StatusBlueFg, UITheme.StatusBlueBg);

            var cardDatabases = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Tenant Databases",
                Value = data.ActiveDatabases.ToString(),
                Subtitle = "Dedicated database instances",
                Margin = new Padding(8, 0, 8, 0)
            };
            cardDatabases.SetBadge("SQL LocalDB", UITheme.StatusGreenFg, UITheme.StatusGreenBg);

            var cardSubs = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Active Subscriptions",
                Value = data.ActiveSubscriptionsCount.ToString(),
                Subtitle = "Enterprise & Pro subscriptions",
                Margin = new Padding(8, 0, 8, 0)
            };
            cardSubs.SetBadge("Billing", UITheme.StatusGreenFg, UITheme.StatusGreenBg);

            var cardUsers = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Platform Users",
                Value = data.PlatformUsersCount.ToString(),
                Subtitle = "Administrators, managers & staff",
                Margin = new Padding(8, 0, 0, 0)
            };
            cardUsers.SetBadge("Accounts", UITheme.StatusYellowFg, UITheme.StatusYellowBg);

            kpiTable.Controls.Add(cardBusinesses, 0, 0);
            kpiTable.Controls.Add(cardDatabases, 1, 0);
            kpiTable.Controls.Add(cardSubs, 2, 0);
            kpiTable.Controls.Add(cardUsers, 3, 0);
            rootLayout.Controls.Add(kpiTable, 0, 1);

            // 3. CHARTS ROW (Registration Trend + Subscription Distribution)
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

            var trendChart = new DashboardTrendChart
            {
                Dock = DockStyle.Fill,
                ChartTitle = "Tenant Onboarding Trends",
                ChartSubtitle = "New cake business companies registered on the platform",
                IsCurrency = false,
                Margin = new Padding(0, 0, 10, 0)
            };
            trendChart.SetData(data.RegistrationTrend, isCurrency: false);

            var planChart = new DashboardBarChart
            {
                Dock = DockStyle.Fill,
                ChartTitle = "Subscription Tier Distribution",
                ChartSubtitle = "Platform tenant license distribution",
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

            var badgePlatform = new Label
            {
                Text = "Database-per-Tenant",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = UITheme.StatusGreenFg,
                BackColor = UITheme.StatusGreenBg,
                Dock = DockStyle.Right,
                AutoSize = true,
                Padding = new Padding(10, 4, 10, 4),
                Margin = new Padding(0, 14, 0, 0)
            };
            badgePlatform.Paint += (s, e) =>
            {
                using var borderPen = new Pen(Color.FromArgb(40, UITheme.StatusGreenFg), 1f);
                e.Graphics.DrawRoundedRectangle(borderPen, new Rectangle(0, 0, badgePlatform.Width - 1, badgePlatform.Height - 1), 10);
            };

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

            grid.ClearSelection();
            card.Controls.Add(grid);
            grid.BringToFront();
            return card;
        }
    }
}
