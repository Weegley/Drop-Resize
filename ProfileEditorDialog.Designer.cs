namespace DropResize
{
    partial class ProfileEditorDialog
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
            this.mainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.nameLabel = new System.Windows.Forms.Label();
            this.nameTextBox = new System.Windows.Forms.TextBox();
            this.maxWidthLabel = new System.Windows.Forms.Label();
            this.maxWidthBox = new System.Windows.Forms.NumericUpDown();
            this.maxHeightLabel = new System.Windows.Forms.Label();
            this.maxHeightBox = new System.Windows.Forms.NumericUpDown();
            this.jpegQualityLabel = new System.Windows.Forms.Label();
            this.jpegQualityBox = new System.Windows.Forms.NumericUpDown();
            this.outputFormatLabel = new System.Windows.Forms.Label();
            this.outputFormatBox = new System.Windows.Forms.ComboBox();
            this.suffixLabel = new System.Windows.Forms.Label();
            this.suffixTextBox = new System.Windows.Forms.TextBox();
            this.doNotEnlargeBox = new System.Windows.Forms.CheckBox();
            this.buttonsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.mainLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.maxWidthBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxHeightBox)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.jpegQualityBox)).BeginInit();
            this.buttonsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainLayout
            // 
            this.mainLayout.ColumnCount = 2;
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.mainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Controls.Add(this.nameLabel, 0, 0);
            this.mainLayout.Controls.Add(this.nameTextBox, 1, 0);
            this.mainLayout.Controls.Add(this.maxWidthLabel, 0, 1);
            this.mainLayout.Controls.Add(this.maxWidthBox, 1, 1);
            this.mainLayout.Controls.Add(this.maxHeightLabel, 0, 2);
            this.mainLayout.Controls.Add(this.maxHeightBox, 1, 2);
            this.mainLayout.Controls.Add(this.jpegQualityLabel, 0, 3);
            this.mainLayout.Controls.Add(this.jpegQualityBox, 1, 3);
            this.mainLayout.Controls.Add(this.outputFormatLabel, 0, 4);
            this.mainLayout.Controls.Add(this.outputFormatBox, 1, 4);
            this.mainLayout.Controls.Add(this.suffixLabel, 0, 5);
            this.mainLayout.Controls.Add(this.suffixTextBox, 1, 5);
            this.mainLayout.Controls.Add(this.doNotEnlargeBox, 1, 6);
            this.mainLayout.Controls.Add(this.buttonsPanel, 0, 7);
            this.mainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainLayout.Location = new System.Drawing.Point(0, 0);
            this.mainLayout.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.mainLayout.Name = "mainLayout";
            this.mainLayout.Padding = new System.Windows.Forms.Padding(18);
            this.mainLayout.RowCount = 8;
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 46F));
            this.mainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainLayout.Size = new System.Drawing.Size(540, 415);
            this.mainLayout.TabIndex = 0;
            // 
            // nameLabel
            // 
            this.nameLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nameLabel.Location = new System.Drawing.Point(22, 18);
            this.nameLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(172, 46);
            this.nameLabel.TabIndex = 0;
            this.nameLabel.Text = "Name";
            this.nameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // nameTextBox
            // 
            this.nameTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nameTextBox.Location = new System.Drawing.Point(202, 23);
            this.nameTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.nameTextBox.Name = "nameTextBox";
            this.nameTextBox.Size = new System.Drawing.Size(316, 26);
            this.nameTextBox.TabIndex = 1;
            // 
            // maxWidthLabel
            // 
            this.maxWidthLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.maxWidthLabel.Location = new System.Drawing.Point(22, 64);
            this.maxWidthLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.maxWidthLabel.Name = "maxWidthLabel";
            this.maxWidthLabel.Size = new System.Drawing.Size(172, 46);
            this.maxWidthLabel.TabIndex = 2;
            this.maxWidthLabel.Text = "Max width";
            this.maxWidthLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // maxWidthBox
            // 
            this.maxWidthBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.maxWidthBox.Location = new System.Drawing.Point(202, 69);
            this.maxWidthBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.maxWidthBox.Maximum = new decimal(new int[] {
            20000,
            0,
            0,
            0});
            this.maxWidthBox.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.maxWidthBox.Name = "maxWidthBox";
            this.maxWidthBox.Size = new System.Drawing.Size(316, 26);
            this.maxWidthBox.TabIndex = 3;
            this.maxWidthBox.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.maxWidthBox.Enter += new System.EventHandler(this.maxWidthBox_Enter);
            // 
            // maxHeightLabel
            // 
            this.maxHeightLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.maxHeightLabel.Location = new System.Drawing.Point(22, 110);
            this.maxHeightLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.maxHeightLabel.Name = "maxHeightLabel";
            this.maxHeightLabel.Size = new System.Drawing.Size(172, 46);
            this.maxHeightLabel.TabIndex = 4;
            this.maxHeightLabel.Text = "Max height";
            this.maxHeightLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // maxHeightBox
            // 
            this.maxHeightBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.maxHeightBox.Location = new System.Drawing.Point(202, 115);
            this.maxHeightBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.maxHeightBox.Maximum = new decimal(new int[] {
            20000,
            0,
            0,
            0});
            this.maxHeightBox.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.maxHeightBox.Name = "maxHeightBox";
            this.maxHeightBox.Size = new System.Drawing.Size(316, 26);
            this.maxHeightBox.TabIndex = 5;
            this.maxHeightBox.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.maxHeightBox.Enter += new System.EventHandler(this.maxHeightBox_Enter);
            // 
            // jpegQualityLabel
            // 
            this.jpegQualityLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jpegQualityLabel.Location = new System.Drawing.Point(22, 156);
            this.jpegQualityLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.jpegQualityLabel.Name = "jpegQualityLabel";
            this.jpegQualityLabel.Size = new System.Drawing.Size(172, 46);
            this.jpegQualityLabel.TabIndex = 6;
            this.jpegQualityLabel.Text = "JPEG quality";
            this.jpegQualityLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // jpegQualityBox
            // 
            this.jpegQualityBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.jpegQualityBox.Location = new System.Drawing.Point(202, 161);
            this.jpegQualityBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.jpegQualityBox.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.jpegQualityBox.Name = "jpegQualityBox";
            this.jpegQualityBox.Size = new System.Drawing.Size(316, 26);
            this.jpegQualityBox.TabIndex = 7;
            this.jpegQualityBox.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.jpegQualityBox.Enter += new System.EventHandler(this.jpegQualityBox_Enter);
            // 
            // outputFormatLabel
            // 
            this.outputFormatLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputFormatLabel.Location = new System.Drawing.Point(22, 202);
            this.outputFormatLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.outputFormatLabel.Name = "outputFormatLabel";
            this.outputFormatLabel.Size = new System.Drawing.Size(172, 46);
            this.outputFormatLabel.TabIndex = 8;
            this.outputFormatLabel.Text = "Output";
            this.outputFormatLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // outputFormatBox
            // 
            this.outputFormatBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.outputFormatBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.outputFormatBox.FormattingEnabled = true;
            this.outputFormatBox.Location = new System.Drawing.Point(202, 207);
            this.outputFormatBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.outputFormatBox.Name = "outputFormatBox";
            this.outputFormatBox.Size = new System.Drawing.Size(316, 28);
            this.outputFormatBox.TabIndex = 9;
            // 
            // suffixLabel
            // 
            this.suffixLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.suffixLabel.Location = new System.Drawing.Point(22, 248);
            this.suffixLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.suffixLabel.Name = "suffixLabel";
            this.suffixLabel.Size = new System.Drawing.Size(172, 46);
            this.suffixLabel.TabIndex = 10;
            this.suffixLabel.Text = "Suffix";
            this.suffixLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // suffixTextBox
            // 
            this.suffixTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.suffixTextBox.Location = new System.Drawing.Point(202, 253);
            this.suffixTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.suffixTextBox.Name = "suffixTextBox";
            this.suffixTextBox.Size = new System.Drawing.Size(316, 26);
            this.suffixTextBox.TabIndex = 11;
            this.suffixTextBox.Enter += new System.EventHandler(this.suffixTextBox_Enter);
            // 
            // doNotEnlargeBox
            // 
            this.doNotEnlargeBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.doNotEnlargeBox.Location = new System.Drawing.Point(202, 299);
            this.doNotEnlargeBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.doNotEnlargeBox.Name = "doNotEnlargeBox";
            this.doNotEnlargeBox.Size = new System.Drawing.Size(316, 36);
            this.doNotEnlargeBox.TabIndex = 12;
            this.doNotEnlargeBox.Text = "Do not enlarge smaller images";
            this.doNotEnlargeBox.UseVisualStyleBackColor = true;
            // 
            // buttonsPanel
            // 
            this.mainLayout.SetColumnSpan(this.buttonsPanel, 2);
            this.buttonsPanel.Controls.Add(this.okButton);
            this.buttonsPanel.Controls.Add(this.cancelButton);
            this.buttonsPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.buttonsPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonsPanel.Location = new System.Drawing.Point(22, 345);
            this.buttonsPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.buttonsPanel.Name = "buttonsPanel";
            this.buttonsPanel.Size = new System.Drawing.Size(496, 47);
            this.buttonsPanel.TabIndex = 13;
            // 
            // okButton
            // 
            this.okButton.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.okButton.Location = new System.Drawing.Point(372, 5);
            this.okButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(120, 35);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            // 
            // cancelButton
            // 
            this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.cancelButton.Location = new System.Drawing.Point(244, 5);
            this.cancelButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(120, 35);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            // 
            // ProfileEditorDialog
            // 
            this.AcceptButton = this.okButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.cancelButton;
            this.ClientSize = new System.Drawing.Size(540, 415);
            this.Controls.Add(this.mainLayout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Icon = global::DropResize.Properties.Resources.AppIcon;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ProfileEditorDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Edit Profile";
            this.mainLayout.ResumeLayout(false);
            this.mainLayout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.maxWidthBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.maxHeightBox)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.jpegQualityBox)).EndInit();
            this.buttonsPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel mainLayout;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Label maxWidthLabel;
        private System.Windows.Forms.NumericUpDown maxWidthBox;
        private System.Windows.Forms.Label maxHeightLabel;
        private System.Windows.Forms.NumericUpDown maxHeightBox;
        private System.Windows.Forms.Label jpegQualityLabel;
        private System.Windows.Forms.NumericUpDown jpegQualityBox;
        private System.Windows.Forms.Label outputFormatLabel;
        private System.Windows.Forms.ComboBox outputFormatBox;
        private System.Windows.Forms.Label suffixLabel;
        private System.Windows.Forms.TextBox suffixTextBox;
        private System.Windows.Forms.CheckBox doNotEnlargeBox;
        private System.Windows.Forms.FlowLayoutPanel buttonsPanel;
        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
    }
}
