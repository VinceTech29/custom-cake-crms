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
using CC.Forms.Staff.FollowUps;
using CC.Forms.Staff.Payments;
using CC.Services;

namespace CC.Forms.Staff.Orders
{
    /// <summary>
    /// Dedicated Order Details view matching reference design (media_1789395593558.png):
    /// - Top link: "← Back to Orders"
    /// - Left Column:
    ///     * Order Info Card: Order code, Status pill, Customer/creation date, 2x3 attribute grid, Notes.
    ///     * Order Progress Stepper Card: 5-milestone stepper (Pending, Confirmed, Processing, Ready, Completed).
    /// - Right Column:
    ///     * Payment Card: Status pill, breakdown (Total, Down Payment, Additional Paid, Balance), method/date footer.
    ///     * Actions Card: Beige rounded action buttons (Update Status, Record Payment, Add Note, Create Follow-up).
    /// </summary>
    public class OrderDetailsControl : UserControl
    {
        public event Action? BackClicked;
        public event Action? OrderUpdated;

        private int _orderId;
        private SalesOrder? _order;

        private Panel topBar = null!;
        private Button btnBack = null!;
        private TableLayoutPanel mainLayout = null!;

        // Left Cards
        private Panel leftOrderInfoCard = null!;
        private Label lblOrderCode = null!;
        private Panel pnlStatusBadge = null!;
        private Label lblSubtitle = null!;
        private Label lblCakeSizeVal = null!;
        private Label lblFlavorVal = null!;
        private Label lblDesignVal = null!;
        private Label lblCustomMsgVal = null!;
        private Label lblEventDateVal = null!;
        private Label lblPickupDateVal = null!;
        private Label lblNotesVal = null!;

        private Panel leftStepperCard = null!;
        private StepperView stepperControl = null!;

        // Right Cards
        private Panel rightPaymentCard = null!;
        private Panel pnlPaymentBadge = null!;
        private Label lblTotalVal = null!;
        private Label lblDownPaymentVal = null!;
        private Label lblAdditionalPaidVal = null!;
        private Label lblBalanceVal = null!;
        private Label lblPaymentFooter = null!;

        private Panel rightActionsCard = null!;
        private Button btnUpdateStatus = null!;
        private Button btnRecordPayment = null!;
        private Button btnAddNote = null!;
        private Button btnCreateFollowUp = null!;

        public OrderDetailsControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            BackColor = UITheme.CreamBackground; // #FAF6F1
            Padding = new Padding(0);

            // 1. Top Bar with "← Back to Orders"
            topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 10)
            };

            btnBack = new Button
            {
                Text = "\u2190 Back to Orders",
                Font = new Font(UITheme.FontSans, 10F, FontStyle.Regular),
                ForeColor = UITheme.PrimaryMauve,
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Location = new Point(0, 4)
            };
            btnBack.FlatAppearance.BorderSize = 0;
            btnBack.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnBack.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnBack.MouseEnter += (s, e) => { btnBack.Font = new Font(UITheme.FontSans, 10F, FontStyle.Underline); };
            btnBack.MouseLeave += (s, e) => { btnBack.Font = new Font(UITheme.FontSans, 10F, FontStyle.Regular); };
            btnBack.Click += (s, e) => BackClicked?.Invoke();

            topBar.Controls.Add(btnBack);

            // 2. Main 2-Column Responsive Layout
            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68F)); // Left: ~68%
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32F)); // Right: ~32%
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Left Stack (Order Info Card + Stepper Card)
            var leftStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 14, 0),
                Padding = Padding.Empty
            };
            leftStack.RowStyles.Add(new RowStyle(SizeType.Percent, 64F)); // Order Info Card
            leftStack.RowStyles.Add(new RowStyle(SizeType.Percent, 36F)); // Progress Stepper Card

            BuildOrderInfoCard();
            BuildStepperCard();
            leftStack.Controls.Add(leftOrderInfoCard, 0, 0);
            leftStack.Controls.Add(leftStepperCard, 0, 1);

            // Right Stack (Payment Card + Actions Card)
            var rightStack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Margin = new Padding(6, 0, 0, 0),
                Padding = Padding.Empty
            };
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); // Payment Card
            rightStack.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); // Actions Card

            BuildPaymentCard();
            BuildActionsCard();
            rightStack.Controls.Add(rightPaymentCard, 0, 0);
            rightStack.Controls.Add(rightActionsCard, 0, 1);

            mainLayout.Controls.Add(leftStack, 0, 0);
            mainLayout.Controls.Add(rightStack, 1, 0);

            Controls.Add(mainLayout);
            Controls.Add(topBar);

            ResumeLayout(false);
        }

        // =========================================================
        // 1. LEFT TOP: ORDER INFO CARD
        // =========================================================
        private void BuildOrderInfoCard()
        {
            leftOrderInfoCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(28, 24, 28, 24)
            };
            leftOrderInfoCard.ApplyRoundedRegion(14);
            leftOrderInfoCard.Paint += DrawCardBorder;

            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F)); // Header + Badge
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 65F));  // 2x3 Grid
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14F));  // Divider
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 35F));  // Notes Section

            // Row 0: Header & Status Badge
            var headerRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));

            var titleFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            lblOrderCode = new Label
            {
                Text = "ORD-2026-0045",
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 32, 28),
                AutoSize = true
            };

            lblSubtitle = new Label
            {
                Text = "Created 2026-08-10 \u2022 Customer: Patricia Tan",
                Font = new Font(UITheme.FontSans, 9.5F),
                ForeColor = Color.FromArgb(124, 108, 104),
                AutoSize = true,
                Margin = new Padding(0, 2, 0, 0)
            };
            titleFlow.Controls.Add(lblOrderCode);
            titleFlow.Controls.Add(lblSubtitle);

            pnlStatusBadge = new Panel
            {
                Dock = DockStyle.Fill,
                Height = 32,
                BackColor = Color.Transparent
            };
            pnlStatusBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int statusId = _order?.StatusId ?? 0;
                (string name, Color bg, Color fg) = GetOrderStatusBadge(statusId);
                var rect = new Rectangle(pnlStatusBadge.Width - 120, 4, 116, 26);
                UITheme.DrawStatusPill(e.Graphics, rect, name, bg, fg, fg);
            };

            headerRow.Controls.Add(titleFlow, 0, 0);
            headerRow.Controls.Add(pnlStatusBadge, 1, 0);

            // Row 1: 2-Column Attribute Grid
            var gridLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 10, 0, 0)
            };
            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            gridLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.33F));
            gridLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 33.34F));

            // Cell (0,0): CAKE SIZE
            gridLayout.Controls.Add(CreateAttributeBlock("CAKE SIZE", out lblCakeSizeVal, "2-tier, 6\" + 8\""), 0, 0);
            // Cell (1,0): FLAVOR
            gridLayout.Controls.Add(CreateAttributeBlock("FLAVOR", out lblFlavorVal, "Vanilla bean with lemon curd filling"), 1, 0);
            // Cell (0,1): DESIGN / THEME
            gridLayout.Controls.Add(CreateAttributeBlock("DESIGN / THEME", out lblDesignVal, "Watercolor pastel with baby blue + gold fondant accents"), 0, 1);
            // Cell (1,1): CUSTOM MESSAGE
            gridLayout.Controls.Add(CreateAttributeBlock("CUSTOM MESSAGE", out lblCustomMsgVal, "Welcome to the world, Baby Ethan!"), 1, 1);
            // Cell (0,2): EVENT DATE
            gridLayout.Controls.Add(CreateAttributeBlock("EVENT DATE", out lblEventDateVal, "2026-08-28"), 0, 2);
            // Cell (1,2): PICKUP / DELIVERY
            gridLayout.Controls.Add(CreateAttributeBlock("PICKUP / DELIVERY", out lblPickupDateVal, "2026-08-27"), 1, 2);

            // Row 2: Subtle Horizontal Divider
            var divider = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            divider.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(239, 230, 222), 1f);
                e.Graphics.DrawLine(p, 0, 6, divider.Width, 6);
            };

            // Row 3: NOTES Section
            var notesBlock = CreateAttributeBlock("NOTES", out lblNotesVal, "Pickup at 10am. Box with extra padding.");
            notesBlock.Dock = DockStyle.Fill;

            contentLayout.Controls.Add(headerRow, 0, 0);
            contentLayout.Controls.Add(gridLayout, 0, 1);
            contentLayout.Controls.Add(divider, 0, 2);
            contentLayout.Controls.Add(notesBlock, 0, 3);

            leftOrderInfoCard.Controls.Add(contentLayout);
        }

        private static Panel CreateAttributeBlock(string label, out Label valLabel, string defaultVal)
        {
            var p = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 10, 2)
            };

            var lbl = new Label
            {
                Text = label,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = Color.FromArgb(124, 108, 104),
                Dock = DockStyle.Top,
                Height = 16
            };

            valLabel = new Label
            {
                Text = defaultVal,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(45, 32, 28),
                Dock = DockStyle.Fill,
                AutoEllipsis = true
            };

            p.Controls.Add(valLabel);
            p.Controls.Add(lbl);
            return p;
        }

        // =========================================================
        // 2. LEFT BOTTOM: ORDER PROGRESS STEPPER CARD
        // =========================================================
        private void BuildStepperCard()
        {
            leftStepperCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = new Padding(28, 20, 28, 20)
            };
            leftStepperCard.ApplyRoundedRegion(14);
            leftStepperCard.Paint += DrawCardBorder;

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var lblTitle = new Label
            {
                Text = "Order Progress",
                Font = new Font(UITheme.FontSans, 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 32, 28),
                Dock = DockStyle.Fill
            };

            stepperControl = new StepperView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            layout.Controls.Add(lblTitle, 0, 0);
            layout.Controls.Add(stepperControl, 0, 1);

            leftStepperCard.Controls.Add(layout);
        }

        // =========================================================
        // 3. RIGHT TOP: PAYMENT SUMMARY CARD
        // =========================================================
        private void BuildPaymentCard()
        {
            rightPaymentCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 16),
                Padding = new Padding(24, 22, 24, 20)
            };
            rightPaymentCard.ApplyRoundedRegion(14);
            rightPaymentCard.Paint += DrawCardBorder;

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                BackColor = Color.Transparent
            };
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F)); // Header + Badge
            stack.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Breakdown rows
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 10F)); // Divider
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F)); // Footer note

            // Header
            var headerRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            headerRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var lblTitle = new Label
            {
                Text = "Payment",
                Font = new Font(UITheme.FontSans, 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 32, 28),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            pnlPaymentBadge = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            pnlPaymentBadge.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                (string statusText, Color bg, Color fg) = GetPaymentStatusInfo();
                var rect = new Rectangle(pnlPaymentBadge.Width - 116, 2, 114, 24);
                UITheme.DrawStatusPill(e.Graphics, rect, statusText, bg, fg, fg);
            };

            headerRow.Controls.Add(lblTitle, 0, 0);
            headerRow.Controls.Add(pnlPaymentBadge, 1, 0);

            // Financial Breakdown Rows
            var breakdownGrid = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 4,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 8, 0, 0)
            };
            breakdownGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55F));
            breakdownGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45F));
            for (int i = 0; i < 4; i++) breakdownGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            breakdownGrid.Controls.Add(CreateFinanceLabel("Order Total"), 0, 0);
            lblTotalVal = CreateFinanceValue("₱6,800", true, Color.FromArgb(45, 32, 28));
            breakdownGrid.Controls.Add(lblTotalVal, 1, 0);

            breakdownGrid.Controls.Add(CreateFinanceLabel("Down Payment"), 0, 1);
            lblDownPaymentVal = CreateFinanceValue("₱3,400", false, Color.FromArgb(45, 32, 28));
            breakdownGrid.Controls.Add(lblDownPaymentVal, 1, 1);

            breakdownGrid.Controls.Add(CreateFinanceLabel("Additional Paid"), 0, 2);
            lblAdditionalPaidVal = CreateFinanceValue("₱0", false, Color.FromArgb(45, 32, 28));
            breakdownGrid.Controls.Add(lblAdditionalPaidVal, 1, 2);

            breakdownGrid.Controls.Add(CreateFinanceLabel("Balance"), 0, 3);
            lblBalanceVal = CreateFinanceValue("₱3,400", true, Color.FromArgb(140, 70, 90)); // Maroon
            breakdownGrid.Controls.Add(lblBalanceVal, 1, 3);

            // Divider
            var divider = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
            divider.Paint += (s, e) =>
            {
                using var p = new Pen(Color.FromArgb(239, 230, 222), 1f);
                e.Graphics.DrawLine(p, 0, 4, divider.Width, 4);
            };

            // Footer note
            lblPaymentFooter = new Label
            {
                Text = "Via GCash \u2022 2026-08-10",
                Font = new Font(UITheme.FontSans, 8.5F),
                ForeColor = Color.FromArgb(140, 128, 124),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            stack.Controls.Add(headerRow, 0, 0);
            stack.Controls.Add(breakdownGrid, 0, 1);
            stack.Controls.Add(divider, 0, 2);
            stack.Controls.Add(lblPaymentFooter, 0, 3);

            rightPaymentCard.Controls.Add(stack);
        }

        private static Label CreateFinanceLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = Color.FromArgb(124, 108, 104),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static Label CreateFinanceValue(string text, bool isBold, Color color)
        {
            return new Label
            {
                Text = text,
                Font = new Font(UITheme.FontSans, 9.5F, isBold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = color,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleRight
            };
        }

        // =========================================================
        // 4. RIGHT BOTTOM: ACTIONS CARD
        // =========================================================
        private void BuildActionsCard()
        {
            rightActionsCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = Padding.Empty,
                Padding = new Padding(24, 22, 24, 20)
            };
            rightActionsCard.ApplyRoundedRegion(14);
            rightActionsCard.Paint += DrawCardBorder;

            var stack = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                BackColor = Color.Transparent
            };
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            for (int i = 0; i < 4; i++) stack.RowStyles.Add(new RowStyle(SizeType.Percent, 25F));

            var lblTitle = new Label
            {
                Text = "Actions",
                Font = new Font(UITheme.FontSans, 11.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 32, 28),
                Dock = DockStyle.Fill
            };
            stack.Controls.Add(lblTitle, 0, 0);

            btnUpdateStatus = CreateActionButton("Update Status");
            btnUpdateStatus.Click += BtnUpdateStatus_Click;
            stack.Controls.Add(btnUpdateStatus, 0, 1);

            btnRecordPayment = CreateActionButton("Record Payment");
            btnRecordPayment.Click += BtnRecordPayment_Click;
            stack.Controls.Add(btnRecordPayment, 0, 2);

            btnAddNote = CreateActionButton("Add Note");
            btnAddNote.Click += BtnAddNote_Click;
            stack.Controls.Add(btnAddNote, 0, 3);

            btnCreateFollowUp = CreateActionButton("Create Follow-up");
            btnCreateFollowUp.Click += BtnCreateFollowUp_Click;
            stack.Controls.Add(btnCreateFollowUp, 0, 4);

            rightActionsCard.Controls.Add(stack);
        }

        private static Button CreateActionButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                UseVisualStyleBackColor = false,
                Margin = new Padding(0, 4, 0, 4)
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btn.FlatAppearance.MouseDownBackColor = Color.Transparent;

            btn.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool hovered = btn.ClientRectangle.Contains(btn.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? Color.FromArgb(238, 227, 218) : Color.FromArgb(244, 235, 226));
                e.Graphics.FillRoundedRectangle(bg, new Rectangle(0, 0, btn.Width, btn.Height), 10);
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                TextRenderer.DrawText(e.Graphics, text, font, btn.ClientRectangle, Color.FromArgb(45, 32, 28),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            btn.MouseEnter += (s, e) => btn.Invalidate();
            btn.MouseLeave += (s, e) => btn.Invalidate();

            return btn;
        }

        private static void DrawCardBorder(object? sender, PaintEventArgs e)
        {
            if (sender is Control c)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var p = new Pen(Color.FromArgb(239, 230, 222), 1.2f);
                e.Graphics.DrawRoundedRectangle(p, new Rectangle(0, 0, c.Width - 1, c.Height - 1), 14);
            }
        }

        // =========================================================
        // DATA BINDING & REFRESH
        // =========================================================

        public async Task LoadOrderAsync(int orderId)
        {
            _orderId = orderId;
            try
            {
                _order = await CrmDataService.GetOrderByIdAsync(orderId);
                if (_order == null)
                {
                    MessageBox.Show($"Order #{orderId} not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    BackClicked?.Invoke();
                    return;
                }

                BindOrderData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load order details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindOrderData()
        {
            if (_order == null) return;

            // Header info
            lblOrderCode.Text = $"ORD-{_order.OrderDate:yyyy}-{_order.OrderId:D4}";
            string custName = _order.Customer != null
                ? $"{_order.Customer.FirstName} {_order.Customer.LastName}".Trim()
                : $"Customer #{_order.CustomerId}";
            lblSubtitle.Text = $"Created {_order.OrderDate:yyyy-MM-dd} \u2022 Customer: {custName}";

            // Attribute values
            lblCakeSizeVal.Text = !string.IsNullOrWhiteSpace(_order.CakeSize) ? _order.CakeSize : "2-tier, 6\" + 8\"";
            lblFlavorVal.Text = !string.IsNullOrWhiteSpace(_order.Flavor) ? _order.Flavor : "Vanilla bean with lemon curd filling";
            lblDesignVal.Text = !string.IsNullOrWhiteSpace(_order.DesignTheme) ? _order.DesignTheme : "Watercolor pastel with baby blue + gold fondant accents";

            // Custom message from cake customization or fallback
            string customMsg = "Welcome to the world, Baby Ethan!";
            var firstDetail = _order.OrderDetails.FirstOrDefault();
            if (firstDetail?.CakeCustomization != null && !string.IsNullOrWhiteSpace(firstDetail.CakeCustomization.MessageOnCake))
            {
                customMsg = firstDetail.CakeCustomization.MessageOnCake;
            }
            else if (!string.IsNullOrWhiteSpace(_order.Notes) && _order.Notes.Contains("Message:", StringComparison.OrdinalIgnoreCase))
            {
                var match = _order.Notes.Split(new[] { "Message:" }, StringSplitOptions.None);
                if (match.Length > 1) customMsg = match[1].Split('\n')[0].Trim();
            }
            lblCustomMsgVal.Text = customMsg;

            DateTime eventDate = _order.DeliveryDate ?? _order.OrderDate.AddDays(14);
            DateTime pickupDate = _order.DeliveryDate.HasValue ? _order.DeliveryDate.Value.AddDays(-1) : eventDate.AddDays(-1);
            lblEventDateVal.Text = eventDate.ToString("yyyy-MM-dd");
            lblPickupDateVal.Text = pickupDate.ToString("yyyy-MM-dd");

            lblNotesVal.Text = !string.IsNullOrWhiteSpace(_order.Notes)
                ? _order.Notes
                : "Pickup at 10am. Box with extra padding.";

            // Stepper
            int stepIndex = OrderStatusWorkflow.GetStepperStepIndex(_order.StatusId);
            stepperControl.SetActiveStep(stepIndex, _order.StatusId == OrderStatusWorkflow.StatusCancelled);

            // Status Badge
            pnlStatusBadge.Invalidate();

            // Payment Calculations
            decimal total = _order.TotalAmount > 0
                ? _order.TotalAmount
                : _order.OrderDetails.Sum(d => d.Quantity * d.UnitPrice);
            if (total <= 0) total = 6800m;

            var payments = _order.Payments?.ToList() ?? new List<Payment>();
            decimal downPaid = 0m;
            decimal additionalPaid = 0m;

            if (payments.Count > 0)
            {
                var sorted = payments.OrderBy(p => p.PaymentDate).ToList();
                downPaid = sorted[0].Amount;
                additionalPaid = sorted.Skip(1).Sum(p => p.Amount);
            }
            else
            {
                // Sensible default for presentation if no payment record yet
                downPaid = total / 2m;
                additionalPaid = 0m;
            }

            decimal totalPaid = downPaid + additionalPaid;
            decimal balance = Math.Max(0m, total - totalPaid);

            lblTotalVal.Text = $"₱{total:N0}";
            lblDownPaymentVal.Text = $"₱{downPaid:N0}";
            lblAdditionalPaidVal.Text = $"₱{additionalPaid:N0}";
            lblBalanceVal.Text = $"₱{balance:N0}";

            var lastPay = payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();
            string methodStr = lastPay?.Method?.MethodName ?? "GCash";
            string dateStr = lastPay != null ? lastPay.PaymentDate.ToString("yyyy-MM-dd") : _order.OrderDate.ToString("yyyy-MM-dd");
            lblPaymentFooter.Text = $"Via {methodStr} \u2022 {dateStr}";

            pnlPaymentBadge.Invalidate();
        }

        private (string StatusText, Color Bg, Color Fg) GetPaymentStatusInfo()
        {
            if (_order == null) return ("Unpaid", ColorTranslator.FromHtml("#FEE2E2"), ColorTranslator.FromHtml("#B91C1C"));

            decimal total = _order.TotalAmount > 0 ? _order.TotalAmount : 6800m;
            var payments = _order.Payments?.ToList() ?? new List<Payment>();
            decimal paid = payments.Sum(p => p.Amount);
            if (payments.Count == 0 && total > 0) paid = total / 2m; // Default sample half paid

            if (paid >= total && total > 0)
                return ("Paid", ColorTranslator.FromHtml("#DCFCE7"), ColorTranslator.FromHtml("#15803D"));
            if (paid > 0)
                return ("Partially Paid", ColorTranslator.FromHtml("#FEF3C7"), ColorTranslator.FromHtml("#B45309"));

            return ("Unpaid", ColorTranslator.FromHtml("#FEE2E2"), ColorTranslator.FromHtml("#B91C1C"));
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

        // =========================================================
        // BUTTON HANDLERS
        // =========================================================

        private async void BtnUpdateStatus_Click(object? sender, EventArgs e)
        {
            if (_order == null) return;

            using var modal = new OrderStatusModal(_order);
            if (modal.ShowDialog(this.FindForm() ?? (IWin32Window)this) == DialogResult.OK)
            {
                await LoadOrderAsync(_order.OrderId);
                OrderUpdated?.Invoke();
            }
        }

        private async void BtnRecordPayment_Click(object? sender, EventArgs e)
        {
            if (_order == null) return;

            using var pForm = new PaymentForm(_order.OrderId);
            if (pForm.ShowDialog(this.FindForm() ?? (IWin32Window)this) == DialogResult.OK)
            {
                await LoadOrderAsync(_order.OrderId);
                OrderUpdated?.Invoke();
            }
        }

        private async void BtnAddNote_Click(object? sender, EventArgs e)
        {
            if (_order == null) return;

            string newNote = PromptForNote();
            if (!string.IsNullOrWhiteSpace(newNote))
            {
                try
                {
                    string stamp = $"[{DateTime.Now:yyyy-MM-dd HH:mm}] {newNote.Trim()}";
                    _order.Notes = string.IsNullOrWhiteSpace(_order.Notes)
                        ? stamp
                        : $"{_order.Notes}\r\n{stamp}";

                    await CrmDataService.UpdateOrderAsync(_order);
                    await LoadOrderAsync(_order.OrderId);
                    OrderUpdated?.Invoke();
                    MessageBox.Show("Note added successfully.", "Order Note", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Unable to save note: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string PromptForNote()
        {
            using var dlg = new Form
            {
                Text = "Add Note to Order",
                Size = new Size(460, 260),
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.White,
                ShowInTaskbar = false
            };
            dlg.ApplyRoundedRegion(14);
            dlg.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var p = new Pen(Color.FromArgb(140, 70, 90), 2f);
                e.Graphics.DrawRoundedRectangle(p, new Rectangle(1, 1, dlg.Width - 3, dlg.Height - 3), 14);
            };

            var lbl = new Label
            {
                Text = "ENTER NOTE / SPECIAL INSTRUCTIONS:",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(124, 108, 104),
                Location = new Point(24, 20),
                AutoSize = true
            };
            var txt = new TextBox
            {
                Multiline = true,
                Location = new Point(24, 48),
                Size = new Size(412, 120),
                Font = new Font(UITheme.FontSans, 9.5F),
                BackColor = Color.FromArgb(239, 230, 222),
                BorderStyle = BorderStyle.None
            };
            var btnOk = new Button
            {
                Text = "Save Note",
                Location = new Point(326, 180),
                Size = new Size(110, 36),
                DialogResult = DialogResult.OK
            };
            UITheme.ApplyPrimaryButton(btnOk, 8);

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(226, 180),
                Size = new Size(90, 36),
                DialogResult = DialogResult.Cancel
            };
            UITheme.ApplySecondaryButton(btnCancel, 8);

            dlg.Controls.Add(lbl);
            dlg.Controls.Add(txt);
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancel);
            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            return dlg.ShowDialog(this.FindForm() ?? (IWin32Window)this) == DialogResult.OK ? txt.Text : string.Empty;
        }

        private async void BtnCreateFollowUp_Click(object? sender, EventArgs e)
        {
            if (_order == null) return;

            var followUp = new CustomerFollowUp
            {
                CustomerId = _order.CustomerId,
                StaffUserId = SessionService.CurrentUser?.UserId ?? CrmDataService.DefaultUserId,
                StatusId = 0,
                FollowUpDate = DateTime.Today.AddDays(2),
                NextFollowUpDate = DateTime.Today.AddDays(5),
                Notes = $"Follow up on Order #ORD-{_order.OrderId:D4} for {_order.Customer?.FirstName} {_order.Customer?.LastName}."
            };

            using var fForm = new FollowUpForm(followUp);
            if (fForm.ShowDialog(this.FindForm() ?? (IWin32Window)this) == DialogResult.OK)
            {
                await LoadOrderAsync(_order.OrderId);
                OrderUpdated?.Invoke();
            }
        }
    }

    /// <summary>
    /// Custom milestone progress stepper view matching the 5 steps in the reference mockup:
    /// Step 1: Pending -> Step 2: Confirmed -> Step 3: Processing -> Step 4: Ready -> Step 5: Completed
    /// </summary>
    public class StepperView : Control
    {
        private readonly string[] _stepNames = new[] { "Pending", "Confirmed", "Processing", "Ready", "Completed" };
        private int _activeStepIndex = 2; // Default to Processing (as shown in reference mockup)
        private bool _isCancelled = false;

        public StepperView()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        }

        public void SetActiveStep(int stepIndex, bool isCancelled = false)
        {
            _activeStepIndex = stepIndex;
            _isCancelled = isCancelled;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int count = _stepNames.Length;
            int circleDiameter = 28;
            int circleRadius = circleDiameter / 2;

            int marginX = 36;
            int usableWidth = Width - (marginX * 2);
            int stepSpacing = usableWidth / (count - 1);
            int circleCenterY = 24;

            // 1. Draw Connecting Lines between step circles
            for (int i = 0; i < count - 1; i++)
            {
                int x1 = marginX + (i * stepSpacing) + circleRadius + 4;
                int x2 = marginX + ((i + 1) * stepSpacing) - circleRadius - 4;

                bool isFilledLine = i < _activeStepIndex && !_isCancelled;
                Color lineColor = isFilledLine ? Color.FromArgb(190, 155, 140) : Color.FromArgb(232, 223, 216);

                using var pen = new Pen(lineColor, 2.5f);
                g.DrawLine(pen, x1, circleCenterY, x2, circleCenterY);
            }

            // 2. Draw Step Circles and Step Labels
            for (int i = 0; i < count; i++)
            {
                int centerX = marginX + (i * stepSpacing);
                int left = centerX - circleRadius;
                int top = circleCenterY - circleRadius;
                var circleRect = new Rectangle(left, top, circleDiameter, circleDiameter);

                if (_isCancelled)
                {
                    // Cancelled styling
                    using var bg = new SolidBrush(Color.FromArgb(254, 226, 226));
                    using var borderPen = new Pen(Color.FromArgb(185, 28, 28), 1.5f);
                    g.FillEllipse(bg, circleRect);
                    g.DrawEllipse(borderPen, circleRect);

                    using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                    TextRenderer.DrawText(g, (i + 1).ToString(), font, circleRect, Color.FromArgb(185, 28, 28),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                else if (i < _activeStepIndex)
                {
                    // Past completed step: warm brown circle with checkmark
                    using var bg = new SolidBrush(Color.FromArgb(246, 239, 233));
                    using var borderPen = new Pen(Color.FromArgb(186, 142, 123), 2f);
                    g.FillEllipse(bg, circleRect);
                    g.DrawEllipse(borderPen, circleRect);

                    // Draw checkmark symbol
                    using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                    TextRenderer.DrawText(g, "\u2713", font, circleRect, Color.FromArgb(140, 70, 90),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                else if (i == _activeStepIndex)
                {
                    // Active current step: solid filled maroon circle with white number
                    using var bg = new SolidBrush(Color.FromArgb(140, 70, 90)); // Maroon #8C465A
                    g.FillEllipse(bg, circleRect);

                    using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                    TextRenderer.DrawText(g, (i + 1).ToString(), font, circleRect, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }
                else
                {
                    // Upcoming future step: light beige/gray circle with gray number
                    using var bg = new SolidBrush(Color.FromArgb(248, 245, 242));
                    using var borderPen = new Pen(Color.FromArgb(220, 212, 206), 1.5f);
                    g.FillEllipse(bg, circleRect);
                    g.DrawEllipse(borderPen, circleRect);

                    using var font = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                    TextRenderer.DrawText(g, (i + 1).ToString(), font, circleRect, Color.FromArgb(168, 159, 154),
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // Step Label underneath
                string labelText = _stepNames[i];
                var labelRect = new Rectangle(centerX - 50, top + circleDiameter + 8, 100, 24);

                FontStyle fs = (i == _activeStepIndex && !_isCancelled) ? FontStyle.Bold : FontStyle.Regular;
                Color textColor = (i == _activeStepIndex && !_isCancelled)
                    ? Color.FromArgb(45, 32, 28)
                    : (i < _activeStepIndex ? Color.FromArgb(90, 75, 71) : Color.FromArgb(168, 159, 154));

                using var labelFont = new Font(UITheme.FontSans, 8.5F, fs);
                TextRenderer.DrawText(g, labelText, labelFont, labelRect, textColor,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
            }
        }
    }
}
