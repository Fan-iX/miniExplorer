using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using ShellApp;
using System.Reflection;


namespace miniExplorer
{
    public partial class miniBrowser : Form
    {
        private SplitContainer scMain = new SplitContainer()
        {
            AllowDrop = true,
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BorderStyle = BorderStyle.None,
            TabStop = false,
            SplitterWidth = 1,
            IsSplitterFixed = true,
            FixedPanel = FixedPanel.Panel1
        };
        private SplitContainer scFavorites = new SplitContainer()
        {
            AllowDrop = true,
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BorderStyle = BorderStyle.None,
            TabStop = false,
            SplitterWidth = 2,
            FixedPanel = FixedPanel.Panel1,
            IsSplitterFixed = false
        };
        private SplitContainer sc = new SplitContainer()
        {
            AllowDrop = true,
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            BorderStyle = BorderStyle.None,
            TabStop = false,
            SplitterWidth = 3
        };
        private TabControl tc = new TabControl()
        {
            Dock = DockStyle.Fill
        };
        private ListViewWithoutScrollBar lvFavorites = new ListViewWithoutScrollBar()
        {
            View = View.List,
            AllowDrop = true,
            // FullRowSelect = true,
            Dock = DockStyle.Fill,
            SmallImageList = new ImageList()
        };
        private ListViewWithoutScrollBar lvFolder = new ListViewWithoutScrollBar()
        {
            View = View.Details,
            AllowDrop = true,
            LabelEdit = true,
            // FullRowSelect = true,
            Dock = DockStyle.Fill,
            ListViewItemSorter = new DirectoryListViewColumnSorter(),
            SmallImageList = new ImageList()
        };
        private ListViewWithoutScrollBar lvFile = new ListViewWithoutScrollBar()
        {
            View = View.Details,
            AllowDrop = true,
            LabelEdit = true,
            // FullRowSelect = true,
            Dock = DockStyle.Fill,
            ListViewItemSorter = new FileListViewColumnSorter(),
            SmallImageList = new ImageList()
        };
        private TextBox tb = new TextBox()
        {
            AllowDrop = true,
            AutoSize = false,
            TabStop = false,
            Dock = DockStyle.Fill,
            AutoCompleteMode = AutoCompleteMode.Append,
            AutoCompleteSource = AutoCompleteSource.CustomSource
        };
        private FileSystemWatcher watcher = new FileSystemWatcher()
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = false
        };
        private ShellContextMenu ctxMnu = new ShellContextMenu();
        private ContextMenuStrip cms = new ContextMenuStrip();
        private ToolStripMenuItem cmsiExpDir;
        private ToolStripMenuItem cmsiHist;

        private string _dirPath;

        public string dirPath
        {
            get
            {
                return this._dirPath;
            }
            set
            {
                if (!Directory.Exists(value)) return;
                if (this._dirPath != null)
                    this.history.Insert(0, this._dirPath);
                this.history = this.history.Distinct().Take(10).ToList();
                this._dirPath = ShellInfoHelper.GetExactPathName(value);
                refreshForm();
            }
        }

        private Size fullSize;
        private DpiFactor dpiScale;

        private bool allowFold = false;
        private List<string> history = new List<string>();
        private List<string> favorites;

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            fullSize = new Size(this.ClientSize.Width, Math.Max(this.ClientSize.Height, 150 * dpiScale));
            Properties.Settings.Default.FullSize = this.fullSize;
            Properties.Settings.Default.LastDirPath = this.dirPath;
            Properties.Settings.Default.WindowLocation = this.Location;
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);
            watcher.Dispose();
        }

        protected void ResizeControl()
        {
            scMain.SplitterDistance = 18 * this.dpiScale;
            scFavorites.SplitterDistance = 100 * this.dpiScale;
            lvFolder.SmallImageList.ImageSize = new Size(
                16 * this.dpiScale,
                16 * this.dpiScale
            );
            lvFavorites.SmallImageList.ImageSize = new Size(
                16 * this.dpiScale,
                16 * this.dpiScale
            );
            lvFile.SmallImageList.ImageSize = new Size(
                16 * this.dpiScale,
                16 * this.dpiScale
            );
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            this.dpiScale = new DpiFactor(e.DeviceDpiNew / 96.0f);
            if (scMain.Visible) this.MinimumSize = new Size(150 * dpiScale, 150 * dpiScale);
            ResizeControl();
            if (scMain.Visible) 
            {
                refreshForm();
                refreshFavorite();
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_NCLBUTTONDOWN = 0x00A1;
            const int WM_NCLBUTTONDBLCLK = 0x00A3;
            const int WM_NCRBUTTONDOWN = 0x00A4;
            const int WM_CONTEXTMENU = 0x007B;
            switch (m.Msg)
            {
                case WM_CONTEXTMENU:
                    m.Result = IntPtr.Zero;
                    break;
                case WM_NCLBUTTONDBLCLK:
                case WM_NCRBUTTONDOWN:
                    allowFold = !allowFold;
                    if (allowFold)
                        this.FoldWindow();
                    else
                        this.UnfoldWindow();
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        public miniBrowser()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(miniBrowser));
            fullSize = Properties.Settings.Default.FullSize;
            favorites = Properties.Settings.Default.Favorites;

            watcher.Created += new FileSystemEventHandler(watcher_FileInfoChange);
            watcher.Changed += new FileSystemEventHandler(watcher_FileInfoChange);
            watcher.Deleted += new FileSystemEventHandler(watcher_FileInfoChange);
            watcher.Renamed += new RenamedEventHandler(watcher_FileInfoChange);

            this.AllowDrop = true;
            this.ClientSize = fullSize;
            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;
            this.StartPosition = FormStartPosition.WindowsDefaultLocation;
            this.TopMost = true;
            this.KeyPreview = true;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.dpiScale = new DpiFactor(this.DeviceDpi / 96.0f);
            this.MinimumSize = new Size(150 * dpiScale, 150 * dpiScale);
            this.Font = SystemFonts.CaptionFont;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("miniExplorer.Resources.miniExplorer.ico"))
            {
                this.Icon = new Icon(stream);
            }

            lvFolder.Columns.Add("文件夹", -2, HorizontalAlignment.Left);
            lvFolder.Columns.Add("修改时间", 0, HorizontalAlignment.Left);

            lvFile.Columns.Add("文件", -2, HorizontalAlignment.Left);
            lvFile.Columns.Add("类型", 0, HorizontalAlignment.Left);
            lvFile.Columns.Add("修改时间", 0, HorizontalAlignment.Left);
            lvFile.Columns.Add("大小", 0, HorizontalAlignment.Right);

            sc.Panel1.Controls.Add(lvFolder);
            sc.Panel2.Controls.Add(lvFile);

            cms.DropShadowEnabled = false;
            cmsiExpDir = (ToolStripMenuItem)cms.Items.Add("资源管理器窗口");
            cmsiExpDir.DropDown.DropShadowEnabled = false;

            cmsiHist = (ToolStripMenuItem)cms.Items.Add("历史记录");
            cmsiHist.DropDown.DropShadowEnabled = false;

            cms.Items.Add("跳转到...").Click += new EventHandler(cms_GoTo_Click);

            tb.KeyUp += new KeyEventHandler(tb_KeyUp);
            tb.DragEnter += new DragEventHandler(tb_DragEnter);
            tb.DragDrop += new DragEventHandler(tb_DragDrop);

            sc.DragEnter += new DragEventHandler(sc_DragEnter);
            lvFolder.DragEnter += new DragEventHandler(sc_DragEnter);
            lvFile.DragEnter += new DragEventHandler(sc_DragEnter);
            sc.DragLeave += new EventHandler(form_DragLeave);
            lvFolder.DragLeave += new EventHandler(form_DragLeave);
            lvFile.DragLeave += new EventHandler(form_DragLeave);

            sc.MouseDoubleClick += new MouseEventHandler(sc_DoubleClick);

            lvFolder.MouseClick += new MouseEventHandler(lv_MouseClick);
            lvFile.MouseClick += new MouseEventHandler(lv_MouseClick);
            lvFavorites.MouseClick += new MouseEventHandler(lv_MouseClick);

            lvFile.DragOver += new DragEventHandler(lv_DragOver);
            lvFavorites.DragOver += new DragEventHandler(lv_DragOver);

            lvFolder.ItemDrag += new ItemDragEventHandler(lvBrowser_ItemDrag);
            lvFile.ItemDrag += new ItemDragEventHandler(lvBrowser_ItemDrag);
            lvFavorites.ItemDrag += new ItemDragEventHandler(lvFavorites_ItemDrag);

            lvFolder.DragDrop += new DragEventHandler(lvBrowser_DragDrop);
            lvFolder.KeyDown += new KeyEventHandler(lvBrowser_KeyDown);
            lvFolder.MouseUp += new MouseEventHandler(lv_MouseUp);
            lvFolder.MouseDown += new MouseEventHandler(lvBrowser_MouseDown);
            lvFile.DragDrop += new DragEventHandler(lvBrowser_DragDrop);
            lvFile.KeyDown += new KeyEventHandler(lvBrowser_KeyDown);
            lvFile.MouseUp += new MouseEventHandler(lv_MouseUp);
            lvFile.MouseDown += new MouseEventHandler(lvBrowser_MouseDown);

            lvFavorites.DragEnter += new DragEventHandler(lvFavorites_DragEnter);
            lvFavorites.MouseDoubleClick += new MouseEventHandler(lvFavorites_DoubleClick);
            lvFavorites.KeyDown += new KeyEventHandler(lvFavorites_KeyDown);
            lvFavorites.DragDrop += new DragEventHandler(lvFavorites_DragDrop);

            lvFolder.DragOver += new DragEventHandler(lvFolder_DragOver);
            lvFolder.MouseDoubleClick += new MouseEventHandler(lvFolder_DoubleClick);
            lvFolder.ColumnClick += new ColumnClickEventHandler(lvFolder_ColumnClick);
            lvFolder.AfterLabelEdit += new LabelEditEventHandler(lvFolder_AfterLabelEdit);

            lvFile.MouseDoubleClick += new MouseEventHandler(lvFile_DoubleClick);
            lvFile.ColumnClick += new ColumnClickEventHandler(lvFile_ColumnClick);
            lvFile.AfterLabelEdit += new LabelEditEventHandler(lvFile_AfterLabelEdit);

            this.DragEnter += new DragEventHandler(form_DragEnter);
            this.DragLeave += new EventHandler(form_DragLeave);
            this.ResizeEnd += new EventHandler(form_ResizeEnd);
            this.KeyDown += new KeyEventHandler(form_KeyDown);
            this.Resize += new EventHandler(form_Resize);
            this.Load += new EventHandler(form_Load);

            scMain.Panel1.Controls.Add(tb);
            scMain.Panel2.Controls.Add(scFavorites);
            scFavorites.Panel1.Controls.Add(lvFavorites);
            scFavorites.Panel2.Controls.Add(sc);
            this.Controls.Add(scMain);
            lvFile.Select();
            this.ResumeLayout(false);

            ResizeControl();

            if (Environment.GetCommandLineArgs().Length > 1 && Directory.Exists(Environment.GetCommandLineArgs()[1]))
                this.dirPath = Environment.GetCommandLineArgs()[1];
            else
                this.dirPath = Properties.Settings.Default.LastDirPath;
            if (!Directory.Exists(this.dirPath))
                this.dirPath = ShellInfoHelper.GetDownloadFolderPath();
            refreshFavorite();
        }

        public void refreshFavorite(){
            ListViewItem item;
            lvFavorites.Items.Clear();
            lvFavorites.SmallImageList.Images.Clear();
            lvFavorites.BeginUpdate();
            foreach(string favPath in favorites){
                if(Directory.Exists(favPath)||File.Exists(favPath)){
                    lvFavorites.SmallImageList.Images.Add(favPath, ShellInfoHelper.GetIconFromPath(favPath));
                    string displayPath = Path.GetFileName(favPath);
                    if (displayPath=="") displayPath = favPath;
                    item = new ListViewItem(displayPath, favPath);
                    item.SubItems[0].Tag = favPath;
                    lvFavorites.Items.Add(item);
                }
            }
            lvFavorites.EndUpdate();
        }

        public void addToFavorite(string path){
            favorites.Add(path);
            updateFaverite();
            refreshFavorite();
        }

        public void removeFromFavorite(string path){
            favorites = favorites.Where(x => x != path).Distinct().ToList();
            updateFaverite();
            refreshFavorite();
        }

        public void updateFaverite(){
            favorites = favorites.Where(x => Directory.Exists(x)||File.Exists(x)).Distinct().ToList();
            Properties.Settings.Default.Favorites = favorites;
            Properties.Settings.Default.Save();
        }

        public void refreshForm()
        {
            this.Text = this.watcher.Path = this.dirPath;
            tb.Text = this.dirPath.TrimEnd('\\') + "\\";
            DirectoryInfo dirInfo = new DirectoryInfo(this.dirPath);
            ListViewItem item;
            lvFolder.Items.Clear();
            lvFolder.SmallImageList.Images.Clear();
            lvFolder.BeginUpdate();
            string parentPath = Path.GetFullPath(Path.Combine(this.dirPath, ".."));
            lvFolder.SmallImageList.Images.Add(parentPath, ShellInfoHelper.GetIconFromPath(parentPath));
            item = new ListViewItem("..", parentPath);
            item.SubItems[0].Tag = parentPath;
            item.SubItems.Add(new DirectoryInfo(parentPath).LastWriteTime.ToString("yyyy/MM/dd hh:mm"));
            lvFolder.Items.Add(item);

            foreach (DirectoryInfo folder in dirInfo.GetDirectories().Where(x => (x.Attributes & FileAttributes.Hidden) == 0).OrderByDescending(x => x.LastWriteTime))
            {
                lvFolder.SmallImageList.Images.Add(folder.FullName, ShellInfoHelper.GetIconFromPath(folder.FullName));
                item = new ListViewItem(ShellInfoHelper.GetDisplayNameFromPath(folder.FullName), folder.FullName);
                item.SubItems[0].Tag = folder.FullName;
                item.SubItems.Add(folder.LastWriteTime.ToString("yyyy/MM/dd hh:mm"));
                lvFolder.Items.Add(item);
            }

            lvFile.Items.Clear();
            lvFile.SmallImageList.Images.Clear();
            lvFile.BeginUpdate();
            foreach (FileInfo file in dirInfo.GetFiles().Where(x => (x.Attributes & FileAttributes.Hidden) == 0).OrderByDescending(x => x.LastWriteTime))
            {
                if (file.Extension == "")
                {
                    lvFile.SmallImageList.Images.Add(file.FullName, ShellInfoHelper.GetIconFromPath(file.FullName));
                    item = new ListViewItem(ShellInfoHelper.GetDisplayNameFromPath(file.FullName), file.FullName);
                }
                else
                {
                    string imageKey = file.Extension;
                    if (!lvFile.SmallImageList.Images.ContainsKey(imageKey))
                    {
                        lvFile.SmallImageList.Images.Add(imageKey, ShellInfoHelper.GetIconFromPath(file.FullName));
                    }
                    item = new ListViewItem(file.Name, imageKey);
                }
                item.SubItems[0].Tag = file.FullName;
                item.SubItems.Add(Path.GetExtension(file.FullName));
                item.SubItems.Add(file.LastWriteTime.ToString("yyyy/MM/dd hh:mm"));
                item.SubItems.Add(FileSizeHelper.GetHumanReadableFileSize(file.Length));
                item.SubItems[3].Tag = (long)file.Length;

                lvFile.Items.Add(item);
            }

            lvFolder.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFolder.Columns[0].Width = lvFolder.ClientSize.Width - lvFolder.Columns[1].Width;
            lvFile.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFile.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFile.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFile.Columns[0].Width = lvFile.ClientSize.Width - lvFile.Columns[1].Width - lvFile.Columns[2].Width - lvFile.Columns[3].Width;
            sc.SplitterDistance = Math.Min(sc.Height / 2, Math.Max(lvFolder.Items.Count + 2, 4) * lvFolder.GetItemRect(0).Height);

            lvFolder.EndUpdate();
            lvFile.EndUpdate();
        }

        private void watcher_FileInfoChange(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            watcher.EnableRaisingEvents = false;
            updateFaverite();
            if (scMain.Visible) {
                refreshForm();
                refreshFavorite();
            }
            watcher.EnableRaisingEvents = true;
        }

        private void watcher_FileInfoChange(object sender, RenamedEventArgs e)
        {
            if (favorites.Contains(e.OldFullPath))
                favorites[favorites.IndexOf(e.OldFullPath)] = e.FullPath;
            updateFaverite();
            if (scMain.Visible) {
                refreshForm();
                refreshFavorite();
            }
        }

        private void form_Resize(object sender, EventArgs e)
        {
            lvFolder.Columns[0].Width = lvFolder.ClientSize.Width - lvFolder.Columns[1].Width;
            lvFile.Columns[0].Width = lvFile.ClientSize.Width - lvFile.Columns[1].Width - lvFile.Columns[2].Width - lvFile.Columns[3].Width;
            if (scMain.Visible)
            {
                sc.SplitterDistance = Math.Min(sc.Height / 2, Math.Max(lvFolder.Items.Count + 2, 4) * lvFolder.GetItemRect(0).Height);
            }
        }

        public void FoldWindow()
        {
            scMain.Hide();
            this.MinimumSize = new Size(150 * dpiScale, 0);
            this.MaximumSize = new Size(int.MaxValue, 0);
            this.ClientSize = new Size(fullSize.Width, 0);
        }

        public void UnfoldWindow()
        {
            scMain.Show();
            sc.Focus();
            this.MaximumSize = new Size(0, 0);
            this.ClientSize = fullSize;
            this.MinimumSize = new Size(150 * dpiScale, 150 * dpiScale);
            refreshForm();
            refreshFavorite();
        }

        private void form_ResizeEnd(object sender, EventArgs e)
        {
            if (scMain.Visible) fullSize = this.ClientSize;
        }

        private void form_DragEnter(object sender, DragEventArgs e)
        {
            if (allowFold)
                UnfoldWindow();
        }

        private void form_DragLeave(object sender, EventArgs e)
        {
            if (allowFold & !this.ClientRectangle.Contains(this.PointToClient(Control.MousePosition)))
                FoldWindow();
        }

        private void sc_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            this.Activate();
        }

        private void lvFavorites_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            this.Activate();
        }

        private void tb_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Copy;
            }
            this.Activate();
            tb.Select();
        }

        private void tb_DragDrop(object sender, DragEventArgs e)
        {
            string[] formats = e.Data.GetFormats(false);
            string path = "";

            if (formats.Contains("Text"))
            {
                path = (string)e.Data.GetData(DataFormats.Text);
            }
            else if (formats.Contains("FileDrop"))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                path = paths[0];
                if (File.Exists(path))
                    path = Path.GetDirectoryName(path);
            }
            if (Directory.Exists(path))
            {
                this.dirPath = path;
            }
        }

        private void lvFolder_DragOver(object sender, DragEventArgs e)
        {
            ListView lvFolder = sender as ListView;
            lvFolder.Focus();
            Point point = lvFolder.PointToClient(new Point(e.X, e.Y));
            ListViewItem item = lvFolder.GetItemAt(point.X, point.Y);
            if (item != null)
            {
                item.Focused = true;
            }
        }

        private void lv_DragOver(object sender, DragEventArgs e)
        {
            ListView lv = sender as ListView;
            lv.Focus();
        }

        private void lvFavorites_DragDrop(object sender, DragEventArgs e)
        {
            ListView lvFavorites = sender as ListView;
            lvFavorites.Focus();
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] sourceNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string sourceName in sourceNames)
                {
                    addToFavorite(sourceName);
                }
            }
        }

        private void lvFavorites_KeyDown(object sender, KeyEventArgs e)
        {
            ListView lvFavorites = sender as ListView;
            if(e.KeyCode == Keys.Delete)
            {
                foreach (ListViewItem item in lvFavorites.SelectedItems)
                {
                    string fullPath = (string)item.SubItems[0].Tag;
                    removeFromFavorite(fullPath);
                }
            }
        }

        private void lvBrowser_DragDrop(object sender, DragEventArgs e)
        {
            ListView lv = sender as ListView;
            Point point = lv.PointToClient(new Point(e.X, e.Y));
            ListViewItem item = lv.GetItemAt(point.X, point.Y);
            string destinationDirectory;
            if (item == null || File.Exists((string)item.SubItems[0].Tag))
                destinationDirectory = this.dirPath;
            else
                destinationDirectory = (string)item.SubItems[0].Tag;
            if (!Directory.Exists(destinationDirectory))
                return;
            lv.SelectedItems.Clear();
            if (item != null) item.Selected = true;
            string[] sourceNames = (string[])e.Data.GetData(DataFormats.FileDrop);
            watcher.EnableRaisingEvents = false;
            foreach (string sourceName in sourceNames)
            {
                string destinationName = Path.Combine(destinationDirectory, Path.GetFileName(sourceName));
                if (sourceName == destinationName) continue;
                FileSystemHelper.OperateFileSystemItem(sourceName, destinationName, DragDropEffects.Move);
            }
            refreshForm();
            refreshFavorite();
            watcher.EnableRaisingEvents = true;
        }

        private void lvBrowser_ItemDrag(object sender, ItemDragEventArgs e)
        {
            List<string> paths = new List<string>();
            foreach (ListViewItem item in lvFile.SelectedItems)
                paths.Add((string)item.SubItems[0].Tag);
            foreach (ListViewItem item in lvFolder.SelectedItems)
                paths.Add((string)item.SubItems[0].Tag);
            DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
            DoDragDrop(fileData, DragDropEffects.All);
        }

        private void lvFavorites_ItemDrag(object sender, ItemDragEventArgs e)
        {
            ListView lvFavorites = sender as ListView;
            List<string> paths = new List<string>();
            foreach (ListViewItem item in lvFavorites.SelectedItems)
                paths.Add((string)item.SubItems[0].Tag);
            DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
            DoDragDrop(fileData, DragDropEffects.All);
        }

        private void lvBrowser_KeyDown(object sender, KeyEventArgs e)
        {
            ListView lv = sender as ListView;
            if (e.Control && e.KeyCode == Keys.V)
            {
                StringCollection dropList = Clipboard.GetFileDropList();
                if (dropList == null || dropList.Count == 0) return;

                string destinationDirectory = this.dirPath;
                DragDropEffects effect = DragDropEffects.Copy;

                if (Clipboard.GetData("Preferred DropEffect") != null)
                {
                    MemoryStream effectMS = (MemoryStream)Clipboard.GetData("Preferred DropEffect");
                    byte[] effectByte = new byte[4];
                    effectMS.Read(effectByte, 0, effectByte.Length);
                    effect = (DragDropEffects)BitConverter.ToInt32(effectByte, 0);
                }

                foreach (string sourceName in dropList)
                {
                    string destinationName = Path.Combine(destinationDirectory, Path.GetFileName(sourceName));
                    FileSystemHelper.OperateFileSystemItem(sourceName, destinationName, effect);
                }
            }
            else if (e.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.X))
            {
                DragDropEffects effect = e.KeyCode == Keys.C ? DragDropEffects.Copy : DragDropEffects.Move;
                StringCollection dropList = new StringCollection();
                foreach (ListViewItem item in lvFolder.SelectedItems)
                    dropList.Add((string)item.SubItems[0].Tag);
                foreach (ListViewItem item in lvFile.SelectedItems)
                    dropList.Add((string)item.SubItems[0].Tag);
                DataObject data = new DataObject();
                data.SetFileDropList(dropList);
                data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)effect)));
                Clipboard.SetDataObject(data);
            }
            else if (e.KeyCode == Keys.Delete)
            {
                foreach (ListViewItem item in lv.SelectedItems)
                {
                    string fullPath = (string)item.SubItems[0].Tag;
                    if (e.Shift)
                        FileSystemHelper.DeleteFileSystemItem(fullPath, RecycleOption.DeletePermanently);
                    else
                        FileSystemHelper.DeleteFileSystemItem(fullPath, RecycleOption.SendToRecycleBin);
                }
            }
            else if (e.Alt && e.KeyCode == Keys.Up)
            {
                GoToParentDirectory();
            }
            else if(e.KeyCode == Keys.F2 && lv.SelectedItems.Count > 0)
            {
                lv.SelectedItems[0].BeginEdit();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Tab&&tb.Focused)
            {
                string target = this.dirPath;

                string text = tb.Text.Substring(0, tb.SelectionStart);
                string textRemain = tb.Text.Substring(tb.SelectionStart);
                if (!Path.IsPathRooted(text) || text.EndsWith(":")) return true;
                text = Path.GetFullPath(text);
                string dir = Path.GetDirectoryName(text);
                string query = Path.GetFileName(text);
                if (dir == null) dir = text;

                if (!Directory.Exists(dir))
                {
                    tb.Text = dir + textRemain;
                    tb.SelectionStart = tb.Text.Length - textRemain.Length;
                    return true;
                }
                else
                {
                    string queryRemain = textRemain.Split(new char[] { '/', '\\' }, 2).First();
                    string queryFull = query + queryRemain;
                    if (Directory.Exists(Path.Combine(dir, queryFull)))
                    {
                        textRemain = textRemain.Substring(queryRemain.Length).TrimStart(new char[] { '/', '\\' });
                        target = Path.Combine(dir, queryFull);
                    }
                    else
                    {
                        List<string> dirCandidate = new DirectoryInfo(dir).GetDirectories().Select(x => x.Name).Where(x => x.ToLower().StartsWith(query.ToLower())).ToList();
                        if (dirCandidate.Count != 1) return true;
                        target = Path.Combine(dir, dirCandidate.First());
                    }
                }
                this.dirPath = target;
                tb.Text += textRemain;
                tb.SelectionStart = tb.Text.Length - textRemain.Length;
                return true;
            }
            if (keyData == Keys.Enter&&tb.Focused)
            {
                if (Directory.Exists(tb.Text))
                {
                    string text = tb.Text.Substring(0, tb.SelectionStart);
                    this.dirPath = tb.Text;
                    if(!text.EndsWith(":")&&(File.Exists(text)||Directory.Exists(text))){
                        tb.SelectionStart = Path.GetFullPath(text).Length;
                    }else{
                        tb.SelectionStart = text.Length;
                    }
                }
                else
                {
                    tb.Text = this.dirPath.TrimEnd('\\') + "\\";
                    tb.SelectionStart = tb.Text.Length;
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void tb_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.V)
            {
                string query = Path.GetFileName(tb.Text);
                string dir = Path.GetDirectoryName(tb.Text);
                if (query == "" || !Directory.Exists(dir)) return;
                if (!this.dirPath.ToLower().StartsWith(dir.ToLower()))
                {
                    if (Directory.Exists(tb.Text))
                    {
                        this.dirPath = tb.Text;
                        tb.SelectionStart = tb.Text.Length;
                        return;
                    }
                }
            }
        }

        private void form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.O)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                ChangeDirDialog();
            }
            else if (e.Control && e.KeyCode == Keys.W)
            {
                Application.Exit();
            }
            else if (e.Control && e.KeyCode == Keys.E)
            {
                this.Location = new Point(Cursor.Position.X - this.ClientSize.Width / 2, Cursor.Position.Y - this.ClientSize.Height / 2);
            }
            else if (e.Control && e.KeyCode == Keys.D)
            {
                addToFavorite(this.dirPath);
            }
        }

        private void form_Load(object sender, EventArgs e)
        {
            this.Location = Properties.Settings.Default.WindowLocation;
            this.ClientSize = Properties.Settings.Default.FullSize;
            watcher.EnableRaisingEvents = true;
        }

        public void ChangeDirDialog()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = this.dirPath;
                if (
                    folderDialog.ShowDialog() == DialogResult.OK &&
                    folderDialog.SelectedPath[0] != '\\'
                )
                {
                    this.dirPath = folderDialog.SelectedPath;
                }
            }
        }

        public void GoToParentDirectory()
        {
            string path = Path.GetDirectoryName(this.dirPath);
            if (path == null)
                ChangeDirDialog();
            else
            {
                this.dirPath = path;
            }
        }

        public void OnMouseEnterWindow()
        {
            if (allowFold) UnfoldWindow();
        }

        public void OnMouseLeaveWindow()
        {
            if (allowFold) FoldWindow();
        }

        private void sc_DoubleClick(object sender, MouseEventArgs e)
        {
            SplitContainer sc = sender as SplitContainer;
            sc.SplitterDistance = Math.Min(sc.Height / 2, (lvFolder.Items.Count + 2) * lvFolder.GetItemRect(1).Height);
        }

        private void lvFolder_DoubleClick(object sender, MouseEventArgs e)
        {
            ListView lv = sender as ListView;
            ListViewItem item = lv.FocusedItem;
            string fullPath = (string)item.SubItems[0].Tag;
            if (
                e.Button == MouseButtons.Right && item.SubItems[0].Text == ".." ||
                this.dirPath == fullPath
            )
            {
                ChangeDirDialog();
            }
            else if (e.Button == MouseButtons.Left && Directory.Exists(fullPath))
            {
                this.dirPath = fullPath;
            }
        }

        private void lvFile_DoubleClick(object sender, MouseEventArgs e)
        {
            ListView lvFile = sender as ListView;
            ListViewItem item = lvFile.FocusedItem;
            string fullPath = (string)item.SubItems[0].Tag;
            if (e.Button == MouseButtons.Left && File.Exists(fullPath))
            {
                Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
            }
        }

        private void lvFolder_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            ListView lvFolder = sender as ListView;
            ListViewItem focusedItem = lvFolder.FocusedItem;
            if (e.Label == null) return;
            string sourceName = (string)focusedItem.SubItems[0].Tag;
            string destinationName = Path.Combine(this.dirPath, e.Label);
            if (focusedItem.SubItems[0].Text == "..")
            {
                e.CancelEdit = true;
                return;
            }
            if (sourceName != destinationName)
            {
                if (Directory.Exists(sourceName))
                {
                    if (File.Exists(destinationName))
                        MessageBox.Show("指定的文件夹名与已存在的文件重名。请指定其他名称。", "重命名文件夹");
                    else if (!Directory.Exists(destinationName))
                    {
                        FileSystem.MoveDirectory(sourceName, destinationName, UIOption.AllDialogs, UICancelOption.DoNothing);
                        if (Directory.Exists(sourceName))
                            e.CancelEdit = true;
                        else
                            focusedItem.SubItems[0].Tag = destinationName;
                    }
                    else if (
                            MessageBox.Show(
        $@"此目标已包含名为“{e.Label}”的文件夹。
如果任何文件使用相同的名称，将会询问你是否要替换这些文件。
你仍然将文件夹{sourceName}
与文件夹{destinationName}
合并吗？",
                                "确认文件夹替换",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question
                            ) == DialogResult.Yes
                        )
                    {
                        watcher.EnableRaisingEvents = false;
                        FileSystem.MoveDirectory(sourceName, destinationName, UIOption.AllDialogs, UICancelOption.DoNothing);
                        e.CancelEdit = true;
                        if (!Directory.Exists(sourceName))
                            lvFolder.Items.Remove(focusedItem);
                        watcher.EnableRaisingEvents = true;
                    }
                    else
                    {
                        e.CancelEdit = true;
                    }
                }
            }
        }
        private void lvFile_AfterLabelEdit(object sender, LabelEditEventArgs e)
        {
            ListView lvFile = sender as ListView;
            ListViewItem focusedItem = lvFile.FocusedItem;
            if (e.Label == null) return;
            string sourceName = (string)focusedItem.SubItems[0].Tag;
            string destinationName = Path.Combine(this.dirPath, e.Label);
            if (sourceName != destinationName)
            {
                if (File.Exists(sourceName))
                {
                    if (Directory.Exists(destinationName))
                        MessageBox.Show("指定的文件名与已存在的文件夹重名。请指定其他名称。", "重命名文件");
                    else
                    {
                        FileSystem.MoveFile(sourceName, destinationName, UIOption.AllDialogs, UICancelOption.DoNothing);
                        if (File.Exists(sourceName))
                            e.CancelEdit = true;
                        else
                            focusedItem.SubItems[0].Tag = destinationName;
                    }
                }
            }
        }

        private void lv_MouseClick(object sender, MouseEventArgs e)
        {
            ListView lv = sender as ListView;
            if (e.Button == MouseButtons.Right)
            {
                var item = lv.FocusedItem;
                if (item != null && item.Bounds.Contains(e.Location))
                {
                    FileInfo[] arrFI = new FileInfo[1];
                    arrFI[0] = new FileInfo((string)item.SubItems[0].Tag);
                    ctxMnu.ShowContextMenu(arrFI, this.PointToScreen(new Point(e.X, e.Y + 35)));
                }
            }
        }

        private void lvFavorites_DoubleClick(object sender, MouseEventArgs e)
        {
            ListView lvFavorites = sender as ListView;
            ListViewItem item = lvFavorites.FocusedItem;
            string fullPath = (string)item.SubItems[0].Tag;
            if (e.Button == MouseButtons.Left)
            {
                if (Directory.Exists(fullPath))
                {
                    this.dirPath = fullPath;
                }
                else if (File.Exists(fullPath))
                {
                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
            }
        }

        private void lv_MouseUp(object sender, MouseEventArgs e)
        {
            ListView lv = sender as ListView;
            if (e.Button == MouseButtons.Right)
            {
                if (lv.HitTest(e.Location).Location == ListViewHitTestLocations.None)
                {
                    cmsiExpDir.DropDown.Items.Clear();
                    List<string> paths = ShellApplication.GetExplorerPaths().Distinct().ToList();
                    paths.Sort();
                    foreach (string path in paths)
                        if (ShellInfoHelper.GetExactPathName(path) != this.dirPath)
                        {
                            ToolStripItem item = cmsiExpDir.DropDown.Items.Add(
                                ShellInfoHelper.GetDisplayNameFromPath(path),
                                ShellInfoHelper.GetIconFromPath(path),
                                new EventHandler(cmsiExpDir_Click)
                            );
                            item.ToolTipText = path;
                        }
                    cmsiHist.DropDown.Items.Clear();
                    foreach (string path in this.history)
                    {
                        ToolStripItem item = cmsiHist.DropDown.Items.Add(
                                ShellInfoHelper.GetDisplayNameFromPath(path),
                                ShellInfoHelper.GetIconFromPath(path),
                                new EventHandler(cmsiExpDir_Click)
                            );
                        item.ToolTipText = path;
                    }
                    cms.Show(Cursor.Position);
                }
            }
        }

        private void cms_GoTo_Click(object sender, EventArgs e)
        {
            ChangeDirDialog();
        }

        private void cmsiExpDir_Click(object sender, EventArgs e)
        {
            ToolStripItem item = sender as ToolStripItem;
            this.dirPath = item.ToolTipText;
        }

        private void lvBrowser_MouseDown(object sender, MouseEventArgs e)
        {
            ListView lv = sender as ListView;
            if (e.Button == MouseButtons.Left && e.Clicks == 2)
            {
                if (lv.HitTest(e.Location).Location == ListViewHitTestLocations.None)
                {
                    GoToParentDirectory();
                }
            }
        }

        private void lvFolder_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListView lv = sender as ListView;
            DirectoryListViewColumnSorter lvColumnSorter = lv.ListViewItemSorter as DirectoryListViewColumnSorter;
            if (e.Column == lvColumnSorter.SortColumn)
            {
                if (lvColumnSorter.Order == SortOrder.Ascending)
                    lvColumnSorter.Order = SortOrder.Descending;
                else
                    lvColumnSorter.Order = SortOrder.Ascending;
            }
            else
            {
                lvColumnSorter.SortColumn = e.Column;
                lvColumnSorter.Order = SortOrder.Ascending;
            }
            lv.Sort();
        }

        private void lvFile_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListView lv = sender as ListView;
            FileListViewColumnSorter lvColumnSorter = lv.ListViewItemSorter as FileListViewColumnSorter;
            if (e.Column == lvColumnSorter.SortColumn)
            {
                if (lvColumnSorter.Order == SortOrder.Ascending)
                    lvColumnSorter.Order = SortOrder.Descending;
                else
                    lvColumnSorter.Order = SortOrder.Ascending;
            }
            else
            {
                lvColumnSorter.SortColumn = e.Column;
                lvColumnSorter.Order = SortOrder.Ascending;
            }
            lv.Sort();
        }
    }
}
