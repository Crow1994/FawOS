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
    public partial class CreateBanWindow : Form
    {
        private AppServiceInterface appServices;
        private string accDir;
        private string accID;
        private DamDataSet dataSet;
        private DamDataSet.BanListRow banRecord = null;

        public CreateBanWindow(AppServiceInterface appServices, string accDir, string accID, DamDataSet dataSet, DamDataSet.BanListRow banRecord)
        { 
            this.appServices = appServices;
            this.accDir = accDir;
            this.accID = accID;
            this.dataSet = dataSet;
            this.banRecord = banRecord;

            InitializeComponent();

            if (banRecord != null)
            {
                richTextBox1.Text = banRecord.BanReason;
                dateTimePickerStartDate.Value = banRecord.BanStart.ToUniversalTime();
                numericUpDownDuration.Value = (decimal)(banRecord.BanEnd - banRecord.BanEnd).TotalDays;
            }
            else
            {
                dateTimePickerStartDate.Value = DateTime.Now;
                numericUpDownDuration.Value = 0;
            }
        }

        private void dateTimePickerStartDate_ValueChanged(object sender, EventArgs e)
        {
            textBoxEndDate.Text = calcEndDate().ToLongDateString();
        }

        private void numericUpDownDuration_ValueChanged(object sender, EventArgs e)
        {
            textBoxEndDate.Text = calcEndDate().ToLongDateString();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            if (accDir != string.Empty)
                appServices.BanAccount(accDir, accID, richTextBox1.Text, dateTimePickerStartDate.Value, calcEndDate());

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private DateTime calcEndDate()
        {
            return dateTimePickerStartDate.Value.AddDays((int)numericUpDownDuration.Value);
        }
    }
}
