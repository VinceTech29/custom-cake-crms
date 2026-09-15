using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Staff.Inquiries
{
    /// <summary>
    /// Quick status update modal matching the design system
    /// </summary>
    public class InquiryStatusModal : Form
    {
        private static readonly Color ColorModalBg = Color.White;
        private static readonly Color ColorBorder = Color.FromArgb(140, 70, 90); // Maroon #8C465A
        private static readonly Color ColorLabel = Color.FromArgb(124, 108, 104);
        private static readonly Color ColorPrimaryBtn = Color.FromArgb(140, 70, 90);
        private static readonly Color ColorCancelBtn = Color.FromArgb(239, 231, 223);

        private readonly CustomerInquiry _inquiry;
        private ComboBox cbStatus = null!;
        private TextBox txtNotes = null!;
        private Button btnConvertToOrder = null!;
        private Button btnSave = null!;

        public bool ConvertToOrderRequested { get; private set; }

        public InquiryStatusModal(CustomerInquiry inquiry)
        {
            _inquiry = inquiry;
            InitializeModal();
            BuildContent();
        }

        private void InitializeModal()
        {
            Text = "Update Status";
            Size = new Size(530, 420);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ColorModalBg;
            DoubleBuffered = true;
            ShowInTaskbar = false;

            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(ColorBorder, 2.5f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(1, 1, Width - 3, Height - 3), 16);
            };

            Load += (s, e) =>
            {
                this.CenterOnParentOrScreen();
                this.ApplyRoundedRegion(16);
            };
            Shown += (s, e) => this.CenterOnParentOrScreen();
        }

        private void BuildContent()
        {
            var root = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorModalBg,
                Padding = new Padding(28, 24, 28, 24)
            };

            // 1. Header
            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));

            var titleFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            var lblTitle = new Label
            {
                Text = "Update Status",
                Font = new Font(UITheme.FontSerif, 16F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true
            };

            var custName = _inquiry.Customer != null ? $"{_inquiry.Customer.FirstName} {_inquiry.Customer.LastName}".Trim() : $"Customer #{_inquiry.CustomerId}";
            var lblSub = new Label
            {
                Text = $"{_inquiry.InquiryCode} • {custName}",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 0)
            };

            titleFlow.Controls.Add(lblTitle);
            titleFlow.Controls.Add(lblSub);

            var btnClose = new Button
            {
                Text = "✕",
                Size = new Size(32, 32),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = ColorLabel,
                BackColor = Color.Transparent
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            headerTable.Controls.Add(titleFlow, 0, 0);
            headerTable.Controls.Add(btnClose, 1, 0);
            root.Controls.Add(headerTable);

            // 2. Status Selection
            var lblStatus = new Label
            {
                Text = "NEW STATUS",
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = ColorLabel,
                Location = new Point(28, 95),
                AutoSize = true
            };
            root.Controls.Add(lblStatus);

            int contentWidth = Width - 56;

            var cbStatusWrapper = new Panel
            {
                Location = new Point(28, 120),
                Size = new Size(contentWidth, 44),
                BackColor = Color.FromArgb(239, 230, 222)
            };
            cbStatusWrapper.ApplyRoundedRegion(12);

            cbStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 10F),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(239, 230, 222),
                ForeColor = UITheme.TextDark,
                Location = new Point(12, 10),
                Width = contentWidth - 24
            };
            var nextStatuses = InquiryStatusWorkflow.GetNextValidStatuses(_inquiry.Status);
            if (nextStatuses.Count > 0)
            {
                foreach (var st in nextStatuses)
                {
                    cbStatus.Items.Add(st);
                }
                cbStatus.SelectedIndex = 0;
            }
            else
            {
                cbStatus.Items.Add($"{_inquiry.Status} (Locked)");
                cbStatus.SelectedIndex = 0;
                cbStatus.Enabled = false;
                lblStatus.Text = $"STATUS ({_inquiry.Status.ToUpper()} — LOCKED)";
                lblStatus.ForeColor = Color.FromArgb(185, 28, 28);
            }
            cbStatusWrapper.Controls.Add(cbStatus);
            root.Controls.Add(cbStatusWrapper);

            // 3. Notes
            var lblNotes = new Label
            {
                Text = "ADDITIONAL NOTES",
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = ColorLabel,
                Location = new Point(28, 180),
                AutoSize = true
            };
            root.Controls.Add(lblNotes);

            var txtNotesWrapper = new Panel
            {
                Location = new Point(28, 205),
                Size = new Size(contentWidth, 80),
                BackColor = Color.FromArgb(239, 230, 222)
            };
            txtNotesWrapper.ApplyRoundedRegion(12);

            txtNotes = new TextBox
            {
                Multiline = true,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(239, 230, 222),
                Font = new Font(UITheme.FontSans, 9.5F),
                ForeColor = UITheme.TextDark,
                Location = new Point(12, 10),
                Size = new Size(contentWidth - 24, 60),
                Text = _inquiry.Notes
            };
            txtNotesWrapper.Controls.Add(txtNotes);
            root.Controls.Add(txtNotesWrapper);

            // 4. Action Buttons
            btnConvertToOrder = new Button
            {
                Text = "Convert to Order →",
                Size = new Size(160, 42),
                BackColor = Color.FromArgb(22, 163, 74), // Green #16A34A
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Visible = string.Equals(_inquiry.Status, "Approved", StringComparison.OrdinalIgnoreCase)
            };
            btnConvertToOrder.FlatAppearance.BorderSize = 0;
            btnConvertToOrder.ApplyRoundedRegion(12);
            btnConvertToOrder.Click += async (s, e) =>
            {
                _inquiry.Status = "Approved";
                if (!string.IsNullOrWhiteSpace(txtNotes.Text))
                {
                    _inquiry.Notes = txtNotes.Text.Trim();
                }

                try
                {
                    await CrmDataService.UpdateInquiryAsync(_inquiry);
                    ConvertToOrderRequested = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to update inquiry status in database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            btnSave = new Button
            {
                Text = "Update Status",
                Size = new Size(130, 42),
                BackColor = ColorPrimaryBtn,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Visible = nextStatuses.Count > 0
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.ApplyRoundedRegion(12);
            btnSave.Click += async (s, e) =>
            {
                if (cbStatus.SelectedItem == null) return;
                string newStatus = cbStatus.SelectedItem.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(newStatus) || newStatus.Contains("(Locked)")) return;

                if (!InquiryStatusWorkflow.CanTransition(_inquiry.Status, newStatus))
                {
                    MessageBox.Show($"Cannot transition inquiry from '{_inquiry.Status}' to '{newStatus}'. The inquiry lifecycle is forward-only.", "Invalid Status Transition", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _inquiry.Status = newStatus;
                if (!string.IsNullOrWhiteSpace(txtNotes.Text))
                {
                    _inquiry.Notes = txtNotes.Text.Trim();
                }

                try
                {
                    await CrmDataService.UpdateInquiryAsync(_inquiry);
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to update inquiry status in database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(95, 42),
                BackColor = ColorCancelBtn,
                ForeColor = UITheme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.ApplyRoundedRegion(12);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            var buttonFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                Location = new Point(Width - 28 - 420, 320),
                Size = new Size(420, 50),
                BackColor = Color.Transparent
            };
            buttonFlow.Controls.Add(btnConvertToOrder);
            buttonFlow.Controls.Add(btnSave);
            buttonFlow.Controls.Add(btnCancel);

            cbStatus.SelectedIndexChanged += (s, e) =>
            {
                bool isApproved = string.Equals(cbStatus.SelectedItem?.ToString(), "Approved", StringComparison.OrdinalIgnoreCase);
                btnConvertToOrder.Visible = isApproved;
            };

            root.Controls.Add(buttonFlow);

            Controls.Add(root);
        }
    }
}
