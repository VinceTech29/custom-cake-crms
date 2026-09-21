using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Staff
{
    /// <summary>
    /// Role-specific Staff Dashboard:
    /// Focuses on personal & daily operational work: assigned follow-ups, baking/processing orders,
    /// ready pickups, task activity trends, and urgent daily tasks with period filtering (7d/30d/90d/all).
    /// </summary>
    public class StaffDashboardForm : CC.Forms.Shared.DashboardShell
    {
        private string _activePeriod = "1d";
        private Panel _contentScrollPanel = null!;
        public Task InitializationTask { get; private set; } = Task.CompletedTask;

        public StaffDashboardForm() : base("Dashboard")
        {
            PageTitle = "Staff Dashboard";
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
                case "Retention & Campaigns":
                    MessageBox.Show("You do not have permission to access this feature.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            try
            {
                var data = await CrmDataService.GetStaffDashboardDataAsync(
                    userId: SessionService.CurrentUser?.UserId,
                    companyId: SessionService.CurrentUser?.CompanyId,
                    period: _activePeriod);

                if (IsDisposed) return;

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
            rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Urgent Tasks Table

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
                Text = "Staff Dashboard",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            var lblSubtitle = new Label
            {
                Text = $"Today's Personal Operations & Daily Transactions \u00B7 {DateTime.Now:MMM d, yyyy}",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            var periodSelector = new PeriodSelectorControl(PeriodSelectorControl.StaffPeriods)
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

            // 2. 8 ROLE-SPECIFIC KPI CARDS (4x2 GRID)
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

            // Row 1: Daily Immediate Action & Schedule
            var cardDue = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Follow-ups Due",
                Value = data.MyDueFollowupsCount.ToString(),
                Subtitle = "Pending client communications",
                Margin = new Padding(0, 0, 6, 6)
            };
            cardDue.SetBadge("Schedule", UITheme.StatusYellowFg, UITheme.StatusYellowBg);
            cardDue.CardClicked += (s, e) => Navigate("Follow-ups");

            var cardOverdue = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Overdue Tasks",
                Value = data.MyOverdueFollowupsCount.ToString(),
                Subtitle = "Requires prompt response",
                Margin = new Padding(6, 0, 6, 6)
            };
            cardOverdue.SetBadge("Urgent", UITheme.StatusRedFg, UITheme.StatusRedBg);
            cardOverdue.CardClicked += (s, e) => Navigate("Follow-ups");

            var cardInquiries = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Handled Inquiries",
                Value = data.MyHandledInquiriesCount.ToString(),
                Subtitle = "Open inquiries assigned",
                Margin = new Padding(6, 0, 6, 6)
            };
            cardInquiries.SetBadge("Pipeline", UITheme.StatusBlueFg, UITheme.StatusBlueBg);
            cardInquiries.CardClicked += (s, e) => Navigate("Inquiries");

            var cardTodayDue = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Orders Due Today",
                Value = data.TodayDueOrdersCount.ToString(),
                Subtitle = "Scheduled for release today",
                Margin = new Padding(6, 0, 0, 6)
            };
            cardTodayDue.SetBadge("Today", UITheme.PrimaryMauve, Color.FromArgb(245, 235, 240));
            cardTodayDue.CardClicked += (s, e) => Navigate("Orders");

            // Row 2: Production Stages & Client Base
            var cardProcessing = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Processing",
                Value = data.ProcessingOrdersCount.ToString(),
                Subtitle = "Baking & decorating in progress",
                Margin = new Padding(0, 6, 6, 0)
            };
            cardProcessing.SetBadge("Production", UITheme.StatusBlueFg, UITheme.StatusBlueBg);
            cardProcessing.CardClicked += (s, e) => Navigate("Orders");

            var cardReady = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Ready Pickup",
                Value = data.ReadyOrdersCount.ToString(),
                Subtitle = "Awaiting customer collection",
                Margin = new Padding(6, 6, 6, 0)
            };
            cardReady.SetBadge("Ready", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardReady.CardClicked += (s, e) => Navigate("Orders");

            var cardCompleted = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Completed Orders",
                Value = data.CompletedOrdersCount.ToString(),
                Subtitle = "Fulfilled in selected period",
                Margin = new Padding(6, 6, 6, 0)
            };
            cardCompleted.SetBadge("Fulfilled", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardCompleted.CardClicked += (s, e) => Navigate("Orders");

            var cardCustomers = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Customers",
                Value = data.TotalCustomersCount.ToString(),
                Subtitle = "Registered client database",
                Margin = new Padding(6, 6, 0, 0)
            };
            cardCustomers.SetBadge("CRM", UITheme.StatusGreenFg, UITheme.StatusGreenBg);
            cardCustomers.CardClicked += (s, e) => Navigate("Customers");

            kpiTable.Controls.Add(cardDue, 0, 0);
            kpiTable.Controls.Add(cardOverdue, 1, 0);
            kpiTable.Controls.Add(cardInquiries, 2, 0);
            kpiTable.Controls.Add(cardTodayDue, 3, 0);
            kpiTable.Controls.Add(cardProcessing, 0, 1);
            kpiTable.Controls.Add(cardReady, 1, 1);
            kpiTable.Controls.Add(cardCompleted, 2, 1);
            kpiTable.Controls.Add(cardCustomers, 3, 1);
            rootLayout.Controls.Add(kpiTable, 0, 1);

            // 3. CHARTS ROW (Line Chart + Bar Chart)
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
                ChartTitle = "Today's Task Activity",
                ChartSubtitle = "Completed client touchpoints and order fulfillment tasks",
                IsCurrency = false,
                Margin = new Padding(0, 0, 10, 0)
            };
            trendChart.SetData(data.TaskCompletionTrend, isCurrency: false);

            var barChart = new DashboardBarChart
            {
                Dock = DockStyle.Fill,
                ChartTitle = "Order Status Distribution",
                ChartSubtitle = "Shop orders across fulfillment stages",
                Margin = new Padding(10, 0, 0, 0)
            };
            barChart.SetData(data.OrderStatusBreakdown);
            barChart.CategoryClicked += (s, cat) => Navigate("Orders");

            chartsTable.Controls.Add(trendChart, 0, 0);
            chartsTable.Controls.Add(barChart, 1, 0);
            rootLayout.Controls.Add(chartsTable, 0, 2);

            // 4. URGENT TASKS TABLE
            var urgentPanel = BuildUrgentTasksPanel(data);
            rootLayout.Controls.Add(urgentPanel, 0, 3);

                _contentScrollPanel.SuspendLayout();
                _contentScrollPanel.Controls.Clear();
                _contentScrollPanel.Controls.Add(rootLayout);
                _contentScrollPanel.ResumeLayout(true);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[StaffDashboard] Error loading data: {ex.Message}");
            }
        }

        private Panel BuildUrgentTasksPanel(StaffDashboardData data)
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
                Text = "Today's Urgent Tasks & Deliveries",
                Font = new Font(UITheme.FontSerif, 12.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };

            var lblSubtitle = new Label
            {
                Text = $"{data.UrgentTasks.Count} actionable items requiring client follow-up or bakery dispatch",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            headerLeft.Controls.Add(lblTitle);
            headerLeft.Controls.Add(lblSubtitle);

            var btnViewAll = new Label
            {
                Text = "View All Tasks \u2192",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Dock = DockStyle.Right,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 16, 0, 0)
            };
            btnViewAll.Click += (s, e) => Navigate("Follow-ups");

            headerPanel.Controls.Add(btnViewAll);
            headerPanel.Controls.Add(headerLeft);
            card.Controls.Add(headerPanel);

            if (data.UrgentTasks == null || data.UrgentTasks.Count == 0)
            {
                var emptyLbl = new Label
                {
                    Text = "No urgent tasks pending for this period.",
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

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "TYPE", Width = 110, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTitle", HeaderText = "TASK / DETAILS", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 200, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER", Width = 170, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDueDate", HeaderText = "DUE DATE", Width = 125, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS", Width = 125, SortMode = DataGridViewColumnSortMode.NotSortable });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAction", HeaderText = "", Width = 80, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

            foreach (var item in data.UrgentTasks)
            {
                int rowIdx = grid.Rows.Add(item.Type, item.Title, item.CustomerName, item.DueDate.ToString("MMM d, yyyy"), item.Status, "Open \u2192");
                grid.Rows[rowIdx].Tag = item;
            }

            grid.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics == null) return;
                var task = grid.Rows[e.RowIndex].Tag as StaffUrgentTaskItem;
                if (task == null) return;

                e.PaintBackground(e.CellBounds, true);
                var g = e.Graphics;
                int px5 = 20;
                int cellY = e.CellBounds.Top + (e.CellBounds.Height - 20) / 2;

                if (e.ColumnIndex == 0) // TYPE
                {
                    Color typeBg = task.Type == "Follow-up" ? Color.FromArgb(240, 234, 245) : Color.FromArgb(235, 243, 252);
                    Color typeFg = task.Type == "Follow-up" ? UITheme.PrimaryMauve : UITheme.StatusBlueFg;
                    UITheme.DrawStatusBadge(g, e.CellBounds, task.Type, typeBg, typeFg, showDot: false);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 1) // TASK / DETAILS
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                    var rect = new Rectangle(e.CellBounds.Left + px5, cellY - 2, Math.Max(0, e.CellBounds.Width - px5 - 10), 24);
                    TextRenderer.DrawText(g, task.Title, font, rect, UITheme.TextDark,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 2) // CUSTOMER
                {
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                    g.DrawString(task.CustomerName, font, Brushes.Black, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 3) // DUE DATE
                {
                    using var font = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                    using var brush = new SolidBrush(UITheme.TextMuted);
                    g.DrawString(task.DueDate.ToString("MMM d, yyyy"), font, brush, e.CellBounds.Left + px5, cellY);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 4) // STATUS
                {
                    UITheme.DrawStatusBadge(g, e.CellBounds, task.Status, task.StatusBg, task.StatusFg, showDot: true);
                    e.Handled = true;
                }
                else if (e.ColumnIndex == 5) // ACTION
                {
                    UITheme.DrawActionLink(g, e.CellBounds, "Open \u2192");
                    e.Handled = true;
                }
            };

            grid.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && grid.Rows[e.RowIndex].Tag is StaffUrgentTaskItem task)
                {
                    if (task.Type == "Follow-up") Navigate("Follow-ups");
                    else Navigate("Orders");
                }
            };

            grid.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 5)
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
