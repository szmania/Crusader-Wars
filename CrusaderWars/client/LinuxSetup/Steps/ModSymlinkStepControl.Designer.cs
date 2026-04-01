namespace CrusaderWars.client.LinuxSetup.Steps
{
    partial class ModSymlinkStepControl
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
            this.lblModSymlinkStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblModSymlinkStatus
            // 
            this.lblModSymlinkStatus.AutoSize = true;
            this.lblModSymlinkStatus.Location = new System.Drawing.Point(20, 20);
            this.lblModSymlinkStatus.Name = "lblModSymlinkStatus";
            this.lblModSymlinkStatus.Size = new System.Drawing.Size(135, 13);
            this.lblModSymlinkStatus.TabIndex = 0;
            this.lblModSymlinkStatus.Text = "Creating mod symlinks...";
            // 
            // ModSymlinkStepControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblModSymlinkStatus);
            this.Name = "ModSymlinkStepControl";
            this.Size = new System.Drawing.Size(760, 316);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblModSymlinkStatus;
    }
}
