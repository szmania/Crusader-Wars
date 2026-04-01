namespace CrusaderWars.client.LinuxSetup.Steps
{
    partial class WinePrefixStepControl
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
            this.lblWinePrefixStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblWinePrefixStatus
            // 
            this.lblWinePrefixStatus.AutoSize = true;
            this.lblWinePrefixStatus.Location = new System.Drawing.Point(20, 20);
            this.lblWinePrefixStatus.Name = "lblWinePrefixStatus";
            this.lblWinePrefixStatus.Size = new System.Drawing.Size(124, 13);
            this.lblWinePrefixStatus.TabIndex = 0;
            this.lblWinePrefixStatus.Text = "Creating Wine prefix...";
            // 
            // WinePrefixStepControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblWinePrefixStatus);
            this.Name = "WinePrefixStepControl";
            this.Size = new System.Drawing.Size(760, 316);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblWinePrefixStatus;
    }
}
