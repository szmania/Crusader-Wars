using CrusaderWars.client;

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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UC_UnitMapper));
            this.button1 = new System.Windows.Forms.Button();
            this.BtnVerifyMods = new System.Windows.Forms.Button();
            this.uC_Toggle1 = new UC_Toggle();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.button1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Paradox King Script", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(188, 46);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(143, 40);
            this.button1.TabIndex = 2;
            this.button1.Text = "Required Mods";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // BtnVerifyMods
            // 
            this.BtnVerifyMods.BackColor = System.Drawing.Color.Transparent;
            this.BtnVerifyMods.BackgroundImage = global::CrusaderWars.Properties.Resources.searchModsIcon;
            this.BtnVerifyMods.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.BtnVerifyMods.Cursor = System.Windows.Forms.Cursors.Hand;
            this.BtnVerifyMods.FlatAppearance.BorderSize = 0;
            this.BtnVerifyMods.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnVerifyMods.Font = new System.Drawing.Font("Paradox King Script", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnVerifyMods.ForeColor = System.Drawing.Color.White;
            this.BtnVerifyMods.Location = new System.Drawing.Point(326, -1);
            this.BtnVerifyMods.Name = "BtnVerifyMods";
            this.BtnVerifyMods.Size = new System.Drawing.Size(45, 41);
            this.BtnVerifyMods.TabIndex = 3;
            this.toolTip1.SetToolTip(this.BtnVerifyMods, "Click to verify if all correct mods are installed.");
            this.BtnVerifyMods.UseVisualStyleBackColor = false;
            this.BtnVerifyMods.Click += new System.EventHandler(this.BtnVerifyMods_Click);
            // 
            // uC_Toggle1
            // 
            this.uC_Toggle1.BackColor = System.Drawing.Color.Transparent;
            this.uC_Toggle1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("uC_Toggle1.BackgroundImage")));
            this.uC_Toggle1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.uC_Toggle1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.uC_Toggle1.Location = new System.Drawing.Point(217, 92);
            this.uC_Toggle1.Name = "uC_Toggle1";
            this.uC_Toggle1.Size = new System.Drawing.Size(79, 67);
            this.uC_Toggle1.State = true;
            this.uC_Toggle1.TabIndex = 1;
            this.uC_Toggle1.Click += new System.EventHandler(this.uC_Toggle1_Click);
            // 
            // pictureBox1
            // 
            this.pictureBox1.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.pictureBox1.BackgroundImage = global::CrusaderWars.Properties.Resources._default;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox1.Location = new System.Drawing.Point(21, 29);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(133, 122);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // UC_UnitMapper
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.Controls.Add(this.BtnVerifyMods);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.uC_Toggle1);
            this.Controls.Add(this.pictureBox1);
            this.Name = "UC_UnitMapper";
            this.Size = new System.Drawing.Size(370, 183);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox pictureBox1;
        private client.UC_Toggle uC_Toggle1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button BtnVerifyMods;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}
