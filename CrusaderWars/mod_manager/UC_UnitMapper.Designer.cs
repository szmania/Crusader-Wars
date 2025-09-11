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
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pictureBox1.Location = new System.Drawing.Point(20, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(139, 139);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(290, 80);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(160, 30);
            this.button1.TabIndex = 2;
            this.button1.Text = "Required Mods";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(53)))), ((int)(((byte)(0)))));
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Paradox King Script", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // BtnVerifyMods
            // 
            this.BtnVerifyMods.Location = new System.Drawing.Point(410, 3);
            this.BtnVerifyMods.Name = "BtnVerifyMods";
            this.BtnVerifyMods.Size = new System.Drawing.Size(35, 35);
            this.BtnVerifyMods.TabIndex = 3;
            this.BtnVerifyMods.Text = "";
            this.BtnVerifyMods.UseVisualStyleBackColor = true;
            this.BtnVerifyMods.BackgroundImage = global::CrusaderWars.Properties.Resources.searchModsIcon;
            this.BtnVerifyMods.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.BtnVerifyMods.Click += new System.EventHandler(this.BtnVerifyMods_Click);
            // 
            // toolTip1
            // 
            this.toolTip1.ToolTipTitle = "Mod Verification";
            this.toolTip1.SetToolTip(this.BtnVerifyMods, "Verify required mods are installed");
            // 
            // uC_Toggle1
            // 
            this.uC_Toggle1.BackgroundImage = global::CrusaderWars.Properties.Resources.toggle_no;
            this.uC_Toggle1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.uC_Toggle1.Location = new System.Drawing.Point(183, 58);
            this.uC_Toggle1.Name = "uC_Toggle1";
            this.uC_Toggle1.Size = new System.Drawing.Size(80, 45);
            this.uC_Toggle1.TabIndex = 1;
            this.uC_Toggle1.Click += new System.EventHandler(this.uC_Toggle1_Click);
            // 
            // UC_UnitMapper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.uC_Toggle1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.BtnVerifyMods);
            this.Name = "UC_UnitMapper";
            this.Size = new System.Drawing.Size(480, 185);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.UC_UnitMapper_Paint);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        internal client.UC_Toggle uC_Toggle1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button BtnVerifyMods;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
