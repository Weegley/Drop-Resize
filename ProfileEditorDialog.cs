using System;
using System.Windows.Forms;

namespace DropResize
{
    public partial class ProfileEditorDialog : Form
    {
        public ProfileEditorDialog(ResizeProfile profile)
        {
            InitializeComponent();

            Profile = profile.Clone();
            Text = profile.IsBuiltIn ? "Edit Built-in Profile" : "Edit Profile";

            nameTextBox.Text = Profile.Name;
            maxWidthBox.Value = Math.Max(maxWidthBox.Minimum, Math.Min(maxWidthBox.Maximum, Profile.MaxWidth));
            maxHeightBox.Value = Math.Max(maxHeightBox.Minimum, Math.Min(maxHeightBox.Maximum, Profile.MaxHeight));
            jpegQualityBox.Value = Math.Max(
                jpegQualityBox.Minimum,
                Math.Min(jpegQualityBox.Maximum, Profile.JpegQuality));

            outputFormatBox.Items.AddRange(new object[] { OutputFormat.Jpeg, OutputFormat.Png, OutputFormat.Bmp });
            outputFormatBox.SelectedItem = Profile.OutputFormat;
            suffixTextBox.Text = Profile.Suffix;
            doNotEnlargeBox.Checked = Profile.DoNotEnlarge;
        }

        public ResizeProfile Profile { get; private set; }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (string.IsNullOrWhiteSpace(nameTextBox.Text))
                {
                    MessageBox.Show(
                        "Profile name is required.",
                        "Drop&Resize",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    e.Cancel = true;
                    return;
                }

                Profile.Name = nameTextBox.Text.Trim();
                Profile.MaxWidth = (int)maxWidthBox.Value;
                Profile.MaxHeight = (int)maxHeightBox.Value;
                Profile.JpegQuality = (long)jpegQualityBox.Value;
                Profile.OutputFormat = (OutputFormat)outputFormatBox.SelectedItem;
                Profile.Suffix = string.IsNullOrWhiteSpace(suffixTextBox.Text)
                    ? "_resized"
                    : suffixTextBox.Text.Trim();
                Profile.DoNotEnlarge = doNotEnlargeBox.Checked;
                Profile.ResizeMode = ResizeMode.Fit;
            }

            base.OnFormClosing(e);
        }

        private void maxWidthBox_Enter(object sender, EventArgs e)
        {
            maxWidthBox.Select(0, maxWidthBox.Text.Length);
        }

        private void maxHeightBox_Enter(object sender, EventArgs e)
        {
            maxHeightBox.Select(0, maxHeightBox.Text.Length);
        }

        private void jpegQualityBox_Enter(object sender, EventArgs e)
        {
            jpegQualityBox.Select(0, jpegQualityBox.Text.Length);
        }

        private void suffixTextBox_Enter(object sender, EventArgs e)
        {
            suffixTextBox.Select(0, suffixTextBox.TextLength);
        }
    }
}
