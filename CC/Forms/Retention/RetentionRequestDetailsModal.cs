using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Retention
{
    public class RetentionRequestDetailsModal : Form
    {
        private readonly int requestId;
        private RetentionRequest? request;

        private Panel headerPanel = null!;
        private Panel scrollableBodyPanel = null!;
        private Panel footerPanel = null!;

        private Label lblHeaderTitle = null!;
        private Label lblStatusBadge = null!;
        private FlowLayoutPanel contentFlow = null!;

        // Action controls
        private Panel actionPanel = null!;
        private Button btnApprove = null!;
        private Button btnReject = null!;
        private Button btnClose = null!;
        private Label lblActionNotice = null!;

        // Inline rejection input panel
        private Panel pnlRejectionInput = null!;
        private TextBox txtRejectionReason = null!;
        private TextBox txtRejectionRemarks = null!;
        private Button btnConfirmRejection = null!;
        private Button btnCancelRejection = null!;

        public RetentionRequestDetailsModal(RetentionRequest request)
        {
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.requestId = request.RequestId;
            InitializeComponent();
            BuildRegions();
            RenderDetails();
            ConfigureActionButtons();
        }

        public RetentionRequestDetailsModal(int requestId)
        {
            this.requestId = requestId;
            InitializeComponent();
            BuildRegions();
            _ = LoadDataAsync();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Text = "Retention Request Details";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Size = new Size(700, 760);
            BackColor = Color.White;
            ShowInTaskbar = false;
            DoubleBuffered = true;

            Load += (s, e) => this.CenterOnParentOrScreen();
            ResumeLayout(false);
        }

        private void BuildRegions()
        {
            Controls.Clear();

            // 1. FIXED HEADER
            headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.White,
                Padding = new Padding(24, 16, 24, 16)
            };

            lblHeaderTitle = new Label
            {
                Text = request != null 
                    ? $"Retention Request #{request.RequestId:D4} \u00B7 {request.CustomerName}"
                    : $"Retention Request Details #{requestId:D4}",
                Font = new Font(UITheme.FontSans, 13F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Location = new Point(24, 20)
            };

            lblStatusBadge = new Label
            {
                Text = "PENDING",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                Height = 26,
                Width = 95,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(headerPanel.Width - 160, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = UITheme.StatusYellowBg,
                ForeColor = UITheme.StatusYellowFg
            };

            var btnCloseX = new Button
            {
                Text = "\uE711",
                Font = new Font("Segoe MDL2 Assets", 10F),
                ForeColor = UITheme.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(32, 32),
                Location = new Point(headerPanel.Width - 48, 18),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnCloseX.FlatAppearance.BorderSize = 0;
            btnCloseX.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = UITheme.BorderColor
            };

            headerPanel.Controls.Add(lblHeaderTitle);
            headerPanel.Controls.Add(lblStatusBadge);
            headerPanel.Controls.Add(btnCloseX);
            headerPanel.Controls.Add(headerBorder);

            // 2. FIXED FOOTER
            footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 72,
                BackColor = Color.FromArgb(250, 248, 246),
                Padding = new Padding(24, 14, 24, 14)
            };

            var footerBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = UITheme.BorderColor
            };
            footerPanel.Controls.Add(footerBorder);

            actionPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            lblActionNotice = new Label
            {
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Location = new Point(4, 16)
            };
            actionPanel.Controls.Add(lblActionNotice);

            btnReject = new Button
            {
                Text = "Reject Request",
                Height = 38,
                Width = 120,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.StatusRedFg,
                BackColor = Color.FromArgb(254, 242, 242),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(footerPanel.Width - 365, 16),
                Visible = false
            };
            btnReject.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnReject.Click += (s, e) => ShowRejectionPanel(true);
            actionPanel.Controls.Add(btnReject);

            btnApprove = new Button
            {
                Text = "Approve Request",
                Height = 38,
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(22, 163, 74),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(footerPanel.Width - 235, 16),
                Visible = false
            };
            btnApprove.FlatAppearance.BorderSize = 0;
            btnApprove.Click += async (s, e) => await ApproveRequestAsync();
            actionPanel.Controls.Add(btnApprove);

            btnClose = new Button
            {
                Text = "Close",
                Height = 38,
                Width = 85,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(footerPanel.Width - 88, 16)
            };
            btnClose.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnClose.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            actionPanel.Controls.Add(btnClose);

            footerPanel.Controls.Add(actionPanel);

            // 3. SCROLLABLE BODY
            scrollableBodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.FromArgb(253, 252, 251),
                Padding = new Padding(24, 16, 24, 20)
            };

            contentFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Width = 635,
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, 0, 20)
            };
            scrollableBodyPanel.Controls.Add(contentFlow);

            Controls.Add(scrollableBodyPanel);
            Controls.Add(footerPanel);
            Controls.Add(headerPanel);
        }

        private async Task LoadDataAsync()
        {
            try
            {
                request = await CrmDataService.GetRetentionRequestByIdAsync(requestId);
                if (request == null)
                {
                    MessageBox.Show("Retention request not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Close();
                    return;
                }

                RenderDetails();
                ConfigureActionButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load retention request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RenderDetails()
        {
            if (request == null) return;

            contentFlow.SuspendLayout();
            contentFlow.Controls.Clear();
            lblHeaderTitle.Text = $"Retention Request #{request.RequestId:D4} \u00B7 {request.CustomerName}";

            // Update status badge
            lblStatusBadge.Text = request.Status.ToUpperInvariant();
            switch (request.Status)
            {
                case "Approved":
                    lblStatusBadge.BackColor = UITheme.StatusGreenBg;
                    lblStatusBadge.ForeColor = UITheme.StatusGreenFg;
                    break;
                case "Rejected":
                    lblStatusBadge.BackColor = UITheme.StatusRedBg;
                    lblStatusBadge.ForeColor = UITheme.StatusRedFg;
                    break;
                default:
                    lblStatusBadge.BackColor = UITheme.StatusYellowBg;
                    lblStatusBadge.ForeColor = UITheme.StatusYellowFg;
                    break;
            }

            // --- SECTION 1: CUSTOMER & RETENTION PROPOSAL ---
            var sec1 = CreateSectionCard("CUSTOMER & RETENTION PROPOSAL");
            sec1.Controls.Add(CreateDataRow("Customer / Client:", $"{request.CustomerName} ({request.CustomerEmail})"));
            sec1.Controls.Add(CreateDataRow("Target Segment:", request.TargetSegment));
            sec1.Controls.Add(CreateDataRow("Action Type:", request.ActionType));
            sec1.Controls.Add(CreateDataRow("Proposed Discount:", request.DiscountPercent > 0 ? $"{request.DiscountPercent:0.#}%" : "Standard (No discount)"));
            sec1.Controls.Add(CreateDataRow("Retention Details:", string.IsNullOrWhiteSpace(request.RetentionDetails) ? "None specified" : request.RetentionDetails));
            contentFlow.Controls.Add(sec1);

            // --- SECTION 2: REASON FOR RETENTION ---
            var sec2 = CreateSectionCard("REASON FOR RETENTION");
            sec2.Controls.Add(CreateDataRow("Reason Category:", request.ReasonCategory));
            string detailsText = !string.IsNullOrWhiteSpace(request.ReasonCustomDetails)
                ? request.ReasonCustomDetails
                : (!string.IsNullOrWhiteSpace(request.RetentionDetails) ? request.RetentionDetails : "Proactive customer appreciation and retention incentive");
            sec2.Controls.Add(CreateDataRow("Specific Details / Note:", detailsText));
            contentFlow.Controls.Add(sec2);

            // --- SECTION 3: APPROVAL HISTORY & AUDIT TRAIL ---
            var sec3 = CreateSectionCard("APPROVAL HISTORY (AUDIT TIMELINE)");
            PopulateApprovalTimeline(sec3);
            contentFlow.Controls.Add(sec3);

            // --- SECTION 4: AUTOMATIC EMAIL CAMPAIGN (Requirements 4, 5, 6) ---
            if (request.Status == "Approved")
            {
                if (string.IsNullOrWhiteSpace(request.GeneratedCampaignSubject) || string.IsNullOrWhiteSpace(request.GeneratedCampaignBody))
                {
                    var (genSubject, genBody) = CrmDataService.GenerateFormattedRetentionEmail(request);
                    request.GeneratedCampaignSubject = genSubject;
                    request.GeneratedCampaignBody = genBody;
                    request.AddedToCampaign = true;
                    request.AddedToCampaignDate ??= request.ReviewedDate ?? DateTime.UtcNow;
                }

                var sec4 = CreateSectionCard("AUTOMATICALLY GENERATED EMAIL CAMPAIGN");

                // Success Banner
                var pnlSuccessBanner = new Panel
                {
                    Width = 600,
                    Height = 42,
                    BackColor = Color.FromArgb(240, 253, 244),
                    Margin = new Padding(0, 0, 0, 10),
                    Padding = new Padding(12, 10, 12, 10)
                };
                var lblSuccess = new Label
                {
                    Text = $"\u2714 Automatically Added to Email Campaigns on {request.AddedToCampaignDate?.ToLocalTime().ToString("MMM dd, yyyy h:mm tt") ?? DateTime.Now.ToString("MMM dd, yyyy")}",
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(22, 163, 74),
                    Dock = DockStyle.Fill
                };
                pnlSuccessBanner.Controls.Add(lblSuccess);
                sec4.Controls.Add(pnlSuccessBanner);

                sec4.Controls.Add(CreateDataRow("Campaign Status:", "Ready to Send (Personalized retention offer)"));
                sec4.Controls.Add(CreateDataRow("Email Subject:", request.GeneratedCampaignSubject ?? "Special Offer from Sweet Story"));

                var lblBodyLabel = new Label
                {
                    Text = "Formatted Email Body (Ready for Dispatch):",
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                    ForeColor = UITheme.TextMuted,
                    Width = 600,
                    Margin = new Padding(0, 8, 0, 4)
                };
                sec4.Controls.Add(lblBodyLabel);

                var txtBodyPreview = new TextBox
                {
                    Width = 600,
                    Height = 180,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    Font = new Font("Segoe UI", 9.5F),
                    BackColor = Color.FromArgb(250, 248, 246),
                    ForeColor = UITheme.TextDark,
                    BorderStyle = BorderStyle.FixedSingle,
                    Margin = new Padding(0, 0, 0, 10),
                    Text = request.GeneratedCampaignBody ?? string.Empty
                };
                sec4.Controls.Add(txtBodyPreview);

                var btnSendNow = new Button
                {
                    Text = "\u2709 Dispatch Retention Email Now",
                    Height = 36,
                    Width = 240,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                    ForeColor = Color.White,
                    BackColor = UITheme.PrimaryMauve,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, 0, 6)
                };
                btnSendNow.FlatAppearance.BorderSize = 0;
                btnSendNow.Click += async (s, e) =>
                {
                    btnSendNow.Enabled = false;
                    btnSendNow.Text = "Sending...";
                    try
                    {
                        await CrmDataService.SendManualRetentionEmailAsync(
                            customerId: request.CustomerId,
                            segmentName: request.TargetSegment,
                            forceIgnoreCooldown: true,
                            overrideSubject: request.GeneratedCampaignSubject,
                            overrideBody: request.GeneratedCampaignBody
                        );
                        MessageBox.Show($"Retention email successfully dispatched to {request.CustomerName} ({request.CustomerEmail})!", "Email Sent", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        btnSendNow.Text = "\u2714 Email Dispatched";
                        btnSendNow.BackColor = Color.FromArgb(22, 163, 74);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to send email: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        btnSendNow.Enabled = true;
                        btnSendNow.Text = "\u2709 Dispatch Retention Email Now";
                    }
                };
                sec4.Controls.Add(btnSendNow);

                var lblNotice = new Label
                {
                    Text = "\u2139 This email campaign entry is also active under the Email Campaigns tab where it can be reviewed and dispatched at any time.",
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Italic),
                    ForeColor = UITheme.TextMuted,
                    Width = 600,
                    AutoSize = true,
                    Margin = new Padding(0, 4, 0, 4)
                };
                sec4.Controls.Add(lblNotice);

                contentFlow.Controls.Add(sec4);
            }

            // Inline Rejection Form Panel
            pnlRejectionInput = BuildRejectionPanel();
            pnlRejectionInput.Visible = false;
            contentFlow.Controls.Add(pnlRejectionInput);

            contentFlow.ResumeLayout(true);
        }

        private void PopulateApprovalTimeline(FlowLayoutPanel sec3)
        {
            if (request == null) return;

            // Step 1: Request Submitted
            var step1 = CreateTimelineCard(
                title: "1. Retention Request Submitted",
                subtitle: $"Requested by: {request.RequestedByUserName} (Manager)",
                timestamp: request.RequestedDate.ToLocalTime().ToString("MMM dd, yyyy h:mm tt"),
                badgeText: "SUBMITTED",
                badgeFg: UITheme.PrimaryMauve,
                badgeBg: Color.FromArgb(243, 240, 255),
                details: string.IsNullOrWhiteSpace(request.RetentionDetails) ? null : $"Proposal: {request.ActionType} ({(request.DiscountPercent > 0 ? $"{request.DiscountPercent:0.#}% discount" : "Standard")}) \u00B7 {request.RetentionDetails}"
            );
            sec3.Controls.Add(step1);

            // Step 2: Review (Approved / Rejected / Pending)
            if (request.ReviewedDate.HasValue)
            {
                bool isApproved = request.Status == "Approved";
                string reviewTitle = isApproved ? "2. Request Approved" : "2. Request Rejected";
                string reviewer = isApproved
                    ? (request.ReviewedByUserName ?? "Admin")
                    : (request.RejectedByUserName ?? request.ReviewedByUserName ?? "Admin");
                string reviewSub = $"{request.Status} by: {reviewer} (Business Admin)";
                string timeStr = (request.RejectionDate ?? request.ReviewedDate.Value).ToLocalTime().ToString("MMM dd, yyyy h:mm tt");
                string badge = isApproved ? "APPROVED" : "REJECTED";
                Color fg = isApproved ? UITheme.StatusGreenFg : UITheme.StatusRedFg;
                Color bg = isApproved ? UITheme.StatusGreenBg : UITheme.StatusRedBg;

                string? details = null;
                if (!isApproved)
                {
                    details = $"Reason for Rejection: {request.RejectionReason}";
                    if (!string.IsNullOrWhiteSpace(request.AdminRemarks))
                    {
                        details += $"\r\nRemarks: {request.AdminRemarks}";
                    }
                }
                else
                {
                    details = "Approved by Business Admin. Customer automatically added to Email Campaigns with formatted personalized offer.";
                    if (!string.IsNullOrWhiteSpace(request.AdminRemarks))
                    {
                        details += $"\r\nRemarks: {request.AdminRemarks}";
                    }
                }

                var step2 = CreateTimelineCard(
                    title: reviewTitle,
                    subtitle: reviewSub,
                    timestamp: timeStr,
                    badgeText: badge,
                    badgeFg: fg,
                    badgeBg: bg,
                    details: details
                );
                sec3.Controls.Add(step2);
            }
            else
            {
                var stepPending = CreateTimelineCard(
                    title: "2. Pending Admin Review & Decision",
                    subtitle: "Awaiting Business Admin approval or rejection.",
                    timestamp: "In Progress",
                    badgeText: "PENDING",
                    badgeFg: UITheme.StatusYellowFg,
                    badgeBg: UITheme.StatusYellowBg,
                    details: null
                );
                sec3.Controls.Add(stepPending);
            }
        }

        private TableLayoutPanel CreateTimelineCard(
            string title,
            string subtitle,
            string timestamp,
            string badgeText,
            Color badgeFg,
            Color badgeBg,
            string? details)
        {
            var card = new TableLayoutPanel
            {
                Width = 600,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = string.IsNullOrWhiteSpace(details) ? 2 : 3,
                BackColor = Color.FromArgb(249, 250, 251),
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(14, 10, 14, 12)
            };
            card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var topRow = new TableLayoutPanel
            {
                Width = 572,
                Height = 26,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                UseMnemonic = false
            };

            var badge = new Label
            {
                Text = badgeText,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                Height = 22,
                Width = 90,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = badgeBg,
                ForeColor = badgeFg,
                Dock = DockStyle.Right,
                UseMnemonic = false
            };

            topRow.Controls.Add(lblTitle, 0, 0);
            topRow.Controls.Add(badge, 1, 0);
            card.Controls.Add(topRow, 0, 0);

            var lblSub = new Label
            {
                Text = $"{subtitle}  \u00B7  {timestamp}",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 2),
                UseMnemonic = false
            };
            card.Controls.Add(lblSub, 0, 1);

            if (!string.IsNullOrWhiteSpace(details))
            {
                var lblDet = new Label
                {
                    Text = details,
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(70, 70, 70),
                    Dock = DockStyle.Fill,
                    AutoSize = true,
                    Margin = new Padding(0, 4, 0, 0),
                    UseMnemonic = false
                };
                card.Controls.Add(lblDet, 0, 2);
            }

            return card;
        }

        private Panel BuildRejectionPanel()
        {
            var pnl = new Panel
            {
                Width = 615,
                AutoSize = true,
                BackColor = Color.FromArgb(254, 242, 242),
                Margin = new Padding(0, 10, 0, 16),
                Padding = new Padding(16, 14, 16, 14)
            };

            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Width = 580,
                Dock = DockStyle.Top
            };

            var lblTitle = new Label
            {
                Text = "Reject Retention Request",
                Font = new Font(UITheme.FontSans, 11F, FontStyle.Bold),
                ForeColor = UITheme.StatusRedFg,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 6)
            };
            flow.Controls.Add(lblTitle);

            var lblReasonReq = new Label
            {
                Text = "Reason for Rejection * (Mandatory):",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.StatusRedFg,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            flow.Controls.Add(lblReasonReq);

            txtRejectionReason = new TextBox
            {
                Width = 575,
                Height = 52,
                Multiline = true,
                Font = new Font(UITheme.FontSans, 9F),
                ScrollBars = ScrollBars.Vertical,
                Margin = new Padding(0, 0, 0, 8)
            };
            flow.Controls.Add(txtRejectionReason);

            var lblRemarks = new Label
            {
                Text = "Additional Remarks / Notes (Optional):",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            flow.Controls.Add(lblRemarks);

            txtRejectionRemarks = new TextBox
            {
                Width = 575,
                Height = 44,
                Multiline = true,
                Font = new Font(UITheme.FontSans, 9F),
                ScrollBars = ScrollBars.Vertical,
                Margin = new Padding(0, 0, 0, 12)
            };
            flow.Controls.Add(txtRejectionRemarks);

            var btnRow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Width = 575
            };

            btnConfirmRejection = new Button
            {
                Text = "Confirm Rejection",
                Height = 36,
                Width = 145,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.StatusRedFg,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnConfirmRejection.FlatAppearance.BorderSize = 0;
            btnConfirmRejection.Click += async (s, e) => await ConfirmRejectionAsync();

            btnCancelRejection = new Button
            {
                Text = "Cancel",
                Height = 36,
                Width = 80,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F),
                ForeColor = UITheme.TextDark,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCancelRejection.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnCancelRejection.Click += (s, e) => ShowRejectionPanel(false);

            btnRow.Controls.Add(btnConfirmRejection);
            btnRow.Controls.Add(btnCancelRejection);
            flow.Controls.Add(btnRow);

            pnl.Controls.Add(flow);
            return pnl;
        }

        private void ShowRejectionPanel(bool show)
        {
            pnlRejectionInput.Visible = show;
            btnApprove.Enabled = !show;
            btnReject.Enabled = !show;
            if (show)
            {
                txtRejectionReason.Focus();
                scrollableBodyPanel.ScrollControlIntoView(pnlRejectionInput);
            }
        }

        private void ConfigureActionButtons()
        {
            if (request == null) return;

            string role = SessionService.CurrentUser?.Role ?? "";
            int currentUserId = SessionService.CurrentUser?.UserId ?? 0;
            bool isAdmin = role == "Business Admin" || role == "Admin" || role == "SuperAdmin";
            bool isRequester = request.RequestedByUserId == currentUserId && role != "SuperAdmin";

            if (request.Status != "Pending")
            {
                // Already approved or rejected
                btnApprove.Visible = false;
                btnReject.Visible = false;
                lblActionNotice.Text = $"This request has been reviewed ({request.Status}).";
                return;
            }

            // Pending request
            if (!isAdmin)
            {
                // Manager role
                btnApprove.Visible = false;
                btnReject.Visible = false;
                lblActionNotice.Text = "\u2139 Pending Business Admin review. Managers cannot approve retention requests.";
            }
            else if (isRequester)
            {
                // Admin who is also the requester: Cannot approve own request
                btnApprove.Visible = false;
                btnReject.Visible = false;
                lblActionNotice.Text = "\u26A0 You submitted this request. A different Business Admin must review and approve it.";
            }
            else
            {
                // Admin reviewing another user's request
                btnApprove.Visible = true;
                btnReject.Visible = true;
                lblActionNotice.Text = "Review proposal and choose an action:";
            }
        }

        private async Task ApproveRequestAsync()
        {
            if (request == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to approve this retention request for customer '{request.CustomerName}'?\r\n\r\nAction: {request.ActionType}\r\nDiscount: {request.DiscountPercent:0.#}%\r\n\r\nApproving will automatically generate and add a personalized email campaign to the Email Campaigns module.",
                "Confirm Retention Approval",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            btnApprove.Enabled = false;
            btnReject.Enabled = false;

            try
            {
                request = await CrmDataService.ApproveRetentionRequestAsync(
                    requestId: request.RequestId,
                    adminRemarks: "Approved by Business Admin"
                );

                MessageBox.Show(
                    $"Retention request #{request.RequestId} has been approved successfully!\r\n\r\nA personalized email campaign has been automatically formatted and added to the Email Campaigns module.",
                    "Approved & Campaign Created",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                RenderDetails();
                ConfigureActionButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to approve request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnApprove.Enabled = true;
                btnReject.Enabled = true;
            }
        }

        private async Task ConfirmRejectionAsync()
        {
            if (request == null) return;

            string reason = txtRejectionReason.Text.Trim();
            string remarks = txtRejectionRemarks.Text.Trim();

            if (string.IsNullOrWhiteSpace(reason))
            {
                MessageBox.Show("A reason for rejection is strictly mandatory.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRejectionReason.Focus();
                return;
            }

            btnConfirmRejection.Enabled = false;
            btnConfirmRejection.Text = "Rejecting...";

            try
            {
                request = await CrmDataService.RejectRetentionRequestAsync(
                    requestId: request.RequestId,
                    rejectionReason: reason,
                    adminRemarks: string.IsNullOrWhiteSpace(remarks) ? null : remarks
                );

                MessageBox.Show(
                    "Retention request has been rejected.",
                    "Rejected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                ShowRejectionPanel(false);
                RenderDetails();
                ConfigureActionButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to reject request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnConfirmRejection.Enabled = true;
                btnConfirmRejection.Text = "Confirm Rejection";
            }
        }

        private FlowLayoutPanel CreateSectionCard(string title)
        {
            var card = new FlowLayoutPanel
            {
                Width = 620,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(18, 14, 18, 16)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            };

            var header = new Panel
            {
                Width = 584,
                Height = 26,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 10)
            };

            var accentBar = new Panel
            {
                Width = 4,
                Height = 16,
                BackColor = UITheme.PrimaryMauve,
                Location = new Point(0, 3)
            };
            header.Controls.Add(accentBar);

            var lbl = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                AutoSize = true,
                Location = new Point(10, 2),
                UseMnemonic = false
            };
            header.Controls.Add(lbl);
            card.Controls.Add(header);

            return card;
        }

        private TableLayoutPanel CreateDataRow(string label, string value)
        {
            var row = new TableLayoutPanel
            {
                Width = 584,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 3, 0, 5),
                ColumnCount = 2,
                RowCount = 1
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            var lblK = new Label
            {
                Text = label,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 2)
            };

            var lblV = new Label
            {
                Text = value,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 2)
            };

            row.Controls.Add(lblK, 0, 0);
            row.Controls.Add(lblV, 1, 0);
            return row;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(Color.FromArgb(140, 70, 90), 2f);
            e.Graphics.DrawRoundedRectangle(pen, new Rectangle(1, 1, Width - 3, Height - 3), 12);
        }
    }
}
