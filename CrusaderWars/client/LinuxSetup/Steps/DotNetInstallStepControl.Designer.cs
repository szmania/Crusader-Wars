namespace CrusaderWars.client.LinuxSetup.Steps
{
    partial class DotNetInstallStepControl
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
            this.lblDotNetStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblDotNetStatus
            // 
            this.lblDotNetStatus.AutoSize = true;
            this.lblDotNetStatus.Location = new System.Drawing.Point(20, 20);
            this.lblDotNetStatus.Name = "lblDotNetStatus";
            this.lblDotNetStatus.Size = new System.Drawing.Size(121, 13);
            this.lblDotNetStatus.TabIndex = 0;
            this.lblDotNetStatus.Text = "Preparing to install .NET...";
            // 
            // DotNetInstallStepControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblDotNetStatus);
            this.Name = "DotNetInstallStepControl";
            this.Size = new System.Drawing.Size(760, 316);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblDotNetStatus;
    }
}
