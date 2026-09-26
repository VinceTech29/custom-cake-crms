using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Manager.Reports
{
    /// <summary>
    /// Manager Reports module displaying overall transactions (orders and payments)
    /// with daily/monthly filtering, live KPI summaries, and CSV export capabilities.
    /// </summary>
    public class ReportListForm : Form, INavigationAware
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;

        private TableLayoutPanel kpiTable = null!;
        private Label lblTotalRevenue = null!;
        private Label lblTotalTransactions = null!;
        private Label lblDailySummary = null!;
        private Label lblMonthlySummary = null!;

        private Panel searchFilterPanel = null!;
        private Panel searchPill = null!;
        private TextBox txtSearchBox = null!;
        private FlowLayoutPanel filterPillContainer = null!;
        private DateTimePicker dtpFilterDate = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private Label lblFromDate = null!;
        private Label lblToDate = null!;

        private Panel tableCardPanel = null!;
        private DataGridView gridTransactions = null!;
        private PaginationControl pagination = null!;
        private int currentPage = 1;
        private const int PageSize = 10;

        private Button btnExportDaily = null!;
        private Button btnExportMonthly = null!;
        private Button btnExportAnnual = null!;
        private Button btnExportAll = null!;

        private string activeFilter = "All"; // "All", "Daily", "Monthly", "Annually", "Orders", "Payments", "Retention"
        private string activeSearchQuery = string.Empty;
        private DateTime selectedDate = DateTime.Today;
        private List<TransactionRecord> currentRecords = new List<TransactionRecord>();

        public ReportListForm()
        {
            InitializeComponent();
            BuildTopToolbar();
            BuildKpiCards();
            BuildSearchAndFilterBar();
            BuildDataGridCard();

            Controls.Add(tableCardPanel);
            Controls.Add(searchFilterPanel);
            Controls.Add(kpiTable);
            Controls.Add(topPanel);
        }

        public async Task InitializeDataAsync() => await RefreshDataAsync();

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1100, 750);
            Font = new Font(UITheme.FontSans, 9.5F);
            FormBorderStyle = FormBorderStyle.None;
            Name = "ReportListForm";
            Padding = Padding.Empty;
            BackColor = UITheme.CreamBackground;
            ResumeLayout(false);
        }

        // =========================================================
        // 1. TOP TOOLBAR (Title, Subtitle, and Export Action Buttons)
        // =========================================================
        private void BuildTopToolbar()
        {
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                Padding = new Padding(0, 0, 0, 14),
                BackColor = Color.Transparent
            };

            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerTable.ColumnStyles.Clear();
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var titleStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 16, 0)
            };

            lblTitle = new Label
            {
                Text = "Reports & Analytics",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            lblSubtitle = new Label
            {
                Text = "Overall transactions, revenue, and daily/monthly summaries",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            // Action buttons stack on the right
            var btnStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0)
            };

            btnExportAll = new Button
            {
                Text = "Export CSV",
                Height = 38,
                Width = 110,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = UITheme.TextDark,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnExportAll.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnExportAll.ApplyRoundedRegion(10);
            btnExportAll.Click += async (s, e) => await ExportReportAsync("All");

            btnExportAnnual = new Button
            {
                Text = "Export Annual",
                Height = 38,
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Margin = new Padding(6, 0, 0, 0)
            };
            UITheme.ApplyActionButton(btnExportAnnual, "\uE787", 12);
            btnExportAnnual.Click += async (s, e) => await ExportReportAsync("Annually");

            btnExportMonthly = new Button
            {
                Text = "Export Monthly",
                Height = 38,
                Width = 145,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Margin = new Padding(6, 0, 0, 0)
            };
            UITheme.ApplyActionButton(btnExportMonthly, "\uE787", 12);
            btnExportMonthly.Click += async (s, e) => await ExportReportAsync("Monthly");

            btnExportDaily = new Button
            {
                Text = "Export Daily",
                Height = 38,
                Width = 135,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Margin = new Padding(6, 0, 0, 0)
            };
            UITheme.ApplyActionButton(btnExportDaily, "\uE916", 12);
            btnExportDaily.Click += async (s, e) => await ExportReportAsync("Daily");

            btnStack.Controls.Add(btnExportAll);
            btnStack.Controls.Add(btnExportAnnual);
            btnStack.Controls.Add(btnExportMonthly);
            btnStack.Controls.Add(btnExportDaily);

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnStack, 1, 0);

            topPanel.Controls.Add(headerTable);
        }

        // =========================================================
        // 2. 4 KPI SUMMARY CARDS (Total Revenue, Transactions, Daily, Monthly)
        // Calibrated 24px vertical breathing room
        // =========================================================
        private void BuildKpiCards()
        {
            kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 114,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 24),
                Margin = Padding.Empty
            };

            kpiTable.RowStyles.Clear();
            kpiTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            for (int i = 0; i < 4; i++)
            {
                kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }

            // Card 1: TOTAL REVENUE (Green)
            var cardRev = CreateKpiCard("TOTAL REVENUE", "\uE8C7", Color.FromArgb(22, 163, 74), out lblTotalRevenue);
            // Card 2: TOTAL TRANSACTIONS (Dark)
            var cardTxn = CreateKpiCard("TOTAL TRANSACTIONS", "\uE719", Color.FromArgb(31, 27, 24), out lblTotalTransactions);
            // Card 3: TODAY'S VOLUME (Orange)
            var cardDaily = CreateKpiCard("TODAY'S TRANSACTIONS", "\uE916", Color.FromArgb(217, 119, 6), out lblDailySummary);
            // Card 4: MONTHLY VOLUME (Blue)
            var cardMonthly = CreateKpiCard("THIS MONTH", "\uE9F9", Color.FromArgb(37, 99, 235), out lblMonthlySummary);

            kpiTable.Controls.Add(cardRev, 0, 0);
            kpiTable.Controls.Add(cardTxn, 1, 0);
            kpiTable.Controls.Add(cardDaily, 2, 0);
            kpiTable.Controls.Add(cardMonthly, 3, 0);
        }

        private Panel CreateKpiCard(string title, string iconGlyph, Color numColor, out Label countLabel)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 14, 0),
                Padding = new Padding(18, 14, 18, 14)
            };
            card.ApplyRoundedRegion(14);

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
            };

            var topRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.Transparent
            };

            var lblCardTitle = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Dock = DockStyle.Left
            };

            var lblIcon = new Label
            {
                Text = iconGlyph,
                Font = new Font("Segoe MDL2 Assets", 11F),
                ForeColor = numColor,
                AutoSize = true,
                Dock = DockStyle.Right
            };

            topRow.Controls.Add(lblCardTitle);
            topRow.Controls.Add(lblIcon);

            var lblNum = new Label
            {
                Text = "0",
                Font = new Font(UITheme.FontSans, 18F, FontStyle.Bold),
                ForeColor = numColor,
                AutoSize = true,
                Location = new Point(18, 38)
            };

            countLabel = lblNum;

            card.Controls.Add(lblNum);
            card.Controls.Add(topRow);
            return card;
        }

        // =========================================================
        // 3. SEARCH & PERIOD FILTER BAR
        // =========================================================
        private void BuildSearchAndFilterBar()
        {
            searchFilterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(0, 0, 0, 24),
                BackColor = Color.Transparent
            };

            var rowFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Search pill (py-2.5 pl-10 rounded-xl)
            searchPill = new Panel
            {
                Size = new Size(280, 40),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 12, 0)
            };

            searchPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, searchPill.Width - 1, searchPill.Height - 1), 12);

                using var font = new Font("Segoe MDL2 Assets", 10.5f);
                var rect = new Rectangle(14, 0, 20, searchPill.Height);
                TextRenderer.DrawText(e.Graphics, "\uE721", font, rect, UITheme.TextMuted,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            };
            searchPill.ApplyRoundedRegion(12);

            txtSearchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Font = new Font(UITheme.FontSans, 9.5F),
                ForeColor = UITheme.TextDark,
                Location = new Point(40, 11),
                Width = searchPill.Width - 52
            };
            txtSearchBox.TextChanged += async (s, e) =>
            {
                activeSearchQuery = txtSearchBox.Text.Trim();
                currentPage = 1;
                await RefreshDataAsync();
            };
            searchPill.Controls.Add(txtSearchBox);
            rowFlow.Controls.Add(searchPill);

            // Filter pills: All, Daily (Today), Monthly (This Month), Annually (This Year), Orders, Payments
            filterPillContainer = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 8, 0)
            };

            string[] filters = new[] { "All", "Daily", "Monthly", "Annually", "Orders", "Payments", "Retention" };

            foreach (var filterName in filters)
            {
                var btn = new Button
                {
                    Text = filterName switch
                    {
                        "Daily" => "Daily (Today)",
                        "Monthly" => "Monthly (This Month)",
                        "Annually" => "Annually (This Year)",
                        "Retention" => "Retention Requests",
                        _ => filterName
                    },
                    AutoSize = true,
                    Height = 36,
                    Margin = new Padding(0, 0, 6, 0),
                    Padding = new Padding(12, 0, 12, 0),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                    Tag = filterName
                };

                btn.FlatAppearance.BorderSize = 0;
                btn.Paint += (s, e) =>
                {
                    var b = (Button)s!;
                    bool isActive = string.Equals(b.Tag as string, activeFilter, StringComparison.OrdinalIgnoreCase);
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    if (!isActive)
                    {
                        using var pen = new Pen(UITheme.BorderColor, 1.2f);
                        e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, b.Width - 1, b.Height - 1), 16);
                    }
                };

                btn.Click += async (s, e) =>
                {
                    activeFilter = ((Button)s!).Tag?.ToString() ?? "All";
                    currentPage = 1;
                    UpdateFilterPillStyles();
                    await RefreshDataAsync();
                };

                filterPillContainer.Controls.Add(btn);
            }

            rowFlow.Controls.Add(filterPillContainer);

            // Date Picker for custom date inspection (Daily/Monthly reference)
            dtpFilterDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Height = 36,
                Width = 120,
                Font = new Font(UITheme.FontSans, 9.5F),
                Margin = new Padding(4, 4, 8, 0)
            };
            dtpFilterDate.ValueChanged += async (s, e) =>
            {
                selectedDate = dtpFilterDate.Value;
                currentPage = 1;
                if (activeFilter != "Daily" && activeFilter != "Monthly")
                {
                    activeFilter = "Daily";
                    UpdateFilterPillStyles();
                }
                await RefreshDataAsync();
            };
            rowFlow.Controls.Add(dtpFilterDate);

            // From / To pickers for custom range (used by Retention Requests and range-based filters)
            lblFromDate = new Label
            {
                Text = "From:",
                AutoSize = true,
                Font = new Font(UITheme.FontSans, 9F),
                ForeColor = UITheme.TextMuted,
                Margin = new Padding(0, 12, 4, 0)
            };
            dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddMonths(-3),
                Height = 36,
                Width = 110,
                Font = new Font(UITheme.FontSans, 9.5F),
                Margin = new Padding(0, 4, 6, 0)
            };
            lblToDate = new Label
            {
                Text = "To:",
                AutoSize = true,
                Font = new Font(UITheme.FontSans, 9F),
                ForeColor = UITheme.TextMuted,
                Margin = new Padding(0, 12, 4, 0)
            };
            dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Height = 36,
                Width = 110,
                Font = new Font(UITheme.FontSans, 9.5F),
                Margin = new Padding(0, 4, 6, 0)
            };
            var btnApplyRange = new Button
            {
                Text = "Apply",
                Height = 34,
                Width = 65,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 5, 0, 0)
            };
            btnApplyRange.FlatAppearance.BorderSize = 0;
            btnApplyRange.Click += async (s, e) =>
            {
                if (dtpFrom.Value.Date > dtpTo.Value.Date)
                {
                    MessageBox.Show("\"From\" date must be before or equal to \"To\" date.", "Invalid Range",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                currentPage = 1;
                await RefreshDataAsync();
            };

            rowFlow.Controls.Add(lblFromDate);
            rowFlow.Controls.Add(dtpFrom);
            rowFlow.Controls.Add(lblToDate);
            rowFlow.Controls.Add(dtpTo);
            rowFlow.Controls.Add(btnApplyRange);

            searchFilterPanel.Controls.Add(rowFlow);
            UpdateFilterPillStyles();
        }

        private void UpdateFilterPillStyles()
        {
            foreach (Control c in filterPillContainer.Controls)
            {
                if (c is Button btn && btn.Tag is string tag)
                {
                    bool isActive = string.Equals(tag, activeFilter, StringComparison.OrdinalIgnoreCase);
                    btn.BackColor = isActive ? UITheme.PrimaryMauve : Color.White;
                    btn.ForeColor = isActive ? Color.White : UITheme.TextDark;
                    btn.Invalidate();
                }
            }
        }

        // =========================================================
        // 4. TRANSACTIONS DATA TABLE
        // =========================================================
        private void BuildDataGridCard()
        {
            tableCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            tableCardPanel.ApplyRoundedRegion(14);

            tableCardPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, tableCardPanel.Width - 1, tableCardPanel.Height - 1), 14);
            };

            gridTransactions = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyTableStyle(gridTransactions);
            gridTransactions.RowTemplate.Height = 58;

            var colDate = new DataGridViewTextBoxColumn
            {
                Name = "colDate",
                HeaderText = "DATE & TIME",
                Width = 145,
                ReadOnly = true
            };

            var colRef = new DataGridViewTextBoxColumn
            {
                Name = "colRef",
                HeaderText = "REFERENCE #",
                Width = 145,
                ReadOnly = true
            };

            var colType = new DataGridViewTextBoxColumn
            {
                Name = "colType",
                HeaderText = "TYPE",
                Width = 110,
                ReadOnly = true
            };

            var colCustomer = new DataGridViewTextBoxColumn
            {
                Name = "colCustomer",
                HeaderText = "CUSTOMER",
                Width = 180,
                ReadOnly = true
            };

            var colDetails = new DataGridViewTextBoxColumn
            {
                Name = "colDetails",
                HeaderText = "DETAILS",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 180,
                ReadOnly = true
            };

            var colAmount = new DataGridViewTextBoxColumn
            {
                Name = "colAmount",
                HeaderText = "AMOUNT",
                Width = 125,
                ReadOnly = true
            };

            var colStatus = new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "STATUS",
                Width = 130,
                ReadOnly = true
            };

            gridTransactions.Columns.AddRange(colDate, colRef, colType, colCustomer, colDetails, colAmount, colStatus);
            gridTransactions.CellPainting += GridTransactions_CellPainting;

            pagination = new PaginationControl
            {
                Dock = DockStyle.Bottom
            };
            pagination.PageChanged += async (newPage) =>
            {
                currentPage = newPage;
                await RefreshDataAsync();
            };

            tableCardPanel.Controls.Add(gridTransactions);
            tableCardPanel.Controls.Add(pagination);
        }

        private void GridTransactions_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            e.PaintBackground(e.ClipBounds, true);

            string colName = gridTransactions.Columns[e.ColumnIndex].Name;
            var record = e.RowIndex < currentRecords.Count ? currentRecords[e.RowIndex] : null;

            if (colName == "colRef" && record != null)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                using var brush = new SolidBrush(UITheme.PrimaryMauve);
                TextRenderer.DrawText(e.Graphics, record.ReferenceNo, font, e.CellBounds, UITheme.PrimaryMauve,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                e.Handled = true;
            }
            else if (colName == "colType" && record != null)
            {
                // Type pill: Order (Blue) or Payment (Green)
                bool isPayment = record.Type == "Payment";
                Color bg = isPayment ? UITheme.StatusGreenBg : UITheme.StatusBlueBg;
                Color fg = isPayment ? UITheme.StatusGreenFg : UITheme.StatusBlueFg;

                int pillWidth = 84;
                int pillHeight = 26;
                int pillX = e.CellBounds.Left + 4;
                int pillY = e.CellBounds.Top + (e.CellBounds.Height - pillHeight) / 2;

                UITheme.DrawStatusPill(e.Graphics, new Rectangle(pillX, pillY, pillWidth, pillHeight), record.Type, bg, fg);
                e.Handled = true;
            }
            else if (colName == "colCustomer" && record != null)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, record.CustomerName, font, e.CellBounds, UITheme.TextDark,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                e.Handled = true;
            }
            else if (colName == "colAmount" && record != null)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                string amtText = $"₱{record.Amount:N2}";
                TextRenderer.DrawText(e.Graphics, amtText, font, e.CellBounds, UITheme.TextDark,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
                e.Handled = true;
            }
            else if (colName == "colStatus" && record != null)
            {
                Color bg = UITheme.StatusYellowBg;
                Color fg = UITheme.StatusYellowFg;

                string st = record.Status.ToLowerInvariant();
                if (st.Contains("completed") || st.Contains("confirmed") || st.Contains("approved"))
                {
                    bg = UITheme.StatusGreenBg;
                    fg = UITheme.StatusGreenFg;
                }
                else if (st.Contains("cancelled") || st.Contains("failed") || st.Contains("refunded"))
                {
                    bg = UITheme.StatusRedBg;
                    fg = UITheme.StatusRedFg;
                }
                else if (st.Contains("processing") || st.Contains("new"))
                {
                    bg = UITheme.StatusBlueBg;
                    fg = UITheme.StatusBlueFg;
                }

                int pillWidth = 96;
                int pillHeight = 26;
                int pillX = e.CellBounds.Left + 4;
                int pillY = e.CellBounds.Top + (e.CellBounds.Height - pillHeight) / 2;

                UITheme.DrawStatusPill(e.Graphics, new Rectangle(pillX, pillY, pillWidth, pillHeight), record.Status, bg, fg);
                e.Handled = true;
            }
            else
            {
                e.PaintContent(e.ClipBounds);
                e.Handled = true;
            }
        }

        // =========================================================
        // 5. DATA REFRESH & AGGREGATION
        // =========================================================
        public async Task RefreshDataAsync()
        {
            try
            {
                // Determine period and type filter
                string period = "All";
                string? typeFilter = null;
                DateTime? fromDate = null;
                DateTime? toDate = null;

                if (activeFilter == "Daily")
                {
                    period = "Daily";
                }
                else if (activeFilter == "Monthly")
                {
                    period = "Monthly";
                }
                else if (activeFilter == "Annually")
                {
                    period = "Annually";
                }
                else if (activeFilter == "Orders")
                {
                    typeFilter = "Orders";
                }
                else if (activeFilter == "Payments")
                {
                    typeFilter = "Payments";
                }
                else if (activeFilter == "Retention")
                {
                    typeFilter = "Retention";
                }

                // If custom From/To pickers are set (non-default), apply range filter
                if (dtpFrom != null && dtpTo != null &&
                    (dtpFrom.Value.Date != DateTime.Today.AddMonths(-3).Date || dtpTo.Value.Date != DateTime.Today.Date))
                {
                    fromDate = dtpFrom.Value.Date;
                    toDate = dtpTo.Value.Date;
                    period = $"range:{fromDate:yyyy-MM-dd}:{toDate:yyyy-MM-dd}";
                }

                // Query database with pagination
                var paged = await CrmDataService.GetOverallTransactionsPagedAsync(
                    period: period,
                    filterDate: selectedDate,
                    typeFilter: typeFilter,
                    searchQuery: activeSearchQuery,
                    page: currentPage,
                    pageSize: PageSize
                );

                if (paged.TotalPages > 0 && currentPage > paged.TotalPages)
                {
                    currentPage = paged.TotalPages;
                    paged = await CrmDataService.GetOverallTransactionsPagedAsync(
                        period: period,
                        filterDate: selectedDate,
                        typeFilter: typeFilter,
                        searchQuery: activeSearchQuery,
                        page: currentPage,
                        pageSize: PageSize
                    );
                }

                currentRecords = paged.Items;

                // Update Grid
                gridTransactions.Rows.Clear();
                foreach (var rec in currentRecords)
                {
                    int rowIdx = gridTransactions.Rows.Add(
                        rec.Date.ToString("yyyy-MM-dd HH:mm"),
                        rec.ReferenceNo,
                        rec.Type,
                        rec.CustomerName,
                        rec.Details,
                        rec.Amount,
                        rec.Status
                    );
                    gridTransactions.Rows[rowIdx].Tag = rec;
                }

                // Update Subtitle count
                lblSubtitle.Text = $"{paged.TotalCount} transaction(s) found \u00B7 {activeFilter} view";
                pagination.SetPagination(paged.Page, paged.PageSize, paged.TotalCount, "transactions");

                // Refresh KPI Summary Cards
                var metrics = await CrmDataService.GetReportSummaryMetricsAsync(selectedDate);
                lblTotalRevenue.Text = $"₱{metrics.TotalRevenue:N0}";
                lblTotalTransactions.Text = metrics.TotalTransactions.ToString();
                lblDailySummary.Text = $"{metrics.DailyCount} (₱{metrics.DailyRevenue:N0})";
                lblMonthlySummary.Text = $"{metrics.MonthlyCount} (₱{metrics.MonthlyRevenue:N0})";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ReportListForm.RefreshDataAsync] {ex.Message}");
            }
        }

        // =========================================================
        // 6. CSV EXPORT ENGINE
        // =========================================================
        private async Task ExportReportAsync(string exportType) // "Daily", "Monthly", "Annually", "All"
        {
            try
            {
                string period = exportType;
                DateTime target = selectedDate;
                string defaultFilename;

                if (exportType == "Daily")
                {
                    defaultFilename = $"SweetStory_Daily_Report_{target:yyyy-MM-dd}.csv";
                }
                else if (exportType == "Monthly")
                {
                    defaultFilename = $"SweetStory_Monthly_Report_{target:yyyy-MM}.csv";
                }
                else if (exportType == "Annually")
                {
                    defaultFilename = $"SweetStory_Annual_Report_{target:yyyy}.csv";
                }
                else
                {
                    defaultFilename = $"SweetStory_Overall_Transactions_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "CSV Spreadsheet (*.csv)|*.csv|All Files (*.*)|*.*",
                    FileName = defaultFilename,
                    Title = $"Export {exportType} Transaction Report"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return;

                // Query records specific to export type
                var exportRecords = await CrmDataService.GetOverallTransactionsAsync(
                    period: period,
                    filterDate: target,
                    typeFilter: null,
                    searchQuery: null
                );

                var sb = new StringBuilder();

                // CSV Header / Metadata Block
                sb.AppendLine("SWEET STORY CUSTOM CAKE CRM - TRANSACTION REPORT");
                sb.AppendLine($"Report Type,{exportType}");
                sb.AppendLine($"Target Date,{target:yyyy-MM-dd}");
                sb.AppendLine($"Generated On,{DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"Total Records,{exportRecords.Count}");
                sb.AppendLine($"Total Amount,PHP {exportRecords.Sum(r => r.Amount):N2}");
                sb.AppendLine();

                // Column Headers
                sb.AppendLine("Date & Time,Reference #,Type,Customer,Details / Method,Amount (PHP),Status");

                // Rows
                foreach (var rec in exportRecords)
                {
                    sb.AppendLine(string.Join(",",
                        EscapeCsv(rec.Date.ToString("yyyy-MM-dd HH:mm")),
                        EscapeCsv(rec.ReferenceNo),
                        EscapeCsv(rec.Type),
                        EscapeCsv(rec.CustomerName),
                        EscapeCsv(rec.Details),
                        rec.Amount.ToString("F2"),
                        EscapeCsv(rec.Status)
                    ));
                }

                // Summary Footer Row
                sb.AppendLine();
                sb.AppendLine(string.Join(",",
                    "TOTAL",
                    "",
                    "",
                    "",
                    "",
                    exportRecords.Sum(r => r.Amount).ToString("F2"),
                    $"{exportRecords.Count} records"
                ));

                // Write with UTF-8 BOM so Excel displays properly
                await File.WriteAllTextAsync(sfd.FileName, sb.ToString(), new UTF8Encoding(true));

                MessageBox.Show(
                    $"Report successfully exported to:\n{sfd.FileName}\n\nTotal Records: {exportRecords.Count}\nTotal Amount: ₱{exportRecords.Sum(r => r.Amount):N2}",
                    "Export Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export report: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string EscapeCsv(string val)
        {
            if (string.IsNullOrEmpty(val)) return "\"\"";
            if (val.Contains(",") || val.Contains("\"") || val.Contains("\n") || val.Contains("\r"))
            {
                return $"\"{val.Replace("\"", "\"\"")}\"";
            }
            return $"\"{val}\"";
        }
    }
}
