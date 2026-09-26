using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Forms.Authentication;

namespace CC.Controls
{
    /// <summary>
    /// Sidebar navigation control replicating media_1789377987308.png design basis:
    /// - 250px fixed width, pure white background, thin right border #E5DFD8
    /// - 44x44 maroon rounded-square badge with cake icon + "Custom Cake CRMS" Georgia Bold 12pt
    /// - 48px high nav buttons (maroon active pill with white text/icon, muted #7D7068 inactive)
    /// - Footer: 42x42 maroon JS avatar + stacked name/subtext + 40px #EFE7DF "Sign out" pill button
    /// </summary>
    public class SidebarControl : UserControl
    {
        public event EventHandler<string>? NavigationRequested;
        public event EventHandler? SignOutRequested;

        public const int SidebarWidth = 256; // 256px wide (calibrated)

        private const string IconFontName = "Segoe MDL2 Assets";
        private readonly Dictionary<string, Button> _navButtons = new Dictionary<string, Button>();
        private FlowLayoutPanel _navFlow = null!;
        private string _activeKey = "Dashboard";

        // Session Information
        private string _companyName = "Custom Cake CRMS";
        private string _userDisplayName = "Jerome Santos";
        private string _userRole = "Staff";

        private Panel _headerPanel = null!;
        private Panel _logoBadge = null!;
        private Label _companyNameLabel = null!;
        private Label _userNameLabel = null!;
        private Label _shopNameFooterLabel = null!;
        private Panel _avatarPanel = null!;
        private Button _signOutBtn = null!;

        public SidebarControl()
        {
            Dock = DockStyle.Left;
            Width = SidebarWidth;
            MinimumSize = new Size(SidebarWidth, 0);
            BackColor = Color.White;
            DoubleBuffered = true;

            BuildLayout();
        }

        public void SetCompanyName(string name)
        {
            _companyName = name ?? _companyName;
            if (_companyNameLabel != null) _companyNameLabel.Text = _companyName;
        }

        public void SetUserDisplayName(string name)
        {
            _userDisplayName = name ?? _userDisplayName;
            if (_userNameLabel != null) _userNameLabel.Text = _userDisplayName;
            _avatarPanel?.Invalidate();
        }

        public void SetUserRole(string role)
        {
            _userRole = role ?? _userRole;
            if (_shopNameFooterLabel != null) _shopNameFooterLabel.Text = _userRole;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // Draw thin vertical divider on right edge
            using var pen = new Pen(UITheme.BorderColor, 1f);
            e.Graphics.DrawLine(pen, Width - 1, 0, Width - 1, Height);
        }

        // =========================================================
        // LAYOUT STRUCTURE
        // =========================================================

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.White,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 74f));  // Logo Header (36x36 logo)
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1f));   // Divider line
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));  // Nav list (py-2.5 px-4)
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 138f)); // Pinned Footer (36x36 avatar + py-2.5 px-4 sign out)

            _headerPanel = BuildHeader();
            root.Controls.Add(_headerPanel, 0, 0);
            root.Controls.Add(BuildDivider(), 0, 1);
            root.Controls.Add(BuildNavPanel(), 0, 2);
            root.Controls.Add(BuildFooter(), 0, 3);

            Controls.Add(root);
        }

        // ---------------------------------------------------------
        // 1. HEADER (Logo Badge 36×36px, Icon 18px + Company Name)
        // ---------------------------------------------------------
        private Panel BuildHeader()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16, 18, 8, 16)
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            // 36×36px logo icon badge, icon 18px
            _logoBadge = new Panel
            {
                Size = new Size(36, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            _logoBadge.Paint += (s, e) =>
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                if (UITheme.LogoImage != null)
                {
                    var bgRect = new Rectangle(0, 0, _logoBadge.Width, _logoBadge.Height);
                    using var bgBrush = new SolidBrush(Color.FromArgb(254, 242, 245)); // Soft blush container
                    using var borderPen = new Pen(Color.FromArgb(240, 212, 219), 1f);
                    g.FillRoundedRectangle(bgBrush, bgRect, 8);
                    g.DrawRoundedRectangle(borderPen, new Rectangle(0, 0, _logoBadge.Width - 1, _logoBadge.Height - 1), 8);

                    var logoRect = new Rectangle(3, 3, _logoBadge.Width - 6, _logoBadge.Height - 6);
                    UITheme.DrawLogo(g, logoRect);
                }
                else
                {
                    using var brush = new SolidBrush(UITheme.PrimaryMauve);
                    g.FillRoundedRectangle(brush, new Rectangle(0, 0, _logoBadge.Width, _logoBadge.Height), 10);
                }
            };

            _companyNameLabel = new Label
            {
                Text = _companyName,
                Font = new Font(UITheme.FontSerif, 11F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 0)
            };

            flow.Controls.Add(_logoBadge);
            flow.Controls.Add(_companyNameLabel);
            panel.Controls.Add(flow);

            return panel;
        }

        private Control BuildDivider()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = UITheme.BorderColor,
                Margin = Padding.Empty
            };
        }

        // ---------------------------------------------------------
        // 2. NAV PANEL (Dashboard, Customers, Orders, Payments, Follow-ups)
        // Calibrated: text-sm, py-2.5 px-4, icon 18px
        // ---------------------------------------------------------
        private Control BuildNavPanel()
        {
            var wrapper = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16, 16, 16, 10) // px-4 (16px)
            };

            _navFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = false,
                BackColor = Color.White,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };

            AddNavItemInternal(_navFlow, "Dashboard", "\uE80F");
            AddNavItemInternal(_navFlow, "Customers", "\uE77B");
            AddNavItemInternal(_navFlow, "Inquiries", "\uE8BD");
            AddNavItemInternal(_navFlow, "Orders", "\uE719");
            AddNavItemInternal(_navFlow, "Payments", "\uE8C7");
            AddNavItemInternal(_navFlow, "Follow-ups", "\uE717");

            wrapper.Controls.Add(_navFlow);
            SetActiveItem(_activeKey);
            return wrapper;
        }

        public void AddNavItem(string key, string iconGlyph)
        {
            if (_navFlow == null || _navButtons.ContainsKey(key)) return;
            AddNavItemInternal(_navFlow, key, iconGlyph);
        }

        public void ResetNavItems(IEnumerable<(string Key, string Glyph)> items)
        {
            if (_navFlow == null) return;

            _navFlow.Controls.Clear();
            _navButtons.Clear();

            string firstKey = string.Empty;
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(firstKey)) firstKey = item.Key;
                AddNavItemInternal(_navFlow, item.Key, item.Glyph);
            }

            if (!string.IsNullOrEmpty(firstKey))
            {
                SetActiveItem(firstKey);
            }
        }

        public bool HasNavItem(string key) => _navButtons.ContainsKey(key);
        public IReadOnlyCollection<string> NavItemKeys => _navButtons.Keys.ToList().AsReadOnly();

        private void AddNavItemInternal(FlowLayoutPanel flow, string key, string iconGlyph)
        {
            var btn = new Button
            {
                Text = string.Empty,
                FlatStyle = FlatStyle.Flat,
                Height = 40, // py-2.5 calibrated (40px)
                Width = SidebarWidth - 32, // 224px (px-4 = 16px margins)
                Margin = new Padding(0, 0, 0, 8),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Tag = key,
                AutoSize = false,
                UseVisualStyleBackColor = false
            };

            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isActive = key == _activeKey;
                bool hovered = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position));

                // Active pill background (maroon), hover background (#FAF5F1), or transparent
                if (isActive)
                {
                    using var pillBrush = new SolidBrush(UITheme.PrimaryMauve);
                    e.Graphics.FillRoundedRectangle(pillBrush, new Rectangle(0, 0, btn.Width, btn.Height), 10);
                }
                else if (hovered)
                {
                    using var hoverBrush = new SolidBrush(Color.FromArgb(250, 245, 241));
                    e.Graphics.FillRoundedRectangle(hoverBrush, new Rectangle(0, 0, btn.Width, btn.Height), 10);
                }

                Color textColor = isActive ? Color.White : Color.FromArgb(125, 112, 104);

                // 18px Icon Glyph on left (px-4 = 16px)
                using var iconFont = new Font(IconFontName, 12.5F);
                var iconRect = new Rectangle(14, 0, 22, btn.Height);
                TextRenderer.DrawText(e.Graphics, iconGlyph, iconFont, iconRect, textColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

                // Text label on right (text-sm = 9.5pt)
                using var textFont = new Font(UITheme.FontSans, 9.5F, isActive ? FontStyle.Bold : FontStyle.Regular);
                var textRect = new Rectangle(42, 0, Math.Max(0, btn.Width - 46), btn.Height);
                TextRenderer.DrawText(e.Graphics, key, textFont, textRect, textColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            };

            btn.MouseEnter += (s, e) => btn.Invalidate();
            btn.MouseLeave += (s, e) => btn.Invalidate();

            btn.Region = null;
            btn.Click += (s, e) =>
            {
                SetActiveItem(key);
                NavigationRequested?.Invoke(this, key);
            };

            _navButtons[key] = btn;
            flow.Controls.Add(btn);
        }

        // ---------------------------------------------------------
        // 3. FOOTER (Avatar 36×36px, text-sm name + Sign Out py-2.5 px-4 text-sm)
        // ---------------------------------------------------------
        private Control BuildFooter()
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 14)
            };

            var topDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = UITheme.BorderColor,
                Margin = Padding.Empty
            };

            var userLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 42,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White,
                Margin = new Padding(0, 8, 0, 8)
            };
            userLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46F));
            userLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // 36×36px Maroon circular user avatar
            _avatarPanel = new Panel { Size = new Size(36, 36), Location = new Point(0, 3) };
            _avatarPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(UITheme.PrimaryMauve);
                e.Graphics.FillEllipse(brush, 0, 0, _avatarPanel.Width - 1, _avatarPanel.Height - 1);

                string initials = GetInitials(_userDisplayName);
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, initials, font, _avatarPanel.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            // Stacked Name & Role Subtext (text-sm name)
            var userTextContainer = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            var infoStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                Margin = new Padding(2, 2, 0, 0),
                BackColor = Color.Transparent
            };

            _userNameLabel = new Label
            {
                Text = _userDisplayName,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold), // text-sm name
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 1)
            };

            _shopNameFooterLabel = new Label
            {
                Text = _userRole,
                Font = new Font(UITheme.FontSans, 8.5F),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            infoStack.Controls.Add(_userNameLabel);
            infoStack.Controls.Add(_shopNameFooterLabel);
            userTextContainer.Controls.Add(infoStack);

            userLayout.Controls.Add(_avatarPanel, 0, 0);
            userLayout.Controls.Add(userTextContainer, 1, 0);

            // Pill-shaped "Sign out" button (#EFE7DF background, py-2.5 px-4 text-sm, 38px height)
            _signOutBtn = new Button
            {
                Text = string.Empty,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                Dock = DockStyle.Bottom,
                Height = 38, // py-2.5 calibrated
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 6, 0, 0),
                TabStop = false,
                UseVisualStyleBackColor = false
            };
            _signOutBtn.FlatAppearance.BorderSize = 0;
            _signOutBtn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            _signOutBtn.FlatAppearance.MouseDownBackColor = Color.Transparent;

            _signOutBtn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool hovered = _signOutBtn.ClientRectangle.Contains(_signOutBtn.PointToClient(Cursor.Position));

                // Draw pill background
                using var bg = new SolidBrush(hovered ? Color.FromArgb(228, 219, 210) : UITheme.TableHeaderTan);
                e.Graphics.FillRoundedRectangle(bg, new Rectangle(0, 0, _signOutBtn.Width, _signOutBtn.Height), 10);

                // Exit icon
                Color textColor = Color.FromArgb(74, 66, 61);
                using var iconFont = new Font(IconFontName, 10.5F);
                var iconRect = new Rectangle(16, 0, 18, _signOutBtn.Height);
                TextRenderer.DrawText(e.Graphics, "\uE7E8", iconFont, iconRect, textColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

                // "Sign out" text (text-sm = 9.5pt)
                using var textFont = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                var textRect = new Rectangle(40, 0, Math.Max(0, _signOutBtn.Width - 44), _signOutBtn.Height);
                TextRenderer.DrawText(e.Graphics, "Sign out", textFont, textRect, textColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);
            };

            _signOutBtn.MouseEnter += (s, e) => _signOutBtn.Invalidate();
            _signOutBtn.MouseLeave += (s, e) => _signOutBtn.Invalidate();

            _signOutBtn.Region = null;
            _signOutBtn.Click += (s, e) => SignOutRequested?.Invoke(this, EventArgs.Empty);

            panel.Controls.Add(_signOutBtn);
            panel.Controls.Add(userLayout);
            panel.Controls.Add(topDivider);

            return panel;
        }

        public void SetActiveItem(string key)
        {
            _activeKey = key;

            foreach (var kvp in _navButtons)
            {
                kvp.Value.BackColor = Color.Transparent;
                kvp.Value.FlatAppearance.MouseOverBackColor = Color.Transparent;
                kvp.Value.Invalidate();
            }
        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "JS";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return (parts[0][0].ToString() + parts[^1][0]).ToUpper();
        }
    }
}
