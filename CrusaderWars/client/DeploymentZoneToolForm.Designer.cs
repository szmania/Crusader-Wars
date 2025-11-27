namespace CrusaderWars.client
{
    partial class DeploymentZoneToolForm
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
            this.mapPanel = new System.Windows.Forms.Panel();
            this.attackerGroupBox = new System.Windows.Forms.GroupBox();
            this.nudAttackerHeight = new System.Windows.Forms.NumericUpDown();
            this.lblAttackerHeight = new System.Windows.Forms.Label();
            this.nudAttackerWidth = new System.Windows.Forms.NumericUpDown();
            this.lblAttackerWidth = new System.Windows.Forms.Label();
            this.nudAttackerY = new System.Windows.Forms.NumericUpDown();
            this.lblAttackerY = new System.Windows.Forms.Label();
            this.nudAttackerX = new System.Windows.Forms.NumericUpDown();
            this.lblAttackerX = new System.Windows.Forms.Label();
            this.defenderGroupBox = new System.Windows.Forms.GroupBox();
            this.nudDefenderHeight = new System.Windows.Forms.NumericUpDown();
            this.lblDefenderHeight = new System.Windows.Forms.Label();
            this.nudDefenderWidth = new System.Windows.Forms.NumericUpDown();
            this.lblDefenderWidth = new System.Windows.Forms.Label();
            this.nudDefenderY = new System.Windows.Forms.NumericUpDown();
            this.lblDefenderY = new System.Windows.Forms.Label();
            this.nudDefenderX = new System.Windows.Forms.NumericUpDown();
            this.lblDefenderX = new System.Windows.Forms.Label();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblBattleDate = new System.Windows.Forms.Label();
            this.lblBattleType = new System.Windows.Forms.Label();
            this.lblProvinceName = new System.Windows.Forms.Label();
            this.lblCoordinates = new System.Windows.Forms.Label();
            this.attackerGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudAttackerHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAttackerWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAttackerY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAttackerX)).BeginInit();
            this.defenderGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDefenderHeight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDefenderWidth)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDefenderY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDefenderX)).BeginInit();
            this.SuspendLayout();
            // 
            // mapPanel
            // 
            this.mapPanel.BackColor = System.Drawing.Color.DarkGray;
            this.mapPanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.mapPanel.Location = new System.Drawing.Point(12, 12);
            this.mapPanel.Name = "mapPanel";
            this.mapPanel.Size = new System.Drawing.Size(500, 500);
            this.mapPanel.TabIndex = 0;
            // 
            // attackerGroupBox
            // 
            this.attackerGroupBox.Controls.Add(this.nudAttackerHeight);
            this.attackerGroupBox.Controls.Add(this.lblAttackerHeight);
            this.attackerGroupBox.Controls.Add(this.nudAttackerWidth);
            this.attackerGroupBox.Controls.Add(this.lblAttackerWidth);
            this.attackerGroupBox.Controls.Add(this.nudAttackerY);
            this.attackerGroupBox.Controls.Add(this.lblAttackerY);
            this.attackerGroupBox.Controls.Add(this.nudAttackerX);
            this.attackerGroupBox.Controls.Add(this.lblAttackerX);
            this.attackerGroupBox.Location = new System.Drawing.Point(528, 62);
            this.attackerGroupBox.Name = "attackerGroupBox";
            this.attackerGroupBox.Size = new System.Drawing.Size(260, 150);
            this.attackerGroupBox.TabIndex = 1;
            this.attackerGroupBox.TabStop = false;
            this.attackerGroupBox.Text = "Attacker Zone";
            // 
            // nudAttackerHeight
            // 
            this.nudAttackerHeight.DecimalPlaces = 2;
            this.nudAttackerHeight.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudAttackerHeight.Location = new System.Drawing.Point(134, 101);
            this.nudAttackerHeight.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudAttackerHeight.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudAttackerHeight.Name = "nudAttackerHeight";
            this.nudAttackerHeight.Size = new System.Drawing.Size(120, 20);
            this.nudAttackerHeight.TabIndex = 7;
            // 
            // lblAttackerHeight
            // 
            this.lblAttackerHeight.AutoSize = true;
            this.lblAttackerHeight.Location = new System.Drawing.Point(15, 103);
            this.lblAttackerHeight.Name = "lblAttackerHeight";
            this.lblAttackerHeight.Size = new System.Drawing.Size(41, 13);
            this.lblAttackerHeight.TabIndex = 6;
            this.lblAttackerHeight.Text = "Height:";
            // 
            // nudAttackerWidth
            // 
            this.nudAttackerWidth.DecimalPlaces = 2;
            this.nudAttackerWidth.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudAttackerWidth.Location = new System.Drawing.Point(134, 75);
            this.nudAttackerWidth.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudAttackerWidth.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudAttackerWidth.Name = "nudAttackerWidth";
            this.nudAttackerWidth.Size = new System.Drawing.Size(120, 20);
            this.nudAttackerWidth.TabIndex = 5;
            // 
            // lblAttackerWidth
            // 
            this.lblAttackerWidth.AutoSize = true;
            this.lblAttackerWidth.Location = new System.Drawing.Point(15, 77);
            this.lblAttackerWidth.Name = "lblAttackerWidth";
            this.lblAttackerWidth.Size = new System.Drawing.Size(38, 13);
            this.lblAttackerWidth.TabIndex = 4;
            this.lblAttackerWidth.Text = "Width:";
            // 
            // nudAttackerY
            // 
            this.nudAttackerY.DecimalPlaces = 2;
            this.nudAttackerY.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudAttackerY.Location = new System.Drawing.Point(134, 49);
            this.nudAttackerY.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.nudAttackerY.Minimum = new decimal(new int[] {
            5000,
            0,
            0,
            -2147483648});
            this.nudAttackerY.Name = "nudAttackerY";
            this.nudAttackerY.Size = new System.Drawing.Size(120, 20);
            this.nudAttackerY.TabIndex = 3;
            // 
            // lblAttackerY
            // 
            this.lblAttackerY.AutoSize = true;
            this.lblAttackerY.Location = new System.Drawing.Point(15, 51);
            this.lblAttackerY.Name = "lblAttackerY";
            this.lblAttackerY.Size = new System.Drawing.Size(57, 13);
            this.lblAttackerY.TabIndex = 2;
            this.lblAttackerY.Text = "Center Y:";
            // 
            // nudAttackerX
            // 
            this.nudAttackerX.DecimalPlaces = 2;
            this.nudAttackerX.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudAttackerX.Location = new System.Drawing.Point(134, 23);
            this.nudAttackerX.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.nudAttackerX.Minimum = new decimal(new int[] {
            5000,
            0,
            0,
            -2147483648});
            this.nudAttackerX.Name = "nudAttackerX";
            this.nudAttackerX.Size = new System.Drawing.Size(120, 20);
            this.nudAttackerX.TabIndex = 1;
            // 
            // lblAttackerX
            // 
            this.lblAttackerX.AutoSize = true;
            this.lblAttackerX.Location = new System.Drawing.Point(15, 25);
            this.lblAttackerX.Name = "lblAttackerX";
            this.lblAttackerX.Size = new System.Drawing.Size(57, 13);
            this.lblAttackerX.TabIndex = 0;
            this.lblAttackerX.Text = "Center X:";
            // 
            // defenderGroupBox
            // 
            this.defenderGroupBox.Controls.Add(this.nudDefenderHeight);
            this.defenderGroupBox.Controls.Add(this.lblDefenderHeight);
            this.defenderGroupBox.Controls.Add(this.nudDefenderWidth);
            this.defenderGroupBox.Controls.Add(this.lblDefenderWidth);
            this.defenderGroupBox.Controls.Add(this.nudDefenderY);
            this.defenderGroupBox.Controls.Add(this.lblDefenderY);
            this.defenderGroupBox.Controls.Add(this.nudDefenderX);
            this.defenderGroupBox.Controls.Add(this.lblDefenderX);
            this.defenderGroupBox.Location = new System.Drawing.Point(528, 230);
            this.defenderGroupBox.Name = "defenderGroupBox";
            this.defenderGroupBox.Size = new System.Drawing.Size(260, 150);
            this.defenderGroupBox.TabIndex = 2;
            this.defenderGroupBox.TabStop = false;
            this.defenderGroupBox.Text = "Defender Zone";
            // 
            // nudDefenderHeight
            // 
            this.nudDefenderHeight.DecimalPlaces = 2;
            this.nudDefenderHeight.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDefenderHeight.Location = new System.Drawing.Point(134, 101);
            this.nudDefenderHeight.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudDefenderHeight.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDefenderHeight.Name = "nudDefenderHeight";
            this.nudDefenderHeight.Size = new System.Drawing.Size(120, 20);
            this.nudDefenderHeight.TabIndex = 7;
            // 
            // lblDefenderHeight
            // 
            this.lblDefenderHeight.AutoSize = true;
            this.lblDefenderHeight.Location = new System.Drawing.Point(15, 103);
            this.lblDefenderHeight.Name = "lblDefenderHeight";
            this.lblDefenderHeight.Size = new System.Drawing.Size(41, 13);
            this.lblDefenderHeight.TabIndex = 6;
            this.lblDefenderHeight.Text = "Height:";
            // 
            // nudDefenderWidth
            // 
            this.nudDefenderWidth.DecimalPlaces = 2;
            this.nudDefenderWidth.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDefenderWidth.Location = new System.Drawing.Point(134, 75);
            this.nudDefenderWidth.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.nudDefenderWidth.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDefenderWidth.Name = "nudDefenderWidth";
            this.nudDefenderWidth.Size = new System.Drawing.Size(120, 20);
            this.nudDefenderWidth.TabIndex = 5;
            // 
            // lblDefenderWidth
            // 
            this.lblDefenderWidth.AutoSize = true;
            this.lblDefenderWidth.Location = new System.Drawing.Point(15, 77);
            this.lblDefenderWidth.Name = "lblDefenderWidth";
            this.lblDefenderWidth.Size = new System.Drawing.Size(38, 13);
            this.lblDefenderWidth.TabIndex = 4;
            this.lblDefenderWidth.Text = "Width:";
            // 
            // nudDefenderY
            // 
            this.nudDefenderY.DecimalPlaces = 2;
            this.nudDefenderY.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDefenderY.Location = new System.Drawing.Point(134, 49);
            this.nudDefenderY.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.nudDefenderY.Minimum = new decimal(new int[] {
            5000,
            0,
            0,
            -2147483648});
            this.nudDefenderY.Name = "nudDefenderY";
            this.nudDefenderY.Size = new System.Drawing.Size(120, 20);
            this.nudDefenderY.TabIndex = 3;
            // 
            // lblDefenderY
            // 
            this.lblDefenderY.AutoSize = true;
            this.lblDefenderY.Location = new System.Drawing.Point(15, 51);
            this.lblDefenderY.Name = "lblDefenderY";
            this.lblDefenderY.Size = new System.Drawing.Size(57, 13);
            this.lblDefenderY.TabIndex = 2;
            this.lblDefenderY.Text = "Center Y:";
            // 
            // nudDefenderX
            // 
            this.nudDefenderX.DecimalPlaces = 2;
            this.nudDefenderX.Increment = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.nudDefenderX.Location = new System.Drawing.Point(134, 23);
            this.nudDefenderX.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.nudDefenderX.Minimum = new decimal(new int[] {
            5000,
            0,
            0,
            -2147483648});
            this.nudDefenderX.Name = "nudDefenderX";
            this.nudDefenderX.Size = new System.Drawing.Size(120, 20);
            this.nudDefenderX.TabIndex = 1;
            // 
            // lblDefenderX
            // 
            this.lblDefenderX.AutoSize = true;
            this.lblDefenderX.Location = new System.Drawing.Point(15, 25);
            this.lblDefenderX.Name = "lblDefenderX";
            this.lblDefenderX.Size = new System.Drawing.Size(57, 13);
            this.lblDefenderX.TabIndex = 0;
            this.lblDefenderX.Text = "Center X:";
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(632, 489);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(713, 489);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // lblBattleDate
            // 
            this.lblBattleDate.AutoSize = true;
            this.lblBattleDate.Location = new System.Drawing.Point(528, 13);
            this.lblBattleDate.Name = "lblBattleDate";
            this.lblBattleDate.Size = new System.Drawing.Size(63, 13);
            this.lblBattleDate.TabIndex = 5;
            this.lblBattleDate.Text = "Battle Date:";
            // 
            // lblBattleType
            // 
            this.lblBattleType.AutoSize = true;
            this.lblBattleType.Location = new System.Drawing.Point(528, 35);
            this.lblBattleType.Name = "lblBattleType";
            this.lblBattleType.Size = new System.Drawing.Size(65, 13);
            this.lblBattleType.TabIndex = 6;
            this.lblBattleType.Text = "Battle Type:";
            // 
            // lblProvinceName
            // 
            this.lblProvinceName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblProvinceName.Location = new System.Drawing.Point(597, 13);
            this.lblProvinceName.Name = "lblProvinceName";
            this.lblProvinceName.Size = new System.Drawing.Size(191, 13);
            this.lblProvinceName.TabIndex = 7;
            this.lblProvinceName.Text = "Province:";
            this.lblProvinceName.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // lblCoordinates
            // 
            this.lblCoordinates.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblCoordinates.Location = new System.Drawing.Point(597, 35);
            this.lblCoordinates.Name = "lblCoordinates";
            this.lblCoordinates.Size = new System.Drawing.Size(191, 13);
            this.lblCoordinates.TabIndex = 8;
            this.lblCoordinates.Text = "Coords:";
            this.lblCoordinates.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // DeploymentZoneToolForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 524);
            this.Controls.Add(this.lblCoordinates);
            this.Controls.Add(this.lblProvinceName);
            this.Controls.Add(this.lblBattleType);
            this.Controls.Add(this.lblBattleDate);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.defenderGroupBox);
            this.Controls.Add(this.attackerGroupBox);
            this.Controls.Add(this.mapPanel);
            this.Name = "DeploymentZoneToolForm";
            this.Text = "Deployment Zone Editor";
            this.attackerGroupBox.ResumeLayout(false);
            this.attackerGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudAttackerHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAttackerWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAttackerY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudAttackerX)).EndInit();
            this.defenderGroupBox.ResumeLayout(false);
            this.defenderGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDefenderHeight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDefenderWidth)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDefenderY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDefenderX)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel mapPanel;
        private System.Windows.Forms.GroupBox attackerGroupBox;
        private System.Windows.Forms.NumericUpDown nudAttackerHeight;
        private System.Windows.Forms.Label lblAttackerHeight;
        private System.Windows.Forms.NumericUpDown nudAttackerWidth;
        private System.Windows.Forms.Label lblAttackerWidth;
        private System.Windows.Forms.NumericUpDown nudAttackerY;
        private System.Windows.Forms.Label lblAttackerY;
        private System.Windows.Forms.NumericUpDown nudAttackerX;
        private System.Windows.Forms.Label lblAttackerX;
        private System.Windows.Forms.GroupBox defenderGroupBox;
        private System.Windows.Forms.NumericUpDown nudDefenderHeight;
        private System.Windows.Forms.Label lblDefenderHeight;
        private System.Windows.Forms.NumericUpDown nudDefenderWidth;
        private System.Windows.Forms.Label lblDefenderWidth;
        private System.Windows.Forms.NumericUpDown nudDefenderY;
        private System.Windows.Forms.Label lblDefenderY;
        private System.Windows.Forms.NumericUpDown nudDefenderX;
        private System.Windows.Forms.Label lblDefenderX;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblBattleDate;
        private System.Windows.Forms.Label lblBattleType;
        private System.Windows.Forms.Label lblProvinceName;
        private System.Windows.Forms.Label lblCoordinates;
    }
}
