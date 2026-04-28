namespace CrusaderWars.client.LinuxSetup.Steps
{
    partial class ShortcutCreationStepControl
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
            this.lblShortcutStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblShortcutStatus
            // 
            this.lblShortcutStatus.AutoSize = true;
            this.lblShortcutStatus.Location = new System.Drawing.Point(20, 20);
            this.lblShortcutStatus.Name = "lblShortcutStatus";
            this.lblShortcutStatus.Size = new System.Drawing.Size(106, 13);
            this.lblShortcutStatus.TabIndex = 0;
            this.lblShortcutStatus.Text = "Steam Integration...";
            // 
            // ShortcutCreationStepControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblShortcutStatus);
            this.Name = "ShortcutCreationStepControl";
            this.Size = new System.Drawing.Size(760, 316);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblShortcutStatus;
    }
}
