using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Timers;

namespace miniExplorer
{
    public class PocketForm : Form
    {
        public bool Foldable = false;
        public bool Folded = false;
        public Size UnfoldedSize = new Size(300, 300);
        public DpiFactor DpiScale
        {
            get => new DpiFactor(DeviceDpi / 96.0f);
        }

        public PocketForm()
        {
            AllowDrop = true;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            StartPosition = FormStartPosition.WindowsDefaultLocation;
            TopMost = true;
            KeyPreview = true;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.CaptionFont;
            DragEnter += new DragEventHandler((object sender, DragEventArgs e) =>
            {
                if (Foldable)
                {
                    Unfold();
                    System.Timers.Timer foldTimer = new System.Timers.Timer()
                    {
                        Interval = 100
                    };
                    foldTimer.Elapsed += new ElapsedEventHandler((object s, ElapsedEventArgs ea) =>
                    {
                        if (!this.ClientRectangle.Contains(Cursor.Position))
                        {
                            Fold();
                            foldTimer.Stop();
                            foldTimer.Dispose();
                        }
                    });
                }
            });
            DragLeave += new EventHandler((object sender, EventArgs e) =>
            {
                if (Foldable & !this.ClientRectangle.Contains(this.PointToClient(Control.MousePosition))) Fold();
            });
            ResizeEnd += new EventHandler((object sender, EventArgs e) =>
            {
                Foldable = false;
                Folded = false;
                UnfoldedSize = new Size(ClientSize.Width, Math.Max(ClientSize.Height, 150 * DpiScale));
            });
        }

        protected override void WndProc(ref Message m)
        {
            // const int WM_NCLBUTTONDOWN = 0x00A1;
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
                    Foldable = !Foldable;
                    if (Foldable)
                        Fold();
                    else
                        Unfold();
                    break;
                default:
                    base.WndProc(ref m);
                    break;
            }
        }

        protected override void OnDpiChanged(DpiChangedEventArgs e)
        {
            base.OnDpiChanged(e);
            if (!Folded) MinimumSize = new Size(150 * DpiScale, 150 * DpiScale);
        }

        public void OnMouseEnterWindow()
        {
            if (Foldable) Unfold();
        }

        public void OnMouseLeaveWindow()
        {
            if (Foldable) Fold();
        }

        public void Fold()
        {
            Folded = true;
            MinimumSize = new Size(150 * DpiScale, 0);
            MaximumSize = new Size(int.MaxValue, 0);
            ClientSize = new Size(UnfoldedSize.Width, 0);
        }

        public void Unfold()
        {
            MaximumSize = new Size(0, 0);
            MinimumSize = new Size(150 * DpiScale, 150 * DpiScale);
            ClientSize = UnfoldedSize;
            Folded = false;
        }
    }

    public class ListViewWithoutHorizontalScrollBar : ListView
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

        public bool Updating = false;

        new void BeginUpdate()
        {
            Updating = true;
            base.BeginUpdate();
        }
        new void EndUpdate()
        {
            base.EndUpdate();
            Updating = false;
        }
    }

    public class TabControlW : TabControl
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            // Send TCM_SETMINTABWIDTH
            SendMessage(this.Handle, 0x1331, IntPtr.Zero, (IntPtr)10);
        }

        public TabControlW()
        {
        }

        public TabPage PointedTab
        {
            get
            {
                Point pt = PointToClient(Cursor.Position);
                for (int i = 0; i < TabPages.Count; i++)
                    if (GetTabRect(i).Contains(pt))
                        return TabPages[i];
                return null;
            }
        }
    }

    public class OneTimeTimer : System.Timers.Timer
    {
        public Action DisposeAction;
        public OneTimeTimer(double interval) : base(interval)
        {
            AutoReset = false;
            Elapsed += new ElapsedEventHandler((object sender, ElapsedEventArgs e) =>
            {
                if (DisposeAction != null)
                    DisposeAction();
                Stop();
                Dispose();
            });
        }
    }
}
