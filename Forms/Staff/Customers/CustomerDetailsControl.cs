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

namespace CC.Forms.Staff.Customers
{
    /// <summary>
    /// Full customer details view matching reference design (media_1789386280613.png):
    /// - Back button: "← Back to Customers"
    /// - Left Profile Card: Avatar, Name, Edit icon button, Email, Phone, Address, Customer Since, Total Orders, Last Contact, Preferences, Notes
    /// - Right Top Card: "Order History" with count badge and order details table
    /// - Right Bottom Card: "Interaction History" with count badge and cards for feedback, follow-ups, and inquiries
    /// </summary>
    public class CustomerDetailsControl : UserControl
    {
        public event Action? BackClicked;
        public event Action? CustomerUpdated;

        private int _customerId;
        private Customer? _customer;

        private Panel topBar = null!;
        private Button btnBack = null!;
        private TableLayoutPanel mainLayout = null!;

        // Left Card Controls
        private Panel leftCard = null!;
        private Label lblAvatar = null!;
        private Label lblCustomerName = null!;
        private Button btnEditCustomer = null!;
        private Label lblEmailValue = null!;
        private Label lblPhoneValue = null!;
        private Label lblAddressValue = null!;
        private Label lblSinceValue = null!;
        private Label lblTotalOrdersValue = null!;
        private Label lblLastContactValue = null!;
        private Label lblPreferencesValue = null!;
        private Label lblNotesValue = null!;

        // Right Cards
        private Panel rightTopCard = null!;
        private Label lblOrderCountBadge = null!;
        private DataGridView gridOrders = null!;
        private Label lblNoOrders = null!;

        private Panel rightBottomCard = null!;
        private Label lblInteractionCountBadge = null!;
        private Panel pnlInteractionsScroll = null!;
        private Label lblNoInteractions = null!;

        public CustomerDetailsControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Dock = DockStyle.Fill;
            BackColor = UITheme.CreamBackground;
            Padding = new Padding(0);

            // 1. Top Bar with "← Back to Customers"
            topBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 10)
            };

            btnBack = new Button
            {
                Text = "\u2190 Back to Customers",
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
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 380F)); // Left Profile Card (380px)
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // Right Stack (Fill)
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            BuildLeftCard();
            BuildRightStack();

            Controls.Add(mainLayout);
            Controls.Add(topBar);

            ResumeLayout(false);
        }

        public async Task LoadCustomerAsync(int customerId)
        {
            _customerId = customerId;
            try
            {
                _customer = await CrmDataService.GetCustomerByIdAsync(customerId);
                if (_customer == null)
                {
                    MessageBox.Show($"Customer #{customerId} not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    BackClicked?.Invoke();
                    return;
                }

                PopulateCustomerData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load customer details: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BuildLeftCard()
        {
            leftCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 16, 0),
                Padding = new Padding(24, 24, 24, 24),
                AutoScroll = true
            };
            leftCard.ApplyRoundedRegion(14);
            leftCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, leftCard.Width - 1, leftCard.Height - 1), 14);
            };

            var contentStack = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Width = 330,
                BackColor = Color.Transparent
            };

            // Header row: Avatar + Name + Edit Button
            var headerTable = new TableLayoutPanel
            {
                Width = 328,
                Height = 56,
                ColumnCount = 3,
                RowCount = 1,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 16)
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F)); // Avatar 48px + 4px
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));  // Name
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40F));  // Edit button 36px

            lblAvatar = new Label
            {
                Size = new Size(46, 46),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(UITheme.FontSans, 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Margin = Padding.Empty
            };
            lblAvatar.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(UITheme.PrimaryMauve);
                e.Graphics.FillEllipse(brush, 0, 0, lblAvatar.Width - 1, lblAvatar.Height - 1);
                TextRenderer.DrawText(e.Graphics, lblAvatar.Text, lblAvatar.Font, lblAvatar.ClientRectangle, Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };

            lblCustomerName = new Label
            {
                Text = "",
                Font = new Font(UITheme.FontSerif, 17F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(8, 0, 0, 0),
                AutoEllipsis = true
            };

            btnEditCustomer = new Button
            {
                Size = new Size(34, 34),
                Anchor = AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
                Margin = Padding.Empty
            };
            btnEditCustomer.FlatAppearance.BorderSize = 0;
            btnEditCustomer.FlatAppearance.MouseOverBackColor = Color.Transparent;
            btnEditCustomer.FlatAppearance.MouseDownBackColor = Color.Transparent;
            btnEditCustomer.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool hovered = btnEditCustomer.ClientRectangle.Contains(btnEditCustomer.PointToClient(Cursor.Position));
                using var bg = new SolidBrush(hovered ? Color.FromArgb(228, 220, 212) : Color.FromArgb(242, 236, 230));
                e.Graphics.FillEllipse(bg, 0, 0, btnEditCustomer.Width - 1, btnEditCustomer.Height - 1);

                using var font = new Font("Segoe MDL2 Assets", 10.5F);
                TextRenderer.DrawText(e.Graphics, "\uE70F", font, btnEditCustomer.ClientRectangle, UITheme.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            };
            btnEditCustomer.MouseEnter += (s, e) => btnEditCustomer.Invalidate();
            btnEditCustomer.MouseLeave += (s, e) => btnEditCustomer.Invalidate();
            btnEditCustomer.Click += BtnEditCustomer_Click;

            headerTable.Controls.Add(lblAvatar, 0, 0);
            headerTable.Controls.Add(lblCustomerName, 1, 0);
            headerTable.Controls.Add(btnEditCustomer, 2, 0);
            contentStack.Controls.Add(headerTable);

            // Customer Attributes
            lblEmailValue = AddAttributeBlock(contentStack, "EMAIL");
            lblPhoneValue = AddAttributeBlock(contentStack, "PHONE");
            lblAddressValue = AddAttributeBlock(contentStack, "ADDRESS");
            lblSinceValue = AddAttributeBlock(contentStack, "CUSTOMER SINCE");
            lblTotalOrdersValue = AddAttributeBlock(contentStack, "TOTAL ORDERS");
            lblLastContactValue = AddAttributeBlock(contentStack, "LAST CONTACT");

            // Horizontal Divider
            var divider = new Panel
            {
                Width = 328,
                Height = 1,
                BackColor = UITheme.BorderColor,
                Margin = new Padding(0, 10, 0, 14)
            };
            contentStack.Controls.Add(divider);

            lblPreferencesValue = AddAttributeBlock(contentStack, "PREFERENCES");
            lblNotesValue = AddAttributeBlock(contentStack, "NOTES");

            leftCard.Controls.Add(contentStack);
            mainLayout.Controls.Add(leftCard, 0, 0);
        }

        private Label AddAttributeBlock(FlowLayoutPanel stack, string labelText)
        {
            var lblTitle = new Label
            {
                Text = labelText,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2)
            };

            var lblValue = new Label
            {
                Text = "-",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                MaximumSize = new Size(328, 0),
                Margin = new Padding(0, 0, 0, 12)
            };

            stack.Controls.Add(lblTitle);
            stack.Controls.Add(lblValue);
            return lblValue;
        }

        private void BuildRightStack()
        {
            var rightTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                BackColor = Color.Transparent,
                Padding = Padding.Empty,
                Margin = Padding.Empty
            };
            rightTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); // Order History
            rightTable.RowStyles.Add(new RowStyle(SizeType.Percent, 50F)); // Interaction History

            // 1. ORDER HISTORY CARD
            rightTopCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Margin = new Padding(0, 0, 0, 14),
                Padding = new Padding(20, 16, 20, 16)
            };
            rightTopCard.ApplyRoundedRegion(14);
            rightTopCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, rightTopCard.Width - 1, rightTopCard.Height - 1), 14);
            };

            var orderHeader = CreateCardHeader("\uE7BF", "Order History", out lblOrderCountBadge);
            orderHeader.Dock = DockStyle.Top;

            gridOrders = new DataGridView
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(0, 8, 0, 0),
                ScrollBars = ScrollBars.Vertical,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            UITheme.ApplyTableStyle(gridOrders);
            gridOrders.RowTemplate.Height = 48;
            gridOrders.AutoGenerateColumns = false;
            gridOrders.ScrollBars = ScrollBars.Vertical;
            gridOrders.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            gridOrders.ColumnHeadersDefaultCellStyle.Padding = new Padding(12, 0, 0, 0);
            gridOrders.DefaultCellStyle.Padding = new Padding(12, 0, 0, 0);

            var colOrderId = new DataGridViewTextBoxColumn
            {
                Name = "colOrderId",
                HeaderText = "ORDER ID",
                DataPropertyName = "OrderId",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 19F,
                MinimumWidth = 85,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colOrderId.DefaultCellStyle.ForeColor = UITheme.PrimaryMauve;
            colOrderId.DefaultCellStyle.Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);

            var colDesign = new DataGridViewTextBoxColumn
            {
                Name = "colDesign",
                HeaderText = "DESIGN",
                DataPropertyName = "Design",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 31F,
                MinimumWidth = 100,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colEventDate = new DataGridViewTextBoxColumn
            {
                Name = "colEventDate",
                HeaderText = "EVENT DATE",
                DataPropertyName = "EventDate",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 20F,
                MinimumWidth = 85,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            var colTotal = new DataGridViewTextBoxColumn
            {
                Name = "colTotal",
                HeaderText = "TOTAL",
                DataPropertyName = "Total",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 14F,
                MinimumWidth = 65,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            colTotal.DefaultCellStyle.Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);

            var colStatus = new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "STATUS",
                DataPropertyName = "Status",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 16F,
                MinimumWidth = 85,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };

            gridOrders.Columns.AddRange(colOrderId, colDesign, colEventDate, colTotal, colStatus);
            gridOrders.CellPainting += GridOrders_CellPainting;

            lblNoOrders = new Label
            {
                Text = "No order history yet",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Italic),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            rightTopCard.Controls.Add(lblNoOrders);
            rightTopCard.Controls.Add(gridOrders);
            rightTopCard.Controls.Add(orderHeader);

            // 2. INTERACTION HISTORY CARD
            rightBottomCard = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(20, 16, 20, 16)
            };
            rightBottomCard.ApplyRoundedRegion(14);
            rightBottomCard.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, rightBottomCard.Width - 1, rightBottomCard.Height - 1), 14);
            };

            var interactionHeader = CreateCardHeader("\uE8BD", "Interaction History", out lblInteractionCountBadge);
            interactionHeader.Dock = DockStyle.Top;

            pnlInteractionsScroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 8, 4, 0)
            };

            lblNoInteractions = new Label
            {
                Text = "No interaction history yet",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Italic),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false
            };

            rightBottomCard.Controls.Add(lblNoInteractions);
            rightBottomCard.Controls.Add(pnlInteractionsScroll);
            rightBottomCard.Controls.Add(interactionHeader);

            rightTable.Controls.Add(rightTopCard, 0, 0);
            rightTable.Controls.Add(rightBottomCard, 0, 1);
            mainLayout.Controls.Add(rightTable, 1, 0);
        }

        private Panel CreateCardHeader(string glyph, string title, out Label countBadge)
        {
            var header = new Panel
            {
                Height = 36,
                BackColor = Color.Transparent
            };

            var leftFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            var iconLbl = new Label
            {
                Text = glyph,
                Font = new Font("Segoe MDL2 Assets", 11.5F),
                ForeColor = UITheme.TextDark,
                Size = new Size(24, 28),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 0, 6, 0)
            };

            var titleLbl = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSans, 11F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 2, 0, 0)
            };

            leftFlow.Controls.Add(iconLbl);
            leftFlow.Controls.Add(titleLbl);

            countBadge = new Label
            {
                Text = "0",
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                BackColor = Color.FromArgb(239, 230, 222),
                Size = new Size(26, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Right,
                Margin = new Padding(0, 2, 0, 0)
            };
            countBadge.ApplyRoundedRegion(11);

            header.Controls.Add(leftFlow);
            header.Controls.Add(countBadge);

            return header;
        }

        private void PopulateCustomerData()
        {
            if (_customer == null) return;

            string fullName = $"{_customer.FirstName} {_customer.LastName}".Trim();
            lblCustomerName.Text = fullName;
            lblAvatar.Text = GetInitials(fullName);
            lblAvatar.Invalidate();

            lblEmailValue.Text = !string.IsNullOrWhiteSpace(_customer.Email) ? _customer.Email : "-";
            lblPhoneValue.Text = !string.IsNullOrWhiteSpace(_customer.Phone) ? _customer.Phone : "-";
            lblAddressValue.Text = !string.IsNullOrWhiteSpace(_customer.AddressText) ? _customer.AddressText : "-";
            lblSinceValue.Text = _customer.RegisteredDate > DateTime.MinValue ? _customer.RegisteredDate.ToString("yyyy-MM-dd") : "-";

            var orders = _customer.Orders?.OrderByDescending(o => o.OrderDate).ToList() ?? new List<SalesOrder>();
            lblTotalOrdersValue.Text = orders.Count.ToString();

            DateTime lastDate = orders.FirstOrDefault()?.OrderDate ?? _customer.RegisteredDate;
            var followups = _customer.FollowUps?.OrderByDescending(f => f.FollowUpDate).ToList() ?? new List<CustomerFollowUp>();
            if (followups.Any() && followups[0].FollowUpDate > lastDate)
            {
                lastDate = followups[0].FollowUpDate;
            }
            lblLastContactValue.Text = lastDate > DateTime.MinValue ? lastDate.ToString("yyyy-MM-dd") : "-";

            lblPreferencesValue.Text = !string.IsNullOrWhiteSpace(_customer.CakePreferences) ? _customer.CakePreferences : "None specified";
            lblNotesValue.Text = !string.IsNullOrWhiteSpace(_customer.Notes) ? _customer.Notes : "No notes";

            // 1. Order History
            lblOrderCountBadge.Text = orders.Count.ToString();
            if (orders.Count > 0)
            {
                lblNoOrders.Visible = false;
                gridOrders.Visible = true;

                var rows = orders.Select(o => new
                {
                    OrderId = $"#ORD-{o.OrderId}",
                    Design = !string.IsNullOrWhiteSpace(o.DesignTheme) ? o.DesignTheme : (!string.IsNullOrWhiteSpace(o.CakeSize) ? o.CakeSize : "Custom Cake"),
                    EventDate = o.DeliveryDate?.ToString("yyyy-MM-dd") ?? o.OrderDate.ToString("yyyy-MM-dd"),
                    Total = $"₱{(o.TotalAmount > 0 ? o.TotalAmount : 0):N0}",
                    Status = GetStatusName(o.StatusId)
                }).ToList();

                gridOrders.DataSource = rows;
            }
            else
            {
                gridOrders.Visible = false;
                lblNoOrders.Visible = true;
            }

            // 2. Interaction History (Follow-ups, Inquiries, Feedback)
            var inquiries = _customer.Inquiries?.OrderByDescending(i => i.CreatedAt).ToList() ?? new List<CustomerInquiry>();
            int totalInteractions = followups.Count + inquiries.Count;
            lblInteractionCountBadge.Text = totalInteractions.ToString();

            pnlInteractionsScroll.Controls.Clear();

            if (totalInteractions > 0)
            {
                lblNoInteractions.Visible = false;
                pnlInteractionsScroll.Visible = true;

                int cardY = 0;
                const int cardGap = 10;

                // Render follow-ups / feedback cards
                foreach (var f in followups)
                {
                    string statusText = f.StatusId switch
                    {
                        1 => "Completed",
                        2 => "Cancelled",
                        3 => "Overdue",
                        _ => "Scheduled"
                    };

                    (Color pillBg, Color pillFg) = statusText switch
                    {
                        "Completed" => (UITheme.StatusGreenBg, UITheme.StatusGreenFg),
                        "Cancelled" => (UITheme.StatusRedBg, UITheme.StatusRedFg),
                        "Overdue" => (UITheme.StatusRedBg, UITheme.StatusRedFg),
                        _ => (Color.FromArgb(224, 242, 254), Color.FromArgb(2, 132, 199)) // Soft Blue #E0F2FE / #0284C7
                    };

                    string cardTitle = !string.IsNullOrWhiteSpace(f.Notes) && f.Notes.Length > 20
                        ? "Post-Order Feedback"
                        : "Follow-up Task";

                    var card = CreateInteractionCard(cardTitle, statusText, pillBg, pillFg, $"Scheduled: {f.FollowUpDate:yyyy-MM-dd}", f.Notes);
                    card.Location = new Point(0, cardY);
                    card.Width = pnlInteractionsScroll.ClientSize.Width - 8;
                    card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                    pnlInteractionsScroll.Controls.Add(card);
                    cardY += card.Height + cardGap;
                }

                // Render inquiries cards
                foreach (var inq in inquiries)
                {
                    (Color pillBg, Color pillFg) = inq.Status.ToLower() switch
                    {
                        "approved" => (UITheme.StatusGreenBg, UITheme.StatusGreenFg),
                        "quoted" => (Color.FromArgb(243, 232, 255), Color.FromArgb(126, 34, 206)),
                        "in progress" => (Color.FromArgb(241, 245, 249), Color.FromArgb(71, 85, 105)),
                        _ => (Color.FromArgb(219, 234, 254), Color.FromArgb(29, 78, 216))
                    };

                    string inqDate = inq.EventDate > DateTime.MinValue ? $"Event: {inq.EventDate:yyyy-MM-dd}" : $"Created: {inq.CreatedAt:yyyy-MM-dd}";
                    string notes = !string.IsNullOrWhiteSpace(inq.Notes) ? inq.Notes : inq.CakeType;

                    var card = CreateInteractionCard($"Inquiry: {inq.InquiryCode}", inq.Status, pillBg, pillFg, inqDate, notes);
                    card.Location = new Point(0, cardY);
                    card.Width = pnlInteractionsScroll.ClientSize.Width - 8;
                    card.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

                    pnlInteractionsScroll.Controls.Add(card);
                    cardY += card.Height + cardGap;
                }
            }
            else
            {
                pnlInteractionsScroll.Visible = false;
                lblNoInteractions.Visible = true;
            }
        }

        private Panel CreateInteractionCard(string title, string statusText, Color pillBg, Color pillFg, string dateStr, string? notes)
        {
            var card = new Panel
            {
                Height = 84,
                BackColor = Color.FromArgb(247, 243, 238), // #F7F3EE
                Padding = new Padding(14, 12, 14, 10)
            };
            card.ApplyRoundedRegion(10);
            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(Color.FromArgb(234, 226, 218), 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            };

            // Top row: Title + Status Pill
            var topRow = new Panel
            {
                Dock = DockStyle.Top,
                Height = 26,
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var pillBadge = new Label
            {
                Text = statusText,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = pillFg,
                BackColor = pillBg,
                Size = new Size(88, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Right
            };
            pillBadge.ApplyRoundedRegion(11);

            topRow.Controls.Add(lblTitle);
            topRow.Controls.Add(pillBadge);

            // Subtitle: Date
            var lblDate = new Label
            {
                Text = dateStr,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 18,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Description: Notes
            var lblNotes = new Label
            {
                Text = !string.IsNullOrWhiteSpace(notes) ? notes : "No details provided.",
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(70, 60, 56),
                Dock = DockStyle.Fill,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            card.Controls.Add(lblNotes);
            card.Controls.Add(lblDate);
            card.Controls.Add(topRow);

            return card;
        }

        private void GridOrders_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            // Paint Status Pill in last column
            if (gridOrders.Columns["colStatus"] is { } colStatus && e.ColumnIndex == colStatus.Index)
            {
                e.PaintBackground(e.CellBounds, true);
                var status = e.Value?.ToString() ?? "Pending";
                (Color bg, Color fg) = status.ToLower() switch
                {
                    "completed" => (UITheme.StatusGreenBg, UITheme.StatusGreenFg),
                    "processing" => (Color.FromArgb(224, 242, 254), Color.FromArgb(2, 132, 199)),
                    "ready" => (Color.FromArgb(254, 243, 199), Color.FromArgb(180, 83, 9)),
                    "confirmed" => (Color.FromArgb(240, 253, 244), Color.FromArgb(22, 101, 52)),
                    "cancelled" => (UITheme.StatusRedBg, UITheme.StatusRedFg),
                    _ => (UITheme.StatusYellowBg, UITheme.StatusYellowFg)
                };

                int pillWidth = Math.Min(95, Math.Max(65, e.CellBounds.Width - 14));
                var pillRect = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top + (e.CellBounds.Height - 24) / 2, pillWidth, 24);
                UITheme.DrawStatusPill(e.Graphics, pillRect, $"\u2022 {status}", bg, fg, fg);
                e.Handled = true;
            }
        }

        private async void BtnEditCustomer_Click(object? sender, EventArgs e)
        {
            if (_customer == null) return;

            using var form = new CustomerForm(_customer);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                await LoadCustomerAsync(_customerId);
                CustomerUpdated?.Invoke();
            }
        }

        private static string GetStatusName(int statusId)
        {
            return statusId switch
            {
                0 => "Pending",
                1 => "Confirmed",
                2 => "Processing",
                3 => "Completed",
                4 => "Ready",
                5 => "Cancelled",
                _ => "Pending"
            };
        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "CU";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1) return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
            return (parts[0][0].ToString() + parts[^1][0]).ToUpper();
        }
    }
}
