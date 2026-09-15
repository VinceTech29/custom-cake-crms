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
    /// <summary>
    /// Payments List Module Form implementing the List Panel Pattern specified in target mockup (media_1789365009186.png).
    /// </summary>
    public class PaymentListForm : Form, ISearchable
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnNewPayment = null!;

        private Panel searchWrapperPanel = null!;
        private Panel searchPill = null!;
        private TextBox txtSearchBox = null!;
        private FlowLayoutPanel filterPillContainer = null!;

        private Panel tableCardPanel = null!;
        private DataGridView gridPayments = null!;
        private string activeFilter = "All";
        private string activeSearchQuery = string.Empty;

        public PaymentListForm()
        {
            InitializeComponent();

            BuildHeadingBlock();
            BuildSearchBar();
            BuildFilterBar();
            BuildDataGridCard();

            // Direct docking in reverse order: tableCard (Fill), filterPills (Top), searchWrapper (Top), topPanel (Top)
            Controls.Add(tableCardPanel);
            Controls.Add(filterPillContainer);
            Controls.Add(searchWrapperPanel);
            Controls.Add(topPanel);

            Load += async (s, e) => await RefreshGridAsync();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1100, 700);
            Font = new Font(UITheme.FontSans, 9.5F);
            FormBorderStyle = FormBorderStyle.None;
            Name = "PaymentListForm";
            Padding = new Padding(0);
            BackColor = UITheme.CreamBackground; // #FAF6F1
            ResumeLayout(false);
        }

        public async void Search(string query)
        {
            activeSearchQuery = query ?? string.Empty;
            if (txtSearchBox != null && txtSearchBox.Text != activeSearchQuery)
            {
                txtSearchBox.Text = activeSearchQuery;
            }
            await RefreshGridAsync();
        }

        private void BuildHeadingBlock()
        {
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                Padding = new Padding(0, 0, 0, 14),
                BackColor = Color.Transparent
            };

            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));
            headerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var titleStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            // Module heading: text-2xl (22pt Bold)
            lblTitle = new Label
            {
                Text = "Payments",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            // Subtitle: text-sm (9.5pt Regular)
            lblSubtitle = new Label
            {
                Text = "Payment tracking for all orders",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            // Action button: px-5 py-2.5 text-sm font-semibold rounded-xl, icon 16px
            btnNewPayment = new Button
            {
                Text = "New Payment",
                Size = new Size(160, 40),
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 4, 0, 0)
            };
            UITheme.ApplyActionButton(btnNewPayment, "\uE710", 12);
            btnNewPayment.Click += BtnNewPayment_Click;

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnNewPayment, 1, 0);

            topPanel.Controls.Add(headerTable);
        }

        private void BuildSearchBar()
        {
            searchWrapperPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(0, 0, 0, 16),
                BackColor = Color.Transparent
            };

            searchPill = new Panel
            {
                Size = new Size(420, 40),
                Location = new Point(0, 0),
                BackColor = Color.White
            };

            searchPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var pen = new Pen(UITheme.BorderColor, 1.2f))
                {
                    e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, searchPill.Width - 1, searchPill.Height - 1), 12);
                }

                using var font = new Font("Segoe MDL2 Assets", 10.5f);
                var rect = new Rectangle(14, 0, 20, searchPill.Height);
                TextRenderer.DrawText(e.Graphics, "\uE721", font, rect, UITheme.TextMuted,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            };
            searchPill.ApplyRoundedRegion(12);

            txtSearchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                Font = new Font(UITheme.FontSans, 9.5F),
                ForeColor = UITheme.TextDark,
                Location = new Point(40, 10),
                Width = 360,
                PlaceholderText = "Search by order, customer, or payment method..."
            };
            txtSearchBox.TextChanged += (s, e) => Search(txtSearchBox.Text);

            searchPill.Controls.Add(txtSearchBox);
            searchWrapperPanel.Controls.Add(searchPill);
        }

        private void BuildFilterBar()
        {
            filterPillContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                WrapContents = true,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 4, 0, 16),
                BackColor = Color.Transparent
            };

            string[] filters = new[] { "All", "Unpaid", "Partially Paid", "Fully Paid" };

            foreach (var filterName in filters)
            {
                var btn = new Button
                {
                    Text = filterName,
                    AutoSize = true,
                    Height = 32,
                    Margin = new Padding(0, 0, 8, 4),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                    Tag = filterName
                };

                btn.FlatAppearance.BorderSize = 0;
                btn.Click += async (s, e) =>
                {
                    activeFilter = filterName;
                    UpdateFilterPillStyles();
                    await RefreshGridAsync();
                };

                filterPillContainer.Controls.Add(btn);
            }

            UpdateFilterPillStyles();
        }

        private void UpdateFilterPillStyles()
        {
            foreach (Control c in filterPillContainer.Controls)
            {
                if (c is Button btn && btn.Tag is string fName)
                {
                    bool isActive = string.Equals(fName, activeFilter, StringComparison.OrdinalIgnoreCase);

                    if (isActive)
                    {
                        btn.BackColor = UITheme.PrimaryMauve;
                        btn.ForeColor = Color.White;
                    }
                    else
                    {
                        btn.BackColor = UITheme.TableHeaderTan;
                        btn.ForeColor = UITheme.TextDark;
                    }
                    btn.ApplyRoundedRegion(14);
                }
            }
        }

        private void BuildDataGridCard()
        {
            tableCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            tableCardPanel.ApplyRoundedRegion(14);

            gridPayments = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyTableStyle(gridPayments);

            // 1. ORDER / CUSTOMER (First Column Pattern - Fill white space)
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrder", HeaderText = "ORDER / CUSTOMER", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 240, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 2. TOTAL
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "TOTAL", Width = 95, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 3. DOWN PAID
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDownPaid", HeaderText = "DOWN PAID", Width = 105, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 4. BALANCE
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colBalance", HeaderText = "BALANCE", Width = 95, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 5. METHOD
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colMethod", HeaderText = "METHOD", Width = 110, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 6. LAST PAYMENT
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLastPayment", HeaderText = "LAST PAYMENT", Width = 125, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 7. STATUS
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS", Width = 120, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 8. ACTION ("Collect" / "View →")
            gridPayments.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAction", HeaderText = "", Width = 100, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

            gridPayments.CellPainting += GridPayments_CellPainting;
            gridPayments.CellClick += GridPayments_CellClick;
            gridPayments.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridPayments.Columns["colAction"] is { } colAction && e.ColumnIndex == colAction.Index)
                    gridPayments.Cursor = Cursors.Hand;
            };
            gridPayments.CellMouseLeave += (s, e) => gridPayments.Cursor = Cursors.Default;

            tableCardPanel.Controls.Add(gridPayments);
        }

        private void GridPayments_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            var item = gridPayments.Rows[e.RowIndex].DataBoundItem as PaymentRowViewModel;
            if (item == null) return;

            e.PaintBackground(e.CellBounds, true);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cellY = e.CellBounds.Top + (e.CellBounds.Height - 20) / 2;

            int colIdxOrder = gridPayments.Columns["colOrder"]?.Index ?? -1;
            int colIdxTotal = gridPayments.Columns["colTotal"]?.Index ?? -1;
            int colIdxDownPaid = gridPayments.Columns["colDownPaid"]?.Index ?? -1;
            int colIdxBalance = gridPayments.Columns["colBalance"]?.Index ?? -1;
            int colIdxMethod = gridPayments.Columns["colMethod"]?.Index ?? -1;
            int colIdxLastPayment = gridPayments.Columns["colLastPayment"]?.Index ?? -1;
            int colIdxStatus = gridPayments.Columns["colStatus"]?.Index ?? -1;
            int colIdxAction = gridPayments.Columns["colAction"]?.Index ?? -1;

            // 1. FIRST COLUMN PATTERN (Maroon Avatar + Customer Name + Order Code)
            if (colIdxOrder >= 0 && e.ColumnIndex == colIdxOrder)
            {
                int avatarSize = 36;
                int avatarX = e.CellBounds.Left + 20; // px-5
                int avatarY = e.CellBounds.Top + (e.CellBounds.Height - avatarSize) / 2;

                using (var brush = new SolidBrush(UITheme.PrimaryMauve))
                {
                    g.FillEllipse(brush, avatarX, avatarY, avatarSize, avatarSize);
                }

                string initials = GetInitials(item.CustomerName);
                using (var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var size = g.MeasureString(initials, font);
                    g.DrawString(initials, font, brush,
                        avatarX + (avatarSize - size.Width) / 2,
                        avatarY + (avatarSize - size.Height) / 2);
                }

                int textX = avatarX + avatarSize + 14;
                int textWidth = Math.Max(0, e.CellBounds.Width - (textX - e.CellBounds.Left) - 8);

                using (var nameFont = new Font(UITheme.FontSans, 10F, FontStyle.Bold))
                {
                    var nameRect = new Rectangle(textX, e.CellBounds.Top + 14, textWidth, 22);
                    TextRenderer.DrawText(g, item.CustomerName, nameFont, nameRect, UITheme.TextDark,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }

                using (var subFont = new Font(UITheme.FontSans, 8.5F))
                {
                    var subRect = new Rectangle(textX, e.CellBounds.Top + 38, textWidth, 18);
                    TextRenderer.DrawText(g, item.OrderCode, subFont, subRect, UITheme.TextMuted,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }

                e.Handled = true;
            }
            // 2. TOTAL
            else if (colIdxTotal >= 0 && e.ColumnIndex == colIdxTotal)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                using var brush = new SolidBrush(UITheme.TextDark);
                g.DrawString($"₱{item.Total:N0}", font, brush, e.CellBounds.Left + 12, cellY);
                e.Handled = true;
            }
            // 3. DOWN PAID
            else if (colIdxDownPaid >= 0 && e.ColumnIndex == colIdxDownPaid)
            {
                using var font = new Font(UITheme.FontSans, 9F);
                using var brush = new SolidBrush(UITheme.TextDark);
                g.DrawString($"₱{item.DownPaid:N0}", font, brush, e.CellBounds.Left + 12, cellY);
                e.Handled = true;
            }
            // 4. BALANCE
            else if (colIdxBalance >= 0 && e.ColumnIndex == colIdxBalance)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                using var brush = new SolidBrush(item.Balance > 0 ? Color.FromArgb(170, 60, 50) : UITheme.TextDark);
                g.DrawString($"₱{item.Balance:N0}", font, brush, e.CellBounds.Left + 12, cellY);
                e.Handled = true;
            }
            // 5. METHOD
            else if (colIdxMethod >= 0 && e.ColumnIndex == colIdxMethod)
            {
                using var font = new Font(UITheme.FontSans, 9F);
                using var brush = new SolidBrush(UITheme.TextDark);
                g.DrawString(item.MethodName, font, brush, e.CellBounds.Left + 12, cellY);
                e.Handled = true;
            }
            // 6. LAST PAYMENT
            else if (colIdxLastPayment >= 0 && e.ColumnIndex == colIdxLastPayment)
            {
                using var font = new Font(UITheme.FontSans, 9F);
                using var brush = new SolidBrush(UITheme.TextMuted);
                g.DrawString(item.LastPaymentDate, font, brush, e.CellBounds.Left + 12, cellY);
                e.Handled = true;
            }
            // 7. STATUS
            else if (colIdxStatus >= 0 && e.ColumnIndex == colIdxStatus)
            {
                (string statusText, Color bg, Color fg) = GetPaymentStatusBadge(item.StatusText);
                var pillRect = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top + (e.CellBounds.Height - 26) / 2, 114, 26);
                UITheme.DrawStatusPill(g, pillRect, statusText, bg, fg, fg);
                e.Handled = true;
            }
            // 8. ACTION ("Collect" pill button or "View →")
            else if (colIdxAction >= 0 && e.ColumnIndex == colIdxAction)
            {
                if (item.Balance > 0)
                {
                    var btnRect = new Rectangle(e.CellBounds.Right - 82, e.CellBounds.Top + (e.CellBounds.Height - 28) / 2, 74, 28);
                    using (var bgBrush = new SolidBrush(UITheme.PrimaryMauve))
                    {
                        g.FillRoundedRectangle(bgBrush, btnRect, 14);
                    }
                    using (var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold))
                    using (var textBrush = new SolidBrush(Color.White))
                    {
                        var textStr = "Collect";
                        var sz = g.MeasureString(textStr, font);
                        g.DrawString(textStr, font, textBrush, btnRect.Left + (btnRect.Width - sz.Width) / 2, btnRect.Top + (btnRect.Height - sz.Height) / 2);
                    }
                }
                else
                {
                    using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                    using var brush = new SolidBrush(UITheme.PrimaryMauve);
                    string viewText = "View →";
                    var sz = g.MeasureString(viewText, font);
                    g.DrawString(viewText, font, brush, e.CellBounds.Right - sz.Width - 14, cellY);
                }
                e.Handled = true;
            }
        }

        private static (string Name, Color Bg, Color Fg) GetPaymentStatusBadge(string status)
        {
            return status.ToLower() switch
            {
                "fully paid" => ("Fully Paid", UITheme.StatusGreenBg, UITheme.StatusGreenFg),
                "partially paid" => ("Partially Paid", UITheme.StatusYellowBg, UITheme.StatusYellowFg),
                "unpaid" => ("Unpaid", UITheme.StatusRedBg, UITheme.StatusRedFg),
                _ => ("Unpaid", UITheme.StatusRedBg, UITheme.StatusRedFg)
            };
        }

        private async void GridPayments_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridPayments.Columns["colAction"] is { } colAction && e.ColumnIndex == colAction.Index)
            {
                var item = gridPayments.Rows[e.RowIndex].DataBoundItem as PaymentRowViewModel;
                if (item != null)
                {
                    using var f = new PaymentForm(item.OrderId);
                    if (f.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                    {
                        await RefreshGridAsync();
                    }
                }
            }
        }

        private async void BtnNewPayment_Click(object? sender, EventArgs e)
        {
            using var form = new PaymentForm();
            if (form.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
            {
                await RefreshGridAsync();
            }
        }

        private async Task RefreshGridAsync()
        {
            try
            {
                var orders = await CrmDataService.GetOrdersAsync();
                var rows = new List<PaymentRowViewModel>();

                foreach (var order in orders)
                {
                    decimal total = order.TotalAmount > 0
                        ? order.TotalAmount
                        : order.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);
                    if (total == 0) total = 3500m;

                    var payments = order.Payments.ToList();
                    decimal downPaid = payments.Sum(p => p.Amount);
                    decimal additional = 0m;
                    decimal balance = Math.Max(0m, total - (downPaid + additional));

                    string statusText = balance == 0 ? "Fully Paid" : (downPaid > 0 ? "Partially Paid" : "Unpaid");

                    var lastPayment = payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
                    string methodStr = lastPayment != null
                        ? (!string.IsNullOrWhiteSpace(lastPayment.Method?.MethodName)
                            ? lastPayment.Method.MethodName
                            : (lastPayment.MethodId switch
                            {
                                1 => "Cash",
                                2 => "GCash",
                                3 => "Bank Transfer",
                                4 => "Credit Card",
                                _ => "Other"
                            }))
                        : "-";

                    string lastPaymentDateStr = lastPayment != null ? lastPayment.PaymentDate.ToString("yyyy-MM-dd") : "-";

                    rows.Add(new PaymentRowViewModel
                    {
                        OrderId = order.OrderId,
                        OrderCode = $"#ORD-{order.OrderId}",
                        CustomerName = order.Customer != null ? $"{order.Customer.FirstName} {order.Customer.LastName}".Trim() : $"Customer #{order.CustomerId}",
                        Total = total,
                        DownPaid = downPaid,
                        Additional = additional,
                        Balance = balance,
                        MethodName = methodStr,
                        LastPaymentDate = lastPaymentDateStr,
                        StatusText = statusText
                    });
                }

                var query = rows.AsQueryable();

                if (!string.Equals(activeFilter, "All", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(r => string.Equals(r.StatusText, activeFilter, StringComparison.OrdinalIgnoreCase));
                }

                string search = activeSearchQuery.Trim().ToLower();
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(r =>
                        r.OrderCode.ToLower().Contains(search) ||
                        r.CustomerName.ToLower().Contains(search) ||
                        r.MethodName.ToLower().Contains(search)
                    );
                }

                gridPayments.DataSource = query.ToList();
                gridPayments.RowTemplate.Height = 68;
                foreach (DataGridViewRow row in gridPayments.Rows)
                {
                    row.Height = 68;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load payments from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "PM";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return (parts[0][0].ToString() + parts[^1][0]).ToUpper();
        }

        private class PaymentRowViewModel
        {
            public int OrderId { get; set; }
            public string OrderCode { get; set; } = string.Empty;
            public string CustomerName { get; set; } = string.Empty;
            public decimal Total { get; set; }
            public decimal DownPaid { get; set; }
            public decimal Additional { get; set; }
            public decimal Balance { get; set; }
            public string MethodName { get; set; } = string.Empty;
            public string LastPaymentDate { get; set; } = string.Empty;
            public string StatusText { get; set; } = string.Empty;
        }
    }
}
