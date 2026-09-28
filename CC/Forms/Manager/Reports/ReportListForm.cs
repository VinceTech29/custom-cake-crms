using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Manager.Reports
{
    /// <summary>
    /// Manager Reports module displaying overall transactions (orders and payments)
    /// with date-range filtering, multi-branch support, live KPI summaries, and PDF export capabilities.
    /// </summary>
    public class ReportListForm : Form, INavigationAware
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnTopExportPdf = null!;

        private TableLayoutPanel kpiTable = null!;
        private Label lblTotalRevenue = null!;
        private Label lblTotalTransactions = null!;
        private Label lblDailySummary = null!;
        private Label lblMonthlySummary = null!;

        private Panel searchFilterPanel = null!;
        private Panel searchPill = null!;
        private TextBox txtSearchBox = null!;
        private FlowLayoutPanel filterPillContainer = null!;
        private Label lblBranch = null!;
        private ComboBox cmbBranchFilter = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private Label lblFromDate = null!;
        private Label lblToDate = null!;
        private Button btnGenerateReport = null!;
        private Button btnExportPdf = null!;

        private Panel tableCardPanel = null!;
        private DataGridView gridTransactions = null!;
        private Label lblNoRecordsNotice = null!;
        private PaginationControl pagination = null!;
        private int currentPage = 1;
        private const int PageSize = 10;

        private string activeFilter = "All"; // "All", "Orders", "Payments", "Retention"
        private string activeSearchQuery = string.Empty;
        private List<TransactionRecord> currentRecords = new List<TransactionRecord>();

        private BranchCapabilityInfo? branchCapability;
        private List<BranchListItem> availableBranches = new();
        private bool _isInitialized = false;

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

            Load += async (s, e) =>
            {
                if (!_isInitialized)
                {
                    await InitializeDataAsync();
                }
            };
        }

        public async Task InitializeDataAsync()
        {
            _isInitialized = true;
            await LoadBranchFiltersAsync();
            await RefreshDataAsync();
        }

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
        // 1. TOP TOOLBAR (Title, Subtitle, and Top Export PDF Button)
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
                Text = "Overall transactions, revenue, and date-range analytics",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            // Action button stack on the right
            var btnStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0)
            };

            btnTopExportPdf = new Button
            {
                Text = "Export PDF",
                Height = 38,
                Width = 135,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(175, 45, 45),
                ForeColor = Color.White,
                Margin = new Padding(6, 0, 0, 0)
            };
            btnTopExportPdf.FlatAppearance.BorderSize = 0;
            btnTopExportPdf.ApplyRoundedRegion(10);
            UITheme.ApplyActionButton(btnTopExportPdf, "\uE787", 12);
            btnTopExportPdf.Click += async (s, e) => await ExportPdfAsync();

            btnStack.Controls.Add(btnTopExportPdf);

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnStack, 1, 0);

            topPanel.Controls.Add(headerTable);
        }

        // =========================================================
        // 2. 4 KPI SUMMARY CARDS (Total Revenue, Transactions, Daily, Monthly)
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

            var cardRev = CreateKpiCard("TOTAL REVENUE", "\uE8C7", Color.FromArgb(22, 163, 74), out lblTotalRevenue);
            var cardTxn = CreateKpiCard("TOTAL TRANSACTIONS", "\uE719", Color.FromArgb(31, 27, 24), out lblTotalTransactions);
            var cardDaily = CreateKpiCard("TODAY'S TRANSACTIONS", "\uE916", Color.FromArgb(217, 119, 6), out lblDailySummary);
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
        // 3. SEARCH & DATE FILTER BAR (With Branch Selector)
        // =========================================================
        private void BuildSearchAndFilterBar()
        {
            searchFilterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 94,
                Padding = new Padding(0, 0, 0, 14),
                BackColor = Color.Transparent
            };

            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 2,
                ColumnCount = 1,
                BackColor = Color.Transparent
            };
            mainTable.RowStyles.Clear();
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));

            var row1Flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Search pill
            searchPill = new Panel
            {
                Size = new Size(260, 36),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 12, 0)
            };
            searchPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, searchPill.Width - 1, searchPill.Height - 1), 12);

                using var font = new Font("Segoe MDL2 Assets", 10.5f);
                var rect = new Rectangle(12, 0, 20, searchPill.Height);
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
                Location = new Point(38, 9),
                Width = searchPill.Width - 48
            };
            txtSearchBox.TextChanged += async (s, e) =>
            {
                activeSearchQuery = txtSearchBox.Text.Trim();
                currentPage = 1;
                await RefreshDataAsync();
            };
            searchPill.Controls.Add(txtSearchBox);
            row1Flow.Controls.Add(searchPill);

            // Filter pills: All, Orders, Payments, Retention
            filterPillContainer = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 12, 0)
            };

            string[] filters = new[] { "All", "Orders", "Payments", "Retention" };
            foreach (var filterName in filters)
            {
                var btn = new Button
                {
                    Text = filterName switch
                    {
                        "All" => "All Transactions",
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
                        e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, b.Width - 1, b.Height - 1), 14);
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
            row1Flow.Controls.Add(filterPillContainer);

            // Branch Dropdown (Visible only if Multi-Branching is allowed)
            lblBranch = new Label
            {
                Text = "Branch:",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(4, 9, 4, 0),
                Visible = false
            };
            cmbBranchFilter = new ComboBox
            {
                Width = 160,
                Height = 36,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F),
                BackColor = Color.White,
                ForeColor = UITheme.TextDark,
                Margin = new Padding(0, 1, 0, 0),
                Visible = false
            };
            cmbBranchFilter.SelectedIndexChanged += async (s, e) =>
            {
                currentPage = 1;
                await RefreshDataAsync();
            };

            row1Flow.Controls.Add(lblBranch);
            row1Flow.Controls.Add(cmbBranchFilter);

            // Row 2: Date Range Pickers & Execution Buttons
            var row2Flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0)
            };

            lblFromDate = new Label
            {
                Text = "From Date:",
                AutoSize = true,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                Margin = new Padding(0, 9, 4, 0)
            };

            dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddDays(-30),
                Height = 36,
                Width = 115,
                Font = new Font(UITheme.FontSans, 9.5F),
                Margin = new Padding(0, 1, 14, 0)
            };

            lblToDate = new Label
            {
                Text = "To Date:",
                AutoSize = true,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                Margin = new Padding(0, 9, 4, 0)
            };

            dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Height = 36,
                Width = 115,
                Font = new Font(UITheme.FontSans, 9.5F),
                Margin = new Padding(0, 1, 14, 0)
            };

            btnGenerateReport = new Button
            {
                Text = "Generate Report",
                Height = 36,
                Width = 150,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnGenerateReport.FlatAppearance.BorderSize = 0;
            btnGenerateReport.ApplyRoundedRegion(10);
            UITheme.ApplyActionButton(btnGenerateReport, "\uE768", 11);
            btnGenerateReport.Click += async (s, e) =>
            {
                if (dtpFrom.Value.Date > dtpTo.Value.Date)
                {
                    MessageBox.Show(
                        "\"From Date\" cannot be later than \"To Date\". Please adjust your date range.",
                        "Validation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
                currentPage = 1;
                await RefreshDataAsync();
            };

            btnExportPdf = new Button
            {
                Text = "Export PDF",
                Height = 36,
                Width = 130,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(175, 45, 45),
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 0)
            };
            btnExportPdf.FlatAppearance.BorderSize = 0;
            btnExportPdf.ApplyRoundedRegion(10);
            UITheme.ApplyActionButton(btnExportPdf, "\uE787", 11);
            btnExportPdf.Click += async (s, e) => await ExportPdfAsync();

            row2Flow.Controls.Add(lblFromDate);
            row2Flow.Controls.Add(dtpFrom);
            row2Flow.Controls.Add(lblToDate);
            row2Flow.Controls.Add(dtpTo);
            row2Flow.Controls.Add(btnGenerateReport);
            row2Flow.Controls.Add(btnExportPdf);

            mainTable.Controls.Add(row1Flow, 0, 0);
            mainTable.Controls.Add(row2Flow, 0, 1);

            searchFilterPanel.Controls.Add(mainTable);
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
        // 4. BRANCH FILTER LOADER
        // =========================================================
        private async Task LoadBranchFiltersAsync()
        {
            try
            {
                branchCapability = await CrmDataService.GetBranchCapabilityAsync();
                bool canBranch = branchCapability != null && branchCapability.AllowBranching;

                lblBranch.Visible = canBranch;
                cmbBranchFilter.Visible = canBranch;

                if (canBranch)
                {
                    availableBranches = await CrmDataService.GetBranchesAsync();
                    cmbBranchFilter.Items.Clear();
                    cmbBranchFilter.Items.Add(new ComboBoxItem("All Branches", 0));
                    foreach (var b in availableBranches.Where(b => b.IsActive))
                    {
                        cmbBranchFilter.Items.Add(new ComboBoxItem(b.BranchName, b.BranchId));
                    }

                    int? targetBranchId = SessionService.CurrentUser?.BranchId;
                    bool isStaff = string.Equals(SessionService.CurrentUser?.Role, "Staff", StringComparison.OrdinalIgnoreCase);

                    if (targetBranchId.HasValue && targetBranchId.Value > 0)
                    {
                        for (int i = 0; i < cmbBranchFilter.Items.Count; i++)
                        {
                            if (cmbBranchFilter.Items[i] is ComboBoxItem item && item.Value == targetBranchId.Value)
                            {
                                cmbBranchFilter.SelectedIndex = i;
                                break;
                            }
                        }
                        if (isStaff)
                        {
                            cmbBranchFilter.Enabled = false;
                        }
                        else if (cmbBranchFilter.SelectedIndex < 0)
                        {
                            cmbBranchFilter.SelectedIndex = 0;
                        }
                    }
                    else
                    {
                        cmbBranchFilter.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LoadBranchFiltersAsync] {ex.Message}");
            }
        }

        // =========================================================
        // 5. TRANSACTIONS DATA TABLE
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

            lblNoRecordsNotice = new Label
            {
                Text = "No transaction records found matching the specified date range and branch.",
                Font = new Font(UITheme.FontSans, 11F, FontStyle.Italic),
                ForeColor = UITheme.TextMuted,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Visible = false,
                BackColor = Color.White
            };

            pagination = new PaginationControl
            {
                Dock = DockStyle.Bottom
            };
            pagination.PageChanged += async (newPage) =>
            {
                currentPage = newPage;
                await RefreshDataAsync();
            };

            tableCardPanel.Controls.Add(lblNoRecordsNotice);
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
        // 6. DATA REFRESH & AGGREGATION
        // =========================================================
        public async Task RefreshDataAsync()
        {
            try
            {
                DateTime fromDate = dtpFrom.Value.Date;
                DateTime toDate = dtpTo.Value.Date;

                if (fromDate > toDate)
                {
                    MessageBox.Show(
                        "\"From Date\" cannot be later than \"To Date\".",
                        "Invalid Date Range",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                int? branchId = null;
                string branchDisplayName = "All Branches";
                if (cmbBranchFilter != null && cmbBranchFilter.Visible && cmbBranchFilter.SelectedItem is ComboBoxItem sel)
                {
                    branchId = sel.Value > 0 ? sel.Value : 0;
                    branchDisplayName = sel.Text;
                }
                else if (SessionService.CurrentUser?.BranchId.HasValue == true && SessionService.CurrentUser.BranchId.Value > 0)
                {
                    branchId = SessionService.CurrentUser.BranchId.Value;
                    branchDisplayName = SessionService.CurrentUser.BranchName ?? "Current Branch";
                }

                string period = $"range:{fromDate:yyyy-MM-dd}:{toDate:yyyy-MM-dd}";
                string? typeFilter = activeFilter switch
                {
                    "Orders" => "Orders",
                    "Payments" => "Payments",
                    "Retention" => "Retention",
                    _ => null
                };

                // Query database with pagination
                var paged = await CrmDataService.GetOverallTransactionsPagedAsync(
                    period: period,
                    filterDate: null,
                    typeFilter: typeFilter,
                    searchQuery: activeSearchQuery,
                    page: currentPage,
                    pageSize: PageSize,
                    fromDate: fromDate,
                    toDate: toDate,
                    branchId: branchId
                );

                if (paged.TotalPages > 0 && currentPage > paged.TotalPages)
                {
                    currentPage = paged.TotalPages;
                    paged = await CrmDataService.GetOverallTransactionsPagedAsync(
                        period: period,
                        filterDate: null,
                        typeFilter: typeFilter,
                        searchQuery: activeSearchQuery,
                        page: currentPage,
                        pageSize: PageSize,
                        fromDate: fromDate,
                        toDate: toDate,
                        branchId: branchId
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

                // Handle empty results notice
                if (currentRecords.Count == 0)
                {
                    lblNoRecordsNotice.Visible = true;
                    gridTransactions.Visible = false;
                    lblSubtitle.Text = $"0 transactions found ({fromDate:MMM dd, yyyy} - {toDate:MMM dd, yyyy}) \u00B7 {branchDisplayName}";
                }
                else
                {
                    lblNoRecordsNotice.Visible = false;
                    gridTransactions.Visible = true;
                    lblSubtitle.Text = $"{paged.TotalCount} transaction(s) found ({fromDate:MMM dd, yyyy} - {toDate:MMM dd, yyyy}) \u00B7 {branchDisplayName}";
                }

                pagination.SetPagination(paged.Page, paged.PageSize, paged.TotalCount, "transactions");

                // Refresh KPI Summary Cards
                var metrics = await CrmDataService.GetReportSummaryMetricsAsync(toDate);
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
        // 7. PDF EXPORT ENGINE
        // =========================================================
        private async Task ExportPdfAsync()
        {
            try
            {
                DateTime fromDate = dtpFrom.Value.Date;
                DateTime toDate = dtpTo.Value.Date;

                if (fromDate > toDate)
                {
                    MessageBox.Show(
                        "\"From Date\" cannot be later than \"To Date\". Please adjust your date range before exporting.",
                        "Validation Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                // Determine branch filter
                int? branchIdFilter = null;
                string branchDisplayName = "All Branches";
                if (cmbBranchFilter.Visible && cmbBranchFilter.SelectedItem is ComboBoxItem selBranch)
                {
                    branchIdFilter = selBranch.Value > 0 ? selBranch.Value : 0;
                    branchDisplayName = selBranch.Text;
                }
                else if (SessionService.CurrentUser?.BranchId.HasValue == true && SessionService.CurrentUser.BranchId.Value > 0)
                {
                    branchIdFilter = SessionService.CurrentUser.BranchId.Value;
                    branchDisplayName = SessionService.CurrentUser.BranchName ?? "Current Branch";
                }

                string period = $"range:{fromDate:yyyy-MM-dd}:{toDate:yyyy-MM-dd}";
                string? typeFilter = activeFilter switch
                {
                    "Orders" => "Orders",
                    "Payments" => "Payments",
                    "Retention" => "Retention",
                    _ => null
                };

                // Query all matching database records
                var exportRecords = await CrmDataService.GetOverallTransactionsAsync(
                    period: period,
                    filterDate: null,
                    typeFilter: typeFilter,
                    searchQuery: activeSearchQuery,
                    fromDate: fromDate,
                    toDate: toDate,
                    branchId: branchIdFilter
                );

                if (exportRecords.Count == 0)
                {
                    MessageBox.Show(
                        "No transaction records found matching the specified date range and branch.\nCannot export an empty report.",
                        "Export PDF",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }

                string safeBrand = string.Join("_", SessionService.GetActiveBrandName().Split(Path.GetInvalidFileNameChars())).Replace(" ", "_");
                string defaultFilename = $"{safeBrand}_Transactions_Report_{fromDate:yyyyMMdd}_to_{toDate:yyyyMMdd}.pdf";

                using var sfd = new SaveFileDialog
                {
                    Filter = "PDF Document (*.pdf)|*.pdf|All Files (*.*)|*.*",
                    FileName = defaultFilename,
                    Title = "Export PDF Report"
                };

                if (sfd.ShowDialog() != DialogResult.OK) return;

                string reportTypeLabel = activeFilter switch
                {
                    "Orders" => "Orders Report",
                    "Payments" => "Payments Report",
                    "Retention" => "Retention Requests Report",
                    _ => "Overall Transactions & Revenue"
                };

                var options = new PdfReportOptions
                {
                    CompanyName = SessionService.GetActiveBrandName(),
                    BranchName = branchDisplayName,
                    ReportTitle = "OVERALL TRANSACTIONS & REVENUE REPORT",
                    ReportType = reportTypeLabel,
                    FromDate = fromDate,
                    ToDate = toDate,
                    ActiveSearchQuery = activeSearchQuery,
                    GeneratedBy = SessionService.CurrentUser?.FullName ?? "Administrator",
                    GeneratedDate = DateTime.Now,
                    Records = exportRecords,
                    Orientation = PdfSharp.PageOrientation.Landscape
                };

                PdfReportService.GenerateTransactionReport(sfd.FileName, options);

                // Save generation audit log in database
                await CrmDataService.LogReportGenerationAsync(
                    reportType: "Overall Transactions Report",
                    parameters: new Dictionary<string, string>
                    {
                        { "FromDate", fromDate.ToString("yyyy-MM-dd") },
                        { "ToDate", toDate.ToString("yyyy-MM-dd") },
                        { "Branch", branchDisplayName },
                        { "FilterType", activeFilter },
                        { "RecordCount", exportRecords.Count.ToString() },
                        { "TotalAmount", exportRecords.Sum(r => r.Amount).ToString("F2") }
                    }
                );

                MessageBox.Show(
                    $"Report successfully exported to:\n{sfd.FileName}\n\nTotal Records: {exportRecords.Count}\nTotal Revenue: PHP {exportRecords.Sum(r => r.Amount):N2}",
                    "PDF Export Complete",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export PDF report: {ex.Message}", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================================================
        // HELPER COMBOBOX ITEM CLASS
        // =========================================================
        private class ComboBoxItem
        {
            public string Text { get; }
            public int Value { get; }

            public ComboBoxItem(string text, int value)
            {
                Text = text;
                Value = value;
            }

            public override string ToString() => Text;
        }
    }
}
