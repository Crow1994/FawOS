namespace DAM
{
    partial class BannedPlayers
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            DAM.DamDataSet damDataSet;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BannedPlayers));
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.accDirDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.accIDDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.banReasonDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.banStartDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.banEndDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.banListBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxFilter = new System.Windows.Forms.TextBox();
            this.textBoxCharacters = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.richTextBoxBanReason = new System.Windows.Forms.RichTextBox();
            this.timerFilter = new System.Windows.Forms.Timer(this.components);
            this.checkBoxShowExpiredBans = new System.Windows.Forms.CheckBox();
            this.buttonUnban = new System.Windows.Forms.Button();
            this.buttonEditBan = new System.Windows.Forms.Button();
            this.buttonShowAccount = new System.Windows.Forms.Button();
            damDataSet = new DAM.DamDataSet();
            ((System.ComponentModel.ISupportInitialize)(damDataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.banListBindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // damDataSet
            // 
            damDataSet.DataSetName = "DamDataSet";
            damDataSet.Locale = new System.Globalization.CultureInfo("");
            damDataSet.SchemaSerializationMode = System.Data.SchemaSerializationMode.IncludeSchema;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.accDirDataGridViewTextBoxColumn,
            this.accIDDataGridViewTextBoxColumn,
            this.banReasonDataGridViewTextBoxColumn,
            this.banStartDataGridViewTextBoxColumn,
            this.banEndDataGridViewTextBoxColumn});
            this.dataGridView1.DataSource = this.banListBindingSource;
            this.dataGridView1.Location = new System.Drawing.Point(12, 131);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(748, 374);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            // 
            // accDirDataGridViewTextBoxColumn
            // 
            this.accDirDataGridViewTextBoxColumn.DataPropertyName = "AccDir";
            this.accDirDataGridViewTextBoxColumn.HeaderText = "Account";
            this.accDirDataGridViewTextBoxColumn.Name = "accDirDataGridViewTextBoxColumn";
            this.accDirDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // accIDDataGridViewTextBoxColumn
            // 
            this.accIDDataGridViewTextBoxColumn.DataPropertyName = "AccID";
            this.accIDDataGridViewTextBoxColumn.HeaderText = "ID";
            this.accIDDataGridViewTextBoxColumn.Name = "accIDDataGridViewTextBoxColumn";
            this.accIDDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // banReasonDataGridViewTextBoxColumn
            // 
            this.banReasonDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.banReasonDataGridViewTextBoxColumn.DataPropertyName = "BanReason";
            this.banReasonDataGridViewTextBoxColumn.HeaderText = "Reason";
            this.banReasonDataGridViewTextBoxColumn.Name = "banReasonDataGridViewTextBoxColumn";
            this.banReasonDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // banStartDataGridViewTextBoxColumn
            // 
            this.banStartDataGridViewTextBoxColumn.DataPropertyName = "BanStart";
            this.banStartDataGridViewTextBoxColumn.HeaderText = "Start";
            this.banStartDataGridViewTextBoxColumn.Name = "banStartDataGridViewTextBoxColumn";
            this.banStartDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // banEndDataGridViewTextBoxColumn
            // 
            this.banEndDataGridViewTextBoxColumn.DataPropertyName = "BanEnd";
            this.banEndDataGridViewTextBoxColumn.HeaderText = "End";
            this.banEndDataGridViewTextBoxColumn.Name = "banEndDataGridViewTextBoxColumn";
            this.banEndDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // banListBindingSource
            // 
            this.banListBindingSource.DataMember = "BanList";
            this.banListBindingSource.DataSource = damDataSet;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.Location = new System.Drawing.Point(685, 516);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Close";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 522);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Filter";
            // 
            // textBoxFilter
            // 
            this.textBoxFilter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.textBoxFilter.Location = new System.Drawing.Point(44, 520);
            this.textBoxFilter.Name = "textBoxFilter";
            this.textBoxFilter.Size = new System.Drawing.Size(172, 20);
            this.textBoxFilter.TabIndex = 3;
            this.textBoxFilter.TextChanged += new System.EventHandler(this.textBoxFilter_TextChanged);
            // 
            // textBoxCharacters
            // 
            this.textBoxCharacters.Location = new System.Drawing.Point(81, 6);
            this.textBoxCharacters.Name = "textBoxCharacters";
            this.textBoxCharacters.ReadOnly = true;
            this.textBoxCharacters.Size = new System.Drawing.Size(679, 20);
            this.textBoxCharacters.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 5;
            this.label2.Text = "Characters";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(9, 33);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Ban Reason";
            // 
            // richTextBoxBanReason
            // 
            this.richTextBoxBanReason.Location = new System.Drawing.Point(81, 33);
            this.richTextBoxBanReason.Name = "richTextBoxBanReason";
            this.richTextBoxBanReason.ReadOnly = true;
            this.richTextBoxBanReason.Size = new System.Drawing.Size(679, 92);
            this.richTextBoxBanReason.TabIndex = 7;
            this.richTextBoxBanReason.Text = "";
            // 
            // timerFilter
            // 
            this.timerFilter.Tick += new System.EventHandler(this.TimerFilterUpdate);
            // 
            // checkBoxShowExpiredBans
            // 
            this.checkBoxShowExpiredBans.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.checkBoxShowExpiredBans.AutoSize = true;
            this.checkBoxShowExpiredBans.Location = new System.Drawing.Point(249, 522);
            this.checkBoxShowExpiredBans.Name = "checkBoxShowExpiredBans";
            this.checkBoxShowExpiredBans.Size = new System.Drawing.Size(138, 17);
            this.checkBoxShowExpiredBans.TabIndex = 9;
            this.checkBoxShowExpiredBans.Text = "Show expired bans only";
            this.checkBoxShowExpiredBans.UseVisualStyleBackColor = true;
            this.checkBoxShowExpiredBans.CheckedChanged += new System.EventHandler(this.checkBoxShowExpiredBans_CheckedChanged);
            // 
            // buttonUnban
            // 
            this.buttonUnban.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonUnban.Location = new System.Drawing.Point(604, 516);
            this.buttonUnban.Name = "buttonUnban";
            this.buttonUnban.Size = new System.Drawing.Size(75, 23);
            this.buttonUnban.TabIndex = 10;
            this.buttonUnban.Text = "Unban";
            this.buttonUnban.UseVisualStyleBackColor = true;
            this.buttonUnban.Click += new System.EventHandler(this.buttonUnban_Click);
            // 
            // buttonEditBan
            // 
            this.buttonEditBan.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonEditBan.Location = new System.Drawing.Point(523, 516);
            this.buttonEditBan.Name = "buttonEditBan";
            this.buttonEditBan.Size = new System.Drawing.Size(75, 23);
            this.buttonEditBan.TabIndex = 11;
            this.buttonEditBan.Text = "Edit Info";
            this.buttonEditBan.UseVisualStyleBackColor = true;
            this.buttonEditBan.Click += new System.EventHandler(this.buttonEditBan_Click);
            // 
            // buttonShowAccount
            // 
            this.buttonShowAccount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonShowAccount.Location = new System.Drawing.Point(425, 516);
            this.buttonShowAccount.Name = "buttonShowAccount";
            this.buttonShowAccount.Size = new System.Drawing.Size(92, 23);
            this.buttonShowAccount.TabIndex = 12;
            this.buttonShowAccount.Text = "Show Account";
            this.buttonShowAccount.UseVisualStyleBackColor = true;
            this.buttonShowAccount.Click += new System.EventHandler(this.buttonShowAccount_Click);
            // 
            // BannedPlayers
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(772, 546);
            this.Controls.Add(this.buttonShowAccount);
            this.Controls.Add(this.buttonEditBan);
            this.Controls.Add(this.buttonUnban);
            this.Controls.Add(this.checkBoxShowExpiredBans);
            this.Controls.Add(this.richTextBoxBanReason);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxCharacters);
            this.Controls.Add(this.textBoxFilter);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.dataGridView1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "BannedPlayers";
            this.ShowInTaskbar = false;
            this.Text = "Banned Players";
            ((System.ComponentModel.ISupportInitialize)(damDataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.banListBindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxFilter;
        private System.Windows.Forms.TextBox textBoxCharacters;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox richTextBoxBanReason;
        private System.Windows.Forms.Timer timerFilter;
        private System.Windows.Forms.CheckBox checkBoxShowExpiredBans;
        private System.Windows.Forms.Button buttonUnban;
        private System.Windows.Forms.Button buttonEditBan;
        private System.Windows.Forms.DataGridViewTextBoxColumn accDirDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn accIDDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn banReasonDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn banStartDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn banEndDataGridViewTextBoxColumn;
        private System.Windows.Forms.BindingSource banListBindingSource;
        private System.Windows.Forms.Button buttonShowAccount;
        public System.Windows.Forms.DataGridView dataGridView1;
    }
}