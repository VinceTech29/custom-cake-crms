using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.SuperAdmin.Businesses
{
    public class BusinessModal : Form
    {
        private static readonly Color ColorModalBg = Color.White;
        private static readonly Color ColorFieldBg = Color.FromArgb(246, 243, 239);
        private static readonly Color ColorLabel = Color.FromArgb(124, 108, 104);
        private static readonly Color ColorAsterisk = Color.FromArgb(156, 107, 115);
        private static readonly Color ColorPrimaryBtn = Color.FromArgb(140, 70, 90);
        private static readonly Color ColorCancelBtn = Color.FromArgb(239, 231, 223);

        private TextBox txtCompanyName = null!;
        private TextBox txtCompanyCode = null!;
        private TextBox txtContactEmail = null!;
        private TextBox txtContactPhone = null!;

        private TextBox txtAddressLine1 = null!;
        private TextBox txtCity = null!;
        private TextBox txtState = null!;
        private TextBox txtPostalCode = null!;
        private TextBox txtCountry = null!;

        private TextBox txtServerName = null!;
        private TextBox txtDatabaseName = null!;
        private CheckBox chkIsActive = null!;

        private GroupBox? grpAdmin;
        private TextBox? txtAdminFullName;
        private TextBox? txtAdminUsername;
        private TextBox? txtAdminEmail;
        private TextBox? txtAdminPassword;

        private Label lblError = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private readonly CompanyListItem? _existingCompany;
        private readonly bool _isEditMode;

        public BusinessModal(CompanyListItem? company = null)
        {
            _existingCompany = company;
            _isEditMode = company != null;

            InitializeModal();
            BuildContent();

            if (_isEditMode && company != null)
            {
                LoadExistingDataAsync(company.CompanyId);
            }
        }

        private void InitializeModal()
        {
            Text = _isEditMode ? "Edit Business" : "Add New Business";
            Size = new Size(640, 750);
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
                txtCompanyName.Focus();
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
                Text = _isEditMode ? "Edit Business & Tenant" : "Register New Business",
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Location = new Point(28, 20)
            };

            var btnClose = new Button
            {
                Text = "\u2715",
                Font = new Font(UITheme.FontSans, 11F),
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

            // Body
            var bodyPanel = new Panel
            {
                Location = new Point(28, 75),
                Size = new Size(Width - 56, Height - 160),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            int y = 5;
            int fullWidth = bodyPanel.Width - 25;
            int halfWidth = (fullWidth - 14) / 2;

            // Company Name
            bodyPanel.Controls.Add(CreateLabel("BUSINESS / COMPANY NAME", true, new Point(0, y)));
            y += 24;
            txtCompanyName = CreateTextBox("e.g. Sweet Delights Bakery", new Point(0, y), fullWidth);
            bodyPanel.Controls.Add(txtCompanyName);
            y += 48;

            // Company Code & Phone Row
            bodyPanel.Controls.Add(CreateLabel("COMPANY CODE", true, new Point(0, y)));
            bodyPanel.Controls.Add(CreateLabel("CONTACT PHONE", false, new Point(halfWidth + 14, y)));
            y += 24;
            txtCompanyCode = CreateTextBox("e.g. SDB01", new Point(0, y), halfWidth);
            txtContactPhone = CreateTextBox("e.g. +63 917 555 1234", new Point(halfWidth + 14, y), halfWidth);
            bodyPanel.Controls.Add(txtCompanyCode);
            bodyPanel.Controls.Add(txtContactPhone);
            y += 48;

            // Contact Email
            bodyPanel.Controls.Add(CreateLabel("CONTACT EMAIL", true, new Point(0, y)));
            y += 24;
            txtContactEmail = CreateTextBox("e.g. contact@sweetdelights.ph", new Point(0, y), fullWidth);
            bodyPanel.Controls.Add(txtContactEmail);
            y += 48;

            // Address Line 1
            bodyPanel.Controls.Add(CreateLabel("ADDRESS LINE 1", false, new Point(0, y)));
            y += 24;
            txtAddressLine1 = CreateTextBox("e.g. 123 Baker Street", new Point(0, y), fullWidth);
            bodyPanel.Controls.Add(txtAddressLine1);
            y += 48;

            // City & State Row
            bodyPanel.Controls.Add(CreateLabel("CITY", false, new Point(0, y)));
            bodyPanel.Controls.Add(CreateLabel("STATE / PROVINCE", false, new Point(halfWidth + 14, y)));
            y += 24;
            txtCity = CreateTextBox("e.g. Quezon City", new Point(0, y), halfWidth);
            txtState = CreateTextBox("e.g. Metro Manila", new Point(halfWidth + 14, y), halfWidth);
            bodyPanel.Controls.Add(txtCity);
            bodyPanel.Controls.Add(txtState);
            y += 48;

            // Postal Code & Country Row
            bodyPanel.Controls.Add(CreateLabel("POSTAL CODE", false, new Point(0, y)));
            bodyPanel.Controls.Add(CreateLabel("COUNTRY", false, new Point(halfWidth + 14, y)));
            y += 24;
            txtPostalCode = CreateTextBox("e.g. 1100", new Point(0, y), halfWidth);
            txtCountry = CreateTextBox("Philippines", new Point(halfWidth + 14, y), halfWidth);
            bodyPanel.Controls.Add(txtPostalCode);
            bodyPanel.Controls.Add(txtCountry);
            y += 48;

            // Tenant Database Server & Database Name
            bodyPanel.Controls.Add(CreateLabel("DATABASE SERVER", true, new Point(0, y)));
            bodyPanel.Controls.Add(CreateLabel("TENANT DATABASE NAME", true, new Point(halfWidth + 14, y)));
            y += 24;
            txtServerName = CreateTextBox("(localdb)\\MSSQLLocalDB", new Point(0, y), halfWidth);
            txtDatabaseName = CreateTextBox("e.g. SDB01_CRM", new Point(halfWidth + 14, y), halfWidth);
            bodyPanel.Controls.Add(txtServerName);
            bodyPanel.Controls.Add(txtDatabaseName);

            if (!_isEditMode)
            {
                txtCompanyCode.TextChanged += (s, e) =>
                {
                    string c = txtCompanyCode.Text.Trim();
                    if (!string.IsNullOrEmpty(c) && (string.IsNullOrWhiteSpace(txtDatabaseName.Text) || txtDatabaseName.Text.EndsWith("_CRM", StringComparison.OrdinalIgnoreCase)))
                    {
                        txtDatabaseName.Text = $"{c.ToUpperInvariant()}_CRM";
                    }
                };
            }
            y += 48;

            // Active Status Checkbox
            chkIsActive = new CheckBox
            {
                Text = "Active business (allows Business Admin, Managers, and Staff to log in)",
                Checked = true,
                Font = new Font(UITheme.FontSans, 9.5F),
                ForeColor = UITheme.TextDark,
                Location = new Point(0, y),
                AutoSize = true
            };
            bodyPanel.Controls.Add(chkIsActive);
            y += 36;

            // Initial Business Admin section (only when creating new business)
            if (!_isEditMode)
            {
                grpAdmin = new GroupBox
                {
                    Text = " Initial Business Admin Account ",
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                    ForeColor = ColorPrimaryBtn,
                    Location = new Point(0, y),
                    Size = new Size(fullWidth, 195),
                    Padding = new Padding(12)
                };

                int gy = 26;
                int gHalf = (fullWidth - 36) / 2;

                grpAdmin.Controls.Add(CreateLabel("ADMIN FULL NAME", true, new Point(12, gy)));
                grpAdmin.Controls.Add(CreateLabel("ADMIN USERNAME", true, new Point(gHalf + 24, gy)));
                gy += 22;
                txtAdminFullName = CreateTextBox("e.g. Maria Dela Cruz", new Point(12, gy), gHalf);
                txtAdminUsername = CreateTextBox("e.g. admin.sdb", new Point(gHalf + 24, gy), gHalf);
                grpAdmin.Controls.Add(txtAdminFullName);
                grpAdmin.Controls.Add(txtAdminUsername);
                gy += 44;

                grpAdmin.Controls.Add(CreateLabel("ADMIN EMAIL", true, new Point(12, gy)));
                grpAdmin.Controls.Add(CreateLabel("INITIAL PASSWORD", true, new Point(gHalf + 24, gy)));
                gy += 22;
                txtAdminEmail = CreateTextBox("e.g. admin@sweetdelights.ph", new Point(12, gy), gHalf);
                txtAdminPassword = CreateTextBox("Temp@12345", new Point(gHalf + 24, gy), gHalf);
                txtAdminPassword.UseSystemPasswordChar = true;
                grpAdmin.Controls.Add(txtAdminEmail);
                grpAdmin.Controls.Add(txtAdminPassword);

                bodyPanel.Controls.Add(grpAdmin);
                y += 205;
            }

            // Error label
            lblError = new Label
            {
                Text = string.Empty,
                Font = new Font(UITheme.FontSans, 9F),
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
                Text = _isEditMode ? "Save Changes" : "Create Business",
                Size = new Size(140, 42),
                Location = new Point(footerPanel.Width - 145, 15),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = ColorPrimaryBtn,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            UITheme.ApplyActionButton(btnSave, "\uE73E", 10);
            btnSave.Click += async (s, e) => await SaveBusinessAsync();

            footerPanel.Controls.Add(btnCancel);
            footerPanel.Controls.Add(btnSave);
            Controls.Add(footerPanel);
        }

        private Label CreateLabel(string text, bool isRequired, Point location)
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

        private TextBox CreateTextBox(string placeholder, Point location, int width)
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

        private async void LoadExistingDataAsync(int companyId)
        {
            try
            {
                var details = await CrmDataService.GetCompanyByIdAsync(companyId);
                if (details == null) return;

                txtCompanyName.Text = details.CompanyName;
                txtCompanyCode.Text = details.CompanyCode;
                txtContactEmail.Text = details.ContactEmail;
                txtContactPhone.Text = details.ContactPhone;

                txtAddressLine1.Text = details.AddressLine1;
                txtCity.Text = details.City;
                txtState.Text = details.State;
                txtPostalCode.Text = details.PostalCode;
                txtCountry.Text = details.Country;

                txtServerName.Text = details.ServerName;
                txtDatabaseName.Text = details.DatabaseName;
                chkIsActive.Checked = details.IsActive;
            }
            catch (Exception ex)
            {
                lblError.Text = $"Failed to load company details: {ex.Message}";
            }
        }

        private async Task SaveBusinessAsync()
        {
            lblError.Text = string.Empty;

            string name = txtCompanyName.Text.Trim();
            string code = txtCompanyCode.Text.Trim().ToUpperInvariant();
            string email = txtContactEmail.Text.Trim();
            string phone = txtContactPhone.Text.Trim();

            string addr1 = txtAddressLine1.Text.Trim();
            string city = txtCity.Text.Trim();
            string state = txtState.Text.Trim();
            string zip = txtPostalCode.Text.Trim();
            string country = txtCountry.Text.Trim();

            string srv = txtServerName.Text.Trim();
            string db = txtDatabaseName.Text.Trim();
            bool active = chkIsActive.Checked;

            if (string.IsNullOrWhiteSpace(name))
            {
                lblError.Text = "Business / Company Name is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(code))
            {
                lblError.Text = "Company Code is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                lblError.Text = "A valid Contact Email is required.";
                return;
            }

            btnSave.Enabled = false;

            try
            {
                if (_isEditMode && _existingCompany != null)
                {
                    await CrmDataService.UpdateCompanyAsync(
                        _existingCompany.CompanyId,
                        name,
                        code,
                        email,
                        phone,
                        addr1,
                        null,
                        city,
                        state,
                        zip,
                        country,
                        active,
                        srv,
                        db
                    );
                }
                else
                {
                    string? adminName = txtAdminFullName?.Text.Trim();
                    string? adminUser = txtAdminUsername?.Text.Trim();
                    string? adminMail = txtAdminEmail?.Text.Trim();
                    string? adminPass = txtAdminPassword?.Text.Trim();

                    await CrmDataService.CreateCompanyAsync(
                        name,
                        code,
                        email,
                        phone,
                        addr1,
                        null,
                        city,
                        state,
                        zip,
                        country,
                        active,
                        srv,
                        db,
                        adminName,
                        adminUser,
                        adminMail,
                        adminPass
                    );
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                lblError.Text = $"Failed to save business: {ex.Message}";
                btnSave.Enabled = true;
            }
        }
    }
}
