using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CC.Forms.Authentication;

namespace CC.Controls
{
    public class BarItem
    {
        public string Category { get; set; } = string.Empty;
        public int Value { get; set; }
        public Color BarColor { get; set; } = UITheme.PrimaryMauve;
        public string? ExtraLabel { get; set; }
    }

    public class DashboardBarChart : UserControl
    {
        public event EventHandler<string>? CategoryClicked;

        private string _chartTitle = "Status Breakdown";
        private string _chartSubtitle = string.Empty;
        private List<BarItem> _items = new();
        private int _hoveredIndex = -1;

        public string ChartTitle
        {
            get => _chartTitle;
            set { _chartTitle = value; Invalidate(); }
        }

        public string ChartSubtitle
        {
            get => _chartSubtitle;
            set { _chartSubtitle = value; Invalidate(); }
        }

        public DashboardBarChart()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Padding = new Padding(20);
            MinimumSize = new Size(260, 220);
            Cursor = Cursors.Hand;

            MouseMove += OnMouseMoveBar;
            MouseLeave += (s, e) => { _hoveredIndex = -1; Invalidate(); };
            Click += OnBarClick;
        }

        public void SetData(IEnumerable<BarItem> items)
        {
            _items = items?.ToList() ?? new List<BarItem>();
            _hoveredIndex = -1;
            Invalidate();
        }

        private void OnMouseMoveBar(object? sender, MouseEventArgs e)
        {
            if (_items == null || _items.Count == 0) return;

            int top = 70;
            int bottom = Height - 20;
            int totalHeight = bottom - top;
            int rowHeight = Math.Max(28, totalHeight / Math.Max(1, _items.Count));

            int newHover = -1;
            for (int i = 0; i < _items.Count; i++)
            {
                int y = top + (i * rowHeight);
                if (e.Y >= y && e.Y <= y + rowHeight)
                {
                    newHover = i;
                    break;
                }
            }

            if (newHover != _hoveredIndex)
            {
                _hoveredIndex = newHover;
                Invalidate();
            }
        }

        private void OnBarClick(object? sender, EventArgs e)
        {
            if (_hoveredIndex >= 0 && _hoveredIndex < _items.Count)
            {
                CategoryClicked?.Invoke(this, _items[_hoveredIndex].Category);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Card background & border
            using (var bgBrush = new SolidBrush(Color.White))
            using (var borderPen = new Pen(UITheme.BorderColor, 1f))
            {
                g.FillRoundedRectangle(bgBrush, rect, 12);
                g.DrawRoundedRectangle(borderPen, rect, 12);
            }

            // Title
            using (var titleFont = new Font(UITheme.FontSerif, 12F, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(UITheme.TextDark))
            {
                g.DrawString(_chartTitle, titleFont, titleBrush, 22, 18);
            }

            // Subtitle
            if (!string.IsNullOrWhiteSpace(_chartSubtitle))
            {
                using var subFont = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular);
                using var subBrush = new SolidBrush(UITheme.TextMuted);
                g.DrawString(_chartSubtitle, subFont, subBrush, 22, 42);
            }

            int top = 70;
            int bottom = Height - 20;
            int left = 22;
            int right = Width - 24;
            int plotWidth = right - left;
            int plotHeight = bottom - top;

            // Empty state
            if (_items == null || _items.Count == 0 || _items.All(i => i.Value == 0))
            {
                using var emptyFont = new Font(UITheme.FontSans, 9.5F, FontStyle.Italic);
                using var emptyBrush = new SolidBrush(UITheme.TextMuted);
                string msg = "No data available for this period.";
                var sz = g.MeasureString(msg, emptyFont);
                g.DrawString(msg, emptyFont, emptyBrush, left + (plotWidth - sz.Width) / 2, top + (plotHeight - sz.Height) / 2);
                return;
            }

            int maxVal = _items.Max(i => i.Value);
            if (maxVal <= 0) maxVal = 1;
            int totalSum = _items.Sum(i => i.Value);
            if (totalSum <= 0) totalSum = 1;

            int rowHeight = Math.Max(26, plotHeight / _items.Count);
            int barThickness = Math.Clamp(rowHeight - 12, 10, 20);

            using var catFont = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
            using var valFont = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold);
            using var textBrush = new SolidBrush(UITheme.TextDark);
            using var mutedBrush = new SolidBrush(UITheme.TextMuted);

            int labelColWidth = 110;
            int valueColWidth = 55;
            int barAreaWidth = plotWidth - labelColWidth - valueColWidth;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                int rowY = top + (i * rowHeight);
                bool isHovered = i == _hoveredIndex;

                // Category Label
                g.DrawString(item.Category, catFont, isHovered ? textBrush : mutedBrush, left, rowY + 2);

                // Background track
                int barX = left + labelColWidth;
                int barY = rowY + (rowHeight - barThickness) / 2;
                var trackRect = new Rectangle(barX, barY, barAreaWidth, barThickness);
                using var trackBrush = new SolidBrush(Color.FromArgb(246, 243, 239));
                g.FillRoundedRectangle(trackBrush, trackRect, barThickness / 2);

                // Foreground Bar
                int barW = (int)((float)item.Value / maxVal * barAreaWidth);
                if (barW > 0)
                {
                    var barRect = new Rectangle(barX, barY, Math.Max(barThickness, barW), barThickness);
                    Color col = isHovered ? Color.FromArgb(Math.Min(255, item.BarColor.R + 25), Math.Min(255, item.BarColor.G + 25), Math.Min(255, item.BarColor.B + 25)) : item.BarColor;
                    using var barBrush = new SolidBrush(col);
                    g.FillRoundedRectangle(barBrush, barRect, barThickness / 2);
                }

                // Value and percentage label
                float pct = (float)item.Value / totalSum * 100f;
                string valStr = item.ExtraLabel ?? $"{item.Value} ({pct:0}%)";
                g.DrawString(valStr, valFont, textBrush, barX + barAreaWidth + 10, rowY + 2);
            }
        }
    }
}
