using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DAM
{
    /// <summary>
    /// Change a player location.
    /// </summary>
    public partial class ChangeLocationWindow : Form
    {
        AppServiceInterface appServices;
        List<FLDataFile> charFiles;

        public ChangeLocationWindow(AppServiceInterface appServices, FLGameData gameData, List<FLDataFile> charFiles)
        {
            this.appServices = appServices;
            this.charFiles = charFiles;

            InitializeComponent();
            hashListBindingSource.DataSource = gameData.DataStore;
            hashListBindingSource1.DataSource = gameData.DataStore;
            FilterUpdate();
        }

        private void ChangeLocationWindow_Load(object sender, EventArgs e)
        {
            // Select the row that the player is currently at
            if (charFiles.Count == 1 && charFiles[0].SettingExists("Player", "base")
                    && charFiles[0].SettingExists("Player", "last_base"))
            {
                checkBox2.Checked = false;

                string currentBaseNick = charFiles[0].GetSetting("Player", "last_base").Str(0);
                foreach (DataGridViewRow row in dataGridViewBase.Rows)
                {
                    GameDataSet.HashListRow dataRow = (GameDataSet.HashListRow)((DataRowView)row.DataBoundItem).Row;
                    if (dataRow.ItemNickName == currentBaseNick)
                    {
                        row.Selected = true;
                        dataGridViewBase.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }
            }


            if (charFiles[0] != null && charFiles[0].SettingExists("Player", "system")
                && charFiles[0].SettingExists("Player", "pos"))
            {
                checkBox2.Checked = true;

                string currentSystemNick = charFiles[0].GetSetting("Player", "system").Str(0);
                foreach (DataGridViewRow row in dataGridViewSystem.Rows)
                {
                    GameDataSet.HashListRow dataRow = (GameDataSet.HashListRow)((DataRowView)row.DataBoundItem).Row;
                    if (dataRow.ItemNickName == currentSystemNick)
                    {
                        row.Selected = true;
                        dataGridViewSystem.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }

                textBoxPosX.Text = charFiles[0].GetSetting("Player", "pos").Str(0);
                textBoxPosY.Text = charFiles[0].GetSetting("Player", "pos").Str(1);
                textBoxPosZ.Text = charFiles[0].GetSetting("Player", "pos").Str(2);
            }
            else
            {
                textBoxPosX.Text = "0";
                textBoxPosY.Text = "100000";
                textBoxPosZ.Text = "100000";
            }
        }

        /// <summary>
        /// Save the new location
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveButton_Click(object sender, EventArgs e)
        {
            if (dataGridViewBase.SelectedRows.Count != 1)
                return;
            foreach (FLDataFile charFile in charFiles)
            {
                foreach (DataGridViewRow row in dataGridViewBase.SelectedRows)
                {
                    GameDataSet.HashListRow dataRow = (GameDataSet.HashListRow)((DataRowView)row.DataBoundItem).Row;
                    string baseNick = dataRow.ItemNickName;
                    string systemNick = baseNick.Substring(0, 4);

                    charFile.AddSetting("Player", "base", new object[] { baseNick });
                    charFile.AddSetting("Player", "last_base", new object[] { baseNick });
                    charFile.AddSetting("Player", "system", new object[] { systemNick });
                    break;
                }

                if (checkBox2.Checked)
                {
                    foreach (DataGridViewRow row in dataGridViewSystem.SelectedRows)
                    {
                        GameDataSet.HashListRow dataRow = (GameDataSet.HashListRow)((DataRowView)row.DataBoundItem).Row;
                        string systemNick = dataRow.ItemNickName;
                        charFile.DeleteSetting("Player", "base");
                        charFile.AddSetting("Player", "pos", new object[] { textBoxPosX.Text, textBoxPosY.Text, textBoxPosZ.Text });
                        charFile.AddSetting("Player", "rotation", new object[] { 0, 0, 0 });
                        charFile.AddSetting("Player", "system", new object[] { systemNick });
                        break;
                    }
                }
                else
                {
                    charFile.DeleteSetting("Player", "pos");
                    charFile.DeleteSetting("Player", "rotation");
                }

                appServices.SaveCharFile(charFile);
            }
            this.Close();
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            timer1.Start();
        }

        /// <summary>
        /// Update the filter applied to the character list data grid view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterUpdate()
        {
            string filterBase = "(ItemType = '" + FLGameData.GAMEDATA_BASES + "')";
            if (textBox1.Text.Length > 0)
            {
                string filterText = textBox1.Text;
                if (filterBase != null)
                    filterBase += " AND ";
                filterBase += "((ItemNickName LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%')";
                filterBase += "OR (IDSName LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%'))";
            }
            hashListBindingSource.Filter = filterBase;

            string filterSystem = "(ItemType = '" + FLGameData.GAMEDATA_SYSTEMS + "')";
            if (textBox1.Text.Length > 0)
            {
                string filterText = textBox1.Text;
                if (filterSystem != null)
                    filterSystem += " AND ";
                filterSystem += "((ItemNickName LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%')";
                filterSystem += "OR (IDSName LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%'))";
            }

            hashListBindingSource1.Filter = filterSystem;
        }


        private void dataGridViewBase_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridViewBase.SelectedRows)
            {
                richTextBoxInfo.Clear();
                richTextBoxInfo.AppendText("@@@INSERTED_RTF_CODE_HACK@@@");
                string rtf = "";

                if ((string)row.Cells[itemTypeDataGridViewTextBoxColumn.Index].Value == "ships")
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

        // <summary>
        /// Show details when row is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in dataGridViewSystem.SelectedRows)
            {
                richTextBoxInfo.Clear();
                richTextBoxInfo.AppendText("@@@INSERTED_RTF_CODE_HACK@@@");
                string rtf = "";

                if ((string)row.Cells[itemTypeDataGridViewTextBoxColumn.Index].Value == "ships")
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

        private void checkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                dataGridViewSystem.Enabled = true;
                textBoxPosX.Enabled = true;
                textBoxPosY.Enabled = true;
                textBoxPosZ.Enabled = true;
            }
            else
            {
                dataGridViewSystem.Enabled = false;
                textBoxPosX.Enabled = false;
                textBoxPosY.Enabled = false;
                textBoxPosZ.Enabled = false;

            }
        }

        private void dataGridViewBase_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            checkBox2.Checked = false;
            saveButton_Click(null, null);
        }

        private void dataGridViewSystem_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            checkBox2.Checked = true;
            saveButton_Click(null, null);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            FilterUpdate();
        }
    }
}
