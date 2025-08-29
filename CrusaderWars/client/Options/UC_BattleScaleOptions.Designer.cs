namespace CrusaderWars.client
{
    partial class UC_BattleScaleOptions
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
            this.components = new System.ComponentModel.Container();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.OptionSelection_BattleSizeScale = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.OptionSelection_AutoScale = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 66.66666F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
            this.tableLayoutPanel1.Controls.Add(this.OptionSelection_BattleSizeScale, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.OptionSelection_AutoScale, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(571, 201);
            this.tableLayoutPanel1.TabIndex = 0;
            // 
            // OptionSelection_BattleSizeScale
            // 
            this.OptionSelection_BattleSizeScale.BackColor = System.Drawing.Color.White;
            this.OptionSelection_BattleSizeScale.Dock = System.Windows.Forms.DockStyle.Top;
            this.OptionSelection_BattleSizeScale.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.OptionSelection_BattleSizeScale.Font = new System.Drawing.Font("Paradox King Script", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OptionSelection_BattleSizeScale.FormattingEnabled = true;
            this.OptionSelection_BattleSizeScale.Items.AddRange(new object[] {
            "10%",
            "25%",
            "50%",
            "75%",
            "100%"});
            this.OptionSelection_BattleSizeScale.Location = new System.Drawing.Point(383, 3);
            this.OptionSelection_BattleSizeScale.Name = "OptionSelection_BattleSizeScale";
            this.OptionSelection_BattleSizeScale.Size = new System.Drawing.Size(185, 26);
            this.OptionSelection_BattleSizeScale.TabIndex = 5;
            this.toolTip1.SetToolTip(this.OptionSelection_BattleSizeScale, "Scales the Attila battles to lower numbers to improve performance.\r\nThe battle re" +
        "sults from Attila will be scaled 100% to CK3.");
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Paradox King Script", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.label4.Location = new System.Drawing.Point(3, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(215, 19);
            this.label4.TabIndex = 2;
            this.label4.Text = "Overhaul battle size scale:";
            this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // OptionSelection_AutoScale
            // 
            this.OptionSelection_AutoScale.BackColor = System.Drawing.Color.White;
            this.OptionSelection_AutoScale.Dock = System.Windows.Forms.DockStyle.Top;
            this.OptionSelection_AutoScale.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.OptionSelection_AutoScale.Font = new System.Drawing.Font("Paradox King Script", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.OptionSelection_AutoScale.FormattingEnabled = true;
            this.OptionSelection_AutoScale.Items.AddRange(new object[] {
            "Disabled",
            "Enabled"});
            this.OptionSelection_AutoScale.Location = new System.Drawing.Point(383, 43);
            this.OptionSelection_AutoScale.Name = "OptionSelection_AutoScale";
            this.OptionSelection_AutoScale.Size = new System.Drawing.Size(185, 26);
            this.OptionSelection_AutoScale.TabIndex = 8;
            this.toolTip1.SetToolTip(this.OptionSelection_AutoScale, "Enabled: CW will auto size the maximum number of soldiers an Attila can have acco" +
        "rding to huge scale battles. (Recommended)\r\nDisabled: Units maximum size will al" +
        "ways be from user set options.");
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Paradox King Script", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.White;
            this.label2.ImageAlign = System.Drawing.ContentAlignment.TopCenter;
            this.label2.Location = new System.Drawing.Point(3, 40);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(175, 19);
            this.label2.TabIndex = 4;
            this.label2.Text = "Auto Scale Units Max:";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // toolTip1
            // 
            this.toolTip1.AutomaticDelay = 100000;
            this.toolTip1.IsBalloon = true;
            // 
            // UC_BattleScaleOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Transparent;
            this.Controls.Add(this.tableLayoutPanel1);
            this.DoubleBuffered = true;
            this.Name = "UC_BattleScaleOptions";
            this.Size = new System.Drawing.Size(571, 201);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox OptionSelection_BattleSizeScale;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox OptionSelection_AutoScale;
    }
}
