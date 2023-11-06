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

namespace miniExplorer{
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
}