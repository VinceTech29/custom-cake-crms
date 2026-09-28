using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CC.Forms.Authentication;

namespace CC.Controls
{
    public class TrendPoint
    {
        public DateTime Date { get; set; }
        public decimal Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    public class DashboardTrendChart : UserControl
    {
        private string _chartTitle = "Trend Analytics";
        private string _chartSubtitle = string.Empty;
        private List<TrendPoint> _points = new();
        private bool _isCurrency = false;
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

        public bool IsCurrency
        {
            get => _isCurrency;
            set { _isCurrency = value; Invalidate(); }
        }

        public DashboardTrendChart()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Padding = new Padding(20);
            MinimumSize = new Size(300, 220);

            MouseMove += OnMouseMoveChart;
            MouseLeave += (s, e) => { _hoveredIndex = -1; Invalidate(); };
        }

        public void SetData(IEnumerable<TrendPoint> points, bool isCurrency = false)
        {
            _points = points?.OrderBy(p => p.Date).ToList() ?? new List<TrendPoint>();
            _isCurrency = isCurrency;
            _hoveredIndex = -1;
            Invalidate();
        }

        private void OnMouseMoveChart(object? sender, MouseEventArgs e)
        {
            if (_points == null || _points.Count < 2)
            {
                if (_hoveredIndex != -1) { _hoveredIndex = -1; Invalidate(); }
                return;
            }

            int top = 70;
            int bottom = Height - 40;
            int left = 55;
            int right = Width - 30;
            int plotWidth = right - left;

            int newHover = -1;
            for (int i = 0; i < _points.Count; i++)
            {
                float px = left + (float)i / (_points.Count - 1) * plotWidth;
                if (Math.Abs(e.X - px) < 18 && e.Y >= top - 10 && e.Y <= bottom + 10)
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
            int bottom = Height - 40;
            int left = 60;
            int right = Width - 30;
            int plotWidth = right - left;
            int plotHeight = bottom - top;

            // Empty State
            if (_points == null || _points.Count == 0)
            {
                using var emptyFont = new Font(UITheme.FontSans, 9.5F, FontStyle.Italic);
                using var emptyBrush = new SolidBrush(UITheme.TextMuted);
                string msg = "No data available for this period.";
                var sz = g.MeasureString(msg, emptyFont);
                g.DrawString(msg, emptyFont, emptyBrush, left + (plotWidth - sz.Width) / 2, top + (plotHeight - sz.Height) / 2);
                return;
            }

            decimal maxVal = _points.Max(p => p.Value);
            if (maxVal <= 0) maxVal = _isCurrency ? 1000m : 5m;
            // Round maxVal up nicely
            maxVal = Math.Ceiling(maxVal * 1.15m);

            // Gridlines & Y-Axis labels (3 horizontal lines)
            using var gridPen = new Pen(Color.FromArgb(242, 238, 233), 1f) { DashStyle = DashStyle.Dash };
            using var axisFont = new Font(UITheme.FontSans, 7.5F, FontStyle.Regular);
            using var axisBrush = new SolidBrush(Color.FromArgb(160, 150, 145));

            int steps = 3;
            for (int i = 0; i <= steps; i++)
            {
                float y = bottom - (float)i / steps * plotHeight;
                g.DrawLine(gridPen, left, y, right, y);

                decimal valAtStep = maxVal * i / steps;
                string label = _isCurrency ? $"P{valAtStep:N0}" : $"{valAtStep:N0}";
                var lsz = g.MeasureString(label, axisFont);
                g.DrawString(label, axisFont, axisBrush, left - lsz.Width - 6, y - lsz.Height / 2);
            }

            // Calculate screen coordinates
            var screenPoints = new PointF[_points.Count];
            for (int i = 0; i < _points.Count; i++)
            {
                float x = _points.Count > 1
                    ? left + (float)i / (_points.Count - 1) * plotWidth
                    : left + plotWidth / 2f;
                float yRatio = (float)(_points[i].Value / maxVal);
                float y = bottom - (yRatio * plotHeight);
                screenPoints[i] = new PointF(x, y);
            }

            // Draw Area Gradient Fill
            if (screenPoints.Length > 1)
            {
                using var areaPath = new GraphicsPath();
                areaPath.AddLine(left, bottom, screenPoints[0].X, screenPoints[0].Y);
                areaPath.AddLines(screenPoints);
                areaPath.AddLine(screenPoints[^1].X, bottom, left, bottom);

                using var gradBrush = new LinearGradientBrush(
                    new Point(0, top),
                    new Point(0, bottom),
                    Color.FromArgb(50, 140, 70, 90), // Soft mauve tint
                    Color.FromArgb(5, 140, 70, 90));
                g.FillPath(gradBrush, areaPath);

                // Draw Smooth Line
                using var linePen = new Pen(UITheme.PrimaryMauve, 2.5f) { LineJoin = LineJoin.Round };
                g.DrawLines(linePen, screenPoints);
            }

            // Draw Points & X-Axis Labels
            int labelStep = Math.Max(1, _points.Count / 6);
            for (int i = 0; i < _points.Count; i++)
            {
                var pt = screenPoints[i];
                bool isHovered = i == _hoveredIndex;

                // Circle point
                if (_points.Count <= 30 || isHovered)
                {
                    float radius = isHovered ? 6f : 3.5f;
                    using var pointBrush = new SolidBrush(isHovered ? UITheme.UpgradeGold : Color.White);
                    using var pointPen = new Pen(UITheme.PrimaryMauve, isHovered ? 2.5f : 1.8f);
                    g.FillEllipse(pointBrush, pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
                    g.DrawEllipse(pointPen, pt.X - radius, pt.Y - radius, radius * 2, radius * 2);
                }

                // X-Axis Date label
                if (i % labelStep == 0 || i == _points.Count - 1)
                {
                    string dateText = _points[i].Date.ToString("MMM d");
                    var sz = g.MeasureString(dateText, axisFont);
                    g.DrawString(dateText, axisFont, axisBrush, pt.X - sz.Width / 2, bottom + 8);
                }
            }

            // Tooltip on Hover
            if (_hoveredIndex >= 0 && _hoveredIndex < _points.Count)
            {
                var p = _points[_hoveredIndex];
                var pt = screenPoints[_hoveredIndex];

                string valStr = _isCurrency ? $"P{p.Value:N2}" : $"{p.Value:N0}";
                string tooltip = $"{p.Date:MMM d, yyyy}: {valStr}";
                using var tipFont = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
                var tsz = g.MeasureString(tooltip, tipFont);

                float tipX = Math.Clamp(pt.X - tsz.Width / 2 - 8, 10, Width - tsz.Width - 24);
                float tipY = Math.Max(10, pt.Y - 32);

                var tipRect = new Rectangle((int)tipX, (int)tipY, (int)tsz.Width + 16, 22);
                using var tipBg = new SolidBrush(UITheme.TextDark);
                g.FillRoundedRectangle(tipBg, tipRect, 6);
                g.DrawString(tooltip, tipFont, Brushes.White, tipX + 8, tipY + 4);
            }
        }
    }
}
