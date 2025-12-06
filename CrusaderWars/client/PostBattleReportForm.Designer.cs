namespace CrusaderWars.client
{
    partial class PostBattleReportForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PostBattleReportForm));
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.lblTitle = new System.Windows.Forms.Label();
            this.treeViewReport = new System.Windows.Forms.TreeView();
            this.btnContinue = new System.Windows.Forms.Button();
            this.lblBattleResult = new System.Windows.Forms.Label();
            this.lblSiegeResult = new System.Windows.Forms.Label();
            this.lblWallDamage = new System.Windows.Forms.Label();
            this.lblBattleName = new System.Windows.Forms.Label();
            this.lblBattleDate = new System.Windows.Forms.Label();
            this.lblLocationDetails = new System.Windows.Forms.Label();
            this.lblTimeOfDay = new System.Windows.Forms.Label();
            this.lblSeason = new System.Windows.Forms.Label();
            this.lblWeather = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::CrusaderWars.Properties.Resources.logo.ToBitmap();
            this.pictureBox1.Location = new System.Drawing.Point(12, 12);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(128, 128);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Georgia", 24F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(212)))), ((int)(((byte)(164)))));
            this.lblTitle.Location = new System.Drawing.Point(146, 12);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(388, 38);
            this.lblTitle.TabIndex = 1;
            this.lblTitle.Text = "After Action Report";
            // 
            // treeViewReport
            // 
            this.treeViewReport.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeViewReport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.treeViewReport.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.treeViewReport.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(212)))), ((int)(((byte)(164)))));
            this.treeViewReport.Location = new System.Drawing.Point(12, 146);
            this.treeViewReport.Name = "treeViewReport";
            this.treeViewReport.Size = new System.Drawing.Size(960, 420);
            this.treeViewReport.TabIndex = 2;
            this.treeViewReport.BeforeExpand += new System.Windows.Forms.TreeViewCancelEventHandler(this.treeViewReport_BeforeExpand);
            this.treeViewReport.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeViewReport_NodeMouseDoubleClick);
            // 
            // btnContinue
            // 
            this.btnContinue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnContinue.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnContinue.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnContinue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(212)))), ((int)(((byte)(164)))));
            this.btnContinue.Location = new System.Drawing.Point(852, 582);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(120, 40);
            this.btnContinue.TabIndex = 3;
            this.btnContinue.Text = "Continue";
            this.btnContinue.UseVisualStyleBackColor = true;
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            // 
            // lblBattleResult
            // 
            this.lblBattleResult.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblBattleResult.AutoSize = true;
            this.lblBattleResult.Font = new System.Drawing.Font("Georgia", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBattleResult.ForeColor = System.Drawing.Color.White;
            this.lblBattleResult.Location = new System.Drawing.Point(12, 578);
            this.lblBattleResult.Name = "lblBattleResult";
            this.lblBattleResult.Size = new System.Drawing.Size(151, 23);
            this.lblBattleResult.TabIndex = 4;
            this.lblBattleResult.Text = "Battle Result: ";
            // 
            // lblSiegeResult
            // 
            this.lblSiegeResult.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblSiegeResult.AutoSize = true;
            this.lblSiegeResult.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSiegeResult.ForeColor = System.Drawing.Color.White;
            this.lblSiegeResult.Location = new System.Drawing.Point(12, 608);
            this.lblSiegeResult.Name = "lblSiegeResult";
            this.lblSiegeResult.Size = new System.Drawing.Size(106, 18);
            this.lblSiegeResult.TabIndex = 5;
            this.lblSiegeResult.Text = "Siege Result: ";
            // 
            // lblWallDamage
            // 
            this.lblWallDamage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblWallDamage.AutoSize = true;
            this.lblWallDamage.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWallDamage.ForeColor = System.Drawing.Color.White;
            this.lblWallDamage.Location = new System.Drawing.Point(300, 608);
            this.lblWallDamage.Name = "lblWallDamage";
            this.lblWallDamage.Size = new System.Drawing.Size(111, 18);
            this.lblWallDamage.TabIndex = 6;
            this.lblWallDamage.Text = "Wall Damage: ";
            // 
            // lblBattleName
            // 
            this.lblBattleName.AutoSize = true;
            this.lblBattleName.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBattleName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(212)))), ((int)(((byte)(164)))));
            this.lblBattleName.Location = new System.Drawing.Point(150, 55);
            this.lblBattleName.Name = "lblBattleName";
            this.lblBattleName.Size = new System.Drawing.Size(107, 18);
            this.lblBattleName.TabIndex = 7;
            this.lblBattleName.Text = "Battle Name: ";
            // 
            // lblBattleDate
            // 
            this.lblBattleDate.AutoSize = true;
            this.lblBattleDate.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblBattleDate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(212)))), ((int)(((byte)(164)))));
            this.lblBattleDate.Location = new System.Drawing.Point(150, 78);
            this.lblBattleDate.Name = "lblBattleDate";
            this.lblBattleDate.Size = new System.Drawing.Size(99, 18);
            this.lblBattleDate.TabIndex = 8;
            this.lblBattleDate.Text = "Battle Date: ";
            // 
            // lblLocationDetails
            // 
            this.lblLocationDetails.AutoSize = true;
            this.lblLocationDetails.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblLocationDetails.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(212)))), ((int)(((byte)(164)))));
            this.lblLocationDetails.Location = new System.Drawing.Point(150, 101);
            this.lblLocationDetails.Name = "lblLocationDetails";
            this.lblLocationDetails.Size = new System.Drawing.Size(127, 18);
            this.lblLocationDetails.TabIndex = 9;
            this.lblLocationDetails.Text = "Location Details:";
            // 
            // lblTimeOfDay
            // 
            this.lblTimeOfDay.AutoSize = true;
            this.lblTimeOfDay.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTimeOfDay.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(212)))), ((int)(((byte)(164)))));
            this.lblTimeOfDay.Location = new System.Drawing.Point(450, 55);
            this.lblTimeOfDay.Name = "lblTimeOfDay";
            this.lblTimeOfDay.Size = new System.Drawing.Size(109, 18);
            this.lblTimeOfDay.TabIndex = 10;
            this.lblTimeOfDay.Text = "Time of Day: ";
            // 
            // lblSeason
            // 
            this.lblSeason.AutoSize = true;
            this.lblSeason.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSeason.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(212)))), ((int)(((byte)(164)))));
            this.lblSeason.Location = new System.Drawing.Point(450, 78);
            this.lblSeason.Name = "lblSeason";
            this.lblSeason.Size = new System.Drawing.Size(69, 18);
            this.lblSeason.TabIndex = 11;
            this.lblSeason.Text = "Season: ";
            // 
            // lblWeather
            // 
            this.lblWeather.AutoSize = true;
            this.lblWeather.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblWeather.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(212)))), ((int)(((byte)(164)))));
            this.lblWeather.Location = new System.Drawing.Point(450, 101);
            this.lblWeather.Name = "lblWeather";
            this.lblWeather.Size = new System.Drawing.Size(79, 18);
            this.lblWeather.TabIndex = 12;
            this.lblWeather.Text = "Weather: ";
            // 
            // PostBattleReportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(43)))), ((int)(((byte)(43)))));
            this.ClientSize = new System.Drawing.Size(984, 634);
            this.Controls.Add(this.lblWeather);
            this.Controls.Add(this.lblSeason);
            this.Controls.Add(this.lblTimeOfDay);
            this.Controls.Add(this.lblLocationDetails);
            this.Controls.Add(this.lblBattleDate);
            this.Controls.Add(this.lblBattleName);
            this.Controls.Add(this.lblWallDamage);
            this.Controls.Add(this.lblSiegeResult);
            this.Controls.Add(this.lblBattleResult);
            this.Controls.Add(this.btnContinue);
            this.Controls.Add(this.treeViewReport);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.pictureBox1);
            // Use the logo from Properties.Resources directly instead of trying to get it from resources object
            // Icon assignment removed - crusader_conflicts_logo is a Bitmap, not an Icon
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.Name = "PostBattleReportForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Crusader Conflicts: After Action Report";
            this.Load += new System.EventHandler(this.PostBattleReportForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.TreeView treeViewReport;
        private System.Windows.Forms.Button btnContinue;
        private System.Windows.Forms.Label lblBattleResult;
        private System.Windows.Forms.Label lblSiegeResult;
        private System.Windows.Forms.Label lblWallDamage;
        private System.Windows.Forms.Label lblBattleName;
        private System.Windows.Forms.Label lblBattleDate;
        private System.Windows.Forms.Label lblLocationDetails;
        private System.Windows.Forms.Label lblTimeOfDay;
        private System.Windows.Forms.Label lblSeason;
        private System.Windows.Forms.Label lblWeather;
    }
}
