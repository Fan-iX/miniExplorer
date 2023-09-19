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
    public class ListViewWithoutScrollBar : ListView
    {
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
        public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
        public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, int dwNewLong);

        const int GWL_STYLE = -16;
        const int WM_NCCALCSIZE = 0x83;
        const int WS_VSCROLL = 0x200000;
        const int WS_HSCROLL = 0x100000;

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_NCCALCSIZE:
                    int style = (int)GetWindowLongPtr64(this.Handle, GWL_STYLE);
                    if ((style & WS_HSCROLL) == WS_HSCROLL)
                        style &= ~WS_HSCROLL;
                    SetWindowLongPtr64(this.Handle, GWL_STYLE, style);
                    base.WndProc(ref m);
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }
    }

    public class DirectoryListViewColumnSorter : IComparer
    {
        public int SortColumn;
        public SortOrder Order;
        public DirectoryListViewColumnSorter()
        {
            SortColumn = 1;
            Order = SortOrder.None;
        }
        public int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;
            CaseInsensitiveComparer ObjectCompare = new CaseInsensitiveComparer();
            if (listviewX.SubItems[0].Text == "..") return -1;
            if (listviewY.SubItems[0].Text == "..") return 1;
            compareResult = ObjectCompare.Compare(
                    listviewX.SubItems[SortColumn].Text,
                    listviewY.SubItems[SortColumn].Text
                );
            if (SortColumn == 1) compareResult = -compareResult;
            if (Order == SortOrder.Ascending)
                return compareResult;
            else if (Order == SortOrder.Descending)
                return -compareResult;
            else
                return 0;
        }
    }

    public class FileListViewColumnSorter : IComparer
    {
        public int SortColumn;
        public SortOrder Order;
        public FileListViewColumnSorter()
        {
            SortColumn = 1;
            Order = SortOrder.None;
        }
        public int Compare(object x, object y)
        {
            int compareResult;
            ListViewItem listviewX, listviewY;
            listviewX = (ListViewItem)x;
            listviewY = (ListViewItem)y;
            CaseInsensitiveComparer ObjectCompare = new CaseInsensitiveComparer();
            if (SortColumn == 3)
            {
                compareResult = ObjectCompare.Compare(
                    listviewX.SubItems[SortColumn].Tag,
                    listviewY.SubItems[SortColumn].Tag
                );
            }
            else
            {
                compareResult = ObjectCompare.Compare(
                    listviewX.SubItems[SortColumn].Text,
                    listviewY.SubItems[SortColumn].Text
                );
            }
            if (SortColumn == 2) compareResult = -compareResult;
            if (Order == SortOrder.Ascending)
                return compareResult;
            else if (Order == SortOrder.Descending)
                return -compareResult;
            else
                return 0;
        }
    }
    public partial class miniBrowser : Form
    {
        private SplitContainer sc = new SplitContainer()
        {
            AllowDrop = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right,
            Orientation = Orientation.Horizontal,
            BorderStyle = BorderStyle.None,
            TabStop = false,
            SplitterWidth = 3
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
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
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
            tb.Size = new Size(this.ClientSize.Width, 18 * this.dpiScale);
            sc.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 18 * this.dpiScale);
            sc.Location = new Point(0, 18 * this.dpiScale);
            lvFolder.SmallImageList.ImageSize = new Size(
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
            if (sc.Visible) this.MinimumSize = new Size(150 * dpiScale, 150 * dpiScale);
            ResizeControl();
            if (sc.Visible) refreshForm();
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
                        this.Fold();
                    else
                        this.Unfold();
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
            lvFolder.ItemDrag += new ItemDragEventHandler(lv_ItemDrag);
            lvFolder.DragDrop += new DragEventHandler(lv_DragDrop);
            lvFolder.KeyDown += new KeyEventHandler(lv_KeyDown);
            lvFolder.MouseUp += new MouseEventHandler(lv_MouseUp);
            lvFolder.MouseDown += new MouseEventHandler(lv_MouseDown);
            lvFile.MouseClick += new MouseEventHandler(lv_MouseClick);
            lvFile.ItemDrag += new ItemDragEventHandler(lv_ItemDrag);
            lvFile.DragDrop += new DragEventHandler(lv_DragDrop);
            lvFile.KeyDown += new KeyEventHandler(lv_KeyDown);
            lvFile.MouseUp += new MouseEventHandler(lv_MouseUp);
            lvFile.MouseDown += new MouseEventHandler(lv_MouseDown);

            lvFolder.DragOver += new DragEventHandler(lvFolder_DragOver);
            lvFolder.MouseDoubleClick += new MouseEventHandler(lvFolder_DoubleClick);
            lvFolder.ColumnClick += new ColumnClickEventHandler(lvFolder_ColumnClick);
            lvFolder.AfterLabelEdit += new LabelEditEventHandler(lvFolder_AfterLabelEdit);

            lvFile.DragOver += new DragEventHandler(lvFile_DragOver);
            lvFile.MouseDoubleClick += new MouseEventHandler(lvFile_DoubleClick);
            lvFile.ColumnClick += new ColumnClickEventHandler(lvFile_ColumnClick);
            lvFile.AfterLabelEdit += new LabelEditEventHandler(lvFile_AfterLabelEdit);

            this.DragEnter += new DragEventHandler(form_DragEnter);
            this.DragLeave += new EventHandler(form_DragLeave);
            this.ResizeEnd += new EventHandler(form_ResizeEnd);
            this.KeyDown += new KeyEventHandler(form_KeyDown);
            this.Resize += new EventHandler(form_Resize);
            this.Load += new EventHandler(form_Load);

            this.Controls.Add(tb);
            this.Controls.Add(sc);
            lvFile.Select();
            this.ResumeLayout(false);

            ResizeControl();

            if (Environment.GetCommandLineArgs().Length > 1 && Directory.Exists(Environment.GetCommandLineArgs()[1]))
                this.dirPath = Environment.GetCommandLineArgs()[1];
            else
                this.dirPath = Properties.Settings.Default.LastDirPath;
            if (!Directory.Exists(this.dirPath))
                this.dirPath = ShellInfoHelper.GetDownloadFolderPath();
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
            lvFolder.Columns[0].Width = this.ClientSize.Width - lvFolder.Columns[1].Width - 40;
            lvFile.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFile.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFile.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFile.Columns[0].Width = this.ClientSize.Width - lvFile.Columns[1].Width - lvFile.Columns[2].Width - lvFile.Columns[3].Width - 40;
            sc.SplitterDistance = Math.Min(sc.Height / 2, Math.Max(lvFolder.Items.Count + 2, 4) * lvFolder.GetItemRect(0).Height);

            lvFolder.EndUpdate();
            lvFile.EndUpdate();
        }

        private void watcher_FileInfoChange(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            watcher.EnableRaisingEvents = false;
            if (sc.Visible) refreshForm();
            watcher.EnableRaisingEvents = true;
        }

        private void watcher_FileInfoChange(object sender, RenamedEventArgs e)
        {
            if (sc.Visible) refreshForm();
        }

        private void form_Resize(object sender, EventArgs e)
        {
            lvFolder.Columns[0].Width = this.ClientSize.Width - lvFolder.Columns[1].Width - 40;
            lvFile.Columns[0].Width = this.ClientSize.Width - lvFile.Columns[1].Width - lvFile.Columns[2].Width - lvFile.Columns[3].Width - 40;
            if (sc.Visible)
            {
                sc.SplitterDistance = Math.Min(sc.Height / 2, Math.Max(lvFolder.Items.Count + 2, 4) * lvFolder.GetItemRect(0).Height);
            }
        }

        public void Fold()
        {
            sc.Hide();
            tb.Hide();
            this.MinimumSize = new Size(150 * dpiScale, 0);
            this.MaximumSize = new Size(int.MaxValue, 0);
            this.ClientSize = new Size(fullSize.Width, 0);
        }

        public void Unfold()
        {
            tb.Show();
            sc.Show();
            sc.Focus();
            this.MaximumSize = new Size(0, 0);
            this.ClientSize = fullSize;
            this.MinimumSize = new Size(150 * dpiScale, 150 * dpiScale);
            refreshForm();
        }

        private void form_ResizeEnd(object sender, EventArgs e)
        {
            if (sc.Visible)
                fullSize = this.ClientSize;
        }

        private void form_DragEnter(object sender, DragEventArgs e)
        {
            if (allowFold)
                Unfold();
        }

        private void form_DragLeave(object sender, EventArgs e)
        {
            if (allowFold & !this.ClientRectangle.Contains(this.PointToClient(Control.MousePosition)))
                Fold();
        }

        private void sc_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            this.Activate();
            lvFile.Select();
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
            Point point = lvFolder.PointToClient(new Point(e.X, e.Y));
            ListViewItem item = lvFolder.GetItemAt(point.X, point.Y);
            if (item != null)
            {
                item.Focused = true;
            }
        }

        private void lvFile_DragOver(object sender, DragEventArgs e)
        {
            lvFile.Focus();
        }

        private void lv_DragDrop(object sender, DragEventArgs e)
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
            watcher.EnableRaisingEvents = true;
        }

        private void lv_ItemDrag(object sender, ItemDragEventArgs e)
        {
            List<string> paths = new List<string>();
            foreach (ListViewItem item in lvFile.SelectedItems)
                paths.Add((string)item.SubItems[0].Tag);
            foreach (ListViewItem item in lvFolder.SelectedItems)
                paths.Add((string)item.SubItems[0].Tag);
            DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
            DoDragDrop(fileData, DragDropEffects.All);
        }

        private void lv_KeyDown(object sender, KeyEventArgs e)
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
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Tab)
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
            if (keyData == Keys.Enter)
            {
                if (Directory.Exists(tb.Text))
                {
                    string text = tb.Text.Substring(0, tb.SelectionStart);
                    this.dirPath = tb.Text;
                    tb.SelectionStart = Path.GetFullPath(text).Length;
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
            if (e.Control && e.KeyCode == Keys.W)
            {
                Application.Exit();
            }
            if (e.Control && e.KeyCode == Keys.E)
            {
                this.Location = new Point(Cursor.Position.X - this.ClientSize.Width / 2, Cursor.Position.Y - this.ClientSize.Height / 2);
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
            if (allowFold) Unfold();
        }

        public void OnMouseLeaveWindow()
        {
            if (allowFold) Fold();
        }

        private void sc_DoubleClick(object sender, MouseEventArgs e)
        {
            sc.SplitterDistance = Math.Min(sc.Height / 2, (lvFolder.Items.Count + 2) * lvFolder.GetItemRect(1).Height);
        }

        private void lvFolder_DoubleClick(object sender, MouseEventArgs e)
        {
            ListViewItem item = lvFolder.FocusedItem;
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
            if (sourceName != destinationName)
            {
                if (Directory.Exists(sourceName))
                {
                    if (File.Exists(destinationName))
                        MessageBox.Show("指定的文件夹名与已存在的文件重名。请指定其他名称。", "重命名文件夹");
                    else if (!Directory.Exists(destinationName))
                    {
                        watcher.EnableRaisingEvents = false;
                        FileSystem.MoveDirectory(sourceName, destinationName, UIOption.AllDialogs, UICancelOption.DoNothing);
                        if (Directory.Exists(sourceName))
                            e.CancelEdit = true;
                        else
                            focusedItem.SubItems[0].Tag = destinationName;
                        watcher.EnableRaisingEvents = true;
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
                this.Text = lv.HitTest(e.Location).Location.ToString();
                var item = lv.FocusedItem;
                if (item != null && item.Bounds.Contains(e.Location))
                {
                    FileInfo[] arrFI = new FileInfo[1];
                    arrFI[0] = new FileInfo((string)item.SubItems[0].Tag);
                    ctxMnu.ShowContextMenu(arrFI, this.PointToScreen(new Point(e.X, e.Y + 35)));
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

        private void lv_MouseDown(object sender, MouseEventArgs e)
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
