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

namespace CC.Forms.Admin.Branches
{
    /// <summary>
    /// Dedicated Branch Management module for Business Admins:
    /// - Governed strictly by subscription plan branching capability (AllowBranching, MaxBranches).
    /// - Displays subscription plan branding, usage quotas, and upgrade restrictions.
    /// - Search, filter (All, Active, Archived), full CRUD (Create, Edit, Soft-Archive, Restore).
    /// - Preserves complete historical data when archiving branches.
    /// </summary>
    public class BranchListForm : Form, INavigationAware
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnAddBranch = null!;

        private TableLayoutPanel kpiTable = null!;
        private Label lblTotalBranches = null!;
        private Label lblActiveBranches = null!;
        private Label lblArchivedBranches = null!;
        private Label lblBranchLimit = null!;

        private Panel searchFilterPanel = null!;
        private Panel searchPill = null!;
        private TextBox txtSearchBox = null!;
        private ComboBox cmbStatusFilter = null!;

        private Panel tableCardPanel = null!;
        private DataGridView gridBranches = null!;
        private PaginationControl pagination = null!;
        private int currentPage = 1;
        private const int PageSize = 10;

        private List<BranchListItem> branchesList = new List<BranchListItem>();
        private BranchCapabilityInfo? branchCapability;
        private string activeSearchQuery = string.Empty;
        private string activeStatusFilter = "All Branches";

        public BranchListForm()
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
            Name = "BranchListForm";
            Padding = Padding.Empty;
            BackColor = UITheme.CreamBackground;
            ResumeLayout(false);
        }

        // =========================================================
        // 1. TOP TOOLBAR
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
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            headerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var titleStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = "Branch Management",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            lblSubtitle = new Label
            {
                Text = "Manage business physical branch locations, managers, and operational assignments",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            btnAddBranch = new Button
            {
                Text = "+ Add Branch",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(150, 42),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 6, 0, 0)
            };
            btnAddBranch.FlatAppearance.BorderSize = 0;
            btnAddBranch.Click += async (s, e) => await OnAddBranchClickedAsync();

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnAddBranch, 1, 0);

            topPanel.Controls.Add(headerTable);
        }

        // =========================================================
        // 2. 4 KPI CARDS (Total, Active, Archived, Plan Limit)
        // =========================================================
        private void BuildKpiCards()
        {
            kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 90,
                ColumnCount = 4,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 14),
                BackColor = Color.Transparent
            };

            for (int i = 0; i < 4; i++)
            {
                kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }

            var cardTotal = CreateKpiCard("TOTAL BRANCHES", out lblTotalBranches, "\uE80F", Color.FromArgb(70, 70, 70));
            var cardActive = CreateKpiCard("ACTIVE LOCATIONS", out lblActiveBranches, "\uE73E", Color.FromArgb(46, 133, 90));
            var cardArchived = CreateKpiCard("ARCHIVED / INACTIVE", out lblArchivedBranches, "\uE74D", Color.FromArgb(180, 110, 40));
            var cardLimit = CreateKpiCard("SUBSCRIPTION QUOTA", out lblBranchLimit, "\uE8C7", UITheme.PrimaryMauve);

            kpiTable.Controls.Add(cardTotal, 0, 0);
            kpiTable.Controls.Add(cardActive, 1, 0);
            kpiTable.Controls.Add(cardArchived, 2, 0);
            kpiTable.Controls.Add(cardLimit, 3, 0);
        }

        private Panel CreateKpiCard(string title, out Label valLabel, string iconGlyph, Color accentColor)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(4, 0, 4, 0),
                Padding = new Padding(16, 12, 16, 12)
            };
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
            };

            var lblHeader = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSans, 7.5F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Location = new Point(16, 12)
            };

            valLabel = new Label
            {
                Text = "0",
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Location = new Point(14, 34)
            };

            var lblIcon = new Label
            {
                Text = iconGlyph,
                Font = new Font("Segoe MDL2 Assets", 15F, FontStyle.Regular),
                ForeColor = accentColor,
                AutoSize = false,
                Size = new Size(34, 34),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(card.Width - 48, 14)
            };
            card.Resize += (s, e) => lblIcon.Location = new Point(card.Width - 48, 14);

            card.Controls.Add(lblHeader);
            card.Controls.Add(valLabel);
            card.Controls.Add(lblIcon);

            return card;
        }

        // =========================================================
        // 3. SEARCH & STATUS FILTER BAR
        // =========================================================
        private void BuildSearchAndFilterBar()
        {
            searchFilterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                Padding = new Padding(0, 8, 0, 12),
                BackColor = Color.Transparent
            };

            var filterFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = false,
                BackColor = Color.Transparent
            };

            // Search pill
            searchPill = new Panel
            {
                Width = 320,
                Height = 40,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 14, 0),
                Padding = new Padding(12, 6, 12, 6)
            };
            searchPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, searchPill.Width - 1, searchPill.Height - 1), 20);
            };

            var lblSearchIcon = new Label
            {
                Text = "\uE721",
                Font = new Font("Segoe MDL2 Assets", 10F),
                ForeColor = UITheme.TextMuted,
                AutoSize = false,
                Size = new Size(20, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Left
            };

            txtSearchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font(UITheme.FontSans, 9.5F),
                ForeColor = UITheme.TextMuted,
                Text = "Search branches by name, code, manager, location...",
                Dock = DockStyle.Fill
            };
            txtSearchBox.Enter += (s, e) =>
            {
                if (txtSearchBox.Text == "Search branches by name, code, manager, location...")
                {
                    txtSearchBox.Text = "";
                    txtSearchBox.ForeColor = UITheme.TextDark;
                }
            };
            txtSearchBox.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtSearchBox.Text))
                {
                    txtSearchBox.Text = "Search branches by name, code, manager, location...";
                    txtSearchBox.ForeColor = UITheme.TextMuted;
                }
            };
            txtSearchBox.TextChanged += (s, e) =>
            {
                if (txtSearchBox.Text != "Search branches by name, code, manager, location...")
                {
                    activeSearchQuery = txtSearchBox.Text.Trim();
                    currentPage = 1;
                    ApplyFiltersAndRender();
                }
            };

            searchPill.Controls.Add(txtSearchBox);
            searchPill.Controls.Add(lblSearchIcon);

            // Status Combo
            cmbStatusFilter = new ComboBox
            {
                Width = 160,
                Height = 38,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F),
                BackColor = Color.White,
                ForeColor = UITheme.TextDark,
                Margin = new Padding(0, 2, 0, 0)
            };
            cmbStatusFilter.Items.AddRange(new object[] { "All Branches", "Active Only", "Archived Only" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += (s, e) =>
            {
                activeStatusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Branches";
                currentPage = 1;
                ApplyFiltersAndRender();
            };

            filterFlow.Controls.Add(searchPill);
            filterFlow.Controls.Add(cmbStatusFilter);

            searchFilterPanel.Controls.Add(filterFlow);
        }

        // =========================================================
        // 4. DATA GRID CARD & PAGINATION
        // =========================================================
        private void BuildDataGridCard()
        {
            tableCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = Padding.Empty
            };
            tableCardPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, tableCardPanel.Width - 1, tableCardPanel.Height - 1), 12);
            };

            gridBranches = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = UITheme.BorderColor,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                EnableHeadersVisualStyles = false,
                RowTemplate = { Height = 56 },
                ColumnHeadersHeight = 44,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
            };

            gridBranches.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(249, 246, 242),
                ForeColor = Color.FromArgb(120, 105, 95),
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 14, 0)
            };

            gridBranches.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = UITheme.TextDark,
                Font = new Font(UITheme.FontSans, 9.5F),
                SelectionBackColor = Color.FromArgb(248, 242, 237),
                SelectionForeColor = UITheme.TextDark,
                Padding = new Padding(14, 0, 14, 0)
            };

            var colCode = new DataGridViewTextBoxColumn
            {
                Name = "colCode",
                HeaderText = "CODE",
                Width = 90
            };
            var colName = new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "BRANCH NAME",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 26F
            };
            var colAddress = new DataGridViewTextBoxColumn
            {
                Name = "colAddress",
                HeaderText = "LOCATION / ADDRESS",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 26F
            };
            var colManager = new DataGridViewTextBoxColumn
            {
                Name = "colManager",
                HeaderText = "MANAGER",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 16F
            };
            var colOrders = new DataGridViewTextBoxColumn
            {
                Name = "colOrders",
                HeaderText = "ORDERS",
                Width = 85,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            var colSales = new DataGridViewTextBoxColumn
            {
                Name = "colSales",
                HeaderText = "TOTAL SALES",
                Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight }
            };
            var colStatus = new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "STATUS",
                Width = 110
            };
            var colActions = new DataGridViewTextBoxColumn
            {
                Name = "colActions",
                HeaderText = "ACTIONS",
                Width = 150
            };

            gridBranches.Columns.AddRange(colCode, colName, colAddress, colManager, colOrders, colSales, colStatus, colActions);

            gridBranches.CellPainting += GridBranches_CellPainting;
            gridBranches.CellClick += GridBranches_CellClick;

            pagination = new PaginationControl
            {
                Dock = DockStyle.Bottom
            };
            pagination.PageChanged += (newPage) =>
            {
                currentPage = newPage;
                ApplyFiltersAndRender();
            };

            tableCardPanel.Controls.Add(gridBranches);
            tableCardPanel.Controls.Add(pagination);
        }

        // =========================================================
        // 5. DATA REFRESH & BINDING
        // =========================================================
        public async Task RefreshDataAsync()
        {
            try
            {
                branchCapability = await CrmDataService.GetBranchCapabilityAsync();
                branchesList = await CrmDataService.GetBranchesAsync(includeArchived: true);

                UpdateCapabilityBanner();
                UpdateKpiCards();
                ApplyFiltersAndRender();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BranchListForm.RefreshDataAsync] {ex.Message}");
            }
        }

        private void UpdateCapabilityBanner()
        {
            if (branchCapability == null) return;

            if (!branchCapability.AllowBranching)
            {
                lblSubtitle.Text = $"Multi-Branching is disabled for your subscription ({branchCapability.PlanName}). Upgrade your subscription to manage multiple branches.";
                btnAddBranch.Visible = false;
            }
            else
            {
                lblSubtitle.Text = $"{branchCapability.ActiveBranchesCount} of {branchCapability.MaxBranches} active branches used \u00B7 {branchCapability.PlanName}";
                btnAddBranch.Visible = true;
                btnAddBranch.Text = "+ Add Branch";
                btnAddBranch.BackColor = UITheme.PrimaryMauve;
            }
        }

        private void UpdateKpiCards()
        {
            int total = branchesList.Count;
            int active = branchesList.Count(b => b.IsActive);
            int archived = total - active;

            lblTotalBranches.Text = total.ToString();
            lblActiveBranches.Text = active.ToString();
            lblArchivedBranches.Text = archived.ToString();

            if (branchCapability != null)
            {
                if (!branchCapability.AllowBranching)
                {
                    lblBranchLimit.Text = "Disabled";
                }
                else
                {
                    lblBranchLimit.Text = $"{active} / {branchCapability.MaxBranches} Allowed";
                }
            }
        }

        private void ApplyFiltersAndRender()
        {
            var filtered = branchesList.AsEnumerable();

            // Status filter
            if (activeStatusFilter == "Active Only")
            {
                filtered = filtered.Where(b => b.IsActive);
            }
            else if (activeStatusFilter == "Archived Only")
            {
                filtered = filtered.Where(b => !b.IsActive);
            }

            // Search filter
            if (!string.IsNullOrWhiteSpace(activeSearchQuery))
            {
                string q = activeSearchQuery.ToLowerInvariant();
                filtered = filtered.Where(b =>
                    b.BranchName.ToLowerInvariant().Contains(q) ||
                    b.BranchCode.ToLowerInvariant().Contains(q) ||
                    (b.Address != null && b.Address.ToLowerInvariant().Contains(q)) ||
                    (b.ManagerName != null && b.ManagerName.ToLowerInvariant().Contains(q)) ||
                    (b.ContactPhone != null && b.ContactPhone.ToLowerInvariant().Contains(q)) ||
                    (b.ContactEmail != null && b.ContactEmail.ToLowerInvariant().Contains(q))
                );
            }

            var resultList = filtered.OrderByDescending(b => b.IsActive).ThenBy(b => b.BranchName).ToList();

            int totalCount = resultList.Count;
            pagination.SetPagination(currentPage, PageSize, totalCount, "branches");

            var pageItems = resultList
                .Skip((currentPage - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            gridBranches.Rows.Clear();

            foreach (var b in pageItems)
            {
                int rowIndex = gridBranches.Rows.Add(
                    b.BranchCode,
                    b.BranchName,
                    !string.IsNullOrWhiteSpace(b.Address) ? b.Address : "—",
                    !string.IsNullOrWhiteSpace(b.ManagerName) ? b.ManagerName : "Unassigned",
                    b.OrderCount.ToString("N0"),
                    $"₱{b.TotalSales:N2}",
                    b.IsActive ? "Active" : "Archived",
                    ""
                );

                gridBranches.Rows[rowIndex].Tag = b;
            }
        }

        // =========================================================
        // 6. CUSTOM CELL PAINTING & ACTIONS
        // =========================================================
        private void GridBranches_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var branch = gridBranches.Rows[e.RowIndex].Tag as BranchListItem;
            if (branch == null) return;

            int colIdxStatus = gridBranches.Columns["colStatus"]?.Index ?? -1;
            int colIdxActions = gridBranches.Columns["colActions"]?.Index ?? -1;
            int colIdxCode = gridBranches.Columns["colCode"]?.Index ?? -1;

            // CODE column: Pill badge
            if (colIdxCode >= 0 && e.ColumnIndex == colIdxCode)
            {
                DrawCodePill(g, e.CellBounds, branch.BranchCode);
                e.Handled = true;
            }
            // STATUS column: Active (green) or Archived (gray)
            else if (colIdxStatus >= 0 && e.ColumnIndex == colIdxStatus)
            {
                string statusText = branch.IsActive ? "• Active" : "• Archived";
                Color pillBg = branch.IsActive ? Color.FromArgb(235, 247, 238) : Color.FromArgb(245, 245, 245);
                Color pillText = branch.IsActive ? Color.FromArgb(46, 133, 90) : Color.FromArgb(128, 128, 128);

                DrawBadgePill(g, e.CellBounds, statusText, pillBg, pillText);
                e.Handled = true;
            }
            // ACTIONS column: Edit | Archive / Restore
            else if (colIdxActions >= 0 && e.ColumnIndex == colIdxActions)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                int yCenter = e.CellBounds.Top + (e.CellBounds.Height - 24) / 2;

                // Edit Button
                var rectEdit = new Rectangle(e.CellBounds.Left + 10, yCenter, 36, 24);
                TextRenderer.DrawText(g, "Edit", font, rectEdit, UITheme.PrimaryMauve, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                // Divider dot
                var rectDot = new Rectangle(e.CellBounds.Left + 46, yCenter, 10, 24);
                TextRenderer.DrawText(g, "·", font, rectDot, Color.FromArgb(180, 170, 165), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Archive / Restore Button
                string toggleText = branch.IsActive ? "Archive" : "Restore";
                Color toggleColor = branch.IsActive ? Color.FromArgb(180, 70, 70) : Color.FromArgb(46, 133, 90);
                var rectToggle = new Rectangle(e.CellBounds.Left + 58, yCenter, 70, 24);
                TextRenderer.DrawText(g, toggleText, font, rectToggle, toggleColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
        }

        private static void DrawCodePill(Graphics g, Rectangle bounds, string code)
        {
            using var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold);
            int pillWidth = 64;
            int pillHeight = 24;
            int pillX = bounds.Left + 12;
            int pillY = bounds.Top + (bounds.Height - pillHeight) / 2;

            var pillRect = new Rectangle(pillX, pillY, pillWidth, pillHeight);
            using (var brush = new SolidBrush(Color.FromArgb(244, 240, 236)))
            {
                g.FillRoundedRectangle(brush, pillRect, 6);
            }

            TextRenderer.DrawText(g, code, font, pillRect, UITheme.PrimaryMauve,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static void DrawBadgePill(Graphics g, Rectangle bounds, string text, Color bgColor, Color textColor)
        {
            using var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold);
            var size = TextRenderer.MeasureText(text, font);
            int pillWidth = size.Width + 16;
            int pillHeight = 24;
            int pillX = bounds.Left + 14;
            int pillY = bounds.Top + (bounds.Height - pillHeight) / 2;

            var pillRect = new Rectangle(pillX, pillY, pillWidth, pillHeight);
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRoundedRectangle(brush, pillRect, 8);
            }

            TextRenderer.DrawText(g, text, font, pillRect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private async void GridBranches_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var branchItem = gridBranches.Rows[e.RowIndex].Tag as BranchListItem;
            if (branchItem == null) return;

            int colIdxActions = gridBranches.Columns["colActions"]?.Index ?? -1;
            if (e.ColumnIndex != colIdxActions) return;

            var cellBounds = gridBranches.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
            var clickPoint = gridBranches.PointToClient(Cursor.Position);
            int relativeX = clickPoint.X - cellBounds.Left;

            // Clicked Edit (X: 10..46)
            if (relativeX >= 10 && relativeX < 46)
            {
                var branchEntity = await CrmDataService.GetBranchByIdAsync(branchItem.BranchId);
                if (branchEntity != null)
                {
                    using var modal = new BranchModal(branchEntity);
                    if (modal.ShowDialog(this) == DialogResult.OK)
                    {
                        await RefreshDataAsync();
                        SessionService.NotifyActiveBranchChanged();
                    }
                }
            }
            // Clicked Archive / Restore (X: 58..140)
            else if (relativeX >= 58 && relativeX <= 140)
            {
                if (branchItem.IsActive)
                {
                    // Confirm Archive
                    var confirm = MessageBox.Show(
                        $"Are you sure you want to archive branch '{branchItem.BranchName}'?\n\n" +
                        "All historical sales orders, customers, and payment transactions will remain intact and reportable, but new transactions cannot be assigned to this branch.",
                        "Archive Branch",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question
                    );

                    if (confirm == DialogResult.Yes)
                    {
                        try
                        {
                            bool success = await CrmDataService.ArchiveBranchAsync(branchItem.BranchId);
                            if (success)
                            {
                                await RefreshDataAsync();
                                SessionService.NotifyActiveBranchChanged();
                            }
                        }
                        catch (InvalidOperationException ex)
                        {
                            MessageBox.Show(ex.Message, "Cannot Archive Branch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to archive branch: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    // Restore branch
                    try
                    {
                        bool success = await CrmDataService.RestoreBranchAsync(branchItem.BranchId);
                        if (success)
                        {
                            await RefreshDataAsync();
                            SessionService.NotifyActiveBranchChanged();
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        MessageBox.Show(ex.Message, "Cannot Restore Branch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to restore branch: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // =========================================================
        // 7. ADD BRANCH ACTION & PLAN LIMIT CHECKS
        // =========================================================
        private async Task OnAddBranchClickedAsync()
        {
            if (branchCapability == null)
            {
                branchCapability = await CrmDataService.GetBranchCapabilityAsync();
            }

            // Check 1: Does plan allow multi-branching?
            if (!branchCapability.AllowBranching)
            {
                MessageBox.Show(
                    $"Multi-Branching is not included in your active subscription plan ({branchCapability.PlanName}).\n\n" +
                    "Your current plan is configured for Single-Branch operations only.\n\n" +
                    "To create multiple branch locations, assign branch managers, and track branch-isolated sales, please upgrade to a subscription plan that supports multi-branch operations.",
                    "Subscription Upgrade Required",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information
                );
                return;
            }

            // Check 2: Has active count reached MaxBranches?
            if (branchCapability.ActiveBranchesCount >= branchCapability.MaxBranches)
            {
                MessageBox.Show(
                    $"You have reached the maximum allowed branches ({branchCapability.MaxBranches}) for your plan ({branchCapability.PlanName}).\n\n" +
                    "To add another branch, you can either archive an unused branch or upgrade your subscription plan limit.",
                    "Branch Limit Reached",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning
                );
                return;
            }

            // Allowed: open BranchModal
            using var modal = new BranchModal();
            if (modal.ShowDialog(this) == DialogResult.OK)
            {
                await RefreshDataAsync();
                SessionService.NotifyActiveBranchChanged();
            }
        }
    }
}
