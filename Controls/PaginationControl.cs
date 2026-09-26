using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using CC.Forms.Authentication;

namespace CC.Controls
{
    /// <summary>
    /// Reusable pagination control for WinForms data tables.
    /// Standardized to max 10 rows per page with:
    /// - Summary label: "Showing X–Y of Z [items]" or empty state "No [items] found."
    /// - Navigation controls: [< Previous] [1] [2] [3] ... [Next >]
    /// - Responsive button states (active highlight, disabled Previous/Next).
    /// </summary>
    public class PaginationControl : UserControl
    {
        public event Action<int>? PageChanged;

        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalCount = 0;
        private int _totalPages = 1;
        private string _itemLabel = "records";

        private Label lblSummary = null!;
        private FlowLayoutPanel buttonPanel = null!;

        public int CurrentPage => _currentPage;
        public int PageSize => _pageSize;
        public int TotalCount => _totalCount;
        public int TotalPages => _totalPages;

        public PaginationControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            Dock = DockStyle.Bottom;
            Height = 52;
            BackColor = Color.White;
            Padding = new Padding(16, 8, 16, 8);

            lblSummary = new Label
            {
                Dock = DockStyle.Left,
                AutoSize = false,
                Width = 320,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted
            };

            buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            Controls.Add(buttonPanel);
            Controls.Add(lblSummary);
            ResumeLayout(false);
            PerformLayout();
        }

        /// <summary>
        /// Updates the pagination state and rebuilds the controls.
        /// </summary>
        public void SetPagination(int currentPage, int pageSize, int totalCount, string itemLabel = "records")
        {
            _pageSize = Math.Max(1, pageSize);
            _totalCount = Math.Max(0, totalCount);
            _totalPages = Math.Max(1, (int)Math.Ceiling((double)_totalCount / _pageSize));
            _currentPage = Math.Clamp(currentPage, 1, _totalPages);
            _itemLabel = itemLabel;

            buttonPanel.SuspendLayout();
            buttonPanel.Controls.Clear();

            if (_totalCount == 0)
            {
                lblSummary.Text = $"No {_itemLabel} found.";
                buttonPanel.ResumeLayout(true);
                return;
            }

            int startRow = ((_currentPage - 1) * _pageSize) + 1;
            int endRow = Math.Min(_currentPage * _pageSize, _totalCount);
            lblSummary.Text = $"Showing {startRow}–{endRow} of {_totalCount} {_itemLabel}";

            // 1. Previous Button
            var btnPrev = CreateNavButton("< Previous", _currentPage > 1);
            btnPrev.Click += (s, e) =>
            {
                if (_currentPage > 1)
                {
                    PageChanged?.Invoke(_currentPage - 1);
                }
            };
            buttonPanel.Controls.Add(btnPrev);

            // 2. Numeric Page Buttons with smart ellipsis
            // If total pages <= 7, display all: [1] [2] [3] [4] [5] [6] [7]
            // If total pages > 7, display window: e.g. [1] ... [4] [5] [6] ... [15]
            if (_totalPages <= 7)
            {
                for (int i = 1; i <= _totalPages; i++)
                {
                    buttonPanel.Controls.Add(CreatePageButton(i, i == _currentPage));
                }
            }
            else
            {
                // Always show first page
                buttonPanel.Controls.Add(CreatePageButton(1, _currentPage == 1));

                int windowStart = Math.Max(2, _currentPage - 1);
                int windowEnd = Math.Min(_totalPages - 1, _currentPage + 1);

                if (windowStart > 2)
                {
                    buttonPanel.Controls.Add(CreateEllipsisLabel());
                }

                for (int i = windowStart; i <= windowEnd; i++)
                {
                    buttonPanel.Controls.Add(CreatePageButton(i, i == _currentPage));
                }

                if (windowEnd < _totalPages - 1)
                {
                    buttonPanel.Controls.Add(CreateEllipsisLabel());
                }

                // Always show last page
                buttonPanel.Controls.Add(CreatePageButton(_totalPages, _currentPage == _totalPages));
            }

            // 3. Next Button
            var btnNext = CreateNavButton("Next >", _currentPage < _totalPages);
            btnNext.Click += (s, e) =>
            {
                if (_currentPage < _totalPages)
                {
                    PageChanged?.Invoke(_currentPage + 1);
                }
            };
            buttonPanel.Controls.Add(btnNext);

            buttonPanel.ResumeLayout(true);
        }

        private Button CreatePageButton(int pageNumber, bool isActive)
        {
            var btn = new Button
            {
                Text = pageNumber.ToString(),
                Size = new Size(34, 32),
                Margin = new Padding(2, 2, 2, 2),
                FlatStyle = FlatStyle.Flat,
                Cursor = isActive ? Cursors.Default : Cursors.Hand,
                Font = new Font(UITheme.FontSans, 9F, isActive ? FontStyle.Bold : FontStyle.Regular),
                BackColor = isActive ? UITheme.PrimaryMauve : Color.FromArgb(246, 243, 239),
                ForeColor = isActive ? Color.White : UITheme.TextDark,
                TabStop = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.ApplyRoundedRegion(6);

            if (!isActive)
            {
                btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(235, 228, 220);
                btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(246, 243, 239);
                btn.Click += (s, e) => PageChanged?.Invoke(pageNumber);
            }

            return btn;
        }

        private Button CreateNavButton(string text, bool isEnabled)
        {
            var btn = new Button
            {
                Text = text,
                AutoSize = true,
                MinimumSize = new Size(74, 32),
                Height = 32,
                Margin = new Padding(4, 2, 4, 2),
                FlatStyle = FlatStyle.Flat,
                Enabled = isEnabled,
                Cursor = isEnabled ? Cursors.Hand : Cursors.Default,
                Font = new Font(UITheme.FontSans, 8.5F, FontStyle.Regular),
                BackColor = isEnabled ? Color.FromArgb(246, 243, 239) : Color.FromArgb(250, 249, 247),
                ForeColor = isEnabled ? UITheme.TextDark : Color.FromArgb(180, 172, 166),
                TabStop = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.ApplyRoundedRegion(6);

            if (isEnabled)
            {
                btn.MouseEnter += (s, e) => btn.BackColor = Color.FromArgb(235, 228, 220);
                btn.MouseLeave += (s, e) => btn.BackColor = Color.FromArgb(246, 243, 239);
            }

            return btn;
        }

        private Label CreateEllipsisLabel()
        {
            return new Label
            {
                Text = "...",
                Size = new Size(24, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(UITheme.FontSans, 9F, FontStyle.Regular),
                ForeColor = UITheme.TextMuted,
                Margin = new Padding(1, 2, 1, 2)
            };
        }
    }
}
