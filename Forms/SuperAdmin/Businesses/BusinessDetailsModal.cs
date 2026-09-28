using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Controls;
using CC.Forms.Authentication;
using CC.Services;

namespace CC.Forms.SuperAdmin.Businesses
{
    public class BusinessDetailsModal : Form
    {
        private static readonly Color ColorModalBg = Color.White;
        private static readonly Color ColorCardBg = Color.FromArgb(248, 246, 243);
        private static readonly Color ColorPrimaryBtn = Color.FromArgb(140, 70, 90);
        private static readonly Color ColorCancelBtn = Color.FromArgb(239, 231, 223);

        private readonly int _companyId;
        private CompanyDetails? _details;

        public BusinessDetailsModal(int companyId)
        {
            _companyId = companyId;

            InitializeModal();
            LoadAndBuildAsync();
        }

        private void InitializeModal()
        {
            Text = "Business & Tenant Details";
            Size = new Size(580, 680);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = ColorModalBg;
            DoubleBuffered = true;
            ShowInTaskbar = false;

            Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var pen = new Pen(ColorPrimaryBtn, 2.5f);
                e.Graphics.DrawRoundedRectangle(pen, new Rectangle(1, 1, Width - 3, Height - 3), 16);
            };

            Load += (s, e) => this.ApplyRoundedRegion(16);
        }

        private async void LoadAndBuildAsync()
        {
            try
            {
                _details = await CrmDataService.GetCompanyByIdAsync(_companyId);
                BuildUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load business details: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void BuildUI()
        {
            Controls.Clear();

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 65,
                Padding = new Padding(28, 20, 24, 0),
                BackColor = Color.Transparent
            };

            var lblTitle = new Label
            {
                Text = "Business & Tenant Details",
                Font = new Font(UITheme.FontSerif, 18F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                AutoSize = true,
                Location = new Point(28, 20)
            };

            var btnClose = new Button
            {
                Text = "\u2715",
                Font = new Font(UITheme.FontSans, 11F),
                ForeColor = Color.FromArgb(140, 120, 115),
                Size = new Size(32, 32),
                Location = new Point(Width - 56, 18),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(246, 243, 239)
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.ApplyRoundedRegion(16);
            btnClose.Click += (s, e) => Close();

            headerPanel.Controls.Add(lblTitle);
            headerPanel.Controls.Add(btnClose);
            Controls.Add(headerPanel);

            if (_details == null) return;

            // Scrollable Content
            var bodyPanel = new Panel
            {
                Location = new Point(28, 75),
                Size = new Size(Width - 56, Height - 160),
                AutoScroll = true,
                BackColor = Color.Transparent
            };

            int y = 5;
            int cardW = bodyPanel.Width - 25;

            // Header Banner Card
            var bannerCard = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(cardW, 90),
                BackColor = ColorCardBg
            };
            bannerCard.ApplyRoundedRegion(12);

            var lblBizName = new Label
            {
                Text = _details.CompanyName,
                Font = new Font(UITheme.FontSerif, 15F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                Location = new Point(16, 14),
                AutoSize = true
            };

            var lblCodePill = new Label
            {
                Text = $"Code: {_details.CompanyCode}",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Location = new Point(16, 44),
                AutoSize = true
            };

            var lblStatusPill = new Label
            {
                Text = _details.IsActive ? "\u25CF Active" : "\u25CF Inactive",
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = _details.IsActive ? Color.FromArgb(46, 133, 90) : Color.FromArgb(180, 60, 60),
                Location = new Point(cardW - 110, 18),
                AutoSize = true
            };

            var lblRegDate = new Label
            {
                Text = $"Registered: {_details.CreatedDate:MMM dd, yyyy}",
                Font = new Font(UITheme.FontSans, 8.5F),
                ForeColor = UITheme.TextMuted,
                Location = new Point(cardW - 160, 44),
                AutoSize = true
            };

            bannerCard.Controls.Add(lblBizName);
            bannerCard.Controls.Add(lblCodePill);
            bannerCard.Controls.Add(lblStatusPill);
            bannerCard.Controls.Add(lblRegDate);
            bodyPanel.Controls.Add(bannerCard);
            y += 105;

            // Details Sections
            bodyPanel.Controls.Add(CreateSectionCard("Contact & Communication", new[]
            {
                ("Email Address", _details.ContactEmail),
                ("Phone Number", string.IsNullOrWhiteSpace(_details.ContactPhone) ? "Not provided" : _details.ContactPhone)
            }, new Point(0, y), cardW));
            y += 105;

            string fullAddr = $"{_details.AddressLine1}, {_details.City}, {_details.State} {_details.PostalCode}, {_details.Country}".Trim(' ', ',');
            bodyPanel.Controls.Add(CreateSectionCard("Physical Location", new[]
            {
                ("Street Address", string.IsNullOrWhiteSpace(_details.AddressLine1) ? "Not provided" : _details.AddressLine1),
                ("City & Province", $"{_details.City}, {_details.State}".Trim(' ', ',')),
                ("Country & Zip", $"{_details.Country} ({_details.PostalCode})")
            }, new Point(0, y), cardW));
            y += 135;

            bodyPanel.Controls.Add(CreateSectionCard("Multi-Tenant Routing & Database", new[]
            {
                ("Database Server", _details.ServerName),
                ("Tenant Database Name", _details.DatabaseName),
                ("Routing Status", _details.IsActive ? "Online & Accessible" : "Routing Suspended")
            }, new Point(0, y), cardW));
            y += 135;

            bodyPanel.Controls.Add(CreateSectionCard("Subscription & Platform Usage", new[]
            {
                ("Active Subscription Plan", _details.SubscriptionPlan),
                ("Active User Accounts", $"{_details.UserCount} team member(s)")
            }, new Point(0, y), cardW));
            y += 105;

            Controls.Add(bodyPanel);

            // Footer Panel
            var footerPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 75,
                Padding = new Padding(28, 12, 28, 20),
                BackColor = Color.Transparent
            };

            var btnCloseBottom = new Button
            {
                Text = "Close",
                Size = new Size(110, 42),
                Location = new Point(footerPanel.Width - 140, 15),
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                BackColor = ColorCancelBtn,
                ForeColor = UITheme.TextDark,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            btnCloseBottom.FlatAppearance.BorderSize = 0;
            btnCloseBottom.ApplyRoundedRegion(10);
            btnCloseBottom.Click += (s, e) => Close();

            footerPanel.Controls.Add(btnCloseBottom);
            Controls.Add(footerPanel);
        }

        private Panel CreateSectionCard(string title, (string Key, string Val)[] fields, Point location, int width)
        {
            var pnl = new Panel
            {
                Location = location,
                Width = width,
                BackColor = Color.White
            };

            int curY = 0;
            var lblSection = new Label
            {
                Text = title.ToUpperInvariant(),
                Font = new Font(UITheme.FontSans, 8F, FontStyle.Bold),
                ForeColor = UITheme.PrimaryMauve,
                Location = new Point(4, curY),
                AutoSize = true
            };
            pnl.Controls.Add(lblSection);
            curY += 24;

            var card = new Panel
            {
                Location = new Point(0, curY),
                Width = width,
                Height = fields.Length * 28 + 12,
                BackColor = ColorCardBg,
                Padding = new Padding(12, 8, 12, 8)
            };
            card.ApplyRoundedRegion(10);

            int itemY = 8;
            foreach (var (k, v) in fields)
            {
                var lblK = new Label
                {
                    Text = k,
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Bold),
                    ForeColor = UITheme.TextMuted,
                    Location = new Point(12, itemY),
                    Width = 170,
                    AutoEllipsis = true
                };

                var lblV = new Label
                {
                    Text = v,
                    Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                    ForeColor = UITheme.TextDark,
                    Location = new Point(190, itemY),
                    Width = width - 210,
                    AutoEllipsis = true
                };

                card.Controls.Add(lblK);
                card.Controls.Add(lblV);
                itemY += 26;
            }

            pnl.Controls.Add(card);
            pnl.Height = curY + card.Height + 6;
            return pnl;
        }
    }
}
