using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace DAM.Tool_UI
{
    public partial class DecodeAllFilesWindow : Form
    {
        public DecodeAllFilesWindow()
        {
            InitializeComponent();
        }

        BackgroundWorker bgWkr;

        private void DecodeAllFilesWindow_Load(object sender, EventArgs e)
        {
            bgWkr = new BackgroundWorker();
            bgWkr.DoWork += new DoWorkEventHandler(FixIt);
            bgWkr.WorkerReportsProgress = true;
            bgWkr.WorkerSupportsCancellation = true;
            bgWkr.ProgressChanged += new ProgressChangedEventHandler(ProgressUpdate);
            bgWkr.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FixItCompleted);
            bgWkr.RunWorkerAsync();
        }

        private void ProgressUpdate(object sender, ProgressChangedEventArgs e)
        {
            progressBar1.Value = e.ProgressPercentage;
        }

        /// <summary>
        /// Scan account directories background processing thread
        /// </summary>
        private void FixIt(object sender, DoWorkEventArgs e)
        {
            
            DateTime updateTime = DateTime.Now;
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
            string[] accDirs1 = Directory.GetDirectories(AppSettings.Default.setAccountDir, "??-????????");
            for (int i = 0; i < accDirs1.Length && !bgWkr.CancellationPending; i++)
            {
                if (updateTime < DateTime.Now)
                {
                    updateTime = DateTime.Now.AddMilliseconds(1000);
                    bgWkr.ReportProgress((i * 99) / accDirs1.Length);
                }

                string[] charFiles = Directory.GetFiles(accDirs1[i], "??-????????.fl");
                foreach (string charFilePath in charFiles)
                {
                    try
                    {
                        byte[] buf;
                        using (FileStream fs = new FileStream(charFilePath, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                            buf = new byte[fs.Length];
                            fs.Read(buf, 0, (int)fs.Length);
                            fs.Close();
                        }

                        buf = DecodeFLS1(buf);

                        using (FileStream fs = new FileStream(charFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            fs.Write(buf, 0, buf.Length);
                            fs.Close();
                        }
                    }
                    catch { }
                }
            }
            System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
        }

        byte[] gene = { (byte)'G', (byte)'e', (byte)'n', (byte)'e' };

        byte[] DecodeFLS1(byte[] buf)
        {
            if (buf.Length >= 4 && buf[0] == 'F' && buf[1] == 'L' && buf[2] == 'S' && buf[3] == '1')
            {
                byte[] dbuf = new byte[buf.Length - 4];
                for (int i = 4; i < buf.Length; i++)
                {
                    int k = (gene[i % 4] + (i - 4)) % 256;
                    dbuf[i - 4] = (byte)(buf[i] ^ (k | 0x80));
                }
                buf = dbuf;
            }
            return buf;
        }

        /// <summary>
        /// Called when account directory background processing thread completes.
        /// </summary>
        private void FixItCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.Close();
        }
    }
}
