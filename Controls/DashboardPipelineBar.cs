using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CC.Forms.Authentication;

namespace CC.Controls
{
    public class PipelineStage
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public Color StageColor { get; set; } = UITheme.PrimaryMauve;
    }

    public class DashboardPipelineBar : UserControl
    {
        public event EventHandler<string>? StageClicked;

        private string _title = "Pipeline Stages";
        private List<PipelineStage> _stages = new();
        private int _hoveredStage = -1;

        public string Title
        {
            get => _title;
            set { _title = value; Invalidate(); }
        }

        public DashboardPipelineBar()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Padding = new Padding(20);
            Height = 130;
            MinimumSize = new Size(300, 120);
            Cursor = Cursors.Hand;

            MouseMove += OnMouseMoveStage;
            MouseLeave += (s, e) => { _hoveredStage = -1; Invalidate(); };
            Click += OnStageClick;
        }

        public void SetStages(IEnumerable<PipelineStage> stages)
        {
            _stages = stages?.ToList() ?? new List<PipelineStage>();
            _hoveredStage = -1;
            Invalidate();
        }

        private void OnMouseMoveStage(object? sender, MouseEventArgs e)
        {
            if (_stages == null || _stages.Count == 0) return;

            int total = _stages.Sum(s => s.Count);
            if (total <= 0) return;

            int barY = 56;
            int barHeight = 22;
            int left = 24;
            int barWidth = Width - 48;

            if (e.Y < barY || e.Y > barY + barHeight)
            {
                if (_hoveredStage != -1) { _hoveredStage = -1; Invalidate(); }
                return;
            }

            int curX = left;
            int newHover = -1;
            for (int i = 0; i < _stages.Count; i++)
            {
                int segW = (int)((float)_stages[i].Count / total * barWidth);
                if (e.X >= curX && e.X <= curX + segW)
                {
                    newHover = i;
                    break;
                }
                curX += segW;
            }

            if (newHover != _hoveredStage)
            {
                _hoveredStage = newHover;
                Invalidate();
            }
        }

        private void OnStageClick(object? sender, EventArgs e)
        {
            if (_hoveredStage >= 0 && _hoveredStage < _stages.Count)
            {
                StageClicked?.Invoke(this, _stages[_hoveredStage].Name);
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
            using (var titleFont = new Font(UITheme.FontSerif, 11.5F, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(UITheme.TextDark))
            {
                g.DrawString(_title, titleFont, titleBrush, 22, 18);
            }

            int total = _stages.Sum(s => s.Count);
            int barY = 52;
            int barHeight = 22;
            int left = 24;
            int barWidth = Width - 48;

            // Empty state
            if (_stages == null || _stages.Count == 0 || total == 0)
            {
                using var emptyFont = new Font(UITheme.FontSans, 9F, FontStyle.Italic);
                using var emptyBrush = new SolidBrush(UITheme.TextMuted);
                g.DrawString("No pipeline data available for this period.", emptyFont, emptyBrush, left, barY);
                return;
            }

            // Total count badge on top right
            using (var totalFont = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold))
            using (var totalBrush = new SolidBrush(UITheme.TextMuted))
            {
                string totalStr = $"Total: {total} items";
                var sz = g.MeasureString(totalStr, totalFont);
                g.DrawString(totalStr, totalFont, totalBrush, Width - sz.Width - 24, 20);
            }

            // Draw segmented progress bar
            int curX = left;
            for (int i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];
                int segW = (int)Math.Round((float)stage.Count / total * barWidth);
                if (i == _stages.Count - 1)
                {
                    segW = (left + barWidth) - curX; // Fill to exact end
                }

                if (segW > 0)
                {
                    var segRect = new Rectangle(curX, barY, segW, barHeight);
                    Color fillCol = i == _hoveredStage
                        ? Color.FromArgb(Math.Min(255, stage.StageColor.R + 30), Math.Min(255, stage.StageColor.G + 30), Math.Min(255, stage.StageColor.B + 30))
                        : stage.StageColor;

                    using var brush = new SolidBrush(fillCol);
                    g.FillRectangle(brush, segRect);
                }
                curX += segW;
            }

            // Outline around the segmented bar with rounded edges
            using (var outlinePen = new Pen(UITheme.BorderColor, 1f))
            {
                g.DrawRoundedRectangle(outlinePen, new Rectangle(left, barY, barWidth, barHeight), 8);
            }

            // Legend / stage markers below bar
            using var legFont = new Font(UITheme.FontSans, 8F, FontStyle.Regular);
            using var legBold = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
            using var legBrush = new SolidBrush(UITheme.TextDark);

            int legX = left;
            int legY = barY + barHeight + 12;
            for (int i = 0; i < _stages.Count; i++)
            {
                var stage = _stages[i];
                float pct = (float)stage.Count / total * 100f;
                string text = $"{stage.Name}: {stage.Count} ({pct:0}%)";

                // Color dot
                using var dotBrush = new SolidBrush(stage.StageColor);
                g.FillEllipse(dotBrush, legX, legY + 2, 8, 8);

                g.DrawString(text, i == _hoveredStage ? legBold : legFont, legBrush, legX + 12, legY);

                var sz = g.MeasureString(text, legBold);
                legX += (int)sz.Width + 24;
                if (legX > Width - 100 && i < _stages.Count - 1)
                {
                    legX = left;
                    legY += 16;
                }
            }
        }
    }
}
