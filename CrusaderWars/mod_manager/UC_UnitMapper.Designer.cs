namespace CrusaderWars.mod_manager
{
    partial class UC_UnitMapper
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.button1 = new System.Windows.Forms.Button();
            this.BtnVerifyMods = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.uC_Toggle1 = new CrusaderWars.client.UC_Toggle();
            this.BtnSubmods = new System.Windows.Forms.Button();
            this.playthroughTitleLabel = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Location = new System.Drawing.Point(30, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(139, 139);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(53)))), ((int)(((byte)(0)))));
            this.button1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Paradox King Script", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.White;
this.button1.Location = new System.Drawing.Point(720, 60);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(160, 30);
            this.button1.TabIndex = 2;
            this.button1.Text = "Required Mods";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // BtnVerifyMods
            // 
            this.BtnVerifyMods.BackgroundImage = global::CrusaderWars.Properties.Resources.searchModsIcon;
            this.BtnVerifyMods.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.BtnVerifyMods.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnVerifyMods.Location = new System.Drawing.Point(183, 10);
            this.BtnVerifyMods.Name = "BtnVerifyMods";
            this.BtnVerifyMods.Size = new System.Drawing.Size(40, 35);
            this.BtnVerifyMods.TabIndex = 3;
            this.BtnVerifyMods.Text = "";
            this.toolTip1.SetToolTip(this.BtnVerifyMods, "Verify required mods are installed");
            this.BtnVerifyMods.UseVisualStyleBackColor = true;
            this.BtnVerifyMods.Click += new System.EventHandler(this.BtnVerifyMods_Click);
            // 
            // toolTip1
            // 
            this.toolTip1.ToolTipTitle = "Mod Verification";
            // 
            // uC_Toggle1
            // 
            this.uC_Toggle1.BackgroundImage = global::CrusaderWars.Properties.Resources.toggle_no;
            this.uC_Toggle1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
this.uC_Toggle1.Location = new System.Drawing.Point(405, 42);
            this.uC_Toggle1.Name = "uC_Toggle1";
            this.uC_Toggle1.Size = new System.Drawing.Size(80, 60);
this.uC_Toggle1.TabIndex = 1;
            this.toolTip1.SetToolTip(this.uC_Toggle1, "Enable or disable this playthrough. Only one playthrough can be active at a time.");
            this.uC_Toggle1.Click += new System.EventHandler(this.uC_Toggle1_Click);
            // 
            // BtnSubmods
            // 
            this.BtnSubmods.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnSubmods.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(53)))), ((int)(((byte)(0)))));
            this.BtnSubmods.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnSubmods.FlatAppearance.BorderSize = 0;
            this.BtnSubmods.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnSubmods.Font = new System.Drawing.Font("Paradox King Script", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnSubmods.ForeColor = System.Drawing.Color.White;
this.BtnSubmods.Location = new System.Drawing.Point(720, 100);
            this.BtnSubmods.Name = "BtnSubmods";
            this.BtnSubmods.Size = new System.Drawing.Size(160, 30);
            this.BtnSubmods.TabIndex = 4;
            this.BtnSubmods.Text = "OPTIONAL SUB-MODS";
            this.toolTip1.SetToolTip(this.BtnSubmods, "Select supported optional sub-mods for Total War: Attila that add additional fac" +
        "tions or units.");
            this.BtnSubmods.UseVisualStyleBackColor = true;
            this.BtnSubmods.Visible = false;
            this.BtnSubmods.Click += new System.EventHandler(this.BtnSubmods_Click);
            // 
            // customMapperLabel
            // 
            this.customMapperLabel = new System.Windows.Forms.Label();
            this.customMapperLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.customMapperLabel.AutoSize = true;
            this.customMapperLabel.Font = new System.Drawing.Font("Paradox King Script", 9.75F);
            this.customMapperLabel.ForeColor = System.Drawing.Color.White;
this.customMapperLabel.Location = new System.Drawing.Point(779, 10);
            this.customMapperLabel.Name = "customMapperLabel";
            this.customMapperLabel.Size = new System.Drawing.Size(101, 16);
            this.customMapperLabel.TabIndex = 5;
            this.customMapperLabel.Text = "Select Mapper:";
            this.customMapperLabel.Visible = false;
            // 
            // customMapperComboBox
            // 
            this.customMapperComboBox = new System.Windows.Forms.ComboBox();
            this.customMapperComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.customMapperComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.customMapperComboBox.FormattingEnabled = true;
this.customMapperComboBox.Location = new System.Drawing.Point(720, 30);
            this.customMapperComboBox.Name = "customMapperComboBox";
            this.customMapperComboBox.Size = new System.Drawing.Size(160, 21);
            this.customMapperComboBox.TabIndex = 6;
            this.customMapperComboBox.Visible = false;
            // 
            // 
            // playthroughTitleLabel
            // 
            this.playthroughTitleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.playthroughTitleLabel.AutoSize = false;
            this.playthroughTitleLabel.BackColor = System.Drawing.Color.Transparent;
            this.playthroughTitleLabel.Font = new System.Drawing.Font("Paradox King Script", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.playthroughTitleLabel.ForeColor = System.Drawing.Color.White;
            this.playthroughTitleLabel.Location = new System.Drawing.Point(0, 8);
            this.playthroughTitleLabel.Name = "playthroughTitleLabel";
            this.playthroughTitleLabel.Size = new System.Drawing.Size(890, 30);
            this.playthroughTitleLabel.TabIndex = 7;
            this.playthroughTitleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // twGameLabel
            // 
            this.twGameLabel = new System.Windows.Forms.Label();
            this.twGameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.twGameLabel.AutoSize = true;
            this.twGameLabel.Font = new System.Drawing.Font("Paradox King Script", 9.75F);
            this.twGameLabel.ForeColor = System.Drawing.Color.White;
            this.twGameLabel.Location = new System.Drawing.Point(720, 136);
            this.twGameLabel.Name = "twGameLabel";
            this.twGameLabel.Size = new System.Drawing.Size(101, 16);
            this.twGameLabel.TabIndex = 8;
            this.twGameLabel.Text = "TW Game:";
            this.twGameLabel.Visible = false;
            // 
            // twGameComboBox
            // 
            this.twGameComboBox = new System.Windows.Forms.ComboBox();
            this.twGameComboBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.twGameComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.twGameComboBox.FormattingEnabled = true;
            this.twGameComboBox.Location = new System.Drawing.Point(720, 154);
            this.twGameComboBox.Name = "twGameComboBox";
            this.twGameComboBox.Size = new System.Drawing.Size(160, 21);
            this.twGameComboBox.TabIndex = 9;
            this.twGameComboBox.Visible = false;
            this.toolTip1.SetToolTip(this.twGameComboBox, "When both Total War: Attila and Total War: Rome 2 have a mapper for this playthrough in the same time period, this chooses which game is used. 'Auto' prefers Attila.");
            // 
            // UC_UnitMapper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.customMapperLabel);
            this.Controls.Add(this.customMapperComboBox);
            this.Controls.Add(this.BtnSubmods);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.uC_Toggle1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.BtnVerifyMods);
            this.Controls.Add(this.playthroughTitleLabel);
            this.Controls.Add(this.twGameLabel);
            this.Controls.Add(this.twGameComboBox);
            this.Name = "UC_UnitMapper";
            this.Size = new System.Drawing.Size(890, 185);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.UC_UnitMapper_Paint);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        internal client.UC_Toggle uC_Toggle1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button BtnVerifyMods;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Button BtnSubmods;
        private System.Windows.Forms.Label playthroughTitleLabel;
        internal System.Windows.Forms.Label customMapperLabel;
        internal System.Windows.Forms.ComboBox customMapperComboBox;
        internal System.Windows.Forms.Label twGameLabel;
        internal System.Windows.Forms.ComboBox twGameComboBox;
    }
}
