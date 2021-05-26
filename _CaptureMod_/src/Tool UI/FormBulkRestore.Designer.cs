namespace DAM.Tool_UI
{
    partial class FormBulkRestore
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormBulkRestore));
            this.textBoxBackupFile = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxFilesToRestore = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonRestore = new System.Windows.Forms.Button();
            this.textBoxLog = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // textBoxBackupFile
            // 
            this.textBoxBackupFile.Location = new System.Drawing.Point(102, 10);
            this.textBoxBackupFile.Name = "textBoxBackupFile";
            this.textBoxBackupFile.Size = new System.Drawing.Size(441, 20);
            this.textBoxBackupFile.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 13);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(84, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Backup file path";
            // 
            // textBoxFilesToRestore
            // 
            this.textBoxFilesToRestore.Location = new System.Drawing.Point(12, 56);
            this.textBoxFilesToRestore.Multiline = true;
            this.textBoxFilesToRestore.Name = "textBoxFilesToRestore";
            this.textBoxFilesToRestore.Size = new System.Drawing.Size(612, 157);
            this.textBoxFilesToRestore.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(75, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Files to restore";
            // 
            // buttonRestore
            // 
            this.buttonRestore.Location = new System.Drawing.Point(549, 7);
            this.buttonRestore.Name = "buttonRestore";
            this.buttonRestore.Size = new System.Drawing.Size(75, 23);
            this.buttonRestore.TabIndex = 4;
            this.buttonRestore.Text = "Restore Now";
            this.buttonRestore.UseVisualStyleBackColor = true;
            this.buttonRestore.Click += new System.EventHandler(this.buttonRestore_Click);
            // 
            // textBoxLog
            // 
            this.textBoxLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxLog.Location = new System.Drawing.Point(15, 219);
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.Size = new System.Drawing.Size(609, 204);
            this.textBoxLog.TabIndex = 5;
            this.textBoxLog.Text = "";
            // 
            // FormBulkRestore
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(636, 435);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.buttonRestore);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxFilesToRestore);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxBackupFile);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "FormBulkRestore";
            this.Text = "Bulk Restore";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxBackupFile;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxFilesToRestore;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonRestore;
        private System.Windows.Forms.RichTextBox textBoxLog;
    }
}