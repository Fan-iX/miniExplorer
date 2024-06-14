using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using ShellApp;
using System.Timers;
using System.Reflection;

namespace miniExplorer
{
    public class Global
    {
        // public static ShellContextMenu ctxMnu = new ShellContextMenu();
    }

    public class Consts
    {
        public static readonly Dictionary<DragDropEffects, string> dragEffectDict = new Dictionary<DragDropEffects, string>()
        {
            { DragDropEffects.All, "自动" },
            { DragDropEffects.Copy, "复制" },
            { DragDropEffects.Move, "移动" }
        };
    }

    public class BrowserForm : PocketForm
    {
        public ShellContextMenu scmItemContextMenu = new ShellContextMenu();
        private class AddressBarTextBox : TextBox
        {
            public BrowserForm Form { get => FindForm() as BrowserForm; }
            public AddressBarTextBox()
            {
                AllowDrop = true;
                AutoSize = false;
                TabStop = false;
                Margin = new Padding(0);
                Dock = DockStyle.Fill;
                AutoCompleteMode = AutoCompleteMode.Append;
                AutoCompleteSource = AutoCompleteSource.CustomSource;
                DragEnter += new DragEventHandler((object sender, DragEventArgs e) =>
                {
                    e.Effect = DragDropEffects.All;
                    Form.Activate();
                    Select();
                });
                DragDrop += new DragEventHandler((object sender, DragEventArgs e) =>
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
                    Form.ActiveBrowser.NavigateTo(path);
                });
                KeyUp += new KeyEventHandler((object sender, KeyEventArgs e) =>
                {
                    if (e.Control && e.KeyCode == Keys.V)
                    {
                        int selectionStart = SelectionStart;
                        int textLength = Text.Length;
                        Form.ActiveBrowser.NavigateTo(Text);
                        SelectionStart = selectionStart == textLength ? Text.Length : selectionStart;
                    }
                });
            }
        }
        private TableLayoutPanel tlpMain = new TableLayoutPanel()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            TabStop = false,
        };
        private TableLayoutPanel tlpAddressBar = new TableLayoutPanel()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            AutoSize = false,
            TabStop = false,
            Margin = new Padding(0),
        };
        private AddressBarTextBox tbAddressBar = new AddressBarTextBox();
        private SplitContainer scFavorites = new SplitContainer()
        {
            AllowDrop = true,
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BorderStyle = BorderStyle.None,
            TabStop = false,
            SplitterWidth = 3,
            Panel1Collapsed = true,
            FixedPanel = FixedPanel.Panel1,
            Margin = new Padding(0),
        };
        private ListViewFavorites lvFavorites = new ListViewFavorites();
        public BrowserTabControl tcBrowser = new BrowserTabControl();

        public BrowserTabPage ActiveTabPage { get => tcBrowser.SelectedTab as BrowserTabPage; }
        public BrowserContainer ActiveBrowser { get => tcBrowser.ActiveBrowser; }
        public void UpdateText()
        {
            Text = tbAddressBar.Text = ActiveBrowser.dirPath.TrimEnd('\\') + "\\";
            string tabText = ShellInfoHelper.GetDisplayName(ActiveBrowser.dirPath);
            if (tabText.Length > 15) tabText = tabText.Substring(0, 8) + "..." + tabText.Substring(tabText.Length - 4);
            tcBrowser.SelectedTab.Text = tabText;
        }

        private class SettingsContextMenu : ContextMenuStrip
        {
            public BrowserForm Form;
            public SettingsContextMenu()
            {
                DropShadowEnabled = false;
                ToolStripMenuItem cmsmDragEffect = (ToolStripMenuItem)Items.Add("拖出效果");
                cmsmDragEffect.DropDown.DropShadowEnabled = false;
                cmsmDragEffect.DropDown.Closing += new ToolStripDropDownClosingEventHandler((object sender, ToolStripDropDownClosingEventArgs e) =>
                {
                    if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                        e.Cancel = true;
                });
                foreach (var kv in Consts.dragEffectDict)
                {
                    ToolStripMenuItem item = (ToolStripMenuItem)cmsmDragEffect.DropDownItems.Add(kv.Value);
                    item.Tag = kv.Key;
                    item.Click += new EventHandler((object sender, EventArgs e) =>
                    {
                        foreach (ToolStripMenuItem menuItem in cmsmDragEffect.DropDownItems)
                        {
                            menuItem.Checked = false;
                        }
                        item.Checked = true;
                        Properties.Settings.Default.DragEffect = (DragDropEffects)item.Tag;
                    });
                }
                ToolStripMenuItem cmsmDropEffect = (ToolStripMenuItem)Items.Add("拖入效果");
                cmsmDropEffect.DropDown.DropShadowEnabled = false;
                cmsmDropEffect.DropDown.Closing += new ToolStripDropDownClosingEventHandler((object sender, ToolStripDropDownClosingEventArgs e) =>
                {
                    if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                        e.Cancel = true;
                });
                foreach (var kv in Consts.dragEffectDict)
                {
                    ToolStripMenuItem item = (ToolStripMenuItem)cmsmDropEffect.DropDownItems.Add(kv.Value);
                    item.Tag = kv.Key;
                    item.Click += new EventHandler((object sender, EventArgs e) =>
                    {
                        foreach (ToolStripMenuItem menuItem in cmsmDropEffect.DropDownItems)
                        {
                            menuItem.Checked = false;
                        }
                        item.Checked = true;
                        Properties.Settings.Default.DropEffect = (DragDropEffects)item.Tag;
                    });
                }
                ToolStripMenuItem cmsiShowHidden = (ToolStripMenuItem)Items.Add("显示隐藏文件");
                cmsiShowHidden.Click += new EventHandler((object sender, EventArgs e) =>
                {
                    cmsiShowHidden.Checked = Properties.Settings.Default.ShowHidden = !Properties.Settings.Default.ShowHidden;
                    if (Form != null)
                        Form.ActiveBrowser.UpdateListView();
                });
                Closing += new ToolStripDropDownClosingEventHandler((object sender, ToolStripDropDownClosingEventArgs e) =>
                {
                    if (e.CloseReason == ToolStripDropDownCloseReason.ItemClicked)
                        e.Cancel = true;
                });
                Opening += new CancelEventHandler((object sender, CancelEventArgs e) =>
                {
                    foreach (ToolStripMenuItem menuItem in cmsmDragEffect.DropDownItems)
                    {
                        menuItem.Checked = (DragDropEffects)menuItem.Tag == Properties.Settings.Default.DragEffect;
                    }
                    foreach (ToolStripMenuItem menuItem in cmsmDropEffect.DropDownItems)
                    {
                        menuItem.Checked = (DragDropEffects)menuItem.Tag == Properties.Settings.Default.DropEffect;
                    }
                    cmsiShowHidden.Checked = Properties.Settings.Default.ShowHidden;
                });
            }
        }

        private class AdressBarButton : Button
        {
            public AdressBarButton(string text)
            {
                Text = text;
                TextAlign = ContentAlignment.MiddleCenter;
                Margin = new Padding(0);
                AutoSizeMode = AutoSizeMode.GrowAndShrink;
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                ContextMenuStrip = new ContextMenuStrip();
            }
        }

        private class AdressBarCheckButton : CheckBox
        {
            public AdressBarCheckButton(string text)
            {
                Text = text;
                TextAlign = ContentAlignment.MiddleCenter;
                Margin = new Padding(0);
                Appearance = Appearance.Button;
                FlatStyle = FlatStyle.Flat;
                FlatAppearance.BorderSize = 0;
                ContextMenuStrip = new ContextMenuStrip();
            }
        }
        AdressBarCheckButton btnFavorites = new AdressBarCheckButton("🔖");
        AdressBarButton btnBack = new AdressBarButton("←");
        AdressBarButton btnSettings = new AdressBarButton("=");

        public BrowserForm()
        {
            MinimumSize = new Size(150 * DpiScale, 150 * DpiScale);
            KeyPreview = true;
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("miniExplorer.Resources.miniExplorer.ico"))
            {
                Icon = new Icon(stream);
            }

            tlpAddressBar.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpAddressBar.Controls.Add(btnFavorites);
            tlpAddressBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpAddressBar.Controls.Add(btnBack);
            tlpAddressBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpAddressBar.Controls.Add(tbAddressBar);
            tlpAddressBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            tlpAddressBar.Controls.Add(btnSettings);
            tlpAddressBar.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            scFavorites.Panel1.Controls.Add(lvFavorites);
            scFavorites.Panel2.Controls.Add(tcBrowser);
            tlpMain.Controls.Add(tlpAddressBar);
            tlpMain.Controls.Add(scFavorites);
            Controls.Add(tlpMain);

            scFavorites.SplitterMoved += new SplitterEventHandler((object sender, SplitterEventArgs e) =>
            {
                if (!scFavorites.Panel1Collapsed)
                {
                    Properties.Settings.Default.FavoritesPanelWidth = scFavorites.Panel1.Width / DpiScale;
                }
            });

            btnFavorites.CheckedChanged += new EventHandler((object sender, EventArgs e) =>
            {
                scFavorites.Panel1Collapsed = !btnFavorites.Checked;
                ActiveBrowser.ResizeWidget();
            });
            btnFavorites.MouseUp += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    ContextMenuStrip cms = btnFavorites.ContextMenuStrip;
                    cms.Items.Clear();
                    List<string> paths = ShellApplicationHelper.GetExplorerPaths().Distinct().ToList();
                    paths.Sort();
                    foreach (string path in paths)
                    {
                        ToolStripItem item = cms.Items.Add(
                            ShellInfoHelper.GetDisplayName(path),
                            ShellInfoHelper.GetIconFromPath(path),
                            new EventHandler((object s, EventArgs ea) =>
                            {
                                ActiveBrowser.NavigateTo(path);
                            })
                        );
                        item.ToolTipText = path;
                    }
                    cms.Show(btnFavorites, new Point(0, btnFavorites.Height));
                }
            });

            btnBack.Click += new EventHandler((object sender, EventArgs e) =>
            {
                ActiveBrowser.NavigateBack();
            });
            btnBack.MouseUp += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    ContextMenuStrip cms = btnBack.ContextMenuStrip;
                    cms.Items.Clear();
                    foreach (string path in ActiveBrowser.History)
                    {
                        if (!Directory.Exists(path)) continue;
                        ToolStripItem item = cms.Items.Add(
                            ShellInfoHelper.GetDisplayName(path),
                            ShellInfoHelper.GetIconFromPath(path),
                            new EventHandler((object s, EventArgs ea) =>
                            {
                                ActiveBrowser.NavigateTo(path);
                            })
                        );
                        item.ToolTipText = path;
                    }
                    cms.Show(btnBack, new Point(0, btnBack.Height));
                }
            });

            btnSettings.ContextMenuStrip.Dispose();
            btnSettings.ContextMenuStrip = new SettingsContextMenu()
            {
                Form = this
            };
            btnSettings.Click += new EventHandler((object sender, EventArgs e) =>
            {
                btnSettings.ContextMenuStrip.Show(btnSettings, new Point(0, btnSettings.Height));
            });

            for (int i = 0; i < Properties.Settings.Default.TabPaths.Count;)
            {
                if (!Directory.Exists(Properties.Settings.Default.TabPaths[i]))
                {
                    Properties.Settings.Default.TabPaths.RemoveAt(i);
                    if (i < Properties.Settings.Default.SelectedIndex)
                    {
                        Properties.Settings.Default.SelectedIndex--;
                    }
                }
                else
                {
                    i++;
                }
            }

            if (Environment.GetCommandLineArgs().Length > 1)
            {
                Properties.Settings.Default.SelectedIndex = 0;
                foreach (string path in Environment.GetCommandLineArgs().Skip(1))
                {
                    tcBrowser.AddBrowser(path);
                }
            }
            if (Properties.Settings.Default.TabPaths.Count == 0)
            {
                tcBrowser.AddBrowser(ShellInfoHelper.GetDownloadFolderPath());
            }
            foreach (string path in Properties.Settings.Default.TabPaths)
            {
                tcBrowser.AddBrowser(path);
            }
            tcBrowser.SelectedIndex = Properties.Settings.Default.SelectedIndex;

            Load += new EventHandler((object sender, EventArgs e) =>
            {
                Location = Properties.Settings.Default.WindowLocation;
                UnfoldedSize = Properties.Settings.Default.UnfoldedSize;
                ClientSize = UnfoldedSize;
                scFavorites.SplitterDistance = Properties.Settings.Default.FavoritesPanelWidth * DpiScale;
                scFavorites.Panel1Collapsed = !Properties.Settings.Default.ShowFavorites;
                btnFavorites.Checked = Properties.Settings.Default.ShowFavorites;
                lvFavorites.UpdateItems();
                UpdateText();
                ResizeWidget();
            });
            Resize += new EventHandler((object sender, EventArgs e) =>
            {
                if (!Folded)
                {
                    var lvFile = ActiveBrowser.lvFile;
                    var lvFolder = ActiveBrowser.lvFolder;
                    lvFolder.Columns[0].Width = lvFolder.ClientSize.Width - lvFolder.Columns[1].Width;
                    lvFile.Columns[0].Width = lvFile.ClientSize.Width - lvFile.Columns[1].Width - lvFile.Columns[2].Width - lvFile.Columns[3].Width;
                    ActiveBrowser.SplitterDistance = Math.Min(ActiveBrowser.Height / 2, Math.Max(lvFolder.Items.Count + 2, 4) * lvFolder.GetItemRect(0).Height);
                }
            });
            KeyDown += new KeyEventHandler((object sender, KeyEventArgs e) =>
            {
                if (e.Control && e.KeyCode == Keys.W)
                {
                    if (tcBrowser.TabPages.Count > 2)
                    {
                        tcBrowser.SelectedTab.Dispose();
                        ActiveTabPage.Activate();
                    }
                }
                else if (e.Control && e.KeyCode == Keys.E)
                {
                    Location = new Point(Cursor.Position.X - ClientSize.Width / 2, Cursor.Position.Y - ClientSize.Height / 2);
                }
                else if (e.Control && e.KeyCode == Keys.D)
                {
                    if (lvFavorites.Items.ContainsKey(ActiveBrowser.dirPath))
                    {
                        lvFavorites.RemoveItem(ActiveBrowser.dirPath);
                    }
                    else
                    {
                        lvFavorites.AddItem(ActiveBrowser.dirPath);
                    }
                }
                else if (e.Control && e.KeyCode == Keys.O)
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    ActiveBrowser.PickDir();
                }
                else if (e.Control && e.KeyCode == Keys.R || e.KeyCode == Keys.F5)
                {
                    ActiveBrowser.UpdateListView();
                    ActiveBrowser.ResizeWidget();
                }
                else if (e.Control && e.KeyCode == Keys.F)
                {
                    tbAddressBar.Focus();
                }
            });
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var tb = tbAddressBar;
            if (keyData == Keys.Tab && tb.Focused)
            {
                string target = ActiveBrowser.dirPath;

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
                ActiveBrowser.NavigateTo(target);
                tb.Text += textRemain;
                tb.SelectionStart = tb.Text.Length - textRemain.Length;
                return true;
            }
            if (keyData == Keys.Enter && tb.Focused)
            {
                if (Directory.Exists(Environment.ExpandEnvironmentVariables(tb.Text)))
                {
                    string text = tb.Text.Substring(0, tb.SelectionStart);
                    ActiveBrowser.NavigateTo(tb.Text);
                    if (!text.EndsWith(":") && (File.Exists(text) || Directory.Exists(text)))
                    {
                        tb.SelectionStart = Path.GetFullPath(text).Length;
                    }
                    else
                    {
                        tb.SelectionStart = text.Length;
                    }
                }
                else
                {
                    tb.Text = ActiveBrowser.dirPath.TrimEnd('\\') + "\\";
                    tb.SelectionStart = tb.Text.Length;
                }
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            scFavorites.SplitterDistance = scFavorites.SplitterDistance * e.DeviceDpiNew / e.DeviceDpiOld;
            base.OnDpiChanged(e);
            btnSettings.ContextMenuStrip.Dispose();
            btnSettings.ContextMenuStrip = new SettingsContextMenu()
            {
                Form = this
            };
            btnBack.ContextMenuStrip.Dispose();
            btnBack.ContextMenuStrip = new ContextMenuStrip();
            btnFavorites.ContextMenuStrip.Dispose();
            btnFavorites.ContextMenuStrip = new ContextMenuStrip();
            if (!Folded) this.MinimumSize = new Size(150 * DpiScale, 150 * DpiScale);
            ResizeWidget();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            Properties.Settings.Default.UnfoldedSize = new Size(ClientSize.Width, Math.Max(UnfoldedSize.Height, 150 * DpiScale));
            Properties.Settings.Default.TabPaths = tcBrowser.dirPaths;
            Properties.Settings.Default.SelectedIndex = tcBrowser.SelectedIndex;
            Properties.Settings.Default.WindowLocation = Location;
            Properties.Settings.Default.ShowFavorites = !scFavorites.Panel1Collapsed;
            Properties.Settings.Default.Save();
            base.OnFormClosing(e);
        }

        private void ResizeWidget()
        {
            tlpAddressBar.Height = 24 * DpiScale;
            btnBack.Height = btnBack.Width =
            btnSettings.Height = btnSettings.Width =
            btnFavorites.Height = btnFavorites.Width =
                24 * DpiScale;
            ActiveBrowser.ResizeWidget();
            ActiveBrowser.ReloadImageList();
            lvFavorites.ReloadImageList();
        }
    }

    class ListViewFavoritesItem : ListViewItem
    {
        private FileSystemWatcher watcher;
        public ListViewFavoritesItem(string path)
        {
            Name = path;
            Text = ShellInfoHelper.GetDisplayName(path);
            ImageKey = path;
            if (Path.GetDirectoryName(path) == null) return;
            watcher = new FileSystemWatcher()
            {
                Path = Path.GetDirectoryName(path),
                Filter = Path.GetFileName(path)
            };
            watcher.Deleted += new FileSystemEventHandler((object sender, FileSystemEventArgs e) =>
            {
                ListViewFavorites lv = ListView as ListViewFavorites;
                lv.RefreshLayoutLock();
                Remove();
            });
            watcher.Renamed += new RenamedEventHandler((object sender, RenamedEventArgs e) =>
            {
                if (e.ChangeType == WatcherChangeTypes.Renamed)
                {
                    Name = e.FullPath;
                    SubItems[0].Text = e.Name;
                    watcher.Filter = Path.GetFileName(e.FullPath);
                }
            });
            watcher.EnableRaisingEvents = true;
        }

        override public void Remove()
        {
            if (watcher != null)
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
            }
            base.Remove();
        }
    }

    public class ListViewFavorites : ListViewWithoutHorizontalScrollBar
    {
        public BrowserForm Form { get => FindForm() as BrowserForm; }
        public List<string> Favorites { get => Properties.Settings.Default.Favorites; }
        public System.Timers.Timer layoutLockTimer = new System.Timers.Timer(100);

        public ListViewFavorites()
        {
            View = View.List;
            AllowDrop = true;
            Dock = DockStyle.Fill;
            SmallImageList = new ImageList();
            DragOver += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                Focus();
                Point point = PointToClient(new Point(e.X, e.Y));
                ListViewItem item = GetItemAt(point.X, point.Y);
                if (item == null) return;
                InsertionMark.AppearsAfterItem = point.Y > item.Bounds.Top + item.Bounds.Height / 2;
                InsertionMark.Index = item.Index;
            });
            DragLeave += new EventHandler((object sender, EventArgs e) =>
            {
                InsertionMark.Index = -1;
            });
            DragEnter += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Copy;
                }
                Form.Activate();
            });
            DragDrop += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                Focus();
                InsertionMark.Index = -1;
                Point point = PointToClient(new Point(e.X, e.Y));
                ListViewItem item = GetItemAt(point.X, point.Y);
                int insertIndex = Items.Count;
                if (item != null)
                {
                    insertIndex = item.Index;
                    if (point.Y > item.Bounds.Top + item.Bounds.Height / 2)
                        insertIndex += 1;
                }
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    string[] sourcePaths = (string[])e.Data.GetData(DataFormats.FileDrop);
                    for (int i = 0; i < Items.Count; i++)
                    {
                        item = Items[i];
                        if (sourcePaths.Contains(item.Name))
                        {
                            Favorites.Remove(item.Name);
                            item.Remove();
                            if (i < insertIndex) insertIndex -= 1;
                        }
                    }
                    foreach (string sourceName in sourcePaths)
                    {
                        Favorites.Insert(insertIndex, sourceName);
                        if (!SmallImageList.Images.ContainsKey(sourceName))
                            SmallImageList.Images.Add(sourceName, ShellInfoHelper.GetIconFromPath(sourceName));
                        Items.Insert(insertIndex, new ListViewFavoritesItem(sourceName));
                        insertIndex += 1;
                    }
                    Properties.Settings.Default.Save();
                }
            });
            MouseDoubleClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                string path = FocusedItem.Name;
                if (e.Button == MouseButtons.Left)
                {
                    if (Directory.Exists(path))
                    {

                        if (Control.ModifierKeys.HasFlag(Keys.Control))
                        {
                            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                        }
                        else
                        {
                            Form.ActiveBrowser.NavigateTo(path);
                        }
                    }
                    else if (File.Exists(path))
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    }
                }
            });
            KeyDown += new KeyEventHandler((object sender, KeyEventArgs e) =>
            {
                if (e.KeyCode == Keys.Delete)
                {
                    foreach (ListViewItem item in SelectedItems)
                    {
                        SmallImageList.Images.RemoveByKey(item.ImageKey);
                        Favorites.Remove(item.Name);
                        item.Remove();
                    }
                    Properties.Settings.Default.Save();
                }
            });
            ItemDrag += new ItemDragEventHandler((object sender, ItemDragEventArgs e) =>
            {
                List<string> paths = new List<string>();
                foreach (ListViewItem item in SelectedItems)
                    paths.Add(item.Name);
                DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
                DoDragDrop(fileData, DragDropEffects.All);
            });
            MouseClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    if (FocusedItem != null && FocusedItem.Bounds.Contains(e.Location))
                    {
                        List<string> paths = new List<string>();
                        foreach (ListViewItem item in SelectedItems)
                            paths.Add(item.Name);
                        Form.scmItemContextMenu.ShowContextMenu(paths.Select(x => new FileInfo(x)).ToArray(), this.PointToScreen(new Point(e.X, e.Y + 35)));
                    }
                }
            });
        }

        public void ReloadImageList()
        {
            SmallImageList.ImageSize = new Size(
                16 * Form.DpiScale,
                16 * Form.DpiScale
            );
            SmallImageList.Images.Clear();
            foreach (ListViewItem item in Items)
            {
                string path = item.Name;
                string imageKey = item.ImageKey;
                if (!SmallImageList.Images.ContainsKey(path))
                    SmallImageList.Images.Add(imageKey, ShellInfoHelper.GetIconFromPath(path));
                item.ImageKey = imageKey;
            }
        }

        public void UpdateItems()
        {
            Items.Clear();
            BeginUpdate();
            foreach (string path in Favorites)
            {
                if (Directory.Exists(path) || File.Exists(path))
                {
                    Items.Add(new ListViewFavoritesItem(path));
                }
            }
            EndUpdate();
        }

        public void AddItem(string path)
        {
            if (Items.ContainsKey(path))
            {
                Items[path].Selected = true;
                return;
            }
            Favorites.Add(path);
            if (!SmallImageList.Images.ContainsKey(path))
                SmallImageList.Images.Add(path, ShellInfoHelper.GetIconFromPath(path));
            Items.Add(new ListViewFavoritesItem(path));
            Properties.Settings.Default.Save();
        }

        public void RemoveItem(string path)
        {
            if (Items.ContainsKey(path))
            {
                Items[path].Remove();
                Favorites.Remove(path);
                Properties.Settings.Default.Save();
            }
        }

        public void RefreshLayoutLock()
        {
            if (!Updating)
            {
                BeginUpdate();
                ElapsedEventHandler evh = null;
                evh = new ElapsedEventHandler((object s, ElapsedEventArgs ev) =>
                {
                    EndUpdate();
                    layoutLockTimer.Elapsed -= evh;
                    layoutLockTimer.Stop();
                });
                layoutLockTimer.Elapsed += evh;
                layoutLockTimer.Start();
            }
            layoutLockTimer.Stop();
            layoutLockTimer.Start();
        }
    }

    public class BrowserTabPage : TabPage
    {
        public BrowserContainer Browser = new BrowserContainer();
        public BrowserForm Form { get => FindForm() as BrowserForm; }
        public BrowserTabPage(string dirPath)
        {
            Browser.dirPath = dirPath;
            Browser.History.Add(dirPath);
            Controls.Add(Browser);
            Text = ShellInfoHelper.GetDisplayName(Browser.dirPath);
        }

        public void Activate()
        {
            (Parent as TabControl).SelectedTab = this;
            Browser.UpdateListView();
            Browser.ResizeWidget();
            Form.UpdateText();
        }
    }

    public class BrowserTabControl : TabControlW
    {
        public BrowserForm Form { get => FindForm() as BrowserForm; }
        public BrowserContainer ActiveBrowser { get => SelectedTab == null ? null : (SelectedTab as BrowserTabPage).Browser; }

        private TabPage draggedTab = null;
        private TabPage contextTab = null;
        private ContextMenuStrip tpCtxMnu = new ContextMenuStrip()
        {
            DropShadowEnabled = false
        };
        public List<string> dirPaths
        {
            get
            {
                List<string> result = new List<string>();
                foreach (TabPage tabPage in TabPages)
                {
                    if (tabPage != tpPlus)
                        result.Add(((BrowserTabPage)tabPage).Browser.dirPath);
                }
                return result;
            }
        }
        private TabPage tpPlus = new TabPage()
        {
            Text = "+",
        };
        public BrowserTabControl()
        {
            Dock = DockStyle.Fill;
            Margin = new Padding(0);
            AllowDrop = true;
            TabPages.Add(tpPlus);
            Selecting += new TabControlCancelEventHandler((object sender, TabControlCancelEventArgs e) =>
            {
                if (e.TabPage == tpPlus)
                {
                    e.Cancel = true;
                }
            });

            tpCtxMnu.Items.Add("删除页").Click += new EventHandler((object sender, EventArgs e) =>
            {
                if (contextTab == SelectedTab)
                {
                    int index = TabPages.IndexOf(contextTab) - 1;
                    if (index < 0) index = 1;
                    BrowserTabPage btp = TabPages[index] as BrowserTabPage;
                    btp.Activate();
                }
                contextTab.Dispose();
            });

            MouseDown += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                if (e.Button == MouseButtons.Left && e.Clicks == 1)
                {
                    if (PointedTab == tpPlus)
                    {
                        AddBrowser(ActiveBrowser.dirPath);
                    }
                    else if (PointedTab != null)
                    {
                        draggedTab = PointedTab;
                        BrowserTabPage btp = PointedTab as BrowserTabPage;
                        btp.Activate();
                        DataObject fileData = new DataObject(DataFormats.FileDrop, new string[] { btp.Browser.dirPath });
                        DoDragDrop(fileData, DragDropEffects.All);
                    }
                }
            });

            MouseClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                TabPage tp = PointedTab;
                if (e.Button == MouseButtons.Right)
                {
                    if (tp != null && tp != tpPlus)
                    {
                        contextTab = tp;
                        tpCtxMnu.Show(this, e.Location);
                    }
                }
            });

            MouseDoubleClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                TabPage tp = PointedTab as TabPage;
                if (e.Button == MouseButtons.Left && e.Clicks == 2)
                {
                    if (PointedTab == tpPlus) return;
                    if (TabPages.Count > 2)
                    {
                        tp.Dispose();
                        Form.ActiveTabPage.Activate();
                    }
                }
            });

            DragOver += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                TabPage tp = PointedTab as TabPage;
                if (draggedTab == null)
                {
                    e.Effect = DragDropEffects.All;
                    if (tp != null && tp != SelectedTab && tp != tpPlus)
                    {
                        BrowserTabPage btp = PointedTab as BrowserTabPage;
                        btp.Activate();
                    }
                }
                else
                {
                    if (tp == null || tp == tpPlus) e.Effect = DragDropEffects.None;
                    else e.Effect = DragDropEffects.Move;
                }
            });

            QueryContinueDrag += new QueryContinueDragEventHandler((object sender, QueryContinueDragEventArgs e) =>
            {
                if ((e.Action & DragAction.Drop) == DragAction.Drop)
                {
                    if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
                        draggedTab = null;
                }
            });

            DragDrop += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                TabPage tp = PointedTab as TabPage;
                if (e.Data.GetDataPresent(DataFormats.FileDrop) && tp == tpPlus)
                {
                    string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);
                    foreach (string path in paths)
                    {
                        if (File.Exists(path))
                            AddBrowser(Path.GetDirectoryName(path));
                        else if (Directory.Exists(path))
                            AddBrowser(path);
                    }
                }
                else if (draggedTab != null && tp != null && tp != draggedTab)
                {
                    if (tp == tpPlus && tp == draggedTab) return;
                    int index = TabPages.IndexOf(tp);
                    TabPages.Remove(draggedTab);
                    TabPages.Insert(index, draggedTab);
                    SelectedTab = draggedTab;
                }
                draggedTab = null;
            });
        }
        public void AddBrowser(string dirPath)
        {
            if (!Directory.Exists(dirPath)) return;
            IntPtr h = Handle;
            BrowserTabPage tabPage = new BrowserTabPage(dirPath);
            TabPages.Insert(TabPages.IndexOf(tpPlus), tabPage);
            tabPage.Activate();
        }
    }

    public class BrowserContainer : SplitContainer
    {
        private string _dirPath;
        public string dirPath
        {
            get => _dirPath;
            set
            {
                string path = Environment.ExpandEnvironmentVariables(value);
                if (!Directory.Exists(path)) return;
                _dirPath = ShellInfoHelper.GetExactPathName(path);
                if (Path.GetDirectoryName(_dirPath) == null)
                {
                    watcher.EnableRaisingEvents = false;
                }
                else
                {
                    watcher.Path = Path.GetDirectoryName(_dirPath);
                    watcher.EnableRaisingEvents = true;
                }
            }
        }
        public System.Timers.Timer layoutLockTimer = new System.Timers.Timer(100);
        public List<string> History = new List<string>();
        public BrowserForm Form { get => FindForm() as BrowserForm; }
        public ListViewDirectoryBrowser lvFolder = new ListViewDirectoryBrowser();
        public ListViewFileBrowser lvFile = new ListViewFileBrowser();
        public ImageList imgList = new ImageList();

        public FileSystemWatcher watcher = new FileSystemWatcher()
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = false
        };

        public List<ListViewItem> SelectedItems
        {
            get
            {
                List<ListViewItem> result = new List<ListViewItem>();
                foreach (ListViewItem item in lvFolder.SelectedItems)
                {
                    result.Add(item);
                }
                foreach (ListViewItem item in lvFile.SelectedItems)
                {
                    result.Add(item);
                }
                return result;
            }
        }

        public BrowserContainer()
        {
            AllowDrop = true;
            Dock = DockStyle.Fill;
            Orientation = Orientation.Horizontal;
            BorderStyle = BorderStyle.None;
            TabStop = false;
            Margin = new Padding(0);
            SplitterWidth = 1;
            FixedPanel = FixedPanel.Panel1;
            DragEnter += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = DragDropEffects.Move;
                }
                Form.Activate();
            });
            lvFolder.SmallImageList = imgList;
            lvFile.SmallImageList = imgList;
            Panel1.Controls.Add(lvFolder);
            Panel2.Controls.Add(lvFile);
            lvFolder.MouseClick += new MouseEventHandler(lv_MouseClick);
            lvFile.MouseClick += new MouseEventHandler(lv_MouseClick);
            lvFolder.MouseUp += new MouseEventHandler(lv_MouseUp);
            lvFile.MouseUp += new MouseEventHandler(lv_MouseUp);
            lvFolder.ItemDrag += new ItemDragEventHandler(lv_ItemDrag);
            lvFile.ItemDrag += new ItemDragEventHandler(lv_ItemDrag);
            lvFolder.MouseDown += new MouseEventHandler(lv_MouseDown);
            lvFile.MouseDown += new MouseEventHandler(lv_MouseDown);
            lvFolder.KeyDown += new KeyEventHandler(lv_KeyDown);
            lvFile.KeyDown += new KeyEventHandler(lv_KeyDown);

            MouseDoubleClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                SplitterDistance = Math.Min(Height / 2, (lvFolder.Items.Count + 2) * lvFolder.GetItemRect(1).Height);
            });
            watcher.Error += new ErrorEventHandler((object sender, ErrorEventArgs e) =>
            {
                RefreshLayoutLock();
            });
            watcher.Deleted += new FileSystemEventHandler((object sender, FileSystemEventArgs e) =>
            {
                RefreshLayoutLock();
            });
            watcher.Changed += new FileSystemEventHandler((object sender, FileSystemEventArgs e) =>
            {
                RefreshLayoutLock();
            });
            watcher.Created += new FileSystemEventHandler((object sender, FileSystemEventArgs e) =>
            {
                RefreshLayoutLock();
            });
            watcher.Renamed += new RenamedEventHandler((object sender, RenamedEventArgs e) =>
            {
                RefreshLayoutLock();
            });
        }

        public void RefreshLayoutLock()
        {
            if (!Updating)
            {
                BeginUpdate();
                ElapsedEventHandler evh = null;
                evh = new ElapsedEventHandler((object s, ElapsedEventArgs ev) =>
                {
                    UpdateListView();
                    ResizeWidget();
                    EndUpdate();
                    layoutLockTimer.Elapsed -= evh;
                    layoutLockTimer.Stop();
                });
                layoutLockTimer.Elapsed += evh;
                layoutLockTimer.Start();
            }
            layoutLockTimer.Stop();
            layoutLockTimer.Start();
        }

        public void ReloadImageList()
        {
            imgList.ImageSize = new Size(
                16 * Form.DpiScale,
                16 * Form.DpiScale
            );
            imgList.Images.Clear();
            foreach (ListViewItem item in lvFolder.Items)
            {
                string path = item.Name;
                string imageKey = item.ImageKey;
                if (!imgList.Images.ContainsKey(path))
                    imgList.Images.Add(path, ShellInfoHelper.GetIconFromPath(path));
                item.ImageKey = imageKey;
            }
            foreach (ListViewItem item in lvFile.Items)
            {
                string path = item.Name;
                string imageKey = item.ImageKey;
                if (!imgList.Images.ContainsKey(path))
                    imgList.Images.Add(path, ShellInfoHelper.GetIconFromPath(path));
                item.ImageKey = imageKey;
            }
        }

        public void UpdateListView()
        {
            if (!Directory.Exists(dirPath))
            {
                string path = dirPath;
                while (!Directory.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                    if (path == null)
                    {
                        path = ShellInfoHelper.GetDownloadFolderPath();
                    }
                }
                NavigateTo(path);
            }
            DirectoryInfo dirInfo = new DirectoryInfo(this.dirPath);
            imgList.Images.Clear();
            lvFolder.Items.Clear();

            lvFolder.BeginUpdate();
            string parentPath = Path.GetFullPath(Path.Combine(this.dirPath, ".."));
            imgList.Images.Add(parentPath, ShellInfoHelper.GetIconFromPath(parentPath));
            ListViewItem item = new ListViewItem("..")
            {
                Name = parentPath,
                ImageKey = parentPath,
            };
            item.SubItems.Add(new DirectoryInfo(parentPath).LastWriteTime.ToString("yyyy/MM/dd hh:mm"));
            lvFolder.Items.Add(item);
            foreach (DirectoryInfo fi in dirInfo.GetDirectories().Where(x => Properties.Settings.Default.ShowHidden || (x.Attributes & FileAttributes.Hidden) == 0).OrderByDescending(x => x.LastWriteTime))
            {
                imgList.Images.Add(fi.FullName, ShellInfoHelper.GetIconFromPath(fi.FullName));
                item = new ListViewItem(ShellInfoHelper.GetDisplayName(fi.FullName))
                {
                    Name = fi.FullName,
                    ImageKey = fi.FullName,
                };
                item.SubItems.Add(fi.LastWriteTime.ToString("yyyy/MM/dd hh:mm"));
                lvFolder.Items.Add(item);
            }
            lvFolder.EndUpdate();

            lvFile.Items.Clear();
            lvFile.BeginUpdate();
            foreach (FileInfo fi in dirInfo.GetFiles().Where(x => Properties.Settings.Default.ShowHidden || (x.Attributes & FileAttributes.Hidden) == 0).OrderByDescending(x => x.LastWriteTime))
            {
                imgList.Images.Add(fi.FullName, ShellInfoHelper.GetIconFromPath(fi.FullName));
                item = new ListViewItem(fi.Name)
                {
                    Name = fi.FullName,
                    ImageKey = fi.FullName,
                };
                item.SubItems.Add(Path.GetExtension(fi.FullName));
                item.SubItems.Add(fi.LastWriteTime.ToString("yyyy/MM/dd hh:mm"));
                item.SubItems.Add(FileSizeHelper.GetHumanReadableFileSize(fi.Length));
                item.SubItems[3].Tag = (long)fi.Length;

                lvFile.Items.Add(item);
            }
            lvFile.EndUpdate();
        }
        public void ResizeWidget()
        {
            lvFolder.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFolder.Columns[0].Width = lvFolder.ClientSize.Width - lvFolder.Columns[1].Width;
            lvFile.AutoResizeColumn(1, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFile.AutoResizeColumn(2, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFile.AutoResizeColumn(3, ColumnHeaderAutoResizeStyle.ColumnContent);
            lvFile.Columns[0].Width = lvFile.ClientSize.Width - lvFile.Columns[1].Width - lvFile.Columns[2].Width - lvFile.Columns[3].Width;
            SplitterDistance = Math.Min(Height / 2, Math.Max(lvFolder.Items.Count + 2, 4) * lvFolder.GetItemRect(0).Height);
        }

        public void NavigateTo(string path)
        {
            dirPath = path;
            History.Insert(0, dirPath);
            History = History.Distinct().Where(x => Directory.Exists(x)).Take(10).ToList();
            UpdateListView();
            ResizeWidget();
            Form.UpdateText();
        }

        public void NavigateBack()
        {

            History.Add(History[0]);
            History.RemoveAt(0);
            History = History.Where(x => Directory.Exists(x)).ToList();
            if (History.Count == 0) return;
            dirPath = History[0];
            UpdateListView();
            ResizeWidget();
            Form.UpdateText();
        }
        public bool Updating = false;
        public void BeginUpdate()
        {
            Updating = true;
            lvFolder.BeginUpdate();
            lvFile.BeginUpdate();
        }
        public void EndUpdate()
        {
            lvFolder.EndUpdate();
            lvFile.EndUpdate();
            Updating = false;
        }

        private void lv_MouseUp(object sender, MouseEventArgs e)
        {
            ListView lv = sender as ListView;
            if (e.Button == MouseButtons.Right && lv.HitTest(e.Location).Location == ListViewHitTestLocations.None)
            {
                Form.scmItemContextMenu.ShowContextMenu(new DirectoryInfo[] { new DirectoryInfo(dirPath) }, Cursor.Position);
            }
        }

        private void lv_MouseClick(object sender, MouseEventArgs e)
        {
            ListView lv = sender as ListView;
            if (e.Button == MouseButtons.Right)
            {
                ListViewItem item = lv.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    List<string> paths = new List<string>();
                    foreach (ListViewItem selectedItem in SelectedItems)
                    {
                        paths.Add(selectedItem.Name);
                    }
                    Form.scmItemContextMenu.ShowContextMenu(paths.Select(x => new FileInfo(x)).ToArray(), Cursor.Position);
                }
            }
            else if (e.Button == MouseButtons.Left && !ModifierKeys.HasFlag(Keys.Control))
            {
                if (lv == lvFolder)
                    lvFile.SelectedItems.Clear();
                if (lv == lvFile)
                    lvFolder.SelectedItems.Clear();
            }
        }

        private void lv_MouseDown(object sender, MouseEventArgs e)
        {
            ListView lv = sender as ListView;
            if (e.Button == MouseButtons.Left)
            {
                if (lv.HitTest(e.Location).Location == ListViewHitTestLocations.None)
                {
                    if (e.Clicks == 1 && !ModifierKeys.HasFlag(Keys.Control))
                    {
                        lvFolder.SelectedItems.Clear();
                        lvFile.SelectedItems.Clear();
                    }
                    else if (e.Clicks == 2)
                    {
                        GoToParentDirectory();
                    }
                }
            }
        }

        private void lv_ItemDrag(object sender, ItemDragEventArgs e)
        {
            List<string> paths = new List<string>();
            foreach (ListViewItem item in SelectedItems)
            {
                paths.Add(item.Name);
            }
            DataObject fileData = new DataObject(DataFormats.FileDrop, paths.ToArray());
            DoDragDrop(fileData, Properties.Settings.Default.DragEffect);
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
            else if (e.Control && e.KeyCode == Keys.A)
            {
                foreach (ListViewItem item in lvFolder.Items)
                {
                    if (item.Text != "..")
                        item.Selected = true;
                }
                foreach (ListViewItem item in lvFile.Items)
                {
                    item.Selected = true;
                }
            }
            else if (e.Control && (e.KeyCode == Keys.C || e.KeyCode == Keys.X))
            {
                DragDropEffects effect = e.KeyCode == Keys.C ? DragDropEffects.Copy : DragDropEffects.Move;
                StringCollection dropList = new StringCollection();
                foreach (ListViewItem item in SelectedItems)
                    dropList.Add(item.Name);
                DataObject data = new DataObject();
                data.SetFileDropList(dropList);
                data.SetData("Preferred DropEffect", new MemoryStream(BitConverter.GetBytes((int)effect)));
                Clipboard.SetDataObject(data);
            }
            else if (e.KeyCode == Keys.Delete)
            {
                foreach (ListViewItem item in SelectedItems)
                {
                    string path = item.Name;
                    if (e.Shift)
                        FileSystemHelper.DeleteFileSystemItem(path, RecycleOption.DeletePermanently);
                    else
                        FileSystemHelper.DeleteFileSystemItem(path, RecycleOption.SendToRecycleBin);
                }
            }
            else if (e.Alt && e.KeyCode == Keys.Up)
            {
                GoToParentDirectory();
            }
            else if (e.KeyCode == Keys.Back)
            {
                NavigateBack();
            }
            else if (e.KeyCode == Keys.F2 && lv.SelectedItems.Count > 0)
            {
                lv.FocusedItem.BeginEdit();
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                layoutLockTimer.Dispose();
                watcher.Dispose();
            }
            base.Dispose(disposing);
        }

        public void GoToParentDirectory()
        {
            string path = Path.GetDirectoryName(this.dirPath);
            if (path == null)
                PickDir();
            else
            {
                NavigateTo(path);
            }
        }

        public void PickDir()
        {
            using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
            {
                folderDialog.SelectedPath = dirPath;
                if (
                    folderDialog.ShowDialog() == DialogResult.OK &&
                    folderDialog.SelectedPath[0] != '\\'
                )
                {
                    NavigateTo(folderDialog.SelectedPath);
                }
            }
        }
    }

    public class ListViewBrowser : ListViewWithoutHorizontalScrollBar
    {
        public BrowserContainer container { get => Parent.Parent as BrowserContainer; }
        public string dirPath { get => container.dirPath; }
        public ListViewBrowser()
        {
            View = View.Details;
            AllowDrop = true;
            LabelEdit = true;
            Margin = new Padding(0);
            Dock = DockStyle.Fill;
            // FullRowSelect = true,
            DragOver += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                Focus();
            });
            DragEnter += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                {
                    e.Effect = Properties.Settings.Default.DropEffect;
                }
                Focus();
            });
        }
    }

    public class ListViewFileBrowser : ListViewBrowser
    {
        public ListViewFileBrowser()
        {
            Columns.Add("文件", -2, HorizontalAlignment.Left);
            Columns.Add("类型", 0, HorizontalAlignment.Left);
            Columns.Add("修改时间", 0, HorizontalAlignment.Left);
            Columns.Add("大小", 0, HorizontalAlignment.Right);

            ListViewItemSorter = new FileListViewColumnSorter();
            MouseDoubleClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                ListViewItem item = FocusedItem;
                string path = item.Name;
                if (e.Button == MouseButtons.Left && File.Exists(path))
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                }
            });
            DragDrop += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                string destinationDirectory = dirPath;
                if (!Directory.Exists(destinationDirectory))
                    return;

                string[] sourceNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string sourceName in sourceNames)
                {
                    string destinationName = Path.Combine(destinationDirectory, Path.GetFileName(sourceName));
                    if (sourceName == destinationName) continue;
                    FileSystemHelper.OperateFileSystemItem(sourceName, destinationName, Properties.Settings.Default.DropEffect);
                }
            });
            AfterLabelEdit += new LabelEditEventHandler((object sender, LabelEditEventArgs e) =>
            {
                if (e.Label == null) return;
                string sourceName = FocusedItem.Name;
                string destinationName = Path.Combine(dirPath, e.Label);
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
                        }
                    }
                }
            });
            ColumnClick += new ColumnClickEventHandler((object sender, ColumnClickEventArgs e) =>
            {
                FileListViewColumnSorter sorter = ListViewItemSorter as FileListViewColumnSorter;
                if (e.Column == sorter.SortColumn)
                {
                    if (sorter.Order == SortOrder.Ascending)
                        sorter.Order = SortOrder.Descending;
                    else
                        sorter.Order = SortOrder.Ascending;
                }
                else
                {
                    sorter.SortColumn = e.Column;
                    sorter.Order = SortOrder.Ascending;
                }
                Sort();
            });
        }
    }

    public class ListViewDirectoryBrowser : ListViewBrowser
    {
        public ListViewDirectoryBrowser()
        {
            ListViewItemSorter = new DirectoryListViewColumnSorter();
            Columns.Add("文件夹", -2, HorizontalAlignment.Left);
            Columns.Add("修改时间", 0, HorizontalAlignment.Left);
            DragOver += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                Focus();
                Point point = PointToClient(new Point(e.X, e.Y));
                ListViewItem item = GetItemAt(point.X, point.Y);
                if (!SelectedItems.Contains(item) || SelectedItems.Count > 1)
                    SelectedItems.Clear();
                if (item != null)
                {
                    item.Focused = true;
                    item.Selected = true;
                }
            });
            DragDrop += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                Point point = PointToClient(new Point(e.X, e.Y));
                ListViewItem item = GetItemAt(point.X, point.Y);
                string destinationDirectory = item == null ? dirPath : item.Name;
                if (!Directory.Exists(destinationDirectory))
                    return;
                if (item != null) item.Selected = true;

                string[] sourceNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                foreach (string sourceName in sourceNames)
                {
                    string destinationName = Path.Combine(destinationDirectory, Path.GetFileName(sourceName));
                    if (sourceName == destinationName) continue;
                    FileSystemHelper.OperateFileSystemItem(sourceName, destinationName, Properties.Settings.Default.DropEffect);
                }
            });
            AfterLabelEdit += new LabelEditEventHandler((object sender, LabelEditEventArgs e) =>
            {
                if (e.Label == null) return;
                string sourceName = FocusedItem.Name;
                string destinationName = Path.Combine(dirPath, e.Label);
                if (FocusedItem.SubItems[0].Text == "..")
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
                            FileSystem.MoveDirectory(sourceName, destinationName, UIOption.AllDialogs, UICancelOption.DoNothing);
                            e.CancelEdit = true;
                        }
                        else
                        {
                            e.CancelEdit = true;
                        }
                    }
                }
            });
            MouseDoubleClick += new MouseEventHandler((object sender, MouseEventArgs e) =>
            {
                string path = FocusedItem.Name;
                if (e.Button == MouseButtons.Left)
                {
                    if (dirPath == path)
                    {
                        container.PickDir();
                    }
                    else if (Directory.Exists(path))
                    {
                        if (Control.ModifierKeys.HasFlag(Keys.Control))
                        {
                            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                        }
                        else
                        {
                            container.NavigateTo(path);
                        }
                    }
                }
            });
            ColumnClick += new ColumnClickEventHandler((object sender, ColumnClickEventArgs e) =>
            {
                DirectoryListViewColumnSorter sorter = ListViewItemSorter as DirectoryListViewColumnSorter;
                if (e.Column == sorter.SortColumn)
                {
                    if (sorter.Order == SortOrder.Ascending)
                        sorter.Order = SortOrder.Descending;
                    else
                        sorter.Order = SortOrder.Ascending;
                }
                else
                {
                    sorter.SortColumn = e.Column;
                    sorter.Order = SortOrder.Ascending;
                }
                Sort();
            });
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
}
