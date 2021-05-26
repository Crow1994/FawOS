using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace DAM.Tool_UI
{
    public partial class StatisticsWindow : Form
    {
        BackgroundWorker bgWkr = null;

        public StatisticsWindow()
        {
            InitializeComponent();
        }

        private void StatisticsWindow_Load(object sender, EventArgs e)
        {
            buttonRefresh_Click(null, null);          
        }

        private void MakeIt(object sender, DoWorkEventArgs e)
        {
            DamDataSet ds = new DamDataSet();
            using (DataAccess da = new DataAccess())
            {
                da.GetGeneralStatisticsTable((DamDataSet.GeneralStatisticsTableDataTable)ds.GeneralStatisticsTable);
                ds.AcceptChanges();
            }
            e.Result = ds.GeneralStatisticsTable;
        }

        private void MakeItCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            damDataSet.GeneralStatisticsTable.Clear();
            damDataSet.GeneralStatisticsTable.Merge((DamDataSet.GeneralStatisticsTableDataTable)e.Result);

            this.Cursor = Cursors.Default;
            label1.Text = "General Statistics";
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            bgWkr = new BackgroundWorker();
            bgWkr.DoWork += new DoWorkEventHandler(MakeIt);
            bgWkr.WorkerReportsProgress = true;
            bgWkr.WorkerSupportsCancellation = true;
            // bgWkr.ProgressChanged += new ProgressChangedEventHandler(ProgressUpdate);
            bgWkr.RunWorkerCompleted += new RunWorkerCompletedEventHandler(MakeItCompleted);
            bgWkr.RunWorkerAsync();
            this.Cursor = Cursors.WaitCursor;;
            label1.Text = "General Statistics - Loading...";
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            string filterText = FLUtility.EscapeLikeExpressionString(textBox1.Text);
            if (filterText == "")
            {
                generalStatisticsTableBindingSource.Filter = null;
                return;
            }
            string expr = "(Description LIKE '%" + filterText + "%')";
            generalStatisticsTableBindingSource.Filter = expr;
        }

    }
}
