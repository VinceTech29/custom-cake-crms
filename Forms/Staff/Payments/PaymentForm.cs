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

namespace CC.Forms.Staff.Payments
{
    public class PaymentForm : Form
    {
        private static readonly Color ColorModalBg = Color.White;
        private static readonly Color ColorFieldBg = Color.FromArgb(239, 230, 222);       // #EFE6DE
        private static readonly Color ColorTextDark = Color.FromArgb(45, 32, 28);         // #2D201C
        private static readonly Color ColorLabel = Color.FromArgb(124, 108, 104);         // #7C6C68
        private static readonly Color ColorAsterisk = Color.FromArgb(156, 107, 115);      // #9C6B73
        private static readonly Color ColorBorder = Color.FromArgb(140, 70, 90);          // High-contrast Noticeable Maroon #8C465A
        private static readonly Color ColorDivider = Color.FromArgb(239, 230, 222);

        private readonly int? initialOrderId;
        private List<SalesOrder> loadedOrders = new();
        private ComboBox cbOrder = null!;
        private TextBox txtAmount = null!;
        private ComboBox cbMethod = null!;
        private ComboBox cbStatus = null!;
        private TextBox txtRef = null!;
        private DateTimePicker dtDate = null!;
        private Label lblOrderSummary = null!;

        public PaymentForm(int? initialOrderId = null)
        {
            this.initialOrderId = initialOrderId;
            InitializeModal();
            BuildContent();
        }

        private void InitializeModal()
        {
            Text = "Record Payment";
            Size = new Size(580, 680);
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

            Load += async (s, e) =>
            {
                this.CenterOnParentOrScreen();
                this.ApplyRoundedRegion(16);
                await LoadOrdersAsync();
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
                Text = "Record Payment",
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
                Text = "Save Payment",
                Size = new Size(140, 44),
                Location = new Point(footerPanel.Width - 32 - 140, 17),
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

            int contentWidth = 516; // 580 - 64
            int leftMargin = 32;
            int y = 14;

            // Order dropdown field
            var orderField = CreateFieldGroup("SELECT ORDER", true, contentWidth, out cbOrder);
            orderField.Location = new Point(leftMargin, y);
            cbOrder.SelectedIndexChanged += CbOrder_SelectedIndexChanged;
            scrollPanel.Controls.Add(orderField);
            y += orderField.Height + 10;

            // Order Summary pill card
            var summaryCard = new Panel
            {
                Location = new Point(leftMargin, y),
                Size = new Size(contentWidth, 40),
                BackColor = Color.FromArgb(248, 244, 240)
            };
            summaryCard.ApplyRoundedRegion(8);

            lblOrderSummary = new Label
            {
                Text = "Select an order to view balance",
                Font = new Font(UITheme.FontSans, 9f, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            summaryCard.Controls.Add(lblOrderSummary);
            scrollPanel.Controls.Add(summaryCard);
            y += summaryCard.Height + 14;

            // Amount field
            var amountField = CreateInputField("PAYMENT AMOUNT (₱)", true, contentWidth, out txtAmount, "0.00");
            amountField.Location = new Point(leftMargin, y);
            scrollPanel.Controls.Add(amountField);
            y += amountField.Height + 14;

            // 2-col row: Method and Status
            int colGap = 16;
            int colWidth = (contentWidth - colGap) / 2;

            var methodField = CreateFieldGroup("PAYMENT METHOD", true, colWidth, out cbMethod);
            methodField.Location = new Point(leftMargin, y);
            cbMethod.Items.AddRange(new object[] { "Cash", "GCash", "Bank Transfer", "Credit Card", "Other" });
            cbMethod.SelectedIndex = 1; // GCash default
            scrollPanel.Controls.Add(methodField);

            var statusField = CreateFieldGroup("STATUS", true, colWidth, out cbStatus);
            statusField.Location = new Point(leftMargin + colWidth + colGap, y);
            cbStatus.Items.AddRange(new object[] { "Pending", "Completed", "Failed" });
            cbStatus.SelectedIndex = 1; // Completed default
            scrollPanel.Controls.Add(statusField);
            y += methodField.Height + 14;

            // 2-col row: Payment Date and Reference
            var dtField = CreateDateField("PAYMENT DATE", true, colWidth, out dtDate);
            dtField.Location = new Point(leftMargin, y);
            scrollPanel.Controls.Add(dtField);

            var refField = CreateInputField("REFERENCE NO.", false, colWidth, out txtRef, "e.g. GCash Ref / Bank Ref");
            refField.Location = new Point(leftMargin + colWidth + colGap, y);
            scrollPanel.Controls.Add(refField);
            y += dtField.Height + 20;

            root.Controls.Add(scrollPanel);
            root.Controls.Add(footerPanel);
            root.Controls.Add(headerPanel);
            Controls.Add(root);
        }

        private Panel CreateInputField(string label, bool required, int width, out TextBox textBox, string placeholder = "")
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

            textBox = new TextBox
            {
                Location = new Point(14, 13),
                Width = width - 28,
                BorderStyle = BorderStyle.None,
                BackColor = ColorFieldBg,
                ForeColor = ColorTextDark,
                Font = new Font(UITheme.FontSans, 10.5f),
                PlaceholderText = placeholder
            };

            boxPanel.Controls.Add(textBox);
            container.Controls.Add(lblFlow);
            container.Controls.Add(boxPanel);
            return container;
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

        private async Task LoadOrdersAsync()
        {
            try
            {
                loadedOrders = await CrmDataService.GetOrdersAsync();
                cbOrder.Items.Clear();

                int selectIndex = 0;
                for (int i = 0; i < loadedOrders.Count; i++)
                {
                    var o = loadedOrders[i];
                    string custName = o.Customer != null ? $"{o.Customer.FirstName} {o.Customer.LastName}".Trim() : $"Cust #{o.CustomerId}";
                    cbOrder.Items.Add(new ComboBoxItem(o.OrderId, $"#ORD-{o.OrderId} - {custName}"));

                    if (initialOrderId.HasValue && o.OrderId == initialOrderId.Value)
                    {
                        selectIndex = i;
                    }
                }

                if (cbOrder.Items.Count > 0)
                {
                    cbOrder.SelectedIndex = selectIndex;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load orders from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CbOrder_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cbOrder.SelectedItem is ComboBoxItem item)
            {
                var order = loadedOrders.FirstOrDefault(o => o.OrderId == item.Id);
                if (order != null)
                {
                    decimal total = order.TotalAmount > 0
                        ? order.TotalAmount
                        : order.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);
                    if (total == 0) total = 3500m;

                    decimal paid = order.Payments.Sum(p => p.Amount);
                    decimal balance = Math.Max(0m, total - paid);

                    lblOrderSummary.Text = $"Total: ₱{total:N0}  |  Paid: ₱{paid:N0}  |  Balance: ₱{balance:N0}";
                    lblOrderSummary.ForeColor = balance > 0 ? Color.FromArgb(170, 80, 50) : UITheme.StatusGreenFg;

                    txtAmount.Text = balance > 0 ? balance.ToString("0") : "0";
                    txtRef.Text = $"REF-{DateTime.Now:yyyyMMddHHmm}";
                }
            }
        }

        private async void BtnSave_Click(object? sender, EventArgs e)
        {
            if (!decimal.TryParse(txtAmount.Text.Trim(), out var amt) || amt <= 0)
            {
                MessageBox.Show("Please enter a valid positive payment amount.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(cbOrder.SelectedItem is ComboBoxItem selectedOrder))
            {
                MessageBox.Show("Please select an order.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                var payment = new Payment
                {
                    OrderId = selectedOrder.Id,
                    MethodId = cbMethod.SelectedIndex + 1,
                    StatusId = cbStatus.SelectedIndex,
                    ProcessedByUserId = SessionService.CurrentUser?.UserId > 0 ? SessionService.CurrentUser.UserId : CrmDataService.DefaultUserId,
                    Amount = amt,
                    PaymentDate = dtDate.Value,
                    TransactionReference = string.IsNullOrWhiteSpace(txtRef.Text) ? $"REF-{DateTime.Now:yyyyMMddHHmmss}" : txtRef.Text.Trim()
                };

                await CrmDataService.CreatePaymentAsync(payment);
                MessageBox.Show($"Payment of ₱{amt:N2} recorded successfully for Order #ORD-{selectedOrder.Id}.", "Payment Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to record payment in database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
