using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DAM.Tool_UI
{
    public partial class FormBulkRestore : Form
    {
        public FormBulkRestore()
        {
            InitializeComponent();
        }

        private void buttonRestore_Click(object sender, EventArgs e)
        {
            foreach (string destFile in textBoxFilesToRestore.Text.Split(new char[] {'\n', ',', ';'}))
            {
                string file = destFile.Trim();
                if (file.Length == 0)
                    continue;

                try
                {
                    // Get accdir/filename.fl
                    string backupFile = textBoxBackupFile.Text.Trim() + file.Substring(file.Length - 26);
                    textBoxLog.AppendText("\n\nFinding " + backupFile + " for " + file);
                
                    if (System.IO.File.Exists(backupFile))
                    {
                        if (System.IO.File.Exists(file))
                        {
                            textBoxLog.AppendText("\nSkipping " + file + " file exists");
                        }
                        else
                        {
                            textBoxLog.AppendText("\nRestoring " + file);
                            System.IO.File.Copy(backupFile, file, true);
                        }
                    }
                    else
                    {
                        textBoxLog.AppendText("\nSkipping " + file + " backup not found");
                    }
                }
                catch (Exception ex)
                {
                    textBoxLog.AppendText("\nError " + ex.Message);
                }
            }
        }
    }
}
