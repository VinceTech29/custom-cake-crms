using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.SuperAdmin.Businesses
{
    public class BusinessListForm : Form
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnAddBusiness = null!;

        private TableLayoutPanel kpiTable = null!;
        private Label lblTotalBiz = null!;
        private Label lblActiveBiz = null!;
        private Label lblInactiveBiz = null!;
        private Label lblTenantDbs = null!;

        private Panel searchFilterPanel = null!;
        private Panel searchPill = null!;
        private TextBox txtSearchBox = null!;
        private ComboBox cmbStatusFilter = null!;

        private Panel tableCardPanel = null!;
        private DataGridView gridBusinesses = null!;

        private List<CompanyListItem> businessList = new List<CompanyListItem>();
        private string activeSearchQuery = string.Empty;
        private string activeStatusFilter = "All Status";

        public BusinessListForm()
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

            Load += async (s, e) => await RefreshDataAsync();
            VisibleChanged += async (s, e) =>
            {
                if (Visible) await RefreshDataAsync();
            };
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1100, 750);
            Font = new Font(UITheme.FontSans, 9.5F);
            FormBorderStyle = FormBorderStyle.None;
            Name = "BusinessListForm";
            Padding = Padding.Empty;
            BackColor = UITheme.CreamBackground;
            ResumeLayout(false);
        }

        private void BuildTopToolbar()
        {
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 78,
                Padding = new Padding(0, 0, 0, 14),
                BackColor = Color.Transparent
            };

            var headerTable = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
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
                Text = "Business & Tenant Management",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            lblSubtitle = new Label
            {
                Text = "Supervise registered businesses, tenant database routing, and platform companies",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            var btnStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                Padding = new Padding(0, 6, 0, 0)
            };

            btnAddBusiness = new Button
            {
                Text = "+ Add Business",
                Height = 40,
                Width = 150,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = Padding.Empty
            };
            UITheme.ApplyActionButton(btnAddBusiness, "\uE710", 10);
            btnAddBusiness.Click += (s, e) =>
            {
                using var modal = new BusinessModal();
                if (modal.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                {
                    _ = RefreshDataAsync();
                }
            };

            btnStack.Controls.Add(btnAddBusiness);

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnStack, 1, 0);
            topPanel.Controls.Add(headerTable);
        }

        private void BuildKpiCards()
        {
            kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 98,
                ColumnCount = 4,
                RowCount = 1,
                Padding = new Padding(0, 0, 0, 16),
                BackColor = Color.Transparent
            };

            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            kpiTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var card1 = CreateKpiCard("TOTAL BUSINESSES", "0", out lblTotalBiz, 0);
            var card2 = CreateKpiCard("ACTIVE TENANTS", "0", out lblActiveBiz, 1);
            var card3 = CreateKpiCard("INACTIVE / SUSPENDED", "0", out lblInactiveBiz, 2);
            var card4 = CreateKpiCard("TENANT DATABASES", "0", out lblTenantDbs, 3);

            kpiTable.Controls.Add(card1, 0, 0);
            kpiTable.Controls.Add(card2, 1, 0);
            kpiTable.Controls.Add(card3, 2, 0);
            kpiTable.Controls.Add(card4, 3, 0);
        }

        private Panel CreateKpiCard(string title, string value, out Label lblValue, int colIdx)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(colIdx == 0 ? 0 : 7, 0, colIdx == 3 ? 0 : 7, 0)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
            };
            card.ApplyRoundedRegion(12);

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font(UITheme.FontSans, 7.5F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 16
            };

            lblValue = new Label
            {
                Text = value,
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.BottomLeft
            };

            card.Controls.Add(lblValue);
            card.Controls.Add(lblTitle);
            return card;
        }

        private void BuildSearchAndFilterBar()
        {
            searchFilterPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                Padding = new Padding(0, 0, 0, 12),
                BackColor = Color.Transparent
            };

            var rowFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent
            };

            searchPill = new Panel
            {
                Size = new Size(360, 40),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 14, 0)
            };
            searchPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, searchPill.Width - 1, searchPill.Height - 1), 12);
            };
            searchPill.ApplyRoundedRegion(12);

            var lblGlass = new Label
            {
                Text = "\uE721",
                Font = new Font("Segoe MDL2 Assets", 10F),
                ForeColor = UITheme.TextMuted,
                Size = new Size(24, 24),
                Location = new Point(10, 8),
                TextAlign = ContentAlignment.MiddleCenter
            };

            txtSearchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font(UITheme.FontSans, 9.5F),
                ForeColor = UITheme.TextDark,
                Location = new Point(38, 10),
                Width = 300,
                PlaceholderText = "Search by business name, code, email..."
            };

            txtSearchBox.TextChanged += async (s, e) =>
            {
                activeSearchQuery = txtSearchBox.Text;
                await RefreshDataAsync();
            };

            searchPill.Controls.Add(lblGlass);
            searchPill.Controls.Add(txtSearchBox);
            rowFlow.Controls.Add(searchPill);

            // Status filter dropdown
            var statusFilterPill = new Panel
            {
                Size = new Size(160, 40),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 12, 0)
            };
            statusFilterPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, statusFilterPill.Width - 1, statusFilterPill.Height - 1), 12);
            };
            statusFilterPill.ApplyRoundedRegion(12);

            cmbStatusFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(10, 9),
                Width = 140,
                BackColor = Color.White
            };
            cmbStatusFilter.Items.AddRange(new object[] { "All Status", "Active", "Inactive" });
            cmbStatusFilter.SelectedIndex = 0;
            cmbStatusFilter.SelectedIndexChanged += async (s, e) =>
            {
                activeStatusFilter = cmbStatusFilter.SelectedItem?.ToString() ?? "All Status";
                await RefreshDataAsync();
            };
            statusFilterPill.Controls.Add(cmbStatusFilter);
            rowFlow.Controls.Add(statusFilterPill);

            searchFilterPanel.Controls.Add(rowFlow);
        }

        private void BuildDataGridCard()
        {
            tableCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };

            tableCardPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, tableCardPanel.Width - 1, tableCardPanel.Height - 1), 12);
            };
            tableCardPanel.ApplyRoundedRegion(12);

            gridBusinesses = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(245, 241, 237),
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false,
                RowTemplate = { Height = 58 },
                EnableHeadersVisualStyles = false,
                ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None
            };

            gridBusinesses.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.FromArgb(250, 248, 245),
                ForeColor = UITheme.TextMuted,
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                Padding = new Padding(16, 0, 0, 0),
                Alignment = DataGridViewContentAlignment.MiddleLeft
            };
            gridBusinesses.ColumnHeadersHeight = 44;

            gridBusinesses.DefaultCellStyle = new DataGridViewCellStyle
            {
                BackColor = Color.White,
                ForeColor = UITheme.TextDark,
                Font = new Font(UITheme.FontSans, 9.5F),
                Padding = new Padding(16, 0, 0, 0),
                SelectionBackColor = Color.FromArgb(252, 246, 247),
                SelectionForeColor = UITheme.TextDark
            };

            // Columns
            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCompany",
                HeaderText = "BUSINESS / COMPANY",
                Width = 260
            });

            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colContact",
                HeaderText = "CONTACT DETAILS",
                Width = 220
            });

            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colDatabase",
                HeaderText = "TENANT DATABASE",
                Width = 180
            });

            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCreated",
                HeaderText = "REGISTERED",
                Width = 120
            });

            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "STATUS",
                Width = 110
            });

            gridBusinesses.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colActions",
                HeaderText = "ACTIONS",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 190
            });

            gridBusinesses.CellPainting += GridBusinesses_CellPainting;
            gridBusinesses.CellClick += GridBusinesses_CellClick;

            tableCardPanel.Controls.Add(gridBusinesses);
        }

        private void GridBusinesses_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;

            var company = gridBusinesses.Rows[e.RowIndex].Tag as CompanyListItem;
            if (company == null) return;

            e.PaintBackground(e.CellBounds, true);
            var g = e.Graphics;
            if (g == null) return;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int colIdxCompany = gridBusinesses.Columns["colCompany"]?.Index ?? -1;
            int colIdxContact = gridBusinesses.Columns["colContact"]?.Index ?? -1;
            int colIdxDatabase = gridBusinesses.Columns["colDatabase"]?.Index ?? -1;
            int colIdxCreated = gridBusinesses.Columns["colCreated"]?.Index ?? -1;
            int colIdxStatus = gridBusinesses.Columns["colStatus"]?.Index ?? -1;
            int colIdxActions = gridBusinesses.Columns["colActions"]?.Index ?? -1;

            // 1. BUSINESS / COMPANY COLUMN
            if (colIdxCompany >= 0 && e.ColumnIndex == colIdxCompany)
            {
                int avatarSize = 34;
                int avatarX = e.CellBounds.Left + 16;
                int avatarY = e.CellBounds.Top + (e.CellBounds.Height - avatarSize) / 2;
                var rectAvatar = new Rectangle(avatarX, avatarY, avatarSize, avatarSize);

                using (var brush = new SolidBrush(UITheme.PrimaryMauve))
                {
                    g.FillRoundedRectangle(brush, rectAvatar, 8);
                }

                string initials = !string.IsNullOrWhiteSpace(company.CompanyCode) ? company.CompanyCode : "CC";
                if (initials.Length > 3) initials = initials.Substring(0, 3);

                using (var fontInitials = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold))
                {
                    TextRenderer.DrawText(g, initials, fontInitials, rectAvatar, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                int textLeft = avatarX + avatarSize + 12;
                int textTop = e.CellBounds.Top + (e.CellBounds.Height - 38) / 2;

                using var fontName = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                using var fontSub = new Font(UITheme.FontSans, 8F, FontStyle.Regular);

                var rectName = new Rectangle(textLeft, textTop, e.CellBounds.Width - (textLeft - e.CellBounds.Left) - 8, 18);
                TextRenderer.DrawText(g, company.CompanyName, fontName, rectName, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                var rectCode = new Rectangle(textLeft, textTop + 19, e.CellBounds.Width - (textLeft - e.CellBounds.Left) - 8, 16);
                TextRenderer.DrawText(g, $"Code: {company.CompanyCode} \u00B7 {company.UserCount} user(s)", fontSub, rectCode, UITheme.TextMuted, TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                e.Handled = true;
            }
            // 2. CONTACT DETAILS
            else if (colIdxContact >= 0 && e.ColumnIndex == colIdxContact)
            {
                int textTop = e.CellBounds.Top + (e.CellBounds.Height - 36) / 2;
                using var fontMail = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                using var fontPhone = new Font(UITheme.FontSans, 8F, FontStyle.Regular);

                var rectEmail = new Rectangle(e.CellBounds.Left + 16, textTop, e.CellBounds.Width - 20, 18);
                TextRenderer.DrawText(g, company.ContactEmail, fontMail, rectEmail, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                var rectPhone = new Rectangle(e.CellBounds.Left + 16, textTop + 18, e.CellBounds.Width - 20, 16);
                TextRenderer.DrawText(g, string.IsNullOrWhiteSpace(company.ContactPhone) ? "No phone" : company.ContactPhone, fontPhone, rectPhone, UITheme.TextMuted, TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                e.Handled = true;
            }
            // 3. DATABASE
            else if (colIdxDatabase >= 0 && e.ColumnIndex == colIdxDatabase)
            {
                int textTop = e.CellBounds.Top + (e.CellBounds.Height - 36) / 2;
                using var fontDb = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                using var fontSrv = new Font(UITheme.FontSans, 8F, FontStyle.Regular);

                var rectDb = new Rectangle(e.CellBounds.Left + 16, textTop, e.CellBounds.Width - 20, 18);
                TextRenderer.DrawText(g, company.DatabaseName, fontDb, rectDb, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                var rectSrv = new Rectangle(e.CellBounds.Left + 16, textTop + 18, e.CellBounds.Width - 20, 16);
                TextRenderer.DrawText(g, company.ServerName, fontSrv, rectSrv, UITheme.TextMuted, TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                e.Handled = true;
            }
            // 4. CREATED DATE
            else if (colIdxCreated >= 0 && e.ColumnIndex == colIdxCreated)
            {
                using var font = new Font(UITheme.FontSans, 9F);
                var rect = new Rectangle(e.CellBounds.Left + 16, e.CellBounds.Top, e.CellBounds.Width - 20, e.CellBounds.Height);
                TextRenderer.DrawText(g, company.CreatedDate.ToString("MMM dd, yyyy"), font, rect, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            // 5. STATUS
            else if (colIdxStatus >= 0 && e.ColumnIndex == colIdxStatus)
            {
                string statusText = company.IsActive ? "\u25CF Active" : "\u25CF Inactive";
                Color pillBg = company.IsActive ? Color.FromArgb(235, 247, 238) : Color.FromArgb(245, 245, 245);
                Color pillText = company.IsActive ? Color.FromArgb(46, 133, 90) : Color.FromArgb(128, 128, 128);

                using var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold);
                var size = TextRenderer.MeasureText(statusText, font);
                int pillWidth = size.Width + 16;
                int pillHeight = 24;
                int pillX = e.CellBounds.Left + 16;
                int pillY = e.CellBounds.Top + (e.CellBounds.Height - pillHeight) / 2;

                var pillRect = new Rectangle(pillX, pillY, pillWidth, pillHeight);
                using (var brush = new SolidBrush(pillBg))
                {
                    g.FillRoundedRectangle(brush, pillRect, 8);
                }

                TextRenderer.DrawText(g, statusText, font, pillRect, pillText, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            // 6. ACTIONS (View | Edit | Toggle)
            else if (colIdxActions >= 0 && e.ColumnIndex == colIdxActions)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                int yCenter = e.CellBounds.Top + (e.CellBounds.Height - 24) / 2;

                // View
                var rectView = new Rectangle(e.CellBounds.Left + 10, yCenter, 40, 24);
                TextRenderer.DrawText(g, "View", font, rectView, Color.FromArgb(40, 95, 160), TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                var rectDot1 = new Rectangle(e.CellBounds.Left + 48, yCenter, 10, 24);
                TextRenderer.DrawText(g, "\u00B7", font, rectDot1, Color.FromArgb(180, 170, 165), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Edit
                var rectEdit = new Rectangle(e.CellBounds.Left + 60, yCenter, 38, 24);
                TextRenderer.DrawText(g, "Edit", font, rectEdit, UITheme.PrimaryMauve, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                var rectDot2 = new Rectangle(e.CellBounds.Left + 98, yCenter, 10, 24);
                TextRenderer.DrawText(g, "\u00B7", font, rectDot2, Color.FromArgb(180, 170, 165), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Toggle
                string toggleText = company.IsActive ? "Deactivate" : "Activate";
                Color toggleColor = company.IsActive ? Color.FromArgb(180, 60, 60) : Color.FromArgb(46, 133, 90);
                var rectToggle = new Rectangle(e.CellBounds.Left + 110, yCenter, 80, 24);
                TextRenderer.DrawText(g, toggleText, font, rectToggle, toggleColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
        }

        private async void GridBusinesses_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var company = gridBusinesses.Rows[e.RowIndex].Tag as CompanyListItem;
            if (company == null) return;

            if (gridBusinesses.Columns["colActions"] is { } colActions && e.ColumnIndex == colActions.Index)
            {
                var cellBounds = gridBusinesses.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                var mousePos = gridBusinesses.PointToClient(Cursor.Position);
                int relX = mousePos.X - cellBounds.Left;

                // View clicked (relX < 55)
                if (relX < 55)
                {
                    using var detailsModal = new BusinessDetailsModal(company.CompanyId);
                    detailsModal.ShowDialog(this.FindForm() ?? this);
                }
                // Edit clicked (relX between 55 and 105)
                else if (relX < 105)
                {
                    using var modal = new BusinessModal(company);
                    if (modal.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                    {
                        await RefreshDataAsync();
                    }
                }
                // Toggle clicked (relX >= 105)
                else
                {
                    await PromptToggleStatusAsync(company);
                }
            }
        }

        private async Task PromptToggleStatusAsync(CompanyListItem company)
        {
            string action = company.IsActive ? "deactivate" : "activate";
            var res = MessageBox.Show(
                $"Are you sure you want to {action} '{company.CompanyName}' ({company.CompanyCode})?\n\n" +
                (company.IsActive
                    ? "Users under this business will no longer be able to log in to the system."
                    : "Users under this business will immediately regain system access."),
                $"Confirm {char.ToUpper(action[0]) + action.Substring(1)}",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                await CrmDataService.ToggleCompanyStatusAsync(company.CompanyId, !company.IsActive);
                await RefreshDataAsync();
            }
        }

        public async Task RefreshDataAsync()
        {
            try
            {
                businessList = await CrmDataService.GetCompaniesAsync(
                    searchQuery: activeSearchQuery,
                    statusFilter: activeStatusFilter
                );

                gridBusinesses.Rows.Clear();
                foreach (var c in businessList)
                {
                    int rowIdx = gridBusinesses.Rows.Add();
                    gridBusinesses.Rows[rowIdx].Tag = c;
                }

                lblSubtitle.Text = $"{businessList.Count} business(es) listed \u00B7 {activeStatusFilter}";

                var metrics = await CrmDataService.GetPlatformMetricsAsync();
                lblTotalBiz.Text = metrics.TotalBusinesses.ToString();
                lblActiveBiz.Text = metrics.ActiveBusinesses.ToString();
                lblInactiveBiz.Text = metrics.InactiveBusinesses.ToString();
                lblTenantDbs.Text = metrics.TotalTenantDatabases.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BusinessListForm.RefreshDataAsync] {ex.Message}");
            }
        }
    }
}
