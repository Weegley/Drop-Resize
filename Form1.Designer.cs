namespace DropResize
{
    partial class Form1
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

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.rootLayout = new System.Windows.Forms.TableLayoutPanel();
            this.sidebarPanel = new System.Windows.Forms.TableLayoutPanel();
            this.profileCaptionLabel = new System.Windows.Forms.Label();
            this.profileComboBox = new System.Windows.Forms.ComboBox();
            this.addProfileButton = new System.Windows.Forms.Button();
            this.editProfileButton = new System.Windows.Forms.Button();
            this.deleteProfileButton = new System.Windows.Forms.Button();
            this.resetProfileButton = new System.Windows.Forms.Button();
            this.profileSeparatorLabel = new System.Windows.Forms.Label();
            this.workersCaptionLabel = new System.Windows.Forms.Label();
            this.workerComboBox = new System.Windows.Forms.ComboBox();
            this.customWorkerCount = new System.Windows.Forms.NumericUpDown();
            this.workersSeparatorLabel = new System.Windows.Forms.Label();
            this.resizeAgainButton = new System.Windows.Forms.Button();
            this.cardsPanel = new DropResize.SelectableFlowLayoutPanel();
            this.instructionLabel = new System.Windows.Forms.Label();
            this.statusLabel = new System.Windows.Forms.Label();
            this.processingOverlay = new System.Windows.Forms.Panel();
            this.processingBox = new System.Windows.Forms.Panel();
            this.processingLabel = new System.Windows.Forms.Label();
            this.processingProgressBar = new System.Windows.Forms.ProgressBar();
            this.resultFlushTimer = new System.Windows.Forms.Timer(this.components);
            this.marqueeScrollTimer = new System.Windows.Forms.Timer(this.components);
            this.rootLayout.SuspendLayout();
            this.sidebarPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.customWorkerCount)).BeginInit();
            this.cardsPanel.SuspendLayout();
            this.processingOverlay.SuspendLayout();
            this.processingBox.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootLayout
            // 
            this.rootLayout.BackColor = System.Drawing.Color.White;
            this.rootLayout.ColumnCount = 2;
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 285F));
            this.rootLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.Controls.Add(this.sidebarPanel, 0, 0);
            this.rootLayout.Controls.Add(this.cardsPanel, 1, 0);
            this.rootLayout.Controls.Add(this.statusLabel, 1, 1);
            this.rootLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootLayout.Location = new System.Drawing.Point(0, 0);
            this.rootLayout.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.rootLayout.Name = "rootLayout";
            this.rootLayout.Padding = new System.Windows.Forms.Padding(18);
            this.rootLayout.RowCount = 2;
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.rootLayout.Size = new System.Drawing.Size(1470, 1046);
            this.rootLayout.TabIndex = 0;
            // 
            // sidebarPanel
            // 
            this.sidebarPanel.ColumnCount = 1;
            this.sidebarPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sidebarPanel.Controls.Add(this.profileCaptionLabel, 0, 0);
            this.sidebarPanel.Controls.Add(this.profileComboBox, 0, 1);
            this.sidebarPanel.Controls.Add(this.addProfileButton, 0, 2);
            this.sidebarPanel.Controls.Add(this.editProfileButton, 0, 3);
            this.sidebarPanel.Controls.Add(this.deleteProfileButton, 0, 4);
            this.sidebarPanel.Controls.Add(this.resetProfileButton, 0, 5);
            this.sidebarPanel.Controls.Add(this.profileSeparatorLabel, 0, 6);
            this.sidebarPanel.Controls.Add(this.workersCaptionLabel, 0, 7);
            this.sidebarPanel.Controls.Add(this.workerComboBox, 0, 8);
            this.sidebarPanel.Controls.Add(this.customWorkerCount, 0, 9);
            this.sidebarPanel.Controls.Add(this.workersSeparatorLabel, 0, 10);
            this.sidebarPanel.Controls.Add(this.resizeAgainButton, 0, 11);
            this.sidebarPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sidebarPanel.Location = new System.Drawing.Point(18, 18);
            this.sidebarPanel.Margin = new System.Windows.Forms.Padding(0, 0, 18, 0);
            this.sidebarPanel.Name = "sidebarPanel";
            this.sidebarPanel.RowCount = 13;
            this.rootLayout.SetRowSpan(this.sidebarPanel, 2);
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 37F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 49F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 37F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 49F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 49F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 28F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.sidebarPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.sidebarPanel.Size = new System.Drawing.Size(267, 1010);
            this.sidebarPanel.TabIndex = 0;
            // 
            // profileCaptionLabel
            // 
            this.profileCaptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.profileCaptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.profileCaptionLabel.Location = new System.Drawing.Point(0, 0);
            this.profileCaptionLabel.Margin = new System.Windows.Forms.Padding(0);
            this.profileCaptionLabel.Name = "profileCaptionLabel";
            this.profileCaptionLabel.Size = new System.Drawing.Size(267, 37);
            this.profileCaptionLabel.TabIndex = 0;
            this.profileCaptionLabel.Text = "Profile";
            this.profileCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // profileComboBox
            // 
            this.profileComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.profileComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.profileComboBox.FormattingEnabled = true;
            this.profileComboBox.Location = new System.Drawing.Point(0, 42);
            this.profileComboBox.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.profileComboBox.Name = "profileComboBox";
            this.profileComboBox.Size = new System.Drawing.Size(267, 28);
            this.profileComboBox.TabIndex = 1;
            // 
            // addProfileButton
            // 
            this.addProfileButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.addProfileButton.Location = new System.Drawing.Point(0, 89);
            this.addProfileButton.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.addProfileButton.Name = "addProfileButton";
            this.addProfileButton.Size = new System.Drawing.Size(267, 40);
            this.addProfileButton.TabIndex = 2;
            this.addProfileButton.Text = "Add";
            this.addProfileButton.UseVisualStyleBackColor = true;
            this.addProfileButton.Click += new System.EventHandler(this.AddProfile_Click);
            // 
            // editProfileButton
            // 
            this.editProfileButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editProfileButton.Location = new System.Drawing.Point(0, 135);
            this.editProfileButton.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.editProfileButton.Name = "editProfileButton";
            this.editProfileButton.Size = new System.Drawing.Size(267, 40);
            this.editProfileButton.TabIndex = 3;
            this.editProfileButton.Text = "Edit";
            this.editProfileButton.UseVisualStyleBackColor = true;
            this.editProfileButton.Click += new System.EventHandler(this.EditProfile_Click);
            // 
            // deleteProfileButton
            // 
            this.deleteProfileButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.deleteProfileButton.Location = new System.Drawing.Point(0, 181);
            this.deleteProfileButton.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.deleteProfileButton.Name = "deleteProfileButton";
            this.deleteProfileButton.Size = new System.Drawing.Size(267, 40);
            this.deleteProfileButton.TabIndex = 4;
            this.deleteProfileButton.Text = "Delete";
            this.deleteProfileButton.UseVisualStyleBackColor = true;
            this.deleteProfileButton.Click += new System.EventHandler(this.DeleteProfile_Click);
            // 
            // resetProfileButton
            // 
            this.resetProfileButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resetProfileButton.Location = new System.Drawing.Point(0, 227);
            this.resetProfileButton.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.resetProfileButton.Name = "resetProfileButton";
            this.resetProfileButton.Size = new System.Drawing.Size(267, 40);
            this.resetProfileButton.TabIndex = 5;
            this.resetProfileButton.Text = "Reset";
            this.resetProfileButton.UseVisualStyleBackColor = true;
            this.resetProfileButton.Click += new System.EventHandler(this.ResetProfile_Click);
            // 
            // profileSeparatorLabel
            // 
            this.profileSeparatorLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.profileSeparatorLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.profileSeparatorLabel.Location = new System.Drawing.Point(0, 282);
            this.profileSeparatorLabel.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.profileSeparatorLabel.Name = "profileSeparatorLabel";
            this.profileSeparatorLabel.Size = new System.Drawing.Size(267, 3);
            this.profileSeparatorLabel.TabIndex = 6;
            // 
            // workersCaptionLabel
            // 
            this.workersCaptionLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workersCaptionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold);
            this.workersCaptionLabel.Location = new System.Drawing.Point(0, 298);
            this.workersCaptionLabel.Margin = new System.Windows.Forms.Padding(0);
            this.workersCaptionLabel.Name = "workersCaptionLabel";
            this.workersCaptionLabel.Size = new System.Drawing.Size(267, 37);
            this.workersCaptionLabel.TabIndex = 7;
            this.workersCaptionLabel.Text = "Workers";
            this.workersCaptionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // workerComboBox
            // 
            this.workerComboBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.workerComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.workerComboBox.FormattingEnabled = true;
            this.workerComboBox.Items.AddRange(new object[] {
            "Auto",
            "1",
            "2",
            "4",
            "8",
            "Custom"});
            this.workerComboBox.Location = new System.Drawing.Point(0, 340);
            this.workerComboBox.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.workerComboBox.Name = "workerComboBox";
            this.workerComboBox.Size = new System.Drawing.Size(267, 28);
            this.workerComboBox.TabIndex = 8;
            this.workerComboBox.SelectedIndexChanged += new System.EventHandler(this.WorkerComboBox_SelectedIndexChanged);
            // 
            // customWorkerCount
            // 
            this.customWorkerCount.Dock = System.Windows.Forms.DockStyle.Fill;
            this.customWorkerCount.Enabled = false;
            this.customWorkerCount.Location = new System.Drawing.Point(0, 389);
            this.customWorkerCount.Margin = new System.Windows.Forms.Padding(0, 5, 0, 0);
            this.customWorkerCount.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.customWorkerCount.Name = "customWorkerCount";
            this.customWorkerCount.Size = new System.Drawing.Size(267, 26);
            this.customWorkerCount.TabIndex = 9;
            this.customWorkerCount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // workersSeparatorLabel
            // 
            this.workersSeparatorLabel.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.workersSeparatorLabel.Dock = System.Windows.Forms.DockStyle.Top;
            this.workersSeparatorLabel.Location = new System.Drawing.Point(0, 445);
            this.workersSeparatorLabel.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
            this.workersSeparatorLabel.Name = "workersSeparatorLabel";
            this.workersSeparatorLabel.Size = new System.Drawing.Size(267, 3);
            this.workersSeparatorLabel.TabIndex = 10;
            // 
            // resizeAgainButton
            // 
            this.resizeAgainButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resizeAgainButton.Location = new System.Drawing.Point(0, 464);
            this.resizeAgainButton.Margin = new System.Windows.Forms.Padding(0, 3, 0, 3);
            this.resizeAgainButton.Name = "resizeAgainButton";
            this.resizeAgainButton.Size = new System.Drawing.Size(267, 40);
            this.resizeAgainButton.TabIndex = 11;
            this.resizeAgainButton.Text = "Resize Again";
            this.resizeAgainButton.UseVisualStyleBackColor = true;
            this.resizeAgainButton.Click += new System.EventHandler(this.ResizeAgain_Click);
            // 
            // cardsPanel
            // 
            this.cardsPanel.AllowDrop = true;
            this.cardsPanel.AutoScroll = true;
            this.cardsPanel.BackColor = System.Drawing.Color.White;
            this.cardsPanel.Controls.Add(this.instructionLabel);
            this.cardsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cardsPanel.Location = new System.Drawing.Point(303, 18);
            this.cardsPanel.Margin = new System.Windows.Forms.Padding(0);
            this.cardsPanel.Name = "cardsPanel";
            this.cardsPanel.Padding = new System.Windows.Forms.Padding(0, 12, 0, 12);
            this.cardsPanel.SelectionRectangle = new System.Drawing.Rectangle(0, 0, 0, 0);
            this.cardsPanel.Size = new System.Drawing.Size(1149, 964);
            this.cardsPanel.TabIndex = 1;
            this.cardsPanel.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.cardsPanel.DragEnter += new System.Windows.Forms.DragEventHandler(this.Form1_DragEnter);
            this.cardsPanel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.CardsPanel_MouseDown);
            this.cardsPanel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.CardsPanel_MouseMove);
            this.cardsPanel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.CardsPanel_MouseUp);
            this.cardsPanel.Resize += new System.EventHandler(this.CardsPanel_Resize);
            // 
            // instructionLabel
            // 
            this.instructionLabel.AllowDrop = true;
            this.instructionLabel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(248)))), ((int)(((byte)(250)))));
            this.instructionLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.instructionLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold);
            this.instructionLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(55)))), ((int)(((byte)(65)))));
            this.instructionLabel.Location = new System.Drawing.Point(0, 12);
            this.instructionLabel.Margin = new System.Windows.Forms.Padding(0);
            this.instructionLabel.Name = "instructionLabel";
            this.instructionLabel.Size = new System.Drawing.Size(1112, 919);
            this.instructionLabel.TabIndex = 0;
            this.instructionLabel.Text = "Drop images here. Drag resized images out from the cards. Press CTRL+A to select " +
    "all cards.";
            this.instructionLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.instructionLabel.DragDrop += new System.Windows.Forms.DragEventHandler(this.Form1_DragDrop);
            this.instructionLabel.DragEnter += new System.Windows.Forms.DragEventHandler(this.Form1_DragEnter);
            // 
            // statusLabel
            // 
            this.statusLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.statusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(76)))), ((int)(((byte)(82)))));
            this.statusLabel.Location = new System.Drawing.Point(307, 982);
            this.statusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(1141, 46);
            this.statusLabel.TabIndex = 2;
            this.statusLabel.Text = "Ready";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // processingOverlay
            // 
            this.processingOverlay.BackColor = System.Drawing.Color.White;
            this.processingOverlay.Controls.Add(this.processingBox);
            this.processingOverlay.Dock = System.Windows.Forms.DockStyle.Fill;
            this.processingOverlay.Location = new System.Drawing.Point(0, 0);
            this.processingOverlay.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.processingOverlay.Name = "processingOverlay";
            this.processingOverlay.Size = new System.Drawing.Size(1470, 1046);
            this.processingOverlay.TabIndex = 1;
            this.processingOverlay.Visible = false;
            this.processingOverlay.Resize += new System.EventHandler(this.ProcessingOverlay_Resize);
            // 
            // processingBox
            // 
            this.processingBox.BackColor = System.Drawing.Color.White;
            this.processingBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.processingBox.Controls.Add(this.processingLabel);
            this.processingBox.Controls.Add(this.processingProgressBar);
            this.processingBox.Location = new System.Drawing.Point(465, 449);
            this.processingBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.processingBox.Name = "processingBox";
            this.processingBox.Size = new System.Drawing.Size(539, 147);
            this.processingBox.TabIndex = 0;
            // 
            // processingLabel
            // 
            this.processingLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.processingLabel.Location = new System.Drawing.Point(27, 22);
            this.processingLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.processingLabel.Name = "processingLabel";
            this.processingLabel.Size = new System.Drawing.Size(483, 43);
            this.processingLabel.TabIndex = 0;
            this.processingLabel.Text = "Processing images...";
            this.processingLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // processingProgressBar
            // 
            this.processingProgressBar.Location = new System.Drawing.Point(27, 80);
            this.processingProgressBar.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.processingProgressBar.Name = "processingProgressBar";
            this.processingProgressBar.Size = new System.Drawing.Size(483, 31);
            this.processingProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.processingProgressBar.TabIndex = 1;
            // 
            // resultFlushTimer
            // 
            this.resultFlushTimer.Interval = 20;
            this.resultFlushTimer.Tick += new System.EventHandler(this.ResultFlushTimer_Tick);
            // 
            // marqueeScrollTimer
            // 
            this.marqueeScrollTimer.Interval = 35;
            this.marqueeScrollTimer.Tick += new System.EventHandler(this.MarqueeScrollTimer_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1470, 1046);
            this.Controls.Add(this.processingOverlay);
            this.Controls.Add(this.rootLayout);
            this.Icon = global::DropResize.Properties.Resources.AppIcon;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MinimumSize = new System.Drawing.Size(1243, 891);
            this.Name = "Form1";
            this.Text = "Drop&Resize";
            this.rootLayout.ResumeLayout(false);
            this.sidebarPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.customWorkerCount)).EndInit();
            this.cardsPanel.ResumeLayout(false);
            this.processingOverlay.ResumeLayout(false);
            this.processingBox.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel rootLayout;
        private System.Windows.Forms.TableLayoutPanel sidebarPanel;
        private System.Windows.Forms.Label profileCaptionLabel;
        private System.Windows.Forms.ComboBox profileComboBox;
        private System.Windows.Forms.Button addProfileButton;
        private System.Windows.Forms.Button editProfileButton;
        private System.Windows.Forms.Button deleteProfileButton;
        private System.Windows.Forms.Button resetProfileButton;
        private System.Windows.Forms.Label profileSeparatorLabel;
        private System.Windows.Forms.Label workersCaptionLabel;
        private System.Windows.Forms.ComboBox workerComboBox;
        private System.Windows.Forms.NumericUpDown customWorkerCount;
        private System.Windows.Forms.Label workersSeparatorLabel;
        private System.Windows.Forms.Button resizeAgainButton;
        private SelectableFlowLayoutPanel cardsPanel;
        private System.Windows.Forms.Label instructionLabel;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Panel processingOverlay;
        private System.Windows.Forms.Panel processingBox;
        private System.Windows.Forms.Label processingLabel;
        private System.Windows.Forms.ProgressBar processingProgressBar;
        private System.Windows.Forms.Timer resultFlushTimer;
        private System.Windows.Forms.Timer marqueeScrollTimer;
    }
}
