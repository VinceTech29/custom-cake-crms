using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CC.Controls
{
    /// <summary>
    /// A Panel control containing an inner borderless TextBox with rounded background painting,
    /// custom border radius, and placeholder text emulation.
    /// </summary>
    public class RoundedTextBox : Panel
    {
        private readonly TextBox _inner;
        private string _placeholder = "";
        private bool _showingPlaceholder;
        public int CornerRadius { get; set; } = 18;

        public string PlaceholderText
        {
            get => _placeholder;
            set { _placeholder = value; ApplyPlaceholder(); }
        }

        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string Text
        {
            get => _showingPlaceholder ? "" : _inner.Text;
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    _showingPlaceholder = false;
                    _inner.Text = "";
                    ApplyPlaceholder();
                }
                else
                {
                    _showingPlaceholder = false;
                    _inner.Text = value;
                    _inner.ForeColor = this.ForeColor;
                }
            }
        }

        public bool Multiline
        {
            get => _inner.Multiline;
            set { _inner.Multiline = value; }
        }

        public TextBox InnerTextBox => _inner;

        public RoundedTextBox()
        {
            this.DoubleBuffered = true;
            _inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Location = new Point(14, 10),
                Font = this.Font
            };
            _inner.TextChanged += (s, e) => this.Invalidate();
            _inner.GotFocus += Inner_GotFocus;
            _inner.LostFocus += Inner_LostFocus;
            this.Controls.Add(_inner);
            this.Resize += (s, e) => LayoutInner();
        }

        private void LayoutInner()
        {
            if (_inner.Multiline)
            {
                _inner.Location = new Point(16, 10);
                _inner.Size = new Size(this.Width - 32, this.Height - 20);
            }
            else
            {
                _inner.Location = new Point(16, (this.Height - _inner.Height) / 2);
                _inner.Width = this.Width - 32;
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            _inner.Font = this.Font;
            LayoutInner();
        }

        protected override void OnBackColorChanged(EventArgs e)
        {
            base.OnBackColorChanged(e);
            _inner.BackColor = this.BackColor;
        }

        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);
            if (!_showingPlaceholder) _inner.ForeColor = this.ForeColor;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _inner.BackColor = this.BackColor;
            LayoutInner();
            ApplyPlaceholder();
        }

        private void ApplyPlaceholder()
        {
            if (string.IsNullOrEmpty(_inner.Text) || _showingPlaceholder)
            {
                _showingPlaceholder = true;
                _inner.Text = _placeholder;
                _inner.ForeColor = Color.FromArgb(150, 145, 140);
            }
        }

        private void Inner_GotFocus(object? sender, EventArgs e)
        {
            if (_showingPlaceholder)
            {
                _showingPlaceholder = false;
                _inner.Text = "";
                _inner.ForeColor = this.ForeColor;
            }
        }

        private void Inner_LostFocus(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_inner.Text))
            {
                _showingPlaceholder = true;
                _inner.Text = _placeholder;
                _inner.ForeColor = Color.FromArgb(150, 145, 140);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, this.Width - 1, this.Height - 1);
            using (var path = RoundedPath(rect, CornerRadius))
            using (var brush = new SolidBrush(this.BackColor))
            {
                e.Graphics.FillPath(brush, path);
            }
            base.OnPaint(e);
        }

        private static GraphicsPath RoundedPath(Rectangle bounds, int radius)
        {
            int d = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (d <= 0)
            {
                var p = new GraphicsPath();
                p.AddRectangle(bounds);
                return p;
            }

            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
