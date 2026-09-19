using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Manager
{
    /// <summary>
    /// Role-specific Manager Dashboard:
    /// Focuses on operational management, shop-wide order fulfillment, pipeline bottlenecks,
    /// staff follow-up resolution, and order volume trends with period filtering (7d/30d/90d/all).
    /// </summary>
    public class ManagerDashboardForm : CC.Forms.Shared.DashboardShell
    {
        private string _activePeriod = "30d";
        private Panel _contentScrollPanel = null!;
        public Task InitializationTask { get; private set; } = Task.CompletedTask;

        public ManagerDashboardForm() : base("Dashboard")
        {
            PageTitle = "Manager Dashboard";
            SidebarCtrl.AddNavItem("Reports", "\uE9F9");
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
                    CC.Controls.ViewHost.ShowFormInPanel(MainPanel, new CC.Forms.Staff.Customers.CustomerListForm());
                    break;
                case "Inquiries":
                    CC.Controls.ViewHost.ShowFormInPanel(MainPanel, new CC.Forms.Staff.Inquiries.InquiryListForm());
                    break;
                case "Orders":
                    CC.Controls.ViewHost.ShowFormInPanel(MainPanel, new CC.Forms.Staff.Orders.OrderListForm());
                    break;
                case "Payments":
                    CC.Controls.ViewHost.ShowFormInPanel(MainPanel, new CC.Forms.Staff.Payments.PaymentListForm());
                    break;
                case "Follow-ups":
                    CC.Controls.ViewHost.ShowFormInPanel(MainPanel, new CC.Forms.Staff.FollowUps.FollowUpListForm());
                    break;
                case "Reports":
                    CC.Controls.ViewHost.ShowFormInPanel(MainPanel, new CC.Forms.Manager.Reports.ReportListForm());
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

        public void ScrollToBottom()
        {
            if (_contentScrollPanel != null)
            {
                _contentScrollPanel.AutoScrollPosition = new Point(0, 500);
            }
        }

        private async Task LoadDashboardDataAsync()
        {
            try
            {
                var data = await CrmDataService.GetManagerDashboardDataAsync(
                    companyId: SessionService.CurrentUser?.CompanyId,
                    period: _activePeriod);

                if (IsDisposed) return;

            var rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header + Period
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // KPI Cards
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Pipeline Funnel Bar
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Charts
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Recent Orders

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
                Text = "Manager Dashboard",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            var lblSubtitle = new Label
            {
                Text = $"Operational Oversight & Order Pipeline \u00B7 {DateTime.Now:MMM d, yyyy}",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            var periodSelector = new PeriodSelectorControl(PeriodSelectorControl.ManagerPeriods)
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

            // 2. 5 MANAGER KPI CARDS (Including Customer Card)
            var kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 5,
                RowCount = 1,
                Height = 145,
                Margin = new Padding(0, 0, 0, 18),
                BackColor = Color.Transparent
            };
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            var cardCustomers = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Customers",
                Value = data.TotalCustomersCount.ToString(),
                Subtitle = "Registered customer base",
                Margin = new Padding(0, 0, 6, 0)
            };
            cardCustomers.SetBadge("CRM", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardCustomers.CardClicked += (s, e) => Navigate("Customers");

            var cardActive = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Active Orders",
                Value = data.ActiveOrdersCount.ToString(),
                Subtitle = "Orders currently in production",
                Margin = new Padding(6, 0, 6, 0)
            };
            cardActive.SetBadge("In Progress", UITheme.StatusBlueFg, UITheme.StatusBlueBg);
            cardActive.CardClicked += (s, e) => Navigate("Orders");

            var cardReady = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Ready Pickup",
                Value = data.ReadyForPickupCount.ToString(),
                Subtitle = "Finished orders awaiting release",
                Margin = new Padding(6, 0, 6, 0)
            };
            cardReady.SetBadge("Completed", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardReady.CardClicked += (s, e) => Navigate("Orders");

            var cardInquiries = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Inquiries",
                Value = data.OpenInquiriesCount.ToString(),
                Subtitle = "Awaiting quote or response",
                Margin = new Padding(6, 0, 6, 0)
            };
            cardInquiries.SetBadge("Pipeline", UITheme.StatusYellowFg, UITheme.StatusYellowBg);
            cardInquiries.CardClicked += (s, e) => Navigate("Inquiries");

            var cardOverdue = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Overdue Tasks",
                Value = data.ShopOverdueFollowupsCount.ToString(),
                Subtitle = "Shop-wide overdue interactions",
                Margin = new Padding(6, 0, 0, 0)
            };
            cardOverdue.SetBadge("Urgent", UITheme.StatusRedFg, UITheme.StatusRedBg);
            cardOverdue.CardClicked += (s, e) => Navigate("Follow-ups");

            kpiTable.Controls.Add(cardCustomers, 0, 0);
            kpiTable.Controls.Add(cardActive, 1, 0);
            kpiTable.Controls.Add(cardReady, 2, 0);
            kpiTable.Controls.Add(cardInquiries, 3, 0);
            kpiTable.Controls.Add(cardOverdue, 4, 0);
            rootLayout.Controls.Add(kpiTable, 0, 1);

            // 3. PIPELINE FUNNEL PROGRESS BAR
            var pipelineBar = new DashboardPipelineBar
            {
                Dock = DockStyle.Top,
                Title = "Order Fulfillment Pipeline Funnel",
                Margin = new Padding(0, 0, 0, 18)
            };
            pipelineBar.SetStages(data.PipelineStages);
            pipelineBar.StageClicked += (s, stage) =>
            {
                if (stage == "Inquiries") Navigate("Inquiries");
                else Navigate("Orders");
            };
            rootLayout.Controls.Add(pipelineBar, 0, 2);

            // 4. CHARTS ROW (Order Trends + Staff Performance)
            var chartsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Height = 280,
                Margin = new Padding(0, 0, 0, 18),
                BackColor = Color.Transparent
            };
            chartsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            chartsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));

            var trendChart = new DashboardTrendChart
            {
                Dock = DockStyle.Fill,
                ChartTitle = "Order Volume Trends",
                ChartSubtitle = "Daily order placements over the period",
                IsCurrency = false,
                Margin = new Padding(0, 0, 10, 0)
            };
            trendChart.SetData(data.OrderVolumeTrend, isCurrency: false);

            var staffChart = new DashboardBarChart
            {
                Dock = DockStyle.Fill,
                ChartTitle = "Staff Task Resolution",
                ChartSubtitle = "Follow-ups completed and pending by team member",
                Margin = new Padding(10, 0, 0, 0)
            };
            staffChart.SetData(data.StaffPerformance);
            staffChart.CategoryClicked += (s, staff) => Navigate("Follow-ups");

            chartsTable.Controls.Add(trendChart, 0, 0);
            chartsTable.Controls.Add(staffChart, 1, 0);
            rootLayout.Controls.Add(chartsTable, 0, 3);

            // 5. RECENT HIGH-PRIORITY ORDERS TABLE
            var ordersPanel = BuildRecentOrdersPanel(data);
            rootLayout.Controls.Add(ordersPanel, 0, 4);

            if (IsDisposed) return;
            _contentScrollPanel.SuspendLayout();
            _contentScrollPanel.Controls.Clear();
            _contentScrollPanel.Controls.Add(rootLayout);
            _contentScrollPanel.ResumeLayout(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ManagerDashboard] Error loading data: {ex.Message}");
            }
        }

        private Panel BuildRecentOrdersPanel(ManagerDashboardData data)
        {
            var card = new Panel
            {
                Dock = DockStyle.Top,
                Height = 360,
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
                Text = "Recent Production Orders Needing Attention",
                Font = new Font(UITheme.FontSerif, 12.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };

            var lblSubtitle = new Label
            {
                Text = $"{data.RecentOrders.Count} orders actively progressing through shop fulfillment",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            headerLeft.Controls.Add(lblTitle);
            headerLeft.Controls.Add(lblSubtitle);

            var btnViewAll = new Label
            {
                Text = "View All Orders \u2192",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Dock = DockStyle.Right,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 16, 0, 0)
            };
            btnViewAll.Click += (s, e) => Navigate("Orders");

            headerPanel.Controls.Add(btnViewAll);
            headerPanel.Controls.Add(headerLeft);
            card.Controls.Add(headerPanel);

            if (data.RecentOrders == null || data.RecentOrders.Count == 0)
            {
                var emptyLbl = new Label
                {
                    Text = "No production orders recorded for this period.",
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

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRef", HeaderText = "ORDER REF", Width = 130, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER", Width = 170, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDetails", HeaderText = "CAKE SPECIFICATION", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 180, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "DELIVERY DATE", Width = 125, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAmount", HeaderText = "TOTAL", Width = 100, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS", Width = 125, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAction", HeaderText = "", Width = 80, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

            foreach (var item in data.RecentOrders)
            {
                int rowIdx = grid.Rows.Add(
                    item.ReferenceNo,
                    item.CustomerName,
                    item.CakeDetails,
                    item.DeliveryDate.ToString("MMM d, yyyy"),
                    $"P{item.TotalAmount:N0}",
                    item.Status,
                    "View \u2192");
                grid.Rows[rowIdx].Tag = item;
            }

            grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics == null) return;
                var item = grid.Rows[e.RowIndex].Tag as ManagerPriorityOrderItem;
                if (item == null) return;

                e.PaintBackground(e.CellBounds, true);
                var g = e.Graphics;
                int px5 = 20;
                int cellY = e.CellBounds.Top + (e.CellBounds.Height - 20) / 2;

                if (e.ColumnIndex == 0) // REF
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                    using var brush = new SolidBrush(UITheme.PrimaryMauve);
                    g.DrawString(item.ReferenceNo, font, brush, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 1) // CUSTOMER
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                    g.DrawString(item.CustomerName, font, Brushes.Black, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 2) // DETAILS
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                    var rect = new Rectangle(e.CellBounds.Left + px5, cellY - 2, Math.Max(0, e.CellBounds.Width - px5 - 10), 24);
                    TextRenderer.DrawText(g, item.CakeDetails, font, rect, UITheme.TextDark,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 3) // DATE
                {
                    using var font = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                    using var brush = new SolidBrush(UITheme.TextMuted);
                    g.DrawString(item.DeliveryDate.ToString("MMM d, yyyy"), font, brush, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 4) // TOTAL
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                    g.DrawString($"\u20B1{item.TotalAmount:N0}", font, Brushes.Black, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 5) // STATUS
                {
                    Color bg = item.Status switch
                    {
                        "Ready" or "Completed" => UITheme.StatusGreenBg,
                        "Processing" => UITheme.StatusYellowBg,
                        _ => UITheme.StatusBlueBg
                    };
                    Color fg = item.Status switch
                    {
                        "Ready" or "Completed" => UITheme.StatusGreenFg,
                        "Processing" => UITheme.StatusYellowFg,
                        _ => UITheme.StatusBlueFg
                    };
                    UITheme.DrawStatusBadge(g, e.CellBounds, item.Status, bg, fg, showDot: true);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 6) // ACTION
                {
                    UITheme.DrawActionLink(g, e.CellBounds, "View \u2192");
                    e.Handled = true;
                }
            };

            grid.CellClick += (s, e) => Navigate("Orders");

            grid.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 6)
                    grid.Cursor = Cursors.Hand;
                else
                    grid.Cursor = Cursors.Default;
            };

            grid.ClearSelection();
            card.Controls.Add(grid);
            grid.BringToFront();
            return card;
        }
    }
}
