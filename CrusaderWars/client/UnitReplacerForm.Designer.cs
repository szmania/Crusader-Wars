namespace CrusaderWars.client
{
    partial class UnitReplacerForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tvCurrentUnits = new System.Windows.Forms.TreeView();
            this.tvAvailableUnits = new System.Windows.Forms.TreeView();
            this.btnReplace = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.InstructionsBox = new System.Windows.Forms.GroupBox();
            this.labelInstructions = new System.Windows.Forms.Label();
            this.InstructionsBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // tvCurrentUnits
            // 
            this.tvCurrentUnits.Location = new System.Drawing.Point(12, 140);
            this.tvCurrentUnits.Name = "tvCurrentUnits";
            this.tvCurrentUnits.Size = new System.Drawing.Size(300, 450);
            this.tvCurrentUnits.TabIndex = 0;
            // 
            // tvAvailableUnits
            // 
            this.tvAvailableUnits.Location = new System.Drawing.Point(472, 140);
            this.tvAvailableUnits.Name = "tvAvailableUnits";
            this.tvAvailableUnits.Size = new System.Drawing.Size(300, 450);
            this.tvAvailableUnits.TabIndex = 1;
            // 
            // btnReplace
            // 
            this.btnReplace.Location = new System.Drawing.Point(344, 350);
            this.btnReplace.Name = "btnReplace";
            this.btnReplace.Size = new System.Drawing.Size(100, 23);
            this.btnReplace.TabIndex = 2;
            this.btnReplace.Text = "-> Replace ->";
            this.btnReplace.UseVisualStyleBackColor = true;
            this.btnReplace.Click += new System.EventHandler(this.btnReplace_Click);
            // 
            // btnOK
            // 
            this.btnOK.Location = new System.Drawing.Point(697, 605);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(616, 605);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 124);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(118, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Current Battle Units";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(469, 124);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(177, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Available Replacement Units";
            // 
            // InstructionsBox
            // 
            this.InstructionsBox.Controls.Add(this.labelInstructions);
            this.InstructionsBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InstructionsBox.Location = new System.Drawing.Point(12, 12);
            this.InstructionsBox.Name = "InstructionsBox";
            this.InstructionsBox.Size = new System.Drawing.Size(760, 100);
            this.InstructionsBox.TabIndex = 7;
            this.InstructionsBox.TabStop = false;
            this.InstructionsBox.Text = "Instructions";
            // 
            // labelInstructions
            // 
            this.labelInstructions.AutoSize = true;
            this.labelInstructions.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelInstructions.Location = new System.Drawing.Point(15, 20);
            this.labelInstructions.Name = "labelInstructions";
            this.labelInstructions.Size = new System.Drawing.Size(454, 65);
            this.labelInstructions.TabIndex = 0;
            this.labelInstructions.Text = "1. Select a unit type to replace from the \'Current Battle Units\' list on the left.\r\n2. Select a unit to replace it with from the \'Available Replacements\' list on the right.\r\n3. Click the \'-> Replace ->\' button.\r\n4. Repeat for other unit types if needed. Replacing one unit replaces ALL units of that same type.\r\n5. Click \'OK\' to confirm your changes and restart the battle.";
            // 
            // UnitReplacerForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(784, 641);
            this.Controls.Add(this.InstructionsBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnReplace);
            this.Controls.Add(this.tvAvailableUnits);
            this.Controls.Add(this.tvCurrentUnits);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UnitReplacerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manual Unit Replacer";
            this.Load += new System.EventHandler(this.UnitReplacerForm_Load);
            this.InstructionsBox.ResumeLayout(false);
            this.InstructionsBox.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TreeView tvCurrentUnits;
        private System.Windows.Forms.TreeView tvAvailableUnits;
        private System.Windows.Forms.Button btnReplace;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.GroupBox InstructionsBox;
        private System.Windows.Forms.Label labelInstructions;
    }
}
