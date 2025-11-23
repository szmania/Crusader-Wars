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
            this.textBoxInstructions = new System.Windows.Forms.TextBox();
            this.btnUndo = new System.Windows.Forms.Button();
            this.InstructionsBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // tvCurrentUnits
            // 
            this.tvCurrentUnits.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tvCurrentUnits.Location = new System.Drawing.Point(12, 170);
            this.tvCurrentUnits.Name = "tvCurrentUnits";
            this.tvCurrentUnits.Size = new System.Drawing.Size(300, 450);
            this.tvCurrentUnits.TabIndex = 0;
            this.tvCurrentUnits.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvCurrentUnits_BeforeSelect);
            this.tvCurrentUnits.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvCurrentUnits_NodeMouseClick);
            // 
            // tvAvailableUnits
            // 
            this.tvAvailableUnits.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvAvailableUnits.Location = new System.Drawing.Point(472, 170);
            this.tvAvailableUnits.Name = "tvAvailableUnits";
            this.tvAvailableUnits.Size = new System.Drawing.Size(300, 450);
            this.tvAvailableUnits.TabIndex = 1;
            // 
            // btnReplace
            // 
            this.btnReplace.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnReplace.Location = new System.Drawing.Point(344, 360);
            this.btnReplace.Name = "btnReplace";
            this.btnReplace.Size = new System.Drawing.Size(100, 23);
            this.btnReplace.TabIndex = 2;
            this.btnReplace.Text = "-> Replace ->";
            this.btnReplace.UseVisualStyleBackColor = true;
            this.btnReplace.Click += new System.EventHandler(this.btnReplace_Click);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(697, 635);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 3;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(616, 635);
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
            this.label1.Location = new System.Drawing.Point(12, 154);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(118, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Current Battle Units";
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(469, 154);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(177, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Available Replacement Units";
            // 
            // InstructionsBox
            // 
            this.InstructionsBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.InstructionsBox.Controls.Add(this.textBoxInstructions);
            this.InstructionsBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.InstructionsBox.Location = new System.Drawing.Point(12, 12);
            this.InstructionsBox.Name = "InstructionsBox";
            this.InstructionsBox.Size = new System.Drawing.Size(760, 130);
            this.InstructionsBox.TabIndex = 7;
            this.InstructionsBox.TabStop = false;
            this.InstructionsBox.Text = "Instructions";
            // 
            // textBoxInstructions
            // 
            this.textBoxInstructions.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBoxInstructions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxInstructions.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxInstructions.Location = new System.Drawing.Point(3, 16);
            this.textBoxInstructions.Multiline = true;
            this.textBoxInstructions.Name = "textBoxInstructions";
            this.textBoxInstructions.ReadOnly = true;
            this.textBoxInstructions.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxInstructions.Size = new System.Drawing.Size(754, 111);
            this.textBoxInstructions.TabIndex = 0;
            this.textBoxInstructions.Text = "How to Replace Units:\r\n1. Select units to replace from the \'Current Battle Units\' list.\r\n   (Use Ctrl+Click for multiple units, or Shift+Click for a range).\r\n2. Select a unit to replace them with from the \'Available...\' list on the right.\r\n3. Click \'-> Replace ->\' to mark them for replacement.\r\n4. Repeat for other groups of units with different replacements.\r\n\r\n- Replacing a Men-At-Arm unit replaces all units of that same type.\r\n- The \'Undo\' button will clear all pending replacements.\r\n- Click \'OK\' when you have finished setting all replacements.";
            // 
            // btnUndo
            // 
            this.btnUndo.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnUndo.Location = new System.Drawing.Point(344, 390);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Size = new System.Drawing.Size(100, 23);
            this.btnUndo.TabIndex = 8;
            this.btnUndo.Text = "Undo";
            this.btnUndo.UseVisualStyleBackColor = true;
            this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
            // 
            // UnitReplacerForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(784, 671);
            this.Controls.Add(this.btnUndo);
            this.Controls.Add(this.InstructionsBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnReplace);
            this.Controls.Add(this.tvAvailableUnits);
            this.Controls.Add(this.tvCurrentUnits);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.Name = "UnitReplacerForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Manual Unit Replacer";
            this.Load += new System.EventHandler(this.UnitReplacerForm_Load);
            this.Resize += new System.EventHandler(this.UnitReplacerForm_Resize);
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
        private System.Windows.Forms.TextBox textBoxInstructions;
        private System.Windows.Forms.Button btnUndo;
    }
}
