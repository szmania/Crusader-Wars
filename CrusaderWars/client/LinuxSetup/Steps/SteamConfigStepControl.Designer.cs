namespace CrusaderWars.client.LinuxSetup.Steps
{
    partial class SteamConfigStepControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        private void InitializeComponent()
        {
            this.lblSteamConfigStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblSteamConfigStatus
            // 
            this.lblSteamConfigStatus.AutoSize = true;
            this.lblSteamConfigStatus.Location = new System.Drawing.Point(20, 20);
            this.lblSteamConfigStatus.Name = "lblSteamConfigStatus";
            this.lblSteamConfigStatus.Size = new System.Drawing.Size(350, 13);
            this.lblSteamConfigStatus.TabIndex = 0;
            this.lblSteamConfigStatus.Text = "Please set the following launch options for Attila in Steam:\n%command% used_mods_cw.txt";
            // 
            // SteamConfigStepControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblSteamConfigStatus);
            this.Name = "SteamConfigStepControl";
            this.Size = new System.Drawing.Size(760, 316);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblSteamConfigStatus;
    }
}
