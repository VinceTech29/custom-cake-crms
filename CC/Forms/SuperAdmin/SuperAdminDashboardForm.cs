using System;
using System.Drawing;
using System.Windows.Forms;

namespace CC.Forms.SuperAdmin
{
    public class SuperAdminDashboardForm : Form
    {
        public SuperAdminDashboardForm()
        {
            Text = "Super Admin Dashboard";
            Width = 900;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48F));

            var lbl = new Label { Text = "Super Admin Dashboard", Font = new Font("Segoe UI", 16, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            var pnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10) };
            var msg = new Label { Text = "Super Admin modules will be implemented in a later phase.", AutoSize = true, Dock = DockStyle.Top };
            var btnLogout = new Button { Text = "Logout", Anchor = AnchorStyles.Right, Width = 120 };
            btnLogout.Click += (s, e) => Close();

            pnl.Controls.Add(msg);
            var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            bottom.Controls.Add(btnLogout);

            layout.Controls.Add(lbl, 0, 0);
            layout.Controls.Add(pnl, 0, 1);
            layout.Controls.Add(bottom, 0, 2);

            Controls.Add(layout);
        }
    }
}
