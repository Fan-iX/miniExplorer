using System;
using System.Configuration;
using System.Windows.Forms;
using System.Collections.Specialized;

namespace miniExplorer
{
    static class Program
    {
        public static miniBrowser browser;
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
            browser = new miniBrowser();
            Application.Run(browser);
        }
    }
}
