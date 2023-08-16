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

public static class FileSizeHelper
{
    static readonly string[] SizeSuffixes = { "B ", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
    const long byteConversion = 1000;
    public static string GetHumanReadableFileSize(long value)
    {
        if (value == 0) { return "0"; }

        int mag = (int)Math.Log(value, byteConversion);
        int adjustedSize = (int)(value / Math.Pow(1000, mag));

        return string.Format("{0:d} {1}", adjustedSize, SizeSuffixes[mag]);
    }
}

public class NativeMethods
{
    public const uint SHGFI_DISPLAYNAME = 0x200;
    public const uint SHGFI_ICON = 0x100;
    public const uint SHGFI_LARGEICON = 0x0;
    public const uint SHGFI_SMALLICON = 0x1;
    public const uint SHGFI_SYSICONINDEX = 0x4000;

    [StructLayout(LayoutKind.Sequential)]
    public struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    };

    public static string GetDownloadFolderPath()
    {
        return SHGetKnownFolderPath(new Guid("374DE290-123F-4565-9164-39C4925E467B"), 0, IntPtr.Zero);
    }
    [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid,
                                                                                          uint dwFlags,
                                                                                          IntPtr hToken);

    [DllImport("user32.dll")]
    public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("shell32")]
    public static extern IntPtr SHGetFileInfo(string pszPath,
                                              uint dwFileAttributes,
                                              out SHFILEINFO psfi,
                                              uint cbSizeFileInfo,
                                              uint uFlags);

    public static Image GetIconFromPath(string pszPath)
    {
        SHFILEINFO info = new SHFILEINFO();
        IntPtr iconIntPtr = SHGetFileInfo(pszPath, 0, out info, (uint)Marshal.SizeOf(info), SHGFI_ICON | SHGFI_SMALLICON);
        return System.Drawing.Icon.FromHandle(info.hIcon).ToBitmap();
    }

    public static string GetDisplayNameFromPath(string pszPath)
    {
        SHFILEINFO info = new SHFILEINFO();
        IntPtr iconIntPtr = SHGetFileInfo(pszPath, 0, out info, (uint)Marshal.SizeOf(info), SHGFI_DISPLAYNAME);
        return info.szDisplayName;
    }
}

public class ListViewWithoutScrollBar : ListView
{
    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case 0x83: // WM_NCCALCSIZE
                int style = (int)GetWindowLongPtr64(this.Handle, GWL_STYLE);
                if ((style & WS_HSCROLL) == WS_HSCROLL)
                    style = style & ~WS_HSCROLL;
                SetWindowLongPtr64(this.Handle, GWL_STYLE, style);
                base.WndProc(ref m);
                break;
            default:
                base.WndProc(ref m);
                break;
        }
    }
    const int GWL_STYLE = -16;
    const int WS_VSCROLL = 0x200000;
    const int WS_HSCROLL = 0x100000;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr", CharSet = CharSet.Auto)]
    public static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", CharSet = CharSet.Auto)]
    public static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, int dwNewLong);
}

public class ListViewColumnSorter : IComparer
{
    public int SortColumn;
    public SortOrder Order;
    private CaseInsensitiveComparer ObjectCompare;
    public ListViewColumnSorter()
    {
        SortColumn = 0;
        Order = SortOrder.None;
        ObjectCompare = new CaseInsensitiveComparer();
    }
    public int Compare(object x, object y)
    {
        int compareResult;
        ListViewItem listviewX, listviewY;
        listviewX = (ListViewItem)x;
        listviewY = (ListViewItem)y;
        if (listviewX.SubItems[0].Text == "..") return -1;
        if (listviewY.SubItems[0].Text == "..") return 1;
        if (SortColumn == 2)
        {
            if ((long)listviewX.SubItems[SortColumn].Tag == -1) return 1;
            if ((long)listviewY.SubItems[SortColumn].Tag == -1) return -1;
            compareResult = ObjectCompare.Compare(
                listviewX.SubItems[SortColumn].Tag,
                listviewY.SubItems[SortColumn].Tag
            );
            if (SortColumn == 1) compareResult = -compareResult;
        }
        else
        {
            compareResult = ObjectCompare.Compare(
                listviewX.SubItems[SortColumn].Text,
                listviewY.SubItems[SortColumn].Text
            );
            if (SortColumn == 1) compareResult = -compareResult;
        }
        if (Order == SortOrder.Ascending)
            return compareResult;
        else if (Order == SortOrder.Descending)
            return (-compareResult);
        else
            return 0;
    }
}

namespace miniExplorer
{
    class DpiFactor
    {
        private float scaling;
        public DpiFactor(float scale)
        {
            scaling = scale;
        }
        public static int operator *(int pt, DpiFactor factor)
        {
            return (int)(pt * factor.scaling);
        }
    }
    public partial class miniBrowser : Form
    {
        // protected override CreateParams CreateParams {
        //     get {
        //         CreateParams param = base.CreateParams;
        //         // param.ExStyle |= 0x80;
        //         return param;
        //     }
        // }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Properties.Settings.Default.MiniSize = this.miniSize;
            Properties.Settings.Default.FullSize = this.fullSize;
            Properties.Settings.Default.LastDirPath = this.dirPath;
            Properties.Settings.Default.WindowLocation = this.Location;
            Properties.Settings.Default.FirstRun = false;
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);
            watcher.Dispose();
        }

        private string dirPath;
        private ListViewWithoutScrollBar lv;
        private TextBox tb;
        private FileSystemWatcher watcher;
        private ShellContextMenu ctxMnu = new ShellContextMenu();

        private Size fullSize;
        private Size miniSize;
        private DpiFactor dpiScale;

        private bool alwaysOpen = true;

        protected override void WndProc(ref Message m)
        {
            const int WM_NCLBUTTONDOWN = 0x00A1;
            const int WM_NCLBUTTONDBLCLK = 0x00A3;
            const int WM_NCRBUTTONDOWN = 0x00A4;
            const int WM_CONTEXTMENU = 0x007B;
            if (m.Msg == WM_CONTEXTMENU)
                m.Result = IntPtr.Zero;
            else if (m.Msg == WM_NCLBUTTONDBLCLK)
                ToggleSize();
            else if (m.Msg == WM_NCRBUTTONDOWN)
                ToggleSize();
            else
                base.WndProc(ref m);
        }

        public miniBrowser()
        {
            miniSize = Properties.Settings.Default.MiniSize;
            fullSize = Properties.Settings.Default.FullSize;
            bool isFirstRun = Properties.Settings.Default.FirstRun;

            watcher = new FileSystemWatcher()
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = false
            };
            watcher.Created += new FileSystemEventHandler(watcher_FileInfoChange);
            watcher.Changed += new FileSystemEventHandler(watcher_FileInfoChange);
            watcher.Deleted += new FileSystemEventHandler(watcher_FileInfoChange);
            watcher.Renamed += new RenamedEventHandler(watcher_FileInfoChange);

            this.FormBorderStyle = FormBorderStyle.SizableToolWindow;

            this.ClientSize = fullSize;
            if (isFirstRun)
            {
                this.dirPath = NativeMethods.GetDownloadFolderPath();
            }
            else
            {
                this.dirPath = Properties.Settings.Default.LastDirPath;
            }
            this.StartPosition = FormStartPosition.WindowsDefaultLocation;
            this.TopMost = true;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.KeyPreview = true;
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.dpiScale = new DpiFactor(this.DeviceDpi / 96.0f);
            this.MinimumSize = new Size(75 * dpiScale, 75 * this.dpiScale);

            tb = new TextBox()
            {
                AllowDrop = true,
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                Size = new Size(this.ClientSize.Width, 18 * this.dpiScale)
            };

            lv = new ListViewWithoutScrollBar()
            {
                View = View.Details,
                AllowDrop = true,
                // FullRowSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom | AnchorStyles.Right,
                Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 18 * this.dpiScale),
                Location = new Point(0, 18 * this.dpiScale),
                ListViewItemSorter = new ListViewColumnSorter()
            };
            lv.SmallImageList = new ImageList()
            {
                ImageSize = new Size(16 * this.dpiScale, 16 * this.dpiScale)
            };

            lv.Columns.Add("名称", -2, HorizontalAlignment.Left);
            lv.Columns.Add("修改时间", 0, HorizontalAlignment.Left);
            lv.Columns.Add("大小", 0, HorizontalAlignment.Right);
            refreshList();
            watcher.EnableRaisingEvents = true;

            tb.KeyDown += new KeyEventHandler(tb_KeyDown);
            tb.DragEnter += new DragEventHandler(tb_DragEnter);
            tb.DragDrop += new DragEventHandler(tb_DragDrop);
            tb.DpiChangedAfterParent += new EventHandler(tb_DpiChangedAfterParent);

            lv.DragEnter += new DragEventHandler(lv_DragEnter);
            lv.DragLeave += new EventHandler(lv_DragLeave);
            lv.DragOver += new DragEventHandler(lv_DragOver);
            lv.DragDrop += new DragEventHandler(lv_DragDrop);
            lv.ItemDrag += new ItemDragEventHandler(lv_ItemDrag);
            lv.MouseDoubleClick += new MouseEventHandler(lv_DoubleClick);
            lv.ColumnClick += new ColumnClickEventHandler(lv_ColumnClick);
            lv.KeyDown += new KeyEventHandler(lv_KeyDown);
            lv.MouseClick += new MouseEventHandler(lv_MouseClick);
            lv.DpiChangedAfterParent += new EventHandler(lv_DpiChangedAfterParent);

            this.MouseDown += new MouseEventHandler(form_MouseDown);
            this.ResizeEnd += new EventHandler(form_ResizeEnd);
            this.KeyDown += new KeyEventHandler(form_KeyDown);
            this.Resize += new EventHandler(form_Resize);
            this.DpiChanged += new DpiChangedEventHandler(form_DpiChanged);
            this.Shown += new EventHandler(form_Shown);

            this.Controls.Add(tb);
            this.Controls.Add(lv);
            lv.Select();
            this.ResumeLayout(false);
        }

        public void refreshList()
        {
            this.Text = dirPath;
            tb.Text = dirPath;
            this.watcher.Path = dirPath;
            lv.Items.Clear();
            lv.SmallImageList.Images.Clear();
            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(this.dirPath);
            ListViewItem item;
            lv.BeginUpdate();
            string parentPath = Path.GetFullPath(Path.Combine(this.dirPath, ".."));
            lv.SmallImageList.Images.Add(parentPath, NativeMethods.GetIconFromPath(parentPath));
            item = new ListViewItem("..", parentPath);
            item.SubItems[0].Tag = parentPath;
            item.SubItems.Add(new DirectoryInfo(parentPath).LastWriteTime.ToString("yyyy/MM/dd hh:mm"));
            item.SubItems.Add("");
            item.SubItems[2].Tag = (long)-1;
            lv.Items.Add(item);
            foreach (FileSystemInfo file in dir.GetFileSystemInfos().Where(x => (x.Attributes & FileAttributes.Hidden) == 0).OrderByDescending(x => x.LastWriteTime))
            {
                if (file is DirectoryInfo || file.Extension == "")
                {
                    lv.SmallImageList.Images.Add(file.FullName, NativeMethods.GetIconFromPath(file.FullName));
                    item = new ListViewItem(NativeMethods.GetDisplayNameFromPath(file.FullName), file.FullName);
                }
                else
                {
                    string imageKey = file.Extension;
                    if (!lv.SmallImageList.Images.ContainsKey(imageKey))
                    {
                        lv.SmallImageList.Images.Add(imageKey, NativeMethods.GetIconFromPath(file.FullName));
                    }
                    item = new ListViewItem(file.Name, imageKey);
                }
                item.SubItems[0].Tag = file.FullName;
                item.SubItems.Add(file.LastWriteTime.ToString("yyyy/MM/dd hh:mm"));
                if (file is DirectoryInfo)
                {
                    item.SubItems.Add("");
                    item.SubItems[2].Tag = (long)-1;
                }
                else if (file is FileInfo)
                {
                    item.SubItems.Add(FileSizeHelper.GetHumanReadableFileSize(((FileInfo)file).Length));
                    item.SubItems[2].Tag = (long)((FileInfo)file).Length;
                }
                lv.Items.Add(item);
            }
            lv.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
            lv.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
            lv.Columns[0].Width = this.ClientSize.Width - lv.Columns[1].Width - lv.Columns[2].Width - 40;
            lv.EndUpdate();
        }

        private void watcher_FileInfoChange(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            watcher.EnableRaisingEvents = false;
            refreshList();
            watcher.EnableRaisingEvents = true;
        }

        private void watcher_FileInfoChange(object sender, RenamedEventArgs e)
        {
            refreshList();
        }

        private void form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(this.Handle, 0xA1, 0x2, 0);
            }
        }

        private void form_Resize(object sender, EventArgs e)
        {
            lv.Columns[0].Width = this.ClientSize.Width - lv.Columns[1].Width - lv.Columns[2].Width - 40;
        }

        private void form_ResizeEnd(object sender, EventArgs e)
        {
            int w, h;
            if (alwaysOpen)
            {
                fullSize = this.ClientSize;
                w = Math.Min(fullSize.Width, miniSize.Width);
                h = Math.Min(fullSize.Height, miniSize.Height);
                miniSize = new Size(w, h);
            }
            else
            {
                miniSize = this.ClientSize;
                w = Math.Max(fullSize.Width, miniSize.Width);
                h = Math.Max(fullSize.Height, miniSize.Height);
                fullSize = new Size(w, h);
            }
        }

        private void lv_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            if (!alwaysOpen)
            {
                this.ClientSize = fullSize;
            }
            this.Activate();
            lv.Select();
        }

        private void lv_DragLeave(object sender, EventArgs e)
        {
            if (!alwaysOpen)
            {
                this.ClientSize = miniSize;
            }
        }

        public void ToggleSize()
        {
            alwaysOpen = !alwaysOpen;
            this.ClientSize = alwaysOpen ? fullSize : miniSize;
            this.Opacity = alwaysOpen ? 1 : 0.8;
        }

        private void tb_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Move;
            }
            this.Activate();
            tb.Select();
        }

        private void tb_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                if (Directory.Exists(file))
                {
                    this.dirPath = file;
                    refreshList();
                    return;
                }
            }
        }

        private void lv_DragOver(object sender, DragEventArgs e)
        {
            ListView lv = sender as ListView;
            Point point = lv.PointToClient(new Point(e.X, e.Y));
            ListViewItem item = lv.GetItemAt(point.X, point.Y);
            if (item != null)
            {
                item.Focused = true;
            }
        }

        private void lv_DragDrop(object sender, DragEventArgs e)
        {
            ListView lv = sender as ListView;
            Point point = lv.PointToClient(new Point(e.X, e.Y));
            ListViewItem item = lv.GetItemAt(point.X, point.Y);
            string fullPath = item == null ? this.dirPath : (string)item.SubItems[0].Tag;
            if (!Directory.Exists(fullPath)) return;
            lv.SelectedItems.Clear();
            if (item != null) item.Selected = true;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (string file in files)
            {
                if (File.Exists(file))
                {
                    FileSystem.MoveFile(file, Path.Combine(fullPath, Path.GetFileName(file)), UIOption.AllDialogs);
                }
                if (Directory.Exists(file))
                {
                    watcher.EnableRaisingEvents = false;
                    FileSystem.MoveDirectory(file, Path.Combine(fullPath, Path.GetFileName(file)), UIOption.AllDialogs);
                    refreshList();
                    watcher.EnableRaisingEvents = true;
                }
            }
        }

        private void lv_ItemDrag(object sender, ItemDragEventArgs e)
        {
            ListView lv = sender as ListView;
            List<string> paths = new List<string>();
            foreach (ListViewItem item in lv.SelectedItems)
            {
                paths.Add((string)item.SubItems[0].Tag);
            }
            DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
            DoDragDrop(fileData, DragDropEffects.All);
        }

        private void tb_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (Directory.Exists(tb.Text))
                {
                    this.dirPath = tb.Text;
                    refreshList();
                }
                else
                {
                    tb.Text = this.dirPath;
                }
                e.Handled = true;
                e.SuppressKeyPress = true;
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

        private void form_Shown(object sender, EventArgs e)
        {
            this.Location = Properties.Settings.Default.WindowLocation;
        }

        private void form_DpiChanged(object sender,
DpiChangedEventArgs e)
        {
            this.dpiScale = new DpiFactor(e.DeviceDpiNew / 96.0f);
            this.MinimumSize = new Size(75 * dpiScale, 75 * this.dpiScale);
        }

        private void lv_DpiChangedAfterParent(object sender, EventArgs e)
        {
            lv.SmallImageList.ImageSize = new Size(
                16 * this.dpiScale,
                16 * this.dpiScale
            );
            lv.Size = new Size(this.ClientSize.Width, this.ClientSize.Height - 18 * this.dpiScale);
            lv.Location = new Point(0, 18 * this.dpiScale);
            refreshList();
        }

        private void tb_DpiChangedAfterParent(object sender, EventArgs e)
        {
            tb.Size = new Size(this.ClientSize.Width, 18 * this.dpiScale);
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
                    refreshList();
                }
            }
        }

        private void title_DoubleClick()
        {
            ToggleSize();
        }

        private void title_RightClick()
        {
            ChangeDirDialog();
        }

        private void lv_DoubleClick(object sender, MouseEventArgs e)
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
            else if (e.Button == MouseButtons.Left)
            {
                if (Directory.Exists(fullPath))
                {
                    this.dirPath = fullPath;
                    refreshList();
                }
                else if (File.Exists(fullPath))
                {
                    Process.Start(new ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
            }
        }

        private void lv_KeyDown(object sender, KeyEventArgs e)
        {
            ListView lv = sender as ListView;
            if (e.Control && e.KeyCode == Keys.C)
            {
                StringCollection paths = new StringCollection();
                foreach (ListViewItem item in lv.SelectedItems)
                {
                    paths.Add((string)item.SubItems[0].Tag);
                }
                Clipboard.SetFileDropList(paths);
            }
            else if (e.KeyCode == Keys.Delete)
            {
                foreach (ListViewItem item in lv.SelectedItems)
                {
                    string fullPath = (string)item.SubItems[0].Tag;
                    try
                    {
                        if (e.Shift)
                        {
                            if (File.Exists(fullPath))
                                FileSystem.DeleteFile(fullPath, UIOption.AllDialogs, RecycleOption.DeletePermanently);
                            else if (Directory.Exists(fullPath))
                                FileSystem.DeleteDirectory(fullPath, UIOption.AllDialogs, RecycleOption.DeletePermanently);
                        }
                        else
                        {
                            if (File.Exists(fullPath))
                                FileSystem.DeleteFile(fullPath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                            else if (Directory.Exists(fullPath))
                                FileSystem.DeleteDirectory(fullPath, UIOption.AllDialogs, RecycleOption.SendToRecycleBin);
                        }
                    }
                    catch (OperationCanceledException) { }
                }
            }
        }

        private void lv_MouseClick(object sender, MouseEventArgs e)
        {
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

        private void lv_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            ListView lv = sender as ListView;
            ListViewColumnSorter lvwColumnSorter = lv.ListViewItemSorter as ListViewColumnSorter;
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                    lvwColumnSorter.Order = SortOrder.Descending;
                else
                    lvwColumnSorter.Order = SortOrder.Ascending;
            }
            else
            {
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }
            lv.Sort();
        }
    }
}
