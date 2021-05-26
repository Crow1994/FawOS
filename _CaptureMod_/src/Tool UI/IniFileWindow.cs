using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DAM
{
    public partial class IniFileWindow : Form
    {
        public IniFileWindow()
        {
            InitializeComponent();
        }

        private void buttonOpen_Click(object sender, EventArgs e)
        {
            openFileDialog1.Multiselect = false;
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    FLDataFile file = new FLDataFile(openFileDialog1.FileName, false);
                    // Build the ini file in a string, section by section
                    StringBuilder strToSave = new StringBuilder();
                    foreach (FLDataFile.Section section in file.sections)
                    {
                        strToSave.AppendLine("[" + section.sectionName + "]");

                        foreach (FLDataFile.Setting entry in section.settings)
                        {
                            string line = entry.settingName;
                            if (entry.NumValues() > 0)
                            {
                                line += " = ";
                                for (int i = 0; i < entry.NumValues(); i++)
                                {
                                    if (i != 0)
                                        line += ", ";
                                    line += entry.Str(i);
                                }
                            }
                            strToSave.AppendLine(line);
                        }
                        strToSave.AppendLine("");
                    }
                    richTextBoxFileView.Text = strToSave.ToString();
                    this.Text = "INI File View: " + openFileDialog1.FileName;
                }
                catch (Exception ex)
                {
                    richTextBoxFileView.Text = ex.ToString();
                    this.Text = "INI File View";
                }
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonSave_Click(object sender, EventArgs e)
        {
            saveFileDialog1.FileName = openFileDialog1.FileName;
            DialogResult result = saveFileDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    FLDataFile editedFile = new FLDataFile(Encoding.ASCII.GetBytes(richTextBoxFileView.Text), saveFileDialog1.FileName, false);
                    editedFile.SaveSettings(saveFileDialog1.FileName, false);
                }
                catch (Exception ex)
                {
                    richTextBoxFileView.Text = ex.ToString();
                }
            }
        }
    }
}
