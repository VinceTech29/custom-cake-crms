using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Retention
{
    public class CreateRetentionRequestModal : Form
    {
        private Panel headerPanel = null!;
        private Panel scrollableBodyPanel = null!;
        private Panel footerPanel = null!;

        private ComboBox cmbCustomer = null!;
        private ComboBox cmbSegment = null!;
        private ComboBox cmbActionType = null!;
        private NumericUpDown numDiscount = null!;
        private TextBox txtDetails = null!;
        private ComboBox cmbReason = null!;
        private Panel pnlCustomReason = null!;
        private TextBox txtCustomReason = null!;
        private Label lblCustomReasonRequired = null!;
        private Button btnCancel = null!;
        private Button btnSubmit = null!;

        private System.Collections.Generic.List<CC.Domain.Entities.Customer> customersList = new();

        public CreateRetentionRequestModal(int? preselectedCustomerId = null, string? preselectedSegment = null)
        {
            InitializeComponent();
            BuildRegions();
            _ = LoadCustomersAsync(preselectedCustomerId, preselectedSegment);
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Text = "New Retention Request";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(620, 680);
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
                Height = 64,
                BackColor = Color.White,
                Padding = new Padding(24, 16, 24, 16)
            };

            var lblTitle = new Label
            {
                Text = "Submit Retention Request",
                Font = new Font(UITheme.FontSans, 14F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Location = new Point(24, 18)
            };

            var btnClose = new Button
            {
                Text = "\uE711",
                Font = new Font("Segoe MDL2 Assets", 10F),
                ForeColor = UITheme.TextMuted,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(32, 32),
                Location = new Point(headerPanel.Width - 56, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = UITheme.BorderColor
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(btnClose);
            headerPanel.Controls.Add(headerBorder);

            // 2. FIXED FOOTER
            footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                BackColor = Color.FromArgb(250, 248, 246),
                Padding = new Padding(24, 14, 24, 14)
            };

            var footerBorder = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = UITheme.BorderColor
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Height = 38,
                Width = 95,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(footerPanel.Width - 250, 15)
            };
            btnCancel.FlatAppearance.BorderColor = UITheme.BorderColor;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            btnSubmit = new Button
            {
                Text = "Submit for Admin Approval",
                Height = 38,
                Width = 200,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(footerPanel.Width - 224, 15)
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Click += async (s, e) => await SubmitRequestAsync();

            footerPanel.Controls.Add(footerBorder);
            footerPanel.Controls.Add(btnCancel);
            footerPanel.Controls.Add(btnSubmit);

            // 3. SCROLLABLE BODY
            scrollableBodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
                Padding = new Padding(28, 20, 28, 20)
            };

            BuildFormContent(scrollableBodyPanel);

            // Add in dock order: header, footer, then body fill
            Controls.Add(scrollableBodyPanel);
            Controls.Add(footerPanel);
            Controls.Add(headerPanel);
        }

        private void BuildFormContent(Panel parent)
        {
            var contentFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Width = parent.ClientSize.Width - 40,
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, 0, 20)
            };

            // Banner explaining workflow
            var infoCard = new Panel
            {
                Width = 530,
                Height = 56,
                BackColor = Color.FromArgb(243, 240, 255),
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(14, 10, 14, 10)
            };
            infoCard.ApplyRoundedRegion(8);
            var lblInfo = new Label
            {
                Text = "\u2139 Manager creates and submits this retention proposal. An Admin will review and decide to Approve or Reject it. Self-approval is strictly prohibited.",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(88, 28, 135),
                Dock = DockStyle.Fill
            };
            infoCard.Controls.Add(lblInfo);
            contentFlow.Controls.Add(infoCard);

            // Field 1: Customer
            contentFlow.Controls.Add(CreateFieldLabel("Customer / Client", true));
            cmbCustomer = new ComboBox
            {
                Width = 530,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9.5F),
                Margin = new Padding(0, 0, 0, 14)
            };
            contentFlow.Controls.Add(cmbCustomer);

            // Field 2: Target Segment & Action Type (Row)
            var rowSegmentAction = new TableLayoutPanel
            {
                Width = 530,
                Height = 65,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 14),
                BackColor = Color.Transparent
            };
            rowSegmentAction.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            rowSegmentAction.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var col1 = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, Margin = Padding.Empty };
            col1.Controls.Add(CreateFieldLabel("Target Segment", true));
            cmbSegment = new ComboBox
            {
                Width = 255,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9.5F)
            };
            cmbSegment.Items.AddRange(new object[] { "At Risk", "Inactive", "Loyal", "Returning", "New" });
            cmbSegment.SelectedIndex = 0;
            col1.Controls.Add(cmbSegment);

            var col2 = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, Dock = DockStyle.Fill, Margin = Padding.Empty };
            col2.Controls.Add(CreateFieldLabel("Retention Action Type", true));
            cmbActionType = new ComboBox
            {
                Width = 255,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9.5F)
            };
            cmbActionType.Items.AddRange(new object[] {
                "Special Discount",
                "VIP Loyalty Reward",
                "Win-Back Campaign",
                "Complimentary Upgrade",
                "Personalized Offer",
                "Other Proposal"
            });
            cmbActionType.SelectedIndex = 0;
            col2.Controls.Add(cmbActionType);

            rowSegmentAction.Controls.Add(col1, 0, 0);
            rowSegmentAction.Controls.Add(col2, 1, 0);
            contentFlow.Controls.Add(rowSegmentAction);

            // Field 3: Discount %
            contentFlow.Controls.Add(CreateFieldLabel("Proposed Discount Percentage (%)", false));
            numDiscount = new NumericUpDown
            {
                Width = 160,
                Height = 32,
                Font = new Font(UITheme.FontSans, 9.5F),
                Minimum = 0,
                Maximum = 100,
                DecimalPlaces = 1,
                Value = 10m,
                Margin = new Padding(0, 0, 0, 14)
            };
            contentFlow.Controls.Add(numDiscount);

            // Field 4: Retention Proposal Details
            contentFlow.Controls.Add(CreateFieldLabel("Retention Proposal Details", true));
            txtDetails = new TextBox
            {
                Width = 530,
                Height = 68,
                Multiline = true,
                Font = new Font(UITheme.FontSans, 9.5F),
                ScrollBars = ScrollBars.Vertical,
                Margin = new Padding(0, 0, 0, 14)
            };
            contentFlow.Controls.Add(txtDetails);

            // Field 5: Reason for Retention (Requirement 3)
            contentFlow.Controls.Add(CreateFieldLabel("Reason for Retention (Required)", true));
            cmbReason = new ComboBox
            {
                Width = 530,
                Height = 32,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9.5F),
                Margin = new Padding(0, 0, 0, 10)
            };
            foreach (var opt in CrmDataService.RetentionReasonOptions)
            {
                cmbReason.Items.Add(opt);
            }
            cmbReason.SelectedIndex = 3; // "Improve Customer Retention"
            cmbReason.SelectedIndexChanged += (s, e) =>
            {
                bool isOther = cmbReason.SelectedItem?.ToString() == "Other";
                pnlCustomReason.Visible = isOther;
                lblCustomReasonRequired.Visible = isOther;
            };
            contentFlow.Controls.Add(cmbReason);

            // Field 5.1: Custom reason text input (mandatory if "Other")
            pnlCustomReason = new Panel
            {
                Width = 530,
                AutoSize = true,
                BackColor = Color.Transparent,
                Visible = false,
                Margin = new Padding(0, 0, 0, 14)
            };
            var pnlCustomFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Top
            };
            lblCustomReasonRequired = CreateFieldLabel("Please specify specific retention reason (Required for 'Other')", true);
            txtCustomReason = new TextBox
            {
                Width = 530,
                Height = 52,
                Multiline = true,
                Font = new Font(UITheme.FontSans, 9.5F),
                ScrollBars = ScrollBars.Vertical
            };
            pnlCustomFlow.Controls.Add(lblCustomReasonRequired);
            pnlCustomFlow.Controls.Add(txtCustomReason);
            pnlCustomReason.Controls.Add(pnlCustomFlow);
            contentFlow.Controls.Add(pnlCustomReason);

            parent.Controls.Add(contentFlow);
        }

        private Label CreateFieldLabel(string text, bool required)
        {
            var lbl = new Label
            {
                Text = text + (required ? " *" : ""),
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };
            return lbl;
        }

        private async Task LoadCustomersAsync(int? preselectedCustomerId, string? preselectedSegment)
        {
            try
            {
                await using var context = CrmDataService.CreateDbContext();
                customersList = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
                    context.Customers.OrderBy(c => c.FirstName).ThenBy(c => c.LastName));

                cmbCustomer.Items.Clear();
                int selectIdx = 0;
                for (int i = 0; i < customersList.Count; i++)
                {
                    var c = customersList[i];
                    cmbCustomer.Items.Add($"{c.FirstName} {c.LastName} ({c.Email ?? "No Email"})");
                    if (preselectedCustomerId.HasValue && c.CustomerId == preselectedCustomerId.Value)
                    {
                        selectIdx = i;
                    }
                }

                if (cmbCustomer.Items.Count > 0)
                {
                    cmbCustomer.SelectedIndex = selectIdx;
                }

                if (!string.IsNullOrWhiteSpace(preselectedSegment))
                {
                    for (int i = 0; i < cmbSegment.Items.Count; i++)
                    {
                        if (string.Equals(cmbSegment.Items[i]?.ToString(), preselectedSegment, StringComparison.OrdinalIgnoreCase))
                        {
                            cmbSegment.SelectedIndex = i;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading customers: {ex.Message}", "Data Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task SubmitRequestAsync()
        {
            if (cmbCustomer.SelectedIndex < 0 || cmbCustomer.SelectedIndex >= customersList.Count)
            {
                MessageBox.Show("Please select a customer.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var customer = customersList[cmbCustomer.SelectedIndex];
            string targetSegment = cmbSegment.SelectedItem?.ToString() ?? "At Risk";
            string actionType = cmbActionType.SelectedItem?.ToString() ?? "Special Discount";
            decimal discount = numDiscount.Value;
            string details = txtDetails.Text.Trim();
            string reasonCategory = cmbReason.SelectedItem?.ToString() ?? "";
            string customReason = txtCustomReason.Text.Trim();

            if (string.IsNullOrWhiteSpace(details))
            {
                MessageBox.Show("Please provide the retention proposal details.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtDetails.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(reasonCategory))
            {
                MessageBox.Show("Please select a Reason for Retention.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cmbReason.Focus();
                return;
            }

            if (reasonCategory == "Other" && string.IsNullOrWhiteSpace(customReason))
            {
                MessageBox.Show("Please specify the custom reason when selecting 'Other'.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtCustomReason.Focus();
                return;
            }

            btnSubmit.Enabled = false;
            btnSubmit.Text = "Submitting...";

            try
            {
                await CrmDataService.CreateRetentionRequestAsync(
                    customerId: customer.CustomerId,
                    targetSegment: targetSegment,
                    actionType: actionType,
                    discountPercent: discount,
                    retentionDetails: details,
                    reasonCategory: reasonCategory,
                    reasonCustomDetails: customReason
                );

                MessageBox.Show(
                    "Retention request submitted successfully!\r\nIt is now in 'Pending' status awaiting Business Admin review and approval.",
                    "Request Submitted",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to submit retention request: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnSubmit.Enabled = true;
                btnSubmit.Text = "Submit for Admin Approval";
            }
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
