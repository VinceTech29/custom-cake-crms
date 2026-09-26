using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Staff.Orders
{
    /// <summary>
    /// Modal dialog for advancing the status of an order strictly forward in the workflow.
    /// Backward status changes and re-selecting the current status are disallowed.
    /// </summary>
    public class OrderStatusModal : Form
    {
        private static readonly Color ColorModalBg = Color.White;
        private static readonly Color ColorBorder = Color.FromArgb(140, 70, 90);       // Maroon #8C465A
        private static readonly Color ColorLabel = Color.FromArgb(124, 108, 104);       // Uppercase Gray #7C6C68
        private static readonly Color ColorPrimaryBtn = Color.FromArgb(140, 70, 90);    // Maroon #8C465A
        private static readonly Color ColorCancelBtn = Color.FromArgb(239, 231, 223);   // Beige #EFE7DF
        private static readonly Color ColorFieldBg = Color.FromArgb(239, 230, 222);    // #EFE6DE
        private static readonly Color ColorTextDark = Color.FromArgb(31, 27, 24);

        private readonly SalesOrder _order;
        private ComboBox cbNextStatus = null!;
        private TextBox txtNote = null!;
        private Button btnSave = null!;

        public OrderStatusModal(SalesOrder order)
        {
            _order = order ?? throw new ArgumentNullException(nameof(order));
            InitializeModal();
            BuildContent();
        }

        private void InitializeModal()
        {
            Text = $"Update Status - Order #ORD-{_order.OrderId}";
            Size = new Size(540, 420);
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
                Padding = new Padding(28, 24, 28, 24)
            };

            // 1. Header
            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 60,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));

            var headerStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = "Update Order Status",
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = ColorTextDark,
                AutoSize = true
            };

            string custName = _order.Customer != null ? $"{_order.Customer.FirstName} {_order.Customer.LastName}" : $"Customer #{_order.CustomerId}";
            var lblSub = new Label
            {
                Text = $"Order #ORD-{_order.OrderId} • {custName}",
                Font = new Font(UITheme.FontSans, 9.5F),
                ForeColor = ColorLabel,
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 0)
            };
            headerStack.Controls.Add(lblTitle);
            headerStack.Controls.Add(lblSub);

            var btnClose = new Button
            {
                Text = "\u2715",
                Size = new Size(36, 36),
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
                using var font = new Font(UITheme.FontSans, 9f, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, "\u2715", font, btnClose.ClientRectangle, Color.FromArgb(124, 108, 104),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnClose.MouseEnter += (s, e) => btnClose.Invalidate();
            btnClose.MouseLeave += (s, e) => btnClose.Invalidate();
            btnClose.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            headerTable.Controls.Add(headerStack, 0, 0);
            headerTable.Controls.Add(btnClose, 1, 0);

            // Divider
            var divider = new Panel
            {
                Dock = DockStyle.Top,
                Height = 1,
                BackColor = Color.FromArgb(239, 230, 222),
                Margin = new Padding(0, 10, 0, 14)
            };

            // 2. Content Panel
            var bodyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorModalBg,
                Padding = new Padding(0, 14, 0, 0)
            };

            int curY = 6;
            int fullWidth = 484;

            // Current Status Banner / Row
            string currentStatusName = OrderStatusWorkflow.GetStatusName(_order.StatusId);
            bool isLocked = OrderStatusWorkflow.IsLocked(_order.StatusId);

            var currRow = new Panel
            {
                Location = new Point(0, curY),
                Size = new Size(fullWidth, 34),
                BackColor = Color.Transparent
            };
            var lblCurrLabel = new Label
            {
                Text = "CURRENT STATUS:",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = ColorLabel,
                Location = new Point(0, 7),
                AutoSize = true
            };
            currRow.Controls.Add(lblCurrLabel);

            var currBadge = new Panel
            {
                Location = new Point(130, 3),
                Size = new Size(130, 28),
                BackColor = Color.Transparent
            };
            currBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                (string _, Color bg, Color fg) = GetBadgeColors(_order.StatusId);
                UITheme.DrawStatusPill(e.Graphics, new Rectangle(0, 0, currBadge.Width - 1, currBadge.Height - 1), currentStatusName, bg, fg, fg);
            };
            currRow.Controls.Add(currBadge);

            bodyPanel.Controls.Add(currRow);
            curY += 40;

            if (isLocked)
            {
                // Locked notice banner
                var lockedCard = new Panel
                {
                    Location = new Point(0, curY),
                    Size = new Size(fullWidth, 68),
                    BackColor = Color.FromArgb(254, 242, 242) // Soft pink/red
                };
                lockedCard.ApplyRoundedRegion(10);

                var lblLockedIcon = new Label
                {
                    Text = "\u26A0",
                    Font = new Font("Segoe UI Emoji", 14F),
                    ForeColor = Color.FromArgb(185, 28, 28),
                    Location = new Point(12, 14),
                    Size = new Size(30, 30)
                };
                var lblLockedMsg = new Label
                {
                    Text = $"This order is '{currentStatusName}' and has reached a terminal state in the workflow. Reverting or modifying status is not permitted under forward-only business rules.",
                    Font = new Font(UITheme.FontSans, 9F),
                    ForeColor = Color.FromArgb(153, 27, 27),
                    Location = new Point(48, 12),
                    Size = new Size(fullWidth - 60, 48)
                };
                lockedCard.Controls.Add(lblLockedIcon);
                lockedCard.Controls.Add(lblLockedMsg);
                bodyPanel.Controls.Add(lockedCard);
                curY += 80;
            }
            else
            {
                // Forward-only Next Status Selection Dropdown
                var lblNext = new Label
                {
                    Text = "NEXT STATUS (FORWARD-ONLY) *",
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                    ForeColor = ColorLabel,
                    Location = new Point(0, curY),
                    AutoSize = true
                };
                curY += 24;

                var nextStatuses = OrderStatusWorkflow.GetNextValidStatusNames(_order.StatusId);

                var comboWrap = new Panel
                {
                    Location = new Point(0, curY),
                    Size = new Size(fullWidth, 44),
                    BackColor = ColorFieldBg
                };
                comboWrap.ApplyRoundedRegion(8);

                cbNextStatus = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = ColorFieldBg,
                    ForeColor = ColorTextDark,
                    Font = new Font(UITheme.FontSans, 10F, FontStyle.Bold),
                    Location = new Point(12, 10),
                    Width = fullWidth - 24
                };

                foreach (var s in nextStatuses)
                {
                    cbNextStatus.Items.Add(s);
                }
                if (cbNextStatus.Items.Count > 0) cbNextStatus.SelectedIndex = 0;

                comboWrap.Controls.Add(cbNextStatus);
                bodyPanel.Controls.Add(lblNext);
                bodyPanel.Controls.Add(comboWrap);
                curY += 56;

                // Optional Notes
                var lblNote = new Label
                {
                    Text = "STATUS CHANGE NOTE / AUDIT LOG (OPTIONAL)",
                    Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                    ForeColor = ColorLabel,
                    Location = new Point(0, curY),
                    AutoSize = true
                };
                curY += 24;

                var noteWrap = new Panel
                {
                    Location = new Point(0, curY),
                    Size = new Size(fullWidth, 68),
                    BackColor = ColorFieldBg
                };
                noteWrap.ApplyRoundedRegion(8);

                txtNote = new TextBox
                {
                    Multiline = true,
                    BorderStyle = BorderStyle.None,
                    BackColor = ColorFieldBg,
                    ForeColor = ColorTextDark,
                    Font = new Font(UITheme.FontSans, 9.5F),
                    Location = new Point(12, 10),
                    Size = new Size(fullWidth - 24, 48),
                    ScrollBars = ScrollBars.Vertical
                };
                noteWrap.Controls.Add(txtNote);
                bodyPanel.Controls.Add(lblNote);
                bodyPanel.Controls.Add(noteWrap);
                curY += 80;
            }

            // 3. Footer Buttons
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 52,
                BackColor = ColorModalBg
            };

            var btnCancel = new Button
            {
                Text = isLocked ? "Close" : "Cancel",
                Size = new Size(110, 42),
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                UseVisualStyleBackColor = false,
                Location = new Point(fullWidth - 230, 4)
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool hovered = btnCancel.ClientRectangle.Contains(btnCancel.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? Color.FromArgb(228, 219, 210) : ColorCancelBtn);
                e.Graphics.FillRoundedRectangle(bg, new Rectangle(0, 0, btnCancel.Width, btnCancel.Height), 10);
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, btnCancel.Text, font, btnCancel.ClientRectangle, ColorTextDark,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnCancel.MouseEnter += (s, e) => btnCancel.Invalidate();
            btnCancel.MouseLeave += (s, e) => btnCancel.Invalidate();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            footerPanel.Controls.Add(btnCancel);

            if (!isLocked)
            {
                btnSave = new Button
                {
                    Text = "Save Status",
                    Size = new Size(110, 42),
                    Cursor = Cursors.Hand,
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.Transparent,
                    UseVisualStyleBackColor = false,
                    Location = new Point(fullWidth - 110, 4)
                };
                btnSave.FlatAppearance.BorderSize = 0;
                btnSave.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    bool hovered = btnSave.ClientRectangle.Contains(btnSave.PointToClient(Cursor.Position));
                    using var bg = new SolidBrush(hovered ? Color.FromArgb(120, 58, 76) : ColorPrimaryBtn);
                    e.Graphics.FillRoundedRectangle(bg, new Rectangle(0, 0, btnSave.Width, btnSave.Height), 10);
                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                    TextRenderer.DrawText(e.Graphics, "Save Status", font, btnSave.ClientRectangle, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };
                btnSave.MouseEnter += (s, e) => btnSave.Invalidate();
                btnSave.MouseLeave += (s, e) => btnSave.Invalidate();
                btnSave.Click += async (s, e) => await SaveStatusAsync();

                footerPanel.Controls.Add(btnSave);
                AcceptButton = btnSave;
            }

            CancelButton = btnCancel;

            root.Controls.Add(bodyPanel);
            root.Controls.Add(footerPanel);
            root.Controls.Add(divider);
            root.Controls.Add(headerTable);

            Controls.Add(root);
        }

        private static (string Name, Color Bg, Color Fg) GetBadgeColors(int statusId)
        {
            return statusId switch
            {
                0 => ("Pending", ColorTranslator.FromHtml("#FEF3C7"), ColorTranslator.FromHtml("#B45309")),
                1 => ("Confirmed", ColorTranslator.FromHtml("#DBEAFE"), ColorTranslator.FromHtml("#1D4ED8")),
                2 => ("Processing", ColorTranslator.FromHtml("#FEF3C7"), ColorTranslator.FromHtml("#B45309")),
                3 => ("Completed", ColorTranslator.FromHtml("#DCFCE7"), ColorTranslator.FromHtml("#15803D")),
                4 => ("Ready", ColorTranslator.FromHtml("#DCFCE7"), ColorTranslator.FromHtml("#15803D")),
                5 => ("Cancelled", ColorTranslator.FromHtml("#FEE2E2"), ColorTranslator.FromHtml("#B91C1C")),
                _ => ("Pending", ColorTranslator.FromHtml("#FEF3C7"), ColorTranslator.FromHtml("#B45309"))
            };
        }

        private async Task SaveStatusAsync()
        {
            if (cbNextStatus == null || cbNextStatus.SelectedItem == null)
            {
                MessageBox.Show("Please select a status to proceed.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedStatusName = cbNextStatus.SelectedItem.ToString()!;
            int targetStatusId = OrderStatusWorkflow.GetStatusId(selectedStatusName);

            try
            {
                _order.StatusId = targetStatusId;

                string noteText = txtNote?.Text.Trim() ?? string.Empty;
                if (!string.IsNullOrEmpty(noteText))
                {
                    _order.Notes = string.IsNullOrWhiteSpace(_order.Notes)
                        ? $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {selectedStatusName}: {noteText}"
                        : $"{_order.Notes}\r\n[{DateTime.Now:yyyy-MM-dd HH:mm}] {selectedStatusName}: {noteText}";
                }

                await CrmDataService.UpdateOrderAsync(_order);

                MessageBox.Show($"Order #{_order.OrderId} status successfully updated to '{selectedStatusName}'.", "Status Updated", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to update order status: {ex.Message}", "Workflow Rule", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
