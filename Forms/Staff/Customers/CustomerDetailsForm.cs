using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;

namespace CC.Forms.Staff.Customers
{
    public class CustomerDetailsForm : Form
    {
        private static readonly Color ColorBorder = Color.FromArgb(140, 70, 90);

        public CustomerDetailsForm(Customer customer)
        {
            string fullName = $"{customer.FirstName} {customer.LastName}".Trim();

            Text = $"Customer Details - {fullName}";
            Size = new Size(1120, 740);
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

            var details = new CustomerDetailsControl
            {
                Dock = DockStyle.Fill
            };
            details.BackClicked += () => { DialogResult = DialogResult.OK; Close(); };

            Controls.Add(details);

            Load += async (s, e) =>
            {
                this.CenterOnParentOrScreen();
                this.ApplyRoundedRegion(16);
                await details.LoadCustomerAsync(customer.CustomerId);
            };
            Shown += (s, e) => this.CenterOnParentOrScreen();
        }
    }
}
