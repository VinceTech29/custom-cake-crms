using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using CC.Services;

namespace CC.Controls
{
    public class BranchSelectorControl : UserControl
    {
        public event EventHandler<int?>? BranchChanged;

        private readonly ComboBox _cboBranches;
        private readonly Label _lblIcon;
        private bool _isInternalUpdate = false;
        private List<CC.Domain.Entities.Branch> _loadedBranches = new();
        private BranchCapabilityInfo? _capability;

        public class BranchComboItem
        {
            public int? BranchId { get; set; }
            public string DisplayText { get; set; } = string.Empty;

            public override string ToString() => DisplayText;
        }

        public BranchSelectorControl()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            Height = 36;
            Width = 220;
            Padding = new Padding(10, 4, 10, 4);

            _lblIcon = new Label
            {
                Text = "\uE716", // Segoe MDL2 Assets store/building or people icon
                Font = new Font("Segoe MDL2 Assets", 11F, FontStyle.Regular),
                ForeColor = UITheme.PrimaryMauve,
                AutoSize = false,
                Size = new Size(22, 26),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Left
            };

            _cboBranches = new ComboBox
            {
                Dock = DockStyle.Fill,
                DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Flat,
                Font = new Font(UITheme.FontSans, 9.5F, FontStyle.Bold),
                ForeColor = UITheme.TextDark,
                BackColor = Color.White,
                Cursor = Cursors.Hand
            };

            _cboBranches.SelectedIndexChanged += OnSelectedIndexChanged;

            Controls.Add(_cboBranches);
            Controls.Add(_lblIcon);

            SessionService.ActiveBranchChanged += OnSessionBranchChanged;

            this.HandleCreated += async (s, e) =>
            {
                await LoadBranchesAsync();
            };
        }

        private void OnSessionBranchChanged()
        {
            if (_isInternalUpdate || IsDisposed || !IsHandleCreated) return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(SyncSelectionFromSession));
            }
            else
            {
                SyncSelectionFromSession();
            }
        }

        private void SyncSelectionFromSession()
        {
            _isInternalUpdate = true;
            try
            {
                int? activeId = SessionService.ActiveBranchId;
                for (int i = 0; i < _cboBranches.Items.Count; i++)
                {
                    if (_cboBranches.Items[i] is BranchComboItem item && item.BranchId == activeId)
                    {
                        _cboBranches.SelectedIndex = i;
                        return;
                    }
                }

                if (_cboBranches.Items.Count > 0 && _cboBranches.SelectedIndex < 0)
                {
                    _cboBranches.SelectedIndex = 0;
                }
            }
            finally
            {
                _isInternalUpdate = false;
            }
        }

        public async Task LoadBranchesAsync()
        {
            if (IsDisposed) return;

            try
            {
                _capability = await CrmDataService.GetBranchCapabilityAsync();
                _loadedBranches = await CrmDataService.GetActiveBranchesAsync();

                if (IsDisposed || !IsHandleCreated) return;

                if (InvokeRequired)
                {
                    Invoke(new Action(PopulateCombo));
                }
                else
                {
                    PopulateCombo();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[BranchSelectorControl.LoadBranchesAsync] {ex.Message}");
            }
        }

        private void PopulateCombo()
        {
            _isInternalUpdate = true;
            try
            {
                _cboBranches.Items.Clear();

                bool isStaffRestricted = SessionService.CurrentUser?.BranchId.HasValue == true &&
                                         string.Equals(SessionService.CurrentUser.Role, "Staff", StringComparison.OrdinalIgnoreCase);

                // If staff is bound to a single branch, only show that branch
                if (isStaffRestricted)
                {
                    int staffBranchId = SessionService.CurrentUser!.BranchId!.Value;
                    string name = SessionService.CurrentUser.BranchName ?? "Assigned Branch";
                    var match = _loadedBranches.Find(b => b.BranchId == staffBranchId);
                    if (match != null) name = $"{match.BranchName} ({match.BranchCode})";

                    var singleItem = new BranchComboItem { BranchId = staffBranchId, DisplayText = name };
                    _cboBranches.Items.Add(singleItem);
                    _cboBranches.SelectedIndex = 0;
                    _cboBranches.Enabled = false;
                    SessionService.SetActiveBranch(staffBranchId, name);
                    return;
                }

                // If company does not have branching enabled or only has 1 branch:
                if (_capability != null && !_capability.AllowBranching)
                {
                    // Single branch mode
                    var mainBranch = _loadedBranches.Count > 0 ? _loadedBranches[0] : null;
                    string mainName = mainBranch != null ? $"{mainBranch.BranchName} ({mainBranch.BranchCode})" : "Main Branch";
                    var singleItem = new BranchComboItem { BranchId = mainBranch?.BranchId, DisplayText = mainName };
                    _cboBranches.Items.Add(singleItem);
                    _cboBranches.SelectedIndex = 0;
                    _cboBranches.Enabled = false;
                    SessionService.SetActiveBranch(mainBranch?.BranchId, mainName);
                    return;
                }

                // Multi-branch enabled: add "All Branches" option + all active branches
                _cboBranches.Enabled = true;
                _cboBranches.Items.Add(new BranchComboItem
                {
                    BranchId = null,
                    DisplayText = "All Branches"
                });

                foreach (var b in _loadedBranches)
                {
                    _cboBranches.Items.Add(new BranchComboItem
                    {
                        BranchId = b.BranchId,
                        DisplayText = $"{b.BranchName} ({b.BranchCode})"
                    });
                }

                SyncSelectionFromSession();
            }
            finally
            {
                _isInternalUpdate = false;
            }
        }

        private void OnSelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isInternalUpdate) return;

            if (_cboBranches.SelectedItem is BranchComboItem item)
            {
                string? cleanName = item.BranchId.HasValue ? item.DisplayText : null;
                SessionService.SetActiveBranch(item.BranchId, cleanName);
                BranchChanged?.Invoke(this, item.BranchId);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = CreateRoundedRectanglePath(rect, 14);
            using var pen = new Pen(UITheme.BorderColor, 1.2f);
            e.Graphics.DrawPath(pen, path);
        }

        private static GraphicsPath CreateRoundedRectanglePath(Rectangle bounds, int radius)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                SessionService.ActiveBranchChanged -= OnSessionBranchChanged;
            }
            base.Dispose(disposing);
        }
    }
}
