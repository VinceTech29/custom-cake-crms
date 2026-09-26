using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Staff.Customers
{
    /// <summary>
    /// "New / Edit Customer" modal dialog matching media_1789378780783.png & media_1789379612476.png:
    /// - Spacious 620 x 720px dimensions with 32px padding for generous alignment and breathing room
    /// - Borderless with smooth 16px rounded corners and subtle border
    /// - Bold serif header ("New Customer" or "Edit Customer") + circular close button (✕)
    /// - Uppercase gray category labels (#7C6C68) with mauve required asterisks (*)
    /// - #EFE6DE rounded input fields (Height 46px, Notes 110px) with 14px radius
    /// - Complete CRUD: Create, Edit/Update, and Delete with instant UI refresh
    /// - Full fallback between SessionService and ApiService
    /// </summary>
    public class CustomerForm : Form
    {
        // Palette matching design system
        private static readonly Color ColorModalBg = Color.White;
        private static readonly Color ColorFieldBg = Color.FromArgb(239, 230, 222);    // #EFE6DE
        private static readonly Color ColorBorder = Color.FromArgb(213, 205, 197);     // Subtle outer border #D5CDC5
        private static readonly Color ColorDivider = Color.FromArgb(239, 230, 222);    // #EFE6DE divider
        private static readonly Color ColorLabel = Color.FromArgb(124, 108, 104);       // Uppercase Gray #7C6C68
        private static readonly Color ColorAsterisk = Color.FromArgb(156, 107, 115);   // Mauve #9C6B73
        private static readonly Color ColorPrimaryBtn = Color.FromArgb(140, 70, 90);    // Maroon #8C465A
        private static readonly Color ColorCancelBtn = Color.FromArgb(239, 231, 223);   // Beige #EFE7DF
        private static readonly Color ColorDeleteBtn = Color.FromArgb(253, 232, 232);   // Soft red #FDE8E8
        private static readonly Color ColorDeleteText = Color.FromArgb(197, 48, 48);   // Dark red #C53030
        private static readonly Color ColorTextDark = Color.FromArgb(31, 27, 24);       // #1F1B18
        private static readonly Color ColorError = Color.FromArgb(197, 48, 48);

        // Input controls
        private TextBox txtFullName = null!;
        private TextBox txtEmail = null!;
        private TextBox txtPhone = null!;
        private TextBox txtAddress = null!;
        private TextBox txtCakePreferences = null!;
        private TextBox txtNotes = null!;

        private Label lblErrName = null!;
        private Label lblErrEmail = null!;

        // Mode and Customer reference
        private readonly Customer? _existingCustomer;
        private readonly bool _isEditMode;

        public Customer? NewCustomer { get; private set; }
        public bool IsDeleted { get; private set; }

        /// <summary>
        /// Constructor for creating a new customer
        /// </summary>
        public CustomerForm() : this(null)
        {
        }

        /// <summary>
        /// Constructor for editing an existing customer
        /// </summary>
        public CustomerForm(Customer? customer)
        {
            _existingCustomer = customer;
            _isEditMode = customer != null;

            InitializeModal();
            BuildContent();

            if (_isEditMode && customer != null)
            {
                PopulateExistingData(customer);
            }
        }

        private void InitializeModal()
        {
            Text = _isEditMode ? "Edit Customer" : "New Customer";
            Size = new Size(680, 800);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ColorModalBg;
            DoubleBuffered = true;
            ShowInTaskbar = false;

            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // High-contrast, noticeable 2.5px modal border
                using var pen = new Pen(Color.FromArgb(140, 70, 90), 2.5f);
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

            // =========================================================
            // 1. HEADER PANEL (74px)
            // =========================================================
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = ColorModalBg,
                Padding = new Padding(32, 0, 32, 0)
            };

            var lblTitle = new Label
            {
                Text = _isEditMode ? "Edit Customer" : "New Customer",
                Font = new Font(UITheme.FontSerif, 18f, FontStyle.Bold),
                ForeColor = ColorTextDark,
                AutoSize = true,
                Location = new Point(32, 24)
            };

            var btnClose = new Button
            {
                Text = "✕",
                Size = new Size(36, 36),
                Location = new Point(headerPanel.Width - 68, 19),
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

            // Drag window from header
            Point dragStart = Point.Empty;
            headerPanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragStart = e.Location; };
            headerPanel.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left) Location = new Point(Location.X + (e.X - dragStart.X), Location.Y + (e.Y - dragStart.Y)); };
            lblTitle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragStart = e.Location; };
            lblTitle.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left) Location = new Point(Location.X + (e.X - dragStart.X), Location.Y + (e.Y - dragStart.Y)); };

            var headerDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ColorDivider
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(btnClose);
            headerPanel.Controls.Add(headerDivider);

            // =========================================================
            // 2. FOOTER PANEL (78px)
            // =========================================================
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 78,
                BackColor = ColorModalBg,
                Padding = new Padding(32, 0, 32, 0)
            };

            var footerDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = ColorDivider
            };
            footerPanel.Controls.Add(footerDivider);

            // "Cancel" button
            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(115, 44),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                UseVisualStyleBackColor = false,
                Location = new Point(footerPanel.Width - 315, 17),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnCancel.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnCancel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool hovered = btnCancel.ClientRectangle.Contains(btnCancel.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? Color.FromArgb(228, 219, 210) : ColorCancelBtn);
                e.Graphics.FillRoundedRectangle(bg, new Rectangle(0, 0, btnCancel.Width, btnCancel.Height), 12);
                using var font = new Font(UITheme.FontSans, 9.5f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "Cancel", font, btnCancel.ClientRectangle, ColorTextDark,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnCancel.MouseEnter += (s, e) => btnCancel.Invalidate();
            btnCancel.MouseLeave += (s, e) => btnCancel.Invalidate();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // Primary action button ("Add Customer" or "Save Changes")
            string primaryText = _isEditMode ? "Save Changes" : "Add Customer";
            var btnSubmit = new Button
            {
                Text = primaryText,
                Size = new Size(165, 44),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                UseVisualStyleBackColor = false,
                Location = new Point(footerPanel.Width - 195, 17),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnSubmit.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnSubmit.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool hovered = btnSubmit.ClientRectangle.Contains(btnSubmit.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? Color.FromArgb(120, 58, 76) : ColorPrimaryBtn);
                e.Graphics.FillRoundedRectangle(bg, new Rectangle(0, 0, btnSubmit.Width, btnSubmit.Height), 12);
                using var font = new Font(UITheme.FontSans, 9.5f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, primaryText, font, btnSubmit.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnSubmit.MouseEnter += (s, e) => btnSubmit.Invalidate();
            btnSubmit.MouseLeave += (s, e) => btnSubmit.Invalidate();
            btnSubmit.Click += BtnSubmit_Click;

            footerPanel.Controls.Add(btnCancel);
            footerPanel.Controls.Add(btnSubmit);

            // In edit mode, add "Delete Customer" button on the left
            if (_isEditMode)
            {
                var btnDelete = new Button
                {
                    Text = "Delete Customer",
                    Size = new Size(140, 44),
                    Cursor = Cursors.Hand,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    UseVisualStyleBackColor = false,
                    Location = new Point(32, 17),
                    Anchor = AnchorStyles.Top | AnchorStyles.Left
                };
                btnDelete.FlatAppearance.BorderSize = 0;
                btnDelete.FlatAppearance.MouseOverBackColor = Color.Transparent;
                btnDelete.FlatAppearance.MouseDownBackColor = Color.Transparent;
                btnDelete.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    bool hovered = btnDelete.ClientRectangle.Contains(btnDelete.PointToClient(Cursor.Position));
                    using var bg = new SolidBrush(hovered ? Color.FromArgb(248, 215, 215) : ColorDeleteBtn);
                    e.Graphics.FillRoundedRectangle(bg, new Rectangle(0, 0, btnDelete.Width, btnDelete.Height), 12);
                    using var font = new Font(UITheme.FontSans, 9f, FontStyle.Bold);
                    TextRenderer.DrawText(e.Graphics, "Delete Customer", font, btnDelete.ClientRectangle, ColorDeleteText,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };
                btnDelete.MouseEnter += (s, e) => btnDelete.Invalidate();
                btnDelete.MouseLeave += (s, e) => btnDelete.Invalidate();
                btnDelete.Click += BtnDelete_Click;
                footerPanel.Controls.Add(btnDelete);
            }

            // =========================================================
            // 3. CENTER FORM FIELDS PANEL (Generous Width & Breathing Room)
            // =========================================================
            const int fieldWidth = 608; // 680 - 36*2
            var fieldsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorModalBg,
                AutoScroll = true,
                Padding = new Padding(36, 20, 36, 16)
            };

            int currentY = 20;
            const int vGap = 18;

            // 1. FULL NAME *
            var fullNameBlock = CreateInputField("FULL NAME", true, "e.g. Maria Santos", fieldWidth, 48, false, out txtFullName, out lblErrName);
            fullNameBlock.Location = new Point(36, currentY);
            currentY += fullNameBlock.Height + vGap;

            // 2. EMAIL * & PHONE (Two Columns)
            int colWidth = (fieldWidth - 20) / 2; // 294px each
            var emailBlock = CreateInputField("EMAIL", true, "email@example.com", colWidth, 48, false, out txtEmail, out lblErrEmail);
            emailBlock.Location = new Point(36, currentY);

            var phoneBlock = CreateInputField("PHONE", false, "+63 917 000 0000", colWidth, 48, false, out txtPhone, out _);
            phoneBlock.Location = new Point(36 + colWidth + 20, currentY);

            currentY += emailBlock.Height + vGap;

            // 3. ADDRESS
            var addressBlock = CreateInputField("ADDRESS", false, "Street, City", fieldWidth, 48, false, out txtAddress, out _);
            addressBlock.Location = new Point(36, currentY);
            currentY += addressBlock.Height + vGap;

            // 4. CAKE PREFERENCES
            var prefBlock = CreateInputField("CAKE PREFERENCES", false, "e.g. Fondant, floral designs, pastel colors", fieldWidth, 48, false, out txtCakePreferences, out _);
            prefBlock.Location = new Point(36, currentY);
            currentY += prefBlock.Height + vGap;

            // 5. NOTES (multiline 110px)
            var notesBlock = CreateInputField("NOTES", false, "Internal notes about this customer...", fieldWidth, 110, true, out txtNotes, out _);
            notesBlock.Location = new Point(36, currentY);

            fieldsPanel.Controls.Add(fullNameBlock);
            fieldsPanel.Controls.Add(emailBlock);
            fieldsPanel.Controls.Add(phoneBlock);
            fieldsPanel.Controls.Add(addressBlock);
            fieldsPanel.Controls.Add(prefBlock);
            fieldsPanel.Controls.Add(notesBlock);

            root.Controls.Add(fieldsPanel);
            root.Controls.Add(headerPanel);
            root.Controls.Add(footerPanel);
            fieldsPanel.BringToFront();

            Controls.Add(root);

            AcceptButton = btnSubmit;
            CancelButton = btnCancel;
        }

        private Panel CreateInputField(string labelText, bool isRequired, string placeholder, int width, int height, bool multiline, out TextBox textBox, out Label errorLabel)
        {
            const int labelHeight = 24;
            const int gapBetweenLabelAndInput = 8;

            var container = new Panel
            {
                Size = new Size(width, labelHeight + gapBetweenLabelAndInput + height),
                BackColor = ColorModalBg
            };

            // Label Flow (Label + Mauve Asterisk)
            var labelFlow = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                Size = new Size(width, labelHeight),
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = ColorModalBg,
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
            labelFlow.Controls.Add(lbl);

            if (isRequired)
            {
                var ast = new Label
                {
                    Text = " *",
                    Font = new Font(UITheme.FontSans, 8.5f, FontStyle.Bold),
                    ForeColor = ColorAsterisk,
                    AutoSize = true,
                    Margin = Padding.Empty
                };
                labelFlow.Controls.Add(ast);
            }

            // Rounded Input Box Panel positioned cleanly below label with guaranteed 8px gap
            var boxPanel = new Panel
            {
                Location = new Point(0, labelHeight + gapBetweenLabelAndInput),
                Size = new Size(width, height),
                BackColor = ColorFieldBg
            };
            boxPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(ColorFieldBg);
                e.Graphics.FillRoundedRectangle(brush, new Rectangle(0, 0, boxPanel.Width, boxPanel.Height), 12);
            };

            // Inner TextBox
            var tb = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = ColorFieldBg,
                ForeColor = ColorTextDark,
                Font = new Font(UITheme.FontSans, 10f),
                PlaceholderText = placeholder,
                Multiline = multiline
            };

            if (multiline)
            {
                tb.Location = new Point(16, 12);
                tb.Size = new Size(width - 32, height - 24);
                tb.ScrollBars = ScrollBars.None;
            }
            else
            {
                tb.Location = new Point(16, (height - tb.PreferredHeight) / 2);
                tb.Width = width - 32;
            }

            boxPanel.Click += (s, e) => tb.Focus();
            boxPanel.Controls.Add(tb);
            textBox = tb;

            // Inline error label
            errorLabel = new Label
            {
                Text = string.Empty,
                Font = new Font(UITheme.FontSans, 7.5f, FontStyle.Bold),
                ForeColor = ColorError,
                AutoSize = true,
                Location = new Point(width - 150, 4),
                TextAlign = ContentAlignment.TopRight,
                Visible = false
            };

            container.Controls.Add(labelFlow);
            container.Controls.Add(boxPanel);
            container.Controls.Add(errorLabel);

            return container;
        }

        private void PopulateExistingData(Customer customer)
        {
            string fullName = $"{customer.FirstName} {customer.LastName}".Trim();
            txtFullName.Text = fullName;
            txtEmail.Text = customer.Email ?? string.Empty;
            txtPhone.Text = customer.Phone ?? string.Empty;
            txtAddress.Text = customer.AddressText ?? string.Empty;
            txtCakePreferences.Text = customer.CakePreferences ?? string.Empty;
            txtNotes.Text = customer.Notes ?? string.Empty;
        }

        private async void BtnSubmit_Click(object? sender, EventArgs e)
        {
            bool isValid = true;
            lblErrName.Visible = false;
            lblErrEmail.Visible = false;

            if (string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                lblErrName.Text = "Name is required";
                lblErrName.Visible = true;
                isValid = false;
            }

            string emailVal = txtEmail.Text.Trim();
            if (string.IsNullOrWhiteSpace(emailVal))
            {
                lblErrEmail.Text = "Email is required";
                lblErrEmail.Visible = true;
                isValid = false;
            }
            else if (!Regex.IsMatch(emailVal, @"^\S+@\S+\.\S+$"))
            {
                lblErrEmail.Text = "Invalid email";
                lblErrEmail.Visible = true;
                isValid = false;
            }

            if (!isValid) return;

            string fullName = txtFullName.Text.Trim();
            string firstName = fullName;
            string lastName = string.Empty;
            int spaceIdx = fullName.IndexOf(' ');
            if (spaceIdx > 0)
            {
                firstName = fullName.Substring(0, spaceIdx).Trim();
                lastName = fullName.Substring(spaceIdx + 1).Trim();
            }

            var currentUser = SessionService.CurrentUser;
            int companyId = currentUser?.CompanyId ?? 1;
            int userId = currentUser?.UserId ?? 1;

            if (_isEditMode && _existingCustomer != null)
            {
                // =====================================================
                // UPDATE (U in CRUD)
                // =====================================================
                _existingCustomer.FirstName = firstName;
                _existingCustomer.LastName = lastName;
                _existingCustomer.Email = emailVal;
                _existingCustomer.Phone = txtPhone.Text.Trim();
                _existingCustomer.AddressText = txtAddress.Text.Trim();
                _existingCustomer.CakePreferences = txtCakePreferences.Text.Trim();
                _existingCustomer.Notes = txtNotes.Text.Trim();

                try
                {
                    NewCustomer = await CrmDataService.UpdateCustomerAsync(_existingCustomer);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to update customer in database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                // =====================================================
                // CREATE (C in CRUD)
                // =====================================================
                var customer = new Customer
                {
                    CompanyId = companyId,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = emailVal,
                    Phone = txtPhone.Text.Trim(),
                    AddressText = txtAddress.Text.Trim(),
                    CakePreferences = txtCakePreferences.Text.Trim(),
                    Notes = txtNotes.Text.Trim(),
                    RegisteredDate = DateTime.UtcNow,
                    CreatedByUserId = userId
                };

                try
                {
                    NewCustomer = await CrmDataService.CreateCustomerAsync(customer);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to create customer in database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_existingCustomer == null) return;

            string name = $"{_existingCustomer.FirstName} {_existingCustomer.LastName}".Trim();
            var confirm = MessageBox.Show(
                $"Are you sure you want to delete customer \"{name}\"?\n\nThis action cannot be undone.",
                "Delete Customer",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2
            );

            if (confirm != DialogResult.Yes) return;

            try
            {
                await CrmDataService.DeleteCustomerAsync(_existingCustomer.CustomerId);
                IsDeleted = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to delete customer from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    public class CreateCustomerRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
    }
}
