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
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(680, 750);
            BackColor = Color.White;
            ShowInTaskbar = false;
            DoubleBuffered = true;
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
                Text = $"Retention Request Details #{requestId:D4}",
                Font = new Font(UITheme.FontSans, 13.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Location = new Point(24, 20)
            };

            lblStatusBadge = new Label
            {
                Text = "PENDING",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                Height = 26,
                Width = 90,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(480, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = UITheme.StatusYellowBg,
                ForeColor = UITheme.StatusYellowFg
            };
            lblStatusBadge.ApplyRoundedRegion(6);

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
                Location = new Point(footerPanel.Width - 340, 16),
                Visible = false
            };
            btnReject.FlatAppearance.BorderColor = Color.FromArgb(254, 202, 202);
            btnReject.Click += (s, e) => ShowRejectionPanel(true);
            actionPanel.Controls.Add(btnReject);

            btnApprove = new Button
            {
                Text = "Approve Request",
                Height = 38,
                Width = 135,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(22, 163, 74),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(footerPanel.Width - 210, 16),
                Visible = false
            };
            btnApprove.FlatAppearance.BorderSize = 0;
            btnApprove.Click += async (s, e) => await ApproveRequestAsync();
            actionPanel.Controls.Add(btnApprove);

            btnClose = new Button
            {
                Text = "Close",
                Height = 38,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(footerPanel.Width - 110, 16)
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
                BackColor = Color.White,
                Padding = new Padding(28, 20, 28, 20)
            };

            contentFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Width = 600,
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
            var sec1 = CreateSectionCard("CUSTOMER & RETENTION PROPOSAL", "\uE77B");
            var sec1Flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Top,
                Width = 550
            };

            sec1Flow.Controls.Add(CreateDataRow("Customer / Client:", $"{request.CustomerName} ({request.CustomerEmail})"));
            sec1Flow.Controls.Add(CreateDataRow("Target Segment:", request.TargetSegment));
            sec1Flow.Controls.Add(CreateDataRow("Action Type:", request.ActionType));
            sec1Flow.Controls.Add(CreateDataRow("Proposed Discount:", $"{request.DiscountPercent:0.#}%"));
            sec1Flow.Controls.Add(CreateDataRow("Retention Details:", request.RetentionDetails, multiline: true));

            sec1.Controls.Add(sec1Flow);
            contentFlow.Controls.Add(sec1);

            // --- SECTION 2: REASON FOR RETENTION ---
            var sec2 = CreateSectionCard("REASON FOR RETENTION", "\uE7BA");
            var sec2Flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Top,
                Width = 550
            };

            sec2Flow.Controls.Add(CreateDataRow("Reason Category:", request.ReasonCategory));
            if (!string.IsNullOrWhiteSpace(request.ReasonCustomDetails))
            {
                sec2Flow.Controls.Add(CreateDataRow("Specific Details / Note:", request.ReasonCustomDetails, multiline: true));
            }

            sec2.Controls.Add(sec2Flow);
            contentFlow.Controls.Add(sec2);

            // --- SECTION 3: APPROVAL HISTORY & AUDIT TRAIL (Requirement 4) ---
            var sec3 = CreateSectionCard("APPROVAL HISTORY (AUDIT TIMELINE)", "\uE823");
            var timelinePanel = BuildApprovalTimeline();
            sec3.Controls.Add(timelinePanel);
            contentFlow.Controls.Add(sec3);

            // Inline Rejection Form Panel
            pnlRejectionInput = BuildRejectionPanel();
            pnlRejectionInput.Visible = false;
            contentFlow.Controls.Add(pnlRejectionInput);
        }

        private Panel BuildApprovalTimeline()
        {
            if (request == null) return new Panel();

            var pnl = new Panel
            {
                Width = 550,
                AutoSize = true,
                BackColor = Color.Transparent,
                Dock = DockStyle.Top,
                Padding = new Padding(8, 4, 8, 4)
            };

            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Top
            };

            // Timeline Step 1: REQUESTED BY MANAGER
            var step1 = CreateTimelineNode(
                title: "1. Retention Request Submitted",
                subtitle: $"Requested by: {request.RequestedByUserName} (Manager)",
                timestamp: request.RequestedDate.ToLocalTime().ToString("MMM dd, yyyy h:mm tt"),
                statusBadgeText: "SUBMITTED",
                statusColor: UITheme.PrimaryMauve,
                statusBg: Color.FromArgb(243, 240, 255),
                details: null
            );
            flow.Controls.Add(step1);

            // Timeline Step 2: ADMIN REVIEW
            if (request.ReviewedDate.HasValue)
            {
                bool isApproved = request.Status == "Approved";
                string reviewTitle = isApproved ? "2. Request Approved" : "2. Request Rejected";
                string reviewSub = isApproved
                    ? $"Approved by: {request.ReviewedByUserName ?? "Admin"} (Business Admin)"
                    : $"Rejected by: {request.RejectedByUserName ?? request.ReviewedByUserName ?? "Admin"} (Business Admin)";
                string timeStr = (request.RejectionDate ?? request.ReviewedDate.Value).ToLocalTime().ToString("MMM dd, yyyy h:mm tt");
                string badge = isApproved ? "APPROVED" : "REJECTED";
                Color fg = isApproved ? UITheme.StatusGreenFg : UITheme.StatusRedFg;
                Color bg = isApproved ? UITheme.StatusGreenBg : UITheme.StatusRedBg;

                string? details = null;
                if (!isApproved && !string.IsNullOrWhiteSpace(request.RejectionReason))
                {
                    details = $"Reason for Rejection: {request.RejectionReason}";
                    if (!string.IsNullOrWhiteSpace(request.AdminRemarks))
                    {
                        details += $"\r\nRemarks: {request.AdminRemarks}";
                    }
                }
                else if (isApproved && !string.IsNullOrWhiteSpace(request.AdminRemarks))
                {
                    details = $"Admin Remarks: {request.AdminRemarks}";
                }

                var step2 = CreateTimelineNode(
                    title: reviewTitle,
                    subtitle: reviewSub,
                    timestamp: timeStr,
                    statusBadgeText: badge,
                    statusColor: fg,
                    statusBg: bg,
                    details: details
                );
                flow.Controls.Add(step2);
            }
            else
            {
                // Pending Placeholder
                var stepPending = CreateTimelineNode(
                    title: "2. Pending Admin Review & Decision",
                    subtitle: "Awaiting Business Admin approval or rejection.",
                    timestamp: "In Progress",
                    statusBadgeText: "PENDING",
                    statusColor: UITheme.StatusYellowFg,
                    statusBg: UITheme.StatusYellowBg,
                    details: null
                );
                flow.Controls.Add(stepPending);
            }

            pnl.Controls.Add(flow);
            return pnl;
        }

        private Panel CreateTimelineNode(
            string title,
            string subtitle,
            string timestamp,
            string statusBadgeText,
            Color statusColor,
            Color statusBg,
            string? details)
        {
            var card = new Panel
            {
                Width = 530,
                AutoSize = true,
                BackColor = Color.FromArgb(249, 250, 251),
                Margin = new Padding(0, 0, 0, 10),
                Padding = new Padding(14, 12, 14, 12)
            };
            card.ApplyRoundedRegion(8);

            var topRow = new TableLayoutPanel
            {
                Width = 502,
                Height = 28,
                ColumnCount = 2,
                RowCount = 1,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));

            var lblT = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Dock = DockStyle.Left
            };

            var badge = new Label
            {
                Text = statusBadgeText,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                Height = 22,
                Width = 85,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = statusBg,
                ForeColor = statusColor,
                Dock = DockStyle.Right
            };
            badge.ApplyRoundedRegion(4);

            topRow.Controls.Add(lblT, 0, 0);
            topRow.Controls.Add(badge, 1, 0);

            var lblSub = new Label
            {
                Text = $"{subtitle}  \u00B7  {timestamp}",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 2, 0, 4)
            };

            card.Controls.Add(lblSub);
            card.Controls.Add(topRow);

            if (!string.IsNullOrWhiteSpace(details))
            {
                var lblDet = new Label
                {
                    Text = details,
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                    ForeColor = Color.FromArgb(60, 60, 60),
                    AutoSize = true,
                    Dock = DockStyle.Top,
                    Margin = new Padding(0, 8, 0, 0)
                };
                card.Controls.Add(lblDet);
            }

            return card;
        }

        private Panel BuildRejectionPanel()
        {
            var pnl = new Panel
            {
                Width = 570,
                AutoSize = true,
                BackColor = Color.FromArgb(254, 242, 242),
                Margin = new Padding(0, 10, 0, 10),
                Padding = new Padding(16, 14, 16, 14)
            };
            pnl.ApplyRoundedRegion(8);

            var flow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
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
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 4)
            };
            flow.Controls.Add(lblReasonReq);

            txtRejectionReason = new TextBox
            {
                Width = 530,
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
                Width = 530,
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
                Margin = Padding.Empty
            };

            btnConfirmRejection = new Button
            {
                Text = "Confirm Rejection",
                Height = 36,
                Width = 145,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(220, 38, 38),
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
                lblActionNotice.Text = $"This request was already reviewed ({request.Status}).";
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
                $"Are you sure you want to approve this retention request for customer '{request.CustomerName}'?\r\n\r\nAction: {request.ActionType}\r\nDiscount: {request.DiscountPercent:0.#}%",
                "Confirm Retention Approval",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            btnApprove.Enabled = false;
            btnReject.Enabled = false;

            try
            {
                await CrmDataService.ApproveRetentionRequestAsync(
                    requestId: request.RequestId,
                    adminRemarks: "Approved by Business Admin"
                );

                MessageBox.Show(
                    "Retention request has been approved successfully.",
                    "Approved",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                await LoadDataAsync();
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
                await CrmDataService.RejectRetentionRequestAsync(
                    requestId: request.RequestId,
                    rejectionReason: reason,
                    adminRemarks: string.IsNullOrWhiteSpace(remarks) ? null : remarks
                );

                MessageBox.Show(
                    "Retention request has been rejected.",
                    "Rejected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                ShowRejectionPanel(false);
                await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to reject request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnConfirmRejection.Enabled = true;
                btnConfirmRejection.Text = "Confirm Rejection";
            }
        }

        private Panel CreateSectionCard(string title, string iconGlyph)
        {
            var card = new Panel
            {
                Width = 570,
                AutoSize = true,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(16, 14, 16, 14)
            };
            card.ApplyRoundedRegion(8);

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 8);
            };

            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 28,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8)
            };

            var lbl = new Label
            {
                Text = $"{iconGlyph}  {title}",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                AutoSize = true,
                Location = new Point(0, 2)
            };
            header.Controls.Add(lbl);
            card.Controls.Add(header);

            return card;
        }

        private Panel CreateDataRow(string label, string value, bool multiline = false)
        {
            var row = new Panel
            {
                Width = 535,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 6)
            };

            var lblK = new Label
            {
                Text = label,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                Width = 170,
                Location = new Point(0, 2),
                AutoSize = false
            };

            var lblV = new Label
            {
                Text = value,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                Location = new Point(175, 2),
                Width = 355,
                AutoSize = true
            };

            row.Controls.Add(lblK);
            row.Controls.Add(lblV);
            return row;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var pen = new Pen(UITheme.BorderColor, 1.5f);
            e.Graphics.DrawRectangle(pen, new Rectangle(0, 0, Width - 1, Height - 1));
        }
    }
}
