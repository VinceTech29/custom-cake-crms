using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Admin.Users
{
    /// <summary>
    /// Modal dialog for creating and editing users (Business Admin, Manager, Staff).
    /// Follows the design system: 16px rounded corners, maroon accents, and smooth input controls.
    /// </summary>
    public class UserModal : Form
    {
        private static readonly Color ColorModalBg = Color.White;
        private static readonly Color ColorFieldBg = Color.FromArgb(246, 243, 239);    // #F6F3EF
        private static readonly Color ColorBorder = Color.FromArgb(213, 205, 197);     // Subtle border
        private static readonly Color ColorLabel = Color.FromArgb(124, 108, 104);       // Uppercase Gray
        private static readonly Color ColorAsterisk = Color.FromArgb(156, 107, 115);   // Mauve
        private static readonly Color ColorPrimaryBtn = Color.FromArgb(140, 70, 90);    // Maroon #8C465A
        private static readonly Color ColorCancelBtn = Color.FromArgb(239, 231, 223);   // Beige #EFE7DF

        private TextBox txtFirstName = null!;
        private TextBox txtLastName = null!;
        private TextBox txtUsername = null!;
        private TextBox txtEmail = null!;
        private TextBox txtPhone = null!;
        private ComboBox cmbRole = null!;
        private TextBox txtPassword = null!;
        private CheckBox chkIsActive = null!;

        private Label lblError = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private readonly SystemUser? _existingUser;
        private readonly bool _isEditMode;

        public SystemUser? ResultUser { get; private set; }

        public UserModal(SystemUser? user = null)
        {
            _existingUser = user;
            _isEditMode = user != null;

            InitializeModal();
            BuildContent();

            if (_isEditMode && user != null)
            {
                PopulateExistingData(user);
            }
        }

        private void InitializeModal()
        {
            Text = _isEditMode ? "Edit User" : "Add New User";
            Size = new Size(580, 700);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ColorModalBg;
            DoubleBuffered = true;
            ShowInTaskbar = false;

            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(ColorPrimaryBtn, 2.5f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(1, 1, Width - 3, Height - 3), 16);
            };

            Load += (s, e) =>
            {
                this.ApplyRoundedRegion(16);
                txtFirstName.Focus();
            };
        }

        private void BuildContent()
        {
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Padding = new Padding(28, 20, 24, 0),
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = _isEditMode ? "Edit Team Member" : "Add New User",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Location = new Point(28, 20)
            };

            var btnClose = new Button
            {
                Text = "\u2715",
                Font = new Font(UITheme.FontSans, 11F, FontStyle.Regular),
                ForeColor = Color.FromArgb(140, 120, 115),
                Size = new Size(32, 32),
                Location = new Point(Width - 56, 18),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(246, 243, 239)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.ApplyRoundedRegion(16);
            btnClose.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(btnClose);
            Controls.Add(headerPanel);

            // Scrollable body panel
            var bodyPanel = new Panel
            {
                Location = new Point(28, 75),
                Size = new Size(Width - 56, Height - 160),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            int y = 5;

            // Name Row: First Name & Last Name side by side
            var lblName = CreateFieldLabel("FULL NAME", true);
            lblName.Location = new Point(0, y);
            bodyPanel.Controls.Add(lblName);
            y += 24;

            int colWidth = (bodyPanel.Width - 14) / 2;
            txtFirstName = CreateStyledTextBox("First Name");
            txtFirstName.Location = new Point(0, y);
            txtFirstName.Width = colWidth;
            bodyPanel.Controls.Add(txtFirstName);

            txtLastName = CreateStyledTextBox("Last Name");
            txtLastName.Location = new Point(colWidth + 14, y);
            txtLastName.Width = colWidth;
            bodyPanel.Controls.Add(txtLastName);
            y += 50;

            // Username
            var lblUser = CreateFieldLabel("USERNAME", true);
            lblUser.Location = new Point(0, y);
            bodyPanel.Controls.Add(lblUser);
            y += 24;

            txtUsername = CreateStyledTextBox("e.g. lea.abad");
            txtUsername.Location = new Point(0, y);
            txtUsername.Width = bodyPanel.Width - 5;
            bodyPanel.Controls.Add(txtUsername);
            y += 50;

            // Email
            var lblEmail = CreateFieldLabel("EMAIL ADDRESS", true);
            lblEmail.Location = new Point(0, y);
            bodyPanel.Controls.Add(lblEmail);
            y += 24;

            txtEmail = CreateStyledTextBox("e.g. user@customcakes.ph");
            txtEmail.Location = new Point(0, y);
            txtEmail.Width = bodyPanel.Width - 5;
            bodyPanel.Controls.Add(txtEmail);
            y += 50;

            // Phone
            var lblPhone = CreateFieldLabel("PHONE NUMBER", false);
            lblPhone.Location = new Point(0, y);
            bodyPanel.Controls.Add(lblPhone);
            y += 24;

            txtPhone = CreateStyledTextBox("e.g. +63 917 123 4567");
            txtPhone.Location = new Point(0, y);
            txtPhone.Width = bodyPanel.Width - 5;
            bodyPanel.Controls.Add(txtPhone);
            y += 50;

            // Role Dropdown
            var lblRole = CreateFieldLabel("ROLE ASSIGNMENT", true);
            lblRole.Location = new Point(0, y);
            bodyPanel.Controls.Add(lblRole);
            y += 24;

            cmbRole = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 10F),
                Location = new Point(0, y),
                Width = bodyPanel.Width - 5,
                Height = 36,
                BackColor = ColorFieldBg,
                FlatStyle = FlatStyle.Flat
            };
            cmbRole.Items.AddRange(new object[] { "Staff", "Manager", "Business Admin" });
            cmbRole.SelectedIndex = 0;
            bodyPanel.Controls.Add(cmbRole);
            y += 50;

            // Password
            var lblPass = CreateFieldLabel(_isEditMode ? "PASSWORD (LEAVE BLANK TO KEEP CURRENT)" : "INITIAL PASSWORD", !_isEditMode);
            lblPass.Location = new Point(0, y);
            bodyPanel.Controls.Add(lblPass);
            y += 24;

            txtPassword = CreateStyledTextBox(_isEditMode ? "Optional new password" : "Minimum 6 characters");
            txtPassword.UseSystemPasswordChar = true;
            txtPassword.Location = new Point(0, y);
            txtPassword.Width = bodyPanel.Width - 5;
            bodyPanel.Controls.Add(txtPassword);
            y += 50;

            // Status Checkbox
            chkIsActive = new CheckBox
            {
                Text = "Active user (grants immediate access to sign in)",
                Checked = true,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                Location = new Point(0, y),
                AutoSize = true
            };
            bodyPanel.Controls.Add(chkIsActive);
            y += 35;

            // Error Label
            lblError = new Label
            {
                Text = string.Empty,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(197, 48, 48),
                Location = new Point(0, y),
                AutoSize = true
            };
            bodyPanel.Controls.Add(lblError);

            Controls.Add(bodyPanel);

            // Footer Panel (Buttons)
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 75,
                Padding = new Padding(28, 12, 28, 20),
                BackColor = Color.Transparent
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 42),
                Location = new Point(footerPanel.Width - 250, 15),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = ColorCancelBtn,
                ForeColor = UITheme.TextDark,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.ApplyRoundedRegion(10);
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            btnSave = new Button
            {
                Text = _isEditMode ? "Save Changes" : "Add User",
                Size = new Size(125, 42),
                Location = new Point(footerPanel.Width - 130, 15),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = ColorPrimaryBtn,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            UITheme.ApplyActionButton(btnSave, "\uE73E", 10);
            btnSave.Click += async (s, e) => await SaveUserAsync();

            footerPanel.Controls.Add(btnCancel);
            footerPanel.Controls.Add(btnSave);
            Controls.Add(footerPanel);
        }

        private Label CreateFieldLabel(string text, bool isRequired)
        {
            var lbl = new Label
            {
                Text = isRequired ? $"{text} *" : text,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = isRequired ? ColorAsterisk : ColorLabel,
                AutoSize = true
            };
            return lbl;
        }

        private TextBox CreateStyledTextBox(string placeholder)
        {
            var tb = new TextBox
            {
                Font = new Font(UITheme.FontSans, 10F),
                BackColor = ColorFieldBg,
                BorderStyle = BorderStyle.FixedSingle,
                Height = 34
            };
            return tb;
        }

        private void PopulateExistingData(SystemUser user)
        {
            txtFirstName.Text = user.FirstName;
            txtLastName.Text = user.LastName;
            txtUsername.Text = user.Username;
            txtEmail.Text = user.Email;
            txtPhone.Text = user.Phone;
            chkIsActive.Checked = user.IsActive;

            if (user.Role != null)
            {
                if (user.Role.RoleName == "Business Admin" || user.Role.RoleName == "Admin")
                    cmbRole.SelectedItem = "Business Admin";
                else if (user.Role.RoleName == "Manager")
                    cmbRole.SelectedItem = "Manager";
                else
                    cmbRole.SelectedItem = "Staff";
            }
            else if (user.RoleId == 2)
            {
                cmbRole.SelectedItem = "Business Admin";
            }
            else if (user.RoleId == 3)
            {
                cmbRole.SelectedItem = "Manager";
            }
            else
            {
                cmbRole.SelectedItem = "Staff";
            }
        }

        private async System.Threading.Tasks.Task SaveUserAsync()
        {
            lblError.Text = string.Empty;

            string first = txtFirstName.Text.Trim();
            string last = txtLastName.Text.Trim();
            string username = txtUsername.Text.Trim();
            string email = txtEmail.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string pass = txtPassword.Text.Trim();
            string selectedRole = cmbRole.SelectedItem?.ToString() ?? "Staff";

            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(last))
            {
                lblError.Text = "First and last name are required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                lblError.Text = "Username is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                lblError.Text = "Please enter a valid email address.";
                return;
            }

            if (!_isEditMode && string.IsNullOrWhiteSpace(pass))
            {
                lblError.Text = "Password is required for new users.";
                return;
            }

            int roleId = selectedRole switch
            {
                "Business Admin" => 2,
                "Manager" => 3,
                _ => 4
            };

            btnSave.Enabled = false;

            try
            {
                if (_isEditMode && _existingUser != null)
                {
                    _existingUser.FirstName = first;
                    _existingUser.LastName = last;
                    _existingUser.Username = username;
                    _existingUser.Email = email;
                    _existingUser.Phone = phone;
                    _existingUser.RoleId = roleId;
                    _existingUser.IsActive = chkIsActive.Checked;

                    ResultUser = await CrmDataService.UpdateUserAsync(_existingUser, string.IsNullOrEmpty(pass) ? null : pass);
                }
                else
                {
                    var newUser = new SystemUser
                    {
                        CompanyId = CrmDataService.DefaultCompanyId,
                        RoleId = roleId,
                        FirstName = first,
                        LastName = last,
                        Username = username,
                        Email = email,
                        Phone = phone,
                        IsActive = chkIsActive.Checked,
                        CreatedDate = DateTime.UtcNow
                    };

                    ResultUser = await CrmDataService.CreateUserAsync(newUser, pass);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Failed to save user: {ex.Message}";
                btnSave.Enabled = true;
            }
        }
    }
}
