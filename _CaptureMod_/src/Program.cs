using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;


namespace DAM
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Parse command line arguments
            string[] args = Environment.GetCommandLineArgs();
            foreach (string arg in args)
            {
                // Notify the application to run a player file clean as soon as possible.
                if (arg == "-autoclean")               
                {
                    RegistryKey key = Registry.CurrentUser.CreateSubKey("Software\\" + Application.ProductName);
                    key.SetValue("AutocleanPending", 1);
                    return;
                }
            }

            // Start the application if it is not already running.
            bool firstInstance;
            System.Threading.Mutex mutex = new System.Threading.Mutex(false, "Local\\DSAccountManager-Running", out firstInstance);
            if (firstInstance)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainWindow());
            }
        }

        public static bool ApplyProcessorAffinity()
        {
            try
            {
                ProcessThreadCollection threads = Process.GetCurrentProcess().Threads;
                IntPtr mask = (IntPtr)AppSettings.Default.setProcessorAffinity;
                foreach (ProcessThread thread in threads)
                {
                    thread.ProcessorAffinity = mask;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }
    }
}
