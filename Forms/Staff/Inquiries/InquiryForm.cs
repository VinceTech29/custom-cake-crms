using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;
using CC.Forms.Staff.Customers;
using CC.Services;

namespace CC.Forms.Staff.Inquiries
{
    /// <summary>
    /// "New / Edit Inquiry" modal dialog matching the design system:
    /// - Spacious 680 x 800px dimensions with 36px padding
    /// - Noticeable 2.5px solid maroon border (#8C465A) with 16px rounded corners
    /// - Bold serif header ("New Inquiry" or "Edit Inquiry") + circular close button (✕)
    /// - Uppercase gray category labels (#7C6C68) with mauve required asterisks (*)
    /// - #EFE6DE rounded input fields with guaranteed label separation
    /// - Customer dropdown with quick "+ New" customer creation
    /// - Complete CRUD (Create, Update, Delete) with instant UI refresh
    /// </summary>
    public class InquiryForm : Form
    {
        private static readonly Color ColorModalBg = Color.White;
        private static readonly Color ColorFieldBg = Color.FromArgb(239, 230, 222);    // #EFE6DE
        private static readonly Color ColorBorder = Color.FromArgb(140, 70, 90);       // Noticeable 2.5px Maroon #8C465A
        private static readonly Color ColorDivider = Color.FromArgb(239, 230, 222);    // #EFE6DE divider
        private static readonly Color ColorLabel = Color.FromArgb(124, 108, 104);       // Uppercase Gray #7C6C68
        private static readonly Color ColorAsterisk = Color.FromArgb(156, 107, 115);   // Mauve #9C6B73
        private static readonly Color ColorPrimaryBtn = Color.FromArgb(140, 70, 90);    // Maroon #8C465A
        private static readonly Color ColorCancelBtn = Color.FromArgb(239, 231, 223);   // Beige #EFE7DF
        private static readonly Color ColorDeleteBtn = Color.FromArgb(253, 232, 232);   // Soft red #FDE8E8
        private static readonly Color ColorDeleteText = Color.FromArgb(197, 48, 48);   // Dark red #C53030
        private static readonly Color ColorTextDark = Color.FromArgb(31, 27, 24);       // #1F1B18
        private static readonly Color ColorError = Color.FromArgb(197, 48, 48);

        // Form controls
        private ComboBox cbCustomer = null!;
        private Button btnQuickAddCustomer = null!;
        private TextBox txtCakeType = null!;
        private DateTimePicker dtEventDate = null!;
        private ComboBox cbAssignedTo = null!;
        private TextBox txtBudget = null!;
        private ComboBox cbStatus = null!;
        private TextBox txtNotes = null!;

        private Button btnSave = null!;
        private Button? btnDelete;
        private Button? btnConvert;
        private Panel? pnlLockedBanner;
        private Label? lblLockedMessage;

        private Label lblErrCustomer = null!;
        private Label lblErrCakeType = null!;

        // Mode and Inquiry reference
        private readonly CustomerInquiry? _existingInquiry;
        private readonly bool _isEditMode;

        public CustomerInquiry? NewInquiry { get; private set; }
        public bool IsDeleted { get; private set; }

        public InquiryForm() : this(null)
        {
        }

        public InquiryForm(CustomerInquiry? inquiry)
        {
            _existingInquiry = inquiry;
            _isEditMode = inquiry != null;

            InitializeModal();
            BuildContent();

            Load += async (s, e) =>
            {
                await PopulateCustomersAsync();
                if (_isEditMode && inquiry != null)
                {
                    PopulateExistingData(inquiry);
                }
            };
        }

        private void InitializeModal()
        {
            Text = _isEditMode ? "Edit Inquiry" : "New Inquiry";
            Size = new Size(680, 670);
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
                Padding = Padding.Empty
            };

            // 1. TOP HEADER (76px)
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = ColorModalBg,
                Padding = new Padding(36, 0, 36, 0)
            };

            var lblTitle = new Label
            {
                Text = _isEditMode ? $"Edit Inquiry #{_existingInquiry?.InquiryCode}" : "New Inquiry",
                Font = new Font(UITheme.FontSerif, 20f, FontStyle.Bold),
                ForeColor = ColorTextDark,
                AutoSize = true,
                Location = new Point(36, 24)
            };

            var btnClose = new Button
            {
                Text = "✕",
                Size = new Size(36, 36),
                Location = new Point(headerPanel.Width - 72, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                UseVisualStyleBackColor = false
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnClose.FlatAppearance.MouseDownBackColor = Color.Transparent;

            btnClose.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool hovered = btnClose.ClientRectangle.Contains(btnClose.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? Color.FromArgb(228, 220, 212) : Color.FromArgb(240, 234, 228));
                e.Graphics.FillEllipse(bg, 0, 0, btnClose.Width - 1, btnClose.Height - 1);
                using var font = new Font(UITheme.FontSans, 10f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "✕", font, btnClose.ClientRectangle, ColorLabel,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnClose.MouseEnter += (s, e) => btnClose.Invalidate();
            btnClose.MouseLeave += (s, e) => btnClose.Invalidate();
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Point dragStart = Point.Empty;
            headerPanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragStart = e.Location; };
            headerPanel.MouseMove += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    Location = new Point(Location.X + (e.X - dragStart.X), Location.Y + (e.Y - dragStart.Y));
                }
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(btnClose);

            // 2. BOTTOM ACTION FOOTER (76px)
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 76,
                BackColor = ColorModalBg,
                Padding = new Padding(36, 0, 36, 0)
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 44),
                BackColor = ColorCancelBtn,
                ForeColor = ColorTextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Location = new Point(footerPanel.Width - 36 - 150 - 12 - 110, 16)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.ApplyRoundedRegion(14);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            btnSave = new Button
            {
                Text = _isEditMode ? "Update Inquiry" : "Create Inquiry",
                Size = new Size(150, 44),
                BackColor = ColorPrimaryBtn,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                Location = new Point(footerPanel.Width - 36 - 150, 16)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.ApplyRoundedRegion(14);
            btnSave.Click += BtnSave_Click;

            if (_isEditMode && _existingInquiry != null)
            {
                btnDelete = new Button
                {
                    Text = "Delete Inquiry",
                    Size = new Size(130, 44),
                    BackColor = ColorDeleteBtn,
                    ForeColor = ColorDeleteText,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(UITheme.FontSans, 9.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                    Location = new Point(36, 16)
                };
                btnDelete.FlatAppearance.BorderSize = 0;
                btnDelete.ApplyRoundedRegion(14);
                btnDelete.Click += BtnDelete_Click;
                footerPanel.Controls.Add(btnDelete);

                btnConvert = new Button
                {
                    Text = "Convert to Order →",
                    Size = new Size(160, 44),
                    BackColor = Color.FromArgb(22, 163, 74), // Green #16A34A
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(UITheme.FontSans, 9.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                    Location = new Point(176, 16),
                    Visible = string.Equals(_existingInquiry.Status, "Approved", StringComparison.OrdinalIgnoreCase)
                };
                btnConvert.FlatAppearance.BorderSize = 0;
                btnConvert.ApplyRoundedRegion(14);
                btnConvert.Click += (s, e) =>
                {
                    using var orderForm = new CC.Forms.Staff.Orders.OrderForm(_existingInquiry);
                    if (orderForm.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                    {
                        MessageBox.Show($"Inquiry {_existingInquiry.InquiryCode} successfully converted to Order #{orderForm.NewOrder?.OrderId}!", "Inquiry Converted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        DialogResult = DialogResult.OK;
                        Close();
                    }
                };
                footerPanel.Controls.Add(btnConvert);
            }

            footerPanel.Controls.Add(btnCancel);
            footerPanel.Controls.Add(btnSave);

            // 3. MAIN FORM BODY (Scrollable Panel)
            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = ColorModalBg,
                Padding = new Padding(36, 6, 36, 16)
            };

            int curY = 4;
            int fullWidth = 608;
            int colWidth = (fullWidth - 20) / 2; // ~294px

            // Locked banner (displayed when inquiry status is locked)
            pnlLockedBanner = new Panel
            {
                Location = new Point(36, curY),
                Size = new Size(fullWidth, 42),
                BackColor = Color.FromArgb(220, 252, 231),
                Visible = false
            };
            pnlLockedBanner.ApplyRoundedRegion(12);

            lblLockedMessage = new Label
            {
                Text = "🔒 Locked",
                Font = new Font(UITheme.FontSans, 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(21, 128, 61),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(14, 0, 0, 0)
            };
            pnlLockedBanner.Controls.Add(lblLockedMessage);
            bodyPanel.Controls.Add(pnlLockedBanner);
            curY += 50;

            // --- A. CUSTOMER ---
            bodyPanel.Controls.Add(CreateCategoryLabel("CUSTOMER", true, 36, curY, out lblErrCustomer));
            curY += 28;

            var custRow = new Panel
            {
                Location = new Point(36, curY),
                Size = new Size(fullWidth, 48),
                BackColor = Color.Transparent
            };

            var cbCustWrapper = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(fullWidth - 110, 48),
                BackColor = ColorFieldBg
            };
            cbCustWrapper.ApplyRoundedRegion(14);

            cbCustomer = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 10f),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorFieldBg,
                ForeColor = ColorTextDark,
                Location = new Point(14, 12),
                Width = cbCustWrapper.Width - 28
            };
            cbCustWrapper.Controls.Add(cbCustomer);

            btnQuickAddCustomer = new Button
            {
                Text = "+ New",
                Size = new Size(100, 48),
                Location = new Point(fullWidth - 100, 0),
                BackColor = ColorFieldBg,
                ForeColor = ColorPrimaryBtn,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9f, FontStyle.Bold)
            };
            btnQuickAddCustomer.FlatAppearance.BorderSize = 0;
            btnQuickAddCustomer.ApplyRoundedRegion(14);
            btnQuickAddCustomer.Click += BtnQuickAddCustomer_Click;

            custRow.Controls.Add(cbCustWrapper);
            custRow.Controls.Add(btnQuickAddCustomer);
            bodyPanel.Controls.Add(custRow);
            curY += 62;

            // --- B. CAKE TYPE & THEME ---
            bodyPanel.Controls.Add(CreateCategoryLabel("CAKE TYPE / THEME", true, 36, curY, out lblErrCakeType));
            curY += 28;

            var txtCakeTypeWrapper = new Panel
            {
                Location = new Point(36, curY),
                Size = new Size(fullWidth, 48),
                BackColor = ColorFieldBg
            };
            txtCakeTypeWrapper.ApplyRoundedRegion(14);

            txtCakeType = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = ColorFieldBg,
                Font = new Font(UITheme.FontSans, 10f),
                ForeColor = ColorTextDark,
                Location = new Point(14, 14),
                Width = fullWidth - 28,
                PlaceholderText = "e.g. 3-tier wedding cake, fondant with pastel florals..."
            };
            txtCakeTypeWrapper.Controls.Add(txtCakeType);
            bodyPanel.Controls.Add(txtCakeTypeWrapper);
            curY += 62;

            // --- C. TWO COLUMN: EVENT DATE & ASSIGNED TO ---
            bodyPanel.Controls.Add(CreateCategoryLabel("EVENT DATE", true, 36, curY, out _));
            bodyPanel.Controls.Add(CreateCategoryLabel("ASSIGNED TO", false, 36 + colWidth + 20, curY, out _));
            curY += 28;

            var dtEventWrapper = new Panel
            {
                Location = new Point(36, curY),
                Size = new Size(colWidth, 48),
                BackColor = ColorFieldBg
            };
            dtEventWrapper.ApplyRoundedRegion(14);

            dtEventDate = new DateTimePicker
            {
                Font = new Font(UITheme.FontSans, 10f),
                Format = DateTimePickerFormat.Short,
                CalendarForeColor = ColorTextDark,
                CalendarMonthBackground = Color.White,
                CalendarTitleBackColor = ColorPrimaryBtn,
                Location = new Point(14, 12),
                Width = colWidth - 28,
                Value = DateTime.Now.AddDays(14)
            };
            dtEventWrapper.Controls.Add(dtEventDate);

            var cbAssignedWrapper = new Panel
            {
                Location = new Point(36 + colWidth + 20, curY),
                Size = new Size(colWidth, 48),
                BackColor = ColorFieldBg
            };
            cbAssignedWrapper.ApplyRoundedRegion(14);

            cbAssignedTo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 10f),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorFieldBg,
                ForeColor = ColorTextDark,
                Location = new Point(14, 12),
                Width = colWidth - 28
            };
            cbAssignedTo.Items.AddRange(new object[] { "Staff - Lea R.", "Staff - Mark P.", "Unassigned" });
            cbAssignedTo.SelectedIndex = 0;
            cbAssignedWrapper.Controls.Add(cbAssignedTo);

            bodyPanel.Controls.Add(dtEventWrapper);
            bodyPanel.Controls.Add(cbAssignedWrapper);
            curY += 62;

            // --- D. TWO COLUMN: ESTIMATED BUDGET & STATUS ---
            bodyPanel.Controls.Add(CreateCategoryLabel("ESTIMATED BUDGET (\u20B1)", false, 36, curY, out _));
            bodyPanel.Controls.Add(CreateCategoryLabel("STATUS", true, 36 + colWidth + 20, curY, out _));
            curY += 28;

            var txtBudgetWrapper = new Panel
            {
                Location = new Point(36, curY),
                Size = new Size(colWidth, 48),
                BackColor = ColorFieldBg
            };
            txtBudgetWrapper.ApplyRoundedRegion(14);

            txtBudget = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = ColorFieldBg,
                Font = new Font(UITheme.FontSans, 10f),
                ForeColor = ColorTextDark,
                Location = new Point(14, 14),
                Width = colWidth - 28,
                PlaceholderText = "e.g. 5000"
            };
            txtBudgetWrapper.Controls.Add(txtBudget);

            var cbStatusWrapper = new Panel
            {
                Location = new Point(36 + colWidth + 20, curY),
                Size = new Size(colWidth, 48),
                BackColor = ColorFieldBg
            };
            cbStatusWrapper.ApplyRoundedRegion(14);

            cbStatus = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 10f),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorFieldBg,
                ForeColor = ColorTextDark,
                Location = new Point(14, 12),
                Width = colWidth - 28
            };
            cbStatus.Items.AddRange(new object[] { "New", "In Progress", "Quoted", "Approved", "Converted", "Closed" });
            cbStatus.SelectedIndex = 0;
            cbStatusWrapper.Controls.Add(cbStatus);

            bodyPanel.Controls.Add(txtBudgetWrapper);
            bodyPanel.Controls.Add(cbStatusWrapper);
            curY += 62;

            // --- E. NOTES / SPECIAL REQUESTS ---
            bodyPanel.Controls.Add(CreateCategoryLabel("NOTES / SPECIAL REQUESTS", false, 36, curY, out _));
            curY += 28;

            var txtNotesWrapper = new Panel
            {
                Location = new Point(36, curY),
                Size = new Size(fullWidth, 100),
                BackColor = ColorFieldBg
            };
            txtNotesWrapper.ApplyRoundedRegion(14);

            txtNotes = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Multiline = true,
                BackColor = ColorFieldBg,
                Font = new Font(UITheme.FontSans, 10f),
                ForeColor = ColorTextDark,
                Location = new Point(14, 12),
                Size = new Size(fullWidth - 28, 76),
                PlaceholderText = "Dietary restrictions, delivery instructions, reference links..."
            };
            txtNotesWrapper.Controls.Add(txtNotes);
            bodyPanel.Controls.Add(txtNotesWrapper);

            root.Controls.Add(bodyPanel);
            root.Controls.Add(footerPanel);
            root.Controls.Add(headerPanel);

            Controls.Add(root);
        }

        private static Panel CreateCategoryLabel(string labelText, bool isRequired, int x, int y, out Label errorLabel)
        {
            var p = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(290, 22),
                BackColor = Color.Transparent
            };

            var fl = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            var lbl = new Label
            {
                Text = labelText,
                Font = new Font(UITheme.FontSans, 8.5f, FontStyle.Bold),
                ForeColor = ColorLabel,
                AutoSize = true,
                Margin = Padding.Empty
            };
            fl.Controls.Add(lbl);

            if (isRequired)
            {
                var ast = new Label
                {
                    Text = " *",
                    Font = new Font(UITheme.FontSans, 9f, FontStyle.Bold),
                    ForeColor = ColorAsterisk,
                    AutoSize = true,
                    Margin = Padding.Empty
                };
                fl.Controls.Add(ast);
            }

            var err = new Label
            {
                Text = string.Empty,
                Font = new Font(UITheme.FontSans, 8f, FontStyle.Italic),
                ForeColor = ColorError,
                AutoSize = true,
                Visible = false,
                Margin = new Padding(8, 1, 0, 0)
            };
            fl.Controls.Add(err);
            errorLabel = err;

            p.Controls.Add(fl);
            return p;
        }

        private async Task PopulateCustomersAsync()
        {
            cbCustomer.Items.Clear();
            try
            {
                var customers = await CrmDataService.GetCustomersAsync();
                foreach (var c in customers)
                {
                    cbCustomer.Items.Add(new CustomerItem
                    {
                        Id = c.CustomerId,
                        DisplayName = $"{c.FirstName} {c.LastName} • {c.Phone}".Trim()
                    });
                }

                if (cbCustomer.Items.Count > 0 && cbCustomer.SelectedIndex < 0)
                {
                    cbCustomer.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load customers from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PopulateExistingData(CustomerInquiry inq)
        {
            for (int i = 0; i < cbCustomer.Items.Count; i++)
            {
                if (cbCustomer.Items[i] is CustomerItem ci && ci.Id == inq.CustomerId)
                {
                    cbCustomer.SelectedIndex = i;
                    break;
                }
            }

            txtCakeType.Text = inq.CakeType;
            dtEventDate.Value = inq.EventDate > DateTime.MinValue ? inq.EventDate : DateTime.Now.AddDays(14);
            cbAssignedTo.SelectedItem = inq.AssignedTo;
            if (cbAssignedTo.SelectedIndex < 0) cbAssignedTo.SelectedIndex = 0;

            txtBudget.Text = inq.EstimatedBudget > 0 ? inq.EstimatedBudget.ToString("0.##") : string.Empty;

            // Forward-only status choices: current status + forward next-step statuses
            cbStatus.Items.Clear();
            cbStatus.Items.Add(inq.Status);
            var nextStatuses = InquiryStatusWorkflow.GetNextValidStatuses(inq.Status);
            foreach (var s in nextStatuses)
            {
                cbStatus.Items.Add(s);
            }
            cbStatus.SelectedIndex = 0;

            txtNotes.Text = inq.Notes;

            if (InquiryStatusWorkflow.IsLocked(inq.Status))
            {
                ApplyLockedState(inq.Status);
            }
        }

        private void ApplyLockedState(string status)
        {
            bool isApproved = string.Equals(status, "Approved", StringComparison.OrdinalIgnoreCase);
            bool isConverted = string.Equals(status, "Converted", StringComparison.OrdinalIgnoreCase);

            string lockMsg;
            Color bannerBg;
            Color bannerFg;

            if (isApproved)
            {
                lockMsg = "🔒 Approved — Locked for conversion. Convert to Order to proceed.";
                bannerBg = Color.FromArgb(220, 252, 231); // #DCFCE7
                bannerFg = Color.FromArgb(21, 128, 61);   // #15803D
            }
            else if (isConverted)
            {
                lockMsg = "🔒 Converted to Order — Locked (Read-Only).";
                bannerBg = Color.FromArgb(243, 236, 229); // #F3ECE5
                bannerFg = Color.FromArgb(68, 64, 60);    // #44403C
            }
            else
            {
                lockMsg = $"🔒 {status} — Locked (Read-Only).";
                bannerBg = Color.FromArgb(254, 226, 226); // #FEE2E2
                bannerFg = Color.FromArgb(185, 28, 28);   // #B91C1C
            }

            if (pnlLockedBanner != null)
            {
                pnlLockedBanner.BackColor = bannerBg;
                pnlLockedBanner.Visible = true;
                if (lblLockedMessage != null)
                {
                    lblLockedMessage.Text = lockMsg;
                    lblLockedMessage.ForeColor = bannerFg;
                }
            }

            cbCustomer.Enabled = false;
            btnQuickAddCustomer.Enabled = false;
            txtCakeType.ReadOnly = true;
            dtEventDate.Enabled = false;
            cbAssignedTo.Enabled = false;
            txtBudget.ReadOnly = true;
            cbStatus.Enabled = false;
            txtNotes.ReadOnly = true;

            btnSave.Visible = false;
            if (btnDelete != null) btnDelete.Visible = false;
            if (btnConvert != null) btnConvert.Visible = isApproved;
        }

        private async void BtnQuickAddCustomer_Click(object? sender, EventArgs e)
        {
            using var form = new CustomerForm();
            if (form.ShowDialog(this.FindForm() ?? this) == DialogResult.OK && form.NewCustomer != null)
            {
                await PopulateCustomersAsync();
                for (int i = 0; i < cbCustomer.Items.Count; i++)
                {
                    if (cbCustomer.Items[i] is CustomerItem ci && ci.Id == form.NewCustomer.CustomerId)
                    {
                        cbCustomer.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            bool valid = true;
            lblErrCustomer.Visible = false;
            lblErrCakeType.Visible = false;

            if (cbCustomer.SelectedItem is not CustomerItem ci || ci.Id <= 0)
            {
                lblErrCustomer.Text = "Required";
                lblErrCustomer.Visible = true;
                valid = false;
            }

            if (string.IsNullOrWhiteSpace(txtCakeType.Text))
            {
                lblErrCakeType.Text = "Required";
                lblErrCakeType.Visible = true;
                valid = false;
            }

            if (!valid) return;

            decimal budget = 0m;
            decimal.TryParse(txtBudget.Text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out budget);

            var selectedCustomerItem = (CustomerItem)cbCustomer.SelectedItem!;

            if (_isEditMode && _existingInquiry != null)
            {
                string newStatus = cbStatus.SelectedItem?.ToString() ?? _existingInquiry.Status;
                if (!InquiryStatusWorkflow.CanTransition(_existingInquiry.Status, newStatus))
                {
                    MessageBox.Show($"Cannot transition inquiry from '{_existingInquiry.Status}' to '{newStatus}'. The inquiry lifecycle is forward-only.", "Invalid Status Transition", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _existingInquiry.CustomerId = selectedCustomerItem.Id;
                _existingInquiry.CakeType = txtCakeType.Text.Trim();
                _existingInquiry.EventDate = dtEventDate.Value.Date;
                _existingInquiry.AssignedTo = cbAssignedTo.SelectedItem?.ToString() ?? "Unassigned";
                _existingInquiry.EstimatedBudget = budget;
                _existingInquiry.Status = newStatus;
                _existingInquiry.Notes = txtNotes.Text.Trim();

                try
                {
                    NewInquiry = await CrmDataService.UpdateInquiryAsync(_existingInquiry);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to update inquiry in database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                var newInq = new CustomerInquiry
                {
                    CustomerId = selectedCustomerItem.Id,
                    CakeType = txtCakeType.Text.Trim(),
                    EventDate = dtEventDate.Value.Date,
                    AssignedTo = cbAssignedTo.SelectedItem?.ToString() ?? "Unassigned",
                    EstimatedBudget = budget,
                    Status = cbStatus.SelectedItem?.ToString() ?? "New",
                    Notes = txtNotes.Text.Trim(),
                    CreatedAt = DateTime.UtcNow
                };

                try
                {
                    NewInquiry = await CrmDataService.CreateInquiryAsync(newInq);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to create inquiry in database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_existingInquiry == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete inquiry {_existingInquiry.InquiryCode}?",
                "Confirm Delete",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    await CrmDataService.DeleteInquiryAsync(_existingInquiry.InquiryId);
                    IsDeleted = true;
                    DialogResult = DialogResult.OK;
                    Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to delete inquiry from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private class CustomerItem
        {
            public int Id { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public override string ToString() => DisplayName;
        }
    }
}
