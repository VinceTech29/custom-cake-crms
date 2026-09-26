using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Retention
{
    public class EmailSettingsForm : Form
    {
        private ComboBox cmbPreset = null!;
        private CheckBox chkSimulation = null!;
        private TextBox txtHost = null!;
        private NumericUpDown numPort = null!;
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnTogglePassword = null!;
        private TextBox txtFromName = null!;
        private TextBox txtFromEmail = null!;
        private CheckBox chkSsl = null!;
        private Button btnTestConnection = null!;
        private Button btnSave = null!;
        private Button btnCancel = null!;
        private Label lblHelpTip = null!;

        public EmailSettingsForm()
        {
            InitializeComponent();
            LoadCurrentSettings();
        }

        private void InitializeComponent()
        {
            Text = "Email & SMTP Settings";
            Size = new Size(580, 580);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = UITheme.CreamBackground;

            var pnl = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(24),
                AutoScroll = true
            };

            var lblTitle = new Label
            {
                Text = "Email & SMTP Settings",
                Font = new Font(UITheme.FontSerif, 14F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Top,
                Height = 28,
                UseMnemonic = false
            };

            var lblDesc = new Label
            {
                Text = "Configure your email server to dispatch retention emails, or use Sandbox Mode for instant testing.",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 36,
                UseMnemonic = false
            };

            var card = new Panel
            {
                Dock = DockStyle.Top,
                Height = 380,
                BackColor = Color.White,
                Padding = new Padding(18)
            };
            card.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            };

            // Simulation checkbox
            chkSimulation = new CheckBox
            {
                Text = "Enable Sandbox / Simulation Mode (Recommended for testing without credentials)",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Location = new Point(18, 14),
                AutoSize = true
            };
            chkSimulation.CheckedChanged += (s, e) => UpdateFieldStates();

            // Preset selector
            var lblPreset = new Label { Text = "Email Provider Preset:", Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), ForeColor = UITheme.TextDark, Location = new Point(18, 44), AutoSize = true };
            cmbPreset = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9F),
                Location = new Point(18, 64),
                Width = 260
            };
            cmbPreset.Items.AddRange(new object[] { "Gmail (smtp.gmail.com)", "Outlook / Office 365", "Yahoo Mail", "Custom SMTP Server" });
            cmbPreset.SelectedIndexChanged += CmbPreset_SelectedIndexChanged;

            // Host & Port
            var lblHost = new Label { Text = "SMTP Host:", Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), ForeColor = UITheme.TextDark, Location = new Point(18, 100), AutoSize = true };
            txtHost = new TextBox { Font = new Font(UITheme.FontSans, 9F), Location = new Point(18, 120), Width = 260 };

            var lblPort = new Label { Text = "Port:", Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), ForeColor = UITheme.TextDark, Location = new Point(290, 100), AutoSize = true };
            numPort = new NumericUpDown { Font = new Font(UITheme.FontSans, 9F), Location = new Point(290, 120), Width = 90, Minimum = 1, Maximum = 65535, Value = 587 };

            chkSsl = new CheckBox { Text = "Enable SSL/TLS", Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), Location = new Point(395, 122), AutoSize = true, Checked = true };

            // Username
            var lblUser = new Label { Text = "Username / Email Address:", Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), ForeColor = UITheme.TextDark, Location = new Point(18, 154), AutoSize = true };
            txtUsername = new TextBox { Font = new Font(UITheme.FontSans, 9F), Location = new Point(18, 174), Width = 480, PlaceholderText = "e.g. yourshop@gmail.com" };

            // Password
            var lblPass = new Label { Text = "App Password (or SMTP Password):", Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), ForeColor = UITheme.TextDark, Location = new Point(18, 208), AutoSize = true };
            txtPassword = new TextBox { Font = new Font(UITheme.FontSans, 9F), Location = new Point(18, 228), Width = 420, UseSystemPasswordChar = true, PlaceholderText = "16-character App Password" };

            btnTogglePassword = new Button
            {
                Text = "\uD83D\uDC41",
                Font = new Font(UITheme.FontSans, 8F),
                Location = new Point(444, 226),
                Size = new Size(54, 26),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnTogglePassword.Click += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
                btnTogglePassword.Text = txtPassword.UseSystemPasswordChar ? "\uD83D\uDC41" : "\u274C";
            };

            // Sender Name & From Email
            var lblFromName = new Label { Text = "Sender Display Name:", Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), ForeColor = UITheme.TextDark, Location = new Point(18, 262), AutoSize = true };
            txtFromName = new TextBox { Font = new Font(UITheme.FontSans, 9F), Location = new Point(18, 282), Width = 230, Text = "Sweet Story Cake Shop" };

            var lblFromEmail = new Label { Text = "Sender Email (Optional):", Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), ForeColor = UITheme.TextDark, Location = new Point(260, 262), AutoSize = true };
            txtFromEmail = new TextBox { Font = new Font(UITheme.FontSans, 9F), Location = new Point(260, 282), Width = 238, PlaceholderText = "Leaves blank to use Username" };

            lblHelpTip = new Label
            {
                Text = "\u2139 Gmail Tip: Generate an 'App Password' under Google Account Security > 2-Step Verification > App passwords.",
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Italic),
                ForeColor = UITheme.TextMuted,
                Location = new Point(18, 320),
                Size = new Size(480, 36)
            };

            card.Controls.Add(chkSimulation);
            card.Controls.Add(lblPreset);
            card.Controls.Add(cmbPreset);
            card.Controls.Add(lblHost);
            card.Controls.Add(txtHost);
            card.Controls.Add(lblPort);
            card.Controls.Add(numPort);
            card.Controls.Add(chkSsl);
            card.Controls.Add(lblUser);
            card.Controls.Add(txtUsername);
            card.Controls.Add(lblPass);
            card.Controls.Add(txtPassword);
            card.Controls.Add(btnTogglePassword);
            card.Controls.Add(lblFromName);
            card.Controls.Add(txtFromName);
            card.Controls.Add(lblFromEmail);
            card.Controls.Add(txtFromEmail);
            card.Controls.Add(lblHelpTip);

            // Bottom Buttons
            var bottomBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 48,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(0, 8, 0, 0)
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Height = 36,
                Width = 90,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnCancel.Click += (s, e) => Close();

            btnSave = new Button
            {
                Text = "\uD83D\uDCBE Save & Apply",
                Height = 36,
                Width = 140,
                FlatStyle = FlatStyle.Flat,
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnTestConnection = new Button
            {
                Text = "\uD83E\uDDEA Test Connection",
                Height = 36,
                Width = 145,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(245, 240, 243),
                ForeColor = UITheme.PrimaryMauve,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(8, 0, 0, 0)
            };
            btnTestConnection.FlatAppearance.BorderColor = UITheme.PrimaryMauve;
            btnTestConnection.Click += BtnTestConnection_Click;

            bottomBar.Controls.Add(btnSave);
            bottomBar.Controls.Add(btnTestConnection);
            bottomBar.Controls.Add(btnCancel);

            pnl.Controls.Add(bottomBar);
            pnl.Controls.Add(card);
            pnl.Controls.Add(lblDesc);
            pnl.Controls.Add(lblTitle);

            Controls.Add(pnl);
        }

        private void LoadCurrentSettings()
        {
            var config = EmailService.GetSmtpConfig();
            txtHost.Text = config.Host;
            numPort.Value = config.Port > 0 ? config.Port : 587;
            txtUsername.Text = config.Username;
            txtPassword.Text = config.Password;
            txtFromName.Text = config.FromName;
            txtFromEmail.Text = config.FromEmail;
            chkSsl.Checked = config.EnableSsl;
            chkSimulation.Checked = config.SimulationMode;

            // Match preset
            if (config.Host.Contains("gmail", StringComparison.OrdinalIgnoreCase))
                cmbPreset.SelectedIndex = 0;
            else if (config.Host.Contains("office365", StringComparison.OrdinalIgnoreCase) || config.Host.Contains("outlook", StringComparison.OrdinalIgnoreCase))
                cmbPreset.SelectedIndex = 1;
            else if (config.Host.Contains("yahoo", StringComparison.OrdinalIgnoreCase))
                cmbPreset.SelectedIndex = 2;
            else
                cmbPreset.SelectedIndex = 3;

            UpdateFieldStates();
        }

        private void CmbPreset_SelectedIndexChanged(object? sender, EventArgs e)
        {
            switch (cmbPreset.SelectedIndex)
            {
                case 0: // Gmail
                    txtHost.Text = "smtp.gmail.com";
                    numPort.Value = 587;
                    chkSsl.Checked = true;
                    lblHelpTip.Text = "\u2139 Gmail Tip: Generate an 'App Password' under Google Account Security > 2-Step Verification > App passwords.";
                    break;
                case 1: // Outlook
                    txtHost.Text = "smtp.office365.com";
                    numPort.Value = 587;
                    chkSsl.Checked = true;
                    lblHelpTip.Text = "\u2139 Outlook Tip: Use your primary Microsoft account email address and account password.";
                    break;
                case 2: // Yahoo
                    txtHost.Text = "smtp.mail.yahoo.com";
                    numPort.Value = 587;
                    chkSsl.Checked = true;
                    lblHelpTip.Text = "\u2139 Yahoo Tip: Generate an App Password in Yahoo Account Security settings.";
                    break;
                case 3: // Custom
                    lblHelpTip.Text = "\u2139 Enter your custom mail server host, port, and authentication credentials.";
                    break;
            }
        }

        private void UpdateFieldStates()
        {
            bool isSim = chkSimulation.Checked;
            if (isSim)
            {
                chkSimulation.ForeColor = UITheme.StatusGreenFg;
                chkSimulation.Text = "Sandbox / Simulation Mode: ENABLED (Emails are simulated safely without SMTP credentials)";
            }
            else
            {
                chkSimulation.ForeColor = UITheme.PrimaryMauve;
                chkSimulation.Text = "Enable Sandbox / Simulation Mode (Test email sending without credentials)";
            }
        }

        private SmtpConfig BuildCurrentConfig()
        {
            return new SmtpConfig
            {
                Host = txtHost.Text.Trim(),
                Port = (int)numPort.Value,
                Username = txtUsername.Text.Trim(),
                Password = txtPassword.Text.Trim(),
                FromName = string.IsNullOrWhiteSpace(txtFromName.Text) ? "Sweet Story Cake Shop" : txtFromName.Text.Trim(),
                FromEmail = txtFromEmail.Text.Trim(),
                EnableSsl = chkSsl.Checked,
                SimulationMode = chkSimulation.Checked
            };
        }

        private async void BtnTestConnection_Click(object? sender, EventArgs e)
        {
            btnTestConnection.Enabled = false;
            btnTestConnection.Text = "Testing...";

            try
            {
                var config = BuildCurrentConfig();
                var (ok, msg) = await EmailService.TestSmtpConnectionAsync(config);
                if (ok)
                {
                    MessageBox.Show(msg, "Connection Test Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show(msg, "Connection Test Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Test error:\n{ex.Message}", "Connection Test Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestConnection.Enabled = true;
                btnTestConnection.Text = "\uD83E\uDDEA Test Connection";
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            var config = BuildCurrentConfig();
            EmailService.SaveSmtpConfig(config);

            MessageBox.Show(
                config.SimulationMode
                    ? "Email settings saved!\n\nSandbox / Simulation Mode is active. You can now test retention emails freely without any sending errors."
                    : "Email settings saved!\n\nReal SMTP dispatch is configured and active.",
                "Settings Applied",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            DialogResult = DialogResult.OK;
            Close();
        }

        public static void ShowSettingsDialog(IWin32Window? owner = null)
        {
            using var dlg = new EmailSettingsForm();
            dlg.ShowDialog(owner);
        }
    }
}
