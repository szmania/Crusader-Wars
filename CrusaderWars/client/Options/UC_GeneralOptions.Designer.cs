namespace CrusaderWars.client.Options
{
    partial class UC_GeneralOptions
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
            this.label1 = new System.Windows.Forms.Label();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.Toggle_SiegeEnginesInField = new CrusaderWars.client.UC_Toggle();
            this.Toggle_DeploymentZones = new CrusaderWars.client.UC_Toggle();
            this.Toggle_ArmiesControl = new CrusaderWars.client.UC_Toggle();
            this.Toggle_ShowPostBattleReport = new CrusaderWars.client.UC_Toggle();
            this.label6 = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Font = new System.Drawing.Font("Georgia", 21.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(212)))), ((int)(((byte)(164)))));
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(261, 34);
            this.label1.TabIndex = 0;
            this.label1.Text = "General Options";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.Toggle_SiegeEnginesInField, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.Toggle_DeploymentZones, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.Toggle_ArmiesControl, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.Toggle_ShowPostBattleReport, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.label6, 0, 4);
            this.tableLayoutPanel1.Location = new System.Drawing.Point(6, 68);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 5;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(682, 313);
            this.tableLayoutPanel1.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Georgia", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.SystemColors.Control;
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(77, 23);
            this.label2.TabIndex = 0;
            this.label2.Text = "Battles:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.SystemColors.Control;
            this.label3.Location = new System.Drawing.Point(3, 62);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(200, 18);
            this.label3.TabIndex = 1;
            this.label3.Text = "Siege Engines in field battles";
            this.toolTip1.SetToolTip(this.label3, "Determines if siege engines are allowed in field battles.\r\nEnabled: Siege engine" +
        "s will be present in field battles if they are in the CK3 army.\r\nDisabled: Siege" +
        " engines will be removed from armies in field battles.");
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.ForeColor = System.Drawing.SystemColors.Control;
            this.label4.Location = new System.Drawing.Point(3, 124);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(142, 18);
            this.label4.TabIndex = 2;
            this.label4.Text = "Deployment Zones";
            this.toolTip1.SetToolTip(this.label4, "Determines the size of the deployment zones in battles.\r\nDynamic: The size is ad" +
        "justed based on the total number of soldiers in the battle.\r\nMedium/Big/Huge: F" +
        "ixed sizes for deployment zones.");
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label5.ForeColor = System.Drawing.SystemColors.Control;
            this.label5.Location = new System.Drawing.Point(3, 186);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(114, 18);
            this.label5.TabIndex = 3;
            this.label5.Text = "Armies Control";
            this.toolTip1.SetToolTip(this.label5, "Determines which armies you control in battle.\r\nPlayer Only: You only control yo" +
        "ur main army.\r\nPlayer and Allies: You control your army and allied armies.\r\nAll:" +
        " You control all armies on your side.");
            // 
            // Toggle_SiegeEnginesInField
            // 
            this.Toggle_SiegeEnginesInField.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(43)))), ((int)(((byte)(43)))));
            this.Toggle_SiegeEnginesInField.Location = new System.Drawing.Point(480, 65);
            this.Toggle_SiegeEnginesInField.Name = "Toggle_SiegeEnginesInField";
            this.Toggle_SiegeEnginesInField.Size = new System.Drawing.Size(199, 40);
            this.Toggle_SiegeEnginesInField.TabIndex = 4;
            // 
            // Toggle_DeploymentZones
            // 
            this.Toggle_DeploymentZones.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(43)))), ((int)(((byte)(43)))));
            this.Toggle_DeploymentZones.Location = new System.Drawing.Point(480, 127);
            this.Toggle_DeploymentZones.Name = "Toggle_DeploymentZones";
            this.Toggle_DeploymentZones.Size = new System.Drawing.Size(199, 40);
            this.Toggle_DeploymentZones.TabIndex = 5;
            // 
            // Toggle_ArmiesControl
            // 
            this.Toggle_ArmiesControl.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(43)))), ((int)(((byte)(43)))));
            this.Toggle_ArmiesControl.Location = new System.Drawing.Point(480, 189);
            this.Toggle_ArmiesControl.Name = "Toggle_ArmiesControl";
            this.Toggle_ArmiesControl.Size = new System.Drawing.Size(199, 40);
            this.Toggle_ArmiesControl.TabIndex = 6;
            // 
            // Toggle_ShowPostBattleReport
            // 
            this.Toggle_ShowPostBattleReport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(43)))), ((int)(((byte)(43)))));
            this.Toggle_ShowPostBattleReport.Location = new System.Drawing.Point(480, 251);
            this.Toggle_ShowPostBattleReport.Name = "Toggle_ShowPostBattleReport";
            this.Toggle_ShowPostBattleReport.Size = new System.Drawing.Size(199, 40);
            this.Toggle_ShowPostBattleReport.TabIndex = 7;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Font = new System.Drawing.Font("Georgia", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label6.ForeColor = System.Drawing.SystemColors.Control;
            this.label6.Location = new System.Drawing.Point(3, 248);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(197, 18);
            this.label6.TabIndex = 8;
            this.label6.Text = "Show After Action Report";
            this.toolTip1.SetToolTip(this.label6, "Determines if the After Action Report screen is shown after a battle.\r\nEnabled: " +
        "Shows a detailed breakdown of the battle results.\r\nDisabled: Skips the report s" +
        "creen and returns to CK3 immediately.");
            // 
            // UC_GeneralOptions
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(43)))), ((int)(((byte)(43)))), ((int)(((byte)(43)))));
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.label1);
            this.Name = "UC_GeneralOptions";
            this.Size = new System.Drawing.Size(726, 450);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private UC_Toggle Toggle_SiegeEnginesInField;
        private UC_Toggle Toggle_DeploymentZones;
        private UC_Toggle Toggle_ArmiesControl;
        private System.Windows.Forms.ToolTip toolTip1;
        private UC_Toggle Toggle_ShowPostBattleReport;
        private System.Windows.Forms.Label label6;
    }
}
