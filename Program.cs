using System;
using System.Linq;
using System.Threading;
using System.Configuration;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

namespace miniExplorer
{
    static class Program
    {
        public static readonly uint WM_ME_SHOW = RegisterWindowMessage("MINI_EXPLORER_SHOW_MSG");
        public static BrowserForm browser;
        public static bool IsPrimaryInstance = !Environment.GetCommandLineArgs().Contains("-multi", StringComparer.OrdinalIgnoreCase);
        public static string[] Args;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Args = args.Where(a => !a.StartsWith("-")).ToArray();
            bool createNew;
            if (IsPrimaryInstance)
            {
                using (Mutex mutex = new Mutex(true, "MiniExplorer", out createNew))
                {
                    if (createNew)
                    {
                        RunApp();
                    }
                    else
                    {
                        SendMessage((IntPtr)0xffff, WM_ME_SHOW, IntPtr.Zero, IntPtr.Zero);
                    }
                }
            }
            else
            {
                RunApp();
            }
        }
        static void RunApp()
        {
            NameValueCollection section = ConfigurationManager.GetSection("System.Windows.Forms.ApplicationConfigurationSection") as NameValueCollection;
            if (section != null)
            {
                section["DpiAwareness"] = "PerMonitorV2";
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            browser = new BrowserForm();
            Application.AddMessageFilter(new MouseMoveFilter());
            Application.Run(browser);
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        public class MouseMoveFilter : IMessageFilter
        {
            bool mouseOut = true;
            #region IMessageFilter Members
            const int WM_MOUSELEAVE = 0x02A3;
            const int WM_NCMOUSEMOVE = 0x0A0;
            const int WM_MOUSEMOVE = 0x0200;
            const int WM_NCMOUSELEAVE = 0x2A2;
            const int WM_LBUTTONDOWN = 0x0201;

            public bool PreFilterMessage(ref Message m)
            {
                switch (m.Msg)
                {
                    case WM_NCMOUSEMOVE:
                    case WM_MOUSEMOVE:
                        if (mouseOut)
                        {
                            browser.OnMouseEnterWindow();
                            mouseOut = false;
                        }
                        break;
                    case WM_NCMOUSELEAVE:
                    case WM_MOUSELEAVE:
                        if (!browser.Bounds.Contains(Control.MousePosition))
                        {
                            browser.OnMouseLeaveWindow();
                            mouseOut = true;
                        }
                        break;
                }
                return false;
            }

            #endregion
        }
    }
}
