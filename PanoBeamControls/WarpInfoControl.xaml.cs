using System.Windows.Controls;
using PanoBeam.Controls.ControlPointsControl;

namespace PanoBeam.Controls
{
    /// <summary>
    /// Interaction logic for WarpInfoControl.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class WarpInfoControl : UserControl
    {
        public WarpInfoControl()
        {
            InitializeComponent();
        }

        public void Update(ControlPoint controlPointData)
        {
            if (controlPointData == null) return;
            X.Text = $"U, X: {controlPointData.U}, {controlPointData.X}";
            Y.Text = $"V, Y: {controlPointData.V}, {controlPointData.Y}";
        }

        //public void Update(int projector, Vertex vertex)
        //{
        //    string text;
        //    if (projector == 0)
        //    {
        //        text = "Links";
        //    }
        //    else
        //    {
        //        text = "Rechts";
        //    }
        //    Index.Text = string.Format("Vertex {0}", vertex.Index);
        //    P.Text = string.Format("{0} {1}x{2}", text, vertex.U, vertex.V);
        //    X.Text = string.Format("X: {0} ({1})", vertex.X, vertex.X - vertex.U);
        //    Y.Text = string.Format("Y: {0} ({1})", vertex.Y, vertex.Y - vertex.V);
        //}

    }
}
