namespace CrusaderWars.mod_manager
{
    partial class TwGameSelectionForm
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
            this.dataGridViewGames = new System.Windows.Forms.DataGridView();
            this.colYearRange = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colAttila = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colRome2 = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.labelDescription = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewGames)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridViewGames
            // 
            this.dataGridViewGames.AllowUserToAddRows = false;
            this.dataGridViewGames.AllowUserToDeleteRows = false;
            this.dataGridViewGames.AllowUserToResizeRows = false;
            this.dataGridViewGames.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridViewGames.BackgroundColor = System.Drawing.SystemColors.Control;
            this.dataGridViewGames.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewGames.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colYearRange,
            this.colAttila,
            this.colRome2});
            this.dataGridViewGames.Location = new System.Drawing.Point(12, 38);
            this.dataGridViewGames.MultiSelect = false;
            this.dataGridViewGames.Name = "dataGridViewGames";
            this.dataGridViewGames.RowHeadersVisible = false;
            this.dataGridViewGames.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridViewGames.Size = new System.Drawing.Size(460, 200);
            this.dataGridViewGames.TabIndex = 0;
            this.dataGridViewGames.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridViewGames_CellValueChanged);
            this.dataGridViewGames.CurrentCellDirtyStateChanged += new System.EventHandler(this.dataGridViewGames_CurrentCellDirtyStateChanged);
            // 
            // colYearRange
            // 
            this.colYearRange.HeaderText = "Year Range";
            this.colYearRange.Name = "colYearRange";
            this.colYearRange.ReadOnly = true;
            this.colYearRange.Width = 180;
            // 
            // colAttila
            // 
            this.colAttila.HeaderText = "TW: Attila";
            this.colAttila.Name = "colAttila";
            this.colAttila.Width = 130;
            // 
            // colRome2
            // 
            this.colRome2.HeaderText = "TW: Rome 2";
            this.colRome2.Name = "colRome2";
            this.colRome2.Width = 130;
            // 
            // labelDescription
            // 
            this.labelDescription.AutoSize = false;
            this.labelDescription.Location = new System.Drawing.Point(12, 9);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(460, 26);
            this.labelDescription.TabIndex = 3;
            this.labelDescription.Text = "Choose which Total War game runs this playthrough for each time period. Greyed cells have no mapper for that game.";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(316, 246);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 1;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(397, 246);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // TwGameSelectionForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(484, 281);
            this.Controls.Add(this.labelDescription);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.dataGridViewGames);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 220);
            this.Name = "TwGameSelectionForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Total War Game Selection";
            this.Load += new System.EventHandler(this.TwGameSelectionForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewGames)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridViewGames;
        private System.Windows.Forms.DataGridViewTextBoxColumn colYearRange;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colAttila;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colRome2;
        private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
    }
}