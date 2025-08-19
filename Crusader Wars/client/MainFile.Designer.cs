namespace Crusader_Wars
{
    partial class HomePage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HomePage));
            this.ExecuteButton = new System.Windows.Forms.Button();
            this.btt_debug = new System.Windows.Forms.Button();
            this.infoLabel = new System.Windows.Forms.Label();
            this.SettingsBtn = new System.Windows.Forms.Button();
            this.InformationToolTip = new System.Windows.Forms.ToolTip(this.components);
            this.labelVersion = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.MainPanelLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.BottomPanelLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.EA_Label = new System.Windows.Forms.Label();
            this.patreonBtn = new System.Windows.Forms.Button();
            this.WebsiteBTN = new System.Windows.Forms.Button();
            this.SteamBTN = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.MainPanelLayout.SuspendLayout();
            this.BottomPanelLayout.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ExecuteButton
            // 
            resources.ApplyResources(this.ExecuteButton, "ExecuteButton");
            this.ExecuteButton.BackColor = System.Drawing.Color.Transparent;
            this.ExecuteButton.BackgroundImage = global::Crusader_Wars.Properties.Resources.start_new;
            this.ExecuteButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ExecuteButton.FlatAppearance.BorderColor = System.Drawing.Color.Black;
            this.ExecuteButton.FlatAppearance.BorderSize = 0;
            this.ExecuteButton.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.ExecuteButton.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.ExecuteButton.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.ExecuteButton.Name = "ExecuteButton";
            this.ExecuteButton.TabStop = false;
            this.ExecuteButton.UseVisualStyleBackColor = true;
            this.ExecuteButton.Click += new System.EventHandler(this.ExecuteButton_Click);
            this.ExecuteButton.MouseEnter += new System.EventHandler(this.ExecuteButton_MouseEnter);
            this.ExecuteButton.MouseLeave += new System.EventHandler(this.ExecuteButton_MouseLeave);
            this.ExecuteButton.MouseHover += new System.EventHandler(this.ExecuteButton_MouseHover);
            // 
            // btt_debug
            // 
            resources.ApplyResources(this.btt_debug, "btt_debug");
            this.btt_debug.BackColor = System.Drawing.Color.DarkSeaGreen;
            this.btt_debug.Name = "btt_debug";
            this.btt_debug.UseVisualStyleBackColor = false;
            this.btt_debug.Click += new System.EventHandler(this.btt_debug_Click);
            // 
            // infoLabel
            // 
            resources.ApplyResources(this.infoLabel, "infoLabel");
            this.infoLabel.BackColor = System.Drawing.Color.Transparent;
            this.infoLabel.ForeColor = System.Drawing.SystemColors.ButtonHighlight;
            this.infoLabel.Name = "infoLabel";
            // 
            // SettingsBtn
            // 
            resources.ApplyResources(this.SettingsBtn, "SettingsBtn");
            this.SettingsBtn.BackColor = System.Drawing.Color.Transparent;
            this.SettingsBtn.BackgroundImage = global::Crusader_Wars.Properties.Resources.options_btn_new;
            this.SettingsBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SettingsBtn.FlatAppearance.BorderSize = 0;
            this.SettingsBtn.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.SettingsBtn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.SettingsBtn.ForeColor = System.Drawing.Color.Black;
            this.SettingsBtn.Name = "SettingsBtn";
            this.SettingsBtn.TabStop = false;
            this.SettingsBtn.UseVisualStyleBackColor = false;
            this.SettingsBtn.Click += new System.EventHandler(this.SettingsBtn_Click);
            this.SettingsBtn.MouseEnter += new System.EventHandler(this.SettingsBtn_MouseEnter);
            this.SettingsBtn.MouseLeave += new System.EventHandler(this.SettingsBtn_MouseLeave);
            this.SettingsBtn.MouseHover += new System.EventHandler(this.SettingsBtn_MouseHover);
            // 
            // InformationToolTip
            // 
            this.InformationToolTip.IsBalloon = true;
            // 
            // labelVersion
            // 
            resources.ApplyResources(this.labelVersion, "labelVersion");
            this.labelVersion.BackColor = System.Drawing.Color.Transparent;
            this.labelVersion.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.labelVersion.Name = "labelVersion";
            // 
            // pictureBox1
            // 
            resources.ApplyResources(this.pictureBox1, "pictureBox1");
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.Image = global::Crusader_Wars.Properties.Resources.Sem_nome;
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.TabStop = false;
            // 
            // MainPanelLayout
            // 
            resources.ApplyResources(this.MainPanelLayout, "MainPanelLayout");
            this.MainPanelLayout.BackColor = System.Drawing.Color.Transparent;
            this.MainPanelLayout.Controls.Add(this.pictureBox1);
            this.MainPanelLayout.Controls.Add(this.ExecuteButton);
            this.MainPanelLayout.Controls.Add(this.infoLabel);
            this.MainPanelLayout.Name = "MainPanelLayout";
            // 
            // BottomPanelLayout
            // 
            this.BottomPanelLayout.BackColor = System.Drawing.Color.Transparent;
            this.BottomPanelLayout.Controls.Add(this.labelVersion);
            this.BottomPanelLayout.Controls.Add(this.EA_Label);
            resources.ApplyResources(this.BottomPanelLayout, "BottomPanelLayout");
            this.BottomPanelLayout.Name = "BottomPanelLayout";
            // 
            // EA_Label
            // 
            resources.ApplyResources(this.EA_Label, "EA_Label");
            this.EA_Label.BackColor = System.Drawing.Color.Transparent;
            this.EA_Label.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.EA_Label.Name = "EA_Label";
            // 
            // patreonBtn
            // 
            resources.ApplyResources(this.patreonBtn, "patreonBtn");
            this.patreonBtn.BackColor = System.Drawing.Color.Transparent;
            this.patreonBtn.BackgroundImage = global::Crusader_Wars.Properties.Resources.patreon_btn_new;
            this.patreonBtn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.patreonBtn.FlatAppearance.BorderSize = 0;
            this.patreonBtn.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.patreonBtn.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.patreonBtn.Name = "patreonBtn";
            this.patreonBtn.UseVisualStyleBackColor = false;
            this.patreonBtn.Click += new System.EventHandler(this.patreonBtn_Click);
            this.patreonBtn.MouseEnter += new System.EventHandler(this.patreonBtn_MouseEnter);
            this.patreonBtn.MouseLeave += new System.EventHandler(this.patreonBtn_MouseLeave);
            this.patreonBtn.MouseHover += new System.EventHandler(this.patreonBtn_MouseHover_1);
            // 
            // WebsiteBTN
            // 
            resources.ApplyResources(this.WebsiteBTN, "WebsiteBTN");
            this.WebsiteBTN.BackColor = System.Drawing.Color.Transparent;
            this.WebsiteBTN.BackgroundImage = global::Crusader_Wars.Properties.Resources.website_btn_new;
            this.WebsiteBTN.Cursor = System.Windows.Forms.Cursors.Hand;
            this.WebsiteBTN.FlatAppearance.BorderSize = 0;
            this.WebsiteBTN.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.WebsiteBTN.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.WebsiteBTN.Name = "WebsiteBTN";
            this.WebsiteBTN.TabStop = false;
            this.WebsiteBTN.UseVisualStyleBackColor = false;
            this.WebsiteBTN.Click += new System.EventHandler(this.WebsiteBTN_Click);
            this.WebsiteBTN.MouseEnter += new System.EventHandler(this.WebsiteBTN_MouseEnter);
            this.WebsiteBTN.MouseLeave += new System.EventHandler(this.WebsiteBTN_MouseLeave);
            this.WebsiteBTN.MouseHover += new System.EventHandler(this.WebsiteBTN_MouseHover);
            // 
            // SteamBTN
            // 
            resources.ApplyResources(this.SteamBTN, "SteamBTN");
            this.SteamBTN.BackColor = System.Drawing.Color.Transparent;
            this.SteamBTN.BackgroundImage = global::Crusader_Wars.Properties.Resources.steam_btn_new;
            this.SteamBTN.Cursor = System.Windows.Forms.Cursors.Hand;
            this.SteamBTN.FlatAppearance.BorderSize = 0;
            this.SteamBTN.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.SteamBTN.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.SteamBTN.Name = "SteamBTN";
            this.SteamBTN.TabStop = false;
            this.SteamBTN.UseVisualStyleBackColor = false;
            this.SteamBTN.Click += new System.EventHandler(this.SteamBTN_Click);
            this.SteamBTN.MouseEnter += new System.EventHandler(this.SteamBTN_MouseEnter);
            this.SteamBTN.MouseLeave += new System.EventHandler(this.SteamBTN_MouseLeave);
            this.SteamBTN.MouseHover += new System.EventHandler(this.SteamBTN_MouseHover);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.tableLayoutPanel1, "tableLayoutPanel1");
            this.tableLayoutPanel1.Controls.Add(this.WebsiteBTN, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.SteamBTN, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.patreonBtn, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.SettingsBtn, 0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            // 
            // HomePage
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.IndianRed;
            this.BackgroundImage = global::Crusader_Wars.Properties.Resources.main_beta1;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.btt_debug);
            this.Controls.Add(this.MainPanelLayout);
            this.Controls.Add(this.BottomPanelLayout);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "HomePage";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.HomePage_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Shown += new System.EventHandler(this.HomePage_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.MainPanelLayout.ResumeLayout(false);
            this.MainPanelLayout.PerformLayout();
            this.BottomPanelLayout.ResumeLayout(false);
            this.BottomPanelLayout.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);

            // viewLogsLink
            this.viewLogsLink = new System.Windows.Forms.Label();
            this.viewLogsLink.Anchor = (System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right);
            this.viewLogsLink.AutoSize = true;
            this.viewLogsLink.Cursor = System.Windows.Forms.Cursors.Hand;
            this.viewLogsLink.Font = new System.Drawing.Font("Paradox King Script", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.viewLogsLink.ForeColor = System.Drawing.Color.WhiteSmoke;
            this.viewLogsLink.Location = new System.Drawing.Point(1130, 680);
            this.viewLogsLink.Name = "viewLogsLink";
            this.viewLogsLink.Size = new System.Drawing.Size(61, 14);
            this.viewLogsLink.TabIndex = 37;
            this.viewLogsLink.Text = "View Logs";
            this.viewLogsLink.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.viewLogsLink.Click += new System.EventHandler(this.viewLogsLink_Click);
            this.Controls.Add(this.viewLogsLink);

            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button ExecuteButton;
        private System.Windows.Forms.Button btt_debug;
        private System.Windows.Forms.Label infoLabel;
        private System.Windows.Forms.Button SettingsBtn;
        private System.Windows.Forms.ToolTip InformationToolTip;
        private System.Windows.Forms.Label labelVersion;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.FlowLayoutPanel MainPanelLayout;
        private System.Windows.Forms.FlowLayoutPanel BottomPanelLayout;
        private System.Windows.Forms.Button patreonBtn;
        private System.Windows.Forms.Button WebsiteBTN;
        private System.Windows.Forms.Button SteamBTN;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Label viewLogsLink;
        private System.Windows.Forms.Label EA_Label;
    }
}

