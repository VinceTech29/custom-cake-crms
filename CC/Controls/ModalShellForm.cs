using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Forms.Authentication;

namespace CC.Controls
{
    /// <summary>
    /// Base Modal Dialog Shell for Custom Cake CRM ("Sweet Story CRM").
    /// Inherited by all "New [X]" dialogs (Customer, Order, Payment, Follow-up, etc.) to guarantee
    /// consistent modal sizing, generous field padding, header/footer divider lines,
    /// circular close button, and inline validation error messages.
    /// </summary>
    public class ModalShellForm : Form
    {
        protected static readonly Color ModalBg = Color.White;
        protected static readonly Color FieldBg = Color.FromArgb(239, 230, 222);    // #EFE6DE
        protected static readonly Color FieldBgHover = Color.FromArgb(232, 222, 213);
        protected static readonly Color LabelColor = Color.FromArgb(124, 108, 104);   // Uppercase Gray #7C6C68
        protected static readonly Color AsteriskColor = Color.FromArgb(156, 107, 115); // Mauve #9C6B73
        protected static readonly Color BorderDivider = Color.FromArgb(239, 230, 222); // #EFE6DE Line
        protected static readonly Color ErrorColor = Color.FromArgb(197, 48, 48);      // Red #C53030

        protected Panel headerPanel = null!;
        protected Label lblHeaderTitle = null!;
        protected Button btnCloseHeader = null!;
        protected Panel headerDivider = null!;

        protected FlowLayoutPanel contentPanel = null!;

        protected Panel footerPanel = null!;
        protected Panel footerDivider = null!;
        protected Button btnCancel = null!;
        protected Button btnSubmit = null!;

        public ModalShellForm(string title, string submitText, int formWidth = 580, int formHeight = 680)
        {
            InitializeShell(title, submitText, formWidth, formHeight);
        }

        private void InitializeShell(string title, string submitText, int formWidth, int formHeight)
        {
            Text = title;
            Size = new Size(formWidth, formHeight);
            MinimumSize = new Size(540, 500);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = ModalBg;
            Font = new Font(UITheme.FontSans, 9.5F);
            DoubleBuffered = true;
            ShowInTaskbar = false;

            if (UITheme.AppIcon != null)
                Icon = UITheme.AppIcon;

            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(140, 70, 90), 2.5f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(1, 1, Width - 3, Height - 3), 16);
            };

            Load += (s, e) =>
            {
                this.CenterOnParentOrScreen();
                this.ApplyRoundedRegion(16);
            };
            Shown += (s, e) => this.CenterOnParentOrScreen();

            var mainTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = ModalBg,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 68F));  // Header
            mainTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));  // Scrollable Content
            mainTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));  // Footer

            // 1. HEADER PANEL (Responsive TableLayoutPanel to ensure Title and Close X never collide)
            headerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModalBg,
                Padding = new Padding(24, 0, 24, 0)
            };

            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = ModalBg
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            lblHeaderTitle = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSerif, 16F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                Margin = new Padding(0)
            };

            btnCloseHeader = new Button
            {
                Text = "\u2715", // X icon
                Size = new Size(34, 34),
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
                BackColor = Color.FromArgb(242, 236, 230),
                ForeColor = LabelColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 10F, FontStyle.Bold)
            };
            btnCloseHeader.FlatAppearance.BorderSize = 0;
            btnCloseHeader.ApplyRoundedRegion(17); // Circular X button
            btnCloseHeader.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            headerTable.Controls.Add(lblHeaderTitle, 0, 0);
            headerTable.Controls.Add(btnCloseHeader, 1, 0);

            headerDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = BorderDivider
            };

            Point dragStart = Point.Empty;
            headerPanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragStart = e.Location; };
            headerPanel.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left) Location = new Point(Location.X + (e.X - dragStart.X), Location.Y + (e.Y - dragStart.Y)); };
            headerTable.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragStart = e.Location; };
            headerTable.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left) Location = new Point(Location.X + (e.X - dragStart.X), Location.Y + (e.Y - dragStart.Y)); };
            lblHeaderTitle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragStart = e.Location; };
            lblHeaderTitle.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left) Location = new Point(Location.X + (e.X - dragStart.X), Location.Y + (e.Y - dragStart.Y)); };

            headerPanel.Controls.Add(headerTable);
            headerPanel.Controls.Add(headerDivider);

            // 2. CONTENT PANEL (FlowLayoutPanel for clean vertical field stacking & scroll safety)
            contentPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 16),
                BackColor = ModalBg
            };

            // 3. FOOTER PANEL (Right-aligned FlowLayoutPanel for Cancel & Submit buttons)
            footerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ModalBg,
                Padding = new Padding(24, 0, 24, 0)
            };

            footerDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = BorderDivider
            };

            var footerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 14, 0, 0),
                BackColor = ModalBg
            };

            btnSubmit = new Button
            {
                Text = submitText,
                Size = new Size(150, 42),
                Margin = new Padding(10, 0, 0, 0)
            };
            UITheme.ApplyPrimaryButton(btnSubmit, 10);

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 42),
                Margin = new Padding(0)
            };
            UITheme.ApplySecondaryButton(btnCancel, 10);
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            footerFlow.Controls.Add(btnSubmit);
            footerFlow.Controls.Add(btnCancel);

            footerPanel.Controls.Add(footerDivider);
            footerPanel.Controls.Add(footerFlow);

            mainTable.Controls.Add(headerPanel, 0, 0);
            mainTable.Controls.Add(contentPanel, 0, 1);
            mainTable.Controls.Add(footerPanel, 0, 2);

            Controls.Add(mainTable);

            AcceptButton = btnSubmit;
            CancelButton = btnCancel;
        }

        // =========================================================================
        // FORM FIELD BUILDER HELPERS
        // =========================================================================

        /// <summary>
        /// Wraps an input control with an uppercase label (and optional mauve asterisk) and an inline error label.
        /// </summary>
        protected Panel CreateFieldBlock(string labelText, bool isRequired, Control inputControl, out Label errorLabel, int blockWidth = 520)
        {
            var block = new Panel
            {
                Width = blockWidth,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14),
                BackColor = ModalBg
            };

            // Label Flow Layout to display Label Text + Mauve Asterisk
            var labelFlow = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                Margin = new Padding(0, 0, 0, 6),
                BackColor = ModalBg
            };

            var lblText = new Label
            {
                Text = labelText.ToUpper(),
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = LabelColor,
                AutoSize = true,
                Margin = new Padding(0)
            };
            labelFlow.Controls.Add(lblText);

            if (isRequired)
            {
                var lblAsterisk = new Label
                {
                    Text = " *",
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                    ForeColor = AsteriskColor,
                    AutoSize = true,
                    Margin = new Padding(0)
                };
                labelFlow.Controls.Add(lblAsterisk);
            }

            // Input background wrapper (#EFE6DE background, rounded corners)
            int inputHeight = inputControl.Height > 38 ? inputControl.Height : 38;
            var inputWrapper = new Panel
            {
                Location = new Point(0, 22),
                Size = new Size(blockWidth, inputHeight),
                BackColor = FieldBg
            };
            inputWrapper.ApplyRoundedRegion(10);

            inputControl.Dock = DockStyle.Fill;
            inputWrapper.Controls.Add(inputControl);

            // Inline Validation Error Label
            errorLabel = new Label
            {
                Text = "Required field",
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = ErrorColor,
                Location = new Point(2, 22 + inputHeight + 2),
                AutoSize = true,
                Visible = false
            };

            block.Controls.Add(labelFlow);
            block.Controls.Add(inputWrapper);
            block.Controls.Add(errorLabel);

            return block;
        }

        /// <summary>
        /// Creates a text input box with placeholder support and styled background.
        /// </summary>
        protected TextBox CreateStyledTextBox(string placeholder, bool multiline = false, int height = 38)
        {
            var txt = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = FieldBg,
                ForeColor = UITheme.TextDark,
                Font = new Font(UITheme.FontSans, 9.5F),
                PlaceholderText = placeholder,
                Multiline = multiline,
                Height = height
            };
            return txt;
        }

        /// <summary>
        /// Creates a styled ComboBox.
        /// </summary>
        protected ComboBox CreateStyledComboBox()
        {
            var combo = new ComboBox
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = FieldBg,
                ForeColor = UITheme.TextDark,
                Font = new Font(UITheme.FontSans, 9.5F),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            return combo;
        }

        /// <summary>
        /// Creates a styled DateTimePicker.
        /// </summary>
        protected DateTimePicker CreateStyledDatePicker()
        {
            var dtp = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                CalendarTitleBackColor = UITheme.PrimaryMauve,
                Font = new Font(UITheme.FontSans, 9.5F),
                Value = DateTime.Now
            };
            return dtp;
        }

        /// <summary>
        /// Combines two field blocks into a 2-column side-by-side layout row.
        /// </summary>
        protected Panel CreateTwoColumnRow(Panel leftBlock, Panel rightBlock, int rowWidth = 520)
        {
            int colGap = 16;
            int colWidth = (rowWidth - colGap) / 2;

            leftBlock.Width = colWidth;
            rightBlock.Width = colWidth;

            // Adjust input wrappers inside blocks
            foreach (Control c in leftBlock.Controls)
            {
                if (c is Panel p && p != leftBlock) p.Width = colWidth;
            }
            foreach (Control c in rightBlock.Controls)
            {
                if (c is Panel p && p != rightBlock) p.Width = colWidth;
            }

            var row = new TableLayoutPanel
            {
                Width = rowWidth,
                Height = Math.Max(leftBlock.Height, rightBlock.Height),
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 0, 14),
                BackColor = ModalBg
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            row.Controls.Add(leftBlock, 0, 0);
            row.Controls.Add(rightBlock, 1, 0);

            return row;
        }
    }
}
