using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace DAM
{
    public partial class HashWindow : Form
    {
        public HashWindow(FLGameData gameData)
        {
            InitializeComponent();
            hashListBindingSource.DataSource = gameData.DataStore;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            timer1.Start();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }


        /// <summary>
        /// Show details when row is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridView1.SelectedRows)
            {
                richTextBoxInfo.Clear();
                richTextBoxInfo.AppendText("@@@INSERTED_RTF_CODE_HACK@@@");
                string rtf = "";

                if ((string)row.Cells[itemTypeDataGridViewTextBoxColumn.Index].Value == FLGameData.GAMEDATA_SHIPS)
                {
                    rtf += FLUtility.FLXmlToRtf((string)row.Cells[iDSInfo1DataGridViewTextBoxColumn.Index].Value);
                    rtf += "\\pard \\par ";
                    rtf += FLUtility.FLXmlToRtf((string)row.Cells[iDSInfoDataGridViewTextBoxColumn.Index].Value);
                }
                else if ((string)row.Cells[itemTypeDataGridViewTextBoxColumn.Index].Value == FLGameData.GAMEDATA_BASES)
                {
                    rtf += FLUtility.FLXmlToRtf((string)row.Cells[iDSInfoDataGridViewTextBoxColumn.Index].Value);
                    rtf += "\\pard \\par ";
                    rtf += FLUtility.FLXmlToRtf((string)row.Cells[iDSInfo1DataGridViewTextBoxColumn.Index].Value);
                    rtf += "\\pard \\par ";
                    rtf += FLUtility.FLXmlToRtf((string)row.Cells[iDSInfo2DataGridViewTextBoxColumn.Index].Value);
                    rtf += "\\pard \\par ";
                    rtf += FLUtility.FLXmlToRtf((string)row.Cells[iDSInfo3DataGridViewTextBoxColumn.Index].Value);
                }
                else
                {
                    string xml = (string)row.Cells[iDSInfoDataGridViewTextBoxColumn.Index].Value;
                    if (xml.Length == 0)
                        xml = "No information available";
                    rtf += FLUtility.FLXmlToRtf(xml);
                }
                richTextBoxInfo.Rtf = richTextBoxInfo.Rtf.Replace("@@@INSERTED_RTF_CODE_HACK@@@", rtf);
                break;
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            string filterText = FLUtility.EscapeLikeExpressionString(textBox1.Text);
            if (filterText == "")
            {
                hashListBindingSource.Filter = null;
                return;
            }
            string expr = "(ItemType = '%" + filterText + "%')";
            expr += " OR (ItemNickName LIKE '%" + filterText + "%')";
            expr += " OR (IDSName LIKE '%" + filterText + "%')";
            expr += " OR (IDSInfo LIKE '%" + filterText + "%')";
            expr += " OR (IDSInfo1 LIKE '%" + filterText + "%')";
            expr += " OR (IDSInfo2 LIKE '%" + filterText + "%')";
            expr += " OR (IDSInfo3 LIKE '%" + filterText + "%')";
            expr += " OR (ItemKeys LIKE '%" + filterText + "%')";
            hashListBindingSource.Filter = expr;
        }

        private void buttonExport_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog d = new SaveFileDialog())
            {
                if (d.ShowDialog() == DialogResult.OK)
                {
                    using (StreamWriter file = new StreamWriter(d.FileName))
                    {
                        foreach (DataGridViewRow row in dataGridView1.SelectedRows)
                        {
                            string name = (string)row.Cells[iDSNameDataGridViewTextBoxColumn.Index].Value;
                            file.WriteLine(String.Format(";{0}", name));                       
                            string nick = (string)row.Cells[itemNickNameDataGridViewTextBoxColumn.Index].Value;
                            file.WriteLine(String.Format("{0}=", nick));
                        }
                    }
                }
            }
        }
    }
}
