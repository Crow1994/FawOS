using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DAM
{
    public partial class ChangeShipWindow : Form
    {
        private AppServiceInterface appServices;
        private FLDataFile charFile;

        public ChangeShipWindow(AppServiceInterface appServices, FLGameData gameData, FLDataFile charFile)
        {
            this.appServices = appServices;
            this.charFile = charFile;
            InitializeComponent();
            hashListBindingSource.DataSource = gameData.DataStore;

            FilterUpdate();
        }

        /// <summary>
        /// Setup list of ships.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeShipWindow_Load(object sender, EventArgs e)
        {
            // Select the row that the player is currently 
            if (charFile.SettingExists("Player", "ship_archetype"))
            {
                uint currentShipHash = charFile.GetSetting("Player", "ship_archetype").UInt(0);
                foreach (DataGridViewRow row in csItemGrid.Rows)
                {
                    GameDataSet.HashListRow dataRow = (GameDataSet.HashListRow)((DataRowView)row.DataBoundItem).Row;
                    if (dataRow.ItemHash == currentShipHash)
                    {
                        row.Selected = true;
                        csItemGrid.FirstDisplayedScrollingRowIndex = row.Index;
                        break;
                    }
                }
            }
        }


        /// <summary>
        /// Update the filter applied to the character list data grid view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FilterUpdate()
        {
            // gameDataTableBindingSource.Filter
            string filter = "(ItemType = '" + FLGameData.GAMEDATA_SHIPS + "')";

            if (textBox1.Text.Length > 0)
            {
                string filterText = textBox1.Text;
                if (filter != null)
                    filter += " AND ";
                filter += "((ItemNickName LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%')";
                filter += "OR (IDSName LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%'))";
            }

            hashListBindingSource.Filter = filter;
        }


        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            if (csItemGrid.SelectedRows.Count != 1)
               return;

            GameDataSet.HashListRow dataRow = (GameDataSet.HashListRow)((DataRowView)csItemGrid.SelectedRows[0].DataBoundItem).Row;
            charFile.AddSetting("Player", "ship_archetype", new object[] { dataRow.ItemHash });
            appServices.SaveCharFile(charFile);
            this.Close();
        }

        // <summary>
        /// Show details when row is selected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            foreach (DataGridViewRow row in csItemGrid.SelectedRows)
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

        /// <summary>
        /// A double click is treated like the OK button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void csItemGrid_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            saveButton_Click(null, null);
        }

        /// <summary>
        /// Update the filter on a timer.
        /// </summary>
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            timer1.Start();
        }

        /// <summary>
        /// Update the filter on a timer.
        /// </summary>
        private void timer1_Tick(object sender, EventArgs e)
        {
            FilterUpdate();
        }
    }
}
