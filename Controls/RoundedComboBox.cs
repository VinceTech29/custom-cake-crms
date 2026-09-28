using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CC.Controls
{
    /// <summary>
    /// A Panel wrapper for ComboBox with rounded background painting matching RoundedTextBox.
    /// </summary>
    public class RoundedComboBox : Panel
    {
        private readonly ComboBox _inner;
        public int CornerRadius { get; set; } = 18;

        public ComboBox InnerComboBox => _inner;

        public object? SelectedItem
        {
            get => _inner.SelectedItem;
            set => _inner.SelectedItem = value;
        }

        public int SelectedIndex
        {
            get => _inner.SelectedIndex;
            set => _inner.SelectedIndex = value;
        }

        public ComboBox.ObjectCollection Items => _inner.Items;

        public RoundedComboBox()
        {
            this.DoubleBuffered = true;
            _inner = new ComboBox
            {
                FlatStyle = FlatStyle.Flat,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = this.Font
            };
            this.Controls.Add(_inner);
            this.Resize += (s, e) => LayoutInner();
        }

        private void LayoutInner()
        {
            _inner.Location = new Point(14, (this.Height - _inner.Height) / 2);
            _inner.Width = this.Width - 28;
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
            _inner.ForeColor = this.ForeColor;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            _inner.BackColor = this.BackColor;
            LayoutInner();
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
