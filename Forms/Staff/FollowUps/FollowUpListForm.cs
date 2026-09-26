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

namespace CC.Forms.Staff.FollowUps
{
    /// <summary>
    /// Follow-ups List Module Form implementing the List Panel Pattern specified in target mockup (media_1789365009186.png).
    /// </summary>
    public class FollowUpListForm : Form, ISearchable
    {
        private Panel topPanel = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Button btnNewFollowUp = null!;

        private Panel searchWrapperPanel = null!;
        private Panel searchPill = null!;
        private TextBox txtSearchBox = null!;
        private FlowLayoutPanel filterPillContainer = null!;

        private Panel tableCardPanel = null!;
        private DataGridView gridFollowUps = null!;
        private string activeFilter = "All";
        private string activeSearchQuery = string.Empty;

        public FollowUpListForm()
        {
            InitializeComponent();

            BuildHeadingBlock();
            BuildSearchBar();
            BuildFilterBar();
            BuildDataGridCard();

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
            Name = "FollowUpListForm";
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
            headerTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210F));
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
                Text = "Follow-ups",
                Font = new Font(UITheme.FontSerif, 22F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 4)
            };

            // Subtitle: text-sm (9.5pt Regular)
            lblSubtitle = new Label
            {
                Text = "Scheduled customer inquiries and touchpoints",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                AutoSize = true,
                Margin = Padding.Empty
            };

            titleStack.Controls.Add(lblTitle);
            titleStack.Controls.Add(lblSubtitle);

            // Action button: px-5 py-2.5 text-sm font-semibold rounded-xl, icon 16px
            btnNewFollowUp = new Button
            {
                Text = "Schedule Follow-up",
                Size = new Size(195, 40),
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0, 4, 0, 0)
            };
            UITheme.ApplyActionButton(btnNewFollowUp, "\uE787", 12);
            btnNewFollowUp.Click += BtnNewFollowUp_Click;

            headerTable.Controls.Add(titleStack, 0, 0);
            headerTable.Controls.Add(btnNewFollowUp, 1, 0);

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
                PlaceholderText = "Search by customer or notes..."
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

            string[] filters = new[] { "All", "Pending", "Completed", "Cancelled" };

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
                btn.Click += (s, e) =>
                {
                    activeFilter = filterName;
                    UpdateFilterPillStyles();
                    _ = RefreshGridAsync();
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

            gridFollowUps = new DataGridView
            {
                Dock = DockStyle.Fill
            };
            UITheme.ApplyTableStyle(gridFollowUps);

            // 1. CUSTOMER (First Column Pattern)
            gridFollowUps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colCustomer", HeaderText = "CUSTOMER", Width = 260, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 2. FOLLOW-UP DATE
            gridFollowUps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colDate", HeaderText = "FOLLOW-UP DATE", Width = 160, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 3. NOTES (Fill available white space)
            gridFollowUps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colNotes", HeaderText = "NOTES", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 260, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 4. STATUS
            gridFollowUps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colStatus", HeaderText = "STATUS", Width = 140, SortMode = DataGridViewColumnSortMode.NotSortable });
            // 5. ACTION ("View →")
            gridFollowUps.Columns.Add(new DataGridViewTextBoxColumn { Name = "colAction", HeaderText = "", Width = 90, SortMode = DataGridViewColumnSortMode.NotSortable, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });

            gridFollowUps.CellPainting += GridFollowUps_CellPainting;
            gridFollowUps.CellClick += GridFollowUps_CellClick;
            gridFollowUps.CellMouseEnter += (s, e) =>
            {
                if (e.RowIndex >= 0 && gridFollowUps.Columns["colAction"] is { } colAction && e.ColumnIndex == colAction.Index)
                    gridFollowUps.Cursor = Cursors.Hand;
            };
            gridFollowUps.CellMouseLeave += (s, e) => gridFollowUps.Cursor = Cursors.Default;

            tableCardPanel.Controls.Add(gridFollowUps);
        }

        private void GridFollowUps_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0 || e.Graphics == null) return;

            var item = gridFollowUps.Rows[e.RowIndex].DataBoundItem as CustomerFollowUp;
            if (item == null) return;

            e.PaintBackground(e.CellBounds, true);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int cellY = e.CellBounds.Top + (e.CellBounds.Height - 20) / 2;

            int colIdxCustomer = gridFollowUps.Columns["colCustomer"]?.Index ?? -1;
            int colIdxDate = gridFollowUps.Columns["colDate"]?.Index ?? -1;
            int colIdxNotes = gridFollowUps.Columns["colNotes"]?.Index ?? -1;
            int colIdxStatus = gridFollowUps.Columns["colStatus"]?.Index ?? -1;
            int colIdxAction = gridFollowUps.Columns["colAction"]?.Index ?? -1;

            // 1. FIRST COLUMN PATTERN (Maroon Avatar + Customer Name + Scheduled Time)
            if (colIdxCustomer >= 0 && e.ColumnIndex == colIdxCustomer)
            {
                int avatarSize = 36;
                int avatarX = e.CellBounds.Left + 20; // px-5
                int avatarY = e.CellBounds.Top + (e.CellBounds.Height - avatarSize) / 2;

                using (var brush = new SolidBrush(UITheme.PrimaryMauve))
                {
                    g.FillEllipse(brush, avatarX, avatarY, avatarSize, avatarSize);
                }

                string custName = item.Customer != null ? $"{item.Customer.FirstName} {item.Customer.LastName}".Trim() : $"Customer #{item.CustomerId}";
                string initials = GetInitials(item.Customer?.FirstName ?? "F", item.Customer?.LastName ?? "U");

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
                    TextRenderer.DrawText(g, custName, nameFont, nameRect, UITheme.TextDark,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }

                using (var subFont = new Font(UITheme.FontSans, 8.5F))
                {
                    string subText = $"Follow-up #{item.FollowUpId}";
                    var subRect = new Rectangle(textX, e.CellBounds.Top + 38, textWidth, 18);
                    TextRenderer.DrawText(g, subText, subFont, subRect, UITheme.TextMuted,
                        TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }

                e.Handled = true;
            }
            // 2. FOLLOW-UP DATE
            else if (colIdxDate >= 0 && e.ColumnIndex == colIdxDate)
            {
                using var font = new Font(UITheme.FontSans, 9F);
                using var brush = new SolidBrush(UITheme.TextMuted);
                g.DrawString(item.FollowUpDate.ToString("yyyy-MM-dd"), font, brush, e.CellBounds.Left + 12, cellY);
                e.Handled = true;
            }
            // 3. NOTES
            else if (colIdxNotes >= 0 && e.ColumnIndex == colIdxNotes)
            {
                using var font = new Font(UITheme.FontSans, 9F);
                using var brush = new SolidBrush(UITheme.TextDark);
                g.DrawString(item.Notes ?? "-", font, brush, e.CellBounds.Left + 12, cellY);
                e.Handled = true;
            }
            // 4. STATUS
            else if (colIdxStatus >= 0 && e.ColumnIndex == colIdxStatus)
            {
                (string statusText, Color bg, Color fg) = GetFollowUpStatusBadge(item.StatusId);
                var pillRect = new Rectangle(e.CellBounds.Left + 8, e.CellBounds.Top + (e.CellBounds.Height - 26) / 2, 114, 26);
                UITheme.DrawStatusPill(g, pillRect, statusText, bg, fg, fg);
                e.Handled = true;
            }
            // 5. ACTION ("View →" in Maroon #8C465A)
            else if (colIdxAction >= 0 && e.ColumnIndex == colIdxAction)
            {
                using var font = new Font(UITheme.FontSans, 9F, FontStyle.Bold);
                using var brush = new SolidBrush(UITheme.PrimaryMauve);
                string viewText = "View →";
                var sz = g.MeasureString(viewText, font);
                g.DrawString(viewText, font, brush, e.CellBounds.Right - sz.Width - 14, cellY);
                e.Handled = true;
            }
        }

        private static (string Name, Color Bg, Color Fg) GetFollowUpStatusBadge(int statusId)
        {
            return statusId switch
            {
                0 => ("Pending", UITheme.StatusYellowBg, UITheme.StatusYellowFg),
                1 => ("Completed", UITheme.StatusGreenBg, UITheme.StatusGreenFg),
                2 => ("Cancelled", UITheme.StatusRedBg, UITheme.StatusRedFg),
                _ => ("Pending", UITheme.StatusYellowBg, UITheme.StatusYellowFg)
            };
        }

        private async void GridFollowUps_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridFollowUps.Columns["colAction"] is { } colAction && e.ColumnIndex == colAction.Index)
            {
                var item = gridFollowUps.Rows[e.RowIndex].DataBoundItem as CustomerFollowUp;
                if (item != null)
                {
                    using var f = new FollowUpForm(item);
                    if (f.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
                    {
                        await RefreshGridAsync();
                    }
                }
            }
        }

        private async void BtnNewFollowUp_Click(object? sender, EventArgs e)
        {
            using var form = new FollowUpForm();
            if (form.ShowDialog(this.FindForm() ?? this) == DialogResult.OK)
            {
                await RefreshGridAsync();
            }
        }

        public async Task RefreshGridAsync()
        {
            try
            {
                var list = await CrmDataService.GetFollowUpsAsync(activeFilter, activeSearchQuery);
                lblSubtitle.Text = $"{list.Count} scheduled follow-ups";
                gridFollowUps.DataSource = null;
                gridFollowUps.DataSource = list;
                gridFollowUps.RowTemplate.Height = 68;
                foreach (DataGridViewRow row in gridFollowUps.Rows)
                {
                    row.Height = 68;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load follow-ups from database: {ex.Message}", "Database Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string GetInitials(string firstName, string lastName)
        {
            char first = !string.IsNullOrWhiteSpace(firstName) ? char.ToUpper(firstName.Trim()[0]) : 'F';
            char last = !string.IsNullOrWhiteSpace(lastName) ? char.ToUpper(lastName.Trim()[0]) : 'U';
            return $"{first}{last}";
        }
    }
}
