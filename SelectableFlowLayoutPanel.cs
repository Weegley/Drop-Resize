using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DropResize
{
    public class SelectableFlowLayoutPanel : FlowLayoutPanel
    {
        private Rectangle selectionRectangle;

        public SelectableFlowLayoutPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        public Rectangle SelectionRectangle
        {
            get { return selectionRectangle; }
            set
            {
                selectionRectangle = value;
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (selectionRectangle.Width <= 0 || selectionRectangle.Height <= 0)
            {
                return;
            }

            using (var brush = new SolidBrush(Color.FromArgb(40, 51, 153, 255)))
            using (var pen = new Pen(Color.FromArgb(80, 120, 200)))
            {
                pen.DashStyle = DashStyle.Dash;
                e.Graphics.FillRectangle(brush, selectionRectangle);
                e.Graphics.DrawRectangle(pen, selectionRectangle);
            }
        }
    }
}
