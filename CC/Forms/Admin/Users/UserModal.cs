using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
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
        private ComboBox cmbBranch = null!;
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

            PopulateBranchesAsync();
        }

        private void InitializeModal()
        {
            Text = _isEditMode ? "Edit User" : "Add New User";
            Size = new Size(580, 680);
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
            // 1. FIXED HEADER
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Padding = new Padding(28, 18, 24, 0),
                BackColor = ColorModalBg
            };

            headerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(238, 233, 228), 1f);
                e.Graphics.DrawLine(pen, 28, headerPanel.Height - 1, headerPanel.Width - 28, headerPanel.Height - 1);
            };

            var lblTitle = new Label
            {
                Text = _isEditMode ? "Edit Team Member" : "Add New User",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Location = new Point(28, 18)
            };

            var btnClose = new Button
            {
                Text = "\u2715",
                Font = new Font(UITheme.FontSans, 11F),
                ForeColor = Color.FromArgb(140, 120, 115),
                Size = new Size(32, 32),
                Location = new Point(Width - 56, 16),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
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

            // 2. FIXED FOOTER
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 72,
                BackColor = ColorModalBg
            };

            footerPanel.Paint += (s, e) =>
            {
                using var pen = new Pen(Color.FromArgb(238, 233, 228), 1f);
                e.Graphics.DrawLine(pen, 28, 0, footerPanel.Width - 28, 0);
            };

            var footerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                BackColor = ColorModalBg,
                Padding = new Padding(0, 15, 28, 15),
                WrapContents = false
            };

            btnSave = new Button
            {
                Text = _isEditMode ? "Save Changes" : "Add User",
                Size = new Size(135, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = ColorPrimaryBtn,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 0)
            };
            UITheme.ApplyActionButton(btnSave, "\uE73E", 10);
            btnSave.Click += async (s, e) => await SaveUserAsync();

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = ColorCancelBtn,
                ForeColor = UITheme.TextDark,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 12, 0)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.ApplyRoundedRegion(10);
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            footerFlow.Controls.Add(btnSave);
            footerFlow.Controls.Add(btnCancel);
            footerPanel.Controls.Add(footerFlow);

            // 3. SCROLLABLE CONTENT BODY
            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = ColorModalBg,
                Padding = new Padding(28, 16, 28, 16)
            };

            int y = 14;
            int fullWidth = 504;
            int halfWidth = (fullWidth - 14) / 2;

            // 1. First Name & Last Name (Separate explicit labels!)
            bodyPanel.Controls.Add(CreateFieldLabel("FIRST NAME", true, new Point(28, y)));
            bodyPanel.Controls.Add(CreateFieldLabel("LAST NAME", true, new Point(28 + halfWidth + 14, y)));
            y += 24;

            txtFirstName = CreateStyledTextBox("e.g. Maria", new Point(28, y), halfWidth);
            txtLastName = CreateStyledTextBox("e.g. Santos", new Point(28 + halfWidth + 14, y), halfWidth);
            bodyPanel.Controls.Add(txtFirstName);
            bodyPanel.Controls.Add(txtLastName);
            y += 48;

            // 2. Username (Full Width)
            bodyPanel.Controls.Add(CreateFieldLabel("USERNAME", true, new Point(28, y)));
            y += 24;

            txtUsername = CreateStyledTextBox("e.g. maria.santos", new Point(28, y), fullWidth);
            bodyPanel.Controls.Add(txtUsername);
            y += 48;

            // 3. Email Address & Phone Number (2 columns)
            bodyPanel.Controls.Add(CreateFieldLabel("EMAIL ADDRESS", true, new Point(28, y)));
            bodyPanel.Controls.Add(CreateFieldLabel("PHONE NUMBER", false, new Point(28 + halfWidth + 14, y)));
            y += 24;

            txtEmail = CreateStyledTextBox("e.g. maria@customcakes.ph", new Point(28, y), halfWidth);
            txtPhone = CreateStyledTextBox("e.g. +63 917 123 4567", new Point(28 + halfWidth + 14, y), halfWidth);
            bodyPanel.Controls.Add(txtEmail);
            bodyPanel.Controls.Add(txtPhone);
            y += 48;

            // 4. Role Assignment & Branch Assignment (2 columns)
            bodyPanel.Controls.Add(CreateFieldLabel("ROLE ASSIGNMENT", true, new Point(28, y)));
            bodyPanel.Controls.Add(CreateFieldLabel("BRANCH ASSIGNMENT", false, new Point(28 + halfWidth + 14, y)));
            y += 24;

            cmbRole = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 10F),
                Location = new Point(28, y),
                Width = halfWidth,
                Height = 34,
                BackColor = ColorFieldBg,
                FlatStyle = FlatStyle.Flat
            };
            cmbRole.Items.AddRange(new object[] { "Staff", "Manager", "Business Admin" });
            cmbRole.SelectedIndex = 0;
            bodyPanel.Controls.Add(cmbRole);

            cmbBranch = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 10F),
                Location = new Point(28 + halfWidth + 14, y),
                Width = halfWidth,
                Height = 34,
                BackColor = ColorFieldBg,
                FlatStyle = FlatStyle.Flat
            };
            bodyPanel.Controls.Add(cmbBranch);
            y += 48;

            // 5. Password (Full Width)
            bodyPanel.Controls.Add(CreateFieldLabel(_isEditMode ? "PASSWORD (LEAVE BLANK TO KEEP CURRENT)" : "INITIAL PASSWORD", !_isEditMode, new Point(28, y)));
            y += 24;

            txtPassword = CreateStyledTextBox(_isEditMode ? "Optional new password" : "Minimum 6 characters", new Point(28, y), fullWidth);
            txtPassword.UseSystemPasswordChar = true;
            bodyPanel.Controls.Add(txtPassword);
            y += 48;

            // 6. Active Checkbox
            chkIsActive = new CheckBox
            {
                Text = "Active user (grants immediate access to sign in)",
                Checked = true,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                Location = new Point(28, y),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            bodyPanel.Controls.Add(chkIsActive);
            y += 36;

            // 7. Error Label
            lblError = new Label
            {
                Text = string.Empty,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(197, 48, 48),
                Location = new Point(28, y),
                AutoSize = true,
                Visible = false
            };
            bodyPanel.Controls.Add(lblError);
            y += 30;

            bodyPanel.AutoScrollMinSize = new Size(0, y);

            // Add docked panels in correct WinForms z-order
            Controls.Add(bodyPanel);
            Controls.Add(headerPanel);
            Controls.Add(footerPanel);
            bodyPanel.BringToFront();
        }

        private Label CreateFieldLabel(string text, bool isRequired, Point location)
        {
            return new Label
            {
                Text = isRequired ? $"{text} *" : text,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = isRequired ? ColorAsterisk : ColorLabel,
                Location = location,
                AutoSize = true
            };
        }

        private TextBox CreateStyledTextBox(string placeholder, Point location, int width)
        {
            return new TextBox
            {
                Font = new Font(UITheme.FontSans, 10F),
                BackColor = ColorFieldBg,
                BorderStyle = BorderStyle.FixedSingle,
                Height = 34,
                Location = location,
                Width = width,
                PlaceholderText = placeholder
            };
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

        private async Task SaveUserAsync()
        {
            lblError.Text = string.Empty;
            lblError.Visible = false;

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
                lblError.Visible = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                lblError.Text = "Username is required.";
                lblError.Visible = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                lblError.Text = "Please enter a valid email address.";
                lblError.Visible = true;
                return;
            }

            if (!_isEditMode && string.IsNullOrWhiteSpace(pass))
            {
                lblError.Text = "Password is required for new users.";
                lblError.Visible = true;
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

                    var selBranch = cmbBranch.SelectedItem as BranchComboItem;
                    _existingUser.BranchId = selBranch?.BranchId;

                    ResultUser = await CrmDataService.UpdateUserAsync(_existingUser, string.IsNullOrEmpty(pass) ? null : pass);
                }
                else
                {
                    var selBranch = cmbBranch.SelectedItem as BranchComboItem;
                    var newUser = new SystemUser
                    {
                        CompanyId = SessionService.CurrentUser?.CompanyId > 0 ? SessionService.CurrentUser.CompanyId : CrmDataService.DefaultCompanyId,
                        RoleId = roleId,
                        BranchId = selBranch?.BranchId,
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
                lblError.Visible = true;
                btnSave.Enabled = true;
            }
        }

        private async void PopulateBranchesAsync()
        {
            try
            {
                cmbBranch.Items.Clear();
                var allItem = new BranchComboItem { BranchId = null, DisplayText = "All Branches / Head Office" };
                cmbBranch.Items.Add(allItem);

                var activeBranches = await CrmDataService.GetActiveBranchesAsync();
                BranchComboItem? selected = null;

                foreach (var b in activeBranches)
                {
                    var item = new BranchComboItem
                    {
                        BranchId = b.BranchId,
                        DisplayText = $"{b.BranchName} ({b.BranchCode})"
                    };
                    cmbBranch.Items.Add(item);

                    if (_existingUser?.BranchId == b.BranchId)
                    {
                        selected = item;
                    }
                }

                cmbBranch.SelectedItem = selected ?? allItem;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserModal.PopulateBranchesAsync] {ex.Message}");
            }
        }

        private class BranchComboItem
        {
            public int? BranchId { get; set; }
            public string DisplayText { get; set; } = string.Empty;
            public override string ToString() => DisplayText;
        }
    }
}
