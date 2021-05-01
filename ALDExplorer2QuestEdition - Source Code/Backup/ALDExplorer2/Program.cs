using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;
using System.IO;

namespace ALDExplorer2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            ExplorerForm explorerForm = null;

            var args = Environment.GetCommandLineArgs();
            if (args.Length == 1)
            {
                explorerForm = new ExplorerForm();
                Application.Run(explorerForm);
                return;
            }
            else if (args.Length == 2)
            {
                string fileName = args[1];
                if (File.Exists(fileName))
                {
                    explorerForm = new ExplorerForm();
                    Application.Run(explorerForm);
                    return;
                }
            }

            var consoleMode = new ConsoleMode();
            consoleMode.RunConsoleMode();
        }
    }
}
