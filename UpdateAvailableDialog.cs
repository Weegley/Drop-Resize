using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace DropResize
{
    public class UpdateAvailableDialog : Form
    {
        private const string RepositoryUrl = "https://github.com/Weegley/Drop-Resize";
        private readonly UpdateInfo updateInfo;

        public UpdateAvailableDialog(UpdateInfo updateInfo)
        {
            this.updateInfo = updateInfo;

            Text = "Update Available";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(430, 180);

            var titleLabel = new Label
            {
                AutoSize = false,
                Location = new Point(18, 18),
                Size = new Size(394, 26),
                Font = new Font(Font.FontFamily, 9.5f, FontStyle.Bold),
                Text = "Drop&Resize " + updateInfo.VersionText + " is available."
            };

            var messageLabel = new Label
            {
                AutoSize = false,
                Location = new Point(18, 52),
                Size = new Size(394, 42),
                Text = "Open the release page or download the latest Windows package."
            };

            var releaseLink = new LinkLabel
            {
                AutoSize = true,
                Location = new Point(18, 104),
                Text = "Open GitHub repository"
            };
            releaseLink.LinkClicked += (sender, args) => OpenUrl(RepositoryUrl);

            var downloadLink = new LinkLabel
            {
                AutoSize = true,
                Location = new Point(18, 128),
                Text = "Download DropResize-Windows.zip"
            };
            downloadLink.LinkClicked += (sender, args) => OpenUrl(updateInfo.DownloadUrl);

            var closeButton = new Button
            {
                DialogResult = DialogResult.OK,
                Location = new Point(320, 128),
                Size = new Size(92, 30),
                Text = "Close"
            };

            Controls.Add(titleLabel);
            Controls.Add(messageLabel);
            Controls.Add(releaseLink);
            Controls.Add(downloadLink);
            Controls.Add(closeButton);
            AcceptButton = closeButton;
            CancelButton = closeButton;
        }

        private static void OpenUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
