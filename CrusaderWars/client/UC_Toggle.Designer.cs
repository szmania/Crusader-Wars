namespace CrusaderWars.client
{
    partial class UC_Toggle
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
            this.SuspendLayout();
            // 
            // UC_Toggle
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.BackgroundImage = global::CrusaderWars.Properties.Resources.toggle_no;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Name = "UC_Toggle";
            this.Size = new System.Drawing.Size(153, 105);
            this.Click += new System.EventHandler(this.UC_Toggle_Click);
            this.ResumeLayout(false);

        }

        #endregion
    }
}
