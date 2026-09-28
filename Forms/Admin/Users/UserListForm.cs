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

namespace CC.Forms.Admin.Users
{
    /// <summary>
    /// User Management module for Business Admin / Owner:
    /// - 4 KPI cards (Total Active, Business Admin, Managers, Staff)
    /// - Live search & role/status filtering
    /// - Table showing user initials avatar, role pill, last login, active status, and action buttons
    /// - Full CRUD (Add User, Edit User, Activate/Deactivate)
    /// </summary>
    public class UserListForm : Form
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnAddUser = null!;

        private TableLayoutPanel kpiTable = null!;
        private Label lblTotalActive = null!;
        private Label lblAdminCount = null!;
        private Label lblManagerCount = null!;
        private Label lblStaffCount = null!;

        private Panel searchFilterPanel = null!;
        private Panel searchPill = null!;
        private TextBox txtSearchBox = null!;
        private ComboBox cmbRoleFilter = null!;
        private ComboBox cmbStatusFilter = null!;

        private Panel tableCardPanel = null!;
        private DataGridView gridUsers = null!;

        private List<SystemUser> usersList = new List<SystemUser>();
        private string activeSearchQuery = string.Empty;
        private string activeRoleFilter = "All Roles";
        private string activeStatusFilter = "All Status";

        public UserListForm()
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
            Name = "UserListForm";
            Padding = Padding.Empty;
            BackColor = UITheme.CreamBackground;
            ResumeLayout(false);
        }

        // =========================================================
        // 1. TOP TOOLBAR (Title, Subtitle, + Add User Button)
        // =========================================================
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
                Text = "User Management",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            lblSubtitle = new Label
            {
                Text = "Manage team members, assign roles, and control access permissions",
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

            btnAddUser = new Button
            {
                Text = "+ Add User",
                Height = 40,
                Width = 135,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White
            };
            btnAddUser.FlatAppearance.BorderSize = 0;
            btnAddUser.ApplyRoundedRegion(10);
            btnAddUser.Click += async (s, e) =>
            {
                using var modal = new UserModal();
                if (modal.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                {
                    await RefreshDataAsync();
                }
            };

            btnStack.Controls.Add(btnAddUser);

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnStack, 1, 0);

            topPanel.Controls.Add(headerTable);
        }

        // =========================================================
        // 2. 4 KPI SUMMARY CARDS
        // =========================================================
        private void BuildKpiCards()
        {
            kpiTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 100,
                ColumnCount = 4,
                RowCount = 1,
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, 0, 16),
                BackColor = Color.Transparent
            };

            for (int i = 0; i < 4; i++)
            {
                kpiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            }
            kpiTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            // Card 1: Total Active
            var cardActive = CreateKpiCard("TOTAL ACTIVE", "4", "Team members active", "\uE77B",
                Color.FromArgb(240, 248, 245), Color.FromArgb(46, 133, 90), out lblTotalActive);

            // Card 2: Business Admin
            var cardAdmin = CreateKpiCard("BUSINESS ADMIN", "1", "Full access", "\uE7EF",
                Color.FromArgb(247, 240, 243), UITheme.PrimaryMauve, out lblAdminCount);

            // Card 3: Managers
            var cardManager = CreateKpiCard("MANAGERS", "1", "Operations & reports", "\uE72A",
                Color.FromArgb(240, 244, 255), Color.FromArgb(41, 98, 178), out lblManagerCount);

            // Card 4: Staff
            var cardStaff = CreateKpiCard("STAFF", "2", "Sales & inquiries", "\uE716",
                Color.FromArgb(242, 245, 238), Color.FromArgb(70, 110, 60), out lblStaffCount);

            kpiTable.Controls.Add(cardActive, 0, 0);
            kpiTable.Controls.Add(cardAdmin, 1, 0);
            kpiTable.Controls.Add(cardManager, 2, 0);
            kpiTable.Controls.Add(cardStaff, 3, 0);
        }

        private Panel CreateKpiCard(
            string label,
            string initialValue,
            string subtitle,
            string iconGlyph,
            Color iconBgColor,
            Color iconColor,
            out Label outValueLabel)
        {
            var card = new Panel
            {
                Dock = DockStyle.Fill,
                Margin = new Padding(4, 0, 4, 0),
                BackColor = Color.White,
                Padding = new Padding(16, 12, 16, 12)
            };

            card.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, card.Width - 1, card.Height - 1), 12);
            };
            card.ApplyRoundedRegion(12);

            var topRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72F));
            topRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28F));
            topRow.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var textStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var lblCardTitle = new Label
            {
                Text = label,
                Font = new Font(UITheme.FontSans, 7.5F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                UseMnemonic = false,
                Margin = new Padding(0, 0, 0, 2)
            };

            var lblVal = new Label
            {
                Text = initialValue,
                Font = new Font(UITheme.FontSerif, 17F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 1)
            };
            outValueLabel = lblVal;

            var lblSub = new Label
            {
                Text = subtitle,
                Font = new Font(UITheme.FontSans, 7.5F, FontStyle.Regular),
                ForeColor = Color.FromArgb(120, 110, 105),
                AutoSize = true,
                UseMnemonic = false,
                Margin = Padding.Empty
            };

            textStack.Controls.Add(lblCardTitle);
            textStack.Controls.Add(lblVal);
            textStack.Controls.Add(lblSub);

            var iconBox = new Panel
            {
                Size = new Size(40, 40),
                BackColor = iconBgColor,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0, 2, 0, 0)
            };
            iconBox.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var font = new Font("Segoe MDL2 Assets", 13f);
                var rect = new Rectangle(0, 0, iconBox.Width, iconBox.Height);
                TextRenderer.DrawText(e.Graphics, iconGlyph, font, rect, iconColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
            };
            iconBox.ApplyRoundedRegion(20);

            topRow.Controls.Add(textStack, 0, 0);
            topRow.Controls.Add(iconBox, 1, 0);

            card.Controls.Add(topRow);
            return card;
        }

        // =========================================================
        // 3. SEARCH & ROLE/STATUS FILTER BAR
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

            // Search pill
            searchPill = new Panel
            {
                Size = new Size(300, 40),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 14, 0)
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
                Location = new Point(42, 11),
                Width = searchPill.Width - 54
            };
            txtSearchBox.TextChanged += async (s, e) =>
            {
                activeSearchQuery = txtSearchBox.Text.Trim();
                await RefreshDataAsync();
            };
            searchPill.Controls.Add(txtSearchBox);
            rowFlow.Controls.Add(searchPill);

            // Role filter dropdown
            var roleFilterPill = new Panel
            {
                Size = new Size(160, 40),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 12, 0)
            };
            roleFilterPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1.2f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, roleFilterPill.Width - 1, roleFilterPill.Height - 1), 12);
            };
            roleFilterPill.ApplyRoundedRegion(12);

            cmbRoleFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(10, 9),
                Width = 140,
                BackColor = Color.White
            };
            cmbRoleFilter.Items.AddRange(new object[] { "All Roles", "Business Admin", "Manager", "Staff" });
            cmbRoleFilter.SelectedIndex = 0;
            cmbRoleFilter.SelectedIndexChanged += async (s, e) =>
            {
                activeRoleFilter = cmbRoleFilter.SelectedItem?.ToString() ?? "All Roles";
                await RefreshDataAsync();
            };
            roleFilterPill.Controls.Add(cmbRoleFilter);
            rowFlow.Controls.Add(roleFilterPill);

            // Status filter dropdown
            var statusFilterPill = new Panel
            {
                Size = new Size(150, 40),
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
                Width = 130,
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

        // =========================================================
        // 4. USERS DATAGRIDVIEW CARD
        // =========================================================
        private void BuildDataGridCard()
        {
            tableCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(2)
            };

            tableCardPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, tableCardPanel.Width - 1, tableCardPanel.Height - 1), 14);
            };
            tableCardPanel.ApplyRoundedRegion(14);

            gridUsers = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(240, 235, 230),
                EnableHeadersVisualStyles = false,
                RowHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = true,
                RowTemplate = { Height = 60 },
                ColumnHeadersHeight = 44,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
            };

            gridUsers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 248, 246);
            gridUsers.DefaultCellStyle.SelectionForeColor = UITheme.TextDark;

            // Custom header styling
            gridUsers.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridUsers.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.TextMuted;
            gridUsers.ColumnHeadersDefaultCellStyle.Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
            gridUsers.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            gridUsers.ColumnHeadersDefaultCellStyle.Padding = new Padding(16, 0, 0, 0);

            // Columns
            var colUser = new DataGridViewTextBoxColumn
            {
                Name = "colUser",
                HeaderText = "USER",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 36F
            };

            var colRole = new DataGridViewTextBoxColumn
            {
                Name = "colRole",
                HeaderText = "ROLE",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 20F
            };

            var colLastLogin = new DataGridViewTextBoxColumn
            {
                Name = "colLastLogin",
                HeaderText = "LAST LOGIN",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 22F
            };

            var colStatus = new DataGridViewTextBoxColumn
            {
                Name = "colStatus",
                HeaderText = "STATUS",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 16F
            };

            var colActions = new DataGridViewTextBoxColumn
            {
                Name = "colActions",
                HeaderText = "ACTIONS",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 18F
            };

            gridUsers.Columns.AddRange(colUser, colRole, colLastLogin, colStatus, colActions);

            gridUsers.CellPainting += GridUsers_CellPainting;
            gridUsers.CellClick += GridUsers_CellClick;

            tableCardPanel.Controls.Add(gridUsers);
        }

        // =========================================================
        // 5. CUSTOM RENDERING (Avatar, Badges, Action Buttons)
        // =========================================================
        private void GridUsers_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var user = gridUsers.Rows[e.RowIndex].Tag as SystemUser;
            if (user == null) return;

            int colIdxUser = gridUsers.Columns["colUser"]?.Index ?? -1;
            int colIdxRole = gridUsers.Columns["colRole"]?.Index ?? -1;
            int colIdxLastLogin = gridUsers.Columns["colLastLogin"]?.Index ?? -1;
            int colIdxStatus = gridUsers.Columns["colStatus"]?.Index ?? -1;
            int colIdxActions = gridUsers.Columns["colActions"]?.Index ?? -1;

            // 1. USER COLUMN: Initials Circle + Full Name + Email
            if (colIdxUser >= 0 && e.ColumnIndex == colIdxUser)
            {
                int avatarSize = 36;
                int avatarLeft = e.CellBounds.Left + 16;
                int avatarTop = e.CellBounds.Top + (e.CellBounds.Height - avatarSize) / 2;

                // Circle avatar background
                Color circleBg = GetAvatarColor(user.FirstName + user.LastName);
                using (var brush = new SolidBrush(circleBg))
                {
                    g.FillEllipse(brush, avatarLeft, avatarTop, avatarSize, avatarSize);
                }

                // Circle initials text
                string initials = GetInitials(user.FirstName, user.LastName);
                using (var fontInitials = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold))
                {
                    var rectAvatar = new Rectangle(avatarLeft, avatarTop, avatarSize, avatarSize);
                    TextRenderer.DrawText(g, initials, fontInitials, rectAvatar, Color.White,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                }

                // Name and email text stack
                int textLeft = avatarLeft + avatarSize + 12;
                int textTop = e.CellBounds.Top + 11;

                using var fontName = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold);
                using var fontEmail = new Font(UITheme.FontSans, 8F, FontStyle.Regular);

                string fullName = $"{user.FirstName} {user.LastName}".Trim();
                var rectName = new Rectangle(textLeft, textTop, e.CellBounds.Width - (textLeft - e.CellBounds.Left) - 8, 18);
                TextRenderer.DrawText(g, fullName, fontName, rectName, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                var rectEmail = new Rectangle(textLeft, textTop + 19, e.CellBounds.Width - (textLeft - e.CellBounds.Left) - 8, 16);
                TextRenderer.DrawText(g, user.Email, fontEmail, rectEmail, UITheme.TextMuted, TextFormatFlags.Left | TextFormatFlags.NoPrefix);

                e.Handled = true;
            }
            // 2. ROLE COLUMN: Badge Pill
            else if (colIdxRole >= 0 && e.ColumnIndex == colIdxRole)
            {
                string roleName = user.Role?.RoleName ?? (user.RoleId == 2 ? "Business Admin" : (user.RoleId == 3 ? "Manager" : "Staff"));

                Color pillBg;
                Color pillText;

                if (roleName.Contains("Admin", StringComparison.OrdinalIgnoreCase))
                {
                    pillBg = Color.FromArgb(247, 235, 239);
                    pillText = UITheme.PrimaryMauve;
                }
                else if (roleName.Equals("Manager", StringComparison.OrdinalIgnoreCase))
                {
                    pillBg = Color.FromArgb(235, 243, 255);
                    pillText = Color.FromArgb(31, 85, 165);
                }
                else
                {
                    pillBg = Color.FromArgb(238, 248, 241);
                    pillText = Color.FromArgb(40, 125, 75);
                }

                DrawBadgePill(g, e.CellBounds, roleName, pillBg, pillText);
                e.Handled = true;
            }
            // 3. LAST LOGIN COLUMN
            else if (colIdxLastLogin >= 0 && e.ColumnIndex == colIdxLastLogin)
            {
                string loginText = user.LastLoginDate.HasValue
                    ? FormatLastLogin(user.LastLoginDate.Value)
                    : "Never logged in";

                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Regular);
                var rect = new Rectangle(e.CellBounds.Left + 16, e.CellBounds.Top, e.CellBounds.Width - 20, e.CellBounds.Height);
                TextRenderer.DrawText(g, loginText, font, rect, UITheme.TextDark, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
            // 4. STATUS COLUMN: Active / Inactive Badge
            else if (colIdxStatus >= 0 && e.ColumnIndex == colIdxStatus)
            {
                string statusText = user.IsActive ? "• Active" : "• Inactive";
                Color pillBg = user.IsActive ? Color.FromArgb(235, 247, 238) : Color.FromArgb(245, 245, 245);
                Color pillText = user.IsActive ? Color.FromArgb(46, 133, 90) : Color.FromArgb(128, 128, 128);

                DrawBadgePill(g, e.CellBounds, statusText, pillBg, pillText);
                e.Handled = true;
            }
            // 5. ACTIONS COLUMN: Edit | Deactivate / Activate buttons
            else if (colIdxActions >= 0 && e.ColumnIndex == colIdxActions)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);

                int yCenter = e.CellBounds.Top + (e.CellBounds.Height - 24) / 2;

                // Edit Button
                var rectEdit = new Rectangle(e.CellBounds.Left + 10, yCenter, 42, 24);
                TextRenderer.DrawText(g, "Edit", font, rectEdit, UITheme.PrimaryMauve, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                // Divider dot / slash
                var rectSlash = new Rectangle(e.CellBounds.Left + 48, yCenter, 12, 24);
                TextRenderer.DrawText(g, "·", font, rectSlash, Color.FromArgb(180, 170, 165), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);

                // Deactivate / Activate Button
                string toggleText = user.IsActive ? "Deactivate" : "Activate";
                Color toggleColor = user.IsActive ? Color.FromArgb(180, 60, 60) : Color.FromArgb(46, 133, 90);
                var rectToggle = new Rectangle(e.CellBounds.Left + 62, yCenter, 80, 24);
                TextRenderer.DrawText(g, toggleText, font, rectToggle, toggleColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);

                e.Handled = true;
            }
        }

        private static void DrawBadgePill(Graphics g, Rectangle bounds, string text, Color bgColor, Color textColor)
        {
            using var font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold);
            var size = TextRenderer.MeasureText(text, font);
            int pillWidth = size.Width + 16;
            int pillHeight = 24;
            int pillX = bounds.Left + 16;
            int pillY = bounds.Top + (bounds.Height - pillHeight) / 2;

            var pillRect = new Rectangle(pillX, pillY, pillWidth, pillHeight);
            using (var brush = new SolidBrush(bgColor))
            {
                g.FillRoundedRectangle(brush, pillRect, 8);
            }

            TextRenderer.DrawText(g, text, font, pillRect, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }

        private static string FormatLastLogin(DateTime dt)
        {
            if (dt.Date == DateTime.Today)
            {
                return $"Today, {dt:h:mm tt}";
            }
            if (dt.Date == DateTime.Today.AddDays(-1))
            {
                return $"Yesterday, {dt:h:mm tt}";
            }
            return dt.ToString("MMM d, yyyy");
        }

        private static string GetInitials(string first, string last)
        {
            string f = !string.IsNullOrWhiteSpace(first) ? first.Substring(0, 1).ToUpper() : string.Empty;
            string l = !string.IsNullOrWhiteSpace(last) ? last.Substring(0, 1).ToUpper() : string.Empty;
            return $"{f}{l}";
        }

        private static Color GetAvatarColor(string seed)
        {
            int hash = Math.Abs(seed.GetHashCode());
            Color[] colors = new[]
            {
                Color.FromArgb(140, 70, 90),   // Mauve/Maroon
                Color.FromArgb(52, 115, 185),  // Blue
                Color.FromArgb(46, 133, 90),   // Green
                Color.FromArgb(190, 110, 50),  // Amber
                Color.FromArgb(105, 80, 160)   // Purple
            };
            return colors[hash % colors.Length];
        }

        // =========================================================
        // 6. SINGLE-CLICK ACTION HANDLING
        // =========================================================
        private async void GridUsers_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var user = gridUsers.Rows[e.RowIndex].Tag as SystemUser;
            if (user == null) return;

            // Check if clicking in Actions column
            if (gridUsers.Columns["colActions"] is { } colActions && e.ColumnIndex == colActions.Index)
            {
                var cellBounds = gridUsers.GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                var mousePos = gridUsers.PointToClient(Cursor.Position);
                int relX = mousePos.X - cellBounds.Left;

                // Edit is clicked (relX < 50)
                if (relX < 50)
                {
                    using var modal = new UserModal(user);
                    if (modal.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                    {
                        await RefreshDataAsync();
                    }
                }
                // Toggle status is clicked (relX >= 50)
                else
                {
                    await PromptToggleUserStatusAsync(user);
                }
            }
        }

        private async Task PromptToggleUserStatusAsync(SystemUser user)
        {
            string action = user.IsActive ? "deactivate" : "activate";
            var res = MessageBox.Show(
                $"Are you sure you want to {action} {user.FirstName} {user.LastName}?\n\n" +
                (user.IsActive
                    ? "They will no longer be able to log in to the system."
                    : "They will immediately be able to log in again."),
                $"Confirm {char.ToUpper(action[0]) + action.Substring(1)}",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (res == DialogResult.Yes)
            {
                await CrmDataService.ToggleUserStatusAsync(user.UserId, !user.IsActive);
                await RefreshDataAsync();
            }
        }

        // =========================================================
        // 7. DATA REFRESH & DATABASE SYNC
        // =========================================================
        public async Task RefreshDataAsync()
        {
            try
            {
                // Query users from database
                usersList = await CrmDataService.GetUsersAsync(
                    searchQuery: activeSearchQuery,
                    roleFilter: activeRoleFilter,
                    statusFilter: activeStatusFilter
                );

                // Update Grid
                gridUsers.Rows.Clear();
                foreach (var u in usersList)
                {
                    int rowIdx = gridUsers.Rows.Add();
                    gridUsers.Rows[rowIdx].Tag = u;
                }

                // Update Subtitle count
                lblSubtitle.Text = $"{usersList.Count} team member(s) listed \u00B7 {activeRoleFilter}";

                // Update KPI Cards
                var metrics = await CrmDataService.GetUserSummaryMetricsAsync();
                lblTotalActive.Text = metrics.TotalActive.ToString();
                lblAdminCount.Text = metrics.AdminCount.ToString();
                lblManagerCount.Text = metrics.ManagerCount.ToString();
                lblStaffCount.Text = metrics.StaffCount.ToString();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[UserListForm.RefreshDataAsync] {ex.Message}");
            }
        }
    }
}
