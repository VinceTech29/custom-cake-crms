using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Admin.Branches
{
    /// <summary>
    /// Modal dialog for creating and editing Branches under a tenant company.
    /// Strictly respects subscription plan branching capabilities.
    /// </summary>
    public class BranchModal : Form
    {
        private static readonly Color ColorModalBg = Color.White;
        private static readonly Color ColorFieldBg = Color.FromArgb(246, 243, 239);
        private static readonly Color ColorBorder = Color.FromArgb(213, 205, 197);
        private static readonly Color ColorLabel = Color.FromArgb(124, 108, 104);
        private static readonly Color ColorAsterisk = Color.FromArgb(156, 107, 115);
        private static readonly Color ColorPrimaryBtn = Color.FromArgb(140, 70, 90);
        private static readonly Color ColorCancelBtn = Color.FromArgb(239, 231, 223);

        private TextBox txtBranchName = null!;
        private TextBox txtBranchCode = null!;
        private TextBox txtAddress = null!;
        private TextBox txtPhone = null!;
        private TextBox txtEmail = null!;
        private TextBox txtManagerName = null!;
        private CheckBox? chkIsActive;

        private Label lblError = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;

        private readonly Branch? _existingBranch;
        private readonly bool _isEditMode;

        public Branch? ResultBranch { get; private set; }

        public BranchModal(Branch? branch = null)
        {
            _existingBranch = branch;
            _isEditMode = branch != null;

            InitializeModal();
            BuildContent();

            if (_isEditMode && branch != null)
            {
                PopulateExistingData(branch);
            }
        }

        private void InitializeModal()
        {
            Text = _isEditMode ? "Edit Branch" : "Add New Branch";
            Size = new Size(580, 600);
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
                txtBranchName.Focus();
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
                Text = _isEditMode ? "Edit Branch Information" : "Add New Branch",
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
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

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
                Text = _isEditMode ? "Save Changes" : "Create Branch",
                Size = new Size(140, 42),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = ColorPrimaryBtn,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 0)
            };
            UITheme.ApplyActionButton(btnSave, "\uE73E", 10);
            btnSave.Click += async (s, e) => await HandleSaveAsync();

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
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

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

            // 1. Branch Name (Full Width)
            bodyPanel.Controls.Add(CreateLabel("BRANCH NAME", true, new Point(28, y)));
            y += 24;
            txtBranchName = CreateTextBox("e.g., Calinan Branch", new Point(28, y), fullWidth);
            bodyPanel.Controls.Add(txtBranchName);
            y += 48;

            // 2. Branch Code & Manager / Supervisor (2 columns)
            bodyPanel.Controls.Add(CreateLabel("BRANCH CODE (e.g., CLN, MTN)", true, new Point(28, y)));
            bodyPanel.Controls.Add(CreateLabel("BRANCH MANAGER / SUPERVISOR", false, new Point(28 + halfWidth + 14, y)));
            y += 24;
            txtBranchCode = CreateTextBox("e.g., CLN", new Point(28, y), halfWidth);
            txtBranchCode.CharacterCasing = CharacterCasing.Upper;
            txtManagerName = CreateTextBox("e.g., Jane Doe", new Point(28 + halfWidth + 14, y), halfWidth);
            bodyPanel.Controls.Add(txtBranchCode);
            bodyPanel.Controls.Add(txtManagerName);
            y += 48;

            // 3. Location / Address (Full Width)
            bodyPanel.Controls.Add(CreateLabel("LOCATION / PHYSICAL ADDRESS", false, new Point(28, y)));
            y += 24;
            txtAddress = CreateTextBox("e.g., McArthur Highway, Calinan, Davao City", new Point(28, y), fullWidth);
            bodyPanel.Controls.Add(txtAddress);
            y += 48;

            // 4. Contact Phone & Email (2 columns)
            bodyPanel.Controls.Add(CreateLabel("CONTACT PHONE", false, new Point(28, y)));
            bodyPanel.Controls.Add(CreateLabel("CONTACT EMAIL", false, new Point(28 + halfWidth + 14, y)));
            y += 24;
            txtPhone = CreateTextBox("e.g., +63 917 123 4567", new Point(28, y), halfWidth);
            txtEmail = CreateTextBox("e.g., calinan@dreamcakes.com", new Point(28 + halfWidth + 14, y), halfWidth);
            bodyPanel.Controls.Add(txtPhone);
            bodyPanel.Controls.Add(txtEmail);
            y += 48;

            // 5. Active Branch Checkbox (in edit mode)
            if (_isEditMode)
            {
                chkIsActive = new CheckBox
                {
                    Text = "Active Branch (Available for operational orders & transactions)",
                    Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                    ForeColor = UITheme.TextDark,
                    AutoSize = true,
                    Location = new Point(28, y),
                    Checked = true,
                    Cursor = Cursors.Hand
                };
                bodyPanel.Controls.Add(chkIsActive);
                y += 36;
            }

            // 6. Error Label
            lblError = new Label
            {
                Text = "",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(197, 48, 48),
                AutoSize = true,
                Location = new Point(28, y),
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

        private void PopulateExistingData(Branch branch)
        {
            txtBranchName.Text = branch.BranchName;
            txtBranchCode.Text = branch.BranchCode;
            txtAddress.Text = branch.Address ?? string.Empty;
            txtPhone.Text = branch.ContactPhone ?? string.Empty;
            txtEmail.Text = branch.ContactEmail ?? string.Empty;
            txtManagerName.Text = branch.ManagerName ?? string.Empty;

            if (chkIsActive != null)
            {
                chkIsActive.Checked = branch.IsActive;
            }
        }

        private async Task HandleSaveAsync()
        {
            lblError.Visible = false;
            string branchName = txtBranchName.Text.Trim();
            string branchCode = txtBranchCode.Text.Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(branchName))
            {
                ShowError("Please enter a Branch Name.");
                txtBranchName.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(branchCode))
            {
                ShowError("Please enter a unique Branch Code.");
                txtBranchCode.Focus();
                return;
            }

            if (branchCode.Length < 2 || branchCode.Length > 10)
            {
                ShowError("Branch Code must be between 2 and 10 characters.");
                txtBranchCode.Focus();
                return;
            }

            btnSave.Enabled = false;
            btnCancel.Enabled = false;
            btnSave.Text = "Saving...";

            try
            {
                if (_isEditMode && _existingBranch != null)
                {
                    _existingBranch.BranchName = branchName;
                    _existingBranch.BranchCode = branchCode;
                    _existingBranch.Address = txtAddress.Text.Trim();
                    _existingBranch.ContactPhone = txtPhone.Text.Trim();
                    _existingBranch.ContactEmail = txtEmail.Text.Trim();
                    _existingBranch.ManagerName = txtManagerName.Text.Trim();
                    if (chkIsActive != null)
                    {
                        _existingBranch.IsActive = chkIsActive.Checked;
                    }

                    ResultBranch = await CrmDataService.UpdateBranchAsync(_existingBranch);
                }
                else
                {
                    var newBranch = new Branch
                    {
                        CompanyId = SessionService.CurrentUser?.CompanyId ?? CrmDataService.DefaultCompanyId,
                        BranchName = branchName,
                        BranchCode = branchCode,
                        Address = txtAddress.Text.Trim(),
                        ContactPhone = txtPhone.Text.Trim(),
                        ContactEmail = txtEmail.Text.Trim(),
                        ManagerName = txtManagerName.Text.Trim(),
                        IsActive = true
                    };

                    ResultBranch = await CrmDataService.CreateBranchAsync(newBranch);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (InvalidOperationException ex)
            {
                ShowError(ex.Message);
            }
            catch (Exception ex)
            {
                ShowError($"Failed to save branch: {ex.Message}");
            }
            finally
            {
                btnSave.Enabled = true;
                btnCancel.Enabled = true;
                btnSave.Text = _isEditMode ? "Save Changes" : "Create Branch";
            }
        }

        private void ShowError(string msg)
        {
            lblError.Text = msg;
            lblError.Visible = true;
        }
    }
}
