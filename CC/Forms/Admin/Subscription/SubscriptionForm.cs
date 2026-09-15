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
    public class SubscriptionForm : Form
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

        private Button btnRenew = null!;
        private Button btnUpgradeDowngrade = null!;

        private Panel featuresCard = null!;
        private Panel billingHistoryCard = null!;
        private DataGridView gridBillingHistory = null!;

        private SubscriptionInfo currentSub = null!;
        private List<BillingHistoryItem> billingItems = new List<BillingHistoryItem>();

        public SubscriptionForm()
        {
            InitializeComponent();
            BuildTopToolbar();
            BuildMainContent();

            Controls.Add(mainContentPanel);
            Controls.Add(topPanel);

            Load += async (s, e) => await RefreshDataAsync();
            VisibleChanged += async (s, e) =>
            {
                if (Visible) await RefreshDataAsync();
            };
        }

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
                Padding = new Padding(0, 0, 12, 20)
            };

            BuildCurrentPlanCard();
            BuildFeaturesCard();
            BuildBillingHistoryCard();

            mainContentPanel.Controls.Add(billingHistoryCard);
            mainContentPanel.Controls.Add(featuresCard);
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
                Height = 225,
                BackColor = Color.White,
                Padding = new Padding(24, 20, 24, 20),
                Margin = new Padding(0, 0, 0, 20)
            };

            currentPlanCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, currentPlanCard.Width - 1, currentPlanCard.Height - 1), 14);
            };
            currentPlanCard.ApplyRoundedRegion(14);

            // Header Row: Left plan name + pill, Right action buttons
            var headerRow = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 65,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            headerRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var planTitleStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var planTitleColumn = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var lblBadgeHeader = new Label
            {
                Text = "CURRENT PLAN",
                Font = new Font(UITheme.FontSans, 7.5F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 3)
            };

            var namePillRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            lblPlanName = new Label
            {
                Text = "Pro Plan",
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 10, 0)
            };

            var activePill = new Panel
            {
                Size = new Size(72, 24),
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
            planTitleStack.Controls.Add(planTitleColumn);

            // Right action buttons: Renew Subscription & Upgrade / Downgrade
            var planBtnStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 4, 0, 0)
            };

            btnUpgradeDowngrade = new Button
            {
                Text = "Upgrade / Downgrade",
                Height = 38,
                Width = 170,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnUpgradeDowngrade.FlatAppearance.BorderSize = 0;
            btnUpgradeDowngrade.ApplyRoundedRegion(10);
            btnUpgradeDowngrade.Click += async (s, e) => await PromptUpgradeDowngradeAsync();

            btnRenew = new Button
            {
                Text = "Renew Subscription",
                Height = 38,
                Width = 155,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = UITheme.TextDark,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnRenew.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnRenew.ApplyRoundedRegion(10);
            btnRenew.Click += async (s, e) => await PromptRenewSubscriptionAsync();

            planBtnStack.Controls.Add(btnUpgradeDowngrade);
            planBtnStack.Controls.Add(btnRenew);

            headerRow.Controls.Add(planTitleStack, 0, 0);
            headerRow.Controls.Add(planBtnStack, 1, 0);

            // Details Row: Price, Renewal Date, Payment Method
            var detailsRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 44,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 0, 0)
            };

            lblPlanPrice = new Label
            {
                Text = "₱9,599 / year  \u00B7  Billed annually",
                Font = new Font(UITheme.FontSans, 10F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 24, 0)
            };

            lblRenewalDate = new Label
            {
                Text = "Next billing: August 15, 2026",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 24, 0)
            };

            lblPaymentMethod = new Label
            {
                Text = "Payment method: Visa ending in 4242",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            detailsRow.Controls.Add(lblPlanPrice);
            detailsRow.Controls.Add(lblRenewalDate);
            detailsRow.Controls.Add(lblPaymentMethod);

            // Divider Line
            var divider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(242, 238, 234),
                Margin = new Padding(0, 8, 0, 12)
            };

            // Seat Usage Progress Bar Row
            var seatPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0)
            };

            var seatHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 22,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            seatHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            seatHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var lblSeatLabel = new Label
            {
                Text = "Seat Usage",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true
            };

            lblSeatCount = new Label
            {
                Text = "5 / 10 seats used",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = true
            };

            seatHeader.Controls.Add(lblSeatLabel, 0, 0);
            seatHeader.Controls.Add(lblSeatCount, 1, 0);
            seatPanel.Controls.Add(seatHeader);

            seatProgressBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 10,
                BackColor = Color.FromArgb(240, 235, 230),
                Margin = new Padding(0, 6, 0, 0)
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
                    e.Graphics.FillRoundedRectangle(brush, new Rectangle(0, 0, fillWidth, seatProgressBar.Height), 5);
                }
            };
            seatProgressBar.ApplyRoundedRegion(5);
            seatPanel.Controls.Add(seatProgressBar);

            // Add in reverse order so headerRow is docked at top, then detailsRow, divider, seatPanel
            currentPlanCard.Controls.Add(seatPanel);
            currentPlanCard.Controls.Add(divider);
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
                Height = 190,
                BackColor = Color.White,
                Padding = new Padding(24, 20, 24, 20),
                Margin = new Padding(0, 16, 0, 16)
            };

            featuresCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, featuresCard.Width - 1, featuresCard.Height - 1), 14);
            };
            featuresCard.ApplyRoundedRegion(14);

            var lblSectionTitle = new Label
            {
                Text = "What's included in your plan",
                Dock = DockStyle.Top,
                Height = 28,
                Font = new Font(UITheme.FontSerif, 14F, FontStyle.Bold),
                ForeColor = UITheme.TextDark
            };
            featuresCard.Controls.Add(lblSectionTitle);

            // 2-Column checklist table
            var gridTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 10, 0, 0)
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
                gridTable.Controls.Add(CreateCheckItem(col1Features[r]), 0, r);
                gridTable.Controls.Add(CreateCheckItem(col2Features[r]), 1, r);
            }

            featuresCard.Controls.Add(gridTable);
        }

        private Panel CreateCheckItem(string text)
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var icon = new Label
            {
                Text = "\uE73E",
                Font = new Font("Segoe MDL2 Assets", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 133, 90),
                Location = new Point(0, 3),
                Size = new Size(20, 20)
            };

            var lbl = new Label
            {
                Text = text,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                Location = new Point(24, 2),
                AutoSize = true,
                UseMnemonic = false
            };

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
                Height = 280,
                BackColor = Color.White,
                Padding = new Padding(24, 20, 24, 16),
                Margin = new Padding(0, 16, 0, 20)
            };

            billingHistoryCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, billingHistoryCard.Width - 1, billingHistoryCard.Height - 1), 14);
            };
            billingHistoryCard.ApplyRoundedRegion(14);

            var lblSectionTitle = new Label
            {
                Text = "Billing History",
                Dock = DockStyle.Top,
                Height = 32,
                Font = new Font(UITheme.FontSerif, 14F, FontStyle.Bold),
                ForeColor = UITheme.TextDark
            };
            billingHistoryCard.Controls.Add(lblSectionTitle);

            gridBillingHistory = new DataGridView
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
                ColumnHeadersHeight = 38,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };

            gridBillingHistory.DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 248, 246);
            gridBillingHistory.DefaultCellStyle.SelectionForeColor = UITheme.TextDark;

            gridBillingHistory.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridBillingHistory.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.TextMuted;
            gridBillingHistory.ColumnHeadersDefaultCellStyle.Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
            gridBillingHistory.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            gridBillingHistory.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);

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

            billingHistoryCard.Controls.Add(gridBillingHistory);
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
            var currentPlan = currentSub?.PlanName ?? "Pro Plan";
            var form = new Form
            {
                Text = "Upgrade / Downgrade Plan",
                Size = new Size(420, 280),
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
                Width = 350
            };
            cmbPlans.Items.AddRange(new object[]
            {
                "Starter Plan (₱4,399/yr · 3 Seats)",
                "Pro Plan (₱9,599/yr · 10 Seats)",
                "Enterprise Plan (₱19,999/yr · 50 Seats)"
            });

            if (currentPlan.Contains("Starter", StringComparison.OrdinalIgnoreCase)) cmbPlans.SelectedIndex = 0;
            else if (currentPlan.Contains("Enterprise", StringComparison.OrdinalIgnoreCase)) cmbPlans.SelectedIndex = 2;
            else cmbPlans.SelectedIndex = 1;

            var btnConfirm = new Button
            {
                Text = "Apply Plan Change",
                Location = new Point(190, 160),
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
                Location = new Point(80, 160),
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
                string targetPlan = cmbPlans.SelectedIndex switch
                {
                    0 => "Starter",
                    2 => "Enterprise",
                    _ => "Pro"
                };

                await CrmDataService.UpgradeDowngradePlanAsync(targetPlan);
                form.DialogResult = DialogResult.OK;
                form.Close();
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
                billingItems = await CrmDataService.GetBillingHistoryAsync();

                // Update Current Plan Card
                lblPlanName.Text = currentSub.PlanName;
                lblPlanPrice.Text = $"₱{currentSub.Price:N0} / {currentSub.BillingCycle}  \u00B7  Billed annually";
                lblRenewalDate.Text = $"Next billing: {currentSub.RenewalDate:MMMM d, yyyy}";
                lblPaymentMethod.Text = $"Payment method: {currentSub.PaymentMethod}";

                lblSeatCount.Text = $"{currentSub.UsedSeats} / {currentSub.MaxSeats} seats used";
                seatProgressBar.Invalidate();

                // Update Billing History Grid
                gridBillingHistory.Rows.Clear();
                foreach (var item in billingItems)
                {
                    int rowIdx = gridBillingHistory.Rows.Add();
                    gridBillingHistory.Rows[rowIdx].Tag = item;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SubscriptionForm.RefreshDataAsync] {ex.Message}");
            }
        }
    }
}
