using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;

namespace DAM
{
    public partial class PropertiesWindow : Form
    {
        public PropertiesWindow()
        {
            InitializeComponent();
        }

        private void PropertiesWindow_Load(object sender, EventArgs e)
        {
            lock (AppSettings.Default)
            {
                textBoxAccDir.Text = AppSettings.Default.setAccountDir;
                textBoxIoncrossDir.Text = AppSettings.Default.setIonCrossDir;
                textBoxFLDir.Text = AppSettings.Default.setFLDir;
                textBoxLoginIDBanFile.Text = AppSettings.Default.setLoginIDBanFile;
                checkBoxChangedOnly.Checked = AppSettings.Default.setCheckChangedOnly;
                checkBoxWriteEncryptedFiles.Checked = AppSettings.Default.setWriteEncryptedFiles;
                textBoxFLHookPort.Value = AppSettings.Default.setFLHookPort;
                textBoxFLHookLogin.Text = AppSettings.Default.setFLHookPassword;
                checkBoxAutomaticCharClean.Checked = AppSettings.Default.setAutomaticCharClean;
                checkBoxAutomaticCharWipe.Checked = AppSettings.Default.setAutomaticCharWipe;
                checkBoxMoveUninterestedChars.Checked = AppSettings.Default.setMoveUninterestedChars;
                textBoxMoveUninterestedCharsDir.Text = AppSettings.Default.setMoveUninterestedCharsDir;
                numericUpDown1.Value = AppSettings.Default.setDaysToDeleteInactiveChars;
                numericUpDown2.Value = AppSettings.Default.setSecsToDeleteUninterestedChars / 60;
                numericUpDown3.Value = AppSettings.Default.setDaysInactiveToDeleteUninterestedChars;
                checkBoxUnicode.Checked = AppSettings.Default.setFLHookUnicode;

                textBoxStatisticsDir.Text = AppSettings.Default.setStatisticsDir;
                textBoxStatsFactions.Text = AppSettings.Default.setStatsFactions;
                numericUpDownHistoryHorizon.Value = AppSettings.Default.setHistoryHorizon;
                LoadStatPlayerListSettings();
                checkBoxStatCharsByName.Checked = AppSettings.Default.setStatPlayerListShowCharsByName;
                checkBoxStatCharsBySys.Checked = AppSettings.Default.setStatPlayerListShowCharsBySys;
                checkBoxStatsTimeUTC.Checked = AppSettings.Default.setStatPlayerListTimeUTC;

                checkBoxAutomaticFixCharFiles.Checked = AppSettings.Default.setAutomaticFixErrors;
                checkBoxCheckDefaultEngine.Checked = AppSettings.Default.setCheckDefaultEngine;
                checkBoxCheckDefaultPowerPlant.Checked = AppSettings.Default.setCheckDefaultPowerPlant;
                checkBoxReportVisitError.Checked = AppSettings.Default.setReportVisitErrors;
                checkBoxCheckDefaultLights.Checked = AppSettings.Default.setCheckDefaultLights;

                checkBoxShowQuitMsg.Checked = AppSettings.Default.setShowQuitMsg;
                domainUpDownUpdatePlayerDatabase.SelectedIndex = AppSettings.Default.setShowUpdateDatabase;
                checkBoxShowMultibanSucc.Checked = AppSettings.Default.setShowMultibanSucc;
                checkBoxShowMultiunbanSucc.Checked = AppSettings.Default.setShowMultiunbanSucc;
                checkBoxShowMultideleteSucc.Checked = AppSettings.Default.setShowMultideleteSucc;
                checkBoxFilterWaitEnter.Checked = AppSettings.Default.setFilterWaitForEnter;
                checkBoxBannedCharsInRed.Checked = AppSettings.Default.setDisplayBannedCharsRed;

                LoadProcessorAffinity();
            }
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            lock (AppSettings.Default)
            {
                AppSettings.Default.setAccountDir = textBoxAccDir.Text;
                AppSettings.Default.setIonCrossDir = textBoxIoncrossDir.Text;
                AppSettings.Default.setFLDir = textBoxFLDir.Text;
                AppSettings.Default.setLoginIDBanFile = textBoxLoginIDBanFile.Text;
                AppSettings.Default.setCheckChangedOnly = checkBoxChangedOnly.Checked;
                AppSettings.Default.setWriteEncryptedFiles = checkBoxWriteEncryptedFiles.Checked;
                AppSettings.Default.setFLHookPort = textBoxFLHookPort.Value;
                AppSettings.Default.setFLHookPassword = textBoxFLHookLogin.Text;
                AppSettings.Default.setFLHookUnicode = checkBoxUnicode.Checked;
                AppSettings.Default.setAutomaticCharClean = checkBoxAutomaticCharClean.Checked;
                AppSettings.Default.setAutomaticCharWipe = checkBoxAutomaticCharWipe.Checked;
                AppSettings.Default.setMoveUninterestedChars = checkBoxMoveUninterestedChars.Checked;
                AppSettings.Default.setMoveUninterestedCharsDir = textBoxMoveUninterestedCharsDir.Text;
                AppSettings.Default.setDaysToDeleteInactiveChars = numericUpDown1.Value;
                AppSettings.Default.setSecsToDeleteUninterestedChars = numericUpDown2.Value * 60;
                AppSettings.Default.setDaysInactiveToDeleteUninterestedChars = numericUpDown3.Value;

                AppSettings.Default.setStatisticsDir = textBoxStatisticsDir.Text;
                AppSettings.Default.setStatsFactions = textBoxStatsFactions.Text;
                AppSettings.Default.setHistoryHorizon = (int)numericUpDownHistoryHorizon.Value;
                SaveStatPlayerListSettings();
                AppSettings.Default.setStatPlayerListShowCharsByName = checkBoxStatCharsByName.Checked;
                AppSettings.Default.setStatPlayerListShowCharsBySys = checkBoxStatCharsBySys.Checked;
                AppSettings.Default.setStatPlayerListTimeUTC = checkBoxStatsTimeUTC.Checked;

                AppSettings.Default.setAutomaticFixErrors = checkBoxAutomaticFixCharFiles.Checked;
                AppSettings.Default.setCheckDefaultEngine = checkBoxCheckDefaultEngine.Checked;
                AppSettings.Default.setCheckDefaultPowerPlant = checkBoxCheckDefaultPowerPlant.Checked;
                AppSettings.Default.setReportVisitErrors = checkBoxReportVisitError.Checked;
                AppSettings.Default.setCheckDefaultLights = checkBoxCheckDefaultLights.Checked;

                AppSettings.Default.setShowQuitMsg = checkBoxShowQuitMsg.Checked;
                AppSettings.Default.setShowUpdateDatabase = domainUpDownUpdatePlayerDatabase.SelectedIndex;
                AppSettings.Default.setShowMultibanSucc = checkBoxShowMultibanSucc.Checked;
                AppSettings.Default.setShowMultiunbanSucc = checkBoxShowMultiunbanSucc.Checked;
                AppSettings.Default.setShowMultideleteSucc = checkBoxShowMultideleteSucc.Checked;
                AppSettings.Default.setFilterWaitForEnter = checkBoxFilterWaitEnter.Checked;
                AppSettings.Default.setDisplayBannedCharsRed = checkBoxBannedCharsInRed.Checked;

                SaveProcessorAffinity();
                Program.ApplyProcessorAffinity();

                AppSettings.Default.Save();
            }
            this.Close();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        #region Open File/Dir-Dialog buttons

        private void accountDirButton_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = textBoxAccDir.Text;
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxAccDir.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void ioncrossDirButton_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = textBoxIoncrossDir.Text;
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxIoncrossDir.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void flDirButton_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = textBoxFLDir.Text;
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxFLDir.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void UninterestedCharsDirButton_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = textBoxMoveUninterestedCharsDir.Text;
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxMoveUninterestedCharsDir.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void statisticsButton_Click(object sender, EventArgs e)
        {
            folderBrowserDialog.SelectedPath = textBoxStatisticsDir.Text;
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxStatisticsDir.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void loginIDBanFileButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(textBoxLoginIDBanFile.Text) && System.IO.File.Exists(textBoxLoginIDBanFile.Text))
                openFileDialog.InitialDirectory = new System.IO.FileInfo(textBoxLoginIDBanFile.Text).Directory.ToString();

            DialogResult result = openFileDialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                textBoxLoginIDBanFile.Text = openFileDialog.FileName;
            }
        }

        #endregion

        #region Prcoessor Affinity

        private void LoadProcessorAffinity()
        {
            int procCount = Environment.ProcessorCount;
            BitArray bits = new BitArray(BitConverter.GetBytes(AppSettings.Default.setProcessorAffinity));
            for (int i = 0; i < procCount; i++)
            {
                checkedListBoxProcessors.Items.Add("Core " + (i+1));
                checkedListBoxProcessors.SetItemChecked(i, bits[i]);
            }
        }

        private void SaveProcessorAffinity()
        {
            int procCount = Environment.ProcessorCount;
            BitArray bits = new BitArray(procCount);
            for (int i = 0; i < procCount; i++)
            {
                bits[i] = checkedListBoxProcessors.GetItemChecked(i);
            }
            int[] tmp = new int[1];
            bits.CopyTo(tmp, 0);
            AppSettings.Default.setProcessorAffinity = tmp[0];
        }

        #endregion

        #region Stat Player List Settings

        private void buttonStatPlayerListShow_Click(object sender, EventArgs e)
        {
            object item;
            item = listBoxStatPlayerListDontShow.SelectedItem;

            // nothing selected
            if (item == null)
                return;

            listBoxStatPlayerListDontShow.Items.Remove(item);
            listBoxStatPlayerListShow.Items.Add(item);

            listBoxStatPlayerListShow.SelectedItem = item;
            listBoxStatPlayerListDontShow.SelectedItem = null;
        }

        private void buttonStatPlayerListDontShow_Click(object sender, EventArgs e)
        {
            object item;
            item = listBoxStatPlayerListShow.SelectedItem;

            // nothing selected
            if (item == null || listBoxStatPlayerListShow.Items.Count == 1)
                return;

            listBoxStatPlayerListShow.Items.Remove(item);
            listBoxStatPlayerListDontShow.Items.Add(item);
        }

        private void buttonStatPlayerListUp_Click(object sender, EventArgs e)
        {
            object tmp;
            int index = listBoxStatPlayerListShow.SelectedIndex;

            // is already on top or none is selected
            if (index == 0 || index == -1)
                return;

            // swap them
            tmp = listBoxStatPlayerListShow.Items[index-1];
            listBoxStatPlayerListShow.Items[index-1] = listBoxStatPlayerListShow.Items[index];
            listBoxStatPlayerListShow.Items[index] = tmp;

            listBoxStatPlayerListShow.SelectedIndex--;
        }

        private void buttonStatPlayerListDown_Click(object sender, EventArgs e)
        {
            object tmp;
            int index = listBoxStatPlayerListShow.SelectedIndex;

            // is already on bottom or none is selected
            if (index == listBoxStatPlayerListShow.Items.Count - 1 || index == -1)
                return;

            // swap them
            tmp = listBoxStatPlayerListShow.Items[index + 1];
            listBoxStatPlayerListShow.Items[index+1] = listBoxStatPlayerListShow.Items[index];
            listBoxStatPlayerListShow.Items[index] = tmp;

            listBoxStatPlayerListShow.SelectedIndex++;
        }

        private void LoadStatPlayerListSettings()
        {
            string[] show = AppSettings.Default.setStatPlayerListShowFields.Split(';');
            string[] dontShow = AppSettings.Default.setStatPlayerListDontShowFields.Split(';');

            listBoxStatPlayerListShow.Items.Clear();
            listBoxStatPlayerListShow.Items.AddRange(show);

            listBoxStatPlayerListDontShow.Items.Clear();
            listBoxStatPlayerListDontShow.Items.AddRange(dontShow);
        }

        private void SaveStatPlayerListSettings()
        {
            string[] show;
            string[] dontShow;

            show = GetListItemsAsStringArray(listBoxStatPlayerListShow);
            dontShow = GetListItemsAsStringArray(listBoxStatPlayerListDontShow);

            AppSettings.Default.setStatPlayerListShowFields = string.Join(";", show);
            AppSettings.Default.setStatPlayerListDontShowFields = string.Join(";", dontShow);
        }

        private string[] GetListItemsAsStringArray(ListBox box)
        {
            string[] items = new string[box.Items.Count];
            for (int i = 0; i < items.Length; i++)
            {
                items[i] = box.Items[i].ToString();
            }
            return items;
        }

        #endregion
    }
}
