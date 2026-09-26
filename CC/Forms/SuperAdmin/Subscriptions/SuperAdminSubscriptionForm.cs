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

namespace CC.Forms.SuperAdmin.Subscriptions
{
    public class SuperAdminSubscriptionForm : Form
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnCreatePlan = null!;

        private TableLayoutPanel kpiTable = null!;
        private Label lblTotalPlans = null!;
        private Label lblActiveSubs = null!;
        private Label lblExpiringSubs = null!;
        private Label lblExpiredSubs = null!;

        private Panel tabBarPanel = null!;
        private Button btnTabPlans = null!;
        private Button btnTabBusinesses = null!;
        private int _currentTab = 0; // 0 = Plans, 1 = Business Subscriptions

        private Panel searchFilterPanel = null!;
        private TextBox txtSearchBox = null!;
        private ComboBox cmbStatusFilter = null!;
        private ComboBox cmbPlanArchiveFilter = null!;

        private Panel contentCardPanel = null!;
        private FlowLayoutPanel flowPlansCards = null!;
        private Panel bizContainer = null!;
        private DataGridView gridBusinesses = null!;

        private List<SubscriptionPlanListItem> _plansList = new();
        private List<CompanySubscriptionListItem> _companySubsList = new();

        public SuperAdminSubscriptionForm()
        {
            InitializeComponent();
            BuildTopToolbar();
            BuildKpiCards();
            BuildTabBar();
            BuildSearchAndFilterBar();
            BuildContentCard();

            Controls.Add(contentCardPanel);
            Controls.Add(searchFilterPanel);
            Controls.Add(tabBarPanel);
            Controls.Add(kpiTable);
            Controls.Add(topPanel);

            Load += async (s, e) =>
            {
                await RefreshDataAsync();
                UpdateCardSizes();
            };
            VisibleChanged += async (s, e) =>
            {
                if (Visible)
                {
                    await RefreshDataAsync();
                    UpdateCardSizes();
                }
            };
            Resize += (s, e) => UpdateCardSizes();
            Layout += (s, e) => UpdateCardSizes();
        }

        public SuperAdminSubscriptionForm(int initialTab = 0, string? initialStatus = null) : this()
        {
            if (initialTab == 1)
            {
                SwitchTab(1);
            }
            if (!string.IsNullOrWhiteSpace(initialStatus))
            {
                int idx = cmbStatusFilter.FindStringExact(initialStatus);
                if (idx >= 0)
                {
                    cmbStatusFilter.SelectedIndex = idx;
                }
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1140, 800);
            Font = new Font(UITheme.FontSans, 9.5F);
            FormBorderStyle = FormBorderStyle.None;
            Name = "SuperAdminSubscriptionForm";
            Padding = Padding.Empty;
            BackColor = UITheme.CreamBackground;
            ResumeLayout(false);
        }

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
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

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
                Text = "Subscription Management",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            lblSubtitle = new Label
            {
                Text = "Manage platform subscription tiers, pricing, duration, branching, and business tenant allocations",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            btnCreatePlan = new Button
            {
                Text = "+ Create New Plan",
                Size = new Size(160, 40),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0, 6, 0, 0)
            };
            btnCreatePlan.FlatAppearance.BorderSize = 0;
            btnCreatePlan.ApplyRoundedRegion(8);
            btnCreatePlan.Click += (s, e) => OpenCreatePlanModal();

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnCreatePlan, 1, 0);
            topPanel.Controls.Add(headerTable);
        }

        private void BuildKpiCards()
        {
            kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 90,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = Padding.Empty,
                Margin = new Padding(0, 0, 0, 16)
            };

            for (int i = 0; i < 4; i++)
            {
                kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }

            var card1 = CreateKpiCard("SUBSCRIPTION PLANS", "0", out lblTotalPlans, Color.FromArgb(70, 60, 55));
            var card2 = CreateKpiCard("ACTIVE SUBSCRIBERS", "0", out lblActiveSubs, Color.FromArgb(46, 133, 90));
            var card3 = CreateKpiCard("EXPIRING SOON", "0", out lblExpiringSubs, Color.FromArgb(210, 145, 50));
            var card4 = CreateKpiCard("EXPIRED SUBSCRIBERS", "0", out lblExpiredSubs, Color.FromArgb(190, 60, 60));

            kpiTable.Controls.Add(card1, 0, 0);
            kpiTable.Controls.Add(card2, 1, 0);
            kpiTable.Controls.Add(card3, 2, 0);
            kpiTable.Controls.Add(card4, 3, 0);
        }

        private Panel CreateKpiCard(string title, string initialValue, out Label valLabel, Color accentColor)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(6, 0, 6, 12),
                Padding = new Padding(16, 12, 16, 12)
            };

            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, pnl.Width - 1, pnl.Height - 1), 12);
            };
            pnl.ApplyRoundedRegion(12);

            var lblT = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSans, 7.5F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 16
            };

            valLabel = new Label
            {
                Text = initialValue,
                Font = new Font(UITheme.FontSerif, 20F, FontStyle.Bold),
                ForeColor = accentColor,
                Dock = DockStyle.Fill
            };

            pnl.Controls.Add(valLabel);
            pnl.Controls.Add(lblT);
            return pnl;
        }

        private void BuildTabBar()
        {
            tabBarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(6, 0, 6, 4)
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            btnTabPlans = new Button
            {
                Text = "Subscription Plans",
                Size = new Size(160, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnTabPlans.FlatAppearance.BorderSize = 0;
            btnTabPlans.ApplyRoundedRegion(8);
            btnTabPlans.Click += (s, e) => SwitchTab(0);

            btnTabBusinesses = new Button
            {
                Text = "Business Subscriptions",
                Size = new Size(180, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(242, 238, 233),
                ForeColor = UITheme.TextDark,
                Cursor = Cursors.Hand,
                Margin = Padding.Empty
            };
            btnTabBusinesses.FlatAppearance.BorderSize = 0;
            btnTabBusinesses.ApplyRoundedRegion(8);
            btnTabBusinesses.Click += (s, e) => SwitchTab(1);

            flow.Controls.Add(btnTabPlans);
            flow.Controls.Add(btnTabBusinesses);
            tabBarPanel.Controls.Add(flow);
        }

        private void SwitchTab(int tab)
        {
            _currentTab = tab;
            if (_currentTab == 0)
            {
                btnTabPlans.BackColor = UITheme.PrimaryMauve;
                btnTabPlans.ForeColor = Color.White;
                btnTabBusinesses.BackColor = Color.FromArgb(242, 238, 233);
                btnTabBusinesses.ForeColor = UITheme.TextDark;

                cmbPlanArchiveFilter.Visible = true;
                cmbStatusFilter.Visible = false;
                txtSearchBox.PlaceholderText = "Search subscription plans...";

                flowPlansCards.Visible = true;
                bizContainer.Visible = false;
                gridBusinesses.Visible = false;
                btnCreatePlan.Visible = true;

                RenderPlanCards();
            }
            else
            {
                btnTabPlans.BackColor = Color.FromArgb(242, 238, 233);
                btnTabPlans.ForeColor = UITheme.TextDark;
                btnTabBusinesses.BackColor = UITheme.PrimaryMauve;
                btnTabBusinesses.ForeColor = Color.White;

                cmbPlanArchiveFilter.Visible = false;
                cmbStatusFilter.Visible = true;
                txtSearchBox.PlaceholderText = "Search by business name or plan...";

                flowPlansCards.Visible = false;
                bizContainer.Visible = true;
                gridBusinesses.Visible = true;
                btnCreatePlan.Visible = false;

                _ = RefreshCompanySubscriptionsAsync();
            }
        }

        private void BuildSearchAndFilterBar()
        {
            searchFilterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(6, 0, 6, 8),
                Visible = true
            };

            var searchPill = new Panel
            {
                Size = new Size(320, 38),
                BackColor = Color.White,
                Location = new Point(6, 0)
            };
            searchPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, searchPill.Width - 1, searchPill.Height - 1), 8);
                using var font = new Font("Segoe MDL2 Assets", 10F);
                TextRenderer.DrawText(e.Graphics, "\uE721", font, new Rectangle(10, 0, 20, searchPill.Height), UITheme.TextMuted, TextFormatFlags.VerticalCenter);
            };
            searchPill.ApplyRoundedRegion(8);

            txtSearchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(34, 10),
                Width = 276,
                PlaceholderText = "Search subscription plans..."
            };
            txtSearchBox.TextChanged += (s, e) =>
            {
                if (_currentTab == 0) RenderPlanCards();
                else _ = RefreshCompanySubscriptionsAsync();
            };
            searchPill.Controls.Add(txtSearchBox);

            // Tab 0 Plan filter: Active vs Archived vs All
            cmbPlanArchiveFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(340, 4),
                Width = 160,
                Height = 36,
                Visible = true
            };
            cmbPlanArchiveFilter.Items.AddRange(new object[] { "Active Plans", "Archived Plans", "All Plans" });
            cmbPlanArchiveFilter.SelectedIndex = 0;
            cmbPlanArchiveFilter.SelectedIndexChanged += (s, e) => RenderPlanCards();

            // Tab 1 Business Subscription status filter
            cmbStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(340, 4),
                Width = 160,
                Height = 36,
                Visible = false
            };
            cmbStatusFilter.Items.AddRange(new object[] { "All Status", "Active", "Expiring", "Expired", "Suspended" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += async (s, e) => await RefreshCompanySubscriptionsAsync();

            searchFilterPanel.Controls.Add(searchPill);
            searchFilterPanel.Controls.Add(cmbPlanArchiveFilter);
            searchFilterPanel.Controls.Add(cmbStatusFilter);
        }

        private void BuildContentCard()
        {
            contentCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = Padding.Empty,
                Margin = new Padding(6, 0, 6, 12)
            };

            // 1. FlowLayoutPanel for Cards
            // 1. FlowLayoutPanel for Cards
            flowPlansCards = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                WrapContents = true,
                Padding = new Padding(6, 4, 6, 24)
            };
            flowPlansCards.Resize += (s, e) => UpdateCardSizes();
            contentCardPanel.Resize += (s, e) => UpdateCardSizes();

            // 2. Grid Businesses (Tab 1)
            bizContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Visible = false
            };
            bizContainer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, bizContainer.Width - 1, bizContainer.Height - 1), 14);
            };
            bizContainer.ApplyRoundedRegion(14);

            gridBusinesses = CreateStyledGrid();
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBusiness", HeaderText = "BUSINESS", FillWeight = 25F });
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDatabase", HeaderText = "DATABASE", FillWeight = 16F });
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCurrentPlan", HeaderText = "PLAN & BRANCHES", FillWeight = 20F });
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colValidity", HeaderText = "VALIDITY DATES", FillWeight = 21F });
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS", FillWeight = 12F });
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBizActions", HeaderText = "ACTIONS", FillWeight = 18F });

            gridBusinesses.CellPainting += GridBusinesses_CellPainting;
            gridBusinesses.CellClick += GridBusinesses_CellClick;

            bizContainer.Controls.Add(gridBusinesses);

            contentCardPanel.Controls.Add(bizContainer);
            contentCardPanel.Controls.Add(flowPlansCards);
        }

        private DataGridView CreateStyledGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 235, 230),
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                RowTemplate = { Height = 48 },
                ColumnHeadersHeight = 40,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 248, 246);
            grid.DefaultCellStyle.SelectionForeColor = UITheme.TextDark;

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.TextMuted;
            grid.ColumnHeadersDefaultCellStyle.Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);

            return grid;
        }

        // =========================================================
        // CARD RENDERING: SUBSCRIPTION PLANS
        // =========================================================

        private int _lastComputedWidth = 0;

        private void UpdateCardSizes()
        {
            if (flowPlansCards == null || flowPlansCards.Controls.Count == 0) return;

            int usableWidth = flowPlansCards.ClientSize.Width - flowPlansCards.Padding.Horizontal - 16;
            if (usableWidth < 260) return;

            if (Math.Abs(usableWidth - _lastComputedWidth) < 4) return;
            _lastComputedWidth = usableWidth;

            // Responsive column breakpoints:
            // >= 1150px -> 4 columns
            // >= 780px  -> 3 columns
            // >= 480px  -> 2 columns
            // < 480px   -> 1 column
            int cols = usableWidth >= 1150 ? 4 : (usableWidth >= 780 ? 3 : (usableWidth >= 480 ? 2 : 1));
            int gap = 16;
            int rowGap = 24;
            int totalGaps = cols * gap;
            int cardWidth = (usableWidth - totalGaps) / cols;
            cardWidth = Math.Max(260, Math.Min(cardWidth, 480));

            flowPlansCards.SuspendLayout();
            foreach (Control c in flowPlansCards.Controls)
            {
                if (c is Panel card && card.Tag is SubscriptionPlanListItem)
                {
                    card.Width = cardWidth;
                    card.Height = 380;
                    card.Margin = new Padding(0, 0, gap, rowGap);
                    card.Invalidate();
                }
            }
            flowPlansCards.ResumeLayout();
        }

        private void RenderPlanCards()
        {
            flowPlansCards.SuspendLayout();
            flowPlansCards.Controls.Clear();

            string search = txtSearchBox?.Text.Trim().ToLower() ?? "";
            string filter = cmbPlanArchiveFilter?.SelectedItem?.ToString() ?? "Active Plans";

            var filtered = _plansList.AsEnumerable();

            if (filter == "Active Plans")
                filtered = filtered.Where(p => p.IsActive);
            else if (filter == "Archived Plans")
                filtered = filtered.Where(p => !p.IsActive);

            if (!string.IsNullOrWhiteSpace(search))
            {
                filtered = filtered.Where(p => p.PlanName.ToLower().Contains(search));
            }

            // Deduplicate by PlanName to prevent duplicate cards
            var list = filtered
                .GroupBy(p => p.PlanName.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(g => g.OrderByDescending(p => p.PlanId).First())
                .OrderBy(p => p.Price)
                .ToList();

            if (list.Count == 0)
            {
                var emptyPanel = new Panel
                {
                    Size = new Size(700, 200),
                    BackColor = Color.White,
                    Margin = new Padding(12, 20, 12, 12),
                    Padding = new Padding(24)
                };
                emptyPanel.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    using var pen = new Pen(UITheme.BorderColor, 1f);
                    e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, emptyPanel.Width - 1, emptyPanel.Height - 1), 12);
                };
                emptyPanel.ApplyRoundedRegion(12);

                var lblEmpty = new Label
                {
                    Text = "No subscription plans found matching your criteria.\nUse '+ Create New Plan' to add a new subscription tier or change your filter.",
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font(UITheme.FontSans, 10.5F),
                    ForeColor = UITheme.TextMuted
                };
                emptyPanel.Controls.Add(lblEmpty);
                flowPlansCards.Controls.Add(emptyPanel);
                flowPlansCards.ResumeLayout();
                return;
            }

            int usableWidth = flowPlansCards.ClientSize.Width - flowPlansCards.Padding.Horizontal - 16;
            int cols = usableWidth >= 1150 ? 4 : (usableWidth >= 780 ? 3 : (usableWidth >= 480 ? 2 : 1));
            int gap = 16;
            int rowGap = 24;
            int totalGaps = cols * gap;
            int initialWidth = usableWidth >= 260 ? (usableWidth - totalGaps) / cols : 300;
            initialWidth = Math.Max(260, Math.Min(initialWidth, 480));

            foreach (var plan in list)
            {
                var card = CreatePlanCard(plan, initialWidth, gap, rowGap);
                flowPlansCards.Controls.Add(card);
            }

            flowPlansCards.ResumeLayout();
            _lastComputedWidth = 0;
            UpdateCardSizes();
        }

        private Panel CreatePlanCard(SubscriptionPlanListItem plan, int initialWidth = 300, int gap = 16, int rowGap = 24)
        {
            var card = new Panel
            {
                Size = new Size(initialWidth, 380),
                BackColor = Color.White,
                Margin = new Padding(0, 0, gap, rowGap),
                Padding = Padding.Empty,
                Tag = plan
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(plan.IsActive ? UITheme.BorderColor : Color.FromArgb(220, 215, 210), 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
            };
            card.ApplyRoundedRegion(14);

            // 1. Top Header Panel (Plan name & badge)
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 82,
                Padding = new Padding(20, 16, 20, 6),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            var topRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 24,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            // Status Pill (Left)
            var statusPill = new Panel
            {
                Dock = DockStyle.Left,
                Width = 84,
                Height = 22,
                BackColor = plan.IsActive ? Color.FromArgb(235, 247, 238) : Color.FromArgb(245, 242, 238)
            };
            statusPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var font = new Font(UITheme.FontSans, 7.5F, FontStyle.Bold);
                var text = plan.IsActive ? "● ACTIVE" : "● ARCHIVED";
                var fg = plan.IsActive ? Color.FromArgb(46, 133, 90) : Color.FromArgb(140, 130, 120);
                TextRenderer.DrawText(e.Graphics, text, font, statusPill.ClientRectangle, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            statusPill.ApplyRoundedRegion(6);
            topRow.Controls.Add(statusPill);

            // Active Biz Count (Right)
            var lblBizBadge = new Label
            {
                Text = $"{plan.ActiveSubscribedBusinesses} BIZ",
                Dock = DockStyle.Right,
                Width = 70,
                TextAlign = ContentAlignment.MiddleRight,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                BackColor = Color.White
            };
            topRow.Controls.Add(lblBizBadge);

            // Plan Title
            var lblPlanTitle = new Label
            {
                Text = plan.PlanName,
                Dock = DockStyle.Bottom,
                Height = 32,
                Font = new Font(UITheme.FontSerif, 14.5F, FontStyle.Bold),
                ForeColor = plan.IsActive ? UITheme.TextDark : UITheme.TextMuted,
                TextAlign = ContentAlignment.BottomLeft,
                BackColor = Color.White,
                UseMnemonic = false
            };

            pnlHeader.Controls.Add(lblPlanTitle);
            pnlHeader.Controls.Add(topRow);

            // 2. Pricing Row
            var pnlPrice = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                Padding = new Padding(20, 2, 20, 6),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            var lblPrice = new Label
            {
                Text = $"₱{plan.Price:N2}",
                Font = new Font(UITheme.FontSerif, 17F, FontStyle.Bold),
                ForeColor = plan.IsActive ? Color.FromArgb(46, 133, 90) : UITheme.TextMuted,
                Dock = DockStyle.Left,
                AutoSize = true,
                BackColor = Color.White
            };

            string durText = plan.DurationDays >= 365 ? $" / {plan.DurationDays / 365} Year(s)" : $" / {plan.DurationDays} Days";
            var lblDuration = new Label
            {
                Text = durText,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Left,
                AutoSize = true,
                BackColor = Color.White,
                Margin = new Padding(0, 5, 0, 0)
            };

            pnlPrice.Controls.Add(lblDuration);
            pnlPrice.Controls.Add(lblPrice);

            // 3. Features Checklist (Middle) - cleanly stacked with pure white background and zero lines
            var pnlFeatures = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(20, 10, 20, 6),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            var featStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                BorderStyle = BorderStyle.None
            };

            // Feature 1: Max Users
            featStack.Controls.Add(CreateFeatureItem("\uE73E", $"{plan.MaxUsers} Team Members / User Seats", true));

            // Feature 2: Branching capability
            if (plan.AllowBranching)
            {
                featStack.Controls.Add(CreateFeatureItem("\uE73E", $"Multi-Branch: Up to {plan.MaxBranches} Branches", true));
            }
            else
            {
                featStack.Controls.Add(CreateFeatureItem("\uE711", "Single Store (Branching Disabled)", false));
            }

            // Feature 3: Duration
            featStack.Controls.Add(CreateFeatureItem("\uE73E", $"Duration: {plan.DurationDays} Days Full Access", true));

            // Feature 4: Active businesses
            featStack.Controls.Add(CreateFeatureItem("\uE73E", $"{plan.ActiveSubscribedBusinesses} Active Business Tenant(s)", true));

            pnlFeatures.Controls.Add(featStack);

            // 4. Actions Panel (Bottom) with responsive 50/50 button split and pure white background
            var pnlActions = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                Padding = new Padding(20, 8, 20, 20),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            var actionsTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var btnEdit = new Button
            {
                Text = "Edit Plan",
                Dock = DockStyle.Fill,
                Height = 36,
                Margin = new Padding(0, 0, 6, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = Color.FromArgb(242, 238, 233),
                ForeColor = UITheme.TextDark,
                Cursor = Cursors.Hand
            };
            btnEdit.FlatAppearance.BorderSize = 0;
            btnEdit.ApplyRoundedRegion(6);
            btnEdit.Click += (s, e) => OpenEditPlanModal(plan);

            Button btnArchiveRestore;
            if (plan.IsActive)
            {
                btnArchiveRestore = new Button
                {
                    Text = "Archive",
                    Dock = DockStyle.Fill,
                    Height = 36,
                    Margin = new Padding(6, 0, 0, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                    BackColor = Color.FromArgb(254, 240, 240),
                    ForeColor = Color.FromArgb(190, 60, 60),
                    Cursor = Cursors.Hand
                };
                btnArchiveRestore.FlatAppearance.BorderSize = 0;
                btnArchiveRestore.ApplyRoundedRegion(6);
                btnArchiveRestore.Click += async (s, e) => await PromptArchivePlanAsync(plan);
            }
            else
            {
                btnArchiveRestore = new Button
                {
                    Text = "Restore",
                    Dock = DockStyle.Fill,
                    Height = 36,
                    Margin = new Padding(6, 0, 0, 0),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                    BackColor = Color.FromArgb(235, 247, 238),
                    ForeColor = Color.FromArgb(46, 133, 90),
                    Cursor = Cursors.Hand
                };
                btnArchiveRestore.FlatAppearance.BorderSize = 0;
                btnArchiveRestore.ApplyRoundedRegion(6);
                btnArchiveRestore.Click += async (s, e) => await PromptRestorePlanAsync(plan);
            }

            actionsTable.Controls.Add(btnEdit, 0, 0);
            actionsTable.Controls.Add(btnArchiveRestore, 1, 0);

            pnlActions.Controls.Add(actionsTable);

            // Add with proper docking and Z-order so nothing overlaps or gets clipped
            card.Controls.Add(pnlFeatures);
            card.Controls.Add(pnlPrice);
            card.Controls.Add(pnlHeader);
            card.Controls.Add(pnlActions);
            pnlFeatures.BringToFront();

            return card;
        }

        private Panel CreateFeatureItem(string iconChar, string text, bool isPositive)
        {
            var pnl = new Panel
            {
                Size = new Size(380, 28),
                Margin = new Padding(0, 4, 0, 4),
                BackColor = Color.White,
                BorderStyle = BorderStyle.None
            };

            var icon = new Label
            {
                Text = iconChar,
                Font = new Font("Segoe MDL2 Assets", 9F, FontStyle.Bold),
                ForeColor = isPositive ? Color.FromArgb(46, 133, 90) : Color.FromArgb(170, 160, 150),
                Location = new Point(0, 2),
                Size = new Size(20, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.White
            };

            var lbl = new Label
            {
                Text = text,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = isPositive ? UITheme.TextDark : UITheme.TextMuted,
                Location = new Point(26, 3),
                AutoSize = true,
                BackColor = Color.White,
                UseMnemonic = false
            };

            pnl.Controls.Add(icon);
            pnl.Controls.Add(lbl);
            return pnl;
        }

        // =========================================================
        // CUSTOM RENDERING: BUSINESSES
        // =========================================================

        private void GridBusinesses_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;
            e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var biz = gridBusinesses.Rows[e.RowIndex].Tag as CompanySubscriptionListItem;
            if (biz == null) return;

            int colBiz = gridBusinesses.Columns["colBusiness"]?.Index ?? 0;
            int colDb = gridBusinesses.Columns["colDatabase"]?.Index ?? 1;
            int colPlan = gridBusinesses.Columns["colCurrentPlan"]?.Index ?? 2;
            int colVal = gridBusinesses.Columns["colValidity"]?.Index ?? 3;
            int colStat = gridBusinesses.Columns["colStatus"]?.Index ?? 4;
            int colAct = gridBusinesses.Columns["colBizActions"]?.Index ?? 5;

            if (e.ColumnIndex == colBiz)
            {
                using var fontName = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                using var fontCode = new Font(UITheme.FontSans, 8F, FontStyle.Regular);

                var rectName = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top + 8, e.CellBounds.Width - 14, 18);
                var rectCode = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top + 26, e.CellBounds.Width - 14, 16);

                TextRenderer.DrawText(g, biz.CompanyName, fontName, rectName, UITheme.TextDark, TextFormatFlags.Left);
                TextRenderer.DrawText(g, $"Code: {biz.CompanyCode}", fontCode, rectCode, UITheme.TextMuted, TextFormatFlags.Left);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colDb)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                TextRenderer.DrawText(g, biz.DatabaseName, font, rect, Color.FromArgb(70, 60, 55), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colPlan)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                using var fontSub = new Font(UITheme.FontSans, 7.5F, FontStyle.Regular);

                var rectPlan = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top + 8, e.CellBounds.Width - 14, 18);
                var rectBranch = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top + 26, e.CellBounds.Width - 14, 16);

                string branchDesc = biz.AllowBranching ? $"Max {biz.MaxBranches} branches" : "Single location";
                TextRenderer.DrawText(g, biz.PlanName, font, rectPlan, UITheme.PrimaryMauve, TextFormatFlags.Left);
                TextRenderer.DrawText(g, $"{biz.MaxUsers} Seats · {branchDesc}", fontSub, rectBranch, UITheme.TextMuted, TextFormatFlags.Left);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colVal)
            {
                using var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                string valText = $"{biz.StartDate:yyyy-MM-dd} → {biz.EndDate:yyyy-MM-dd}";
                TextRenderer.DrawText(g, valText, font, rect, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colStat)
            {
                bool isActive = biz.StatusName.Equals("Active", StringComparison.OrdinalIgnoreCase) && biz.EndDate >= DateTime.UtcNow;
                string statusText = isActive ? "Active" : "Expired";

                Color bg = isActive ? Color.FromArgb(235, 247, 238) : Color.FromArgb(253, 237, 237);
                Color fg = isActive ? Color.FromArgb(46, 133, 90) : Color.FromArgb(190, 60, 60);

                var pillRect = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top + (e.CellBounds.Height - 24) / 2, 68, 24);
                using var b = new SolidBrush(bg);
                g.FillRoundedRectangle(b, pillRect, 6);

                using var font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
                TextRenderer.DrawText(g, statusText, font, pillRect, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colAct)
            {
                using var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold);

                // Assign/Change button
                var btnAssignRect = new Rectangle(e.CellBounds.Left + 6, e.CellBounds.Top + (e.CellBounds.Height - 26) / 2, 78, 26);
                using (var brush = new SolidBrush(Color.FromArgb(240, 235, 230)))
                {
                    g.FillRoundedRectangle(brush, btnAssignRect, 6);
                }
                TextRenderer.DrawText(g, "Change", font, btnAssignRect, UITheme.TextDark, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Renew button
                var btnRenewRect = new Rectangle(e.CellBounds.Left + 90, e.CellBounds.Top + (e.CellBounds.Height - 26) / 2, 68, 26);
                using (var brush = new SolidBrush(Color.FromArgb(235, 247, 238)))
                {
                    g.FillRoundedRectangle(brush, btnRenewRect, 6);
                }
                TextRenderer.DrawText(g, "Renew", font, btnRenewRect, Color.FromArgb(46, 133, 90), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
        }

        private async void GridBusinesses_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var biz = gridBusinesses.Rows[e.RowIndex].Tag as CompanySubscriptionListItem;
            if (biz == null) return;

            if (e.ColumnIndex == (gridBusinesses.Columns["colBizActions"]?.Index ?? -1))
            {
                var cellBounds = gridBusinesses.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                var mousePos = gridBusinesses.PointToClient(Cursor.Position);
                int relX = mousePos.X - cellBounds.Left;

                if (relX < 85)
                {
                    // Assign / Change Plan
                    OpenAssignSubscriptionModal(biz);
                }
                else
                {
                    // Renew
                    await PromptRenewSubscriptionAsync(biz);
                }
            }
        }

        // =========================================================
        // ACTIONS & MODALS
        // =========================================================

        private void OpenCreatePlanModal()
        {
            using var modal = new PlanModal();
            if (modal.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
            {
                _ = RefreshDataAsync();
            }
        }

        private void OpenEditPlanModal(SubscriptionPlanListItem plan)
        {
            using var modal = new PlanModal(plan);
            if (modal.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
            {
                _ = RefreshDataAsync();
            }
        }

        private async Task PromptArchivePlanAsync(SubscriptionPlanListItem plan)
        {
            string msg = plan.ActiveSubscribedBusinesses > 0
                ? $"Are you sure you want to archive '{plan.PlanName}'?\n\nNote: {plan.ActiveSubscribedBusinesses} business tenant(s) currently use this plan. Existing subscriptions will continue working normally, but no new businesses will be able to enroll in this plan."
                : $"Are you sure you want to archive '{plan.PlanName}'?\n\nIt will be hidden from new subscription pickers and active listings.";

            var res = MessageBox.Show(msg, "Archive Subscription Plan", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                await CrmDataService.ArchiveSubscriptionPlanAsync(plan.PlanId);
                MessageBox.Show($"Subscription plan '{plan.PlanName}' has been archived.", "Plan Archived", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await RefreshDataAsync();
            }
        }

        private async Task PromptRestorePlanAsync(SubscriptionPlanListItem plan)
        {
            var res = MessageBox.Show($"Restore '{plan.PlanName}' to active status?\n\nIt will become available again for business subscriptions.", "Restore Subscription Plan", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (res == DialogResult.Yes)
            {
                await CrmDataService.RestoreSubscriptionPlanAsync(plan.PlanId);
                MessageBox.Show($"Subscription plan '{plan.PlanName}' has been restored to active status.", "Plan Restored", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await RefreshDataAsync();
            }
        }

        private void OpenAssignSubscriptionModal(CompanySubscriptionListItem biz)
        {
            using var modal = new AssignSubscriptionModal(biz, _plansList);
            if (modal.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
            {
                _ = RefreshDataAsync();
            }
        }

        private async Task PromptRenewSubscriptionAsync(CompanySubscriptionListItem biz)
        {
            var res = MessageBox.Show(
                $"Renew subscription for '{biz.CompanyName}' for an additional {biz.DurationDays} days?\n\nPlan: {biz.PlanName}\nPrice: ₱{biz.Price:N2}",
                "Confirm Subscription Renewal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                await CrmDataService.RenewSubscriptionAsync(biz.CompanyId);
                MessageBox.Show($"Subscription for '{biz.CompanyName}' has been renewed successfully!", "Renewal Confirmed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await RefreshDataAsync();
            }
        }

        // =========================================================
        // DATA REFRESH
        // =========================================================

        public async Task RefreshDataAsync()
        {
            try
            {
                _plansList = await CrmDataService.GetSubscriptionPlansAsync(includeArchived: true);
                await RefreshCompanySubscriptionsAsync();

                RenderPlanCards();

                // Update KPI Cards
                lblTotalPlans.Text = _plansList.Count(p => p.IsActive).ToString();
                int activeCount = _companySubsList.Count(s => s.StatusName.Equals("Active", StringComparison.OrdinalIgnoreCase) && s.EndDate >= DateTime.UtcNow);
                int expiringCount = _companySubsList.Count(s => s.EndDate >= DateTime.UtcNow && s.EndDate <= DateTime.UtcNow.AddDays(30));
                int expiredCount = _companySubsList.Count(s => s.EndDate < DateTime.UtcNow || s.StatusName.Equals("Expired", StringComparison.OrdinalIgnoreCase));

                lblActiveSubs.Text = activeCount.ToString();
                lblExpiringSubs.Text = expiringCount.ToString();
                lblExpiredSubs.Text = expiredCount.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SuperAdminSubscriptionForm.RefreshDataAsync] {ex.Message}");
            }
        }

        private async Task RefreshCompanySubscriptionsAsync()
        {
            string q = txtSearchBox?.Text ?? "";
            _companySubsList = await CrmDataService.GetCompanySubscriptionsAsync(q);

            string filter = cmbStatusFilter?.SelectedItem?.ToString() ?? "All Status";
            var filtered = _companySubsList;
            if (filter == "Active")
                filtered = filtered.Where(s => s.StatusName.Equals("Active", StringComparison.OrdinalIgnoreCase) && s.EndDate >= DateTime.UtcNow).ToList();
            else if (filter == "Expiring")
                filtered = filtered.Where(s => s.EndDate >= DateTime.UtcNow && s.EndDate <= DateTime.UtcNow.AddDays(30)).ToList();
            else if (filter == "Expired")
                filtered = filtered.Where(s => s.EndDate < DateTime.UtcNow || s.StatusName.Equals("Expired", StringComparison.OrdinalIgnoreCase)).ToList();
            else if (filter == "Suspended")
                filtered = filtered.Where(s => s.StatusName.Equals("Suspended", StringComparison.OrdinalIgnoreCase)).ToList();

            gridBusinesses.Rows.Clear();
            foreach (var b in filtered)
            {
                int rowIdx = gridBusinesses.Rows.Add();
                gridBusinesses.Rows[rowIdx].Tag = b;
            }
        }
    }

    // =========================================================
    // PLAN MODAL (Add / Edit) with Branching & Non-Overlapping Layout
    // =========================================================

    public class PlanModal : Form
    {
        private TextBox txtName = null!;
        private NumericUpDown numPrice = null!;
        private NumericUpDown numDuration = null!;
        private NumericUpDown numSeats = null!;
        private CheckBox chkAllowBranching = null!;
        private NumericUpDown numMaxBranches = null!;
        private CheckBox chkIsActive = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private Label lblError = null!;

        private readonly SubscriptionPlanListItem? _existingPlan;
        private readonly bool _isEdit;

        public PlanModal(SubscriptionPlanListItem? plan = null)
        {
            _existingPlan = plan;
            _isEdit = plan != null;

            Text = _isEdit ? "Edit Subscription Plan" : "Create New Subscription Plan";
            Size = new Size(520, 640);
            MinimumSize = new Size(480, 560);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font(UITheme.FontSans, 9.5F);

            BuildUI();
        }

        private void BuildUI()
        {
            // Bottom button container docked at bottom so controls NEVER overlap
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                BackColor = Color.FromArgb(250, 248, 246),
                Padding = new Padding(24, 14, 24, 14)
            };
            bottomPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 230, 225), 1f);
                e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(105, 40),
                BackColor = Color.FromArgb(239, 231, 223),
                ForeColor = UITheme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(bottomPanel.Width - 265, 14)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.ApplyRoundedRegion(8);
            btnCancel.Click += (s, e) => Close();

            btnSave = new Button
            {
                Text = _isEdit ? "Update Plan" : "Create Plan",
                Size = new Size(140, 40),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(bottomPanel.Width - 150, 14)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.ApplyRoundedRegion(8);
            btnSave.Click += async (s, e) => await SavePlanAsync();

            bottomPanel.Controls.Add(btnCancel);
            bottomPanel.Controls.Add(btnSave);

            // Scrollable body panel
            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(28, 20, 28, 20)
            };

            var header = new Label
            {
                Text = _isEdit ? "Edit Subscription Plan" : "Create Subscription Plan",
                Font = new Font(UITheme.FontSerif, 17F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Location = new Point(28, 16),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = "Configure plan pricing, validity duration, team member seats, and branching capabilities",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Location = new Point(28, 48),
                AutoSize = true
            };

            int top = 82;

            // Plan Name
            bodyPanel.Controls.Add(new Label { Text = "PLAN NAME", Location = new Point(28, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted, AutoSize = true });
            txtName = new TextBox { Location = new Point(28, top + 20), Width = 445, Font = new Font(UITheme.FontSans, 10F) };
            if (_isEdit) txtName.Text = _existingPlan!.PlanName;
            bodyPanel.Controls.Add(txtName);
            top += 58;

            // Price
            bodyPanel.Controls.Add(new Label { Text = "PRICE (PHP)", Location = new Point(28, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted, AutoSize = true });
            numPrice = new NumericUpDown { Location = new Point(28, top + 20), Width = 445, Font = new Font(UITheme.FontSans, 10F), Maximum = 10000000, DecimalPlaces = 2 };
            numPrice.Value = _isEdit ? _existingPlan!.Price : 4999m;
            bodyPanel.Controls.Add(numPrice);
            top += 58;

            // Duration (Days)
            bodyPanel.Controls.Add(new Label { Text = "DURATION (DAYS)", Location = new Point(28, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted, AutoSize = true });
            numDuration = new NumericUpDown { Location = new Point(28, top + 20), Width = 212, Font = new Font(UITheme.FontSans, 10F), Maximum = 3650, Minimum = 1 };
            numDuration.Value = _isEdit ? _existingPlan!.DurationDays : 365;
            bodyPanel.Controls.Add(numDuration);

            // Max Users / Seats
            bodyPanel.Controls.Add(new Label { Text = "MAX SEATS (USERS)", Location = new Point(260, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted, AutoSize = true });
            numSeats = new NumericUpDown { Location = new Point(260, top + 20), Width = 213, Font = new Font(UITheme.FontSans, 10F), Maximum = 1000, Minimum = 1 };
            numSeats.Value = _isEdit ? _existingPlan!.MaxUsers : 5;
            bodyPanel.Controls.Add(numSeats);
            top += 62;

            // Branching Capability Group
            var grpBranching = new Panel
            {
                Location = new Point(28, top),
                Width = 445,
                Height = 85,
                BackColor = Color.FromArgb(250, 248, 246)
            };
            grpBranching.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(235, 230, 225), 1f);
                e.Graphics.DrawRoundedRectangle(p, new Rectangle(0, 0, grpBranching.Width - 1, grpBranching.Height - 1), 8);
            };
            grpBranching.ApplyRoundedRegion(8);

            chkAllowBranching = new CheckBox
            {
                Text = "Enable Multi-Branching for this plan",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Location = new Point(16, 12),
                AutoSize = true,
                Checked = _isEdit ? _existingPlan!.AllowBranching : false
            };

            var lblMaxBranch = new Label
            {
                Text = "Maximum Allowed Branches:",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Location = new Point(16, 44),
                AutoSize = true
            };

            numMaxBranches = new NumericUpDown
            {
                Location = new Point(230, 42),
                Width = 100,
                Font = new Font(UITheme.FontSans, 9.5F),
                Minimum = 1,
                Maximum = 500,
                Value = _isEdit ? Math.Max(1, _existingPlan!.MaxBranches) : 3,
                Enabled = chkAllowBranching.Checked
            };

            chkAllowBranching.CheckedChanged += (s, e) =>
            {
                numMaxBranches.Enabled = chkAllowBranching.Checked;
            };

            grpBranching.Controls.Add(chkAllowBranching);
            grpBranching.Controls.Add(lblMaxBranch);
            grpBranching.Controls.Add(numMaxBranches);
            bodyPanel.Controls.Add(grpBranching);
            top += 95;

            // Status Checkbox
            chkIsActive = new CheckBox
            {
                Text = "Active Plan (Available for new tenant subscriptions)",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                Location = new Point(28, top),
                AutoSize = true,
                Checked = _isEdit ? _existingPlan!.IsActive : true
            };
            bodyPanel.Controls.Add(chkIsActive);
            top += 36;

            lblError = new Label
            {
                Location = new Point(28, top),
                Width = 445,
                Height = 35,
                ForeColor = Color.FromArgb(190, 60, 60),
                Font = new Font(UITheme.FontSans, 8.5F)
            };
            bodyPanel.Controls.Add(lblError);

            bodyPanel.Controls.Add(header);
            bodyPanel.Controls.Add(lblSub);

            Controls.Add(bodyPanel);
            Controls.Add(bottomPanel);
        }

        private async Task SavePlanAsync()
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                lblError.Text = "Please enter a valid plan name.";
                return;
            }

            decimal price = numPrice.Value;
            int dur = (int)numDuration.Value;
            int seats = (int)numSeats.Value;
            bool allowBranching = chkAllowBranching.Checked;
            int maxBranches = allowBranching ? (int)numMaxBranches.Value : 1;
            bool isActive = chkIsActive.Checked;

            btnSave.Enabled = false;
            try
            {
                if (_isEdit && _existingPlan != null)
                {
                    await CrmDataService.UpdateSubscriptionPlanAsync(
                        _existingPlan.PlanId,
                        name,
                        price,
                        dur,
                        seats,
                        allowBranching,
                        maxBranches,
                        isActive);
                }
                else
                {
                    await CrmDataService.CreateSubscriptionPlanAsync(
                        name,
                        price,
                        dur,
                        seats,
                        allowBranching,
                        maxBranches);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                btnSave.Enabled = true;
            }
        }
    }

    // =========================================================
    // ASSIGN SUBSCRIPTION MODAL with Active Filter & Non-Overlapping Layout
    // =========================================================

    public class AssignSubscriptionModal : Form
    {
        private ComboBox cmbPlans = null!;
        private DateTimePicker dtStart = null!;
        private DateTimePicker dtEnd = null!;
        private ComboBox cmbStatus = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private Label lblError = null!;

        private readonly CompanySubscriptionListItem _biz;
        private readonly List<SubscriptionPlanListItem> _availablePlans;

        public AssignSubscriptionModal(CompanySubscriptionListItem biz, List<SubscriptionPlanListItem> plans)
        {
            _biz = biz;
            // Only active plans for new assignments, plus the business's current plan if it's archived
            _availablePlans = plans.Where(p => p.IsActive || p.PlanId == biz.PlanId).ToList();

            Text = $"Assign Subscription: {biz.CompanyName}";
            Size = new Size(520, 560);
            MinimumSize = new Size(480, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;
            Font = new Font(UITheme.FontSans, 9.5F);

            BuildUI();
        }

        private void BuildUI()
        {
            var bottomPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                BackColor = Color.FromArgb(250, 248, 246),
                Padding = new Padding(24, 14, 24, 14)
            };
            bottomPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(235, 230, 225), 1f);
                e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(105, 40),
                BackColor = Color.FromArgb(239, 231, 223),
                ForeColor = UITheme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(bottomPanel.Width - 280, 14)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.ApplyRoundedRegion(8);
            btnCancel.Click += (s, e) => Close();

            btnSave = new Button
            {
                Text = "Save Subscription",
                Size = new Size(155, 40),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Location = new Point(bottomPanel.Width - 165, 14)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.ApplyRoundedRegion(8);
            btnSave.Click += async (s, e) => await SaveSubscriptionAsync();

            bottomPanel.Controls.Add(btnCancel);
            bottomPanel.Controls.Add(btnSave);

            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(28, 20, 28, 20)
            };

            var header = new Label
            {
                Text = "Assign Subscription Plan",
                Font = new Font(UITheme.FontSerif, 17F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Location = new Point(28, 16),
                AutoSize = true
            };

            var lblBiz = new Label
            {
                Text = $"Business: {_biz.CompanyName} ({_biz.CompanyCode})",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Location = new Point(28, 48),
                AutoSize = true
            };

            int top = 82;

            // Plan Selector
            bodyPanel.Controls.Add(new Label { Text = "SUBSCRIPTION PLAN", Location = new Point(28, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted, AutoSize = true });
            cmbPlans = new ComboBox { Location = new Point(28, top + 20), Width = 445, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(UITheme.FontSans, 9.5F) };
            int selectedIdx = 0;
            for (int i = 0; i < _availablePlans.Count; i++)
            {
                var p = _availablePlans[i];
                string branchDesc = p.AllowBranching ? $"Max {p.MaxBranches} Branches" : "Single Store";
                string archTag = !p.IsActive ? " [ARCHIVED]" : "";
                cmbPlans.Items.Add($"{p.PlanName}{archTag} (₱{p.Price:N2} · {p.MaxUsers} Seats · {branchDesc} · {p.DurationDays} Days)");
                if (p.PlanId == _biz.PlanId) selectedIdx = i;
            }
            if (cmbPlans.Items.Count > 0) cmbPlans.SelectedIndex = selectedIdx;
            cmbPlans.SelectedIndexChanged += (s, e) =>
            {
                if (cmbPlans.SelectedIndex >= 0 && cmbPlans.SelectedIndex < _availablePlans.Count)
                {
                    var chosen = _availablePlans[cmbPlans.SelectedIndex];
                    dtEnd.Value = dtStart.Value.AddDays(chosen.DurationDays > 0 ? chosen.DurationDays : 365);
                }
            };
            bodyPanel.Controls.Add(cmbPlans);
            top += 58;

            // Start Date
            bodyPanel.Controls.Add(new Label { Text = "START DATE", Location = new Point(28, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted, AutoSize = true });
            dtStart = new DateTimePicker { Location = new Point(28, top + 20), Width = 212, Format = DateTimePickerFormat.Short, Value = _biz.StartDate, Font = new Font(UITheme.FontSans, 9.5F) };
            bodyPanel.Controls.Add(dtStart);

            // Expiration Date
            bodyPanel.Controls.Add(new Label { Text = "EXPIRATION DATE", Location = new Point(260, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted, AutoSize = true });
            dtEnd = new DateTimePicker { Location = new Point(260, top + 20), Width = 213, Format = DateTimePickerFormat.Short, Value = _biz.EndDate, Font = new Font(UITheme.FontSans, 9.5F) };
            bodyPanel.Controls.Add(dtEnd);
            top += 58;

            // Status
            bodyPanel.Controls.Add(new Label { Text = "SUBSCRIPTION STATUS", Location = new Point(28, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted, AutoSize = true });
            cmbStatus = new ComboBox { Location = new Point(28, top + 20), Width = 445, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(UITheme.FontSans, 9.5F) };
            cmbStatus.Items.AddRange(new object[] { "Active (StatusId 1)", "Expired (StatusId 2)", "Suspended (StatusId 3)", "Cancelled (StatusId 4)" });
            cmbStatus.SelectedIndex = _biz.StatusId switch
            {
                1 => 0,
                2 => 1,
                3 => 2,
                4 => 3,
                _ => 0
            };
            bodyPanel.Controls.Add(cmbStatus);
            top += 62;

            lblError = new Label
            {
                Location = new Point(28, top),
                Width = 445,
                Height = 35,
                ForeColor = Color.FromArgb(190, 60, 60),
                Font = new Font(UITheme.FontSans, 8.5F)
            };
            bodyPanel.Controls.Add(lblError);

            bodyPanel.Controls.Add(header);
            bodyPanel.Controls.Add(lblBiz);

            Controls.Add(bodyPanel);
            Controls.Add(bottomPanel);
        }

        private async Task SaveSubscriptionAsync()
        {
            if (cmbPlans.SelectedIndex < 0 || cmbPlans.SelectedIndex >= _availablePlans.Count)
            {
                lblError.Text = "Please select a plan.";
                return;
            }

            var chosen = _availablePlans[cmbPlans.SelectedIndex];
            int statusId = cmbStatus.SelectedIndex + 1;
            DateTime sDate = dtStart.Value;
            DateTime eDate = dtEnd.Value;

            if (eDate <= sDate)
            {
                lblError.Text = "Expiration date must be later than start date.";
                return;
            }

            btnSave.Enabled = false;
            try
            {
                await CrmDataService.AssignCompanySubscriptionAsync(_biz.CompanyId, chosen.PlanId, statusId, sDate, eDate);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
                btnSave.Enabled = true;
            }
        }
    }
}
