using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Staff.FollowUps
{
    public class FollowUpForm : Form
    {
        private static readonly Color ColorModalBg = Color.White;
        private static readonly Color ColorFieldBg = Color.FromArgb(239, 230, 222);       // #EFE6DE
        private static readonly Color ColorTextDark = Color.FromArgb(45, 32, 28);         // #2D201C
        private static readonly Color ColorLabel = Color.FromArgb(124, 108, 104);         // #7C6C68
        private static readonly Color ColorAsterisk = Color.FromArgb(156, 107, 115);      // #9C6B73
        private static readonly Color ColorBorder = Color.FromArgb(140, 70, 90);          // High-contrast Noticeable Maroon #8C465A
        private static readonly Color ColorDivider = Color.FromArgb(239, 230, 222);

        private readonly CustomerFollowUp? existingFollowUp;
        private ComboBox cbCustomer = null!;
        private ComboBox cbStaff = null!;
        private ComboBox cbStatus = null!;
        private DateTimePicker dtFollow = null!;
        private DateTimePicker dtNext = null!;
        private TextBox txtNotes = null!;

        public FollowUpForm(CustomerFollowUp? followUp = null)
        {
            existingFollowUp = followUp;
            InitializeModal();
            BuildContent();

            Load += async (s, e) => await LoadDataAsync();
        }

        private void InitializeModal()
        {
            Text = existingFollowUp == null ? "Add Follow-up" : $"Edit Follow-up #FOL-{existingFollowUp.FollowUpId}";
            Size = new Size(600, 700);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ColorModalBg;
            DoubleBuffered = true;
            ShowInTaskbar = false;

            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(ColorBorder, 2.5f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(1, 1, Width - 3, Height - 3), 16);
            };

            Load += (s, e) =>
            {
                this.CenterOnParentOrScreen();
                this.ApplyRoundedRegion(16);
            };
            Shown += (s, e) => this.CenterOnParentOrScreen();
        }

        private void BuildContent()
        {
            var root = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorModalBg,
                Padding = Padding.Empty
            };

            // 1. HEADER (74px)
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                BackColor = ColorModalBg,
                Padding = new Padding(32, 0, 32, 0)
            };

            var lblTitle = new Label
            {
                Text = existingFollowUp == null ? "Add Follow-up" : $"Edit Follow-up #FOL-{existingFollowUp.FollowUpId}",
                Font = new Font(UITheme.FontSerif, 18f, FontStyle.Bold),
                ForeColor = ColorTextDark,
                AutoSize = true,
                Location = new Point(32, 24)
            };

            var btnClose = new Button
            {
                Text = "✕",
                Size = new Size(36, 36),
                Location = new Point(headerPanel.Width - 68, 19),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                UseVisualStyleBackColor = false
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnClose.FlatAppearance.MouseDownBackColor = Color.Transparent;

            btnClose.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool hovered = btnClose.ClientRectangle.Contains(btnClose.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? Color.FromArgb(228, 220, 212) : Color.FromArgb(240, 234, 228));
                e.Graphics.FillEllipse(bg, 0, 0, btnClose.Width - 1, btnClose.Height - 1);
                using var font = new Font(UITheme.FontSans, 10f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "✕", font, btnClose.ClientRectangle, ColorLabel,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnClose.MouseEnter += (s, e) => btnClose.Invalidate();
            btnClose.MouseLeave += (s, e) => btnClose.Invalidate();
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Point dragStart = Point.Empty;
            headerPanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragStart = e.Location; };
            headerPanel.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left) Location = new Point(Location.X + (e.X - dragStart.X), Location.Y + (e.Y - dragStart.Y)); };
            lblTitle.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) dragStart = e.Location; };
            lblTitle.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left) Location = new Point(Location.X + (e.X - dragStart.X), Location.Y + (e.Y - dragStart.Y)); };

            var headerDivider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ColorDivider
            };

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(btnClose);
            headerPanel.Controls.Add(headerDivider);

            // 2. FOOTER (78px)
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 78,
                BackColor = ColorModalBg,
                Padding = new Padding(32, 0, 32, 0)
            };

            var footerDivider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = ColorDivider
            };

            var btnSubmit = new Button
            {
                Text = existingFollowUp == null ? "Save Follow-up" : "Update Follow-up",
                Size = new Size(150, 44),
                Location = new Point(footerPanel.Width - 32 - 150, 17),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 10f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = UITheme.PrimaryMauve,
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.ApplyRoundedRegion(8);
            btnSubmit.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Size = new Size(110, 44),
                Location = new Point(btnSubmit.Left - 12 - 110, 17),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 10f, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            footerPanel.Controls.Add(footerDivider);
            footerPanel.Controls.Add(btnCancel);
            footerPanel.Controls.Add(btnSubmit);

            // 3. SCROLLABLE BODY
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = ColorModalBg,
                Padding = new Padding(32, 20, 32, 20)
            };

            int contentWidth = 536; // 600 - 64
            int leftMargin = 32;
            int y = 14;

            // Customer field
            var custField = CreateFieldGroup("CUSTOMER", true, contentWidth, out cbCustomer);
            custField.Location = new Point(leftMargin, y);
            scrollPanel.Controls.Add(custField);
            y += custField.Height + 14;

            // 2-col row: Staff User & Status
            int colGap = 16;
            int colWidth = (contentWidth - colGap) / 2;

            var staffField = CreateFieldGroup("STAFF USER", false, colWidth, out cbStaff);
            staffField.Location = new Point(leftMargin, y);
            scrollPanel.Controls.Add(staffField);

            var statusField = CreateFieldGroup("STATUS", true, colWidth, out cbStatus);
            statusField.Location = new Point(leftMargin + colWidth + colGap, y);
            cbStatus.Items.AddRange(new object[] { "Pending", "Completed", "Cancelled" });
            cbStatus.SelectedIndex = 0;
            scrollPanel.Controls.Add(statusField);
            y += staffField.Height + 14;

            // 2-col row: Follow-up Date & Next Follow-up Date
            var dtFollowField = CreateDateField("FOLLOW-UP DATE", true, colWidth, out dtFollow);
            dtFollowField.Location = new Point(leftMargin, y);
            scrollPanel.Controls.Add(dtFollowField);

            var dtNextField = CreateDateField("NEXT FOLLOW-UP DATE", false, colWidth, out dtNext);
            dtNextField.Location = new Point(leftMargin + colWidth + colGap, y);
            dtNext.Value = DateTime.Now.AddDays(3);
            scrollPanel.Controls.Add(dtNextField);
            y += dtFollowField.Height + 14;

            // Notes / Task textarea
            var notesField = CreateTextArea("NOTES / TASK INSTRUCTIONS", false, contentWidth, 110, out txtNotes, "Describe the follow-up action or feedback notes...");
            notesField.Location = new Point(leftMargin, y);
            scrollPanel.Controls.Add(notesField);
            y += notesField.Height + 20;

            root.Controls.Add(scrollPanel);
            root.Controls.Add(footerPanel);
            root.Controls.Add(headerPanel);
            Controls.Add(root);
        }

        private Panel CreateFieldGroup(string label, bool required, int width, out ComboBox comboBox)
        {
            const int labelHeight = 24;
            const int gap = 8;
            const int inputHeight = 48;
            int totalHeight = labelHeight + gap + inputHeight;

            var container = new Panel { Size = new Size(width, totalHeight), BackColor = Color.Transparent };

            var lblFlow = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                Size = new Size(width, labelHeight),
                BackColor = Color.Transparent,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            var lbl = new Label
            {
                Text = label.ToUpperInvariant(),
                Font = new Font(UITheme.FontSans, 8f, FontStyle.Bold),
                ForeColor = ColorLabel,
                AutoSize = true,
                Margin = new Padding(0, 4, 2, 0)
            };
            lblFlow.Controls.Add(lbl);

            if (required)
            {
                lblFlow.Controls.Add(new Label
                {
                    Text = "*",
                    Font = new Font(UITheme.FontSans, 9f, FontStyle.Bold),
                    ForeColor = ColorAsterisk,
                    AutoSize = true,
                    Margin = new Padding(0, 2, 0, 0)
                });
            }

            var boxPanel = new Panel
            {
                Location = new Point(0, labelHeight + gap),
                Size = new Size(width, inputHeight),
                BackColor = ColorFieldBg
            };
            boxPanel.ApplyRoundedRegion(8);

            comboBox = new ComboBox
            {
                Location = new Point(14, 11),
                Width = width - 28,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorFieldBg,
                ForeColor = ColorTextDark,
                Font = new Font(UITheme.FontSans, 10f)
            };

            boxPanel.Controls.Add(comboBox);
            container.Controls.Add(lblFlow);
            container.Controls.Add(boxPanel);
            return container;
        }

        private Panel CreateDateField(string label, bool required, int width, out DateTimePicker dtPicker)
        {
            const int labelHeight = 24;
            const int gap = 8;
            const int inputHeight = 48;
            int totalHeight = labelHeight + gap + inputHeight;

            var container = new Panel { Size = new Size(width, totalHeight), BackColor = Color.Transparent };

            var lblFlow = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                Size = new Size(width, labelHeight),
                BackColor = Color.Transparent,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            var lbl = new Label
            {
                Text = label.ToUpperInvariant(),
                Font = new Font(UITheme.FontSans, 8f, FontStyle.Bold),
                ForeColor = ColorLabel,
                AutoSize = true,
                Margin = new Padding(0, 4, 2, 0)
            };
            lblFlow.Controls.Add(lbl);

            if (required)
            {
                lblFlow.Controls.Add(new Label
                {
                    Text = "*",
                    Font = new Font(UITheme.FontSans, 9f, FontStyle.Bold),
                    ForeColor = ColorAsterisk,
                    AutoSize = true,
                    Margin = new Padding(0, 2, 0, 0)
                });
            }

            var boxPanel = new Panel
            {
                Location = new Point(0, labelHeight + gap),
                Size = new Size(width, inputHeight),
                BackColor = ColorFieldBg
            };
            boxPanel.ApplyRoundedRegion(8);

            dtPicker = new DateTimePicker
            {
                Location = new Point(14, 11),
                Width = width - 28,
                Font = new Font(UITheme.FontSans, 10f),
                Value = DateTime.Now
            };

            boxPanel.Controls.Add(dtPicker);
            container.Controls.Add(lblFlow);
            container.Controls.Add(boxPanel);
            return container;
        }

        private Panel CreateTextArea(string label, bool required, int width, int height, out TextBox textBox, string placeholder = "")
        {
            const int labelHeight = 24;
            const int gap = 8;
            int totalHeight = labelHeight + gap + height;

            var container = new Panel { Size = new Size(width, totalHeight), BackColor = Color.Transparent };

            var lblFlow = new FlowLayoutPanel
            {
                Location = new Point(0, 0),
                Size = new Size(width, labelHeight),
                BackColor = Color.Transparent,
                WrapContents = false,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            var lbl = new Label
            {
                Text = label.ToUpperInvariant(),
                Font = new Font(UITheme.FontSans, 8f, FontStyle.Bold),
                ForeColor = ColorLabel,
                AutoSize = true,
                Margin = new Padding(0, 4, 2, 0)
            };
            lblFlow.Controls.Add(lbl);

            var boxPanel = new Panel
            {
                Location = new Point(0, labelHeight + gap),
                Size = new Size(width, height),
                BackColor = ColorFieldBg
            };
            boxPanel.ApplyRoundedRegion(8);

            textBox = new TextBox
            {
                Location = new Point(14, 10),
                Size = new Size(width - 28, height - 20),
                Multiline = true,
                BorderStyle = BorderStyle.None,
                BackColor = ColorFieldBg,
                ForeColor = ColorTextDark,
                Font = new Font(UITheme.FontSans, 10f),
                PlaceholderText = placeholder,
                ScrollBars = ScrollBars.Vertical
            };

            boxPanel.Controls.Add(textBox);
            container.Controls.Add(lblFlow);
            container.Controls.Add(boxPanel);
            return container;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var customers = await CrmDataService.GetCustomersAsync();
                cbCustomer.Items.Clear();
                foreach (var c in customers)
                {
                    cbCustomer.Items.Add(new ComboBoxItem(c.CustomerId, $"{c.FirstName} {c.LastName}"));
                }

                cbStaff.Items.Clear();
                cbStaff.Items.Add(new ComboBoxItem(SessionService.CurrentUser?.UserId ?? CrmDataService.DefaultUserId, SessionService.CurrentUser?.Username ?? "Staff User"));

                if (cbCustomer.Items.Count > 0) cbCustomer.SelectedIndex = 0;
                if (cbStaff.Items.Count > 0) cbStaff.SelectedIndex = 0;

                if (existingFollowUp != null)
                {
                    for (int i = 0; i < cbCustomer.Items.Count; i++)
                    {
                        if (cbCustomer.Items[i] is ComboBoxItem item && item.Id == existingFollowUp.CustomerId)
                        {
                            cbCustomer.SelectedIndex = i;
                            break;
                        }
                    }

                    cbStatus.Items.Clear();
                    string currStatus = FollowUpStatusWorkflow.GetStatusName(existingFollowUp.StatusId);
                    cbStatus.Items.Add(currStatus);

                    if (FollowUpStatusWorkflow.IsLocked(existingFollowUp.StatusId))
                    {
                        cbStatus.SelectedIndex = 0;
                        cbStatus.Enabled = false;
                    }
                    else
                    {
                        foreach (var next in FollowUpStatusWorkflow.GetNextValidStatusNames(existingFollowUp.StatusId))
                        {
                            if (!cbStatus.Items.Contains(next)) cbStatus.Items.Add(next);
                        }
                        cbStatus.SelectedIndex = 0;
                        cbStatus.Enabled = true;
                    }

                    dtFollow.Value = existingFollowUp.FollowUpDate;
                    if (existingFollowUp.NextFollowUpDate.HasValue) dtNext.Value = existingFollowUp.NextFollowUpDate.Value;
                    txtNotes.Text = existingFollowUp.Notes ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load customers from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!(cbCustomer.SelectedItem is ComboBoxItem customerItem))
            {
                MessageBox.Show("Please select a customer.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var staffId = cbStaff.SelectedItem is ComboBoxItem staffItem ? staffItem.Id : (SessionService.CurrentUser?.UserId ?? CrmDataService.DefaultUserId);

            int targetStatusId = cbStatus.SelectedItem != null
                ? FollowUpStatusWorkflow.GetStatusId(cbStatus.SelectedItem.ToString())
                : (existingFollowUp?.StatusId ?? 0);

            try
            {
                if (existingFollowUp == null)
                {
                    var follow = new CustomerFollowUp
                    {
                        CustomerId = customerItem.Id,
                        StaffUserId = staffId,
                        StatusId = targetStatusId,
                        FollowUpDate = dtFollow.Value,
                        NextFollowUpDate = dtNext.Value,
                        Notes = txtNotes.Text.Trim()
                    };

                    await CrmDataService.CreateFollowUpAsync(follow);
                    MessageBox.Show("New follow-up task saved successfully to database.", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    existingFollowUp.CustomerId = customerItem.Id;
                    existingFollowUp.StaffUserId = staffId;
                    existingFollowUp.StatusId = targetStatusId;
                    existingFollowUp.FollowUpDate = dtFollow.Value;
                    existingFollowUp.NextFollowUpDate = dtNext.Value;
                    existingFollowUp.Notes = txtNotes.Text.Trim();

                    await CrmDataService.UpdateFollowUpAsync(existingFollowUp);
                    MessageBox.Show($"Follow-up #FOL-{existingFollowUp.FollowUpId} updated successfully in database.", "Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to save follow-up to database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private class ComboBoxItem
        {
            public int Id { get; }
            public string Name { get; }
            public ComboBoxItem(int id, string name) { Id = id; Name = name; }
            public override string ToString() => Name;
        }
    }
}

