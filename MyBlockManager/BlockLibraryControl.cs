// BlockLibraryControl.cs
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MyBlockManager
{
    public partial class BlockLibraryControl : UserControl
    {
        private readonly SettingsService _settingsService;
        private AppSettings _settings;
        private List<string> _allBlockFiles = new List<string>();

        public BlockLibraryControl()
        {
            InitializeComponent();
            _settingsService = new SettingsService();
        }

        private void BlockLibraryControl_Load(object sender, EventArgs e)
        {
            if (this.DesignMode) return;

            this.btnAddFolder.Click += BtnAddFolder_Click;
            this.btnSearch.Click += (s, ev) => FilterAndDisplayBlocks();
            this.txtSearch.TextChanged += TxtSearch_TextChanged;
            this.listBoxBlocks.SelectedIndexChanged += ListBoxBlocks_SelectedIndexChanged;
            this.listBoxBlocks.DoubleClick += ListBoxBlocks_DoubleClick;
            this.txtPath.KeyDown += TxtPath_KeyDown;
            this.txtPath.DoubleClick += TxtBox_DoubleClick;
            this.txtSearch.DoubleClick += TxtBox_DoubleClick;

            _settings = _settingsService.Load();
            RefreshContent();
        }

        public void RefreshContent()
        {
            // 这是应用主题的唯一入口点。
            // 每当面板显示时，此方法会被调用，从而自动匹配主题。
            ThemeApplicator.ApplyThemeToControl(this);

            UpdateLibraryPathDisplay();
            ScanAllBlockFiles();
        }

        private void UpdateLibraryPathDisplay()
        {
            if (_settings.LibraryPaths.Count > 0)
            {
                txtPath.Text = _settings.LibraryPaths[0];
            }
            else
            {
                txtPath.Text = "Paste path here and press Enter, or click Add...";
            }
        }

        private void TxtBox_DoubleClick(object sender, EventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.SelectAll();
            }
        }

        private void TxtPath_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                string potentialPath = txtPath.Text.Trim();

                if (!string.IsNullOrEmpty(potentialPath) && Directory.Exists(potentialPath))
                {
                    AddOrUpdatePath(potentialPath);
                }
                else
                {
                    MessageBox.Show("The path you entered is not valid. Please try again or select via the Add button.", "Invalid Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnAddFolder_Click(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Select Block Library Folder";
                dialog.Filter = "Folders|*.this-is-a-dummy-extension";
                dialog.CheckFileExists = false;
                dialog.CheckPathExists = true;
                dialog.ValidateNames = false;
                dialog.FileName = "Select Folder";

                string currentPath = txtPath.Text.Trim();
                if (!string.IsNullOrEmpty(currentPath) && Directory.Exists(currentPath))
                {
                    dialog.InitialDirectory = currentPath;
                }

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = Path.GetDirectoryName(dialog.FileName);
                    if (string.IsNullOrEmpty(selectedPath))
                    {
                        selectedPath = Path.GetPathRoot(dialog.FileName);
                    }
                    AddOrUpdatePath(selectedPath);
                }
            }
        }

        private void AddOrUpdatePath(string pathToAdd)
        {
            if (string.IsNullOrEmpty(pathToAdd) || !Directory.Exists(pathToAdd))
            {
                MessageBox.Show("The selected path is not a valid folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _settings.LibraryPaths.Clear();
            _settings.LibraryPaths.Add(pathToAdd);

            _settingsService.Save(_settings);
            RefreshContent();
        }

        private void ScanAllBlockFiles()
        {
            _allBlockFiles.Clear();
            foreach (var path in _settings.LibraryPaths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        var files = Directory.GetFiles(path, "*.dwg", SearchOption.AllDirectories);
                        _allBlockFiles.AddRange(files);
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Failed to scan folder {path}: {ex.Message}");
                    }
                }
            }
            FilterAndDisplayBlocks();
        }

        private void FilterAndDisplayBlocks()
        {
            listBoxBlocks.BeginUpdate();
            listBoxBlocks.Items.Clear();

            string keyword = txtSearch.Text.ToLower().Trim();

            var filteredList = _allBlockFiles
                .Where(f => Path.GetFileNameWithoutExtension(f).ToLower().Contains(keyword))
                .Select(f => new BlockListItem { FullPath = f, DisplayName = Path.GetFileName(f) })
                .ToList();

            listBoxBlocks.Items.AddRange(filteredList.ToArray());
            listBoxBlocks.DisplayMember = "DisplayName";
            listBoxBlocks.EndUpdate();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            FilterAndDisplayBlocks();
        }

        private async void ListBoxBlocks_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxBlocks.SelectedItem is BlockListItem selectedItem)
            {
                pictureBoxPreview.Image = null;
                pictureBoxPreview.Image = await GetThumbnailAsync(selectedPath: selectedItem.FullPath);
            }
        }

        private Task<Bitmap> GetThumbnailAsync(string selectedPath)
        {
            return Task.Run(() =>
            {
                try
                {
                    using (var db = new Database(false, true))
                    {
                        db.ReadDwgFile(selectedPath, FileShare.Read, true, "");
                        return db.ThumbnailBitmap;
                    }
                }
                catch { return null; }
            });
        }

        private void ListBoxBlocks_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxBlocks.SelectedItem is BlockListItem selectedItem)
            {
                InsertBlock(dwgPath: selectedItem.FullPath);
            }
        }

        private void InsertBlock(string dwgPath)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Core.Application.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            string blockName = Path.GetFileNameWithoutExtension(dwgPath);

            using (doc.LockDocument())
            {
                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    try
                    {
                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        ObjectId blockId;

                        if (!bt.Has(blockName))
                        {
                            using (Database sourceDb = new Database(false, true))
                            {
                                sourceDb.ReadDwgFile(dwgPath, FileShare.Read, true, "");
                                blockId = db.Insert(blockName, sourceDb, false);
                            }
                        }
                        else
                        {
                            blockId = bt[blockName];
                        }

                        if (blockId.IsNull)
                        {
                            ed.WriteMessage($"\nCould not find or insert block '{blockName}'.");
                            return;
                        }

                        BlockReference br = new BlockReference(Point3d.Origin, blockId)
                        {
                            ScaleFactors = new Scale3d(1.0),
                            Rotation = 0.0
                        };

                        var jig = new BlockInsertJig(br);
                        PromptResult pr = ed.Drag(jig);

                        if (pr.Status == PromptStatus.OK)
                        {
                            BlockTableRecord curSpace = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                            curSpace.AppendEntity(jig.ResultEntity as BlockReference);
                            tr.AddNewlyCreatedDBObject(jig.ResultEntity, true);
                            tr.Commit();
                        }
                        else
                        {
                            tr.Abort();
                        }
                    }
                    catch (System.Exception ex)
                    {
                        ed.WriteMessage($"\nError during block insertion: {ex.Message}");
                        tr.Abort();
                    }
                }
            }
        }

        private class BlockListItem
        {
            public string DisplayName { get; set; }
            public string FullPath { get; set; }
        }

        /// <summary>
        /// 这是一个内置的静态类，专门负责UI主题的美化。
        /// </summary>
        private static class ThemeApplicator
        {
            private static readonly Color DarkThemeBackground = Color.FromArgb(52, 52, 52);
            private static readonly Color DarkThemeForeground = Color.FromArgb(241, 241, 241);
            private static readonly Color DarkThemeControlBackground = Color.FromArgb(62, 62, 62);
            private static readonly Color DarkThemeBorder = Color.FromArgb(82, 82, 82);
            private static readonly Color DarkThemeHighlight = Color.FromArgb(28, 116, 206);

            public static void ApplyThemeToControl(Control control)
            {
                if (IsAcadDarkTheme())
                {
                    ApplyDarkTheme(control);
                }
            }

            private static bool IsAcadDarkTheme()
            {
                try
                {
                    return (short)Autodesk.AutoCAD.ApplicationServices.Application.GetSystemVariable("COLORTHEME") == 1;
                }
                catch
                {
                    return false;
                }
            }

            private static void ApplyDarkTheme(Control control)
            {
                // 为所有控件设置默认的背景和文字颜色
                control.BackColor = DarkThemeBackground;
                control.ForeColor = DarkThemeForeground;

                // 根据不同控件类型应用精细化样式
                if (control is TextBox || control is ComboBox)
                {
                    control.BackColor = DarkThemeControlBackground;
                    control.ForeColor = DarkThemeForeground;
                    if (control is TextBox txt) txt.BorderStyle = BorderStyle.FixedSingle;
                }
                else if (control is Button btn)
                {
                    // --- 核心修复：强制按钮使用我们自定义的颜色 ---
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderColor = DarkThemeBorder;
                    btn.UseVisualStyleBackColor = false; // 禁用系统视觉样式
                    btn.BackColor = DarkThemeControlBackground;
                    btn.ForeColor = DarkThemeForeground;
                }
                else if (control is ListBox lst)
                {
                    lst.BackColor = DarkThemeControlBackground;
                    lst.ForeColor = DarkThemeForeground;
                    lst.BorderStyle = BorderStyle.FixedSingle;
                    lst.DrawMode = DrawMode.OwnerDrawFixed;
                    lst.DrawItem -= ListBox_DrawItem;
                    lst.DrawItem += ListBox_DrawItem;
                }
                else if (control is PictureBox pic)
                {
                    pic.BackColor = DarkThemeControlBackground;
                }
                else if (control is Label)
                {
                    control.BackColor = Color.Transparent;
                }
                else if (control is Panel || control is TableLayoutPanel) // 显式处理Panel和TableLayoutPanel
                {
                    control.BackColor = DarkThemeBackground;
                }

                // 递归应用到所有子控件
                foreach (Control child in control.Controls)
                {
                    ApplyDarkTheme(child);
                }
            }

            private static void ListBox_DrawItem(object sender, DrawItemEventArgs e)
            {
                if (e.Index < 0) return;
                ListBox lst = sender as ListBox;
                if (lst == null) return;

                // 直接使用我们定义的颜色，而不是依赖控件的属性
                Color background = (e.State & DrawItemState.Selected) == DrawItemState.Selected ? DarkThemeHighlight : DarkThemeControlBackground;

                using (SolidBrush backBrush = new SolidBrush(background))
                {
                    e.Graphics.FillRectangle(backBrush, e.Bounds);
                }

                string text = lst.GetItemText(lst.Items[e.Index]);
                // 核心修复：选中项的文字颜色也应该保持不变
                using (SolidBrush foreBrush = new SolidBrush(DarkThemeForeground))
                {
                    e.Graphics.DrawString(text, e.Font, foreBrush, e.Bounds, StringFormat.GenericDefault);
                }

                e.DrawFocusRectangle();
            }
        }
    }

    public class BlockInsertJig : EntityJig
    {
        private Point3d _currentPosition;

        public Entity ResultEntity => Entity;

        public BlockInsertJig(Entity ent) : base(ent)
        {
            if (ent is BlockReference br)
            {
                _currentPosition = br.Position;
            }
        }

        protected override SamplerStatus Sampler(JigPrompts prompts)
        {
            var jigOpts = new JigPromptPointOptions("\nSpecify insertion point:");
            jigOpts.UserInputControls = UserInputControls.NoZeroResponseAccepted;
            PromptPointResult ppr = prompts.AcquirePoint(jigOpts);

            if (ppr.Status == PromptStatus.Cancel)
                return SamplerStatus.Cancel;

            if (ppr.Value.IsEqualTo(_currentPosition))
                return SamplerStatus.NoChange;

            _currentPosition = ppr.Value;
            return SamplerStatus.OK;
        }

        protected override bool Update()
        {
            if (Entity is BlockReference br)
            {
                br.Position = _currentPosition;
            }
            return true;
        }
    }
}

