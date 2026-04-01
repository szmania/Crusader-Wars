namespace CrusaderWars.client.LinuxSetup.Steps
{
    partial class CompletionStepControl
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
            this.lblCompletionStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblCompletionStatus
            // 
            this.lblCompletionStatus.AutoSize = true;
            this.lblCompletionStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCompletionStatus.Location = new System.Drawing.Point(20, 20);
            this.lblCompletionStatus.Name = "lblCompletionStatus";
            this.lblCompletionStatus.Size = new System.Drawing.Size(142, 20);
            this.lblCompletionStatus.TabIndex = 0;
            this.lblCompletionStatus.Text = "Setup complete!";
            // 
            // CompletionStepControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblCompletionStatus);
            this.Name = "CompletionStepControl";
            this.Size = new System.Drawing.Size(760, 316);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblCompletionStatus;
    }
}
