using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Forms.Authentication;

namespace CC.Controls
{
    /// <summary>
    /// Centralized Design System Tokens and UI Styling Helpers for Custom Cake CRM ("Sweet Story CRM").
    /// Matches the exact target specification and visual mockup (media_1789365009186.png).
    /// </summary>
    public static class UITheme
    {
        // =========================================================
        // SHARED COLOR & STYLE TOKENS
        // =========================================================

        public static readonly Color PrimaryMauve = Color.FromArgb(140, 70, 90);        // #8C465A (Primary Accent / Maroon)
        public static readonly Color PrimaryMauveHover = Color.FromArgb(120, 60, 75);    // #783C4B (Button Hover State)
        public static readonly Color UpgradeGold = Color.FromArgb(201, 151, 90);        // #C9975A (Secondary Accent Gold)
        public static readonly Color UpgradeGoldHover = Color.FromArgb(181, 131, 70);   // Gold Hover
        public static readonly Color CreamBackground = Color.FromArgb(250, 246, 241);   // #FAF6F1 (Page Background)
        public static readonly Color TableHeaderTan = Color.FromArgb(239, 231, 223);    // #EFE7DF (Table Header & Sign-Out Bg)
        public static readonly Color BorderColor = Color.FromArgb(229, 223, 216);      // #E5DFD8 (Dividers & Row Separators)
        public static readonly Color TextDark = Color.FromArgb(31, 27, 24);           // #1F1B18 (Primary Text / Headings)
        public static readonly Color TextMuted = Color.FromArgb(138, 127, 118);       // #8A7F76 (Muted / Secondary Text)
        public static readonly Color CardBackground = Color.White;                     // #FFFFFF (Sidebar, Header, Table Card)
        public static readonly Color InputBackground = Color.White;                    // #FFFFFF (Pill Search Input)

        // Status Pill Colors
        public static readonly Color StatusGreenBg = Color.FromArgb(209, 244, 226);    // Light Green Pill
        public static readonly Color StatusGreenFg = Color.FromArgb(30, 131, 82);     // Dark Green Text
        public static readonly Color StatusYellowBg = Color.FromArgb(254, 243, 214);   // Light Yellow Pill
        public static readonly Color StatusYellowFg = Color.FromArgb(158, 108, 0);    // Dark Yellow/Orange Text
        public static readonly Color StatusBlueBg = Color.FromArgb(216, 238, 254);     // Light Blue Pill
        public static readonly Color StatusBlueFg = Color.FromArgb(27, 108, 176);     // Dark Blue Text
        public static readonly Color StatusRedBg = Color.FromArgb(253, 226, 226);      // Light Red Pill
        public static readonly Color StatusRedFg = Color.FromArgb(197, 48, 48);       // Dark Red Text

        // =========================================================
        // TYPOGRAPHY CONSTANTS
        // =========================================================

        public static readonly string FontSerif = "Georgia";
        public static readonly string FontSans = "Segoe UI";

        // =========================================================
        // BRAND LOGO ASSET & DRAWING HELPER
        // =========================================================

        private static Image? _cachedLogo;

        public static Image? LogoImage
        {
            get
            {
                if (_cachedLogo != null) return _cachedLogo;
                try
                {
                    // 1. Try embedded manifest resource
                    using var stream = typeof(UITheme).Assembly.GetManifestResourceStream("CC.Resources.logo.png");
                    if (stream != null)
                    {
                        using var ms = new System.IO.MemoryStream();
                        stream.CopyTo(ms);
                        _cachedLogo = Image.FromStream(ms);
                        return _cachedLogo;
                    }

                    // 2. Try file from output or project directory
                    string appDir = AppDomain.CurrentDomain.BaseDirectory;
                    string[] paths = new[]
                    {
                        System.IO.Path.Combine(appDir, "Resources", "logo.png"),
                        System.IO.Path.Combine(appDir, "logo.png"),
                        System.IO.Path.Combine(Application.StartupPath, @"..\..\..\Resources\logo.png")
                    };

                    foreach (var path in paths)
                    {
                        if (System.IO.File.Exists(path))
                        {
                            byte[] bytes = System.IO.File.ReadAllBytes(path);
                            using var ms = new System.IO.MemoryStream(bytes);
                            _cachedLogo = Image.FromStream(ms);
                            return _cachedLogo;
                        }
                    }
                }
                catch
                {
                    // Fallback handled gracefully
                }
                return _cachedLogo;
            }
        }

        public static void DrawLogo(Graphics g, Rectangle destRect)
        {
            var logo = LogoImage;
            if (logo != null)
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                float ratioX = (float)destRect.Width / logo.Width;
                float ratioY = (float)destRect.Height / logo.Height;
                float ratio = Math.Min(ratioX, ratioY);

                int w = (int)(logo.Width * ratio);
                int h = (int)(logo.Height * ratio);
                int x = destRect.X + (destRect.Width - w) / 2;
                int y = destRect.Y + (destRect.Height - h) / 2;

                g.DrawImage(logo, new Rectangle(x, y, w, h));
            }
        }

        private static Icon? _cachedIcon;

        public static Icon? AppIcon
        {
            get
            {
                if (_cachedIcon != null) return _cachedIcon;
                try
                {
                    var logo = LogoImage;
                    if (logo is Bitmap bmp)
                    {
                        using var squareBmp = new Bitmap(64, 64);
                        using var g = Graphics.FromImage(squareBmp);
                        g.Clear(Color.Transparent);
                        DrawLogo(g, new Rectangle(4, 4, 56, 56));
                        IntPtr hIcon = squareBmp.GetHicon();
                        _cachedIcon = (Icon)Icon.FromHandle(hIcon).Clone();
                        DestroyIcon(hIcon);
                        return _cachedIcon;
                    }
                }
                catch
                {
                }
                return null;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        // =========================================================
        // CONTROL STYLING HELPERS
        // =========================================================

        public static void ApplyTableStyle(DataGridView grid)
        {
            grid.BackgroundColor = Color.White;
            grid.BorderStyle = BorderStyle.None;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.GridColor = BorderColor;
            grid.RowHeadersVisible = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AutoGenerateColumns = false;
            grid.RowTemplate.Height = 68; // py-4 (calibrated)
            grid.EnableHeadersVisualStyles = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = TableHeaderTan,
                ForeColor = TextMuted,
                Font = new Font(FontSans, 8F, FontStyle.Bold), // text-xs uppercase
                SelectionBackColor = TableHeaderTan,
                SelectionForeColor = TextMuted,
                Padding = new Padding(20, 0, 0, 0) // px-5 (calibrated)
            };

            grid.ColumnHeadersHeight = 48; // py-3.5 (calibrated)
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            grid.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = TextDark,
                Font = new Font(FontSans, 9.5F, FontStyle.Regular), // text-sm (calibrated)
                SelectionBackColor = Color.FromArgb(250, 245, 241),
                SelectionForeColor = TextDark,
                Padding = new Padding(20, 0, 0, 0) // px-5 (calibrated)
            };
        }

        public static void ApplyPrimaryButton(Button btn, int radius = 12)
        {
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font(FontSans, 9.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.UseVisualStyleBackColor = false;
            btn.Region = null; // Never clip — paint the pill instead

            btn.Paint += (s, e) =>
            {
                var r = new Rectangle(0, 0, btn.Width, btn.Height);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                bool hovered = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? PrimaryMauveHover : PrimaryMauve);
                e.Graphics.FillRoundedRectangle(bg, r, radius);

                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, r, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };
        }

        /// <summary>
        /// Calibrated Action Button (New Customer, New Order, Schedule Follow-up, etc.):
        /// px-5 py-2.5 text-sm font-semibold rounded-xl, icon 16px
        /// </summary>
        public static void ApplyActionButton(Button btn, string iconGlyph = "\uE710", int radius = 12)
        {
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font(FontSans, 9.5F, FontStyle.Bold); // text-sm font-semibold
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.UseVisualStyleBackColor = false;
            btn.Region = null;

            btn.Paint += (s, e) =>
            {
                var r = new Rectangle(0, 0, btn.Width, btn.Height);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                bool hovered = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? PrimaryMauveHover : PrimaryMauve);
                e.Graphics.FillRoundedRectangle(bg, r, radius); // rounded-xl (12px)

                using var iconFont = new Font("Segoe MDL2 Assets", 10.5F); // icon 16px
                using var textFont = new Font(FontSans, 9.5F, FontStyle.Bold); // text-sm font-semibold

                string cleanText = btn.Text.StartsWith("+ ") ? btn.Text.Substring(2) : btn.Text;
                var textSize = TextRenderer.MeasureText(cleanText, textFont, Size.Empty, TextFormatFlags.NoPadding);
                int iconWidth = 16;
                int gap = 8;
                int totalContentWidth = iconWidth + gap + textSize.Width;
                int startX = Math.Max(16, (btn.Width - totalContentWidth) / 2); // px-5 (calibrated)

                // Draw 16px icon
                var iconRect = new Rectangle(startX, (btn.Height - 16) / 2, iconWidth, 16);
                TextRenderer.DrawText(e.Graphics, iconGlyph, iconFont, iconRect, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Draw label text
                var textRect = new Rectangle(startX + iconWidth + gap, 0, btn.Width - (startX + iconWidth + gap) - 4, btn.Height);
                TextRenderer.DrawText(e.Graphics, cleanText, textFont, textRect, Color.White,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };

            btn.MouseEnter += (s, e) => btn.Invalidate();
            btn.MouseLeave += (s, e) => btn.Invalidate();
        }

        public static void ApplySecondaryButton(Button btn, int radius = 18)
        {
            btn.BackColor = Color.Transparent;
            btn.ForeColor = TextDark;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font(FontSans, 9.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.UseVisualStyleBackColor = false;
            btn.Region = null;

            btn.Paint += (s, e) =>
            {
                var r = new Rectangle(0, 0, btn.Width, btn.Height);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                bool hovered = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? Color.FromArgb(229, 221, 213) : TableHeaderTan);
                e.Graphics.FillRoundedRectangle(bg, r, radius);

                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, r, TextDark,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };
        }

        public static void ApplyUpgradeButton(Button btn, int radius = 16)
        {
            btn.BackColor = Color.Transparent;
            btn.ForeColor = Color.White;
            btn.FlatStyle = FlatStyle.Flat;
            btn.Font = new Font(FontSans, 9.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btn.UseVisualStyleBackColor = false;
            btn.Region = null;

            btn.Paint += (s, e) =>
            {
                var r = new Rectangle(0, 0, btn.Width, btn.Height);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                bool hovered = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? UpgradeGoldHover : UpgradeGold);
                e.Graphics.FillRoundedRectangle(bg, r, radius);

                TextRenderer.DrawText(e.Graphics, btn.Text, btn.Font, r, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
            };
        }

        public static void DrawStatusPill(Graphics g, Rectangle pillRect, string text, Color bgColor, Color textColor, Color? dotColor = null)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var bgBrush = new SolidBrush(bgColor))
            {
                g.FillRoundedRectangle(bgBrush, pillRect, pillRect.Height / 2);
            }

            int textX = pillRect.Left + 10;
            if (dotColor.HasValue)
            {
                using var dotBrush = new SolidBrush(dotColor.Value);
                int dotSize = 6;
                int dotY = pillRect.Top + (pillRect.Height - dotSize) / 2;
                g.FillEllipse(dotBrush, pillRect.Left + 8, dotY, dotSize, dotSize);
                textX = pillRect.Left + 18;
            }

            using var font = new Font(FontSans, 8.5F, FontStyle.Bold);
            var rect = new Rectangle(textX, pillRect.Top, pillRect.Width - (textX - pillRect.Left) - 2, pillRect.Height);
            TextRenderer.DrawText(g, text, font, rect, textColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
        }

        /// <summary>
        /// Reliably centers a modal dialog relative to the application's top-level form or screen.
        /// Avoids off-center placement when opened from embedded/docked controls (TopLevel=false).
        /// </summary>
        public static void CenterOnParentOrScreen(this Form modal, Control? owner = null)
        {
            modal.StartPosition = FormStartPosition.Manual;

            Form? topLevel = owner?.FindForm() ?? modal.Owner?.FindForm() ?? Form.ActiveForm;
            if (topLevel == null || !topLevel.Visible)
            {
                foreach (Form f in Application.OpenForms)
                {
                    if (f.Visible && f != modal && f.TopLevel)
                    {
                        topLevel = f;
                        break;
                    }
                }
            }

            if (topLevel != null && topLevel.Visible && topLevel.WindowState != FormWindowState.Minimized)
            {
                Rectangle parentBounds = topLevel.WindowState == FormWindowState.Maximized
                    ? Screen.FromControl(topLevel).WorkingArea
                    : topLevel.Bounds;

                int x = parentBounds.Left + (parentBounds.Width - modal.Width) / 2;
                int y = parentBounds.Top + (parentBounds.Height - modal.Height) / 2;

                var screen = Screen.FromRectangle(parentBounds);
                var workArea = screen.WorkingArea;
                x = Math.Max(workArea.Left, Math.Min(x, workArea.Right - modal.Width));
                y = Math.Max(workArea.Top, Math.Min(y, workArea.Bottom - modal.Height));

                modal.Location = new Point(x, y);
            }
            else
            {
                var screen = Screen.PrimaryScreen ?? (Screen.AllScreens.Length > 0 ? Screen.AllScreens[0] : null);
                if (screen != null)
                {
                    var workArea = screen.WorkingArea;
                    int x = workArea.Left + (workArea.Width - modal.Width) / 2;
                    int y = workArea.Top + (workArea.Height - modal.Height) / 2;
                    modal.Location = new Point(x, y);
                }
            }
        }
    }
}
