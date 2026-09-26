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

namespace CC.Forms.Admin.Subscription
{
    /// <summary>
    /// Subscription Management form for Business Admin / Owner:
    /// - Current Plan card with Active pill, renewal date, card details
    /// - Interactive Seat Usage progress bar (dynamic user seat count)
    /// - Plan feature highlights checklist (2 columns with checkmark icons)
    /// - Billing History table with Paid badge pills and Download invoice buttons
    /// - Renew and Upgrade / Downgrade actions
    /// </summary>
    public class SubscriptionForm : Form, INavigationAware
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;

        private Panel mainContentPanel = null!;
        private Panel currentPlanCard = null!;
        private Label lblPlanName = null!;
        private Label lblPlanPrice = null!;
        private Label lblRenewalDate = null!;
        private Label lblPaymentMethod = null!;
        private Label lblSeatCount = null!;
        private Panel seatProgressBar = null!;
        private Label lblFeatureSeats = null!;
        private Label lblFeatureBranching = null!;

        private Button btnRenew = null!;
        private Button btnUpgradeDowngrade = null!;

        private Panel featuresCard = null!;
        private Panel billingHistoryCard = null!;
        private DataGridView gridBillingHistory = null!;
        private PaginationControl pagination = null!;
        private int currentPage = 1;
        private const int PageSize = 10;

        private SubscriptionInfo currentSub = null!;
        private List<BillingHistoryItem> billingItems = new List<BillingHistoryItem>();

        public SubscriptionForm()
        {
            InitializeComponent();
            BuildTopToolbar();
            BuildMainContent();

            Controls.Add(mainContentPanel);
            Controls.Add(topPanel);
        }

        public async Task InitializeDataAsync() => await RefreshDataAsync();

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1100, 850);
            Font = new Font(UITheme.FontSans, 9.5F);
            FormBorderStyle = FormBorderStyle.None;
            Name = "SubscriptionForm";
            Padding = Padding.Empty;
            BackColor = UITheme.CreamBackground;
            ResumeLayout(false);
        }

        // =========================================================
        // 1. TOP TOOLBAR (Title, Subtitle)
        // =========================================================
        private void BuildTopToolbar()
        {
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 75,
                Padding = new Padding(0, 0, 0, 14),
                BackColor = Color.Transparent
            };

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
                Text = "Manage your plan, billing details, and team member seats",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            topPanel.Controls.Add(titleStack);
        }

        // =========================================================
        // 2. MAIN CONTENT (Scrollable Layout)
        // =========================================================
        private void BuildMainContent()
        {
            mainContentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 16, 28)
            };

            BuildCurrentPlanCard();
            BuildFeaturesCard();
            BuildBillingHistoryCard();

            // Clear 20px vertical spacing between cards
            var spacer1 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 20,
                BackColor = Color.Transparent
            };

            var spacer2 = new Panel
            {
                Dock = DockStyle.Top,
                Height = 20,
                BackColor = Color.Transparent
            };

            mainContentPanel.Controls.Add(billingHistoryCard);
            mainContentPanel.Controls.Add(spacer2);
            mainContentPanel.Controls.Add(featuresCard);
            mainContentPanel.Controls.Add(spacer1);
            mainContentPanel.Controls.Add(currentPlanCard);
        }

        // =========================================================
        // 3. CURRENT PLAN CARD
        // =========================================================
        private void BuildCurrentPlanCard()
        {
            currentPlanCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 236,
                BackColor = Color.White,
                Padding = new Padding(32, 24, 32, 24),
                Margin = Padding.Empty
            };

            currentPlanCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, currentPlanCard.Width - 1, currentPlanCard.Height - 1), 12);
            };
            currentPlanCard.ApplyRoundedRegion(12);

            // Header Row: Left plan name + pill, Right action buttons
            var headerRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 58,
                BackColor = Color.White
            };

            var planTitleColumn = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Left,
                BackColor = Color.White
            };

            var lblBadgeHeader = new Label
            {
                Text = "CURRENT PLAN",
                Font = new Font(UITheme.FontSans, 7.5F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2),
                BackColor = Color.White
            };

            var namePillRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.White
            };

            lblPlanName = new Label
            {
                Text = "Pro Plan",
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 12, 0),
                BackColor = Color.White
            };

            var activePill = new Panel
            {
                Size = new Size(74, 24),
                BackColor = Color.FromArgb(235, 247, 238),
                Margin = new Padding(0, 4, 0, 0)
            };
            activePill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
                var rect = new Rectangle(0, 0, activePill.Width, activePill.Height);
                TextRenderer.DrawText(e.Graphics, "• Active", font, rect, Color.FromArgb(46, 133, 90),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            activePill.ApplyRoundedRegion(6);

            namePillRow.Controls.Add(lblPlanName);
            namePillRow.Controls.Add(activePill);

            planTitleColumn.Controls.Add(lblBadgeHeader);
            planTitleColumn.Controls.Add(namePillRow);

            // Right action buttons: Renew Subscription & Upgrade / Downgrade
            var planBtnStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Right,
                AutoSize = true,
                BackColor = Color.White,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnUpgradeDowngrade = new Button
            {
                Text = "Upgrade / Downgrade",
                Height = 38,
                Width = 175,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Margin = new Padding(10, 0, 0, 0)
            };
            btnUpgradeDowngrade.FlatAppearance.BorderSize = 0;
            btnUpgradeDowngrade.ApplyRoundedRegion(8);
            btnUpgradeDowngrade.Click += async (s, e) => await PromptUpgradeDowngradeAsync();

            btnRenew = new Button
            {
                Text = "Renew Subscription",
                Height = 38,
                Width = 160,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = UITheme.TextDark,
                Margin = new Padding(0, 0, 0, 0)
            };
            btnRenew.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnRenew.ApplyRoundedRegion(8);
            btnRenew.Click += async (s, e) => await PromptRenewSubscriptionAsync();

            planBtnStack.Controls.Add(btnUpgradeDowngrade);
            planBtnStack.Controls.Add(btnRenew);

            headerRow.Controls.Add(planTitleColumn);
            headerRow.Controls.Add(planBtnStack);

            // Details Row: Price, Renewal Date, Payment Method
            var detailsRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Padding = new Padding(0, 6, 0, 0)
            };

            lblPlanPrice = new Label
            {
                Text = "₱9,599 / year  \u00B7  Billed annually",
                Font = new Font(UITheme.FontSans, 10F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 28, 0),
                BackColor = Color.White
            };

            lblRenewalDate = new Label
            {
                Text = "Next billing: August 15, 2026",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 28, 0),
                BackColor = Color.White
            };

            lblPaymentMethod = new Label
            {
                Text = "Payment method: Visa ending in 4242",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty,
                BackColor = Color.White
            };

            detailsRow.Controls.Add(lblPlanPrice);
            detailsRow.Controls.Add(lblRenewalDate);
            detailsRow.Controls.Add(lblPaymentMethod);

            // Divider Line Container
            var dividerContainer = new Panel
            {
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.White,
                Padding = new Padding(0, 10, 0, 10)
            };
            var divider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(242, 238, 234)
            };
            dividerContainer.Controls.Add(divider);

            // Seat Usage Progress Bar Section
            var seatSectionPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.White
            };

            var seatHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 20,
                BackColor = Color.White
            };

            var lblSeatLabel = new Label
            {
                Text = "Seat Usage",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Left,
                AutoSize = true,
                BackColor = Color.White
            };

            lblSeatCount = new Label
            {
                Text = "5 / 10 seats used",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true,
                BackColor = Color.White
            };

            seatHeader.Controls.Add(lblSeatLabel);
            seatHeader.Controls.Add(lblSeatCount);

            seatProgressBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 8,
                BackColor = Color.FromArgb(240, 235, 230),
                Margin = new Padding(0, 8, 0, 0)
            };
            seatProgressBar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int max = currentSub?.MaxSeats > 0 ? currentSub.MaxSeats : 10;
                int used = currentSub?.UsedSeats ?? 5;
                float pct = Math.Min(1f, (float)used / max);
                int fillWidth = (int)(seatProgressBar.Width * pct);

                if (fillWidth > 0)
                {
                    using var brush = new SolidBrush(UITheme.PrimaryMauve);
                    e.Graphics.FillRoundedRectangle(brush, new Rectangle(0, 0, fillWidth, seatProgressBar.Height), 4);
                }
            };
            seatProgressBar.ApplyRoundedRegion(4);

            // Add progress bar first, then header so header docks at top, progress bar below
            seatSectionPanel.Controls.Add(seatProgressBar);
            seatSectionPanel.Controls.Add(seatHeader);

            // Add in reverse docking order so headerRow is at top, detailsRow below, divider, then seatSectionPanel
            currentPlanCard.Controls.Add(seatSectionPanel);
            currentPlanCard.Controls.Add(dividerContainer);
            currentPlanCard.Controls.Add(detailsRow);
            currentPlanCard.Controls.Add(headerRow);
        }

        // =========================================================
        // 4. "WHAT'S INCLUDED IN YOUR PLAN" SECTION
        // =========================================================
        private void BuildFeaturesCard()
        {
            featuresCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 228,
                BackColor = Color.White,
                Padding = new Padding(32, 24, 32, 24),
                Margin = Padding.Empty
            };

            featuresCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, featuresCard.Width - 1, featuresCard.Height - 1), 12);
            };
            featuresCard.ApplyRoundedRegion(12);

            var lblSectionTitle = new Label
            {
                Text = "What's included in your plan",
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font(UITheme.FontSerif, 14F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var gridContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(0, 10, 0, 0)
            };

            // 2-Column checklist table (zero border lines)
            var gridTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                BackColor = Color.White,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            gridTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            gridTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            string[] col1Features = new[]
            {
                "Up to 10 team members",
                "Advanced custom cake order management",
                "Full CRM & customer history",
                "Daily, monthly & annual financial reports"
            };

            string[] col2Features = new[]
            {
                "Priority email & phone support",
                "Automated follow-up reminders",
                "Export transactions to CSV",
                "Multi-device synchronization"
            };

            for (int r = 0; r < 4; r++)
            {
                gridTable.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));
                gridTable.Controls.Add(CreateCheckItem(col1Features[r], r == 0, false), 0, r);
                gridTable.Controls.Add(CreateCheckItem(col2Features[r], false, r == 3), 1, r);
            }

            gridContainer.Controls.Add(gridTable);

            // Add container first, then title so title docks cleanly at top without overlapping
            featuresCard.Controls.Add(gridContainer);
            featuresCard.Controls.Add(lblSectionTitle);
        }

        private Panel CreateCheckItem(string text, bool isFirstSeatItem, bool isBranchingItem = false)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            var icon = new Label
            {
                Text = "\uE73E",
                Font = new Font("Segoe MDL2 Assets", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 133, 90),
                Location = new Point(0, 5),
                Size = new Size(22, 20),
                BackColor = Color.White
            };

            var lbl = new Label
            {
                Text = text,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                Location = new Point(26, 4),
                AutoSize = true,
                BackColor = Color.White,
                UseMnemonic = false
            };

            if (isFirstSeatItem)
            {
                lblFeatureSeats = lbl;
            }
            if (isBranchingItem)
            {
                lblFeatureBranching = lbl;
            }

            pnl.Controls.Add(icon);
            pnl.Controls.Add(lbl);
            return pnl;
        }

        // =========================================================
        // 5. BILLING HISTORY SECTION
        // =========================================================
        private void BuildBillingHistoryCard()
        {
            billingHistoryCard = new Panel
            {
                Dock = DockStyle.Top,
                Height = 356,
                BackColor = Color.White,
                Padding = new Padding(32, 24, 32, 24),
                Margin = Padding.Empty
            };

            billingHistoryCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, billingHistoryCard.Width - 1, billingHistoryCard.Height - 1), 12);
            };
            billingHistoryCard.ApplyRoundedRegion(12);

            var lblSectionTitle = new Label
            {
                Text = "Billing History",
                Dock = DockStyle.Top,
                Height = 36,
                Font = new Font(UITheme.FontSerif, 14F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                BackColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };

            gridBillingHistory = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(242, 238, 234),
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                RowTemplate = { Height = 48 },
                ColumnHeadersHeight = 36,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
            };

            gridBillingHistory.AdvancedColumnHeadersBorderStyle.All = DataGridViewAdvancedCellBorderStyle.None;
            gridBillingHistory.AdvancedColumnHeadersBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.Single;

            gridBillingHistory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 248, 246);
            gridBillingHistory.DefaultCellStyle.SelectionForeColor = UITheme.TextDark;

            gridBillingHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridBillingHistory.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(120, 110, 105);
            gridBillingHistory.ColumnHeadersDefaultCellStyle.Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
            gridBillingHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            gridBillingHistory.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(120, 110, 105);
            gridBillingHistory.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);

            var colDate = new DataGridViewTextBoxColumn
            {
                Name = "colDate",
                HeaderText = "DATE",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 20F
            };

            var colDesc = new DataGridViewTextBoxColumn
            {
                Name = "colDesc",
                HeaderText = "DESCRIPTION",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 40F
            };

            var colAmount = new DataGridViewTextBoxColumn
            {
                Name = "colAmount",
                HeaderText = "AMOUNT",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 16F
            };

            var colStatus = new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "STATUS",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 12F
            };

            var colInvoice = new DataGridViewTextBoxColumn
            {
                Name = "colInvoice",
                HeaderText = "INVOICE",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 12F
            };

            gridBillingHistory.Columns.AddRange(colDate, colDesc, colAmount, colStatus, colInvoice);

            gridBillingHistory.CellPainting += GridBillingHistory_CellPainting;
            gridBillingHistory.CellClick += GridBillingHistory_CellClick;

            pagination = new PaginationControl
            {
                Dock = DockStyle.Bottom
            };
            pagination.PageChanged += async (newPage) =>
            {
                currentPage = newPage;
                await RefreshDataAsync();
            };

            // Add grid first (Fill), then pagination (Bottom), then title (Top) so title docks cleanly at top
            billingHistoryCard.Controls.Add(gridBillingHistory);
            billingHistoryCard.Controls.Add(pagination);
            billingHistoryCard.Controls.Add(lblSectionTitle);
        }

        private void GridBillingHistory_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var item = gridBillingHistory.Rows[e.RowIndex].Tag as BillingHistoryItem;
            if (item == null) return;

            int colIdxDate = gridBillingHistory.Columns["colDate"]?.Index ?? -1;
            int colIdxDesc = gridBillingHistory.Columns["colDesc"]?.Index ?? -1;
            int colIdxAmount = gridBillingHistory.Columns["colAmount"]?.Index ?? -1;
            int colIdxStatus = gridBillingHistory.Columns["colStatus"]?.Index ?? -1;
            int colIdxInvoice = gridBillingHistory.Columns["colInvoice"]?.Index ?? -1;

            // Date
            if (colIdxDate >= 0 && e.ColumnIndex == colIdxDate)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                TextRenderer.DrawText(g, item.Date.ToString("MMM d, yyyy"), font, rect, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            // Description
            else if (colIdxDesc >= 0 && e.ColumnIndex == colIdxDesc)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                TextRenderer.DrawText(g, item.Description, font, rect, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            // Amount
            else if (colIdxAmount >= 0 && e.ColumnIndex == colIdxAmount)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                TextRenderer.DrawText(g, $"₱{item.Amount:N2}", font, rect, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            // Status (Green "Paid" pill)
            else if (colIdxStatus >= 0 && e.ColumnIndex == colIdxStatus)
            {
                using var font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
                int pillWidth = 52;
                int pillHeight = 22;
                int pillX = e.CellBounds.Left + 12;
                int pillY = e.CellBounds.Top + (e.CellBounds.Height - pillHeight) / 2;

                var pillRect = new Rectangle(pillX, pillY, pillWidth, pillHeight);
                using (var brush = new SolidBrush(Color.FromArgb(235, 247, 238)))
                {
                    g.FillRoundedRectangle(brush, pillRect, 6);
                }

                TextRenderer.DrawText(g, item.Status, font, pillRect, Color.FromArgb(46, 133, 90),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            // Invoice ("Download" link button)
            else if (colIdxInvoice >= 0 && e.ColumnIndex == colIdxInvoice)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                var rect = new Rectangle(e.CellBounds.Left + 12, e.CellBounds.Top, e.CellBounds.Width - 14, e.CellBounds.Height);
                TextRenderer.DrawText(g, "\uE896 Download", font, rect, UITheme.PrimaryMauve, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
        }

        private void GridBillingHistory_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (gridBillingHistory.Columns["colInvoice"] is { } colInv && e.ColumnIndex == colInv.Index)
            {
                var item = gridBillingHistory.Rows[e.RowIndex].Tag as BillingHistoryItem;
                if (item != null)
                {
                    DownloadInvoice(item);
                }
            }
        }

        private void DownloadInvoice(BillingHistoryItem item)
        {
            using var sfd = new SaveFileDialog
            {
                Filter = "Text Invoice (*.txt)|*.txt|All Files (*.*)|*.*",
                FileName = $"Invoice_{item.Date:yyyyMMdd}_{item.Description.Replace(" ", "_")}.txt",
                Title = "Save Subscription Invoice"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var sb = new StringBuilder();
                sb.AppendLine("=================================================");
                sb.AppendLine("         SWEET STORY CUSTOM CAKE CRM             ");
                sb.AppendLine("            OFFICIAL BILLING INVOICE             ");
                sb.AppendLine("=================================================");
                sb.AppendLine($"Invoice Date:    {item.Date:MMMM dd, yyyy}");
                sb.AppendLine($"Description:     {item.Description}");
                sb.AppendLine($"Amount Paid:     PHP {item.Amount:N2}");
                sb.AppendLine($"Status:          {item.Status}");
                sb.AppendLine($"Payment Method:  Visa ending in 4242");
                sb.AppendLine("=================================================");
                sb.AppendLine("Thank you for choosing Sweet Story Cake CRM!");

                File.WriteAllText(sfd.FileName, sb.ToString());
                MessageBox.Show($"Invoice downloaded successfully to:\n{sfd.FileName}", "Invoice Downloaded", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // =========================================================
        // 6. ACTION HANDLERS (Renew, Upgrade/Downgrade)
        // =========================================================
        private async Task PromptRenewSubscriptionAsync()
        {
            var res = MessageBox.Show(
                $"Renew your {currentSub?.PlanName ?? "Pro Plan"} subscription for 1 additional year?\n\nAmount: ₱{currentSub?.Price ?? 9599m:N2}\nBilled to: {currentSub?.PaymentMethod ?? "Visa ending in 4242"}",
                "Confirm Subscription Renewal",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                await CrmDataService.RenewSubscriptionAsync();
                MessageBox.Show("Subscription renewed successfully! Renewal date updated by 1 year.", "Renewal Confirmed", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await RefreshDataAsync();
            }
        }

        private async Task PromptUpgradeDowngradeAsync()
        {
            var plans = await CrmDataService.GetSubscriptionPlansAsync();
            if (plans.Count == 0)
            {
                MessageBox.Show("No subscription plans are currently available.", "Plans", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var currentPlan = currentSub?.PlanName ?? "Pro Plan";
            var form = new Form
            {
                Text = "Upgrade / Downgrade Plan",
                Size = new Size(460, 290),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = Color.White
            };

            var lbl = new Label
            {
                Text = "Select your desired subscription plan:",
                Font = new Font(UITheme.FontSans, 10F, FontStyle.Bold),
                Location = new Point(24, 20),
                AutoSize = true
            };

            var cmbPlans = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 10F),
                Location = new Point(24, 55),
                Width = 390
            };

            int selectedIdx = 0;
            for (int i = 0; i < plans.Count; i++)
            {
                var p = plans[i];
                string branchDesc = p.AllowBranching ? $" · Max {p.MaxBranches} Branches" : " · Single Location";
                cmbPlans.Items.Add($"{p.PlanName} (₱{p.Price:N2} · {p.MaxUsers} Seats{branchDesc} · {p.DurationDays} Days)");
                if (p.PlanName.Equals(currentPlan, StringComparison.OrdinalIgnoreCase))
                {
                    selectedIdx = i;
                }
            }
            cmbPlans.SelectedIndex = selectedIdx;

            var btnConfirm = new Button
            {
                Text = "Apply Plan Change",
                Location = new Point(230, 170),
                Size = new Size(184, 40),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold)
            };
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.ApplyRoundedRegion(8);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(120, 170),
                Size = new Size(100, 40),
                BackColor = Color.FromArgb(239, 231, 223),
                ForeColor = UITheme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.ApplyRoundedRegion(8);
            btnCancel.Click += (s, e) => form.Close();

            btnConfirm.Click += async (s, e) =>
            {
                if (cmbPlans.SelectedIndex >= 0 && cmbPlans.SelectedIndex < plans.Count)
                {
                    var chosen = plans[cmbPlans.SelectedIndex];
                    await CrmDataService.UpgradeDowngradePlanAsync(chosen.PlanId);
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                }
            };

            form.Controls.Add(lbl);
            form.Controls.Add(cmbPlans);
            form.Controls.Add(btnCancel);
            form.Controls.Add(btnConfirm);

            if (form.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
            {
                MessageBox.Show("Subscription plan updated successfully!", "Plan Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await RefreshDataAsync();
            }
        }

        // =========================================================
        // 7. DATA REFRESH & DATABASE SYNC
        // =========================================================
        public async Task RefreshDataAsync()
        {
            try
            {
                currentSub = await CrmDataService.GetSubscriptionInfoAsync();
                var paged = await CrmDataService.GetBillingHistoryPagedAsync(currentPage, PageSize);
                if (paged.TotalPages > 0 && currentPage > paged.TotalPages)
                {
                    currentPage = paged.TotalPages;
                    paged = await CrmDataService.GetBillingHistoryPagedAsync(currentPage, PageSize);
                }

                billingItems = paged.Items;

                // Update Current Plan Card
                lblPlanName.Text = currentSub.PlanName;
                lblPlanPrice.Text = $"₱{currentSub.Price:N0} / {currentSub.BillingCycle}  \u00B7  Billed annually";
                lblRenewalDate.Text = $"Next billing: {currentSub.RenewalDate:MMMM d, yyyy}";
                lblPaymentMethod.Text = $"Payment method: {currentSub.PaymentMethod}";

                lblSeatCount.Text = $"{currentSub.UsedSeats} / {currentSub.MaxSeats} seats used";
                seatProgressBar.Invalidate();

                if (lblFeatureSeats != null)
                {
                    lblFeatureSeats.Text = $"Up to {currentSub.MaxSeats} team members";
                }

                if (lblFeatureBranching != null)
                {
                    lblFeatureBranching.Text = currentSub.AllowBranching
                        ? $"Multi-branch: Up to {currentSub.MaxBranches} locations"
                        : "Single store location (branching not included)";
                }

                // Update Billing History Grid
                gridBillingHistory.Rows.Clear();
                foreach (var item in billingItems)
                {
                    int rowIdx = gridBillingHistory.Rows.Add();
                    gridBillingHistory.Rows[rowIdx].Tag = item;
                }

                pagination.SetPagination(paged.Page, paged.PageSize, paged.TotalCount, "invoices");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SubscriptionForm.RefreshDataAsync] {ex.Message}");
            }
        }
    }
}
