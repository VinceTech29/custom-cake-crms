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

namespace CC.Forms.Staff.Orders
{
    /// <summary>
    /// "New / Edit Order" modal dialog matching the design system:
    /// - Spacious 680 x 800px dimensions with 36px padding
    /// - Noticeable 2.5px solid maroon border (#8C465A) with 16px rounded corners
    /// - Bold serif header ("New Order" or "Edit Order") + circular close button (✕)
    /// - Uppercase gray category labels (#7C6C68) with mauve required asterisks (*)
    /// - #EFE6DE rounded input fields (Height 48px, Notes 110px) with guaranteed label separation
    /// - Customer dropdown with quick "+ New" customer creation
    /// - Complete CRUD (Create, Update, Delete) with instant UI refresh
    /// </summary>
    public class OrderForm : Form
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
        private ComboBox cbCakeSize = null!;
        private ComboBox cbFlavor = null!;
        private TextBox txtDesignTheme = null!;
        private TextBox txtTotalAmount = null!;
        private DateTimePicker dtDeliveryDate = null!;
        private ComboBox cbStatus = null!;
        private TextBox txtNotes = null!;

        private Label lblErrCustomer = null!;
        private Label lblErrCakeSize = null!;
        private Label lblErrTotal = null!;

        // Mode and Order reference
        private readonly SalesOrder? _existingOrder;
        private readonly CustomerInquiry? _sourceInquiry;
        private readonly bool _isEditMode;

        public SalesOrder? NewOrder { get; private set; }
        public bool IsDeleted { get; private set; }

        public int OrderCustomerId => cbCustomer.SelectedValue is int id ? id : (cbCustomer.SelectedItem is CustomerItem item ? item.Id : 0);
        public string OrderCakeSize => cbCakeSize.Text.Trim();
        public string OrderFlavor => cbFlavor.Text.Trim();
        public string OrderDesignTheme => txtDesignTheme.Text.Trim();
        public DateTime OrderDeliveryDate => dtDeliveryDate.Value;
        public decimal OrderTotalAmount { get; private set; }
        public int OrderStatusId => cbStatus.SelectedItem != null ? OrderStatusWorkflow.GetStatusId(cbStatus.SelectedItem.ToString()) : (_existingOrder?.StatusId ?? 0);
        public string OrderNotes => txtNotes.Text.Trim();

        public OrderForm() : this((SalesOrder?)null)
        {
        }

        public OrderForm(CustomerInquiry inquiry)
        {
            _sourceInquiry = inquiry;
            _existingOrder = null;
            _isEditMode = false;

            InitializeModal();
            BuildContent();

            Load += async (s, e) =>
            {
                await PopulateCustomersAsync(_sourceInquiry.CustomerId);
                PopulateInquiryData(_sourceInquiry);
            };
        }

        public OrderForm(SalesOrder? order)
        {
            _existingOrder = order;
            _sourceInquiry = null;
            _isEditMode = order != null;

            InitializeModal();
            BuildContent();

            Load += async (s, e) =>
            {
                await PopulateCustomersAsync(_existingOrder?.CustomerId);
                if (_isEditMode && _existingOrder != null)
                {
                    PopulateExistingData(_existingOrder);
                }
            };
        }

        private void InitializeModal()
        {
            Text = _sourceInquiry != null ? $"Convert Inquiry ({_sourceInquiry.InquiryCode})" : (_isEditMode ? "Edit Order" : "New Order");
            Size = new Size(680, 800);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ColorModalBg;
            DoubleBuffered = true;
            ShowInTaskbar = false;

            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Noticeable 2.5px maroon border
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

            // =========================================================
            // 1. HEADER PANEL (74px)
            // =========================================================
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = ColorModalBg,
                Padding = new Padding(36, 0, 36, 0)
            };

            string titleText = _sourceInquiry != null ? $"Convert Inquiry ({_sourceInquiry.InquiryCode})" : (_isEditMode ? "Edit Order" : "New Order");
            var lblTitle = new Label
            {
                Text = titleText,
                Font = new Font(UITheme.FontSerif, 18f, FontStyle.Bold),
                ForeColor = ColorTextDark,
                AutoSize = true,
                Location = new Point(36, 24)
            };

            var btnClose = new Button
            {
                Text = "✕",
                Size = new Size(36, 36),
                Location = new Point(headerPanel.Width - 72, 19),
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
                Padding = new Padding(36, 0, 36, 0)
            };

            var footerDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = ColorDivider
            };
            footerPanel.Controls.Add(footerDivider);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(115, 44),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                UseVisualStyleBackColor = false,
                Location = new Point(footerPanel.Width - 325, 17),
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

            string primaryText = _sourceInquiry != null ? "Convert to Order" : (_isEditMode ? "Save Changes" : "Create Order");
            var btnSubmit = new Button
            {
                Text = primaryText,
                Size = new Size(165, 44),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                UseVisualStyleBackColor = false,
                Location = new Point(footerPanel.Width - 201, 17),
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

            if (_isEditMode)
            {
                var btnDelete = new Button
                {
                    Text = "Delete Order",
                    Size = new Size(140, 44),
                    Cursor = Cursors.Hand,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    UseVisualStyleBackColor = false,
                    Location = new Point(36, 17),
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
                    TextRenderer.DrawText(e.Graphics, "Delete Order", font, btnDelete.ClientRectangle, ColorDeleteText,
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

            // 1. CUSTOMER * (Dropdown + inline "+ New" button)
            var customerBlock = CreateCustomerDropdownBlock("CUSTOMER", true, fieldWidth, 48, out cbCustomer, out btnQuickAddCustomer, out lblErrCustomer);
            customerBlock.Location = new Point(36, currentY);
            currentY += customerBlock.Height + vGap;

            // 2. CAKE SIZE * & FLAVOR (Two Columns - Editable Dropdowns: choose or type!)
            int colWidth = (fieldWidth - 20) / 2; // 294px each

            string[] defaultSizes = new[]
            {
                "6\" Round Single",
                "8\" Round Single",
                "9\" Round (2-tier)",
                "10\" Square Single",
                "12\" Round (3-tier)",
                "2-tier (6\" + 8\" Round)",
                "3-tier Wedding Cake",
                "Cupcakes (12 pcs)",
                "Cupcakes (24 pcs)",
                "Bento Cake (4\" Round)",
                "Custom Size / Tier"
            };

            string[] defaultFlavors = new[]
            {
                "Chocolate Ganache",
                "Dark Chocolate Truffle",
                "Red Velvet Cream Cheese",
                "Vanilla Bean & Strawberry Compote",
                "Ube Halaya & Leche Flan",
                "Matcha Green Tea Sponge",
                "Carrot Walnut & Cream Cheese",
                "Mocha Buttercream",
                "Mango Passionfruit",
                "Cookies & Cream",
                "Custom Flavor"
            };

            var sizeBlock = CreateEditableDropdownField("CAKE SIZE", true, defaultSizes, colWidth, 48, out cbCakeSize, out lblErrCakeSize);
            sizeBlock.Location = new Point(36, currentY);

            var flavorBlock = CreateEditableDropdownField("FLAVOR", false, defaultFlavors, colWidth, 48, out cbFlavor, out _);
            flavorBlock.Location = new Point(36 + colWidth + 20, currentY);

            currentY += sizeBlock.Height + vGap;

            // 3. DESIGN THEME & TOTAL AMOUNT * (Two Columns)
            var themeBlock = CreateInputField("DESIGN THEME", false, "e.g. Floral Vintage, Minimalist", colWidth, 48, false, out txtDesignTheme, out _);
            themeBlock.Location = new Point(36, currentY);

            var totalBlock = CreateInputField("TOTAL AMOUNT (₱)", true, "e.g. 3500.00", colWidth, 48, false, out txtTotalAmount, out lblErrTotal);
            totalBlock.Location = new Point(36 + colWidth + 20, currentY);

            currentY += themeBlock.Height + vGap;

            // 4. DELIVERY DATE & STATUS (Two Columns)
            var dateBlock = CreateDateField("DELIVERY DATE", colWidth, 48, out dtDeliveryDate);
            dateBlock.Location = new Point(36, currentY);

            var statusBlock = CreateStatusDropdownField("ORDER STATUS", colWidth, 48, out cbStatus);
            statusBlock.Location = new Point(36 + colWidth + 20, currentY);

            currentY += dateBlock.Height + vGap;

            // 5. NOTES / SPECIAL INSTRUCTIONS (multiline 110px)
            var notesBlock = CreateInputField("NOTES / SPECIAL INSTRUCTIONS", false, "Dietary restrictions, custom cake inscriptions, delivery directions...", fieldWidth, 110, true, out txtNotes, out _);
            notesBlock.Location = new Point(36, currentY);

            fieldsPanel.Controls.Add(customerBlock);
            fieldsPanel.Controls.Add(sizeBlock);
            fieldsPanel.Controls.Add(flavorBlock);
            fieldsPanel.Controls.Add(themeBlock);
            fieldsPanel.Controls.Add(totalBlock);
            fieldsPanel.Controls.Add(dateBlock);
            fieldsPanel.Controls.Add(statusBlock);
            fieldsPanel.Controls.Add(notesBlock);

            root.Controls.Add(fieldsPanel);
            root.Controls.Add(headerPanel);
            root.Controls.Add(footerPanel);
            fieldsPanel.BringToFront();

            Controls.Add(root);

            AcceptButton = btnSubmit;
            CancelButton = btnCancel;
        }

        private Panel CreateCustomerDropdownBlock(string labelText, bool isRequired, int width, int height, out ComboBox combo, out Button btnQuickAdd, out Label errorLabel)
        {
            const int labelHeight = 24;
            const int gapBetweenLabelAndInput = 8;

            var container = new Panel
            {
                Size = new Size(width, labelHeight + gapBetweenLabelAndInput + height),
                BackColor = ColorModalBg
            };

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

            // Wrapper box with rounded tan background
            int comboWidth = width - 96;
            var boxPanel = new Panel
            {
                Location = new Point(0, labelHeight + gapBetweenLabelAndInput),
                Size = new Size(comboWidth, height),
                BackColor = ColorFieldBg
            };
            boxPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(ColorFieldBg);
                e.Graphics.FillRoundedRectangle(brush, new Rectangle(0, 0, boxPanel.Width, boxPanel.Height), 12);
            };

            combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorFieldBg,
                ForeColor = ColorTextDark,
                Font = new Font(UITheme.FontSans, 10f),
                Location = new Point(14, (height - 24) / 2),
                Width = comboWidth - 28
            };
            boxPanel.Controls.Add(combo);

            // Inline "+ New" button to add customer right on the fly
            var quickAdd = new Button
            {
                Text = "+ New",
                Size = new Size(84, height),
                Location = new Point(comboWidth + 12, labelHeight + gapBetweenLabelAndInput),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                UseVisualStyleBackColor = false
            };
            quickAdd.FlatAppearance.BorderSize = 0;
            quickAdd.FlatAppearance.MouseOverBackColor = Color.Transparent;
            quickAdd.FlatAppearance.MouseDownBackColor = Color.Transparent;
            quickAdd.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool hovered = quickAdd.ClientRectangle.Contains(quickAdd.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? Color.FromArgb(120, 58, 76) : ColorPrimaryBtn);
                e.Graphics.FillRoundedRectangle(bg, new Rectangle(0, 0, quickAdd.Width, quickAdd.Height), 12);
                using var font = new Font(UITheme.FontSans, 9f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "+ New", font, quickAdd.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            quickAdd.MouseEnter += (s, e) => quickAdd.Invalidate();
            quickAdd.MouseLeave += (s, e) => quickAdd.Invalidate();
            quickAdd.Click += BtnQuickAddCustomer_Click;
            btnQuickAdd = quickAdd;

            errorLabel = new Label
            {
                Text = string.Empty,
                Font = new Font(UITheme.FontSans, 7.5f, FontStyle.Bold),
                ForeColor = ColorError,
                AutoSize = true,
                Location = new Point(width - 200, 4),
                TextAlign = ContentAlignment.TopRight,
                Visible = false
            };

            container.Controls.Add(labelFlow);
            container.Controls.Add(boxPanel);
            container.Controls.Add(btnQuickAdd);
            container.Controls.Add(errorLabel);

            return container;
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

        private Panel CreateDateField(string labelText, int width, int height, out DateTimePicker datePicker)
        {
            const int labelHeight = 24;
            const int gapBetweenLabelAndInput = 8;

            var container = new Panel
            {
                Size = new Size(width, labelHeight + gapBetweenLabelAndInput + height),
                BackColor = ColorModalBg
            };

            var lbl = new Label
            {
                Text = labelText,
                Font = new Font(UITheme.FontSans, 8.5f, FontStyle.Bold),
                ForeColor = ColorLabel,
                AutoSize = true,
                Location = new Point(0, 0)
            };

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

            datePicker = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                CalendarFont = new Font(UITheme.FontSans, 9.5f),
                Font = new Font(UITheme.FontSans, 10f),
                Location = new Point(14, (height - 24) / 2),
                Width = width - 28,
                Value = DateTime.Today.AddDays(7)
            };
            boxPanel.Controls.Add(datePicker);

            container.Controls.Add(lbl);
            container.Controls.Add(boxPanel);

            return container;
        }

        private Panel CreateStatusDropdownField(string labelText, int width, int height, out ComboBox combo)
        {
            const int labelHeight = 24;
            const int gapBetweenLabelAndInput = 8;

            var container = new Panel
            {
                Size = new Size(width, labelHeight + gapBetweenLabelAndInput + height),
                BackColor = ColorModalBg
            };

            var lbl = new Label
            {
                Text = labelText,
                Font = new Font(UITheme.FontSans, 8.5f, FontStyle.Bold),
                ForeColor = ColorLabel,
                AutoSize = true,
                Location = new Point(0, 0)
            };

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

            combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorFieldBg,
                ForeColor = ColorTextDark,
                Font = new Font(UITheme.FontSans, 10f),
                Location = new Point(14, (height - 24) / 2),
                Width = width - 28
            };
            combo.Items.AddRange(new object[] { "Pending", "Confirmed", "Processing", "Completed", "Ready", "Cancelled" });
            combo.SelectedIndex = 0;
            boxPanel.Controls.Add(combo);

            container.Controls.Add(lbl);
            container.Controls.Add(boxPanel);

            return container;
        }

        private Panel CreateEditableDropdownField(string labelText, bool isRequired, string[] options, int width, int height, out ComboBox combo, out Label errorLabel)
        {
            const int labelHeight = 24;
            const int gapBetweenLabelAndInput = 8;

            var container = new Panel
            {
                Size = new Size(width, labelHeight + gapBetweenLabelAndInput + height),
                BackColor = ColorModalBg
            };

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

            combo = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDown, // Allows picking from list or typing custom value!
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorFieldBg,
                ForeColor = ColorTextDark,
                Font = new Font(UITheme.FontSans, 10f),
                Location = new Point(14, (height - 24) / 2),
                Width = width - 28
            };
            combo.Items.AddRange(options);
            boxPanel.Controls.Add(combo);

            errorLabel = new Label
            {
                Text = "",
                Font = new Font(UITheme.FontSans, 8f),
                ForeColor = ColorError,
                AutoSize = true,
                Location = new Point(0, labelHeight + gapBetweenLabelAndInput + height + 2),
                Visible = false
            };

            container.Controls.Add(labelFlow);
            container.Controls.Add(boxPanel);
            container.Controls.Add(errorLabel);

            return container;
        }

        private async Task PopulateCustomersAsync(int? selectCustomerId = null)
        {
            cbCustomer.Items.Clear();

            try
            {
                var customers = await CrmDataService.GetCustomersAsync();
                var list = customers
                    .OrderBy(c => c.FirstName)
                    .Select(c => new CustomerItem(c.CustomerId, $"{c.FirstName} {c.LastName}".Trim(), c.Phone))
                    .ToList();

                foreach (var item in list)
                {
                    cbCustomer.Items.Add(item);
                }

                if (selectCustomerId.HasValue)
                {
                    for (int i = 0; i < cbCustomer.Items.Count; i++)
                    {
                        if (cbCustomer.Items[i] is CustomerItem ci && ci.Id == selectCustomerId.Value)
                        {
                            cbCustomer.SelectedIndex = i;
                            return;
                        }
                    }
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

        private void PopulateExistingData(SalesOrder order)
        {
            cbCakeSize.Text = order.CakeSize ?? string.Empty;
            cbFlavor.Text = order.Flavor ?? string.Empty;
            txtDesignTheme.Text = order.DesignTheme ?? string.Empty;

            decimal total = order.TotalAmount;
            if (total == 0 && order.OrderDetails.Any())
            {
                total = order.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);
            }
            txtTotalAmount.Text = total > 0 ? total.ToString("0.##") : string.Empty;

            if (order.DeliveryDate.HasValue)
            {
                dtDeliveryDate.Value = order.DeliveryDate.Value;
            }

            cbStatus.Items.Clear();
            string currentStatusName = OrderStatusWorkflow.GetStatusName(order.StatusId);
            cbStatus.Items.Add(currentStatusName);

            if (OrderStatusWorkflow.IsLocked(order.StatusId))
            {
                cbStatus.SelectedIndex = 0;
                cbStatus.Enabled = false;
            }
            else
            {
                foreach (var next in OrderStatusWorkflow.GetNextValidStatusNames(order.StatusId))
                {
                    if (!cbStatus.Items.Contains(next))
                    {
                        cbStatus.Items.Add(next);
                    }
                }
                cbStatus.SelectedIndex = 0;
                cbStatus.Enabled = true;
            }

            txtNotes.Text = order.Notes ?? string.Empty;
        }

        private void PopulateInquiryData(CustomerInquiry inq)
        {
            txtDesignTheme.Text = inq.CakeType ?? string.Empty;

            if (inq.EstimatedBudget > 0)
            {
                txtTotalAmount.Text = inq.EstimatedBudget.ToString("0.##");
            }

            if (inq.EventDate > DateTime.Today.AddDays(-30))
            {
                dtDeliveryDate.Value = inq.EventDate;
            }

            // Attempt smart matching for CakeSize and Flavor if CakeType contains matching text
            if (!string.IsNullOrWhiteSpace(inq.CakeType))
            {
                string ctLower = inq.CakeType.ToLower();
                foreach (var item in cbCakeSize.Items)
                {
                    string sz = item.ToString() ?? "";
                    if (sz.Length > 2 && ctLower.Contains(sz.ToLower()))
                    {
                        cbCakeSize.Text = sz;
                        break;
                    }
                }

                foreach (var item in cbFlavor.Items)
                {
                    string fl = item.ToString() ?? "";
                    if (fl.Length > 2 && ctLower.Contains(fl.ToLower()))
                    {
                        cbFlavor.Text = fl;
                        break;
                    }
                }
            }

            string notePrefix = $"Converted from Inquiry {inq.InquiryCode}.";
            txtNotes.Text = string.IsNullOrWhiteSpace(inq.Notes) ? notePrefix : $"{notePrefix}\r\nNotes: {inq.Notes}";
        }

        private async void BtnQuickAddCustomer_Click(object? sender, EventArgs e)
        {
            using var custForm = new CustomerForm();
            if (custForm.ShowDialog(this.FindForm() ?? this) == DialogResult.OK && custForm.NewCustomer != null)
            {
                await PopulateCustomersAsync(custForm.NewCustomer.CustomerId);
            }
        }

        private async void BtnSubmit_Click(object? sender, EventArgs e)
        {
            bool isValid = true;
            lblErrCustomer.Visible = false;
            lblErrCakeSize.Visible = false;
            lblErrTotal.Visible = false;

            if (cbCustomer.SelectedItem == null)
            {
                lblErrCustomer.Text = "Customer is required";
                lblErrCustomer.Visible = true;
                isValid = false;
            }

            if (string.IsNullOrWhiteSpace(cbCakeSize.Text))
            {
                lblErrCakeSize.Text = "Cake size is required";
                lblErrCakeSize.Visible = true;
                isValid = false;
            }

            string totalStr = txtTotalAmount.Text.Replace("₱", "").Replace(",", "").Trim();
            if (string.IsNullOrWhiteSpace(totalStr) || !decimal.TryParse(totalStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal totalAmount) || totalAmount <= 0)
            {
                lblErrTotal.Text = "Enter valid amount";
                lblErrTotal.Visible = true;
                isValid = false;
            }
            else
            {
                OrderTotalAmount = totalAmount;
            }

            if (!isValid) return;

            int customerId = OrderCustomerId;

            if (_isEditMode && _existingOrder != null)
            {
                // =====================================================
                // UPDATE ORDER
                // =====================================================
                _existingOrder.CustomerId = customerId;
                _existingOrder.CakeSize = cbCakeSize.Text.Trim();
                _existingOrder.Flavor = cbFlavor.Text.Trim();
                _existingOrder.DesignTheme = txtDesignTheme.Text.Trim();
                _existingOrder.TotalAmount = OrderTotalAmount;
                _existingOrder.DeliveryDate = dtDeliveryDate.Value;
                _existingOrder.StatusId = OrderStatusId;
                _existingOrder.Notes = txtNotes.Text.Trim();

                try
                {
                    NewOrder = await CrmDataService.UpdateOrderAsync(_existingOrder);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to update order in database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            else
            {
                // =====================================================
                // CREATE ORDER
                // =====================================================
                var order = new SalesOrder
                {
                    CustomerId = customerId,
                    CreatedByUserId = CrmDataService.DefaultUserId,
                    CakeSize = cbCakeSize.Text.Trim(),
                    Flavor = cbFlavor.Text.Trim(),
                    DesignTheme = txtDesignTheme.Text.Trim(),
                    TotalAmount = OrderTotalAmount,
                    OrderDate = DateTime.UtcNow,
                    DeliveryDate = dtDeliveryDate.Value,
                    StatusId = OrderStatusId,
                    Notes = txtNotes.Text.Trim()
                };

                try
                {
                    NewOrder = await CrmDataService.CreateOrderAsync(order);
                    if (_sourceInquiry != null)
                    {
                        await CrmDataService.UpdateInquiryStatusAsync(_sourceInquiry.InquiryId, "Converted");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to create order in database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private async void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (_existingOrder == null) return;

            var confirm = MessageBox.Show(
                $"Are you sure you want to delete Order #{_existingOrder.OrderId}?\n\nThis action cannot be undone.",
                "Delete Order",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2
            );

            if (confirm != DialogResult.Yes) return;

            try
            {
                await CrmDataService.DeleteOrderAsync(_existingOrder.OrderId);
                IsDeleted = true;
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to delete order from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class CustomerItem
        {
            public int Id { get; }
            public string Name { get; }
            public string? Phone { get; }

            public CustomerItem(int id, string name, string? phone)
            {
                Id = id;
                Name = name;
                Phone = phone;
            }

            public override string ToString()
            {
                return string.IsNullOrWhiteSpace(Phone) ? Name : $"{Name} ({Phone})";
            }
        }
    }
}
