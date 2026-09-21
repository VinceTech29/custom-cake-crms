using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Forms.Authentication;

namespace CC.Controls
{
    public class PeriodSelectorControl : UserControl
    {
        public event EventHandler<string>? PeriodChanged;

        private string _selectedPeriod = "30d";

        // All possible periods
        private static readonly (string Key, string Label)[] AllPeriods = new[]
        {
            ("1d",  "Today"),
            ("7d",  "7 Days"),
            ("30d", "30 Days"),
            ("90d", "90 Days"),
            ("all", "All Time")
        };

        private readonly (string Key, string Label)[] _periods;
        private readonly FlowLayoutPanel _flow;

        /// <summary>Allowed period keys. Null = show all.</summary>
        public static readonly string[] ManagerPeriods = { "7d", "30d" };
        public static readonly string[] StaffPeriods   = { "1d" };
        public static readonly string[] AdminPeriods   = { "1d", "7d", "30d", "90d", "all" };

        /// <param name="allowedKeys">Pass ManagerPeriods / StaffPeriods / AdminPeriods, or null for all.</param>
        public PeriodSelectorControl(string[]? allowedKeys = null)
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Height = 36;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;

            // Filter to allowed keys
            if (allowedKeys != null && allowedKeys.Length > 0)
            {
                var filtered = new System.Collections.Generic.List<(string, string)>();
                foreach (var (k, l) in AllPeriods)
                    if (System.Array.IndexOf(allowedKeys, k) >= 0)
                        filtered.Add((k, l));
                _periods = filtered.ToArray();
            }
            else
            {
                _periods = AllPeriods;
            }

            // Default to the first period in the set
            _selectedPeriod = _periods.Length > 0 ? _periods[0].Key : "30d";

            _flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            foreach (var (key, label) in _periods)
            {
                var btn = new Button
                {
                    Text = label,
                    Tag = key,
                    Height = 32,
                    Width = 84,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, 8, 0),
                    UseVisualStyleBackColor = false
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is string periodKey)
                    {
                        SelectedPeriod = periodKey;
                    }
                };

                btn.Paint += (s, e) =>
                {
                    if (s is not Button b) return;
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    bool isSelected = (b.Tag as string) == _selectedPeriod;
                    var rect = new Rectangle(0, 0, b.Width - 1, b.Height - 1);

                    if (isSelected)
                    {
                        using var fillBrush = new SolidBrush(UITheme.PrimaryMauve);
                        e.Graphics.FillRoundedRectangle(fillBrush, rect, 8);
                        TextRenderer.DrawText(
                            e.Graphics,
                            b.Text,
                            new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                            rect,
                            Color.White,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                    else
                    {
                        using var fillBrush = new SolidBrush(Color.White);
                        using var borderPen = new Pen(UITheme.BorderColor, 1f);
                        e.Graphics.FillRoundedRectangle(fillBrush, rect, 8);
                        e.Graphics.DrawRoundedRectangle(borderPen, rect, 8);
                        TextRenderer.DrawText(
                            e.Graphics,
                            b.Text,
                            new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                            rect,
                            UITheme.TextDark,
                            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                };

                _flow.Controls.Add(btn);
            }

            Controls.Add(_flow);
        }

        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                if (_selectedPeriod != value)
                {
                    _selectedPeriod = value;
                    UpdatePillStates();
                    PeriodChanged?.Invoke(this, _selectedPeriod);
                }
            }
        }

        private void UpdatePillStates()
        {
            foreach (Control ctrl in _flow.Controls)
            {
                ctrl.Invalidate();
            }
        }
    }
}
