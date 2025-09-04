namespace CrusaderWars
{
    partial class LoadingScreen
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
            this.Label_Message = new System.Windows.Forms.Label();
            this.GIF = new System.Windows.Forms.PictureBox();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.ParentLayout = new System.Windows.Forms.TableLayoutPanel();
            this.RightChildLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.LeftChildLayout = new System.Windows.Forms.FlowLayoutPanel();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.Label_UnitMapper = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.GIF)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.ParentLayout.SuspendLayout();
            this.RightChildLayout.SuspendLayout();
            this.LeftChildLayout.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // Label_Message
            // 
            this.Label_Message.AutoSize = true;
            this.Label_Message.BackColor = System.Drawing.Color.Transparent;
            this.Label_Message.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Label_Message.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Label_Message.Font = new System.Drawing.Font("Paradox King Script", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label_Message.ForeColor = System.Drawing.Color.White;
            this.Label_Message.Location = new System.Drawing.Point(3, 0);
            this.Label_Message.Name = "Label_Message";
            this.Label_Message.Size = new System.Drawing.Size(491, 42);
            this.Label_Message.TabIndex = 0;
            this.Label_Message.Text = "Getting Data from CL3 Save File...";
            this.Label_Message.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // GIF
            // 
            this.GIF.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.GIF.BackColor = System.Drawing.Color.Transparent;
            this.GIF.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.GIF.Image = global::CrusaderWars.Properties.Resources.rotating_loading;
            this.GIF.Location = new System.Drawing.Point(830, 3);
            this.GIF.Name = "GIF";
            this.GIF.Size = new System.Drawing.Size(121, 94);
            this.GIF.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.GIF.TabIndex = 1;
            this.GIF.TabStop = false;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel1.BackgroundImage = global::CrusaderWars.Properties.Resources.progress_orange;
            this.flowLayoutPanel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.flowLayoutPanel1.Controls.Add(this.Label_Message);
            this.flowLayoutPanel1.Location = new System.Drawing.Point(327, 29);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(497, 42);
            this.flowLayoutPanel1.TabIndex = 3;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // ParentLayout
            // 
            this.ParentLayout.ColumnCount = 2;
            this.ParentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ParentLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.ParentLayout.Controls.Add(this.RightChildLayout, 1, 0);
            this.ParentLayout.Controls.Add(this.LeftChildLayout, 0, 0);
            this.ParentLayout.Dock = System.Windows.Forms.DockStyle.Top;
            this.ParentLayout.Location = new System.Drawing.Point(0, 0);
            this.ParentLayout.Name = "ParentLayout";
            this.ParentLayout.RowCount = 1;
            this.ParentLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.ParentLayout.Size = new System.Drawing.Size(1920, 100);
            this.ParentLayout.TabIndex = 5;
            // 
            // RightChildLayout
            // 
            this.RightChildLayout.Controls.Add(this.GIF);
            this.RightChildLayout.Controls.Add(this.flowLayoutPanel1);
            this.RightChildLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.RightChildLayout.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.RightChildLayout.Location = new System.Drawing.Point(963, 3);
            this.RightChildLayout.Name = "RightChildLayout";
            this.RightChildLayout.Size = new System.Drawing.Size(954, 94);
            this.RightChildLayout.TabIndex = 0;
            // 
            // LeftChildLayout
            // 
            this.LeftChildLayout.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.LeftChildLayout.Controls.Add(this.pictureBox1);
            this.LeftChildLayout.Controls.Add(this.flowLayoutPanel2);
            this.LeftChildLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.LeftChildLayout.Location = new System.Drawing.Point(3, 3);
            this.LeftChildLayout.Name = "LeftChildLayout";
            this.LeftChildLayout.Size = new System.Drawing.Size(954, 94);
            this.LeftChildLayout.TabIndex = 1;
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flowLayoutPanel2.BackgroundImage = global::CrusaderWars.Properties.Resources.progress_orange;
            this.flowLayoutPanel2.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.flowLayoutPanel2.Controls.Add(this.Label_UnitMapper);
            this.flowLayoutPanel2.Location = new System.Drawing.Point(19, 29);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(433, 42);
            this.flowLayoutPanel2.TabIndex = 4;
            this.flowLayoutPanel2.WrapContents = false;
            // 
            // Label_UnitMapper
            // 
            this.Label_UnitMapper.AutoSize = true;
            this.Label_UnitMapper.BackColor = System.Drawing.Color.Transparent;
            this.Label_UnitMapper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Label_UnitMapper.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Label_UnitMapper.Font = new System.Drawing.Font("Paradox King Script", 26.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Label_UnitMapper.ForeColor = System.Drawing.Color.White;
            this.Label_UnitMapper.Location = new System.Drawing.Point(3, 0);
            this.Label_UnitMapper.Name = "Label_UnitMapper";
            this.Label_UnitMapper.Size = new System.Drawing.Size(427, 42);
            this.Label_UnitMapper.TabIndex = 0;
            this.Label_UnitMapper.Text = "Early Medieval 867 - 1150";
            this.Label_UnitMapper.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.pictureBox1.BackColor = System.Drawing.Color.Transparent;
            this.pictureBox1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.pictureBox1.Location = new System.Drawing.Point(3, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(10, 94);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 6;
            this.pictureBox1.TabStop = false;
            // 
            // LoadingScreen
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            this.BackColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.BackgroundImage = global::CrusaderWars.Properties.Resources.LS_lotr;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ClientSize = new System.Drawing.Size(1920, 1000);
            this.ControlBox = false;
            this.Controls.Add(this.ParentLayout);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "LoadingScreen";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Crusader Conflicts";
            this.TopMost = true;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Shown += new System.EventHandler(this.LoadingScreen_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.GIF)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.ParentLayout.ResumeLayout(false);
            this.RightChildLayout.ResumeLayout(false);
            this.RightChildLayout.PerformLayout();
            this.LeftChildLayout.ResumeLayout(false);
            this.LeftChildLayout.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label Label_Message;
        private System.Windows.Forms.PictureBox GIF;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.TableLayoutPanel ParentLayout;
        private System.Windows.Forms.FlowLayoutPanel RightChildLayout;
        private System.Windows.Forms.FlowLayoutPanel LeftChildLayout;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Label Label_UnitMapper;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}