using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;

namespace CC.Forms.Staff.Orders
{
    /// <summary>
    /// Windowed dialog wrapper for OrderDetailsControl matching reference design (media_1789395593558.png).
    /// </summary>
    public class OrderDetailsForm : Form
    {
        private static readonly Color ColorBorder = Color.FromArgb(140, 70, 90);

        public OrderDetailsForm(SalesOrder order)
        {
            if (order == null) throw new ArgumentNullException(nameof(order));

            Text = $"Order Details - ORD-{order.OrderDate:yyyy}-{order.OrderId:D4}";
            Size = new Size(1140, 720);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = UITheme.CreamBackground;
            DoubleBuffered = true;
            ShowInTaskbar = false;
            Padding = new Padding(12);

            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(ColorBorder, 2.5f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(1, 1, Width - 3, Height - 3), 16);
            };

            var details = new OrderDetailsControl
            {
                Dock = DockStyle.Fill
            };
            details.BackClicked += () => { DialogResult = DialogResult.OK; Close(); };

            Controls.Add(details);

            Load += async (s, e) =>
            {
                this.CenterOnParentOrScreen();
                this.ApplyRoundedRegion(16);
                await details.LoadOrderAsync(order.OrderId);
            };
            Shown += (s, e) => this.CenterOnParentOrScreen();
        }
    }
}
