using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Forms.Authentication;

namespace CC.Controls
{
    public class DashboardKpiCard : UserControl
    {
        public event EventHandler? CardClicked;

        private string _title = "Metric";
        private string _value = "0";
        private string _subtitle = string.Empty;
        private string _badgeText = string.Empty;
        private Color _badgeColor = UITheme.StatusGreenFg;
        private Color _badgeBgColor = UITheme.StatusGreenBg;
        private bool _isHovered = false;

        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        public string Value
        {
            get => _value;
            set { _value = value; Invalidate(); }
        }

        public string Subtitle
        {
            get => _subtitle;
            set { _subtitle = value; Invalidate(); }
        }

        public string BadgeText
        {
            get => _badgeText;
            set { _badgeText = value; Invalidate(); }
        }

        public void SetBadge(string text, Color fg, Color bg)
        {
            _badgeText = text;
            _badgeColor = fg;
            _badgeBgColor = bg;
            Invalidate();
        }

        public DashboardKpiCard()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Cursor = Cursors.Hand;
            Padding = new Padding(18, 14, 18, 14);
            Height = 142;
            MinimumSize = new Size(170, 135);

            MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            MouseLeave += (s, e) => { _isHovered = false; Invalidate(); };
            Click += (s, e) => CardClicked?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Card background & rounded border
            using (var bgBrush = new SolidBrush(_isHovered ? Color.FromArgb(254, 252, 250) : Color.White))
            using (var borderPen = new Pen(_isHovered ? UITheme.PrimaryMauve : UITheme.BorderColor, _isHovered ? 1.4f : 1f))
            {
                g.FillRoundedRectangle(bgBrush, rect, 12);
                g.DrawRoundedRectangle(borderPen, rect, 12);
            }

            // Badge (top right)
            int badgeLeft = Width - 16;
            if (!string.IsNullOrWhiteSpace(_badgeText))
            {
                using var badgeFont = new Font(UITheme.FontSans, 7.5F, FontStyle.Bold);
                var badgeSize = g.MeasureString(_badgeText, badgeFont);
                int bw = (int)badgeSize.Width + 12;
                int bh = 20;
                int bx = Width - bw - 14;
                int by = 16;
                badgeLeft = bx;

                var badgeRect = new Rectangle(bx, by, bw, bh);
                using var bBg = new SolidBrush(_badgeBgColor);
                using var bFg = new SolidBrush(_badgeColor);
                g.FillRoundedRectangle(bBg, badgeRect, 8);
                g.DrawString(_badgeText, badgeFont, bFg, bx + 6, by + 3);
            }

            // Title
            using (var titleFont = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(UITheme.TextMuted))
            {
                var sf = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap,
                    LineAlignment = StringAlignment.Center
                };
                int titleMaxW = Math.Max(40, badgeLeft - 20);
                g.DrawString(_title, titleFont, titleBrush, new RectangleF(16, 16, titleMaxW, 20), sf);
            }

            // Big Metric Value
            using (var valFont = new Font(UITheme.FontSerif, 24F, FontStyle.Bold))
            using (var valBrush = new SolidBrush(UITheme.TextDark))
            {
                g.DrawString(_value, valFont, valBrush, 16, 42);
            }

            // Subtitle
            if (!string.IsNullOrWhiteSpace(_subtitle))
            {
                using var subFont = new Font(UITheme.FontSans, 8F, FontStyle.Regular);
                using var subBrush = new SolidBrush(Color.FromArgb(135, 125, 118));
                var sfSub = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    LineAlignment = StringAlignment.Near
                };
                g.DrawString(_subtitle, subFont, subBrush, new RectangleF(16, 86, Width - 32, Math.Max(28, Height - 88)), sfSub);
            }
        }
    }
}
