using System.Windows.Forms;

namespace CC.Controls
{
    public static class ViewHost
    {
        // Embed a Form into a host panel. Previous embedded form (if any) will be closed.
        public static void ShowFormInPanel(Panel host, Form form)
        {
            if (host == null || form == null) return;

            // Close and dispose previous embedded form if any
            if (host.Tag is Form previous)
            {
                try { previous.Close(); previous.Dispose(); } catch { }
                host.Tag = null;
            }

            form.TopLevel = false;
            form.FormBorderStyle = FormBorderStyle.None;
            form.Dock = DockStyle.Fill;
            form.TopMost = false;

            host.Controls.Clear();
            host.Controls.Add(form);
            host.Tag = form;

            form.Show();
        }
    }
}
