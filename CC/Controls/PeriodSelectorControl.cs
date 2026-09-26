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
        private bool _isInternalUpdate = false;

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
        private readonly DateTimePicker _dtpFrom;
        private readonly DateTimePicker _dtpTo;
        private readonly Button _btnApply;

        /// <summary>Allowed period keys. Null = show all.</summary>
        public static readonly string[] ManagerPeriods = { "7d", "30d" };
        public static readonly string[] StaffPeriods   = { "1d" };
        public static readonly string[] AdminPeriods   = { "1d", "7d", "30d", "90d", "all" };

        public DateTime StartDate => _dtpFrom.Value.Date;
        public DateTime EndDate => _dtpTo.Value.Date;

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

            // 1. Preset shortcut pill buttons
            foreach (var (key, label) in _periods)
            {
                var btn = new Button
                {
                    Text = label,
                    Tag = key,
                    Height = 32,
                    Width = 76,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 0, 6, 0),
                    UseVisualStyleBackColor = false
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) =>
                {
                    if (s is Button b && b.Tag is string periodKey)
                    {
                        ApplyPreset(periodKey);
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

            // 2. Vertical Divider
            var divider = new Panel
            {
                Width = 1,
                Height = 24,
                BackColor = UITheme.BorderColor,
                Margin = new Padding(4, 4, 8, 4)
            };
            _flow.Controls.Add(divider);

            // 3. "From:" label
            var lblFrom = new Label
            {
                Text = "From:",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0)
            };
            _flow.Controls.Add(lblFrom);

            // 4. DateTimePicker From
            _dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Height = 32,
                Width = 100,
                Font = new Font(UITheme.FontSans, 9F),
                Margin = new Padding(0, 1, 6, 0),
                Value = GetInitialStartDate(_selectedPeriod)
            };
            _dtpFrom.ValueChanged += (s, e) =>
            {
                if (_isInternalUpdate) return;
                // Enforce From <= To
                if (_dtpFrom.Value > _dtpTo.Value)
                {
                    _isInternalUpdate = true;
                    _dtpTo.Value = _dtpFrom.Value;
                    _isInternalUpdate = false;
                }
                _selectedPeriod = "custom";
                UpdatePillStates();
            };
            _flow.Controls.Add(_dtpFrom);

            // 5. "To:" label
            var lblTo = new Label
            {
                Text = "To:",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 8, 4, 0)
            };
            _flow.Controls.Add(lblTo);

            // 6. DateTimePicker To
            _dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Height = 32,
                Width = 100,
                Font = new Font(UITheme.FontSans, 9F),
                Margin = new Padding(0, 1, 6, 0),
                Value = DateTime.Today
            };
            _dtpTo.ValueChanged += (s, e) =>
            {
                if (_isInternalUpdate) return;
                // Enforce To >= From
                if (_dtpTo.Value < _dtpFrom.Value)
                {
                    _isInternalUpdate = true;
                    _dtpFrom.Value = _dtpTo.Value;
                    _isInternalUpdate = false;
                }
                _selectedPeriod = "custom";
                UpdatePillStates();
            };
            _flow.Controls.Add(_dtpTo);

            // 7. "Apply" Button
            _btnApply = new Button
            {
                Text = "Apply",
                Height = 32,
                Width = 64,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Margin = new Padding(0, 0, 4, 0),
                UseVisualStyleBackColor = false
            };
            _btnApply.FlatAppearance.BorderSize = 0;
            _btnApply.Click += (s, e) =>
            {
                if (_dtpFrom.Value > _dtpTo.Value)
                {
                    MessageBox.Show("Start date (From) cannot be later than end date (To).", "Date Range Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                _selectedPeriod = "custom";
                UpdatePillStates();
                PeriodChanged?.Invoke(this, $"range:{_dtpFrom.Value:yyyy-MM-dd}:{_dtpTo.Value:yyyy-MM-dd}");
            };
            _flow.Controls.Add(_btnApply);

            Controls.Add(_flow);
        }

        private static DateTime GetInitialStartDate(string periodKey) => periodKey?.ToLowerInvariant() switch
        {
            "1d" => DateTime.Today,
            "7d" => DateTime.Today.AddDays(-7),
            "30d" => DateTime.Today.AddDays(-30),
            "90d" => DateTime.Today.AddDays(-90),
            "all" => DateTime.Today.AddYears(-2),
            _ => DateTime.Today.AddDays(-30)
        };

        private void ApplyPreset(string periodKey)
        {
            _isInternalUpdate = true;
            try
            {
                switch (periodKey.ToLowerInvariant())
                {
                    case "1d":
                        _dtpFrom.Value = DateTime.Today;
                        _dtpTo.Value = DateTime.Today;
                        break;
                    case "7d":
                        _dtpFrom.Value = DateTime.Today.AddDays(-7);
                        _dtpTo.Value = DateTime.Today;
                        break;
                    case "30d":
                        _dtpFrom.Value = DateTime.Today.AddDays(-30);
                        _dtpTo.Value = DateTime.Today;
                        break;
                    case "90d":
                        _dtpFrom.Value = DateTime.Today.AddDays(-90);
                        _dtpTo.Value = DateTime.Today;
                        break;
                    case "all":
                        _dtpFrom.Value = DateTime.Today.AddYears(-2);
                        _dtpTo.Value = DateTime.Today;
                        break;
                }
            }
            finally
            {
                _isInternalUpdate = false;
            }

            _selectedPeriod = periodKey;
            UpdatePillStates();
            PeriodChanged?.Invoke(this, $"range:{_dtpFrom.Value:yyyy-MM-dd}:{_dtpTo.Value:yyyy-MM-dd}");
        }

        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                if (_selectedPeriod != value)
                {
                    _selectedPeriod = value;
                    if (_selectedPeriod != "custom")
                    {
                        ApplyPreset(_selectedPeriod);
                    }
                    else
                    {
                        UpdatePillStates();
                    }
                }
            }
        }

        public void SetDateRange(DateTime from, DateTime to)
        {
            if (from > to) from = to;
            _isInternalUpdate = true;
            try
            {
                _dtpFrom.Value = from;
                _dtpTo.Value = to;
            }
            finally
            {
                _isInternalUpdate = false;
            }
            _selectedPeriod = "custom";
            UpdatePillStates();
            PeriodChanged?.Invoke(this, $"range:{_dtpFrom.Value:yyyy-MM-dd}:{_dtpTo.Value:yyyy-MM-dd}");
        }

        private void UpdatePillStates()
        {
            foreach (Control ctrl in _flow.Controls)
            {
                if (ctrl is Button)
                {
                    ctrl.Invalidate();
                }
            }
        }
    }
}
