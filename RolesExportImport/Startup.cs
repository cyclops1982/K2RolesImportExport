using System;
using System.Collections.Generic;
using System.Text;

namespace RolesExportImport
{

    /// <summary>
    /// See http://www.infosysblogs.com/microsoft/2008/09/how_to_write_custom_main_metho.html (approach 3) to know why 
    /// this class is here.
    /// </summary>
    public class Startup
    {
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                App app = new App();
                app.MainWindow = new Window1();
                app.MainWindow.Show();
                app.Run();
            }
        }
    }
}
