using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Retention
{
    public class RetentionCampaignsForm : Form, INavigationAware
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Label lblRoleBadge = null!;
        private Button btnAutoSendToggle = null!;
        private Button btnRecalculate = null!;
        private Button btnManualEmail = null!;

        // Tab Navigation
        private Panel tabNavBar = null!;
        private Button btnTabSegments = null!;
        private Button btnTabCampaigns = null!;
        private Button btnTabReports = null!;
        private Button btnTabLogs = null!;
        private Button btnTabSettings = null!;
        private Button btnTabRequests = null!;
        private string activeTab = "Segments"; // "Segments", "Campaigns", "Reports", "Logs", "Settings", "Requests"

        // Tab Container Panels
        private Panel mainContentContainer = null!;
        private Panel panelSegments = null!;
        private Panel panelCampaigns = null!;
        private Panel panelReports = null!;
        private Panel panelLogs = null!;
        private Panel panelSettings = null!;
        private Panel panelRequests = null!;

        // --- SEGMENTS TAB CONTROLS ---
        private TableLayoutPanel kpiTableSegments = null!;
        private Panel searchFilterPanel = null!;
        private TextBox txtSearchBox = null!;
        private ComboBox cmbSegmentFilter = null!;
        private Button btnSendSegmentCampaign = null!;
        private Panel tableCardPanel = null!;
        private DataGridView gridCustomers = null!;
        private PaginationControl pagination = null!;
        private int currentPage = 1;
        private const int PageSize = 10;
        private string activeSearchQuery = string.Empty;
        private string activeSegmentFilter = "All";

        // --- CAMPAIGNS TAB CONTROLS ---
        private FlowLayoutPanel flowCampaignCards = null!;

        // --- REPORTS TAB CONTROLS ---
        private TableLayoutPanel kpiTableMetrics = null!;
        private DataGridView gridSegmentPerformance = null!;

        // --- LOGS TAB CONTROLS ---
        private DataGridView gridEmailLogs = null!;
        private DataGridView gridAuditLogs = null!;
        private Button btnShowEmailLogs = null!;
        private Button btnShowAuditLogs = null!;
        private bool viewingEmailLogs = true;

        // --- SETTINGS TAB CONTROLS (Admin only) ---
        private NumericUpDown numActiveDays = null!;
        private NumericUpDown numAtRiskDays = null!;
        private NumericUpDown numLoyalOrders = null!;
        private NumericUpDown numCooldownDays = null!;
        private Button btnSaveSettings = null!;
        private Label lblSettingsNotice = null!;

        // --- REQUESTS TAB CONTROLS ---
        private DataGridView gridRetentionRequests = null!;
        private PaginationControl paginationRequests = null!;
        private ComboBox cmbRequestStatusFilter = null!;
        private TextBox txtRequestSearch = null!;
        private DateTimePicker dtpRequestFrom = null!;
        private DateTimePicker dtpRequestTo = null!;
        private int requestsCurrentPage = 1;
        private const int RequestsPageSize = 15;
        private string requestsStatusFilter = "All";
        private string requestsSearchQuery = string.Empty;
        private List<CC.Domain.Entities.RetentionRequest>? _cachedRequests;

        // Data Cache
        private RetentionDashboardData? currentData;
        private bool isAccessDenied = false;

        public RetentionCampaignsForm()
        {
            // Strict Backend / Client RBAC Check
            string role = SessionService.CurrentUser?.Role ?? "";
            bool hasAccess = role == "Business Admin" || role == "Admin" || role == "Manager";

            if (!hasAccess)
            {
                isAccessDenied = true;
                _ = CrmDataService.RecordAuditLogAsync(
                    userId: SessionService.CurrentUser?.UserId ?? 0,
                    actionType: "UNAUTHORIZED_ACCESS_ATTEMPT",
                    actionDesc: $"Blocked direct load of RetentionCampaignsForm for user '{SessionService.CurrentUser?.Username ?? "Unknown"}' with role '{role}'"
                );

                MessageBox.Show(
                    "You do not have permission to access this feature.",
                    "Access Denied",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                Load += (s, e) => Close();
                return;
            }

            InitializeComponent();
            BuildTopHeader();
            BuildTabNavBar();
            BuildSegmentsTab();
            BuildCampaignsTab();
            BuildReportsTab();
            BuildLogsTab();
            BuildSettingsTab();
            BuildRequestsTab();

            mainContentContainer.Controls.Add(panelSegments);
            mainContentContainer.Controls.Add(panelCampaigns);
            mainContentContainer.Controls.Add(panelReports);
            mainContentContainer.Controls.Add(panelLogs);
            mainContentContainer.Controls.Add(panelSettings);
            mainContentContainer.Controls.Add(panelRequests);

            Controls.Add(mainContentContainer);
            Controls.Add(tabNavBar);
            Controls.Add(topPanel);

            SwitchTab("Segments");
            Shown += (s, e) => { ActiveControl = null; };
        }

        public async Task InitializeDataAsync() => await RefreshDataAsync();

        private void InitializeComponent()
        {
            SuspendLayout();
            DoubleBuffered = true;
            BackColor = UITheme.CreamBackground;
            Dock = DockStyle.Fill;
            AutoScroll = true;

            mainContentContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Padding = new Padding(24, 12, 24, 24)
            };

            ResumeLayout(false);
        }

        // =========================================================
        // TOP HEADER
        // =========================================================

        private void BuildTopHeader()
        {
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                Padding = new Padding(24, 12, 24, 8),
                BackColor = Color.Transparent
            };

            // Action Buttons (Right)
            var actionStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Right,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };

            btnManualEmail = new Button
            {
                Text = "\u2709 Email",
                Height = 36,
                Width = 95,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                BackColor = Color.FromArgb(248, 238, 242),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 0)
            };
            btnManualEmail.FlatAppearance.BorderColor = UITheme.PrimaryMauve;
            btnManualEmail.Click += async (s, e) => await ShowManualEmailDialogAsync();

            btnAutoSendToggle = new Button
            {
                Text = "Auto-Send: ON",
                Height = 36,
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.StatusGreenFg,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 8, 0)
            };
            btnAutoSendToggle.FlatAppearance.BorderSize = 0;
            btnAutoSendToggle.Click += async (s, e) =>
            {
                if (currentData == null) return;
                bool newStatus = !currentData.Settings.AutoSendingEnabled;
                await CrmDataService.ToggleAutoSendingAsync(newStatus, SessionService.CurrentUser?.CompanyId);
                await RefreshDataAsync();
            };

            btnRecalculate = new Button
            {
                Text = "\u26A1 Recalculate",
                Height = 36,
                Width = 125,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 0)
            };
            btnRecalculate.FlatAppearance.BorderSize = 0;
            btnRecalculate.Click += async (s, e) =>
            {
                btnRecalculate.Enabled = false;
                btnRecalculate.Text = "Calculating...";
                try
                {
                    int evaluated = await CrmDataService.RecalculateCustomerSegmentsAsync(SessionService.CurrentUser?.CompanyId);
                    await RefreshDataAsync();
                    MessageBox.Show($"Successfully recalculated retention segments for {evaluated} customer(s).", "Segmentation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                finally
                {
                    btnRecalculate.Enabled = true;
                    btnRecalculate.Text = "\u26A1 Recalculate";
                }
            };

            actionStack.Controls.Add(btnManualEmail);
            actionStack.Controls.Add(btnAutoSendToggle);
            actionStack.Controls.Add(btnRecalculate);

            var titleStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Dock = DockStyle.Left,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var titleRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Margin = Padding.Empty
            };

            lblTitle = new Label
            {
                Text = "Customer Retention & Email Campaigns",
                Font = new Font(UITheme.FontSerif, 15F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 10, 0)
            };

            bool isAdmin = SessionService.CurrentUser?.Role == "Business Admin" || SessionService.CurrentUser?.Role == "Admin";
            lblRoleBadge = new Label
            {
                Text = isAdmin ? "ADMIN ACCESS" : "MANAGER ACCESS",
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = isAdmin ? UITheme.PrimaryMauve : UITheme.StatusBlueFg,
                BackColor = isAdmin ? Color.FromArgb(245, 230, 235) : UITheme.StatusBlueBg,
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(0, 2, 0, 0),
                AutoSize = true
            };

            titleRow.Controls.Add(lblTitle);
            titleRow.Controls.Add(lblRoleBadge);

            lblSubtitle = new Label
            {
                Text = "Order-driven retention segments, automated offers & win-back campaigns",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0, 2, 0, 0)
            };

            titleStack.Controls.Add(titleRow);
            titleStack.Controls.Add(lblSubtitle);

            topPanel.Controls.Add(titleStack);
            topPanel.Controls.Add(actionStack);
        }

        // =========================================================
        // TAB NAV BAR
        // =========================================================

        private void BuildTabNavBar()
        {
            tabNavBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(24, 0, 24, 0),
                BackColor = Color.Transparent
            };

            var tabFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            btnTabSegments = CreateTabButton("Segments & Customers", "Segments");
            btnTabCampaigns = CreateTabButton("Email Campaigns (5)", "Campaigns");
            btnTabReports = CreateTabButton("Performance & Tracking", "Reports");
            btnTabLogs = CreateTabButton("Email & Audit Logs", "Logs");
            btnTabSettings = CreateTabButton("Settings (Admin)", "Settings");
            btnTabRequests = CreateTabButton("Retention Requests", "Requests");

            bool isAdmin = SessionService.CurrentUser?.Role == "Business Admin" || SessionService.CurrentUser?.Role == "Admin";
            if (!isAdmin)
            {
                btnTabSettings.Visible = false; // Hide settings tab from non-admins
            }

            tabFlow.Controls.Add(btnTabSegments);
            tabFlow.Controls.Add(btnTabCampaigns);
            tabFlow.Controls.Add(btnTabReports);
            tabFlow.Controls.Add(btnTabLogs);
            tabFlow.Controls.Add(btnTabSettings);
            tabFlow.Controls.Add(btnTabRequests);

            tabNavBar.Controls.Add(tabFlow);
        }

        private Button CreateTabButton(string label, string tabKey)
        {
            var btn = new Button
            {
                Text = label,
                Tag = tabKey,
                Height = 36,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(14, 0, 14, 0)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += (s, e) => SwitchTab(tabKey);
            return btn;
        }

        public void SwitchTab(string tabKey)
        {
            activeTab = tabKey;

            // Highlight buttons
            foreach (Button b in new[] { btnTabSegments, btnTabCampaigns, btnTabReports, btnTabLogs, btnTabSettings, btnTabRequests })
            {
                if (b == null || !b.Visible) continue;
                bool isSel = (string?)b.Tag == activeTab;
                b.Font = new Font(UITheme.FontSans, 9.5F, isSel ? FontStyle.Bold : FontStyle.Regular);
                b.ForeColor = isSel ? Color.White : UITheme.TextMuted;
                b.BackColor = isSel ? UITheme.PrimaryMauve : Color.White;
            }

            // Show panel
            panelSegments.Visible = activeTab == "Segments";
            panelCampaigns.Visible = activeTab == "Campaigns";
            panelReports.Visible = activeTab == "Reports";
            panelLogs.Visible = activeTab == "Logs";
            panelSettings.Visible = activeTab == "Settings";
            panelRequests.Visible = activeTab == "Requests";

            // Load Requests tab data on switch only if not already cached
            if (activeTab == "Requests" && _cachedRequests == null)
                _ = RefreshRequestsTabAsync();
        }

        // =========================================================
        // TAB 1: SEGMENTS & CUSTOMERS
        // =========================================================

        private void BuildSegmentsTab()
        {
            panelSegments = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            // 1. 5 Segment KPI Cards
            kpiTableSegments = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 5,
                RowCount = 1,
                Height = 142,
                Margin = new Padding(0, 0, 0, 16),
                BackColor = Color.Transparent
            };
            for (int i = 0; i < 5; i++)
                kpiTableSegments.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));

            // 2. Filter & Search Strip
            searchFilterPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 12)
            };

            var searchPill = new Panel
            {
                Width = 260,
                Height = 36,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 16, 0)
            };
            searchPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, searchPill.Width - 1, searchPill.Height - 1), 6);
            };

            txtSearchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font(UITheme.FontSans, 9.5F),
                ForeColor = UITheme.TextDark,
                Location = new Point(10, 8),
                Width = 240,
                PlaceholderText = "Search customer, email..."
            };
            txtSearchBox.TextChanged += (s, e) =>
            {
                activeSearchQuery = txtSearchBox.Text.Trim();
                currentPage = 1;
                RenderCustomerGrid();
            };
            searchPill.Controls.Add(txtSearchBox);

            var lblFilter = new Label
            {
                Text = "Filter Segment:",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 9, 8, 0)
            };

            cmbSegmentFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9F),
                Height = 36,
                Width = 140,
                Margin = new Padding(0, 4, 16, 0)
            };
            cmbSegmentFilter.Items.AddRange(new object[] { "All Segments", "New", "Returning", "Loyal", "At Risk", "Inactive" });
            cmbSegmentFilter.SelectedIndex = 0;
            cmbSegmentFilter.SelectedIndexChanged += (s, e) =>
            {
                activeSegmentFilter = cmbSegmentFilter.SelectedIndex == 0 ? "All" : cmbSegmentFilter.SelectedItem?.ToString() ?? "All";
                currentPage = 1;
                RenderCustomerGrid();
                UpdateSendSegmentButtonText();
            };

            btnSendSegmentCampaign = new Button
            {
                Text = "Send Campaign to Segment",
                Height = 36,
                Width = 230,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 2, 0, 0)
            };
            btnSendSegmentCampaign.FlatAppearance.BorderSize = 0;
            btnSendSegmentCampaign.Click += async (s, e) =>
            {
                if (activeSegmentFilter == "All")
                {
                    MessageBox.Show("Please select a specific segment from the filter dropdown first (e.g. Loyal, At Risk, New) before sending a campaign.", "Select Segment", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var eligible = GetFilteredCustomers().Where(c => c.EligibleToSend).ToList();
                if (!eligible.Any())
                {
                    MessageBox.Show($"No eligible customers found in segment '{activeSegmentFilter}' (either on 14-day cooldown or missing email).", "No Recipients", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var confirm = MessageBox.Show(
                    $"Are you sure you want to approve and send the '{activeSegmentFilter}' retention campaign to {eligible.Count} eligible customer(s)?\n\nAnti-fatigue rule: Recipients will be placed on a 14-day cooldown.",
                    "Confirm Campaign Send",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirm == DialogResult.Yes)
                {
                    var (sent, skipped) = await CrmDataService.SendCampaignForSegmentAsync(activeSegmentFilter, SessionService.CurrentUser?.CompanyId);
                    await RefreshDataAsync();
                    MessageBox.Show($"Campaign completed!\n\nDelivered: {sent} emails\nSkipped (14d cooldown): {skipped}", "Campaign Dispatched", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            searchFilterPanel.Controls.Add(searchPill);
            searchFilterPanel.Controls.Add(lblFilter);
            searchFilterPanel.Controls.Add(cmbSegmentFilter);
            searchFilterPanel.Controls.Add(btnSendSegmentCampaign);

            // 3. Customer Data Table
            tableCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            tableCardPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, tableCardPanel.Width - 1, tableCardPanel.Height - 1), 8);
            };

            gridCustomers = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = UITheme.BorderColor,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };
            UITheme.ApplyTableStyle(gridCustomers);
            gridCustomers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridCustomers.RowTemplate.Height = 46;
            gridCustomers.ColumnHeadersHeight = 42;

            // Columns
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "CustomerName", HeaderText = "CUSTOMER", FillWeight = 16, MinimumWidth = 110 });
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "EMAIL", FillWeight = 20, MinimumWidth = 130 });
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Orders", HeaderText = "COMPLETED", FillWeight = 10, MinimumWidth = 75 });
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "LastOrder", HeaderText = "LAST ORDER", FillWeight = 12, MinimumWidth = 85 });
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Recency", HeaderText = "RECENCY", FillWeight = 9, MinimumWidth = 75 });
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Segment", HeaderText = "RETENTION SEGMENT", FillWeight = 12, MinimumWidth = 85 });
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn { Name = "Cooldown", HeaderText = "SEND STATUS", FillWeight = 11, MinimumWidth = 85 });
            gridCustomers.Columns.Add(new DataGridViewButtonColumn
            {
                Name = "ActionSend",
                HeaderText = "ACTION",
                Text = "\u2709 Email",
                UseColumnTextForButtonValue = true,
                FillWeight = 10,
                MinimumWidth = 80,
                FlatStyle = FlatStyle.Flat
            });

            gridCustomers.CellContentClick += async (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var actionCol = gridCustomers.Columns["ActionSend"];
                if (actionCol != null && e.ColumnIndex == actionCol.Index)
                {
                    var filtered = GetFilteredCustomers();
                    int targetIndex = (currentPage - 1) * PageSize + e.RowIndex;
                    if (targetIndex >= 0 && targetIndex < filtered.Count)
                    {
                        var customer = filtered[targetIndex];
                        await ShowManualEmailDialogAsync(customer.SegmentName, customer);
                    }
                }
            };

            gridCustomers.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Value == null) return;
                string colName = gridCustomers.Columns[e.ColumnIndex].Name;

                if (colName == "Segment")
                {
                    string seg = e.Value.ToString() ?? "";
                    e.CellStyle.Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold);
                    e.CellStyle.ForeColor = seg switch
                    {
                        "New" => Color.FromArgb(41, 128, 185),
                        "Returning" => Color.FromArgb(39, 174, 96),
                        "Loyal" => Color.FromArgb(142, 68, 173),
                        "At Risk" => Color.FromArgb(230, 126, 34),
                        _ => Color.FromArgb(192, 57, 43)
                    };
                }
                else if (colName == "Cooldown")
                {
                    string status = e.Value.ToString() ?? "";
                    e.CellStyle.Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
                    if (status.Contains("Eligible"))
                    {
                        e.CellStyle.ForeColor = UITheme.StatusGreenFg;
                    }
                    else
                    {
                        e.CellStyle.ForeColor = UITheme.StatusYellowFg;
                    }
                }
                else if (colName == "ActionSend")
                {
                    e.CellStyle.BackColor = UITheme.PrimaryMauve;
                    e.CellStyle.ForeColor = Color.White;
                    e.CellStyle.Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
                    e.CellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
                }
            };

            pagination = new PaginationControl { Dock = DockStyle.Bottom };
            pagination.PageChanged += (newPage) =>
            {
                currentPage = newPage;
                RenderCustomerGrid();
            };

            tableCardPanel.Controls.Add(gridCustomers);
            tableCardPanel.Controls.Add(pagination);

            panelSegments.Controls.Add(tableCardPanel);
            panelSegments.Controls.Add(searchFilterPanel);
            panelSegments.Controls.Add(kpiTableSegments);
        }

        private void RenderSegmentCards()
        {
            if (currentData == null) return;
            kpiTableSegments.SuspendLayout();
            kpiTableSegments.Controls.Clear();

            var segs = new[] { "New", "Returning", "Loyal", "At Risk", "Inactive" };
            for (int i = 0; i < segs.Length; i++)
            {
                string segKey = segs[i];
                var info = currentData.SegmentSummaries.ContainsKey(segKey)
                    ? currentData.SegmentSummaries[segKey]
                    : new RetentionSegmentSummary(segKey, 0, 0, "", "", Color.Gray);

                var card = new DashboardKpiCard
                {
                    Dock = DockStyle.Fill,
                    Title = info.SegmentName,
                    Value = info.CustomerCount.ToString(),
                    Subtitle = $"{info.PercentageOfBase}% of base\n{info.OfferSummary}",
                    Margin = new Padding(i == 0 ? 0 : 5, 0, i == 4 ? 0 : 5, 0),
                    Height = 142
                };
                card.SetBadge(segKey.ToUpper(), info.ThemeColor, Color.FromArgb(242, 242, 247));
                card.CardClicked += (s, e) =>
                {
                    cmbSegmentFilter.SelectedItem = segKey;
                };

                kpiTableSegments.Controls.Add(card, i, 0);
            }

            kpiTableSegments.ResumeLayout(true);
        }

        private List<RetentionCustomerItem> GetFilteredCustomers()
        {
            if (currentData == null) return new List<RetentionCustomerItem>();
            var list = currentData.CurrentSegmentCustomers.AsEnumerable();

            if (activeSegmentFilter != "All")
            {
                list = list.Where(c => c.SegmentName == activeSegmentFilter);
            }

            if (!string.IsNullOrWhiteSpace(activeSearchQuery))
            {
                string q = activeSearchQuery.ToLower();
                list = list.Where(c =>
                    c.CustomerName.ToLower().Contains(q) ||
                    c.Email.ToLower().Contains(q) ||
                    c.Phone.ToLower().Contains(q));
            }

            return list.ToList();
        }

        private void RenderCustomerGrid()
        {
            var filtered = GetFilteredCustomers();
            pagination.SetPagination(currentPage, PageSize, filtered.Count, "customers");

            var pageItems = filtered
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            gridCustomers.Rows.Clear();
            foreach (var item in pageItems)
            {
                string recency = item.LastCompletedOrderDate.HasValue
                    ? $"{item.DaysSinceLastOrder}d ago"
                    : "Never";

                string lastOrder = item.LastCompletedOrderDate.HasValue
                    ? item.LastCompletedOrderDate.Value.ToString("MMM d, yyyy")
                    : "-";

                string sendStatus = item.EligibleToSend
                    ? "\u2714 Eligible"
                    : (item.LastEmailSentDate.HasValue ? "14d Cooldown" : "No Email");

                gridCustomers.Rows.Add(
                    item.CustomerName,
                    item.Email,
                    $"{item.CompletedOrdersCount} order(s)",
                    lastOrder,
                    recency,
                    item.SegmentName,
                    sendStatus,
                    "\u2709 Send"
                );
            }
        }

        private void UpdateSendSegmentButtonText()
        {
            if (activeSegmentFilter == "All")
            {
                btnSendSegmentCampaign.Text = "Send Campaign to Filtered Segment";
                btnSendSegmentCampaign.Enabled = false;
            }
            else
            {
                var eligibleCount = GetFilteredCustomers().Count(c => c.EligibleToSend);
                btnSendSegmentCampaign.Text = $"\u2709 Send to {activeSegmentFilter} ({eligibleCount} eligible)";
                btnSendSegmentCampaign.Enabled = eligibleCount > 0;
            }
        }

        // =========================================================
        // TAB 2: EMAIL CAMPAIGNS & FIXED TEMPLATES
        // =========================================================

        private void BuildCampaignsTab()
        {
            panelCampaigns = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Padding = new Padding(0, 0, 16, 16)
            };

            var headerNotice = new Label
            {
                Dock = DockStyle.Top,
                Height = 36,
                Text = "Fixed Ready-Made Retention Emails \u00B7 Dynamic variable {{customer_name}} is replaced automatically upon sending.",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Italic),
                ForeColor = UITheme.TextMuted,
                UseMnemonic = false
            };

            flowCampaignCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            panelCampaigns.Resize += (s, e) =>
            {
                if (flowCampaignCards.Controls.Count > 0)
                {
                    int w = Math.Max(850, flowCampaignCards.ClientSize.Width - 32);
                    foreach (Control c in flowCampaignCards.Controls)
                    {
                        if (c is Panel p) p.Width = w;
                    }
                }
            };

            panelCampaigns.Controls.Add(flowCampaignCards);
            panelCampaigns.Controls.Add(headerNotice);
        }

        private void RenderCampaignCards()
        {
            if (currentData == null) return;
            flowCampaignCards.SuspendLayout();
            flowCampaignCards.Controls.Clear();

            bool isAdmin = SessionService.CurrentUser?.Role == "Business Admin" || SessionService.CurrentUser?.Role == "Admin";
            int cardWidth = Math.Max(850, flowCampaignCards.ClientSize.Width - 32);

            // 1. Render Approved Individual Retention Campaigns (Requirements 4, 5, 6)
            if (currentData.ApprovedCampaigns != null && currentData.ApprovedCampaigns.Any())
            {
                var banner = new Panel
                {
                    Width = cardWidth,
                    Height = 38,
                    BackColor = Color.FromArgb(243, 240, 255),
                    Margin = new Padding(0, 0, 0, 12),
                    Padding = new Padding(14, 8, 14, 8)
                };
                var lblBanner = new Label
                {
                    Text = $"\u2728 Approved Retention Campaigns ({currentData.ApprovedCampaigns.Count} individual customer offer(s) ready to send)",
                    Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                    ForeColor = UITheme.PrimaryMauve,
                    Dock = DockStyle.Fill
                };
                banner.Controls.Add(lblBanner);
                flowCampaignCards.Controls.Add(banner);

                foreach (var req in currentData.ApprovedCampaigns)
                {
                    flowCampaignCards.Controls.Add(BuildApprovedCampaignCard(req, cardWidth));
                }

                var separator = new Panel
                {
                    Width = cardWidth,
                    Height = 36,
                    BackColor = Color.Transparent,
                    Margin = new Padding(0, 8, 0, 10),
                    Padding = new Padding(0, 8, 0, 8)
                };
                var lblSep = new Label
                {
                    Text = "Fixed Segment Automated Templates (Bulk Campaigns)",
                    Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                    ForeColor = UITheme.TextMuted,
                    Dock = DockStyle.Fill
                };
                separator.Controls.Add(lblSep);
                flowCampaignCards.Controls.Add(separator);
            }

            foreach (var template in currentData.Templates)
            {
                var card = new Panel
                {
                    Width = cardWidth,
                    Height = 235,
                    BackColor = Color.White,
                    Margin = new Padding(0, 0, 0, 16),
                    Padding = new Padding(20)
                };
                card.Paint += (s, e) =>
                {
                    using var pen = new Pen(UITheme.BorderColor, 1f);
                    e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
                };

                // Right Column (Offers & Actions) - docked Right first so Left Fill fits smoothly
                var rightStack = new Panel
                {
                    Dock = DockStyle.Right,
                    Width = 260,
                    BackColor = Color.Transparent
                };

                var lblOffer = new Label
                {
                    Text = $"Offer: {template.OfferDescription}",
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                    ForeColor = UITheme.StatusGreenFg,
                    Location = new Point(10, 8),
                    Width = 240,
                    Height = 36,
                    UseMnemonic = false
                };

                var btnSendCampaign = new Button
                {
                    Text = $"\u2709 Approve & Send ({template.SegmentName})",
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = UITheme.PrimaryMauve,
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(10, 50),
                    Width = 240,
                    Height = 36,
                    Cursor = Cursors.Hand,
                    UseMnemonic = false
                };
                btnSendCampaign.FlatAppearance.BorderSize = 0;
                btnSendCampaign.Click += async (s, e) =>
                {
                    var eligibleCount = currentData.CurrentSegmentCustomers.Count(c => c.SegmentName == template.SegmentName && c.EligibleToSend);
                    var confirm = MessageBox.Show(
                        $"Approve and dispatch campaign for '{template.SegmentName}' to {eligibleCount} eligible customer(s)?\n\nAll recipients will be recorded with 14-day anti-fatigue cooldown.",
                        "Confirm Campaign Send",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm == DialogResult.Yes)
                    {
                        var (sent, skipped) = await CrmDataService.SendCampaignForSegmentAsync(template.SegmentName, SessionService.CurrentUser?.CompanyId);
                        await RefreshDataAsync();
                        MessageBox.Show($"Campaign sent to {sent} customer(s). Skipped: {skipped}.", "Campaign Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                var btnEmailCampaign = new Button
                {
                    Text = "\u2709 Email Customer",
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                    ForeColor = UITheme.PrimaryMauve,
                    BackColor = Color.FromArgb(248, 238, 242),
                    FlatStyle = FlatStyle.Flat,
                    Location = new Point(10, 92),
                    Width = 240,
                    Height = 32,
                    Cursor = Cursors.Hand
                };
                btnEmailCampaign.FlatAppearance.BorderColor = UITheme.PrimaryMauve;
                btnEmailCampaign.Click += async (s, e) => await ShowManualEmailDialogAsync(template.SegmentName);

                rightStack.Controls.Add(btnEmailCampaign);
                rightStack.Controls.Add(btnSendCampaign);
                rightStack.Controls.Add(lblOffer);

                if (isAdmin)
                {
                    var btnEditTemplate = new Button
                    {
                        Text = "\u270E Edit Template (Admin)",
                        Font = new Font(UITheme.FontSans, 8F, FontStyle.Regular),
                        ForeColor = UITheme.TextDark,
                        BackColor = Color.FromArgb(245, 240, 235),
                        FlatStyle = FlatStyle.Flat,
                        Location = new Point(10, 130),
                        Width = 240,
                        Height = 30,
                        Cursor = Cursors.Hand
                    };
                    btnEditTemplate.FlatAppearance.BorderColor = UITheme.BorderColor;
                    btnEditTemplate.Click += (s, e) => ShowEditTemplateDialog(template);
                    rightStack.Controls.Add(btnEditTemplate);
                }

                // Left Column (Details)
                var leftStack = new Panel
                {
                    Dock = DockStyle.Fill,
                    BackColor = Color.Transparent,
                    Padding = new Padding(0, 0, 16, 0)
                };

                var lblSegBadge = new Label
                {
                    Text = $"SEGMENT: {template.SegmentName.ToUpper()}",
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                    ForeColor = UITheme.PrimaryMauve,
                    Dock = DockStyle.Top,
                    Height = 22
                };

                var lblSubject = new Label
                {
                    Text = $"Subject: {template.Subject}",
                    Font = new Font(UITheme.FontSans, 10.5F, FontStyle.Bold),
                    ForeColor = UITheme.TextDark,
                    Dock = DockStyle.Top,
                    Height = 24,
                    UseMnemonic = false
                };

                var lblPreview = new Label
                {
                    Text = $"Preview text: {template.PreviewText}",
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Italic),
                    ForeColor = UITheme.TextMuted,
                    Dock = DockStyle.Top,
                    Height = 20,
                    UseMnemonic = false
                };

                var txtBodySnippet = new TextBox
                {
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Font = new Font("Consolas", 8.5F),
                    BackColor = Color.FromArgb(250, 248, 246),
                    ForeColor = UITheme.TextDark,
                    BorderStyle = BorderStyle.FixedSingle,
                    Dock = DockStyle.Fill,
                    Text = template.BodyText
                };

                leftStack.Controls.Add(txtBodySnippet);
                leftStack.Controls.Add(lblPreview);
                leftStack.Controls.Add(lblSubject);
                leftStack.Controls.Add(lblSegBadge);

                card.Controls.Add(leftStack);
                card.Controls.Add(rightStack);
                flowCampaignCards.Controls.Add(card);
            }

            flowCampaignCards.ResumeLayout(true);
        }

        private Panel BuildApprovedCampaignCard(CC.Domain.Entities.RetentionRequest req, int cardWidth)
        {
            var card = new Panel
            {
                Width = cardWidth,
                Height = 235,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(20)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(140, 70, 90), 1.5f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            };

            // Right Column (Offers & Actions)
            var rightStack = new Panel
            {
                Dock = DockStyle.Right,
                Width = 260,
                BackColor = Color.Transparent
            };

            var lblOffer = new Label
            {
                Text = $"Offer: {(req.DiscountPercent > 0 ? $"{req.DiscountPercent:0.#}% Off Next Order" : "Special VIP Offer")}",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.StatusGreenFg,
                Location = new Point(10, 8),
                Width = 240,
                Height = 30,
                UseMnemonic = false
            };

            var lblSource = new Label
            {
                Text = $"From Request #{req.RequestId:D4}\nApproved: {req.ReviewedDate?.ToLocalTime().ToString("MMM dd, yyyy") ?? "Recently"}",
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Location = new Point(10, 42),
                Width = 240,
                Height = 36,
                UseMnemonic = false
            };

            var btnReviewSend = new Button
            {
                Text = "\u2709 Review & Send Email",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(10, 86),
                Width = 240,
                Height = 36,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnReviewSend.FlatAppearance.BorderSize = 0;
            btnReviewSend.Click += async (s, e) =>
            {
                await ShowApprovedRequestEmailDialogAsync(req);
            };

            var btnViewDetails = new Button
            {
                Text = "\uD83D\uDCCB View Request Details",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                BackColor = Color.FromArgb(245, 240, 235),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(10, 128),
                Width = 240,
                Height = 32,
                Cursor = Cursors.Hand
            };
            btnViewDetails.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnViewDetails.Click += async (s, e) =>
            {
                using var modal = new RetentionRequestDetailsModal(req);
                if (modal.ShowDialogWithBackdrop(this) == DialogResult.OK)
                {
                    await RefreshDataAsync();
                }
            };

            rightStack.Controls.Add(btnViewDetails);
            rightStack.Controls.Add(btnReviewSend);
            rightStack.Controls.Add(lblSource);
            rightStack.Controls.Add(lblOffer);

            // Left Column (Details)
            var leftStack = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 16, 0)
            };

            var lblBadge = new Label
            {
                Text = $"\u2714 APPROVED RETENTION CAMPAIGN  \u00B7  {req.CustomerName.ToUpperInvariant()} ({req.CustomerEmail})",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Dock = DockStyle.Top,
                Height = 22
            };

            var lblSubject = new Label
            {
                Text = $"Subject: {req.GeneratedCampaignSubject ?? "Special Retention Offer"}",
                Font = new Font(UITheme.FontSans, 10F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Top,
                Height = 24,
                UseMnemonic = false
            };

            var lblPreview = new Label
            {
                Text = $"Reason: {req.FullReason}  \u00B7  Action: {req.ActionType}  \u00B7  Discount: {req.DiscountPercent:0.#}%",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Italic),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 20,
                UseMnemonic = false
            };

            var txtBodySnippet = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9F),
                BackColor = Color.FromArgb(250, 248, 246),
                ForeColor = UITheme.TextDark,
                BorderStyle = BorderStyle.FixedSingle,
                Dock = DockStyle.Fill,
                Text = req.GeneratedCampaignBody ?? string.Empty
            };

            leftStack.Controls.Add(txtBodySnippet);
            leftStack.Controls.Add(lblPreview);
            leftStack.Controls.Add(lblSubject);
            leftStack.Controls.Add(lblBadge);

            card.Controls.Add(leftStack);
            card.Controls.Add(rightStack);
            return card;
        }

        private async Task ShowApprovedRequestEmailDialogAsync(CC.Domain.Entities.RetentionRequest req)
        {
            using var dlg = new Form
            {
                Text = $"Send Approved Retention Email - {req.CustomerName}",
                Size = new Size(680, 650),
                StartPosition = FormStartPosition.Manual,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = UITheme.CreamBackground
            };
            dlg.Load += (s, e) => dlg.CenterOnParentOrScreen();

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24) };

            var lblHeader = new Label
            {
                Text = $"Review & Send Retention Email \u00B7 {req.CustomerName}",
                Font = new Font(UITheme.FontSerif, 13F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Top,
                Height = 28,
                UseMnemonic = false
            };

            var lblDesc = new Label
            {
                Text = $"Generated automatically from approved Retention Request #{req.RequestId:D4}. You can edit the subject and body before dispatching.",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 32,
                UseMnemonic = false
            };

            var formPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var lblRecip = new Label { Text = $"Recipient: {req.CustomerName} ({req.CustomerEmail})", Location = new Point(0, 6), AutoSize = true, Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold), ForeColor = UITheme.TextDark };
            var lblSub = new Label { Text = "Email Subject Line:", Location = new Point(0, 36), AutoSize = true, Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), ForeColor = UITheme.TextDark };
            var txtSub = new TextBox { Text = req.GeneratedCampaignSubject ?? "Special Offer from Sweet Story", Location = new Point(0, 56), Width = 610, Font = new Font(UITheme.FontSans, 9.5F) };

            var lblBody = new Label { Text = "Email Body (Personalized):", Location = new Point(0, 92), AutoSize = true, Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), ForeColor = UITheme.TextDark };
            var txtBody = new TextBox
            {
                Text = req.GeneratedCampaignBody ?? string.Empty,
                Location = new Point(0, 114),
                Width = 610,
                Height = 320,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9F)
            };

            formPanel.Controls.Add(lblRecip);
            formPanel.Controls.Add(lblSub);
            formPanel.Controls.Add(txtSub);
            formPanel.Controls.Add(lblBody);
            formPanel.Controls.Add(txtBody);

            var bottomActionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 50,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0)
            };

            var btnSend = new Button
            {
                Text = "\u2709 Send Email Now",
                Height = 38,
                Width = 160,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                Cursor = Cursors.Hand
            };
            btnSend.FlatAppearance.BorderSize = 0;
            btnSend.Click += async (s, e) =>
            {
                btnSend.Enabled = false;
                btnSend.Text = "Sending...";
                try
                {
                    await CrmDataService.SendManualRetentionEmailAsync(
                        customerId: req.CustomerId,
                        segmentName: req.TargetSegment,
                        forceIgnoreCooldown: true,
                        overrideSubject: txtSub.Text.Trim(),
                        overrideBody: txtBody.Text.Trim()
                    );
                    MessageBox.Show($"Retention email successfully dispatched to {req.CustomerName} ({req.CustomerEmail})!", "Email Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    dlg.Close();
                    await RefreshDataAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to send email: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    btnSend.Enabled = true;
                    btnSend.Text = "\u2709 Send Email Now";
                }
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Height = 38,
                Width = 85,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F),
                ForeColor = UITheme.TextDark,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnCancel.Click += (s, e) => dlg.Close();

            bottomActionPanel.Controls.Add(btnSend);
            bottomActionPanel.Controls.Add(btnCancel);

            pnl.Controls.Add(formPanel);
            pnl.Controls.Add(bottomActionPanel);
            pnl.Controls.Add(lblDesc);
            pnl.Controls.Add(lblHeader);
            dlg.Controls.Add(pnl);

            dlg.ShowDialogWithBackdrop(this);
        }

        private void ShowEditTemplateDialog(RetentionEmailTemplate template)
        {
            var dlg = new Form
            {
                Text = $"Edit Template - {template.SegmentName}",
                Size = new Size(620, 520),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = UITheme.CreamBackground
            };

            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(24) };

            var lblSub = new Label { Text = "Subject Line:", Location = new Point(0, 10), AutoSize = true, Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold) };
            var txtSub = new TextBox { Text = template.Subject, Location = new Point(0, 32), Width = 550, Font = new Font(UITheme.FontSans, 9.5F) };

            var lblBody = new Label { Text = "Email Body (Must contain {{customer_name}}):", Location = new Point(0, 68), AutoSize = true, Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold) };
            var txtBody = new TextBox { Text = template.BodyText, Location = new Point(0, 90), Width = 550, Height = 220, Multiline = true, ScrollBars = ScrollBars.Vertical, Font = new Font(UITheme.FontSans, 9F) };

            var lblOffer = new Label { Text = "Offer Summary:", Location = new Point(0, 324), AutoSize = true, Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold) };
            var txtOffer = new TextBox { Text = template.OfferDescription, Location = new Point(0, 346), Width = 350, Font = new Font(UITheme.FontSans, 9.5F) };

            var btnSave = new Button
            {
                Text = "Save Template Changes",
                Location = new Point(370, 400),
                Width = 180,
                Height = 36,
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += async (s, e) =>
            {
                if (!txtBody.Text.Contains("{{customer_name}}"))
                {
                    MessageBox.Show("Email body must contain the {{customer_name}} placeholder.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                await CrmDataService.UpdateRetentionTemplateAsync(
                    template.TemplateId,
                    txtSub.Text.Trim(),
                    txtBody.Text.Trim(),
                    txtOffer.Text.Trim(),
                    template.DiscountPercent,
                    SessionService.CurrentUser?.CompanyId);

                dlg.Close();
                await RefreshDataAsync();
                MessageBox.Show("Template updated successfully.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            pnl.Controls.Add(lblSub);
            pnl.Controls.Add(txtSub);
            pnl.Controls.Add(lblBody);
            pnl.Controls.Add(txtBody);
            pnl.Controls.Add(lblOffer);
            pnl.Controls.Add(txtOffer);
            pnl.Controls.Add(btnSave);
            dlg.Controls.Add(pnl);

            dlg.ShowDialog(this);
        }

        // =========================================================
        // TAB 3: PERFORMANCE & TRACKING REPORTS
        // =========================================================

        private void BuildReportsTab()
        {
            panelReports = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            // 4 Retention Metric KPI Cards
            kpiTableMetrics = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 4,
                RowCount = 1,
                Height = 142,
                Margin = new Padding(0, 0, 0, 18),
                BackColor = Color.Transparent
            };
            for (int i = 0; i < 4; i++)
                kpiTableMetrics.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            // Breakdown Table Card
            var pnlGridCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(0, 16, 0, 0)
            };
            pnlGridCard.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, pnlGridCard.Width - 1, pnlGridCard.Height - 1), 10);
            };

            var lblTableTitle = new Label
            {
                Dock = DockStyle.Top,
                Text = "Campaign Deliverability & Conversion Breakdown by Segment",
                Font = new Font(UITheme.FontSerif, 12F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Height = 32,
                UseMnemonic = false
            };

            gridSegmentPerformance = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = UITheme.BorderColor,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };
            UITheme.ApplyTableStyle(gridSegmentPerformance);
            gridSegmentPerformance.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridSegmentPerformance.RowTemplate.Height = 44;
            gridSegmentPerformance.ColumnHeadersHeight = 40;

            gridSegmentPerformance.Columns.Add(new DataGridViewTextBoxColumn { Name = "Segment", HeaderText = "RETENTION SEGMENT", FillWeight = 20 });
            gridSegmentPerformance.Columns.Add(new DataGridViewTextBoxColumn { Name = "Delivered", HeaderText = "DELIVERED", FillWeight = 14 });
            gridSegmentPerformance.Columns.Add(new DataGridViewTextBoxColumn { Name = "Opened", HeaderText = "OPENED (EST.)", FillWeight = 16 });
            gridSegmentPerformance.Columns.Add(new DataGridViewTextBoxColumn { Name = "Clicked", HeaderText = "CLICKED (EST.)", FillWeight = 16 });
            gridSegmentPerformance.Columns.Add(new DataGridViewTextBoxColumn { Name = "Converted", HeaderText = "REPEAT ORDERS", FillWeight = 16 });
            gridSegmentPerformance.Columns.Add(new DataGridViewTextBoxColumn { Name = "Revenue", HeaderText = "RETAINED REVENUE", FillWeight = 18 });

            pnlGridCard.Controls.Add(gridSegmentPerformance);
            pnlGridCard.Controls.Add(lblTableTitle);

            panelReports.Controls.Add(pnlGridCard);
            panelReports.Controls.Add(kpiTableMetrics);
        }

        private void RenderReportsTab()
        {
            if (currentData == null) return;
            kpiTableMetrics.SuspendLayout();
            kpiTableMetrics.Controls.Clear();

            var m = currentData.Metrics;

            var cardOpen = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Open Rate",
                Value = $"{m.OpenRate:F1}%",
                Subtitle = "Target: 30%–45% \u00B7 Email engagement",
                Margin = new Padding(0, 0, 6, 0)
            };
            cardOpen.SetBadge("ENGAGE", UITheme.StatusBlueFg, UITheme.StatusBlueBg);

            var cardClick = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Click-Through Rate",
                Value = $"{m.ClickRate:F1}%",
                Subtitle = "Target: 4%–8% \u00B7 CTA action clicks",
                Margin = new Padding(6, 0, 6, 0)
            };
            cardClick.SetBadge("ACTION", UITheme.StatusGreenFg, UITheme.StatusGreenBg);

            var cardRepeat = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Repeat-Order Rate",
                Value = $"{m.RepeatOrderRate:F1}%",
                Subtitle = "Target: 10%–18% \u00B7 Closed loop orders",
                Margin = new Padding(6, 0, 6, 0)
            };
            cardRepeat.SetBadge("REVENUE", UITheme.PrimaryMauve, Color.FromArgb(245, 230, 235));

            var cardWinBack = new DashboardKpiCard
            {
                Dock = DockStyle.Fill,
                Title = "Win-Back Rate",
                Value = $"{m.WinBackRate:F1}%",
                Subtitle = "Target: 5%–10% \u00B7 Saved churning clients",
                Margin = new Padding(6, 0, 0, 0)
            };
            cardWinBack.SetBadge("WIN-BACK", UITheme.StatusYellowFg, UITheme.StatusYellowBg);

            kpiTableMetrics.Controls.Add(cardOpen, 0, 0);
            kpiTableMetrics.Controls.Add(cardClick, 1, 0);
            kpiTableMetrics.Controls.Add(cardRepeat, 2, 0);
            kpiTableMetrics.Controls.Add(cardWinBack, 3, 0);

            kpiTableMetrics.ResumeLayout(true);

            // Populate table breakdown
            gridSegmentPerformance.Rows.Clear();
            var logs = currentData.RecentEmailLogs;
            var segs = new[] { "New", "Returning", "Loyal", "At Risk", "Inactive" };

            foreach (var seg in segs)
            {
                var segLogs = logs.Where(l => l.SegmentName == seg).ToList();
                int del = segLogs.Count;
                int opn = segLogs.Count(l => l.OpenedDate.HasValue || l.Status == "Opened" || l.Status == "Clicked");
                int clk = segLogs.Count(l => l.ClickedDate.HasValue || l.Status == "Clicked");
                int rep = segLogs.Count(l => l.ConvertedOrderId.HasValue);
                decimal rev = segLogs.Sum(l => l.ConvertedOrderAmount ?? 0m);

                gridSegmentPerformance.Rows.Add(
                    seg,
                    del.ToString(),
                    $"{opn} ({(del > 0 ? (double)opn / del * 100 : 0):F0}%)",
                    $"{clk} ({(opn > 0 ? (double)clk / opn * 100 : 0):F0}%)",
                    $"{rep} order(s)",
                    rev > 0 ? $"P{rev:N2}" : "P0.00"
                );
            }
        }

        // =========================================================
        // TAB 4: EMAIL & AUDIT LOGS
        // =========================================================

        private void BuildLogsTab()
        {
            panelLogs = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            var toggleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.Transparent
            };

            btnShowEmailLogs = new Button
            {
                Text = "\u2709 Retention Email Delivery Logs",
                Location = new Point(0, 4),
                Height = 36,
                Width = 240,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnShowEmailLogs.FlatAppearance.BorderSize = 0;
            btnShowEmailLogs.Click += (s, e) => SwitchLogsView(true);

            btnShowAuditLogs = new Button
            {
                Text = "\uD83D\uDD12 Security & Audit Trail",
                Location = new Point(250, 4),
                Height = 36,
                Width = 220,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnShowAuditLogs.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnShowAuditLogs.Click += (s, e) => SwitchLogsView(false);

            toggleBar.Controls.Add(btnShowEmailLogs);
            toggleBar.Controls.Add(btnShowAuditLogs);

            var logCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            logCard.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, logCard.Width - 1, logCard.Height - 1), 8);
            };

            gridEmailLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = UITheme.BorderColor,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };
            UITheme.ApplyTableStyle(gridEmailLogs);
            gridEmailLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridEmailLogs.RowTemplate.Height = 44;
            gridEmailLogs.ColumnHeadersHeight = 40;

            gridEmailLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "SentDate", HeaderText = "DATE / TIME", FillWeight = 15, MinimumWidth = 100 });
            gridEmailLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Customer", HeaderText = "RECIPIENT", FillWeight = 18, MinimumWidth = 110 });
            gridEmailLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Email", HeaderText = "EMAIL", FillWeight = 20, MinimumWidth = 120 });
            gridEmailLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Segment", HeaderText = "SEGMENT", FillWeight = 12, MinimumWidth = 85 });
            gridEmailLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Subject", HeaderText = "SUBJECT", FillWeight = 23, MinimumWidth = 140 });
            gridEmailLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Status", HeaderText = "STATUS", FillWeight = 12, MinimumWidth = 80 });

            gridAuditLogs = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = UITheme.BorderColor,
                AllowUserToAddRows = false,
                ReadOnly = true,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                Visible = false
            };
            UITheme.ApplyTableStyle(gridAuditLogs);
            gridAuditLogs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridAuditLogs.RowTemplate.Height = 44;
            gridAuditLogs.ColumnHeadersHeight = 40;

            gridAuditLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Timestamp", HeaderText = "TIMESTAMP (UTC)", FillWeight = 18 });
            gridAuditLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "ActionType", HeaderText = "ACTION", FillWeight = 22 });
            gridAuditLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "DETAILS", FillWeight = 60 });

            logCard.Controls.Add(gridEmailLogs);
            logCard.Controls.Add(gridAuditLogs);

            panelLogs.Controls.Add(logCard);
            panelLogs.Controls.Add(toggleBar);
        }

        private void SwitchLogsView(bool showEmail)
        {
            viewingEmailLogs = showEmail;
            btnShowEmailLogs.BackColor = showEmail ? UITheme.PrimaryMauve : Color.White;
            btnShowEmailLogs.ForeColor = showEmail ? Color.White : UITheme.TextMuted;
            btnShowEmailLogs.Font = new Font(UITheme.FontSans, 9F, showEmail ? FontStyle.Bold : FontStyle.Regular);

            btnShowAuditLogs.BackColor = !showEmail ? UITheme.PrimaryMauve : Color.White;
            btnShowAuditLogs.ForeColor = !showEmail ? Color.White : UITheme.TextMuted;
            btnShowAuditLogs.Font = new Font(UITheme.FontSans, 9F, !showEmail ? FontStyle.Bold : FontStyle.Regular);

            gridEmailLogs.Visible = showEmail;
            gridAuditLogs.Visible = !showEmail;
        }

        private void RenderLogsTab()
        {
            if (currentData == null) return;

            // Email logs
            gridEmailLogs.Rows.Clear();
            foreach (var l in currentData.RecentEmailLogs)
            {
                gridEmailLogs.Rows.Add(
                    l.SentDate.ToString("MMM d, yyyy HH:mm"),
                    l.CustomerName,
                    l.CustomerEmail,
                    l.SegmentName,
                    l.Subject,
                    l.Status
                );
            }

            // Audit logs
            gridAuditLogs.Rows.Clear();
            foreach (var a in currentData.AuditLogs)
            {
                gridAuditLogs.Rows.Add(
                    a.Timestamp.ToString("MMM d, yyyy HH:mm:ss"),
                    a.ActionType,
                    a.ActionDescription
                );
            }
        }

        // =========================================================
        // TAB 5: SETTINGS (ADMIN ONLY)
        // =========================================================

        private void BuildSettingsTab()
        {
            panelSettings = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Padding = new Padding(0, 0, 24, 24)
            };

            var card = new Panel
            {
                Width = 720,
                Height = 440,
                BackColor = Color.White,
                Padding = new Padding(24)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            };

            var lblSetTitle = new Label
            {
                Text = "Lifecycle Segmentation Thresholds & Anti-Fatigue Rules",
                Font = new Font(UITheme.FontSerif, 13F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Location = new Point(24, 20),
                AutoSize = true
            };

            lblSettingsNotice = new Label
            {
                Text = "Note: Only Business Admins can modify segmentation thresholds. Changes take effect on the next recalculation.",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Italic),
                ForeColor = UITheme.TextMuted,
                Location = new Point(24, 48),
                AutoSize = true
            };

            // Active Threshold (default 90d)
            var lblActive = new Label { Text = "Active Days Threshold (New / Returning / Loyal limit):", Location = new Point(24, 90), AutoSize = true, Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold) };
            numActiveDays = new NumericUpDown { Minimum = 30, Maximum = 365, Value = 90, Location = new Point(24, 114), Width = 120, Font = new Font(UITheme.FontSans, 9.5F) };

            // At-Risk Threshold (default 180d)
            var lblAtRisk = new Label { Text = "At-Risk Days Threshold (Churn threshold - inactive if past this):", Location = new Point(24, 154), AutoSize = true, Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold) };
            numAtRiskDays = new NumericUpDown { Minimum = 60, Maximum = 730, Value = 180, Location = new Point(24, 178), Width = 120, Font = new Font(UITheme.FontSans, 9.5F) };

            // Loyal Min Orders (default 3)
            var lblLoyal = new Label { Text = "Loyal Customer Minimum Completed Orders:", Location = new Point(24, 218), AutoSize = true, Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold) };
            numLoyalOrders = new NumericUpDown { Minimum = 2, Maximum = 20, Value = 3, Location = new Point(24, 242), Width = 120, Font = new Font(UITheme.FontSans, 9.5F) };

            // Anti-Fatigue Cooldown (default 14d)
            var lblCooldown = new Label { Text = "Anti-Fatigue Cooldown Days (minimum gap between retention emails):", Location = new Point(24, 282), AutoSize = true, Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold) };
            numCooldownDays = new NumericUpDown { Minimum = 7, Maximum = 60, Value = 14, Location = new Point(24, 306), Width = 120, Font = new Font(UITheme.FontSans, 9.5F) };

            btnSaveSettings = new Button
            {
                Text = "Save Threshold Settings",
                Location = new Point(24, 360),
                Width = 200,
                Height = 38,
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveSettings.FlatAppearance.BorderSize = 0;
            btnSaveSettings.Click += async (s, e) =>
            {
                try
                {
                    await CrmDataService.UpdateRetentionSettingsAsync(
                        (int)numActiveDays.Value,
                        (int)numAtRiskDays.Value,
                        (int)numCooldownDays.Value,
                        SessionService.CurrentUser?.CompanyId);

                    await RefreshDataAsync();
                    MessageBox.Show("Retention threshold settings saved and logged successfully.", "Settings Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var btnEmailConfig = new Button
            {
                Text = "\u2699 Email & SMTP Settings",
                Location = new Point(236, 360),
                Width = 200,
                Height = 38,
                BackColor = Color.FromArgb(245, 240, 243),
                ForeColor = UITheme.PrimaryMauve,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnEmailConfig.FlatAppearance.BorderColor = UITheme.PrimaryMauve;
            btnEmailConfig.Click += (s, e) => EmailSettingsForm.ShowSettingsDialog(this);

            card.Controls.Add(lblSetTitle);
            card.Controls.Add(lblSettingsNotice);
            card.Controls.Add(lblActive);
            card.Controls.Add(numActiveDays);
            card.Controls.Add(lblAtRisk);
            card.Controls.Add(numAtRiskDays);
            card.Controls.Add(lblLoyal);
            card.Controls.Add(numLoyalOrders);
            card.Controls.Add(lblCooldown);
            card.Controls.Add(numCooldownDays);
            card.Controls.Add(btnSaveSettings);
            card.Controls.Add(btnEmailConfig);

            panelSettings.Controls.Add(card);
        }

        // =========================================================
        // MANUAL RETENTION EMAIL DISPATCH MODAL
        // =========================================================

        private class RetentionCustomerSuggestion
        {
            public CrmDataService.RetentionCustomerSearchResult Customer { get; set; } = null!;
            public override string ToString()
            {
                string unsub = Customer.IsUnsubscribed ? " [OPTED OUT]" : "";
                string cooldown = Customer.CooldownDaysRemaining > 0 ? $" [Cooldown: {Customer.CooldownDaysRemaining}d]" : "";
                return $"{Customer.FullName} \u00B7 {Customer.Email} \u00B7 [{Customer.SegmentName}]{unsub}{cooldown}";
            }
        }

        public async Task ShowManualEmailDialogAsync(string? preselectedSegment = null, RetentionCustomerItem? preselectedCustomer = null)
        {
            if (currentData == null) return;

            using var dlg = new Form
            {
                Text = "Send Retention Email",
                Size = new Size(680, 650),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = UITheme.CreamBackground
            };

            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24)
            };

            var lblHeader = new Label
            {
                Text = "Send Retention Email",
                Font = new Font(UITheme.FontSerif, 13F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Top,
                Height = 28,
                UseMnemonic = false
            };

            var lblDesc = new Label
            {
                Text = "Select a registered customer from your database to send them a personalized retention email.",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 24,
                UseMnemonic = false
            };

            var searchPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 175,
                BackColor = Color.Transparent
            };

            var lblSearch = new Label
            {
                Text = "Search Customer (Email, Name, or Phone):",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Location = new Point(0, 6),
                AutoSize = true
            };

            var txtSearch = new TextBox
            {
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(0, 26),
                Width = 360,
                PlaceholderText = "Type customer email, name, or phone..."
            };

            var lblName = new Label
            {
                Text = "Recipient Name:",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Location = new Point(380, 6),
                AutoSize = true
            };

            var txtName = new TextBox
            {
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(380, 26),
                Width = 240,
                ReadOnly = true,
                PlaceholderText = "Customer name"
            };

            var lblTemplate = new Label
            {
                Text = "Retention Segment Template:",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Location = new Point(0, 64),
                AutoSize = true
            };

            var cmbTemplate = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9F),
                Location = new Point(0, 84),
                Width = 280
            };
            cmbTemplate.Items.AddRange(new object[] { "New", "Returning", "Loyal", "At Risk", "Inactive" });
            cmbTemplate.SelectedItem = preselectedSegment ?? "New";

            var lblStatusWarning = new Label
            {
                Text = "",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(192, 57, 43),
                Location = new Point(0, 118),
                AutoSize = true,
                Visible = false
            };

            var chkOverrideCooldown = new CheckBox
            {
                Text = "Override 14-day anti-fatigue cooldown (Send Anyway)",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Location = new Point(0, 142),
                AutoSize = true,
                Visible = false
            };

            var lstSuggestions = new ListBox
            {
                Font = new Font(UITheme.FontSans, 9F),
                Location = new Point(0, 54),
                Width = 420,
                Height = 120,
                Visible = false
            };

            searchPanel.Controls.Add(lstSuggestions);
            searchPanel.Controls.Add(lblSearch);
            searchPanel.Controls.Add(txtSearch);
            var lblEmailSettingsHint = new Label
            {
                Text = "\u2699 Email Settings",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Underline),
                ForeColor = UITheme.PrimaryMauve,
                Cursor = Cursors.Hand,
                Location = new Point(490, 86),
                AutoSize = true
            };
            lblEmailSettingsHint.Click += (s, e) => EmailSettingsForm.ShowSettingsDialog(dlg);

            searchPanel.Controls.Add(lblEmailSettingsHint);
            searchPanel.Controls.Add(lblName);
            searchPanel.Controls.Add(txtName);
            searchPanel.Controls.Add(lblTemplate);
            searchPanel.Controls.Add(cmbTemplate);
            searchPanel.Controls.Add(lblStatusWarning);
            searchPanel.Controls.Add(chkOverrideCooldown);

            var lblSub = new Label
            {
                Text = "Subject Preview:",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Top,
                Height = 22
            };

            var txtSub = new TextBox
            {
                Font = new Font(UITheme.FontSans, 9.5F),
                Dock = DockStyle.Top,
                Height = 28
            };

            var lblBody = new Label
            {
                Text = "Email Body Preview (Personalized):",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Top,
                Height = 22
            };

            var txtBody = new TextBox
            {
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9F),
                Dock = DockStyle.Fill
            };

            var bottomActionPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 44,
                BackColor = Color.Transparent
            };

            var btnSend = new Button
            {
                Text = "\u2709 Send Retention Email",
                Dock = DockStyle.Fill,
                Height = 44,
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseMnemonic = false
            };
            btnSend.FlatAppearance.BorderSize = 0;

            var btnOpenEmailSettings = new Button
            {
                Text = "\u2699 Email Settings",
                Dock = DockStyle.Right,
                Width = 145,
                Height = 44,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 240, 243),
                ForeColor = UITheme.PrimaryMauve,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnOpenEmailSettings.FlatAppearance.BorderColor = UITheme.PrimaryMauve;
            btnOpenEmailSettings.Click += (s, e) => EmailSettingsForm.ShowSettingsDialog(dlg);

            bottomActionPanel.Controls.Add(btnSend);
            bottomActionPanel.Controls.Add(btnOpenEmailSettings);

            int selectedCustomerId = 0;
            string selectedCustomerEmail = string.Empty;
            string selectedCustomerFirstName = string.Empty;
            bool isCustomerUnsubscribed = false;
            bool isInCooldown = false;
            int cooldownRemainingDays = 0;

            void UpdateTemplatePreview()
            {
                string seg = cmbTemplate.SelectedItem?.ToString() ?? "New";
                var t = currentData.Templates.FirstOrDefault(x => x.SegmentName == seg)
                    ?? currentData.Templates.FirstOrDefault();

                string displayName = !string.IsNullOrWhiteSpace(selectedCustomerFirstName)
                    ? selectedCustomerFirstName
                    : (!string.IsNullOrWhiteSpace(txtName.Text) ? txtName.Text.Split(' ')[0] : "Valued Customer");

                txtSub.Text = (t?.Subject ?? "Exclusive Offer from Sweet Story for {{customer_name}}").Replace("{{customer_name}}", displayName);
                txtBody.Text = (t?.BodyText ?? "Hi {{customer_name}},\n\nThank you for choosing Sweet Story!").Replace("{{customer_name}}", displayName);
            }

            void ApplySelectedCustomer(CrmDataService.RetentionCustomerSearchResult c)
            {
                selectedCustomerId = c.CustomerId;
                selectedCustomerEmail = c.Email;
                selectedCustomerFirstName = c.FirstName;
                isCustomerUnsubscribed = c.IsUnsubscribed;
                isInCooldown = c.CooldownDaysRemaining > 0;
                cooldownRemainingDays = c.CooldownDaysRemaining;

                txtSearch.Text = c.Email;
                txtName.Text = c.FullName;

                if (cmbTemplate.Items.Contains(c.SegmentName))
                {
                    cmbTemplate.SelectedItem = c.SegmentName;
                }

                lstSuggestions.Visible = false;

                if (c.IsUnsubscribed)
                {
                    lblStatusWarning.Text = "\u26A0 This customer has opted out of marketing emails. Email sending is blocked.";
                    lblStatusWarning.ForeColor = Color.FromArgb(192, 57, 43);
                    lblStatusWarning.Visible = true;
                    chkOverrideCooldown.Visible = false;
                    btnSend.Enabled = false;
                }
                else if (c.CooldownDaysRemaining > 0)
                {
                    lblStatusWarning.Text = $"\u26A0 Customer received an email recently ({c.CooldownDaysRemaining} day(s) cooldown remaining).";
                    lblStatusWarning.ForeColor = Color.FromArgb(230, 126, 34);
                    lblStatusWarning.Visible = true;
                    chkOverrideCooldown.Visible = true;
                    chkOverrideCooldown.Checked = false;
                    btnSend.Enabled = true;
                }
                else
                {
                    lblStatusWarning.Visible = false;
                    chkOverrideCooldown.Visible = false;
                    btnSend.Enabled = true;
                }

                UpdateTemplatePreview();
            }

            if (preselectedCustomer != null)
            {
                selectedCustomerId = preselectedCustomer.CustomerId;
                selectedCustomerEmail = preselectedCustomer.Email;
                selectedCustomerFirstName = preselectedCustomer.CustomerName.Split(' ')[0];
                txtSearch.Text = preselectedCustomer.Email;
                txtName.Text = preselectedCustomer.CustomerName;
                string seg = preselectedCustomer.SegmentName == "Prospect" ? "New" : preselectedCustomer.SegmentName;
                if (cmbTemplate.Items.Contains(seg))
                {
                    cmbTemplate.SelectedItem = seg;
                }

                if (!preselectedCustomer.EligibleToSend)
                {
                    isInCooldown = true;
                    cooldownRemainingDays = 14;
                    lblStatusWarning.Text = "\u26A0 Customer is currently within the 14-day anti-fatigue cooldown.";
                    lblStatusWarning.ForeColor = Color.FromArgb(230, 126, 34);
                    lblStatusWarning.Visible = true;
                    chkOverrideCooldown.Visible = true;
                    chkOverrideCooldown.Checked = false;
                }

                UpdateTemplatePreview();
            }
            else
            {
                UpdateTemplatePreview();
            }

            cmbTemplate.SelectedIndexChanged += (s, e) => UpdateTemplatePreview();
            txtName.Enter += (s, e) => lstSuggestions.Visible = false;
            cmbTemplate.Enter += (s, e) => lstSuggestions.Visible = false;

            var debounceTimer = new System.Windows.Forms.Timer { Interval = 300 };
            debounceTimer.Tick += async (s, e) =>
            {
                debounceTimer.Stop();
                string q = txtSearch.Text.Trim();
                if (q.Length < 1)
                {
                    lstSuggestions.Visible = false;
                    return;
                }

                try
                {
                    var results = await CrmDataService.SearchCustomersForRetentionEmailAsync(
                        q,
                        SessionService.CurrentUser?.CompanyId,
                        8);

                    lstSuggestions.Items.Clear();
                    if (results.Count > 0)
                    {
                        foreach (var item in results)
                        {
                            lstSuggestions.Items.Add(new RetentionCustomerSuggestion { Customer = item });
                        }
                        lstSuggestions.Height = Math.Min(8, results.Count) * 22 + 8;
                        lstSuggestions.Visible = true;
                        lstSuggestions.BringToFront();
                    }
                    else
                    {
                        lstSuggestions.Visible = false;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[CustomerSearch] {ex.Message}");
                    lstSuggestions.Visible = false;
                }
            };

            txtSearch.TextChanged += (s, e) =>
            {
                if (txtSearch.Text != selectedCustomerEmail)
                {
                    selectedCustomerId = 0;
                }
                debounceTimer.Stop();
                debounceTimer.Start();
            };

            txtSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Down && lstSuggestions.Visible && lstSuggestions.Items.Count > 0)
                {
                    lstSuggestions.Focus();
                    if (lstSuggestions.SelectedIndex < 0)
                        lstSuggestions.SelectedIndex = 0;
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    lstSuggestions.Visible = false;
                    e.Handled = true;
                }
            };

            lstSuggestions.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && lstSuggestions.SelectedItem is RetentionCustomerSuggestion item)
                {
                    ApplySelectedCustomer(item.Customer);
                    txtSearch.Focus();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    lstSuggestions.Visible = false;
                    txtSearch.Focus();
                    e.Handled = true;
                }
            };

            lstSuggestions.Click += (s, e) =>
            {
                if (lstSuggestions.SelectedItem is RetentionCustomerSuggestion item)
                {
                    ApplySelectedCustomer(item.Customer);
                }
            };

            btnSend.Click += async (s, e) =>
            {
                if (selectedCustomerId <= 0)
                {
                    MessageBox.Show(
                        "Please search and select a registered customer from the list before sending.",
                        "Select Customer",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtSearch.Focus();
                    return;
                }

                if (isCustomerUnsubscribed)
                {
                    MessageBox.Show(
                        "This customer has unsubscribed and cannot receive marketing emails.",
                        "Unsubscribed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                bool forceOverride = chkOverrideCooldown.Checked;
                if (isInCooldown && !forceOverride)
                {
                    var confirm = MessageBox.Show(
                        $"This customer is currently in the 14-day anti-fatigue cooldown ({cooldownRemainingDays} day(s) remaining).\n\nDo you want to override and send anyway?",
                        "Confirm Anti-Fatigue Override",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (confirm != DialogResult.Yes)
                    {
                        return;
                    }
                    forceOverride = true;
                }

                btnSend.Enabled = false;
                btnSend.Text = "Sending Email...";
                try
                {
                    string seg = cmbTemplate.SelectedItem?.ToString() ?? "New";
                    var log = await CrmDataService.SendManualRetentionEmailAsync(
                        customerId: selectedCustomerId,
                        segmentName: seg,
                        forceIgnoreCooldown: forceOverride,
                        overrideSubject: txtSub.Text.Trim(),
                        overrideBody: txtBody.Text.Trim(),
                        companyId: SessionService.CurrentUser?.CompanyId
                    );

                    dlg.Close();
                    await RefreshDataAsync();

                    if (gridEmailLogs != null && gridEmailLogs.Rows.Count > 0)
                    {
                        UITheme.HighlightNewRow(gridEmailLogs, 0);
                    }

                    MessageBox.Show(
                        $"Retention email successfully delivered!\n\nRecipient: {log.CustomerName} <{log.CustomerEmail}>\nSegment: {log.SegmentName}\nSubject: {log.Subject}\nStatus: {log.Status}\n\nDelivery recorded in Email Logs.",
                        "Email Delivered",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    btnSend.Enabled = true;
                    btnSend.Text = "\u2709 Send Retention Email";

                    var res = MessageBox.Show(
                        $"Failed to send email:\n{ex.Message}\n\nWould you like to open Email Settings to configure SMTP credentials or switch to Sandbox Mode?",
                        "Send Error",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (res == DialogResult.Yes)
                    {
                        EmailSettingsForm.ShowSettingsDialog(dlg);
                    }
                }
            };

            pnl.Controls.Add(txtBody);
            pnl.Controls.Add(lblBody);
            pnl.Controls.Add(txtSub);
            pnl.Controls.Add(lblSub);
            pnl.Controls.Add(searchPanel);
            pnl.Controls.Add(lblDesc);
            pnl.Controls.Add(lblHeader);
            pnl.Controls.Add(bottomActionPanel);

            dlg.Controls.Add(pnl);
            dlg.ShowDialog(this);
        }

        // =========================================================
        // TAB 6: RETENTION REQUESTS & APPROVALS
        // =========================================================

        private void BuildRequestsTab()
        {
            panelRequests = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                AutoScroll = true,
                Visible = false
            };

            // ── Header row (title + New button) ──────────────────────────
            var headerRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 50,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var lblReqTitle = new Label
            {
                Text = "Retention Requests & Approvals",
                Font = new Font(UITheme.FontSans, 13F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };

            bool isManagerOrAbove = SessionService.CurrentUser?.Role is "Manager" or "Business Admin" or "Admin";

            var btnNewRequest = new Button
            {
                Text = "+ New Retention Request",
                Height = 36,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 7, 0, 7),
                Padding = new Padding(14, 0, 14, 0),
                Visible = isManagerOrAbove
            };
            btnNewRequest.FlatAppearance.BorderSize = 0;
            btnNewRequest.Click += async (s, e) =>
            {
                using var modal = new CreateRetentionRequestModal();
                modal.ShowDialog(this);
                await RefreshRequestsTabAsync();
            };

            headerRow.Controls.Add(lblReqTitle, 0, 0);
            headerRow.Controls.Add(btnNewRequest, 1, 0);

            // ── Filter bar ───────────────────────────────────────────────
            var filterBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 46,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10)
            };

            cmbRequestStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Height = 32,
                Width = 130,
                Font = new Font(UITheme.FontSans, 9F),
                Margin = new Padding(0, 7, 8, 7)
            };
            cmbRequestStatusFilter.Items.AddRange(new object[] { "All", "Pending", "Approved", "Rejected" });
            cmbRequestStatusFilter.SelectedIndex = 0;
            cmbRequestStatusFilter.SelectedIndexChanged += async (s, e) =>
            {
                requestsStatusFilter = cmbRequestStatusFilter.SelectedItem?.ToString() ?? "All";
                requestsCurrentPage = 1;
                await RefreshRequestsTabAsync();
            };

            txtRequestSearch = new TextBox
            {
                PlaceholderText = "Search customer or reason...",
                Height = 32,
                Width = 210,
                Font = new Font(UITheme.FontSans, 9F),
                Margin = new Padding(0, 7, 10, 7),
                BorderStyle = BorderStyle.FixedSingle
            };
            var searchDebounce = new System.Windows.Forms.Timer { Interval = 400 };
            searchDebounce.Tick += async (s, e) =>
            {
                searchDebounce.Stop();
                requestsSearchQuery = txtRequestSearch.Text.Trim();
                requestsCurrentPage = 1;
                await RefreshRequestsTabAsync();
            };
            txtRequestSearch.TextChanged += (s, e) => { searchDebounce.Stop(); searchDebounce.Start(); };

            var lblFrom = new Label
            {
                Text = "From:",
                AutoSize = true,
                Font = new Font(UITheme.FontSans, 9F),
                ForeColor = UITheme.TextMuted,
                Margin = new Padding(0, 14, 4, 0)
            };
            dtpRequestFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today.AddMonths(-3),
                Width = 110,
                Margin = new Padding(0, 7, 6, 7)
            };

            var lblTo = new Label
            {
                Text = "To:",
                AutoSize = true,
                Font = new Font(UITheme.FontSans, 9F),
                ForeColor = UITheme.TextMuted,
                Margin = new Padding(0, 14, 4, 0)
            };
            dtpRequestTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Width = 110,
                Margin = new Padding(0, 7, 8, 7)
            };

            var btnApply = new Button
            {
                Text = "Apply",
                Height = 32,
                Width = 65,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 7, 0, 7)
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += async (s, e) =>
            {
                if (dtpRequestFrom.Value.Date > dtpRequestTo.Value.Date)
                {
                    MessageBox.Show("\"From\" date must be before or equal to \"To\" date.", "Invalid Range",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                requestsCurrentPage = 1;
                await RefreshRequestsTabAsync();
            };

            filterBar.Controls.Add(cmbRequestStatusFilter);
            filterBar.Controls.Add(txtRequestSearch);
            filterBar.Controls.Add(lblFrom);
            filterBar.Controls.Add(dtpRequestFrom);
            filterBar.Controls.Add(lblTo);
            filterBar.Controls.Add(dtpRequestTo);
            filterBar.Controls.Add(btnApply);

            // ── DataGridView Container ──────────────────────────────────────────
            var tableCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            tableCard.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, tableCard.Width - 1, tableCard.Height - 1), 8);
            };

            gridRetentionRequests = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = UITheme.BorderColor,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                Cursor = Cursors.Hand
            };

            UITheme.ApplyTableStyle(gridRetentionRequests);
            gridRetentionRequests.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridRetentionRequests.RowTemplate.Height = 48;
            gridRetentionRequests.ColumnHeadersHeight = 42;

            gridRetentionRequests.Columns.AddRange(new DataGridViewColumn[]
            {
                new DataGridViewTextBoxColumn { Name = "colId",          HeaderText = "REF #",          FillWeight = 7,  MinimumWidth = 55,  DataPropertyName = "RequestId" },
                new DataGridViewTextBoxColumn { Name = "colCustomer",    HeaderText = "CUSTOMER",       FillWeight = 16, MinimumWidth = 120, DataPropertyName = "CustomerName" },
                new DataGridViewTextBoxColumn { Name = "colReason",      HeaderText = "REASON FOR RETENTION", FillWeight = 22, MinimumWidth = 140, DataPropertyName = "FullReason" },
                new DataGridViewTextBoxColumn { Name = "colAction",      HeaderText = "ACTION TYPE",    FillWeight = 12, MinimumWidth = 95,  DataPropertyName = "ActionType" },
                new DataGridViewTextBoxColumn { Name = "colDiscount",    HeaderText = "DISCOUNT",      FillWeight = 8,  MinimumWidth = 65,  DataPropertyName = "DiscountPercent" },
                new DataGridViewTextBoxColumn { Name = "colRequestedBy", HeaderText = "REQUESTED BY",   FillWeight = 11, MinimumWidth = 90,  DataPropertyName = "RequestedByUserName" },
                new DataGridViewTextBoxColumn { Name = "colDateReq",     HeaderText = "DATE REQUESTED", FillWeight = 10, MinimumWidth = 85,  DataPropertyName = "RequestedDate" },
                new DataGridViewTextBoxColumn { Name = "colReviewedBy",  HeaderText = "REVIEWED BY",    FillWeight = 11, MinimumWidth = 90,  DataPropertyName = "ReviewedByUserName" },
                new DataGridViewTextBoxColumn { Name = "colDateRev",     HeaderText = "DATE REVIEWED",  FillWeight = 10, MinimumWidth = 85,  DataPropertyName = "ReviewedDate" },
                new DataGridViewTextBoxColumn { Name = "colStatus",      HeaderText = "STATUS",         FillWeight = 11, MinimumWidth = 85,  DataPropertyName = "Status" },
                new DataGridViewTextBoxColumn { Name = "colView",        HeaderText = "ACTION",         FillWeight = 8,  MinimumWidth = 65 }
            });

            gridRetentionRequests.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics == null) return;
                string colName = gridRetentionRequests.Columns[e.ColumnIndex].Name;

                if (colName == "colStatus" && e.Value != null)
                {
                    e.PaintBackground(e.CellBounds, true);
                    string status = e.Value.ToString() ?? "";
                    Color bg = status switch
                    {
                        "Approved" => UITheme.StatusGreenBg,
                        "Rejected" => UITheme.StatusRedBg,
                        _ => UITheme.StatusYellowBg
                    };
                    Color fg = status switch
                    {
                        "Approved" => UITheme.StatusGreenFg,
                        "Rejected" => UITheme.StatusRedFg,
                        _ => UITheme.StatusYellowFg
                    };
                    UITheme.DrawStatusBadge(e.Graphics, e.CellBounds, status, bg, fg);
                    e.Handled = true;
                }
                else if (colName == "colView")
                {
                    e.PaintBackground(e.CellBounds, true);
                    UITheme.DrawActionLink(e.Graphics, e.CellBounds, "View \u2192");
                    e.Handled = true;
                }
            };

            gridRetentionRequests.CellFormatting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Value == null) return;
                string colName = gridRetentionRequests.Columns[e.ColumnIndex].Name;

                if (colName is "colDateReq" or "colDateRev" && e.Value is DateTime dt)
                {
                    e.Value = dt == default ? "—" : dt.ToLocalTime().ToString("MMM dd, yyyy");
                    e.FormattingApplied = true;
                }
                else if (colName == "colDiscount" && e.Value is decimal disc)
                {
                    e.Value = disc > 0 ? $"{disc:0.##}%" : "—";
                    e.FormattingApplied = true;
                }
                else if (colName is "colReviewedBy" or "colDateRev" && e.Value is string sv && string.IsNullOrWhiteSpace(sv))
                {
                    e.Value = "—";
                    e.FormattingApplied = true;
                }
            };

            gridRetentionRequests.CellClick += async (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var row = gridRetentionRequests.Rows[e.RowIndex];
                if (row.DataBoundItem is CC.Domain.Entities.RetentionRequest req)
                {
                    using var modal = new RetentionRequestDetailsModal(req);
                    if (modal.ShowDialogWithBackdrop(this) == DialogResult.OK)
                    {
                        await RefreshRequestsTabAsync(forceRefresh: true);
                        await RefreshDataAsync();
                    }
                }
            };

            // ── Pagination ───────────────────────────────────────────────
            paginationRequests = new PaginationControl
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                BackColor = Color.White
            };
            paginationRequests.PageChanged += newPage =>
            {
                requestsCurrentPage = newPage;
                _ = RefreshRequestsTabAsync(forceRefresh: false);
            };

            tableCard.Controls.Add(gridRetentionRequests);
            tableCard.Controls.Add(paginationRequests);

            // Build content stack (Dock.Fill first, then Dock.Top controls in reverse order)
            panelRequests.Controls.Add(tableCard);
            panelRequests.Controls.Add(filterBar);
            panelRequests.Controls.Add(headerRow);
        }

        private async Task RefreshRequestsTabAsync(bool forceRefresh = false)
        {
            try
            {
                string? statusArg = requestsStatusFilter == "All" ? null : requestsStatusFilter;
                DateTime fromDate = dtpRequestFrom?.Value.Date ?? DateTime.Today.AddMonths(-3);
                DateTime toDate = dtpRequestTo?.Value.Date ?? DateTime.Today;

                if (forceRefresh || _cachedRequests == null)
                {
                    _cachedRequests = await CrmDataService.GetRetentionRequestsAsync(
                        statusFilter: null, // load all to enable instant client-side status filtering
                        fromDate: fromDate,
                        toDate: toDate,
                        companyId: SessionService.CurrentUser?.CompanyId
                    );
                }

                var filtered = _cachedRequests ?? new List<CC.Domain.Entities.RetentionRequest>();

                // Status filter in memory
                if (!string.IsNullOrWhiteSpace(statusArg))
                {
                    filtered = filtered.Where(r => r.Status == statusArg).ToList();
                }

                // Client-side search filter
                if (!string.IsNullOrWhiteSpace(requestsSearchQuery))
                {
                    string q = requestsSearchQuery.ToLower();
                    filtered = filtered.Where(r =>
                        (r.CustomerName?.ToLower().Contains(q) ?? false) ||
                        (r.FullReason?.ToLower().Contains(q) ?? false) ||
                        (r.RequestedByUserName?.ToLower().Contains(q) ?? false) ||
                        (r.ActionType?.ToLower().Contains(q) ?? false)
                    ).ToList();
                }

                int totalCount = filtered.Count;
                int totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)RequestsPageSize));
                if (requestsCurrentPage > totalPages) requestsCurrentPage = totalPages;

                var page = filtered
                    .OrderByDescending(r => r.RequestedDate)
                    .Skip((requestsCurrentPage - 1) * RequestsPageSize)
                    .Take(RequestsPageSize)
                    .ToList();

                gridRetentionRequests.DataSource = null;
                gridRetentionRequests.DataSource = page;

                paginationRequests.SetPagination(requestsCurrentPage, RequestsPageSize, totalCount, "requests");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshRequestsTabAsync] {ex.Message}");
            }
        }

        // =========================================================
        // DATA REFRESH
        // =========================================================

        public async Task RefreshDataAsync()
        {
            if (isAccessDenied) return;

            try
            {
                currentData = await CrmDataService.GetRetentionDashboardDataAsync(SessionService.CurrentUser?.CompanyId);

                // Update Header
                bool autoOn = currentData.Settings.AutoSendingEnabled;
                btnAutoSendToggle.Text = autoOn ? "Auto-Send: ON" : "Auto-Send: OFF";
                btnAutoSendToggle.BackColor = autoOn ? UITheme.StatusGreenFg : Color.FromArgb(180, 70, 70);

                // Render Sub-views
                RenderSegmentCards();
                RenderCustomerGrid();
                UpdateSendSegmentButtonText();
                RenderCampaignCards();
                RenderReportsTab();
                RenderLogsTab();

                // Settings values
                numActiveDays.Value = currentData.Settings.ActiveDaysThreshold;
                numAtRiskDays.Value = currentData.Settings.AtRiskDaysThreshold;
                numLoyalOrders.Value = currentData.Settings.LoyalMinOrders;
                numCooldownDays.Value = currentData.Settings.CooldownDays;
            }
            catch (UnauthorizedAccessException uex)
            {
                MessageBox.Show(uex.Message, "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RetentionCampaignsForm.RefreshDataAsync] {ex.Message}");
            }
        }
    }
}
