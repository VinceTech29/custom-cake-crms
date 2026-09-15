using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Models;
using CC.Services;
using CC.Controls;

namespace CC.Forms.Authentication
{
    public class LoginForm : Form
    {
        // =========================================================
        // THEME & COLOR PALETTE
        // Matches target design: Mauve (#9C6B73) & Cream (#F7F1EA)
        // =========================================================

        private static readonly Color BrandColor = Color.FromArgb(156, 107, 115);      // #9C6B73 Solid Mauve/Dusty Rose
        private static readonly Color BrandColorHover = Color.FromArgb(135, 90, 97);  // Darker Mauve for hover
        private static readonly Color BrandColorSoft = Color.FromArgb(176, 127, 135);  // Lighter Mauve for icon badge
        private static readonly Color CreamBackground = Color.FromArgb(247, 241, 234); // #F7F1EA Off-white/Cream
        private static readonly Color TextDark = Color.FromArgb(45, 32, 28);        // Dark Charcoal
        private static readonly Color TextMuted = Color.FromArgb(135, 120, 115);     // Secondary Gray text
        private static readonly Color BorderColor = Color.FromArgb(226, 216, 207);    // Soft Border
        private static readonly Color ErrorColor = Color.FromArgb(190, 70, 70);

        // =========================================================
        // RESPONSIVE LAYOUT CONSTANTS
        // =========================================================

        // Below this client width, the brand panel hides completely
        // and the form panel fills the whole window.
        private const int LeftPanelHideBreakpoint = 760;

        // Brand (left) panel ~30% width of window, clamped between bounds.
        private const int LeftPanelMinWidth = 320;
        private const int LeftPanelMaxWidth = 440;
        private const double LeftPanelWidthRatio = 0.30;

        // Form panel width bounds for the right panel card
        private const int FormPanelMinWidth = 340;
        private const int FormPanelMaxWidth = 440;

        // Horizontal margin reserved on right panel so card doesn't hit edges
        private const int FormPanelSideMargin = 40;

        // Vertical rhythm gaps at 100% scale
        private const int GapAfterTitle = 6;
        private const int GapAfterSubtitle = 32;
        private const int GapAfterCaption = 8;
        private const int GapAfterUsernameField = 24;
        private const int GapAfterPasswordRow = 10;
        private const int GapAfterPasswordField = 28;
        private const int GapAfterButton = 16;
        private const int GapAfterError = 24;
        private const int FormBottomPadding = 16;

        // =========================================================
        // UI SCALE (responsive growth on maximize)
        // =========================================================

        private const int DesignWidth = 1100;
        private const int DesignHeight = 680;

        private const double MinUiScale = 1.0;
        private const double MaxUiScale = 1.5;

        private double currentScale = MinUiScale;

        private const float BaseFieldHeight = 46F;
        private const int BaseButtonHeight = 44;
        private const int BaseInnerPadding = 16;
        private const int BaseErrorHeight = 24;
        private const int BaseFooterHeight = 20;

        private const float BaseWelcomeFontSize = 24F;
        private const float BaseWelcomeSubtitleFontSize = 10F;
        private const float BaseCaptionFontSize = 8.5F;
        private const float BaseFieldFontSize = 10.5F;
        private const float BaseButtonFontSize = 10.5F;
        private const float BaseToggleFontSize = 11F;
        private const float BaseFooterFontSize = 8F;
        private const float BaseErrorFontSize = 8.5F;
        private const float BaseLinkFontSize = 8.5F;

        // =========================================================
        // CONTROLS
        // =========================================================

        private Panel leftPanel = null!;
        private Panel rightPanel = null!;
        private Panel formPanel = null!;
        private Panel logoPanel = null!;

        private Label lblBrandTitle = null!;
        private Label lblBrandSubtitle = null!;
        private Label lblHeadline = null!;
        private Label lblHeadlineDescription = null!;

        private Label lblWelcome = null!;
        private Label lblWelcomeSubtitle = null!;

        private Label lblUsernameCaption = null!;
        private Panel panelUsername = null!;

        private Label lblPasswordCaption = null!;
        private LinkLabel lnkForgotPassword = null!;
        private Panel panelPassword = null!;
        private Label lblTogglePassword = null!;

        private Label lblError = null!;
        private Label lblFooter = null!;

        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;

        private Button btnSignIn = null!;
        private TableLayoutPanel formTable = null!;

        private FlowLayoutPanel welcomeFlow = null!;
        private FlowLayoutPanel usernameFlow = null!;
        private FlowLayoutPanel passwordFlow = null!;
        private FlowLayoutPanel actionsFlow = null!;
        private TableLayoutPanel passwordHeader = null!;

        public LoginForm()
        {
            InitializeLoginForm();
            InitializeUI();
            UpdateLayout();
        }

        // =========================================================
        // FORM SETTINGS
        // =========================================================

        private void InitializeLoginForm()
        {
            Text = "Custom Cake CRMS";
            StartPosition = FormStartPosition.CenterScreen;

            ClientSize = new Size(DesignWidth, DesignHeight);

            MinimumSize = new Size(480, 560);

            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;

            BackColor = CreamBackground;

            Font = new Font("Segoe UI", 10F, FontStyle.Regular);

            AutoScaleMode = AutoScaleMode.None;

            if (UITheme.AppIcon != null)
                Icon = UITheme.AppIcon;
        }

        // =========================================================
        // UI BUILDING
        // =========================================================

        private void InitializeUI()
        {
            BuildLeftPanel();
            BuildRightPanel();

            Controls.Add(rightPanel);
            Controls.Add(leftPanel);

            AcceptButton = btnSignIn;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateLayout();
        }

        // =========================================================
        // RESPONSIVE LAYOUT DRIVER
        // =========================================================

        private void UpdateLayout()
        {
            if (leftPanel == null || rightPanel == null) return;

            bool showLeftPanel = ClientSize.Width >= LeftPanelHideBreakpoint;
            leftPanel.Visible = showLeftPanel;

            if (showLeftPanel)
            {
                int leftWidth = (int)(ClientSize.Width * LeftPanelWidthRatio);
                leftWidth = Math.Max(LeftPanelMinWidth, Math.Min(LeftPanelMaxWidth, leftWidth));
                leftPanel.Width = leftWidth;
            }

            double widthRatio = ClientSize.Width / (double)DesignWidth;
            double heightRatio = ClientSize.Height / (double)DesignHeight;
            double scale = Math.Min(widthRatio, heightRatio);
            scale = Math.Max(MinUiScale, Math.Min(scale, MaxUiScale));

            ApplyScale(scale);

            PositionHeadline();
            ResizeFormPanel();
            CenterFormPanel();
        }

        // ---------------------------------------------------------
        // LEFT (BRAND) PANEL (~30% Width, Mauve Background)
        // ---------------------------------------------------------

        private void BuildLeftPanel()
        {
            leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = (int)(DesignWidth * LeftPanelWidthRatio),
                BackColor = BrandColor
            };

            leftPanel.Paint += LeftPanel_Paint;
            leftPanel.Resize += (s, e) => PositionHeadline();

            // Brand Logo badge (clean white rounded badge to showcase official logo)
            logoPanel = new Panel
            {
                Size = new Size(52, 52),
                Location = new Point(36, 34),
                BackColor = Color.White
            };
            logoPanel.ApplyRoundedRegion(12);
            logoPanel.Paint += LogoPanel_Paint;

            lblBrandTitle = new Label
            {
                Text = "Custom Cake CRMS",
                AutoSize = true,
                Font = new Font("Georgia", 13.5F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(98, 38)
            };

            lblBrandSubtitle = new Label
            {
                Text = "Crafted for cake businesses",
                AutoSize = true,
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.FromArgb(235, 215, 218),
                Location = new Point(98, lblBrandTitle.Bottom + 2)
            };

            lblHeadline = new Label
            {
                Text = "Every cake tells\na sweet story.",
                AutoSize = false,
                Size = new Size(300, 120),
                Font = new Font("Georgia", 27F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(36, 0)
            };

            lblHeadlineDescription = new Label
            {
                Text = "Manage your inquiries, orders, and customer\nrelationships \u2014 all in one place built for\nPhilippine cake businesses.",
                AutoSize = false,
                Size = new Size(300, 85),
                Font = new Font("Segoe UI", 9.5F),
                ForeColor = Color.FromArgb(235, 215, 218),
                Location = new Point(36, 0)
            };

            leftPanel.Controls.Add(logoPanel);
            leftPanel.Controls.Add(lblBrandTitle);
            leftPanel.Controls.Add(lblBrandSubtitle);
            leftPanel.Controls.Add(lblHeadline);
            leftPanel.Controls.Add(lblHeadlineDescription);
        }

        private void PositionHeadline()
        {
            if (leftPanel == null || lblHeadline == null || lblHeadlineDescription == null) return;

            const int sideMargin = 36;
            int contentWidth = Math.Max(200, leftPanel.Width - sideMargin * 2);
            lblHeadline.Width = contentWidth;
            lblHeadlineDescription.Width = contentWidth;

            int bottomMargin = 56;
            int descHeight = lblHeadlineDescription.Height;
            int headlineHeight = lblHeadline.Height;
            int gap = 16;

            int descY = leftPanel.Height - bottomMargin - descHeight;
            int headlineY = descY - gap - headlineHeight;

            int minHeadlineY = lblBrandSubtitle.Bottom + 36;

            lblHeadline.Location = new Point(sideMargin, Math.Max(headlineY, minHeadlineY));
            lblHeadlineDescription.Location = new Point(sideMargin, lblHeadline.Bottom + gap);
        }

        // Decorative soft-edged circles in corners with alpha transparency
        private void LeftPanel_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Semi-transparent lighter mauve/white brushes
            using Brush circleBrushTop = new SolidBrush(Color.FromArgb(24, 255, 255, 255));
            using Brush circleBrushBottom = new SolidBrush(Color.FromArgb(18, 255, 255, 255));

            // Top-right bleeding circle
            int c1 = (int)(leftPanel.Width * 1.3);
            g.FillEllipse(circleBrushTop, leftPanel.Width - c1 / 2, -c1 / 4, c1, c1);

            // Bottom-left bleeding circle
            int c2 = (int)(leftPanel.Width * 1.1);
            g.FillEllipse(circleBrushBottom, -c2 / 2, leftPanel.Height - c2 / 2, c2, c2);
        }

        // Brand Logo inside the brand badge
        private void LogoPanel_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (UITheme.LogoImage != null)
            {
                var logoRect = new Rectangle(5, 5, logoPanel.Width - 10, logoPanel.Height - 10);
                UITheme.DrawLogo(g, logoRect);
            }
            else
            {
                using Brush brush = new SolidBrush(Color.White);

                // Cake layer body
                Rectangle cakeBase = new Rectangle(10, 23, 28, 15);
                g.FillRoundedRectangle(brush, cakeBase, 4);

                // Top layer polygon
                Point[] top =
                {
                    new Point(10, 23),
                    new Point(38, 23),
                    new Point(34, 18),
                    new Point(14, 18)
                };
                g.FillPolygon(brush, top);

                // Candle
                g.FillRectangle(brush, 23, 9, 2, 9);

                // Flame
                Point[] flame =
                {
                    new Point(24, 4),
                    new Point(21, 9),
                    new Point(27, 9)
                };
                g.FillPolygon(brush, flame);
            }
        }

        // ---------------------------------------------------------
        // RIGHT (FORM) PANEL (~70% Width, Cream Background #F7F1EA)
        // ---------------------------------------------------------

        private void BuildRightPanel()
        {
            rightPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CreamBackground
            };

            formPanel = new Panel
            {
                Size = new Size(FormPanelMaxWidth, 420),
                BackColor = CreamBackground
            };

            // Heading "Welcome back" (Bold Serif)
            lblWelcome = new Label
            {
                Text = "Welcome back",
                AutoSize = true,
                Font = new Font("Georgia", BaseWelcomeFontSize, FontStyle.Bold),
                ForeColor = TextDark
            };

            // Subtext "Sign in to your account to continue."
            lblWelcomeSubtitle = new Label
            {
                Text = "Sign in to your account to continue.",
                AutoSize = true,
                Font = new Font("Segoe UI", BaseWelcomeSubtitleFontSize),
                ForeColor = TextMuted
            };

            // ---- Email Address Field ----

            lblUsernameCaption = new Label
            {
                Text = "EMAIL ADDRESS",
                AutoSize = true,
                Font = new Font("Segoe UI", BaseCaptionFontSize, FontStyle.Bold),
                ForeColor = TextMuted
            };

            panelUsername = CreateBorderedFieldPanel(FormPanelMaxWidth, (int)BaseFieldHeight);

            txtUsername = new TextBox
            {
                Name = "txtUsername",
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", BaseFieldFontSize),
                ForeColor = TextDark,
                Location = new Point(16, 13),
                Width = panelUsername.Width - 32,
                PlaceholderText = "Enter your email"
            };
            panelUsername.Controls.Add(txtUsername);

            // ---- Password Field ----

            lblPasswordCaption = new Label
            {
                Text = "PASSWORD",
                AutoSize = true,
                Font = new Font("Segoe UI", BaseCaptionFontSize, FontStyle.Bold),
                ForeColor = TextMuted
            };

            lnkForgotPassword = new LinkLabel
            {
                Text = "Forgot password?",
                AutoSize = true,
                Font = new Font("Segoe UI", BaseLinkFontSize),
                LinkColor = BrandColor,
                ActiveLinkColor = BrandColorHover,
                LinkBehavior = LinkBehavior.HoverUnderline
            };
            lnkForgotPassword.LinkClicked += LnkForgotPassword_LinkClicked;

            panelPassword = CreateBorderedFieldPanel(FormPanelMaxWidth, (int)BaseFieldHeight);

            txtPassword = new TextBox
            {
                Name = "txtPassword",
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", BaseFieldFontSize),
                ForeColor = TextDark,
                UseSystemPasswordChar = true,
                Location = new Point(16, 13),
                Width = panelPassword.Width - 32 - 40,
                PlaceholderText = "Enter your password"
            };

            // Eye icon toggle for password visibility
            lblTogglePassword = new Label
            {
                Text = "\uD83D\uDC41", // Eye unicode emoji / icon
                AutoSize = true,
                Font = new Font("Segoe UI Symbol", BaseToggleFontSize),
                ForeColor = TextMuted,
                Cursor = Cursors.Hand
            };
            lblTogglePassword.Click += TogglePasswordVisibility_Click;

            panelPassword.Controls.Add(txtPassword);
            panelPassword.Controls.Add(lblTogglePassword);

            // ---- Full-Width "Sign in" Button ----

            btnSignIn = new Button
            {
                Name = "btnSignIn",
                Text = "Sign in",
                Size = new Size(FormPanelMaxWidth, BaseButtonHeight),
                FlatStyle = FlatStyle.Flat,
                BackColor = BrandColor,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", BaseButtonFontSize, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSignIn.FlatAppearance.BorderSize = 0;
            btnSignIn.FlatAppearance.MouseOverBackColor = BrandColorHover;
            btnSignIn.ApplyRoundedRegion(10);
            btnSignIn.Click += BtnSignIn_Click;

            // ---- Error Message Label ----

            lblError = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", BaseErrorFontSize),
                ForeColor = ErrorColor,
                Size = new Size(FormPanelMaxWidth, BaseErrorHeight)
            };

            // ---- Footer Label ----

            lblFooter = new Label
            {
                Text = "Secure business access",
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", BaseFooterFontSize),
                ForeColor = TextMuted,
                Size = new Size(FormPanelMaxWidth, BaseFooterHeight)
            };

            formTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = 6,
                Margin = new Padding(0),
                Padding = new Padding(0)
            };
            formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            welcomeFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0)
            };
            lblWelcome.Margin = new Padding(0, 0, 0, GapAfterTitle);
            lblWelcomeSubtitle.Margin = new Padding(0, 0, 0, GapAfterSubtitle);
            welcomeFlow.Controls.Add(lblWelcome);
            welcomeFlow.Controls.Add(lblWelcomeSubtitle);

            usernameFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0)
            };
            lblUsernameCaption.Margin = new Padding(0, 0, 0, GapAfterCaption);
            panelUsername.Margin = new Padding(0, 0, 0, GapAfterUsernameField);
            usernameFlow.Controls.Add(lblUsernameCaption);
            usernameFlow.Controls.Add(panelUsername);

            passwordFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0)
            };

            passwordHeader = new TableLayoutPanel
            {
                ColumnCount = 2,
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, GapAfterPasswordRow),
                Padding = new Padding(0)
            };
            passwordHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            passwordHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lblPasswordCaption.Margin = new Padding(0);
            lnkForgotPassword.Margin = new Padding(0, 0, 0, 2);
            lnkForgotPassword.Anchor = AnchorStyles.Right;
            passwordHeader.Controls.Add(lblPasswordCaption, 0, 0);
            passwordHeader.Controls.Add(lnkForgotPassword, 1, 0);

            panelPassword.Margin = new Padding(0, 0, 0, GapAfterPasswordField);
            passwordFlow.Controls.Add(passwordHeader);
            passwordFlow.Controls.Add(panelPassword);

            actionsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, GapAfterButton)
            };
            btnSignIn.Margin = new Padding(0);
            actionsFlow.Controls.Add(btnSignIn);

            lblError.Margin = new Padding(0, 0, 0, GapAfterError);
            lblFooter.Margin = new Padding(0, 0, 0, FormBottomPadding);

            formTable.Controls.Add(welcomeFlow, 0, 0);
            formTable.Controls.Add(usernameFlow, 0, 1);
            formTable.Controls.Add(passwordFlow, 0, 2);
            formTable.Controls.Add(actionsFlow, 0, 3);
            formTable.Controls.Add(lblError, 0, 4);
            formTable.Controls.Add(lblFooter, 0, 5);

            formPanel.Controls.Add(formTable);
            rightPanel.Controls.Add(formPanel);

            ApplyFormWidth(FormPanelMaxWidth);
        }

        private Panel CreateBorderedFieldPanel(int width, int height)
        {
            var panel = new Panel
            {
                Size = new Size(width, height),
                BackColor = Color.White
            };

            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(BorderColor, 1.4f);
                e.Graphics.DrawRoundedRectangle(
                    pen,
                    new Rectangle(0, 0, panel.Width - 1, panel.Height - 1),
                    10);
            };

            panel.Resize += (s, e) => panel.Invalidate();

            return panel;
        }

        // =========================================================
        // UI SCALE APPLICATION
        // =========================================================

        private void ApplyScale(double scale)
        {
            if (formPanel == null) return;

            currentScale = scale;

            lblWelcome.Font = new Font("Georgia", Scaled(BaseWelcomeFontSize), FontStyle.Bold);
            lblWelcomeSubtitle.Font = new Font("Segoe UI", Scaled(BaseWelcomeSubtitleFontSize));
            lblUsernameCaption.Font = new Font("Segoe UI", Scaled(BaseCaptionFontSize), FontStyle.Bold);
            lblPasswordCaption.Font = new Font("Segoe UI", Scaled(BaseCaptionFontSize), FontStyle.Bold);
            lnkForgotPassword.Font = new Font("Segoe UI", Scaled(BaseLinkFontSize));
            txtUsername.Font = new Font("Segoe UI", Scaled(BaseFieldFontSize));
            txtPassword.Font = new Font("Segoe UI", Scaled(BaseFieldFontSize));
            lblTogglePassword.Font = new Font("Segoe UI Symbol", Scaled(BaseToggleFontSize));
            btnSignIn.Font = new Font("Segoe UI", Scaled(BaseButtonFontSize), FontStyle.Bold);
            lblError.Font = new Font("Segoe UI", Scaled(BaseErrorFontSize));
            lblFooter.Font = new Font("Segoe UI", Scaled(BaseFooterFontSize));

            int fieldHeight = ScaledInt(BaseFieldHeight);
            panelUsername.Height = fieldHeight;
            panelPassword.Height = fieldHeight;

            btnSignIn.Height = ScaledInt(BaseButtonHeight);

            lblError.Height = ScaledInt(BaseErrorHeight);
            lblFooter.Height = ScaledInt(BaseFooterHeight);

            lblWelcome.Margin = new Padding(0, 0, 0, ScaledInt(GapAfterTitle));
            lblWelcomeSubtitle.Margin = new Padding(0, 0, 0, ScaledInt(GapAfterSubtitle));
            lblUsernameCaption.Margin = new Padding(0, 0, 0, ScaledInt(GapAfterCaption));
            panelUsername.Margin = new Padding(0, 0, 0, ScaledInt(GapAfterUsernameField));
            passwordHeader.Margin = new Padding(0, 0, 0, ScaledInt(GapAfterPasswordRow));
            panelPassword.Margin = new Padding(0, 0, 0, ScaledInt(GapAfterPasswordField));
            actionsFlow.Margin = new Padding(0, 0, 0, ScaledInt(GapAfterButton));
            lblError.Margin = new Padding(0, 0, 0, ScaledInt(GapAfterError));
            lblFooter.Margin = new Padding(0, 0, 0, ScaledInt(FormBottomPadding));
        }

        private float Scaled(float baseSize) => (float)(baseSize * currentScale);

        private int ScaledInt(double baseSize) => (int)Math.Round(baseSize * currentScale);

        private void ResizeFormPanel()
        {
            if (rightPanel == null || formPanel == null) return;

            int scaledMin = ScaledInt(FormPanelMinWidth);
            int scaledMax = ScaledInt(FormPanelMaxWidth);

            int available = Math.Max(0, rightPanel.ClientSize.Width - FormPanelSideMargin * 2);
            int formWidth = Math.Max(scaledMin, Math.Min(scaledMax, available));

            if (!leftPanel.Visible)
            {
                formWidth = Math.Max(scaledMin, Math.Min(scaledMax, rightPanel.ClientSize.Width - FormPanelSideMargin * 2));
            }

            ApplyFormWidth(formWidth);
        }

        private void ApplyFormWidth(int width)
        {
            if (formPanel == null) return;

            panelUsername.Width = width;
            panelPassword.Width = width;
            btnSignIn.Width = width;
            btnSignIn.ApplyRoundedRegion(10);
            lblError.Width = width;
            lblFooter.Width = width;

            int innerPadding = ScaledInt(BaseInnerPadding);

            txtUsername.Width = Math.Max(160, width - innerPadding * 2);
            txtUsername.Location = new Point(innerPadding, (panelUsername.Height - txtUsername.Height) / 2);

            int toggleWidth = lblTogglePassword.Width + ScaledInt(6);
            txtPassword.Width = Math.Max(120, width - innerPadding * 2 - toggleWidth - ScaledInt(6));
            txtPassword.Location = new Point(innerPadding, (panelPassword.Height - txtPassword.Height) / 2);

            lblTogglePassword.Location = new Point(
                panelPassword.Width - lblTogglePassword.Width - innerPadding,
                (panelPassword.Height - lblTogglePassword.Height) / 2);

            formPanel.Width = width;
            if (formTable != null)
            {
                formTable.Width = width;
                formTable.PerformLayout();
                formPanel.Height = formTable.PreferredSize.Height + ScaledInt(FormBottomPadding);
            }
        }

        private void CenterFormPanel()
        {
            if (rightPanel == null || formPanel == null) return;

            formPanel.Location = new Point(
                Math.Max((rightPanel.Width - formPanel.Width) / 2, FormPanelSideMargin),
                Math.Max((rightPanel.Height - formPanel.Height) / 2, 20));
        }

        // =========================================================
        // SHOW / HIDE PASSWORD TOGGLE
        // =========================================================

        private void TogglePasswordVisibility_Click(object? sender, EventArgs e)
        {
            bool wasHidden = txtPassword.UseSystemPasswordChar;
            txtPassword.UseSystemPasswordChar = !wasHidden;
            // Toggle eye symbol icon or text state
            lblTogglePassword.ForeColor = wasHidden ? BrandColor : TextMuted;
            ApplyFormWidth(formPanel.Width);
        }

        // =========================================================
        // FORGOT PASSWORD
        // =========================================================

        private void LnkForgotPassword_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show(
                "Please contact your system administrator to reset your password.",
                "Forgot Password",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        // =========================================================
        // LOGIN LOGIC
        // =========================================================

        private void BtnSignIn_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";

            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                lblError.Text = "Please enter your email and password.";
                return;
            }

            // Accounts mapping (matching database seed data with CompanyId = 2)
            if ((username == "superadmin" || username == "superadmin@sweetstory.ph") && password == "admin123")
            {
                SetCurrentUser(1, "superadmin", "Super", "Admin", "SuperAdmin");
                OpenDashboard("SuperAdmin");
                return;
            }

            if ((username == "admin" || username == "admin@sweetstory.ph") && password == "admin123")
            {
                SetCurrentUser(2, "admin", "Lea", "Abad", "Admin");
                OpenDashboard("Admin");
                return;
            }

            if ((username == "manager" || username == "manager@sweetstory.ph") && password == "admin123")
            {
                SetCurrentUser(3, "manager", "Camille", "Reyes", "Manager");
                OpenDashboard("Manager");
                return;
            }

            if ((username == "staff" || username == "staff@sweetstory.ph") && password == "admin123")
            {
                SetCurrentUser(4, "staff", "Jerome", "Santos", "Staff");
                OpenDashboard("Staff");
                return;
            }

            lblError.Text = "Invalid email or password.";
        }

        // =========================================================
        // SESSION USER CREATION
        // =========================================================

        private void SetCurrentUser(int userId, string username, string firstName, string lastName, string role, int companyId = 2)
        {
            SessionService.CurrentUser = new CurrentUser
            {
                UserId = userId,
                Username = username,
                FirstName = firstName,
                LastName = lastName,
                Role = role,
                CompanyId = companyId
            };
        }

        // =========================================================
        // DASHBOARD OPENING
        // =========================================================

        private void OpenDashboard(string role)
        {
            Form dashboard;

            switch (role)
            {
                case "SuperAdmin":
                    dashboard = new CC.Forms.SuperAdmin.SuperAdminDashboardForm();
                    break;

                case "Admin":
                    dashboard = new CC.Forms.Admin.AdminDashboardForm();
                    break;

                case "Manager":
                    dashboard = new CC.Forms.Manager.ManagerDashboardForm();
                    break;

                case "Staff":
                    dashboard = new CC.Forms.Staff.StaffDashboardForm();
                    break;

                default:
                    lblError.Text = "Invalid user role.";
                    return;
            }

            Hide();
            dashboard.FormClosed += Dashboard_FormClosed;
            dashboard.Show();
        }

        private void Dashboard_FormClosed(object? sender, FormClosedEventArgs e)
        {
            Show();
            txtPassword.Clear();
        }
    }

    // =============================================================
    // GRAPHICS HELPER EXTENSIONS
    // =============================================================

    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle rectangle, int radius)
        {
            using GraphicsPath path = GetRoundedRectanglePath(rectangle, radius);
            graphics.FillPath(brush, path);
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle rectangle, int radius)
        {
            using GraphicsPath path = GetRoundedRectanglePath(rectangle, radius);
            graphics.DrawPath(pen, path);
        }

        public static void ApplyRoundedRegion(this Control control, int radius)
        {
            void UpdateRegion(object? sender, EventArgs e)
            {
                if (control.Width <= 0 || control.Height <= 0) return;
                var rect = new Rectangle(0, 0, control.Width, control.Height);
                using GraphicsPath path = GetRoundedRectanglePath(rect, radius);
                control.Region = new Region(path);
            }

            UpdateRegion(null, EventArgs.Empty);

            // Re-apply region whenever size changes (e.g. after docking or layout perform)
            control.SizeChanged -= UpdateRegion;
            control.SizeChanged += UpdateRegion;
        }

        public static GraphicsPath GetRoundedRectanglePath(Rectangle rectangle, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = Math.Min(radius * 2, Math.Min(rectangle.Width, rectangle.Height));
            if (diameter <= 0)
            {
                path.AddRectangle(rectangle);
                return path;
            }

            Rectangle arc = new Rectangle(rectangle.X, rectangle.Y, diameter, diameter);
            path.AddArc(arc, 180, 90);

            arc.X = rectangle.Right - diameter;
            path.AddArc(arc, 270, 90);

            arc.Y = rectangle.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            arc.X = rectangle.X;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
