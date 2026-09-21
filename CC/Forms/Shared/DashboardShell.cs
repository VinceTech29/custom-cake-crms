using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Shared
{
    /// <summary>
    /// Base application shell matching the design system:
    /// - Sidebar: 250px wide, docked Left (full height)
    /// - TopHeaderBar: 60px high, docked Top (starts at x=250px)
    /// - MainPanel: docked Fill (starts at x=250px, y=60px)
    /// </summary>
    public abstract class DashboardShell : Form
    {
        protected Control Sidebar => SidebarCtrl;
        public Panel MainPanel { get; private set; } = null!;
        protected Panel TopHeaderBar { get; private set; } = null!;
        public SidebarControl SidebarCtrl { get; private set; } = null!;

        private Label lblPageTitle = null!;

        public DashboardShell(string title)
        {
            Text = title;
            Width = 1366;
            Height = 820;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(1040, 680);
            BackColor = UITheme.CreamBackground;

            if (UITheme.AppIcon != null)
                Icon = UITheme.AppIcon;

            InitializeShell();
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string PageTitle
        {
            get => lblPageTitle?.Text ?? Text;
            set
            {
                if (lblPageTitle != null)
                    lblPageTitle.Text = value;
                Text = value;
            }
        }

        // ─────────────────────────────────────────────────────────────
        // SHELL LAYOUT
        // ─────────────────────────────────────────────────────────────

        private void InitializeShell()
        {
            // 1. Sidebar Control (Fixed 250px, Dock Left)
            SidebarCtrl = new SidebarControl
            {
                Dock = DockStyle.Left,
                Width = SidebarControl.SidebarWidth
            };

            var cu = SessionService.CurrentUser;
            if (cu != null)
            {
                SidebarCtrl.SetUserDisplayName($"{cu.FirstName} {cu.LastName}".Trim());
                string roleDisplay = cu.Role switch
                {
                    "Admin" => "Business Admin",
                    "SuperAdmin" => "Super Admin",
                    _ => !string.IsNullOrWhiteSpace(cu.Role) ? cu.Role : "Staff"
                };
                SidebarCtrl.SetUserRole(roleDisplay);
            }
            SidebarCtrl.NavigationRequested += (s, key) => Navigate(key);
            SidebarCtrl.SignOutRequested += (s, e) => OnSignOutRequested();

            // 2. Top Header Bar (Hidden/removed per design requirement)
            TopHeaderBar = BuildTopHeaderBar();
            TopHeaderBar.Visible = false;
            TopHeaderBar.Height = 0;

            // 3. Main Content Panel (Dock Fill, begins directly to the right of sidebar)
            MainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.CreamBackground,
                Padding = new Padding(40, 32, 40, 32) // px-10 py-8 content padding (calibrated)
            };

            // Direct Form docking in reverse order: Fill, Left
            // This guarantees:
            //   Sidebar takes: X=0..250, Y=0..Height
            //   MainPanel takes: X=250..Width, Y=0..Height
            Controls.Add(MainPanel);
            Controls.Add(SidebarCtrl);
        }

        // ─────────────────────────────────────────────────────────────
        // TOP HEADER BAR
        //
        // LEFT  │ "Customers" Georgia Bold 15pt, #1F1B18
        // RIGHT │ bell circle │ JS maroon circle │ name+subtext │ Upgrade gold pill
        // ─────────────────────────────────────────────────────────────

        private Panel BuildTopHeaderBar()
        {
            var header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.White,
                Padding = new Padding(28, 0, 24, 0)
            };
            header.Paint += (s, e) =>
            {
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawLine(pen, 0, header.Height - 1, header.Width, header.Height - 1);
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            // ── LEFT: current page name ────────────────────────────
            string pageText = Text
                .Replace(" - Custom Cake CRMS", "")
                .Replace(" Dashboard", "");

            lblPageTitle = new Label
            {
                Text = pageText,
                Font = new Font(UITheme.FontSerif, 15f, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Margin = new Padding(0, 18, 0, 0)
            };

            // ── RIGHT: controls ────────────────────────────────────
            var rightFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 12, 0, 0),
                Margin = Padding.Empty
            };

            // Bell with cream circle bg + red dot
            var bell = new Panel { Size = new Size(36, 36), Margin = new Padding(0, 0, 14, 0), Cursor = Cursors.Hand };
            bell.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var bg = new SolidBrush(Color.FromArgb(244, 239, 234));
                g.FillEllipse(bg, 0, 0, bell.Width - 1, bell.Height - 1);
                using var font = new Font("Segoe MDL2 Assets", 11f);
                TextRenderer.DrawText(g, "\uEA89", font, bell.ClientRectangle, UITheme.TextDark,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                using var dot = new SolidBrush(Color.FromArgb(215, 50, 50));
                g.FillEllipse(dot, 22, 6, 8, 8);
            };

            // Maroon avatar circle with white initials
            var avatar = new Panel { Size = new Size(36, 36), Margin = new Padding(0, 0, 12, 0) };
            avatar.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using var br = new SolidBrush(UITheme.PrimaryMauve);
                g.FillEllipse(br, 0, 0, avatar.Width - 1, avatar.Height - 1);
                string nm = SessionService.CurrentUser != null
                    ? $"{SessionService.CurrentUser.FirstName} {SessionService.CurrentUser.LastName}"
                    : "Jerome Santos";
                using var font = new Font(UITheme.FontSans, 9f, FontStyle.Bold);
                TextRenderer.DrawText(g, GetInitials(nm), font, avatar.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // Name + subtext stacked
            string displayName = SessionService.CurrentUser != null
                ? $"{SessionService.CurrentUser.FirstName} {SessionService.CurrentUser.LastName}".Trim()
                : "Jerome Santos";

            var userInfo = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Margin = new Padding(0, 0, 18, 0),
                BackColor = Color.Transparent
            };
            userInfo.Controls.Add(new Label
            {
                Text = displayName,
                Font = new Font(UITheme.FontSans, 9.5f, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            });
            userInfo.Controls.Add(new Label
            {
                Text = "Custom Cake CRMS",
                Font = new Font(UITheme.FontSans, 8f, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            });

            // Gold Upgrade pill button
            var btnUpgrade = new Button
            {
                Text = "Upgrade",
                Size = new Size(94, 32),
                Margin = new Padding(0, 2, 0, 0)
            };
            UITheme.ApplyUpgradeButton(btnUpgrade, 16);

            rightFlow.Controls.AddRange(new Control[] { bell, avatar, userInfo, btnUpgrade });

            layout.Controls.Add(lblPageTitle, 0, 0);
            layout.Controls.Add(rightFlow, 1, 0);
            header.Controls.Add(layout);

            return header;
        }

        private string _currentActiveKey = "Dashboard";

        public string CurrentActiveKey => _currentActiveKey;

        public void Navigate(string key, object? filterContext = null)
        {
            if (string.IsNullOrWhiteSpace(key)) return;

            // Prevent redundant re-navigation to the currently active view if no filter context is provided
            if (string.Equals(_currentActiveKey, key, StringComparison.OrdinalIgnoreCase) && filterContext == null)
            {
                return;
            }

            _currentActiveKey = key;
            SidebarCtrl.SetActiveItem(key);
            OnNavigationRequested(key, filterContext);
        }

        protected virtual void OnNavigationRequested(string key, object? filterContext)
        {
            OnNavigationRequested(key);
        }

        protected virtual void OnNavigationRequested(string key)
        {
            PageTitle = key;
        }

        protected virtual void OnSignOutRequested()
        {
            SessionService.CurrentUser = null;
            Close();
        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "JS";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
                return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return (parts[0][0].ToString() + parts[^1][0]).ToUpper();
        }
    }
}
