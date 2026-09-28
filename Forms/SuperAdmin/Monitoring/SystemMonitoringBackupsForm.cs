using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.SuperAdmin.Monitoring
{
    public class SystemMonitoringBackupsForm : Form
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;

        private Panel tabBarPanel = null!;
        private Button btnTabLogs = null!;
        private Button btnTabBackups = null!;
        private int _currentTab = 0;

        private Panel contentCardPanel = null!;
        private Panel logsContainer = null!;
        private Panel backupsContainer = null!;

        // Logs Controls
        private TextBox txtLogSearch = null!;
        private ComboBox cmbActionFilter = null!;
        private DataGridView gridLogs = null!;

        // Backup Controls
        private ComboBox cmbDatabaseSelector = null!;
        private Button btnBackupSelected = null!;
        private Button btnBackupAll = null!;
        private Button btnOpenFolder = null!;
        private Label lblBackupStatus = null!;
        private DataGridView gridBackups = null!;
        private FlowLayoutPanel pnlTenantBadges = null!;

        private List<AuditLogItem> _logsList = new();
        private List<BackupRecord> _backupList = new();
        private List<string> _availableDbs = new();

        public SystemMonitoringBackupsForm()
        {
            InitializeComponent();
            BuildTopToolbar();
            BuildTabBar();
            BuildContentCard();

            Controls.Add(contentCardPanel);
            Controls.Add(tabBarPanel);
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
            ClientSize = new Size(1140, 800);
            Font = new Font(UITheme.FontSans, 9.5F);
            FormBorderStyle = FormBorderStyle.None;
            Name = "SystemMonitoringBackupsForm";
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
                Text = "System Monitoring & Backups",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            lblSubtitle = new Label
            {
                Text = "Platform audit trails, security event tracking, and native multi-database backups across Master & Tenant databases",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);
            topPanel.Controls.Add(titleStack);
        }

        private void BuildTabBar()
        {
            tabBarPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 44,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 0, 0, 8),
                Padding = new Padding(6, 0, 6, 4)
            };

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            btnTabLogs = new Button
            {
                Text = "System Audit & Activity Logs",
                Size = new Size(220, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnTabLogs.FlatAppearance.BorderSize = 0;
            btnTabLogs.ApplyRoundedRegion(8);
            btnTabLogs.Click += (s, e) => SwitchTab(0);

            btnTabBackups = new Button
            {
                Text = "Database Backups (Master + Tenants)",
                Size = new Size(260, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(242, 238, 233),
                ForeColor = UITheme.TextDark,
                Cursor = Cursors.Hand,
                Margin = Padding.Empty
            };
            btnTabBackups.FlatAppearance.BorderSize = 0;
            btnTabBackups.ApplyRoundedRegion(8);
            btnTabBackups.Click += (s, e) => SwitchTab(1);

            flow.Controls.Add(btnTabLogs);
            flow.Controls.Add(btnTabBackups);
            tabBarPanel.Controls.Add(flow);
        }

        private void SwitchTab(int tab)
        {
            _currentTab = tab;
            if (_currentTab == 0)
            {
                btnTabLogs.BackColor = UITheme.PrimaryMauve;
                btnTabLogs.ForeColor = Color.White;
                btnTabBackups.BackColor = Color.FromArgb(242, 238, 233);
                btnTabBackups.ForeColor = UITheme.TextDark;

                logsContainer.Visible = true;
                backupsContainer.Visible = false;
            }
            else
            {
                btnTabLogs.BackColor = Color.FromArgb(242, 238, 233);
                btnTabLogs.ForeColor = UITheme.TextDark;
                btnTabBackups.BackColor = UITheme.PrimaryMauve;
                btnTabBackups.ForeColor = Color.White;

                logsContainer.Visible = false;
                backupsContainer.Visible = true;
            }
        }

        private void BuildContentCard()
        {
            contentCardPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(16),
                Margin = new Padding(6, 0, 6, 12)
            };

            contentCardPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, contentCardPanel.Width - 1, contentCardPanel.Height - 1), 14);
            };
            contentCardPanel.ApplyRoundedRegion(14);

            BuildLogsContainer();
            BuildBackupsContainer();

            contentCardPanel.Controls.Add(backupsContainer);
            contentCardPanel.Controls.Add(logsContainer);
        }

        // =========================================================
        // TAB 1: SYSTEM AUDIT & ACTIVITY LOGS
        // =========================================================

        private void BuildLogsContainer()
        {
            logsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            var filterBar = new Panel { Dock = DockStyle.Top, Height = 48 };

            var searchPill = new Panel
            {
                Size = new Size(320, 38),
                BackColor = Color.FromArgb(250, 248, 246),
                Location = new Point(0, 4)
            };
            searchPill.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, searchPill.Width - 1, searchPill.Height - 1), 8);
                using var font = new Font("Segoe MDL2 Assets", 10F);
                TextRenderer.DrawText(e.Graphics, "\uE721", font, new Rectangle(10, 0, 20, searchPill.Height), UITheme.TextMuted, TextFormatFlags.VerticalCenter);
            };
            searchPill.ApplyRoundedRegion(8);

            txtLogSearch = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(250, 248, 246),
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(34, 10),
                Width = 276,
                PlaceholderText = "Search by event, description, user..."
            };
            txtLogSearch.TextChanged += async (s, e) => await RefreshLogsAsync();
            searchPill.Controls.Add(txtLogSearch);

            cmbActionFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(335, 8),
                Width = 220
            };
            cmbActionFilter.Items.AddRange(new object[]
            {
                "All Actions",
                "LOGIN_SUCCESS",
                "LOGIN_FAILED",
                "USER_CREATED",
                "USER_UPDATED",
                "USER_ACTIVATED",
                "USER_DEACTIVATED",
                "SUBSCRIPTION_PLAN_CREATED",
                "SUBSCRIPTION_PLAN_UPDATED",
                "SUBSCRIPTION_ASSIGNED",
                "SUBSCRIPTION_RENEWED",
                "DATABASE_BACKUP_COMPLETED",
                "DATABASE_BACKUP_FAILED",
                "TERMS_VERSION_CREATED",
                "TERMS_VERSION_UPDATED"
            });
            cmbActionFilter.SelectedIndex = 0;
            cmbActionFilter.SelectedIndexChanged += async (s, e) => await RefreshLogsAsync();

            filterBar.Controls.Add(searchPill);
            filterBar.Controls.Add(cmbActionFilter);

            gridLogs = new DataGridView
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
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowTemplate = { Height = 44 },
                ColumnHeadersHeight = 38,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            gridLogs.DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 248, 246);
            gridLogs.DefaultCellStyle.SelectionForeColor = UITheme.TextDark;
            gridLogs.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridLogs.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.TextMuted;
            gridLogs.ColumnHeadersDefaultCellStyle.Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);

            gridLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTime", HeaderText = "TIMESTAMP", FillWeight = 16F });
            gridLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "ACTION EVENT", FillWeight = 22F });
            gridLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDesc", HeaderText = "DESCRIPTION", FillWeight = 38F });
            gridLogs.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUser", HeaderText = "ACTOR / USER", FillWeight = 24F });

            gridLogs.CellPainting += GridLogs_CellPainting;

            logsContainer.Controls.Add(gridLogs);
            logsContainer.Controls.Add(filterBar);
        }

        private void GridLogs_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;
            e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var log = gridLogs.Rows[e.RowIndex].Tag as AuditLogItem;
            if (log == null) return;

            int colType = gridLogs.Columns["colType"].Index;
            if (e.ColumnIndex == colType)
            {
                Color bg;
                Color fg;

                if (log.ActionType.Contains("FAILED") || log.ActionType.Contains("DEACTIVATED") || log.ActionType.Contains("BLOCKED"))
                {
                    bg = Color.FromArgb(253, 237, 237);
                    fg = Color.FromArgb(190, 60, 60);
                }
                else if (log.ActionType.Contains("BACKUP") || log.ActionType.Contains("TERMS"))
                {
                    bg = Color.FromArgb(235, 243, 255);
                    fg = Color.FromArgb(31, 85, 165);
                }
                else if (log.ActionType.Contains("SUCCESS") || log.ActionType.Contains("ACTIVATED") || log.ActionType.Contains("CREATED") || log.ActionType.Contains("RENEWED"))
                {
                    bg = Color.FromArgb(235, 247, 238);
                    fg = Color.FromArgb(46, 133, 90);
                }
                else
                {
                    bg = Color.FromArgb(246, 243, 239);
                    fg = Color.FromArgb(100, 90, 85);
                }

                var pillRect = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top + (e.CellBounds.Height - 24) / 2, e.CellBounds.Width - 16, 24);
                using var b = new SolidBrush(bg);
                g.FillRoundedRectangle(b, pillRect, 6);

                using var font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
                TextRenderer.DrawText(g, log.ActionType, font, pillRect, fg, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                e.Handled = true;
            }
        }

        // =========================================================
        // TAB 2: MULTI-TENANT DATABASE BACKUPS
        // =========================================================

        private void BuildBackupsContainer()
        {
            backupsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = false
            };

            // Architecture Banner Card
            var banner = new Panel
            {
                Dock = DockStyle.Top,
                Height = 115,
                BackColor = Color.FromArgb(250, 248, 246),
                Padding = new Padding(16, 12, 16, 12),
                Margin = new Padding(0, 0, 0, 16)
            };
            banner.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(UITheme.BorderColor, 1f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, banner.Width - 1, banner.Height - 1), 10);
            };
            banner.ApplyRoundedRegion(10);

            var lblArchTitle = new Label
            {
                Text = "DATABASE-PER-TENANT MULTI-DATABASE BACKUP SCOPE",
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = UITheme.TextMuted,
                Dock = DockStyle.Top,
                Height = 18
            };

            var lblArchDesc = new Label
            {
                Text = "Backups execute native SQL Server BACKUP DATABASE commands generating real .bak archives on disk. The system targets both the platform Master DB (MSME_MasterCRM) and all operational tenant databases.",
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextDark,
                Dock = DockStyle.Top,
                Height = 36
            };

            pnlTenantBadges = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 32,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent
            };

            banner.Controls.Add(pnlTenantBadges);
            banner.Controls.Add(lblArchDesc);
            banner.Controls.Add(lblArchTitle);

            // Action Toolbar
            var actionPanel = new Panel { Dock = DockStyle.Top, Height = 56, Padding = new Padding(0, 10, 0, 6) };

            cmbDatabaseSelector = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(0, 8),
                Width = 220
            };

            btnBackupSelected = new Button
            {
                Text = "Backup Database",
                Location = new Point(230, 6),
                Size = new Size(150, 36),
                BackColor = Color.FromArgb(240, 235, 230),
                ForeColor = UITheme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBackupSelected.FlatAppearance.BorderSize = 0;
            btnBackupSelected.ApplyRoundedRegion(8);
            btnBackupSelected.Click += async (s, e) => await PerformSelectedBackupAsync();

            btnBackupAll = new Button
            {
                Text = "⚡ Backup All Databases (Master + All Tenants)",
                Location = new Point(390, 6),
                Size = new Size(330, 36),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnBackupAll.FlatAppearance.BorderSize = 0;
            btnBackupAll.ApplyRoundedRegion(8);
            btnBackupAll.Click += async (s, e) => await PerformAllBackupsAsync();

            btnOpenFolder = new Button
            {
                Text = "📁 Open Folder",
                Location = new Point(730, 6),
                Size = new Size(120, 36),
                BackColor = Color.FromArgb(240, 235, 230),
                ForeColor = UITheme.TextDark,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnOpenFolder.FlatAppearance.BorderSize = 0;
            btnOpenFolder.ApplyRoundedRegion(8);
            btnOpenFolder.Click += (s, e) =>
            {
                string dir = BackupService.GetBackupDirectory();
                try { Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true }); } catch { }
            };

            lblBackupStatus = new Label
            {
                Location = new Point(860, 14),
                AutoSize = true,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(46, 133, 90)
            };

            actionPanel.Controls.Add(cmbDatabaseSelector);
            actionPanel.Controls.Add(btnBackupSelected);
            actionPanel.Controls.Add(btnBackupAll);
            actionPanel.Controls.Add(btnOpenFolder);
            actionPanel.Controls.Add(lblBackupStatus);

            // Backups Grid
            gridBackups = new DataGridView
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
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowTemplate = { Height = 44 },
                ColumnHeadersHeight = 38,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            gridBackups.DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 248, 246);
            gridBackups.DefaultCellStyle.SelectionForeColor = UITheme.TextDark;
            gridBackups.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridBackups.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.TextMuted;
            gridBackups.ColumnHeadersDefaultCellStyle.Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);

            gridBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "colFile", HeaderText = "BACKUP FILE NAME", FillWeight = 36F });
            gridBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDb", HeaderText = "DATABASE", FillWeight = 20F });
            gridBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "colSize", HeaderText = "FILE SIZE", FillWeight = 16F });
            gridBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "colTime", HeaderText = "TIMESTAMP (UTC)", FillWeight = 20F });
            gridBackups.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS", FillWeight = 14F });

            gridBackups.CellPainting += (s, e) =>
            {
                if (e.RowIndex < 0 || e.Graphics == null) return;
                e.PaintBackground(e.ClipBounds, (e.State & DataGridViewElementStates.Selected) != 0);

                if (e.ColumnIndex == gridBackups.Columns["colStatus"].Index)
                {
                    var pillRect = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top + (e.CellBounds.Height - 24) / 2, 72, 24);
                    using var b = new SolidBrush(Color.FromArgb(235, 247, 238));
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    e.Graphics.FillRoundedRectangle(b, pillRect, 6);

                    using var font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);
                    TextRenderer.DrawText(e.Graphics, "Success", font, pillRect, Color.FromArgb(46, 133, 90), TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    e.Handled = true;
                }
            };

            backupsContainer.Controls.Add(gridBackups);
            backupsContainer.Controls.Add(actionPanel);
            backupsContainer.Controls.Add(banner);
        }

        private async Task PerformSelectedBackupAsync()
        {
            if (cmbDatabaseSelector.SelectedIndex < 0 || cmbDatabaseSelector.SelectedIndex >= _availableDbs.Count) return;
            string db = _availableDbs[cmbDatabaseSelector.SelectedIndex];

            btnBackupSelected.Enabled = false;
            btnBackupAll.Enabled = false;
            lblBackupStatus.Text = $"Backing up '{db}'...";
            lblBackupStatus.ForeColor = UITheme.PrimaryMauve;

            try
            {
                var rec = await BackupService.BackupDatabaseAsync(db);
                if (rec.IsSuccess)
                {
                    lblBackupStatus.Text = $"✓ Backed up {db} ({rec.FormattedSize})";
                    lblBackupStatus.ForeColor = Color.FromArgb(46, 133, 90);
                }
                else
                {
                    lblBackupStatus.Text = $"✗ Error: {rec.ErrorMessage}";
                    lblBackupStatus.ForeColor = Color.FromArgb(190, 60, 60);
                }
                await RefreshBackupsAsync();
            }
            finally
            {
                btnBackupSelected.Enabled = true;
                btnBackupAll.Enabled = true;
            }
        }

        private async Task PerformAllBackupsAsync()
        {
            btnBackupSelected.Enabled = false;
            btnBackupAll.Enabled = false;
            lblBackupStatus.Text = "Backing up all databases...";
            lblBackupStatus.ForeColor = UITheme.PrimaryMauve;

            try
            {
                var list = await BackupService.BackupAllDatabasesAsync();
                int successCount = list.Count(r => r.IsSuccess);
                lblBackupStatus.Text = $"✓ {successCount}/{list.Count} databases backed up successfully!";
                lblBackupStatus.ForeColor = Color.FromArgb(46, 133, 90);
                await RefreshBackupsAsync();
            }
            finally
            {
                btnBackupSelected.Enabled = true;
                btnBackupAll.Enabled = true;
            }
        }

        public async Task RefreshDataAsync()
        {
            await RefreshLogsAsync();
            await RefreshBackupsAsync();
        }

        private async Task RefreshLogsAsync()
        {
            try
            {
                string q = txtLogSearch?.Text ?? "";
                string act = cmbActionFilter?.SelectedItem?.ToString() ?? "All Actions";

                _logsList = await CrmDataService.GetSystemAuditLogsAsync(searchQuery: q, actionFilter: act);
                gridLogs.Rows.Clear();
                foreach (var l in _logsList)
                {
                    int row = gridLogs.Rows.Add();
                    gridLogs.Rows[row].Tag = l;
                    gridLogs.Rows[row].Cells[0].Value = l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    gridLogs.Rows[row].Cells[1].Value = l.ActionType;
                    gridLogs.Rows[row].Cells[2].Value = l.Description;
                    gridLogs.Rows[row].Cells[3].Value = $"{l.UserName} ({l.UserEmail})";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshLogsAsync] {ex.Message}");
            }
        }

        private async Task RefreshBackupsAsync()
        {
            try
            {
                _availableDbs = await BackupService.GetRegisteredDatabasesAsync();

                cmbDatabaseSelector.Items.Clear();
                foreach (var db in _availableDbs) cmbDatabaseSelector.Items.Add(db);
                if (_availableDbs.Count > 0 && cmbDatabaseSelector.SelectedIndex < 0)
                {
                    cmbDatabaseSelector.SelectedIndex = 0;
                }

                // Update Badges
                pnlTenantBadges.Controls.Clear();
                foreach (var db in _availableDbs)
                {
                    bool isMaster = db == "MSME_MasterCRM";
                    var badge = new Label
                    {
                        Text = isMaster ? "🏛 Master: " + db : "🏢 Tenant: " + db,
                        Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                        ForeColor = isMaster ? UITheme.PrimaryMauve : Color.FromArgb(46, 133, 90),
                        BackColor = isMaster ? Color.FromArgb(247, 235, 239) : Color.FromArgb(235, 247, 238),
                        Padding = new Padding(8, 4, 8, 4),
                        Margin = new Padding(0, 0, 8, 0),
                        AutoSize = true
                    };
                    pnlTenantBadges.Controls.Add(badge);
                }

                _backupList = BackupService.GetBackupHistory();
                gridBackups.Rows.Clear();
                foreach (var b in _backupList)
                {
                    int row = gridBackups.Rows.Add();
                    gridBackups.Rows[row].Tag = b;
                    gridBackups.Rows[row].Cells[0].Value = b.FileName;
                    gridBackups.Rows[row].Cells[1].Value = b.DatabaseName;
                    gridBackups.Rows[row].Cells[2].Value = b.FormattedSize;
                    gridBackups.Rows[row].Cells[3].Value = b.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");
                    gridBackups.Rows[row].Cells[4].Value = "Success";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshBackupsAsync] {ex.Message}");
            }
        }
    }
}
