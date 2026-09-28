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
        private CheckBox chkIsActive = null!;

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
            Size = new Size(560, 680);
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
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Padding = new Padding(28, 20, 24, 0),
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = _isEditMode ? "Edit Branch Information" : "Add New Branch",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Location = new Point(28, 20)
            };

            var btnClose = new Label
            {
                Text = "\u2715",
                Font = new Font(UITheme.FontSans, 12F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Cursor = Cursors.Hand,
                Location = new Point(Width - 45, 20)
            };
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(btnClose);

            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 10, 28, 10),
                BackColor = Color.Transparent,
                AutoScroll = true
            };

            int y = 6;

            // 1. Branch Name
            bodyPanel.Controls.Add(CreateFieldLabel("BRANCH NAME", true, ref y));
            txtBranchName = CreateFieldTextBox(ref y);
            bodyPanel.Controls.Add(txtBranchName);

            // 2. Branch Code
            bodyPanel.Controls.Add(CreateFieldLabel("BRANCH CODE (e.g., MAIN, CLN, MTN)", true, ref y));
            txtBranchCode = CreateFieldTextBox(ref y);
            txtBranchCode.CharacterCasing = CharacterCasing.Upper;
            bodyPanel.Controls.Add(txtBranchCode);

            // 3. Address
            bodyPanel.Controls.Add(CreateFieldLabel("LOCATION / PHYSICAL ADDRESS", false, ref y));
            txtAddress = CreateFieldTextBox(ref y);
            bodyPanel.Controls.Add(txtAddress);

            // 4. Branch Manager
            bodyPanel.Controls.Add(CreateFieldLabel("BRANCH MANAGER / SUPERVISOR", false, ref y));
            txtManagerName = CreateFieldTextBox(ref y);
            bodyPanel.Controls.Add(txtManagerName);

            // 5. Contact Phone & Email
            bodyPanel.Controls.Add(CreateFieldLabel("CONTACT PHONE", false, ref y));
            txtPhone = CreateFieldTextBox(ref y);
            bodyPanel.Controls.Add(txtPhone);

            bodyPanel.Controls.Add(CreateFieldLabel("CONTACT EMAIL", false, ref y));
            txtEmail = CreateFieldTextBox(ref y);
            bodyPanel.Controls.Add(txtEmail);

            // 6. Active Checkbox (Edit mode)
            if (_isEditMode)
            {
                chkIsActive = new CheckBox
                {
                    Text = "Active Branch (Available for operational transactions)",
                    Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                    ForeColor = UITheme.TextDark,
                    AutoSize = true,
                    Location = new Point(0, y),
                    Checked = true,
                    Cursor = Cursors.Hand
                };
                bodyPanel.Controls.Add(chkIsActive);
                y += 32;
            }

            // Error Label
            lblError = new Label
            {
                Text = "",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(197, 48, 48),
                AutoSize = true,
                Location = new Point(0, y),
                Visible = false
            };
            bodyPanel.Controls.Add(lblError);

            // Footer Panel (Buttons)
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 75,
                Padding = new Padding(28, 14, 28, 18),
                BackColor = Color.Transparent
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorCancelBtn,
                ForeColor = UITheme.TextDark,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(Width - 250, 16)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            btnSave = new Button
            {
                Text = _isEditMode ? "Save Changes" : "Create Branch",
                Size = new Size(125, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorPrimaryBtn,
                ForeColor = Color.White,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Location = new Point(Width - 130, 16)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += async (s, e) => await HandleSaveAsync();

            footerPanel.Controls.Add(btnCancel);
            footerPanel.Controls.Add(btnSave);

            Controls.Add(bodyPanel);
            Controls.Add(footerPanel);
            Controls.Add(headerPanel);
        }

        private Label CreateFieldLabel(string text, bool required, ref int currentY)
        {
            var lbl = new Label
            {
                Text = required ? text + " *" : text,
                Font = new Font(UITheme.FontSans, 7.5F, FontStyle.Bold),
                ForeColor = required ? ColorAsterisk : ColorLabel,
                AutoSize = true,
                Location = new Point(0, currentY)
            };
            currentY += 18;
            return lbl;
        }

        private TextBox CreateFieldTextBox(ref int currentY)
        {
            var txt = new TextBox
            {
                Font = new Font(UITheme.FontSans, 10F),
                BackColor = ColorFieldBg,
                ForeColor = UITheme.TextDark,
                BorderStyle = BorderStyle.FixedSingle,
                Size = new Size(BodyWidth, 34),
                Location = new Point(0, currentY)
            };
            currentY += 44;
            return txt;
        }

        private int BodyWidth => Width - 56;

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
