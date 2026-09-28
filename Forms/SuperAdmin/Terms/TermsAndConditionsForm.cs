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

namespace CC.Forms.SuperAdmin.Terms
{
    public class TermsAndConditionsForm : Form
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnNewVersion = null!;

        private Panel tabBarPanel = null!;
        private Button btnTabEditor = null!;
        private Button btnTabAcceptances = null!;
        private int _currentTab = 0;

        private Panel contentCardPanel = null!;
        private Panel editorContainer = null!;
        private Panel acceptancesContainer = null!;

        // Editor Controls
        private ListBox lstVersions = null!;
        private TextBox txtVersion = null!;
        private DateTimePicker dtEffective = null!;
        private TextBox txtContent = null!;
        private Button btnSaveTerms = null!;
        private Label lblEditorStatus = null!;
        private TermsAndConditions? _selectedTerms;

        // Acceptance Controls
        private DataGridView gridAcceptances = null!;
        private ComboBox cmbVersionFilter = null!;

        private List<TermsAndConditions> _versionsList = new();
        private List<TermsAcceptanceItem> _acceptancesList = new();

        public TermsAndConditionsForm()
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
            Name = "TermsAndConditionsForm";
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
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

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
                Text = "Terms & Conditions",
                UseMnemonic = false,
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            lblSubtitle = new Label
            {
                Text = "Manage platform terms of service, track versions, and monitor user acceptances",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            btnNewVersion = new Button
            {
                Text = "+ New Terms Version",
                Size = new Size(175, 40),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Right | AnchorStyles.Top,
                Margin = new Padding(0, 6, 0, 0)
            };
            btnNewVersion.FlatAppearance.BorderSize = 0;
            btnNewVersion.ApplyRoundedRegion(8);
            btnNewVersion.Click += (s, e) => PrepareNewVersion();

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnNewVersion, 1, 0);
            topPanel.Controls.Add(headerTable);
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

            btnTabEditor = new Button
            {
                Text = "Terms Versions & Editor",
                Size = new Size(190, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            btnTabEditor.FlatAppearance.BorderSize = 0;
            btnTabEditor.ApplyRoundedRegion(8);
            btnTabEditor.Click += (s, e) => SwitchTab(0);

            btnTabAcceptances = new Button
            {
                Text = "User Acceptance Records",
                Size = new Size(190, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = Color.FromArgb(242, 238, 233),
                ForeColor = UITheme.TextDark,
                Cursor = Cursors.Hand,
                Margin = Padding.Empty
            };
            btnTabAcceptances.FlatAppearance.BorderSize = 0;
            btnTabAcceptances.ApplyRoundedRegion(8);
            btnTabAcceptances.Click += (s, e) => SwitchTab(1);

            flow.Controls.Add(btnTabEditor);
            flow.Controls.Add(btnTabAcceptances);
            tabBarPanel.Controls.Add(flow);
        }

        private void SwitchTab(int tab)
        {
            _currentTab = tab;
            if (_currentTab == 0)
            {
                btnTabEditor.BackColor = UITheme.PrimaryMauve;
                btnTabEditor.ForeColor = Color.White;
                btnTabAcceptances.BackColor = Color.FromArgb(242, 238, 233);
                btnTabAcceptances.ForeColor = UITheme.TextDark;

                editorContainer.Visible = true;
                acceptancesContainer.Visible = false;
                btnNewVersion.Visible = true;
            }
            else
            {
                btnTabEditor.BackColor = Color.FromArgb(242, 238, 233);
                btnTabEditor.ForeColor = UITheme.TextDark;
                btnTabAcceptances.BackColor = UITheme.PrimaryMauve;
                btnTabAcceptances.ForeColor = Color.White;

                editorContainer.Visible = false;
                acceptancesContainer.Visible = true;
                btnNewVersion.Visible = false;
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

            BuildEditorContainer();
            BuildAcceptancesContainer();

            contentCardPanel.Controls.Add(acceptancesContainer);
            contentCardPanel.Controls.Add(editorContainer);
        }

        private void BuildEditorContainer()
        {
            editorContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };

            var split = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.White
            };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 280F));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));

            // Left panel: versions list
            var leftPnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 0, 16, 0) };
            leftPnl.Controls.Add(new Label { Text = "VERSION HISTORY", Dock = DockStyle.Top, Height = 26, Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            lstVersions = new ListBox
            {
                Dock = DockStyle.Fill,
                Font = new Font(UITheme.FontSans, 10F),
                BorderStyle = BorderStyle.FixedSingle,
                ItemHeight = 32
            };
            lstVersions.SelectedIndexChanged += (s, e) => LoadSelectedVersion();
            leftPnl.Controls.Add(lstVersions);

            // Right panel: editor
            var rightPnl = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16, 0, 0, 0) };

            var metaRow = new TableLayoutPanel { Dock = DockStyle.Top, Height = 56, ColumnCount = 2, RowCount = 1 };
            metaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            metaRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            var pnlV = new Panel { Dock = DockStyle.Fill };
            pnlV.Controls.Add(new Label { Text = "VERSION NUMBER", Dock = DockStyle.Top, Height = 18, Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            txtVersion = new TextBox { Dock = DockStyle.Top, Font = new Font(UITheme.FontSans, 10F) };
            pnlV.Controls.Add(txtVersion);

            var pnlD = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 0, 0) };
            pnlD.Controls.Add(new Label { Text = "EFFECTIVE DATE", Dock = DockStyle.Top, Height = 18, Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            dtEffective = new DateTimePicker { Dock = DockStyle.Top, Font = new Font(UITheme.FontSans, 10F), Format = DateTimePickerFormat.Short };
            pnlD.Controls.Add(dtEffective);

            metaRow.Controls.Add(pnlV, 0, 0);
            metaRow.Controls.Add(pnlD, 1, 0);

            var pnlContent = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 10) };
            pnlContent.Controls.Add(new Label { Text = "TERMS CONTENT (MARKDOWN / TEXT)", Dock = DockStyle.Top, Height = 20, Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold), ForeColor = UITheme.TextMuted });
            txtContent = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Consolas", 9.5F),
                BackColor = Color.FromArgb(250, 248, 246)
            };
            pnlContent.Controls.Add(txtContent);

            var botRow = new Panel { Dock = DockStyle.Bottom, Height = 50, Padding = new Padding(0, 10, 0, 0) };
            lblEditorStatus = new Label { Dock = DockStyle.Left, AutoSize = true, Font = new Font(UITheme.FontSans, 8.5F), ForeColor = Color.FromArgb(46, 133, 90), Padding = new Padding(0, 10, 0, 0) };
            btnSaveTerms = new Button
            {
                Text = "Save Terms Version",
                Dock = DockStyle.Right,
                Width = 170,
                BackColor = UITheme.PrimaryMauve,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSaveTerms.FlatAppearance.BorderSize = 0;
            btnSaveTerms.ApplyRoundedRegion(8);
            btnSaveTerms.Click += async (s, e) => await SaveTermsAsync();

            botRow.Controls.Add(lblEditorStatus);
            botRow.Controls.Add(btnSaveTerms);

            rightPnl.Controls.Add(pnlContent);
            rightPnl.Controls.Add(metaRow);
            rightPnl.Controls.Add(botRow);

            split.Controls.Add(leftPnl, 0, 0);
            split.Controls.Add(rightPnl, 1, 0);
            editorContainer.Controls.Add(split);
        }

        private void BuildAcceptancesContainer()
        {
            acceptancesContainer = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Visible = false
            };

            var filterBar = new Panel { Dock = DockStyle.Top, Height = 44 };
            filterBar.Controls.Add(new Label { Text = "Filter by Version:", Location = new Point(0, 12), AutoSize = true, Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold) });
            cmbVersionFilter = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font(UITheme.FontSans, 9.5F),
                Location = new Point(130, 8),
                Width = 160
            };
            cmbVersionFilter.SelectedIndexChanged += async (s, e) => await RefreshAcceptancesAsync();
            filterBar.Controls.Add(cmbVersionFilter);

            gridAcceptances = new DataGridView
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
            gridAcceptances.DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 248, 246);
            gridAcceptances.DefaultCellStyle.SelectionForeColor = UITheme.TextDark;
            gridAcceptances.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            gridAcceptances.ColumnHeadersDefaultCellStyle.ForeColor = UITheme.TextMuted;
            gridAcceptances.ColumnHeadersDefaultCellStyle.Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold);

            gridAcceptances.Columns.Add(new DataGridViewTextBoxColumn { Name = "colUser", HeaderText = "USER NAME", FillWeight = 30F });
            gridAcceptances.Columns.Add(new DataGridViewTextBoxColumn { Name = "colEmail", HeaderText = "EMAIL ADDRESS", FillWeight = 30F });
            gridAcceptances.Columns.Add(new DataGridViewTextBoxColumn { Name = "colVer", HeaderText = "VERSION ACCEPTED", FillWeight = 20F });
            gridAcceptances.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "ACCEPTED TIMESTAMP", FillWeight = 20F });

            acceptancesContainer.Controls.Add(gridAcceptances);
            acceptancesContainer.Controls.Add(filterBar);
        }

        private void PrepareNewVersion()
        {
            SwitchTab(0);
            _selectedTerms = null;
            string nextVer = $"v{_versionsList.Count + 1}.0";
            txtVersion.Text = nextVer;
            dtEffective.Value = DateTime.Today;
            txtContent.Text = "### Terms & Conditions\n\nContent for new version...";
            lblEditorStatus.Text = "Creating new version";
            lblEditorStatus.ForeColor = UITheme.PrimaryMauve;
            txtVersion.Focus();
        }

        private void LoadSelectedVersion()
        {
            if (lstVersions.SelectedIndex < 0 || lstVersions.SelectedIndex >= _versionsList.Count) return;
            _selectedTerms = _versionsList[lstVersions.SelectedIndex];

            txtVersion.Text = _selectedTerms.Version;
            dtEffective.Value = _selectedTerms.EffectiveDate;
            txtContent.Text = _selectedTerms.Content;
            lblEditorStatus.Text = $"Editing existing version #{_selectedTerms.TermsId}";
            lblEditorStatus.ForeColor = Color.FromArgb(46, 133, 90);
        }

        private async Task SaveTermsAsync()
        {
            string ver = txtVersion.Text.Trim();
            string content = txtContent.Text.Trim();
            if (string.IsNullOrWhiteSpace(ver) || string.IsNullOrWhiteSpace(content))
            {
                MessageBox.Show("Please enter both version and content.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnSaveTerms.Enabled = false;
            try
            {
                if (_selectedTerms != null)
                {
                    await CrmDataService.UpdateTermsVersionAsync(_selectedTerms.TermsId, ver, content, dtEffective.Value);
                    lblEditorStatus.Text = "Version updated successfully!";
                }
                else
                {
                    await CrmDataService.CreateTermsVersionAsync(ver, content, dtEffective.Value);
                    lblEditorStatus.Text = "New version created successfully!";
                }

                await RefreshDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnSaveTerms.Enabled = true;
            }
        }

        public async Task RefreshDataAsync()
        {
            try
            {
                _versionsList = await CrmDataService.GetTermsVersionsAsync();
                lstVersions.Items.Clear();
                foreach (var v in _versionsList)
                {
                    lstVersions.Items.Add($"{v.Version} · Eff: {v.EffectiveDate:yyyy-MM-dd} ({v.Acceptances?.Count ?? 0} acceptances)");
                }

                if (_versionsList.Count > 0 && lstVersions.SelectedIndex < 0)
                {
                    lstVersions.SelectedIndex = 0;
                }

                // Populate Version Filter
                cmbVersionFilter.Items.Clear();
                cmbVersionFilter.Items.Add("All Versions");
                foreach (var v in _versionsList) cmbVersionFilter.Items.Add(v.Version);
                cmbVersionFilter.SelectedIndex = 0;

                await RefreshAcceptancesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TermsAndConditionsForm.RefreshDataAsync] {ex.Message}");
            }
        }

        private async Task RefreshAcceptancesAsync()
        {
            try
            {
                int? filterTermsId = null;
                if (cmbVersionFilter.SelectedIndex > 0 && cmbVersionFilter.SelectedIndex - 1 < _versionsList.Count)
                {
                    filterTermsId = _versionsList[cmbVersionFilter.SelectedIndex - 1].TermsId;
                }

                _acceptancesList = await CrmDataService.GetTermsAcceptancesAsync(filterTermsId);
                gridAcceptances.Rows.Clear();
                foreach (var a in _acceptancesList)
                {
                    int row = gridAcceptances.Rows.Add();
                    gridAcceptances.Rows[row].Cells[0].Value = a.UserName;
                    gridAcceptances.Rows[row].Cells[1].Value = a.Email;
                    gridAcceptances.Rows[row].Cells[2].Value = a.Version;
                    gridAcceptances.Rows[row].Cells[3].Value = a.AcceptedDate.ToString("yyyy-MM-dd HH:mm:ss");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshAcceptancesAsync] {ex.Message}");
            }
        }
    }
}
