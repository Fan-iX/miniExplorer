using System;
using System.Configuration;
using System.Windows.Forms;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

namespace miniExplorer
{
    static class Program
    {
        public static BrowserForm browser;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
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

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        public static extern void OutputDebugString(string message);

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
