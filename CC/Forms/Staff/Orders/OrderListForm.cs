using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using CC.Controls;
using CC.Domain.Entities;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.Staff.Orders
{
    /// <summary>
    /// Orders List Module Form matching target calibration and reference design (media_1789381816230.png):
    /// - Heading: text-2xl (22pt Bold), subtitle text-sm (9.5pt Regular)
    /// - Action button: + New Order (px-5 py-2.5 text-sm font-semibold rounded-xl, icon 16px)
    /// - Horizontal Search & Filter row: search input (py-2.5 pl-10 rounded-xl) and filter pills (All, Pending, Confirmed, etc.) on same bar
    /// - Table headers: text-xs uppercase, py-3.5 px-5
    /// - Table rows: py-4 px-5 text-sm, 68px row height
    /// - Columns: ORDER ID, CUSTOMER, DESIGN, EVENT DATE, PICKUP, TOTAL, STATUS (dot + text), ACTION (View →)
    /// </summary>
    public class OrderListForm : Form, ISearchable
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnNewOrder = null!;

        private Panel searchFilterPanel = null!;
        private Panel searchPill = null!;
        private TextBox txtSearchBox = null!;
        private FlowLayoutPanel filterPillContainer = null!;

        private Panel tableCardPanel = null!;
        private DataGridView gridOrders = null!;
        private string activeFilter = "All";
        private string activeSearchQuery = string.Empty;

        private Panel listContainerPanel = null!;
        private OrderDetailsControl detailsControl = null!;

        public OrderListForm()
        {
            InitializeComponent();

            BuildTopToolbar();
            BuildSearchAndFilterBar();
            BuildDataGridCard();

            listContainerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            listContainerPanel.Controls.Add(tableCardPanel);
            listContainerPanel.Controls.Add(searchFilterPanel);
            listContainerPanel.Controls.Add(topPanel);

            detailsControl = new OrderDetailsControl
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            detailsControl.BackClicked += async () =>
            {
                detailsControl.Visible = false;
                listContainerPanel.Visible = true;
                await RefreshGridAsync();
            };
            detailsControl.OrderUpdated += async () =>
            {
                await RefreshGridAsync();
            };

            Controls.Add(detailsControl);
            Controls.Add(listContainerPanel);

            Load += async (s, e) => await RefreshGridAsync();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1100, 700);
            Font = new Font(UITheme.FontSans, 9.5F);
            FormBorderStyle = FormBorderStyle.None;
            Name = "OrderListForm";
            Padding = new Padding(0);
            BackColor = UITheme.CreamBackground; // #FAF6F1
            ResumeLayout(false);
        }

        public void Search(string query)
        {
            activeSearchQuery = query ?? string.Empty;
            if (txtSearchBox != null && txtSearchBox.Text != activeSearchQuery)
            {
                txtSearchBox.Text = activeSearchQuery;
            }
            _ = RefreshGridAsync();
        }

        // =========================================================
        // 1. TOP TOOLBAR (Heading: text-2xl, Subtitle: text-sm, Action: px-5 py-2.5 rounded-xl)
        // =========================================================
        private void BuildTopToolbar()
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
                Text = "Orders",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            // Subtitle: text-sm (9.5pt Regular)
            lblSubtitle = new Label
            {
                Text = "6 total orders",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            // Action button: px-5 py-2.5 text-sm font-semibold rounded-xl, icon 16px
            btnNewOrder = new Button
            {
                Text = "New Order",
                Size = new Size(150, 40),
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 4, 0, 0)
            };
            UITheme.ApplyActionButton(btnNewOrder, "\uE710", 12);
            btnNewOrder.Click += BtnNewOrder_Click;

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnNewOrder, 1, 0);

            topPanel.Controls.Add(headerTable);
        }

        // =========================================================
        // 2. SEARCH & FILTER BAR (Horizontal row: Search input + Filter pills)
        // =========================================================
        private void BuildSearchAndFilterBar()
        {
            searchFilterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                Padding = new Padding(0, 0, 0, 16),
                BackColor = Color.Transparent
            };

            var rowFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            // Search input (py-2.5 pl-10 rounded-xl)
            searchPill = new Panel
            {
                Size = new Size(300, 40),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 12, 0)
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
                Location = new Point(40, 10), // pl-10 (40px)
                Width = 245,
                PlaceholderText = "Search by order ID or customer..."
            };
            txtSearchBox.TextChanged += (s, e) => Search(txtSearchBox.Text);
            searchPill.Controls.Add(txtSearchBox);
            rowFlow.Controls.Add(searchPill);

            // Filter pills: All, Pending, Confirmed, Processing, Ready, Completed, Cancelled
            filterPillContainer = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 0)
            };

            string[] filters = new[] { "All", "Pending", "Confirmed", "Processing", "Ready", "Completed", "Cancelled" };

            foreach (var filterName in filters)
            {
                var btn = new Button
                {
                    Text = filterName,
                    AutoSize = true,
                    Height = 36,
                    Margin = new Padding(0, 0, 6, 0),
                    Padding = new Padding(10, 0, 10, 0),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                    Tag = filterName
                };

                btn.FlatAppearance.BorderSize = 0;
                btn.Paint += (s, e) =>
                {
                    var b = (Button)s!;
                    bool isActive = string.Equals(b.Tag as string, activeFilter, StringComparison.OrdinalIgnoreCase);
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                    if (!isActive)
                    {
                        using var pen = new Pen(UITheme.BorderColor, 1.2f);
                        e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, b.Width - 1, b.Height - 1), 16);
                    }
                };

                btn.Click += (s, e) =>
                {
                    activeFilter = filterName;
                    UpdateFilterPillStyles();
                    _ = RefreshGridAsync();
                };

                filterPillContainer.Controls.Add(btn);
            }

            UpdateFilterPillStyles();
            rowFlow.Controls.Add(filterPillContainer);
            searchFilterPanel.Controls.Add(rowFlow);
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
                        btn.BackColor = Color.White;
                        btn.ForeColor = UITheme.TextDark;
                    }
                    btn.ApplyRoundedRegion(18);
                    btn.Invalidate();
                }
            }
        }

        // =========================================================
        // 3. TABLE CARD & GRID (py-4 px-5 text-sm)
        // =========================================================
        private void BuildDataGridCard()
        {
            tableCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };
            tableCardPanel.ApplyRoundedRegion(14);

            gridOrders = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyTableStyle(gridOrders);

            // Columns matching target reference design (media_1789381816230.png):
            // ORDER ID, CUSTOMER, DESIGN, EVENT DATE, PICKUP, TOTAL, STATUS, ACTION
            gridOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colOrderId", HeaderText = "ORDER ID", Width = 120, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER", Width = 150, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesign", HeaderText = "DESIGN", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 160, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEventDate", HeaderText = "EVENT DATE", Width = 105, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colPickup", HeaderText = "PICKUP", Width = 105, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTotal", HeaderText = "TOTAL", Width = 95, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS", Width = 125, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridOrders.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAction", HeaderText = "", Width = 75, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

            gridOrders.CellPainting += GridOrders_CellPainting;
            gridOrders.CellClick += GridOrders_CellClick;
            gridOrders.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridOrders.Columns["colAction"] is { } col && e.ColumnIndex == col.Index)
                    gridOrders.Cursor = Cursors.Hand;
            };
            gridOrders.CellMouseLeave += (s, e) => gridOrders.Cursor = Cursors.Default;

            tableCardPanel.Controls.Add(gridOrders);
        }

        private void GridOrders_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            var order = gridOrders.Rows[e.RowIndex].DataBoundItem as SalesOrder;
            if (order == null) return;

            e.PaintBackground(e.CellBounds, true);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int px5 = 16; // calibrated horizontal cell padding
            int cellY = e.CellBounds.Top + (e.CellBounds.Height - 20) / 2;

            int colIdxOrderId = gridOrders.Columns["colOrderId"]?.Index ?? -1;
            int colIdxCustomer = gridOrders.Columns["colCustomer"]?.Index ?? -1;
            int colIdxDesign = gridOrders.Columns["colDesign"]?.Index ?? -1;
            int colIdxEventDate = gridOrders.Columns["colEventDate"]?.Index ?? -1;
            int colIdxPickup = gridOrders.Columns["colPickup"]?.Index ?? -1;
            int colIdxTotal = gridOrders.Columns["colTotal"]?.Index ?? -1;
            int colIdxStatus = gridOrders.Columns["colStatus"]?.Index ?? -1;
            int colIdxAction = gridOrders.Columns["colAction"]?.Index ?? -1;

            // 1. ORDER ID (Maroon #8C465A e.g. ORD-2026-0045)
            if (e.ColumnIndex == colIdxOrderId)
            {
                string orderCode = $"ORD-2026-{order.OrderId:D4}";
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                using var brush = new SolidBrush(UITheme.PrimaryMauve);
                g.DrawString(orderCode, font, brush, e.CellBounds.Left + px5, cellY);
                e.Handled = true;
            }
            // 2. CUSTOMER (Bold dark text)
            else if (e.ColumnIndex == colIdxCustomer)
            {
                string custName = order.Customer != null ? $"{order.Customer.FirstName} {order.Customer.LastName}".Trim() : $"Customer #{order.CustomerId}";
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                using var brush = new SolidBrush(UITheme.TextDark);
                g.DrawString(custName, font, brush, e.CellBounds.Left + px5, cellY);
                e.Handled = true;
            }
            // 3. DESIGN (Regular dark text, truncated with ellipsis)
            else if (e.ColumnIndex == colIdxDesign)
            {
                string design = order.OrderDetails.FirstOrDefault()?.ItemDescription ?? order.DesignTheme ?? "Custom Cake Order";
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                var rect = new Rectangle(e.CellBounds.Left + px5, cellY - 2, Math.Max(0, e.CellBounds.Width - px5 - 10), 24);
                TextRenderer.DrawText(g, design, font, rect, UITheme.TextDark,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                e.Handled = true;
            }
            // 4. EVENT DATE (Muted text yyyy-MM-dd)
            else if (e.ColumnIndex == colIdxEventDate)
            {
                string dt = order.OrderDate.ToString("yyyy-MM-dd");
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                using var brush = new SolidBrush(UITheme.TextMuted);
                g.DrawString(dt, font, brush, e.CellBounds.Left + px5, cellY);
                e.Handled = true;
            }
            // 5. PICKUP (Muted text yyyy-MM-dd)
            else if (e.ColumnIndex == colIdxPickup)
            {
                string pickup = (order.DeliveryDate ?? order.OrderDate.AddDays(7)).ToString("yyyy-MM-dd");
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                using var brush = new SolidBrush(UITheme.TextMuted);
                g.DrawString(pickup, font, brush, e.CellBounds.Left + px5, cellY);
                e.Handled = true;
            }
            // 6. TOTAL (Bold dark text e.g. ₱6,800)
            else if (e.ColumnIndex == colIdxTotal)
            {
                decimal total = order.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);
                if (total == 0) total = order.TotalAmount > 0 ? order.TotalAmount : 3500m;
                string amt = $"₱{total:N0}";
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                using var brush = new SolidBrush(UITheme.TextDark);
                g.DrawString(amt, font, brush, e.CellBounds.Left + px5, cellY);
                e.Handled = true;
            }
            // 7. STATUS (Colored pill badge with dot ●)
            else if (e.ColumnIndex == colIdxStatus)
            {
                var (statusName, pillBg, pillFg) = GetOrderStatusBadge(order.StatusId);
                var pillRect = new Rectangle(e.CellBounds.Left + px5, e.CellBounds.Top + (e.CellBounds.Height - 28) / 2, 112, 28);

                using (var bgBrush = new SolidBrush(pillBg))
                using (var fgBrush = new SolidBrush(pillFg))
                {
                    g.FillRoundedRectangle(bgBrush, pillRect, 14);

                    // Draw colored circle dot
                    int dotSize = 6;
                    int dotX = pillRect.Left + 12;
                    int dotY = pillRect.Top + (pillRect.Height - dotSize) / 2;
                    g.FillEllipse(fgBrush, dotX, dotY, dotSize, dotSize);

                    // Draw status name
                    using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                    var textRect = new Rectangle(dotX + dotSize + 6, pillRect.Top, pillRect.Width - (dotX - pillRect.Left + dotSize + 8), pillRect.Height);
                    TextRenderer.DrawText(g, statusName, font, textRect, pillFg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
                e.Handled = true;
            }
            // 8. ACTION ("View →" in Maroon #8C465A)
            else if (colIdxAction >= 0 && e.ColumnIndex == colIdxAction)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                using var brush = new SolidBrush(UITheme.PrimaryMauve);

                string viewText = "View →";
                var sz = g.MeasureString(viewText, font);
                g.DrawString(viewText, font, brush,
                    e.CellBounds.Right - sz.Width - 14,
                    e.CellBounds.Top + (e.CellBounds.Height - sz.Height) / 2);

                e.Handled = true;
            }
        }

        private static (string Name, Color Bg, Color Fg) GetOrderStatusBadge(int statusId)
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

        private async void GridOrders_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridOrders.Columns["colAction"] is { } col && e.ColumnIndex == col.Index)
            {
                var order = gridOrders.Rows[e.RowIndex].DataBoundItem as SalesOrder;
                if (order != null)
                {
                    await ShowOrderDetailsAsync(order.OrderId);
                }
            }
        }

        public async Task ShowOrderDetailsAsync(int orderId)
        {
            listContainerPanel.Visible = false;
            detailsControl.Visible = true;
            await detailsControl.LoadOrderAsync(orderId);
        }

        private async void BtnNewOrder_Click(object? sender, EventArgs e)
        {
            using var form = new OrderForm();
            if (form.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
            {
                await RefreshGridAsync();
            }
        }

        public async Task RefreshGridAsync()
        {
            try
            {
                var list = await CrmDataService.GetOrdersAsync(activeFilter, activeSearchQuery);
                lblSubtitle.Text = $"{list.Count} total orders";
                gridOrders.RowTemplate.Height = 68;
                gridOrders.DataSource = null;
                gridOrders.DataSource = list;
                foreach (DataGridViewRow row in gridOrders.Rows)
                {
                    row.Height = 68;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load orders from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
