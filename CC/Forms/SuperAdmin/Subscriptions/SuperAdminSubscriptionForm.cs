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

        private Panel contentCardPanel = null!;
        private DataGridView gridPlans = null!;
        private DataGridView gridBusinesses = null!;
        private Panel searchFilterPanel = null!;
        private TextBox txtSearchBox = null!;
        private ComboBox cmbStatusFilter = null!;

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

            Load += async (s, e) => await RefreshDataAsync();
            VisibleChanged += async (s, e) =>
            {
                if (Visible) await RefreshDataAsync();
            };
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
                Text = "Manage platform subscription tiers, pricing, duration, and business tenant allocations",
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

                searchFilterPanel.Visible = false;
                gridPlans.Visible = true;
                gridBusinesses.Visible = false;
                btnCreatePlan.Visible = true;
            }
            else
            {
                btnTabPlans.BackColor = Color.FromArgb(242, 238, 233);
                btnTabPlans.ForeColor = UITheme.TextDark;
                btnTabBusinesses.BackColor = UITheme.PrimaryMauve;
                btnTabBusinesses.ForeColor = Color.White;

                searchFilterPanel.Visible = true;
                gridPlans.Visible = false;
                gridBusinesses.Visible = true;
                btnCreatePlan.Visible = false;
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
                Visible = false
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
                PlaceholderText = "Search by business name or plan..."
            };
            txtSearchBox.TextChanged += async (s, e) => await RefreshCompanySubscriptionsAsync();
            searchPill.Controls.Add(txtSearchBox);

            cmbStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(340, 4),
                Width = 160,
                Height = 36
            };
            cmbStatusFilter.Items.AddRange(new object[] { "All Status", "Active", "Expiring", "Expired", "Suspended" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += async (s, e) => await RefreshCompanySubscriptionsAsync();

            searchFilterPanel.Controls.Add(searchPill);
            searchFilterPanel.Controls.Add(cmbStatusFilter);
        }

        private void BuildContentCard()
        {
            contentCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(6, 0, 6, 12)
            };

            contentCardPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, contentCardPanel.Width - 1, contentCardPanel.Height - 1), 14);
            };
            contentCardPanel.ApplyRoundedRegion(14);

            // 1. Grid Plans
            gridPlans = CreateStyledGrid();
            gridPlans.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPlanName", HeaderText = "PLAN NAME", FillWeight = 25F });
            gridPlans.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPrice", HeaderText = "PRICE", FillWeight = 18F });
            gridPlans.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDuration", HeaderText = "DURATION", FillWeight = 18F });
            gridPlans.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMaxSeats", HeaderText = "MAX USERS", FillWeight = 15F });
            gridPlans.Columns.Add(new DataGridViewTextBoxColumn { Name = "colActiveSubs", HeaderText = "SUBSCRIBED BIZ", FillWeight = 16F });
            gridPlans.Columns.Add(new DataGridViewTextBoxColumn { Name = "colActions", HeaderText = "ACTIONS", FillWeight = 14F });

            gridPlans.CellPainting += GridPlans_CellPainting;
            gridPlans.CellClick += GridPlans_CellClick;

            // 2. Grid Businesses
            gridBusinesses = CreateStyledGrid();
            gridBusinesses.Visible = false;
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBusiness", HeaderText = "BUSINESS", FillWeight = 25F });
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDatabase", HeaderText = "DATABASE", FillWeight = 18F });
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCurrentPlan", HeaderText = "PLAN", FillWeight = 16F });
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colValidity", HeaderText = "VALIDITY DATES", FillWeight = 22F });
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS", FillWeight = 14F });
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBizActions", HeaderText = "ACTIONS", FillWeight = 18F });

            gridBusinesses.CellPainting += GridBusinesses_CellPainting;
            gridBusinesses.CellClick += GridBusinesses_CellClick;

            contentCardPanel.Controls.Add(gridBusinesses);
            contentCardPanel.Controls.Add(gridPlans);
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
        // CUSTOM RENDERING: PLANS
        // =========================================================

        private void GridPlans_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;
            e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var plan = gridPlans.Rows[e.RowIndex].Tag as SubscriptionPlanListItem;
            if (plan == null) return;

            int colPlan = gridPlans.Columns["colPlanName"].Index;
            int colPrice = gridPlans.Columns["colPrice"].Index;
            int colDur = gridPlans.Columns["colDuration"].Index;
            int colSeats = gridPlans.Columns["colMaxSeats"].Index;
            int colSubs = gridPlans.Columns["colActiveSubs"].Index;
            int colActions = gridPlans.Columns["colActions"].Index;

            if (e.ColumnIndex == colPlan)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                TextRenderer.DrawText(g, plan.PlanName, font, rect, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colPrice)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                TextRenderer.DrawText(g, $"₱{plan.Price:N2}", font, rect, Color.FromArgb(46, 133, 90), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colDur)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                string durText = plan.DurationDays >= 365 ? $"{plan.DurationDays / 365} Year(s)" : $"{plan.DurationDays} Days";
                TextRenderer.DrawText(g, durText, font, rect, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colSeats)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                TextRenderer.DrawText(g, $"{plan.MaxUsers} Seats", font, rect, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colSubs)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                TextRenderer.DrawText(g, $"{plan.ActiveSubscribedBusinesses} business(es)", font, rect, UITheme.PrimaryMauve, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            else if (e.ColumnIndex == colActions)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                var btnRect = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top + (e.CellBounds.Height - 28) / 2, 72, 28);
                using var brush = new SolidBrush(Color.FromArgb(240, 235, 230));
                g.FillRoundedRectangle(brush, btnRect, 6);
                TextRenderer.DrawText(g, "Edit", font, btnRect, UITheme.TextDark, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
        }

        private async void GridPlans_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var plan = gridPlans.Rows[e.RowIndex].Tag as SubscriptionPlanListItem;
            if (plan == null) return;

            if (e.ColumnIndex == gridPlans.Columns["colActions"].Index)
            {
                OpenEditPlanModal(plan);
            }
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

            int colBiz = gridBusinesses.Columns["colBusiness"].Index;
            int colDb = gridBusinesses.Columns["colDatabase"].Index;
            int colPlan = gridBusinesses.Columns["colCurrentPlan"].Index;
            int colVal = gridBusinesses.Columns["colValidity"].Index;
            int colStat = gridBusinesses.Columns["colStatus"].Index;
            int colAct = gridBusinesses.Columns["colBizActions"].Index;

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
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                TextRenderer.DrawText(g, biz.PlanName, font, rect, UITheme.PrimaryMauve, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
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

            if (e.ColumnIndex == gridBusinesses.Columns["colBizActions"].Index)
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
                _plansList = await CrmDataService.GetSubscriptionPlansAsync();
                await RefreshCompanySubscriptionsAsync();

                // Update Plans Grid
                gridPlans.Rows.Clear();
                foreach (var p in _plansList)
                {
                    int rowIdx = gridPlans.Rows.Add();
                    gridPlans.Rows[rowIdx].Tag = p;
                }

                // Update KPI Cards
                lblTotalPlans.Text = _plansList.Count.ToString();
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
    // PLAN MODAL (Add / Edit)
    // =========================================================

    public class PlanModal : Form
    {
        private TextBox txtName = null!;
        private NumericUpDown numPrice = null!;
        private NumericUpDown numDuration = null!;
        private NumericUpDown numSeats = null!;
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
            Size = new Size(460, 480);
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
            var header = new Label
            {
                Text = _isEdit ? "Edit Subscription Plan" : "Create Subscription Plan",
                Font = new Font(UITheme.FontSerif, 16F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Location = new Point(24, 20),
                AutoSize = true
            };

            var lblSub = new Label
            {
                Text = "Set pricing, validity period, and team member user limits",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Location = new Point(24, 50),
                AutoSize = true
            };

            int top = 85;

            // Plan Name
            Controls.Add(new Label { Text = "PLAN NAME", Location = new Point(24, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            txtName = new TextBox { Location = new Point(24, top + 20), Width = 390, Font = new Font(UITheme.FontSans, 10F) };
            if (_isEdit) txtName.Text = _existingPlan!.PlanName;
            Controls.Add(txtName);
            top += 55;

            // Price
            Controls.Add(new Label { Text = "PRICE (PHP)", Location = new Point(24, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            numPrice = new NumericUpDown { Location = new Point(24, top + 20), Width = 390, Font = new Font(UITheme.FontSans, 10F), Maximum = 1000000, DecimalPlaces = 2 };
            numPrice.Value = _isEdit ? _existingPlan!.Price : 4999m;
            Controls.Add(numPrice);
            top += 55;

            // Duration (Days)
            Controls.Add(new Label { Text = "DURATION (DAYS)", Location = new Point(24, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            numDuration = new NumericUpDown { Location = new Point(24, top + 20), Width = 185, Font = new Font(UITheme.FontSans, 10F), Maximum = 3650, Minimum = 1 };
            numDuration.Value = _isEdit ? _existingPlan!.DurationDays : 365;
            Controls.Add(numDuration);

            // Max Users / Seats
            Controls.Add(new Label { Text = "MAX SEATS (USERS)", Location = new Point(225, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            numSeats = new NumericUpDown { Location = new Point(225, top + 20), Width = 189, Font = new Font(UITheme.FontSans, 10F), Maximum = 500, Minimum = 1 };
            numSeats.Value = _isEdit ? _existingPlan!.MaxUsers : 5;
            Controls.Add(numSeats);
            top += 60;

            lblError = new Label { Location = new Point(24, top), Width = 390, Height = 30, ForeColor = Color.FromArgb(190, 60, 60), Font = new Font(UITheme.FontSans, 8.5F) };
            Controls.Add(lblError);
            top += 35;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(220, top),
                Size = new Size(95, 38),
                BackColor = Color.FromArgb(239, 231, 223),
                ForeColor = UITheme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.ApplyRoundedRegion(8);
            btnCancel.Click += (s, e) => Close();

            btnSave = new Button
            {
                Text = _isEdit ? "Update Plan" : "Create Plan",
                Location = new Point(325, top),
                Size = new Size(115, 38),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.ApplyRoundedRegion(8);
            btnSave.Click += async (s, e) => await SavePlanAsync();

            Controls.Add(header);
            Controls.Add(lblSub);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
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

            btnSave.Enabled = false;
            try
            {
                if (_isEdit && _existingPlan != null)
                {
                    await CrmDataService.UpdateSubscriptionPlanAsync(_existingPlan.PlanId, name, price, dur, seats);
                }
                else
                {
                    await CrmDataService.CreateSubscriptionPlanAsync(name, price, dur, seats);
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
    // ASSIGN SUBSCRIPTION MODAL
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
            _availablePlans = plans;

            Text = $"Assign Subscription: {biz.CompanyName}";
            Size = new Size(460, 440);
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
            var header = new Label
            {
                Text = "Assign Subscription Plan",
                Font = new Font(UITheme.FontSerif, 16F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Location = new Point(24, 20),
                AutoSize = true
            };

            var lblBiz = new Label
            {
                Text = $"Business: {_biz.CompanyName} ({_biz.CompanyCode})",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Location = new Point(24, 50),
                AutoSize = true
            };

            int top = 85;

            // Plan Selector
            Controls.Add(new Label { Text = "SUBSCRIPTION PLAN", Location = new Point(24, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            cmbPlans = new ComboBox { Location = new Point(24, top + 20), Width = 390, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(UITheme.FontSans, 10F) };
            int selectedIdx = 0;
            for (int i = 0; i < _availablePlans.Count; i++)
            {
                var p = _availablePlans[i];
                cmbPlans.Items.Add($"{p.PlanName} (₱{p.Price:N2} · {p.DurationDays} Days · {p.MaxUsers} Seats)");
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
            Controls.Add(cmbPlans);
            top += 55;

            // Start Date
            Controls.Add(new Label { Text = "START DATE", Location = new Point(24, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            dtStart = new DateTimePicker { Location = new Point(24, top + 20), Width = 185, Format = DateTimePickerFormat.Short, Value = _biz.StartDate };
            Controls.Add(dtStart);

            // Expiration Date
            Controls.Add(new Label { Text = "EXPIRATION DATE", Location = new Point(225, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            dtEnd = new DateTimePicker { Location = new Point(225, top + 20), Width = 189, Format = DateTimePickerFormat.Short, Value = _biz.EndDate };
            Controls.Add(dtEnd);
            top += 55;

            // Status
            Controls.Add(new Label { Text = "SUBSCRIPTION STATUS", Location = new Point(24, top), Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            cmbStatus = new ComboBox { Location = new Point(24, top + 20), Width = 390, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font(UITheme.FontSans, 10F) };
            cmbStatus.Items.AddRange(new object[] { "Active (StatusId 1)", "Expired (StatusId 2)", "Suspended (StatusId 3)", "Cancelled (StatusId 4)" });
            cmbStatus.SelectedIndex = _biz.StatusId switch
            {
                1 => 0,
                2 => 1,
                3 => 2,
                4 => 3,
                _ => 0
            };
            Controls.Add(cmbStatus);
            top += 60;

            lblError = new Label { Location = new Point(24, top), Width = 390, Height = 25, ForeColor = Color.FromArgb(190, 60, 60), Font = new Font(UITheme.FontSans, 8.5F) };
            Controls.Add(lblError);
            top += 30;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(220, top),
                Size = new Size(95, 38),
                BackColor = Color.FromArgb(239, 231, 223),
                ForeColor = UITheme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.ApplyRoundedRegion(8);
            btnCancel.Click += (s, e) => Close();

            btnSave = new Button
            {
                Text = "Save Subscription",
                Location = new Point(325, top),
                Size = new Size(135, 38),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.ApplyRoundedRegion(8);
            btnSave.Click += async (s, e) => await SaveSubscriptionAsync();

            Controls.Add(header);
            Controls.Add(lblBiz);
            Controls.Add(btnCancel);
            Controls.Add(btnSave);
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
