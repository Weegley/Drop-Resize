using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace DropResize
{
    public class ImageCardControl : UserControl
    {
        private readonly PictureBox thumbnailBox;
        private readonly Label titleLabel;
        private readonly Label detailsLabel;
        private readonly Label statusLabel;
        private bool isSelected;
        private bool isKeyboardFocused;

        public ImageCardControl(ResizeResult result)
        {
            Result = result;
            Width = 244;
            Height = 132;
            Margin = new Padding(0, 0, 8, 8);
            BackColor = Color.White;

            thumbnailBox = new PictureBox
            {
                Location = new Point(10, 10),
                Size = new Size(72, 72),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.FromArgb(242, 244, 246)
            };

            titleLabel = new Label
            {
                Location = new Point(90, 8),
                Size = new Size(142, 28),
                Font = new Font(Font.FontFamily, 8.0f, FontStyle.Bold),
                AutoEllipsis = true
            };

            detailsLabel = new Label
            {
                Location = new Point(90, 38),
                Size = new Size(142, 62),
                Font = new Font(Font.FontFamily, 7.6f),
                ForeColor = Color.FromArgb(55, 63, 70)
            };

            statusLabel = new Label
            {
                Location = new Point(10, 100),
                Size = new Size(222, 22),
                Font = new Font(Font.FontFamily, 7.6f, FontStyle.Bold),
                ForeColor = result.Success ? Color.FromArgb(20, 120, 70) : Color.FromArgb(180, 45, 45),
                AutoEllipsis = true
            };

            Controls.Add(thumbnailBox);
            Controls.Add(titleLabel);
            Controls.Add(detailsLabel);
            Controls.Add(statusLabel);

            Populate();
            WireMouseEvents(this);
        }

        public event MouseEventHandler CardMouseDown;
        public event MouseEventHandler CardMouseMove;
        public event MouseEventHandler CardMouseUp;

        public ResizeResult Result { get; private set; }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                BackColor = isSelected ? Color.FromArgb(229, 243, 255) : Color.White;
            }
        }

        public bool IsKeyboardFocused
        {
            get { return isKeyboardFocused; }
            set
            {
                isKeyboardFocused = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var color = isKeyboardFocused ? Color.FromArgb(0, 120, 215) : Color.FromArgb(180, 180, 180);
            var width = isKeyboardFocused ? 2 : 1;

            using (var pen = new Pen(color, width))
            {
                var offset = width / 2;
                e.Graphics.DrawRectangle(
                    pen,
                    offset,
                    offset,
                    Width - width,
                    Height - width);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && thumbnailBox.Image != null)
            {
                thumbnailBox.Image.Dispose();
            }

            base.Dispose(disposing);
        }

        public void SetDropHandlers(DragEventHandler dragEnter, DragEventHandler dragDrop)
        {
            WireDropEvents(this, dragEnter, dragDrop);
        }

        private void Populate()
        {
            titleLabel.Text = Path.GetFileName(Result.InputPath);

            if (Result.Success)
            {
                thumbnailBox.Image = Result.Thumbnail ?? LoadThumbnail(Result.OutputPath);
            }

            detailsLabel.Text =
                "Original: " + Result.OriginalWidth + " x " + Result.OriginalHeight + Environment.NewLine +
                "Output: " + Result.OutputWidth + " x " + Result.OutputHeight + Environment.NewLine +
                "Input: " + FormatBytes(Result.OriginalFileSize) + Environment.NewLine +
                "Output: " + FormatBytes(Result.OutputFileSize);

            statusLabel.Text = Result.Success ? "Done" : "Error: " + Result.ErrorMessage;
        }

        private static Image LoadThumbnail(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var image = Image.FromStream(stream))
            {
                return new Bitmap(image);
            }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024)
            {
                return bytes + " B";
            }

            if (bytes < 1024 * 1024)
            {
                return (bytes / 1024.0).ToString("0.0") + " KB";
            }

            return (bytes / 1024.0 / 1024.0).ToString("0.0") + " MB";
        }

        private void WireMouseEvents(Control control)
        {
            control.MouseDown += (sender, args) =>
            {
                if (CardMouseDown != null)
                {
                    CardMouseDown(this, args);
                }
            };
            control.MouseMove += (sender, args) =>
            {
                if (CardMouseMove != null)
                {
                    CardMouseMove(this, args);
                }
            };
            control.MouseUp += (sender, args) =>
            {
                if (CardMouseUp != null)
                {
                    CardMouseUp(this, args);
                }
            };

            foreach (Control child in control.Controls)
            {
                WireMouseEvents(child);
            }
        }

        private static void WireDropEvents(Control control, DragEventHandler dragEnter, DragEventHandler dragDrop)
        {
            control.AllowDrop = true;
            control.DragEnter += dragEnter;
            control.DragDrop += dragDrop;

            foreach (Control child in control.Controls)
            {
                WireDropEvents(child, dragEnter, dragDrop);
            }
        }
    }
}
