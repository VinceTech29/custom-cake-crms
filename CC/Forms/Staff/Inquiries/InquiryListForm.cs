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

namespace CC.Forms.Staff.Inquiries
{
    /// <summary>
    /// Inquiries list view matching media_1789382649692.png:
    /// - Top toolbar: "Inquiries" Georgia 22pt Bold, subtitle counter, "+ New Inquiry" action button
    /// - 4 KPI status summary cards (New, In Progress, Approved, Converted)
    /// - Search input & filter pills (All, New, In Progress, Quoted, Approved, Converted, Closed)
    /// - DataGridView with custom cell painting for status badges and "Update Status" action buttons
    /// - Complete CRUD workflow integration
    /// </summary>
    public class InquiryListForm : Form, INavigationAware
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnNewInquiry = null!;

        private TableLayoutPanel kpiTable = null!;
        private Label lblCountNew = null!;
        private Label lblCountInProgress = null!;
        private Label lblCountApproved = null!;
        private Label lblCountConverted = null!;

        private Panel searchFilterPanel = null!;
        private Panel searchPill = null!;
        private TextBox txtSearchBox = null!;
        private FlowLayoutPanel filterPillContainer = null!;

        private Panel tableCardPanel = null!;
        private DataGridView gridInquiries = null!;
        private PaginationControl pagination = null!;
        private int currentPage = 1;
        private const int PageSize = 10;

        private string activeFilter = "All";
        private string activeSearchQuery = string.Empty;

        public InquiryListForm()
        {
            InitializeComponent();
            BuildTopToolbar();
            BuildKpiCards();
            BuildSearchAndFilterBar();
            BuildDataGridCard();

            Controls.Add(tableCardPanel);
            Controls.Add(searchFilterPanel);
            Controls.Add(kpiTable);
            Controls.Add(topPanel);
        }

        public async Task InitializeDataAsync() => await RefreshGridAsync();

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1100, 750);
            Font = new Font(UITheme.FontSans, 9.5F);
            FormBorderStyle = FormBorderStyle.None;
            Name = "InquiryListForm";
            Padding = Padding.Empty;
            BackColor = UITheme.CreamBackground;
            ResumeLayout(false);
        }

        // =========================================================
        // 1. TOP TOOLBAR (Heading text-2xl, Subtitle text-sm, + New Inquiry)
        // =========================================================
        private void BuildTopToolbar()
        {
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 74,
                Padding = new Padding(0, 0, 0, 10),
                BackColor = Color.Transparent
            };

            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));
            headerTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var titleStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            lblTitle = new Label
            {
                Text = "Inquiries",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            lblSubtitle = new Label
            {
                Text = "6 total inquiries",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            btnNewInquiry = new Button
            {
                Text = "New Inquiry",
                Size = new Size(160, 40),
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 4, 0, 0)
            };
            UITheme.ApplyActionButton(btnNewInquiry, "\uE710", 12);
            btnNewInquiry.Click += BtnNewInquiry_Click;

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnNewInquiry, 1, 0);
            topPanel.Controls.Add(headerTable);
        }

        // =========================================================
        // 2. 4 KPI STATUS SUMMARY CARDS (New, In Progress, Approved, Converted)
        // =========================================================
        private void BuildKpiCards()
        {
            kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 114,
                ColumnCount = 4,
                RowCount = 1,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 0, 0, 24),
                Margin = Padding.Empty
            };

            kpiTable.RowStyles.Clear();
            kpiTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            for (int i = 0; i < 4; i++)
            {
                kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }

            // Card 1: NEW (Blue)
            var cardNew = CreateKpiCard("NEW", "\uE8C8", Color.FromArgb(37, 99, 235), out lblCountNew);
            // Card 2: IN PROGRESS (Orange)
            var cardProg = CreateKpiCard("IN PROGRESS", "\uE916", Color.FromArgb(217, 119, 6), out lblCountInProgress);
            // Card 3: APPROVED (Green)
            var cardAppr = CreateKpiCard("APPROVED", "\uE73E", Color.FromArgb(22, 163, 74), out lblCountApproved);
            // Card 4: CONVERTED (Dark)
            var cardConv = CreateKpiCard("CONVERTED", "\uE72A", Color.FromArgb(31, 27, 24), out lblCountConverted);

            kpiTable.Controls.Add(cardNew, 0, 0);
            kpiTable.Controls.Add(cardProg, 1, 0);
            kpiTable.Controls.Add(cardAppr, 2, 0);
            kpiTable.Controls.Add(cardConv, 3, 0);
        }

        private Panel CreateKpiCard(string title, string iconGlyph, Color numColor, out Label countLabel)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 14, 0),
                Padding = new Padding(18, 14, 18, 14)
            };
            card.ApplyRoundedRegion(14);

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 14);
            };

            var topRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 22,
                BackColor = Color.Transparent
            };

            var lblCardTitle = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Dock = DockStyle.Left
            };

            var lblIcon = new Label
            {
                Text = iconGlyph,
                Font = new Font("Segoe MDL2 Assets", 11F),
                ForeColor = numColor,
                AutoSize = true,
                Dock = DockStyle.Right
            };

            topRow.Controls.Add(lblCardTitle);
            topRow.Controls.Add(lblIcon);

            var lblNum = new Label
            {
                Text = "0",
                Font = new Font(UITheme.FontSerif, 20F, FontStyle.Bold),
                ForeColor = numColor,
                AutoSize = true,
                Location = new Point(18, 38)
            };

            countLabel = lblNum;

            card.Controls.Add(lblNum);
            card.Controls.Add(topRow);
            return card;
        }

        // =========================================================
        // 3. SEARCH & FILTER BAR
        // =========================================================
        private void BuildSearchAndFilterBar()
        {
            searchFilterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 64,
                Padding = new Padding(0, 0, 0, 24),
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

            // Search pill (py-2.5 pl-10 rounded-xl)
            searchPill = new Panel
            {
                Size = new Size(320, 40),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 12, 0)
            };

            searchPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, searchPill.Width - 1, searchPill.Height - 1), 12);

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
                Width = 265,
                PlaceholderText = "Search by customer, cake type, or ID..."
            };
            txtSearchBox.TextChanged += (s, e) =>
            {
                activeSearchQuery = txtSearchBox.Text;
                currentPage = 1;
                _ = RefreshGridAsync();
            };
            searchPill.Controls.Add(txtSearchBox);
            rowFlow.Controls.Add(searchPill);

            // Filter pills: All, New, In Progress, Quoted, Approved, Converted, Closed
            filterPillContainer = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 0)
            };

            string[] filters = new[] { "All", "New", "In Progress", "Quoted", "Approved", "Converted", "Closed" };

            foreach (var filterName in filters)
            {
                var btn = new Button
                {
                    Text = filterName,
                    AutoSize = true,
                    Height = 36,
                    Margin = new Padding(0, 0, 6, 0),
                    Padding = new Padding(12, 0, 12, 0),
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
                    currentPage = 1;
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
        // 4. DATA TABLE CARD & GRID
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

            gridInquiries = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyTableStyle(gridInquiries);

            // Columns matching media_1789382649692.png:
            gridInquiries.Columns.Add(new DataGridViewTextBoxColumn { Name = "colId", HeaderText = "ID", Width = 120, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridInquiries.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER", Width = 155, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridInquiries.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCakeType", HeaderText = "CAKE TYPE", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 180, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridInquiries.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEventDate", HeaderText = "EVENT DATE", Width = 115, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridInquiries.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAssignedTo", HeaderText = "ASSIGNED TO", Width = 130, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridInquiries.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS", Width = 125, SortMode = DataGridViewColumnSortMode.NotSortable });
            gridInquiries.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAction", HeaderText = "", Width = 145, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

            gridInquiries.CellPainting += GridInquiries_CellPainting;
            gridInquiries.CellClick += GridInquiries_CellClick;
            gridInquiries.CellDoubleClick += async (s, e) =>
            {
                if (e.RowIndex < 0) return;
                var inq = gridInquiries.Rows[e.RowIndex].DataBoundItem as CustomerInquiry;
                if (inq != null)
                {
                    using var form = new InquiryForm(inq);
                    if (form.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                    {
                        await RefreshGridAsync();
                    }
                }
            };

            gridInquiries.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridInquiries.Columns["colAction"] is { } colAction && e.ColumnIndex == colAction.Index)
                    gridInquiries.Cursor = Cursors.Hand;
            };
            gridInquiries.CellMouseLeave += (s, e) => gridInquiries.Cursor = Cursors.Default;

            pagination = new PaginationControl
            {
                Dock = DockStyle.Bottom
            };
            pagination.PageChanged += async (newPage) =>
            {
                currentPage = newPage;
                await RefreshGridAsync();
            };

            tableCardPanel.Controls.Add(gridInquiries);
            tableCardPanel.Controls.Add(pagination);
        }

        private void GridInquiries_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            var inq = gridInquiries.Rows[e.RowIndex].DataBoundItem as CustomerInquiry;
            if (inq == null) return;

            e.PaintBackground(e.CellBounds, true);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int px5 = 18;
            int cellY = e.CellBounds.Top + (e.CellBounds.Height - 20) / 2;

            int colIdxId = gridInquiries.Columns["colId"]?.Index ?? -1;
            int colIdxCustomer = gridInquiries.Columns["colCustomer"]?.Index ?? -1;
            int colIdxCakeType = gridInquiries.Columns["colCakeType"]?.Index ?? -1;
            int colIdxEventDate = gridInquiries.Columns["colEventDate"]?.Index ?? -1;
            int colIdxAssignedTo = gridInquiries.Columns["colAssignedTo"]?.Index ?? -1;
            int colIdxStatus = gridInquiries.Columns["colStatus"]?.Index ?? -1;
            int colIdxAction = gridInquiries.Columns["colAction"]?.Index ?? -1;

            // 1. ID (Maroon / subtle text)
            if (colIdxId >= 0 && e.ColumnIndex == colIdxId)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                using var brush = new SolidBrush(Color.FromArgb(140, 90, 80));
                g.DrawString(inq.InquiryCode, font, brush, e.CellBounds.Left + px5, cellY);
                e.Handled = true;
            }
            // 2. CUSTOMER (Bold dark text)
            else if (colIdxCustomer >= 0 && e.ColumnIndex == colIdxCustomer)
            {
                string custName = inq.Customer != null ? $"{inq.Customer.FirstName} {inq.Customer.LastName}".Trim() : $"Customer #{inq.CustomerId}";
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                using var brush = new SolidBrush(UITheme.TextDark);
                g.DrawString(custName, font, brush, e.CellBounds.Left + px5, cellY);
                e.Handled = true;
            }
            // 3. CAKE TYPE (Regular dark text with ellipsis)
            else if (colIdxCakeType >= 0 && e.ColumnIndex == colIdxCakeType)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                var rect = new Rectangle(e.CellBounds.Left + px5, cellY - 2, Math.Max(0, e.CellBounds.Width - px5 - 10), 24);
                TextRenderer.DrawText(g, inq.CakeType, font, rect, UITheme.TextDark,
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
                e.Handled = true;
            }
            // 4. EVENT DATE (Muted date)
            else if (colIdxEventDate >= 0 && e.ColumnIndex == colIdxEventDate)
            {
                string dt = inq.EventDate.ToString("yyyy-MM-dd");
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                using var brush = new SolidBrush(UITheme.TextMuted);
                g.DrawString(dt, font, brush, e.CellBounds.Left + px5, cellY);
                e.Handled = true;
            }
            // 5. ASSIGNED TO (Dark text)
            else if (colIdxAssignedTo >= 0 && e.ColumnIndex == colIdxAssignedTo)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);
                using var brush = new SolidBrush(inq.AssignedTo == "Unassigned" ? UITheme.TextMuted : UITheme.TextDark);
                g.DrawString(inq.AssignedTo, font, brush, e.CellBounds.Left + px5, cellY);
                e.Handled = true;
            }
            // 6. STATUS (Colored pill badge with dot ●)
            else if (colIdxStatus >= 0 && e.ColumnIndex == colIdxStatus)
            {
                var (pillBg, pillFg) = GetStatusBadgeColors(inq.Status);
                var pillRect = new Rectangle(e.CellBounds.Left + px5, e.CellBounds.Top + (e.CellBounds.Height - 28) / 2, 102, 28);

                using (var bgBrush = new SolidBrush(pillBg))
                using (var fgBrush = new SolidBrush(pillFg))
                {
                    g.FillRoundedRectangle(bgBrush, pillRect, 14);

                    int dotSize = 6;
                    int dotX = pillRect.Left + 10;
                    int dotY = pillRect.Top + (pillRect.Height - dotSize) / 2;
                    g.FillEllipse(fgBrush, dotX, dotY, dotSize, dotSize);

                    using var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold);
                    var textRect = new Rectangle(dotX + dotSize + 6, pillRect.Top, pillRect.Width - (dotX - pillRect.Left + dotSize + 8), pillRect.Height);
                    TextRenderer.DrawText(g, inq.Status, font, textRect, pillFg,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
                e.Handled = true;
            }
            // 7. ACTION ("Convert to Order" + "Update Status" pill buttons)
            else if (colIdxAction >= 0 && e.ColumnIndex == colIdxAction)
            {
                if (inq.Status != "Converted" && inq.Status != "Closed")
                {
                    bool isApproved = string.Equals(inq.Status, "Approved", StringComparison.OrdinalIgnoreCase);

                    if (isApproved)
                    {
                        // Approved inquiries are locked from manual status changes; ONLY "Convert to Order" is available
                        var btnConvertRect = new Rectangle(e.CellBounds.Right - 132, e.CellBounds.Top + (e.CellBounds.Height - 28) / 2, 122, 28);
                        using (var bgBrush = new SolidBrush(Color.FromArgb(140, 70, 90))) // Maroon #8C465A
                        {
                            g.FillRoundedRectangle(bgBrush, btnConvertRect, 14);
                        }

                        using (var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold))
                        using (var textBrush = new SolidBrush(Color.White))
                        {
                            string label = "Convert to Order";
                            var sz = g.MeasureString(label, font);
                            g.DrawString(label, font, textBrush, btnConvertRect.Left + (btnConvertRect.Width - sz.Width) / 2, btnConvertRect.Top + (btnConvertRect.Height - sz.Height) / 2);
                        }
                    }
                    else
                    {
                        var btnRect = new Rectangle(e.CellBounds.Right - 118, e.CellBounds.Top + (e.CellBounds.Height - 28) / 2, 108, 28);

                        using (var bgBrush = new SolidBrush(Color.FromArgb(239, 231, 223))) // #EFE7DF
                        using (var borderPen = new Pen(Color.FromArgb(220, 210, 200), 1f))
                        {
                            g.FillRoundedRectangle(bgBrush, btnRect, 14);
                            g.DrawRoundedRectangle(borderPen, btnRect, 14);
                        }

                        using var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold);
                        using var textBrush = new SolidBrush(UITheme.TextDark);
                        string label = "Update Status";
                        var sz = g.MeasureString(label, font);
                        g.DrawString(label, font, textBrush, btnRect.Left + (btnRect.Width - sz.Width) / 2, btnRect.Top + (btnRect.Height - sz.Height) / 2);
                    }
                }
                e.Handled = true;
            }
        }

        private static (Color Bg, Color Fg) GetStatusBadgeColors(string status)
        {
            return status.ToLower() switch
            {
                "quoted" => (Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206)),      // #F3E8FF / #7E22CE
                "approved" => (Color.FromArgb(220, 252, 231), Color.FromArgb(21, 128, 61)),     // #DCFCE7 / #15803D
                "in progress" => (Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105)),  // #F1F5F9 / #475569
                "new" => (Color.FromArgb(219, 234, 254), Color.FromArgb(29, 78, 216)),          // #DBEAFE / #1D4ED8
                "converted" => (Color.FromArgb(243, 236, 229), Color.FromArgb(68, 64, 60)),     // #F3ECE5 / #44403C
                "closed" => (Color.FromArgb(254, 226, 226), Color.FromArgb(185, 28, 28)),        // #FEE2E2 / #B91C1C
                _ => (Color.FromArgb(243, 236, 229), Color.FromArgb(68, 64, 60))
            };
        }

        private async void GridInquiries_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (gridInquiries.Columns["colAction"] is { } colAction && e.ColumnIndex == colAction.Index)
            {
                var inq = gridInquiries.Rows[e.RowIndex].DataBoundItem as CustomerInquiry;
                if (inq == null || inq.Status == "Converted" || inq.Status == "Closed") return;

                bool isApproved = string.Equals(inq.Status, "Approved", StringComparison.OrdinalIgnoreCase);

                if (isApproved)
                {
                    using var orderForm = new CC.Forms.Staff.Orders.OrderForm(inq);
                    if (orderForm.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                    {
                        MessageBox.Show($"Inquiry {inq.InquiryCode} successfully converted to Order #{orderForm.NewOrder?.OrderId}!", "Inquiry Converted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        await RefreshGridAsync();
                    }
                    return;
                }

                using var modal = new InquiryStatusModal(inq);
                if (modal.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                {
                    if (modal.ConvertToOrderRequested)
                    {
                        using var orderForm = new CC.Forms.Staff.Orders.OrderForm(inq);
                        if (orderForm.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                        {
                            MessageBox.Show($"Inquiry {inq.InquiryCode} successfully converted to Order #{orderForm.NewOrder?.OrderId}!", "Inquiry Converted", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                    await RefreshGridAsync();
                }
            }
        }

        private async void BtnNewInquiry_Click(object? sender, EventArgs e)
        {
            using var form = new InquiryForm();
            if (form.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
            {
                await RefreshGridAsync();
            }
        }

        public async Task RefreshGridAsync()
        {
            try
            {
                // 1. KPI counts from database
                var allInquiries = await CrmDataService.GetInquiriesAsync(null, null);
                lblCountNew.Text = allInquiries.Count(i => i.Status.Equals("New", StringComparison.OrdinalIgnoreCase)).ToString();
                lblCountInProgress.Text = allInquiries.Count(i => i.Status.Equals("In Progress", StringComparison.OrdinalIgnoreCase)).ToString();
                lblCountApproved.Text = allInquiries.Count(i => i.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase)).ToString();
                lblCountConverted.Text = allInquiries.Count(i => i.Status.Equals("Converted", StringComparison.OrdinalIgnoreCase)).ToString();

                // 2. Filtered list from database with pagination
                var paged = await CrmDataService.GetInquiriesPagedAsync(activeFilter, activeSearchQuery, currentPage, PageSize);
                if (paged.TotalPages > 0 && currentPage > paged.TotalPages)
                {
                    currentPage = paged.TotalPages;
                    paged = await CrmDataService.GetInquiriesPagedAsync(activeFilter, activeSearchQuery, currentPage, PageSize);
                }

                lblSubtitle.Text = $"{paged.TotalCount} total inquiries";

                gridInquiries.RowTemplate.Height = 68;
                gridInquiries.DataSource = null;
                gridInquiries.DataSource = paged.Items;

                foreach (DataGridViewRow row in gridInquiries.Rows)
                {
                    row.Height = 68;
                }

                pagination.SetPagination(paged.Page, paged.PageSize, paged.TotalCount, "inquiries");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load inquiries from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
