namespace CrusaderWars.client.LinuxSetup.Steps
{
    partial class DetectionStepControl
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
            this.lblLinuxCheck = new System.Windows.Forms.Label();
            this.lblWineCheck = new System.Windows.Forms.Label();
            this.lblSteamCheck = new System.Windows.Forms.Label();
            this.lblAttilaCheck = new System.Windows.Forms.Label();
            this.lblDesktopEnvCheck = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblLinuxCheck
            // 
            this.lblLinuxCheck.AutoSize = true;
            this.lblLinuxCheck.Location = new System.Drawing.Point(20, 20);
            this.lblLinuxCheck.Name = "lblLinuxCheck";
            this.lblLinuxCheck.Size = new System.Drawing.Size(100, 13);
            this.lblLinuxCheck.TabIndex = 0;
            this.lblLinuxCheck.Text = "Detecting Linux...";
            // 
            // lblWineCheck
            // 
            this.lblWineCheck.AutoSize = true;
            this.lblWineCheck.Location = new System.Drawing.Point(20, 50);
            this.lblWineCheck.Name = "lblWineCheck";
            this.lblWineCheck.Size = new System.Drawing.Size(100, 13);
            this.lblWineCheck.TabIndex = 1;
            this.lblWineCheck.Text = "Detecting Wine...";
            // 
            // lblSteamCheck
            // 
            this.lblSteamCheck.AutoSize = true;
            this.lblSteamCheck.Location = new System.Drawing.Point(20, 80);
            this.lblSteamCheck.Name = "lblSteamCheck";
            this.lblSteamCheck.Size = new System.Drawing.Size(100, 13);
            this.lblSteamCheck.TabIndex = 2;
            this.lblSteamCheck.Text = "Detecting Steam...";
            // 
            // lblAttilaCheck
            // 
            this.lblAttilaCheck.AutoSize = true;
            this.lblAttilaCheck.Location = new System.Drawing.Point(20, 110);
            this.lblAttilaCheck.Name = "lblAttilaCheck";
            this.lblAttilaCheck.Size = new System.Drawing.Size(100, 13);
            this.lblAttilaCheck.TabIndex = 3;
            this.lblAttilaCheck.Text = "Detecting Attila...";
            // 
            // lblDesktopEnvCheck
            // 
            this.lblDesktopEnvCheck.AutoSize = true;
            this.lblDesktopEnvCheck.Location = new System.Drawing.Point(20, 140);
            this.lblDesktopEnvCheck.Name = "lblDesktopEnvCheck";
            this.lblDesktopEnvCheck.Size = new System.Drawing.Size(150, 13);
            this.lblDesktopEnvCheck.TabIndex = 4;
            this.lblDesktopEnvCheck.Text = "Detecting Desktop Environment...";
            // 
            // DetectionStepControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblDesktopEnvCheck);
            this.Controls.Add(this.lblAttilaCheck);
            this.Controls.Add(this.lblSteamCheck);
            this.Controls.Add(this.lblWineCheck);
            this.Controls.Add(this.lblLinuxCheck);
            this.Name = "DetectionStepControl";
            this.Size = new System.Drawing.Size(760, 316);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblLinuxCheck;
        private System.Windows.Forms.Label lblWineCheck;
        private System.Windows.Forms.Label lblSteamCheck;
        private System.Windows.Forms.Label lblAttilaCheck;
        private System.Windows.Forms.Label lblDesktopEnvCheck;
    }
}
