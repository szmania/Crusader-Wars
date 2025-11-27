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
            this.components = new System.ComponentModel.Container();
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
            this.UnitReplacerToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.txtSearchCurrent = new System.Windows.Forms.TextBox();
            this.btnPrevCurrent = new System.Windows.Forms.Button();
            this.btnNextCurrent = new System.Windows.Forms.Button();
            this.txtSearchAvailable = new System.Windows.Forms.TextBox();
            this.btnPrevAvailable = new System.Windows.Forms.Button();
            this.btnNextAvailable = new System.Windows.Forms.Button();
            this.btnSearchCurrent = new System.Windows.Forms.Button();
            this.btnSearchAvailable = new System.Windows.Forms.Button();
            this.lblCurrentSearch = new System.Windows.Forms.Label();
            this.lblAvailableSearch = new System.Windows.Forms.Label();
            this.InstructionsBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // tvCurrentUnits
            // 
            this.tvCurrentUnits.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.tvCurrentUnits.Location = new System.Drawing.Point(12, 196);
            this.tvCurrentUnits.HideSelection = false;
            this.tvCurrentUnits.Name = "tvCurrentUnits";
            this.tvCurrentUnits.Size = new System.Drawing.Size(480, 424);
            this.tvCurrentUnits.TabIndex = 0;
            this.tvCurrentUnits.BeforeSelect += new System.Windows.Forms.TreeViewCancelEventHandler(this.tvCurrentUnits_BeforeSelect);
            this.tvCurrentUnits.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.tvCurrentUnits_NodeMouseClick);
            // 
            // tvAvailableUnits
            // 
            this.tvAvailableUnits.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tvAvailableUnits.Location = new System.Drawing.Point(592, 196);
            this.tvAvailableUnits.Name = "tvAvailableUnits";
            this.tvAvailableUnits.Size = new System.Drawing.Size(480, 424);
            this.tvAvailableUnits.TabIndex = 1;
            // 
            // btnReplace
            // 
            this.btnReplace.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnReplace.Location = new System.Drawing.Point(495, 360);
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
            this.btnOK.Location = new System.Drawing.Point(997, 635);
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
            this.btnCancel.Location = new System.Drawing.Point(916, 635);
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
            this.label1.Location = new System.Drawing.Point(12, 180);
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
            this.label2.Location = new System.Drawing.Point(589, 180);
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
            this.InstructionsBox.Size = new System.Drawing.Size(1060, 130);
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
            this.textBoxInstructions.Size = new System.Drawing.Size(1054, 111);
            this.textBoxInstructions.TabIndex = 0;
            this.textBoxInstructions.Text = "How to Replace Units:\r\n1. Select one or more unit groups from the 'Current Battle Units' list on the left.\r\n   (Use Ctrl+Click to select multiple groups).\r\n2. Select a single unit to replace them with from the 'Available...' list on the right.\r\n3. Click '-> Replace ->' to mark the group(s) for replacement.\r\n4. Repeat for other groups of units if you want different replacements.\r\n\r\n- All units within a group (e.g., all Levies, all Longbowmen) will be replaced.\r\n- The 'Undo' button will clear all pending replacements.\r\n- Click 'OK' when you have finished setting all replacements.";
            // 
            // btnUndo
            // 
            this.btnUndo.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnUndo.Location = new System.Drawing.Point(495, 390);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Size = new System.Drawing.Size(100, 23);
            this.btnUndo.TabIndex = 8;
            this.btnUndo.Text = "Undo";
            this.btnUndo.UseVisualStyleBackColor = true;
            this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
            // 
            // UnitReplacerToolTip
            // 
            this.UnitReplacerToolTip.SetToolTip(this.tvCurrentUnits, "List of units currently in the battle. Use Ctrl+Click or Shift+Click to select m" +
        "ultiple units to replace.");
            this.UnitReplacerToolTip.SetToolTip(this.tvAvailableUnits, "List of all possible units you can use as replacements. Select one unit from this" +
        " list.");
            this.UnitReplacerToolTip.SetToolTip(this.btnReplace, "Applies the selected replacement to the unit(s) chosen on the left.");
            this.UnitReplacerToolTip.SetToolTip(this.btnUndo, "Clears all pending replacements you have made in this window.");
            this.UnitReplacerToolTip.SetToolTip(this.btnOK, "Saves all replacements and continues with the battle.");
            this.UnitReplacerToolTip.SetToolTip(this.btnCancel, "Discards all replacements and closes this window.");
            // 
            // txtSearchCurrent
            // 
            this.txtSearchCurrent.Location = new System.Drawing.Point(62, 151);
            this.txtSearchCurrent.Name = "txtSearchCurrent";
            this.txtSearchCurrent.Size = new System.Drawing.Size(200, 20);
            this.txtSearchCurrent.TabIndex = 10;
            this.UnitReplacerToolTip.SetToolTip(this.txtSearchCurrent, "Type here to search for units in the list below. Press Enter to search.");
            // 
            // btnPrevCurrent
            // 
            this.btnPrevCurrent.Location = new System.Drawing.Point(268, 149);
            this.btnPrevCurrent.Name = "btnPrevCurrent";
            this.btnPrevCurrent.Size = new System.Drawing.Size(44, 23);
            this.btnPrevCurrent.TabIndex = 11;
            this.btnPrevCurrent.Text = "▲";
            this.UnitReplacerToolTip.SetToolTip(this.btnPrevCurrent, "Go to the previous search result.");
            this.btnPrevCurrent.UseVisualStyleBackColor = true;
            this.btnPrevCurrent.Visible = false;
            this.btnPrevCurrent.Click += new System.EventHandler(this.btnPrevCurrent_Click);
            // 
            // btnNextCurrent
            // 
            this.btnNextCurrent.Location = new System.Drawing.Point(318, 149);
            this.btnNextCurrent.Name = "btnNextCurrent";
            this.btnNextCurrent.Size = new System.Drawing.Size(44, 23);
            this.btnNextCurrent.TabIndex = 12;
            this.btnNextCurrent.Text = "▼";
            this.UnitReplacerToolTip.SetToolTip(this.btnNextCurrent, "Go to the next search result.");
            this.btnNextCurrent.UseVisualStyleBackColor = true;
            this.btnNextCurrent.Visible = false;
            this.btnNextCurrent.Click += new System.EventHandler(this.btnNextCurrent_Click);
            // 
            // txtSearchAvailable
            // 
            this.txtSearchAvailable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearchAvailable.Location = new System.Drawing.Point(642, 151);
            this.txtSearchAvailable.Name = "txtSearchAvailable";
            this.txtSearchAvailable.Size = new System.Drawing.Size(200, 20);
            this.txtSearchAvailable.TabIndex = 14;
            this.UnitReplacerToolTip.SetToolTip(this.txtSearchAvailable, "Type here to search for units in the list below. Press Enter to search.");
            // 
            // btnPrevAvailable
            // 
            this.btnPrevAvailable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnPrevAvailable.Location = new System.Drawing.Point(848, 149);
            this.btnPrevAvailable.Name = "btnPrevAvailable";
            this.btnPrevAvailable.Size = new System.Drawing.Size(44, 23);
            this.btnPrevAvailable.TabIndex = 15;
            this.btnPrevAvailable.Text = "▲";
            this.UnitReplacerToolTip.SetToolTip(this.btnPrevAvailable, "Go to the previous search result.");
            this.btnPrevAvailable.UseVisualStyleBackColor = true;
            this.btnPrevAvailable.Visible = false;
            this.btnPrevAvailable.Click += new System.EventHandler(this.btnPrevAvailable_Click);
            // 
            // btnNextAvailable
            // 
            this.btnNextAvailable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNextAvailable.Location = new System.Drawing.Point(898, 149);
            this.btnNextAvailable.Name = "btnNextAvailable";
            this.btnNextAvailable.Size = new System.Drawing.Size(44, 23);
            this.btnNextAvailable.TabIndex = 16;
            this.btnNextAvailable.Text = "▼";
            this.UnitReplacerToolTip.SetToolTip(this.btnNextAvailable, "Go to the next search result.");
            this.btnNextAvailable.UseVisualStyleBackColor = true;
            this.btnNextAvailable.Visible = false;
            this.btnNextAvailable.Click += new System.EventHandler(this.btnNextAvailable_Click);
            // 
            // btnSearchCurrent
            // 
            this.btnSearchCurrent.Location = new System.Drawing.Point(268, 149);
            this.btnSearchCurrent.Name = "btnSearchCurrent";
            this.btnSearchCurrent.Size = new System.Drawing.Size(94, 23);
            this.btnSearchCurrent.TabIndex = 17;
            this.btnSearchCurrent.Text = "Search";
            this.UnitReplacerToolTip.SetToolTip(this.btnSearchCurrent, "Search for units in the list.");
            this.btnSearchCurrent.UseVisualStyleBackColor = true;
            this.btnSearchCurrent.Click += new System.EventHandler(this.btnSearchCurrent_Click);
            // 
            // btnSearchAvailable
            // 
            this.btnSearchAvailable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSearchAvailable.Location = new System.Drawing.Point(848, 149);
            this.btnSearchAvailable.Name = "btnSearchAvailable";
            this.btnSearchAvailable.Size = new System.Drawing.Size(94, 23);
            this.btnSearchAvailable.TabIndex = 18;
            this.btnSearchAvailable.Text = "Search";
            this.UnitReplacerToolTip.SetToolTip(this.btnSearchAvailable, "Search for units in the list.");
            this.btnSearchAvailable.UseVisualStyleBackColor = true;
            this.btnSearchAvailable.Click += new System.EventHandler(this.btnSearchAvailable_Click);
            // 
            // lblCurrentSearch
            // 
            this.lblCurrentSearch.AutoSize = true;
            this.lblCurrentSearch.Location = new System.Drawing.Point(12, 154);
            this.lblCurrentSearch.Name = "lblCurrentSearch";
            this.lblCurrentSearch.Size = new System.Drawing.Size(44, 13);
            this.lblCurrentSearch.TabIndex = 9;
            this.lblCurrentSearch.Text = "Search:";
            // 
            // lblAvailableSearch
            // 
            this.lblAvailableSearch.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAvailableSearch.AutoSize = true;
            this.lblAvailableSearch.Location = new System.Drawing.Point(589, 154);
            this.lblAvailableSearch.Name = "lblAvailableSearch";
            this.lblAvailableSearch.Size = new System.Drawing.Size(44, 13);
            this.lblAvailableSearch.TabIndex = 13;
            this.lblAvailableSearch.Text = "Search:";
            // 
            // UnitReplacerForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(1084, 671);
            this.Controls.Add(this.lblAvailableSearch);
            this.Controls.Add(this.lblCurrentSearch);
            this.Controls.Add(this.btnNextAvailable);
            this.Controls.Add(this.btnPrevAvailable);
            this.Controls.Add(this.txtSearchAvailable);
            this.Controls.Add(this.btnNextCurrent);
            this.Controls.Add(this.btnPrevCurrent);
            this.Controls.Add(this.txtSearchCurrent);
            this.Controls.Add(this.btnUndo);
            this.Controls.Add(this.btnSearchCurrent);
            this.Controls.Add(this.btnSearchAvailable);
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
            this.Text = "Unit Replacer";
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
        private System.Windows.Forms.ToolTip UnitReplacerToolTip;
        private System.Windows.Forms.Label lblAvailableSearch;
        private System.Windows.Forms.Label lblCurrentSearch;
        private System.Windows.Forms.Button btnNextAvailable;
        private System.Windows.Forms.Button btnPrevAvailable;
        private System.Windows.Forms.TextBox txtSearchAvailable;
        private System.Windows.Forms.Button btnNextCurrent;
        private System.Windows.Forms.Button btnPrevCurrent;
        private System.Windows.Forms.TextBox txtSearchCurrent;
        private System.Windows.Forms.Button btnSearchCurrent;
        private System.Windows.Forms.Button btnSearchAvailable;
    }
}
