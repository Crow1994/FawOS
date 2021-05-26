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
    public partial class BannedPlayers : Form
    {
        AppServiceInterface appServices;
        DamDataSet dataSet;

        public BannedPlayers(AppServiceInterface appServices, DamDataSet dataSet)
        {
            this.appServices = appServices;
            this.dataSet = dataSet;

            InitializeComponent();
            banListBindingSource.DataSource = dataSet;
            TimerFilterUpdate(null, null);
            dataGridView1_SelectionChanged(null, null);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Update the filter applied to the character list data grid view.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerFilterUpdate(object sender, EventArgs e)
        {
            timerFilter.Stop();
            string filter = "";
            
            if (textBoxFilter.Text.Length > 2)
            {
                string filterText = textBoxFilter.Text;
                if (filter != "")
                    filter += " AND ";
                filter += "((AccID = '" + FLUtility.EscapeEqualsExpressionString(filterText) + "') " +
                    " OR (AccDir = '"+ FLUtility.EscapeLikeExpressionString(filterText) + "') " +
                    " OR (BanReason LIKE '%" + FLUtility.EscapeLikeExpressionString(filterText) + "%'))";
            }

            if (checkBoxShowExpiredBans.Checked)
            {
                if (filter != "")
                    filter += " AND ";
                filter += "(BanEnd < #"+String.Format("{0:s}",DateTime.Now.ToUniversalTime())+"#)";
            }

            banListBindingSource.Filter = filter;
        }


        /// <summary>
        /// Update the account information area.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count != 1)
            {
                textBoxCharacters.Text = "";
                richTextBoxBanReason.Text = "";
                return;
            }

            string accDir = (string)dataGridView1.SelectedRows[0].Cells[accDirDataGridViewTextBoxColumn.Index].Value;

            string query = "(AccDir = '" + FLUtility.EscapeEqualsExpressionString(accDir) + "') AND (IsDeleted = 'false')";
            DamDataSet.CharacterListRow[] charRecords = (DamDataSet.CharacterListRow[])dataSet.CharacterList.Select(query);

            string charStr = "";
            foreach (DamDataSet.CharacterListRow charRecord in charRecords)           
                charStr += charRecord.CharName + ", ";
            textBoxCharacters.Text = charStr;

            richTextBoxBanReason.Text = (string)dataGridView1.SelectedRows[0].Cells[banReasonDataGridViewTextBoxColumn.Index].Value;
        }


        /// <summary>
        /// Update the list filter to show the specified record.
        /// </summary>
        /// <param name="accountID">The account ID to select and show.</param>
        public void HighlightRecord(string accountID)
        {
            textBoxFilter.Text = accountID;
            timerFilter.Start();
        }

        private void checkBoxShowExpiredBans_CheckedChanged(object sender, EventArgs e)
        {
            timerFilter.Start();
        }

        private void textBoxFilter_TextChanged(object sender, EventArgs e)
        {
            timerFilter.Start();
        }

        private void buttonUnban_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                string accDir = (string)dataGridView1.SelectedRows[0].Cells[accDirDataGridViewTextBoxColumn.Index].Value;
                appServices.UnbanAccount(accDir);
            }
        }

        private void buttonEditBan_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                string accDir = (string)dataGridView1.SelectedRows[0].Cells[accDirDataGridViewTextBoxColumn.Index].Value;
                DamDataSet.BanListRow banRecord = dataSet.BanList.FindByAccDir(accDir);
                if (banRecord != null)
                {
                    new CreateBanWindow(appServices, banRecord.AccDir, banRecord.AccID, dataSet, banRecord).ShowDialog(this);
                    dataGridView1_SelectionChanged(null, null);
                }
            }
        }

        private void buttonShowAccount_Click(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
            {
                string accDir = (string)dataGridView1.SelectedRows[0].Cells[accDirDataGridViewTextBoxColumn.Index].Value;
                appServices.FilterOnAccDir(accDir);
            }
        }
    }
}
