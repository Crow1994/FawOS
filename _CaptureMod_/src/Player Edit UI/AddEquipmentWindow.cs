using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DAM
{
    public partial class AddEquipmentWindow : Form
    {
        FLGameData gameData;
        UIDataSet.PIEquipmentTableDataTable equipTable;
        UIDataSet.PIEquipmentTableRow rowToEdit;
        string defaultGameDataType = "";

        /// <summary>
        /// Edit the specified row.
        /// </summary>
        /// <param name="parent">The main window parent.</param>
        /// <param name="rowToEdit">The row to edit.</param>
        public AddEquipmentWindow(FLGameData gameData, UIDataSet.PIEquipmentTableDataTable equipTable, UIDataSet.PIEquipmentTableRow rowToEdit)
        {
            this.gameData = gameData;
            this.equipTable = equipTable;
            this.rowToEdit = rowToEdit;

            if (rowToEdit != null)
            {
                defaultGameDataType = rowToEdit.itemGameDataType;
            }

            InitializeComponent();
            this.hashListBindingSource.DataSource = gameData.DataStore;
            FilterUpdate();
        }

        private void AddEquipmentWindow_Load(object sender, EventArgs e)
        {
            if (rowToEdit != null)
            {
                foreach (DataGridViewRow row in ceItemGrid.Rows)
                {
                    GameDataSet.HashListRow dataRow = (GameDataSet.HashListRow)((DataRowView)row.DataBoundItem).Row;
                    if (dataRow.ItemHash == rowToEdit.itemHash)
                    {
                        row.Selected = true;
                        ceItemGrid.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Do nothing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Add the selected item to the parent's cargo table.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void okButton_Click(object sender, EventArgs e)
        {
            if (ceItemGrid.SelectedRows.Count != 1)
                return;

            foreach (DataGridViewRow row in ceItemGrid.SelectedRows)
            {
                GameDataSet.HashListRow dataRow = (GameDataSet.HashListRow)((DataRowView)row.DataBoundItem).Row;
                if (rowToEdit != null)
                {
                    rowToEdit.itemDescription = gameData.GetItemDescByHash(dataRow.ItemHash);
                    rowToEdit.itemHash = dataRow.ItemHash;
                }
                else
                {
                    equipTable.AddPIEquipmentTableRow("*", gameData.GetItemDescByHash(dataRow.ItemHash), dataRow.ItemHash, "", "");
                }
            }

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
            string filter = "(";
            if (defaultGameDataType.Length > 0)
            {
                filter += "(ItemType = '" + defaultGameDataType + "')";
            }
            if (checkBoxShowAllTypes.Checked || defaultGameDataType.Length==0)
            {
                if (filter.Length > 1)
                    filter += " OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_GUNS + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_TURRETS + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_MINES + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_PROJECTILES + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_SHIELDS + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_THRUSTERS + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_CM + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_CLOAK + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_LIGHTS + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_MISC + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_SCANNERS + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_TRACTORS + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_ENGINES + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_ARMOR + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_FX + "') OR ";
                filter += "(ItemType = '" + FLGameData.GAMEDATA_POWERGEN + "')";
            }
            filter += ")";

            if (textBox1.Text.Length > 0)
            {
                string filterText = textBox1.Text;
                if (filter != null)
                    filter += " AND ";
                filter += "((IDSName LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%')";
                filter += " OR (ItemNickName LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%')";
                filter += " OR (ItemType LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%'))";
            }
            if (filter.Length>2)
                hashListBindingSource.Filter = filter;
        }

        /// <summary>
        /// A double click is treated like the OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_DoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            okButton_Click(sender, e);
        }

        // <summary>
        /// Show details when row is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in ceItemGrid.SelectedRows)
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

        private void checkBoxShowAllTypes_CheckedChanged(object sender, EventArgs e)
        {
            FilterUpdate();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            FilterUpdate();
        }
    }
}
