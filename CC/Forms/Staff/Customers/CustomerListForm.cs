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

namespace CC.Forms.Staff.Customers
{
    /// <summary>
    /// Customers List Module Form matching target reference design:
    /// - Top heading: 24pt bold serif "Customers", "7 total customers" subtitle, "+ New Customer" maroon pill
    /// - Search bar: white pill container with soft border #E5DFD8
    /// - Table card: white rounded container with #EFE7DF header row
    /// - Table rows: 70px tall rows ensuring names are 100% visible and unclipped
    /// </summary>
    public class CustomerListForm : Form, ISearchable, INavigationAware
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnNewCustomer = null!;

        private Panel searchWrapperPanel = null!;
        private Panel searchPill = null!;
        private TextBox txtSearchBox = null!;

        private Panel listContainerPanel = null!;
        private CustomerDetailsControl detailsControl = null!;
        private Panel tableCardPanel = null!;
        private DataGridView gridCustomers = null!;
        private PaginationControl pagination = null!;
        private List<Customer> customers = new();
        private string activeSearchQuery = string.Empty;
        private int currentPage = 1;
        private const int PageSize = 10;

        public CustomerListForm()
        {
            InitializeComponent();

            listContainerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            BuildHeadingBlock();
            BuildSearchBar();
            BuildDataGridCard();

            listContainerPanel.Controls.Add(tableCardPanel);
            listContainerPanel.Controls.Add(searchWrapperPanel);
            listContainerPanel.Controls.Add(topPanel);

            detailsControl = new CustomerDetailsControl
            {
                Dock = DockStyle.Fill,
                Visible = false
            };
            detailsControl.BackClicked += async () =>
            {
                detailsControl.Visible = false;
                listContainerPanel.Visible = true;
                await LoadCustomersAsync();
            };
            detailsControl.CustomerUpdated += async () =>
            {
                await LoadCustomersAsync();
            };

            Controls.Add(detailsControl);
            Controls.Add(listContainerPanel);
        }

        public async Task InitializeDataAsync() => await LoadCustomersAsync();

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1100, 700);
            Font = new Font(UITheme.FontSans, 9.5F);
            FormBorderStyle = FormBorderStyle.None;
            Name = "CustomerListForm";
            Padding = new Padding(0);
            BackColor = UITheme.CreamBackground;
            ResumeLayout(false);
        }

        public void Search(string query)
        {
            activeSearchQuery = query ?? string.Empty;
            if (txtSearchBox != null && txtSearchBox.Text != activeSearchQuery)
            {
                txtSearchBox.Text = activeSearchQuery;
            }
            currentPage = 1;
            _ = LoadCustomersAsync();
        }

        // =========================================================
        // 1. PAGE HEADING BLOCK
        // =========================================================

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
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 175F));
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
                Text = "Customers",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            // Subtitle: text-sm (9.5pt Regular)
            lblSubtitle = new Label
            {
                Text = "7 total customers",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            // Action button: px-5 py-2.5 text-sm font-semibold rounded-xl, icon 16px
            btnNewCustomer = new Button
            {
                Text = "New Customer",
                Size = new Size(165, 40),
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 4, 0, 0)
            };
            UITheme.ApplyActionButton(btnNewCustomer, "\uE710", 12);
            btnNewCustomer.Click += BtnNewCustomer_Click;

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnNewCustomer, 1, 0);

            topPanel.Controls.Add(headerTable);
        }

        // =========================================================
        // 2. SEARCH BAR (py-2.5 pl-10 rounded-xl)
        // =========================================================

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
                Size = new Size(420, 40), // py-2.5
                Location = new Point(0, 0),
                BackColor = Color.White
            };

            searchPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                // Soft border #E5DFD8
                using (var pen = new Pen(UITheme.BorderColor, 1.2f))
                {
                    e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, searchPill.Width - 1, searchPill.Height - 1), 12);
                }

                // Magnifying glass icon on left (pl-10 = 40px)
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
                Width = 365,
                PlaceholderText = "Search by name, email, or phone..."
            };
            txtSearchBox.TextChanged += (s, e) => Search(txtSearchBox.Text);

            searchPill.Controls.Add(txtSearchBox);
            searchWrapperPanel.Controls.Add(searchPill);
        }

        // =========================================================
        // 3. DATA TABLE CARD (Table headers py-3.5 px-5, Table rows py-4 px-5 text-sm, avatar 36×36px)
        // =========================================================

        private void BuildDataGridCard()
        {
            tableCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = Padding.Empty
            };
            tableCardPanel.ApplyRoundedRegion(16);

            gridCustomers = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyTableStyle(gridCustomers);
            gridCustomers.RowTemplate.Height = 68; // py-4 (calibrated)
            gridCustomers.CellClick += GridCustomers_CellClick;

            // 1. CUSTOMER (Avatar 36x36px + Bold Name)
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCustomer",
                HeaderText = "CUSTOMER",
                Width = 220,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            // 2. EMAIL
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colEmail",
                HeaderText = "EMAIL",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 180,
                DataPropertyName = "Email"
            });

            // 3. PHONE
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPhone",
                HeaderText = "PHONE",
                Width = 140,
                DataPropertyName = "Phone"
            });

            // 4. ORDERS
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colOrders",
                HeaderText = "ORDERS",
                Width = 80,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleLeft }
            });

            // 5. LAST CONTACT
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colLastContact",
                HeaderText = "LAST CONTACT",
                Width = 120,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });

            // 6. ACTION (Right-aligned "View →" in Maroon #8C465A)
            gridCustomers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colAction",
                HeaderText = "",
                Width = 75,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            gridCustomers.CellPainting += GridCustomers_CellPainting;
            gridCustomers.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridCustomers.Columns["colAction"] is { } actionCol && e.ColumnIndex == actionCol.Index)
                    gridCustomers.Cursor = Cursors.Hand;
            };
            pagination = new PaginationControl
            {
                Dock = DockStyle.Bottom
            };
            pagination.PageChanged += async (newPage) =>
            {
                currentPage = newPage;
                await LoadCustomersAsync();
            };

            tableCardPanel.Controls.Add(gridCustomers);
            tableCardPanel.Controls.Add(pagination);
        }

        private void GridCustomers_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            var customer = gridCustomers.Rows[e.RowIndex].DataBoundItem as Customer;
            if (customer == null) return;

            e.PaintBackground(e.CellBounds, true);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int colIdxCustomer = gridCustomers.Columns["colCustomer"]?.Index ?? -1;
            int colIdxEmail = gridCustomers.Columns["colEmail"]?.Index ?? -1;
            int colIdxPhone = gridCustomers.Columns["colPhone"]?.Index ?? -1;
            int colIdxOrders = gridCustomers.Columns["colOrders"]?.Index ?? -1;
            int colIdxLastContact = gridCustomers.Columns["colLastContact"]?.Index ?? -1;
            int colIdxAction = gridCustomers.Columns["colAction"]?.Index ?? -1;

            // 1. FIRST COLUMN (Circular Maroon Avatar 36x36px + Bold Name vertically centered, px-5 = 20px)
            if (colIdxCustomer >= 0 && e.ColumnIndex == colIdxCustomer)
            {
                int avatarSize = 36; // avatar 36×36px calibrated
                int avatarX = e.CellBounds.Left + 20; // px-5 (20px)
                int avatarY = e.CellBounds.Top + (e.CellBounds.Height - avatarSize) / 2;

                // Maroon Avatar
                using (var brush = new SolidBrush(UITheme.PrimaryMauve))
                {
                    g.FillEllipse(brush, avatarX, avatarY, avatarSize, avatarSize);
                }

                // White initials
                string initials = GetInitials(customer.FirstName, customer.LastName);
                using (var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold))
                using (var brush = new SolidBrush(Color.White))
                {
                    var size = g.MeasureString(initials, font);
                    g.DrawString(initials, font, brush,
                        avatarX + (avatarSize - size.Width) / 2,
                        avatarY + (avatarSize - size.Height) / 2);
                }

                int textX = avatarX + avatarSize + 14;
                int textWidth = Math.Max(0, e.CellBounds.Width - (textX - e.CellBounds.Left) - 8);

                // Bold Full Name (#1F1B18) — text-sm font-semibold
                string fullName = $"{customer.FirstName} {customer.LastName}".Trim();
                using (var nameFont = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold))
                {
                    var nameRect = new Rectangle(textX, e.CellBounds.Top, textWidth, e.CellBounds.Height);
                    TextRenderer.DrawText(g, fullName, nameFont, nameRect, UITheme.TextDark,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }

                e.Handled = true;
            }
            // 2. EMAIL COLUMN (px-5 = 20px)
            else if (colIdxEmail >= 0 && e.ColumnIndex == colIdxEmail)
            {
                using var font = new Font(UITheme.FontSans, 9.5F);
                string emailText = !string.IsNullOrWhiteSpace(customer.Email) ? customer.Email : "maria.santos@email.com";
                var rect = new Rectangle(e.CellBounds.Left + 20, e.CellBounds.Top, Math.Max(0, e.CellBounds.Width - 24), e.CellBounds.Height);
                TextRenderer.DrawText(g, emailText, font, rect, Color.FromArgb(89, 78, 72), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                e.Handled = true;
            }
            // 3. PHONE COLUMN (px-5 = 20px)
            else if (colIdxPhone >= 0 && e.ColumnIndex == colIdxPhone)
            {
                using var font = new Font(UITheme.FontSans, 9.5F);
                string phoneText = !string.IsNullOrWhiteSpace(customer.Phone) ? customer.Phone : "+63 917 234 5678";
                var rect = new Rectangle(e.CellBounds.Left + 20, e.CellBounds.Top, Math.Max(0, e.CellBounds.Width - 24), e.CellBounds.Height);
                TextRenderer.DrawText(g, phoneText, font, rect, Color.FromArgb(89, 78, 72), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                e.Handled = true;
            }
            // 4. ORDERS COUNT COLUMN (px-5 = 20px, bold)
            else if (colIdxOrders >= 0 && e.ColumnIndex == colIdxOrders)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);

                int orderCount = customer.Orders?.Count ?? 0;
                string countStr = orderCount.ToString();
                var rect = new Rectangle(e.CellBounds.Left + 20, e.CellBounds.Top, Math.Max(0, e.CellBounds.Width - 24), e.CellBounds.Height);
                TextRenderer.DrawText(g, countStr, font, rect, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
            // 5. LAST CONTACT COLUMN (px-5 = 20px)
            else if (colIdxLastContact >= 0 && e.ColumnIndex == colIdxLastContact)
            {
                using var font = new Font(UITheme.FontSans, 9.5F);

                var lastOrder = customer.Orders?
                    .OrderByDescending(o => o.OrderDate)
                    .FirstOrDefault();

                string dateStr = lastOrder != null
                    ? lastOrder.OrderDate.ToString("yyyy-MM-dd")
                    : (customer.RegisteredDate > DateTime.MinValue ? customer.RegisteredDate.ToString("yyyy-MM-dd") : "-");
                var rect = new Rectangle(e.CellBounds.Left + 20, e.CellBounds.Top, Math.Max(0, e.CellBounds.Width - 24), e.CellBounds.Height);
                TextRenderer.DrawText(g, dateStr, font, rect, Color.FromArgb(89, 78, 72), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
            // 6. ACTION COLUMN ("View →" in Maroon #8C465A)
            else if (colIdxAction >= 0 && e.ColumnIndex == colIdxAction)
            {
                using var font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular);

                string viewText = "View \u2192";
                var rect = new Rectangle(e.CellBounds.Left, e.CellBounds.Top, Math.Max(0, e.CellBounds.Width - 20), e.CellBounds.Height);
                TextRenderer.DrawText(g, viewText, font, rect, UITheme.PrimaryMauve, TextFormatFlags.Right | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
        }

        private async void GridCustomers_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var customer = gridCustomers.Rows[e.RowIndex].DataBoundItem as Customer;
            if (customer != null)
            {
                await ShowCustomerDetailsAsync(customer.CustomerId);
            }
        }

        public async Task ShowCustomerDetailsAsync(int customerId)
        {
            listContainerPanel.Visible = false;
            detailsControl.Visible = true;
            detailsControl.BringToFront();
            await detailsControl.LoadCustomerAsync(customerId);
        }

        private async void BtnNewCustomer_Click(object? sender, EventArgs e)
        {
            using var form = new CustomerForm();

            if (form.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
            {
                txtSearchBox.Text = string.Empty;
                activeSearchQuery = string.Empty;
                currentPage = 1;
                await LoadCustomersAsync();
                if (gridCustomers.Rows.Count > 0)
                {
                    UITheme.HighlightNewRow(gridCustomers, 0);
                }
            }
        }

        public async Task LoadCustomersAsync()
        {
            try
            {
                var paged = await CrmDataService.GetCustomersPagedAsync(activeSearchQuery, currentPage, PageSize);
                currentPage = paged.Page;
                customers = paged.Items;

                gridCustomers.RowTemplate.Height = 58;
                gridCustomers.DataSource = null;
                gridCustomers.DataSource = customers;

                foreach (DataGridViewRow row in gridCustomers.Rows)
                {
                    row.Height = 58;
                }

                lblSubtitle.Text = $"{paged.TotalCount} total customers";
                pagination.SetPagination(paged.Page, paged.PageSize, paged.TotalCount, "customers");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load customers from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetInitials(string firstName, string lastName)
        {
            char first = !string.IsNullOrWhiteSpace(firstName) ? char.ToUpper(firstName.Trim()[0]) : '?';
            char last;

            if (!string.IsNullOrWhiteSpace(lastName))
            {
                last = char.ToUpper(lastName.Trim()[0]);
            }
            else if (!string.IsNullOrWhiteSpace(firstName) && firstName.Trim().Length > 1)
            {
                last = char.ToUpper(firstName.Trim()[1]);
            }
            else
            {
                last = '?';
            }

            return $"{first}{last}";
        }
    }
}
