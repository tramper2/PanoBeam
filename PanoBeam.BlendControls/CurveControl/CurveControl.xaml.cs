using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PanoBeam.BlendControls.CurveControl.Enums;
using PanoBeamLib.Blend;
using ControlPointType = PanoBeam.BlendControls.CurveControl.Enums.ControlPointType;
using Gdi = System.Drawing;

namespace PanoBeam.BlendControls.CurveControl
{
    /// <summary>
    /// Interaction logic for CurveControl.xaml
    /// </summary>
    public partial class CurveControl
    {
        //private readonly List<ControlPoint> _points = new List<ControlPoint>();
        // ReSharper disable once InconsistentNaming
        private const int PRECISION = 10;
        private ControlPoint _activeThumbPoint;
        private Curve _curve;
        private readonly int _width = 200;
        private readonly int _height = 200;

        public CurveControl()
        {
            InitializeComponent();
        }

        public void SetBlendCurve(Curve blendCurve)
        {
            _curve = blendCurve;
        }

        public void Refresh()
        {
            if (_curve == null) return;
            for (var i = Canvas1.Children.Count - 1; i >= 0; i--)
            {
                if (Canvas1.Children[i] is ControlPoint)
                {
                    Canvas1.Children.Remove(Canvas1.Children[i]);
                }
            }
            var point = ConvertPoint(_curve.ControlPoints[0], ControlPointFix.None);
            point.Color = Brushes.Orange;
            point.PointType = ControlPointType.Line;
            point.ValueChanged += PointOnValueChanged;
            Canvas1.Children.Add(point);
            //_curveData.Points.Add(point);
            point = ConvertPoint(_curve.ControlPoints[_curve.ControlPoints.Length - 1], ControlPointFix.None);
            point.Color = Brushes.Orange;
            point.PointType = ControlPointType.Line;
            point.ValueChanged += PointOnValueChanged;
            Canvas1.Children.Add(point);

            for (int i = 1; i < _curve.ControlPoints.Length - 1; i++)
            {
                AddPointControl(_curve.ControlPoints[i]);
            }

            RefreshGraph();
        }

        //private void SetPoints(PointForSave[] points)
        //{
        //    if (points == null) return;
        //    if (points.Length < 2) return;
        //    if (_curveData == null) return;
        //    _curveData.Points.Clear();
        //    for (var i = Canvas1.Children.Count - 1; i >= 0; i--)
        //    {
        //        if (Canvas1.Children[i] is ControlPoint)
        //        {
        //            Canvas1.Children.Remove(Canvas1.Children[i]);
        //        }
        //    }
        //    var point = ConvertPoint(points[0], ControlPointFix.None);
        //    point.Color = Brushes.Orange;
        //    point.PointType = ControlPointType.Line;
        //    point.ValueChanged += PointOnValueChanged;
        //    Canvas1.Children.Add(point);
        //    _curveData.Points.Add(point);
        //    point = ConvertPoint(points[points.Length - 1], ControlPointFix.None);
        //    point.Color = Brushes.Orange;
        //    point.PointType = ControlPointType.Line;
        //    point.ValueChanged += PointOnValueChanged;
        //    Canvas1.Children.Add(point);
        //    _curveData.Points.Add(point);

        //    for (int i = 1; i < points.Length - 1; i++)
        //    {
        //        InsertPoint(points[i].X, points[i].Y, points[i].PointType);
        //    }

        //    RefreshGraph();
        //}

        //public double GetY(double x)
        //{
        //    double y;
        //    var knownSamples = new List<KeyValuePair<double, ControlPoint>>();
        //    foreach (var p in _curveData.Points)
        //    {
        //        if (p.PointType == ControlPointType.Line)
        //        {
        //            if (x <= p.X)
        //            {
        //                knownSamples.Add(new KeyValuePair<double, ControlPoint>(p.X, p));
        //                y = SpLine(knownSamples, x);
        //                if (double.IsInfinity(y))
        //                {
        //                    return 199d;
        //                }
        //                return y;
        //            }
        //            else
        //            {
        //                knownSamples.Clear();
        //                knownSamples.Add(new KeyValuePair<double, ControlPoint>(p.X, p));
        //            }
        //        }
        //        else
        //        {
        //            knownSamples.Add(new KeyValuePair<double, ControlPoint>(p.X, p));
        //        }
        //    }
        //    y = SpLine(knownSamples, x);
        //    if (double.IsInfinity(y))
        //    {
        //        return 199d;
        //    }
        //    return y;
        //}

        private void AddPointControl(PanoBeamLib.Blend.ControlPoint controlPoint)
        {
            var point = new ControlPoint(controlPoint.X * _width, controlPoint.Y * _height, controlPoint)
            {
                PointType = Mapper.ConvertControlPointType(controlPoint.PointType)
            };
            point.ValueChanged += PointOnValueChanged;
            Canvas1.Children.Add(point);
            point.PointTypeChanged += RefreshGraph;
            point.Remove += PointOnRemove;
            point.PreviewMouseLeftButtonUp += PointOnMouseUp;
            RefreshGraph();
        }

        public void InsertPoint(double x, double y, ControlPointType pointType = ControlPointType.Spline)
        {
            var point = new PanoBeamLib.Blend.ControlPoint(x / 200d, y / 200d, Mapper.ConvertControlPointType(pointType));
            _curve.InsertPoint(point);
            AddPointControl(point);
        }

        private ControlPoint ConvertPoint(PanoBeamLib.Blend.ControlPoint point, ControlPointFix fix)
        {
            return new ControlPoint(point.X * _width, point.Y * _height, point, fix)
            {
                PointType = Mapper.ConvertControlPointType(point.PointType)
            };
        }

        private List<Line> CalcGraph(List<PanoBeamLib.Blend.ControlPoint> points)
        {
            var lines = new List<Line>();
            var np = points.Count;

            var yCoords = new double[np];        // Newton form coefficients
            var xCoords = new double[np];        // x-coordinates of nodes

            for (int i = 0; i < np; i++)
            {
                var p = points[i];
                xCoords[i] = p.X * _width;
                yCoords[i] = p.Y * _height;
                //g.DrawRectangle(pointsPen, p.X - p.PT_SIZE, p.Y - p.PT_SIZE, p.PT_SIZE * 2, p.PT_SIZE * 2);

                //if (this.checkBoxShowCoords.Checked)
                //g.DrawString(string.Format("{0},{1}", p.X, p.Y), new Gdi.Font("Verdana", 6), Gdi.Brushes.Black, p.X + p.PT_SIZE + 1, p.Y + p.PT_SIZE + 1);
            }

            var a = new double[np];
            var h = new double[np];
            for (int i = 1; i <= np - 1; i++)
            {
                h[i] = xCoords[i] - xCoords[i - 1];
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (h[i] == 0)
                {
                    h[i] = 0.0001d;
                }
            }
            if (np > 2)
            {
                var sub = new double[np - 1];
                var diag = new double[np - 1];
                var sup = new double[np - 1];

                for (int i = 1; i <= np - 2; i++)
                {
                    diag[i] = (h[i] + h[i + 1]) / 3;
                    sup[i] = h[i + 1] / 6;
                    sub[i] = h[i] / 6;
                    a[i] = (yCoords[i + 1] - yCoords[i]) / h[i + 1] - (yCoords[i] - yCoords[i - 1]) / h[i];
                }
                SolveTridiag(sub, diag, sup, ref a, np - 2);
            }

            var oldx = xCoords[0];
            var oldy = yCoords[0];

            for (var i = 1; i <= np - 1; i++)
            {
                for (var j = 1; j <= PRECISION; j++)
                {
                    var x1 = (h[i] * j) / PRECISION;
                    var x2 = h[i] - x1;
                    var y = ((-a[i - 1] / 6 * (x2 + h[i]) * x1 + yCoords[i - 1]) * x2 +
                                (-a[i] / 6 * (x1 + h[i]) * x2 + yCoords[i]) * x1) / h[i];
                    var x = xCoords[i - 1] + x1;

                    var line = new Line((int)oldx, (int)oldy, (int)x, (int)y);
                    lines.Add(line);

                    oldx = x;
                    oldy = y;
                }
            }
            return lines;
        }

        #region Graph
        private void RefreshGraph()
        {
            var np = _curve.ControlPoints.Length;
            if (np == 0) return;

            var points = new List<PanoBeamLib.Blend.ControlPoint>();
            var lines = new List<Line>();
            points.Add(_curve.ControlPoints[0]);
            for (int i = 1; i < np; i++)
            {
                points.Add(_curve.ControlPoints[i]);
                if (_curve.ControlPoints[i].PointType == PanoBeamLib.Blend.ControlPointType.Line)
                {
                    lines.AddRange(CalcGraph(points));
                    if (i + 1 < np)
                    {
                        if (_curve.ControlPoints[i + 1].PointType == PanoBeamLib.Blend.ControlPointType.Line)
                        {
                            lines.Add(new Line((int)(_curve.ControlPoints[i].X * _width), (int)(_curve.ControlPoints[i].Y * _height), (int)(_curve.ControlPoints[i + 1].X * _width),
                                (int)(_curve.ControlPoints[i + 1].Y * _height)));
                            i++;
                        }
                    }
                    points.Clear();
                    points.Add(_curve.ControlPoints[i]);
                }
            }

            var linePen = new Gdi.Pen(Gdi.Color.Yellow);


            using (var tempBitmap = new Gdi.Bitmap(200, 200))
            {
                using (var g = Gdi.Graphics.FromImage(tempBitmap))
                {
                    g.SmoothingMode = Gdi.Drawing2D.SmoothingMode.HighQuality;

                    //g.Clear(Gdi.Color.FromArgb(155,155,155));

                    foreach (var line in lines)
                    {
                        g.DrawLine(linePen, line.X0, line.Y0, line.X1, line.Y1);
                    }
                }

                var hbmp = tempBitmap.GetHbitmap();
                var options = BitmapSizeOptions.FromEmptyOptions();
                BlendImage.Source = Imaging.CreateBitmapSourceFromHBitmap(hbmp, IntPtr.Zero, Int32Rect.Empty, options);
            }
            RefreshPreviewImage();
        }

        private void RefreshPreviewImage()
        {
            using (var tempBitmap = new Gdi.Bitmap(200, 200))
            {
                using (var g = Gdi.Graphics.FromImage(tempBitmap))
                {
                    g.SmoothingMode = Gdi.Drawing2D.SmoothingMode.HighQuality;
                    //g.Clear(Gdi.Color.FromArgb(200, 200, 155));

                    for (int x = 0; x < _width; x++)
                    {
                        var y = _curve.GetY((double)x / _width) * _height;
                        if (y < 0) y = 0;
                        if (y > 199d) y = 199d;
                        var col = (int)(255d / 199d * y);

                        var linePen = new Gdi.Pen(Gdi.Color.FromArgb(col, col, col));
                        g.DrawLine(linePen, x, 0, x, 199);
                    }
                }

                var hbmp = tempBitmap.GetHbitmap();
                var options = BitmapSizeOptions.FromEmptyOptions();
                PreviewImage.Source = Imaging.CreateBitmapSourceFromHBitmap(hbmp, IntPtr.Zero, Int32Rect.Empty, options);
            }

            //using (var tempBitmap = new Gdi.Bitmap(200, 200))
            //{
            //    using (var g = Gdi.Graphics.FromImage(tempBitmap))
            //    {
            //        g.SmoothingMode = Gdi.Drawing2D.SmoothingMode.HighQuality;
            //        g.Clear(Gdi.Color.FromArgb(200, 200, 155));

            //        for (int x = 0; x < _width; x++)
            //        {
            //            var y = _curve.GetY(x) * _height;
            //            if (y < 0) y = 0;
            //            if (y > 199d) y = 199d;
            //            y = 199d - y;

            //            g.DrawRectangle(new Gdi.Pen(Gdi.Color.Black), x, (float)y, 1, 1);
            //        }
            //    }

            //    tempBitmap.GetHbitmap();
            //    BitmapSizeOptions.FromEmptyOptions();
            //    //Image1.Source = Imaging.CreateBitmapSourceFromHBitmap(hbmp, IntPtr.Zero, Int32Rect.Empty, options);
            //}
        }

        private void SolveTridiag(double[] sub, double[] diag, double[] sup, ref double[] b, int n)
        {
            /*                  solve linear system with tridiagonal n by n matrix a
                                using Gaussian elimination *without* pivoting
                                where   a(i,i-1) = sub[i]  for 2<=i<=n
                                        a(i,i)   = diag[i] for 1<=i<=n
                                        a(i,i+1) = sup[i]  for 1<=i<=n-1
                                (the values sub[1], sup[n] are ignored)
                                right hand side vector b[1:n] is overwritten with solution 
                                NOTE: 1...n is used in all arrays, 0 is unused */
            int i;
            /*                  factorization and forward substitution */
            for (i = 2; i <= n; i++)
            {
                sub[i] = sub[i] / diag[i - 1];
                diag[i] = diag[i] - sub[i] * sup[i - 1];
                b[i] = b[i] - sub[i] * b[i - 1];
            }
            b[n] = b[n] / diag[n];
            for (i = n - 1; i >= 1; i--)
            {
                b[i] = (b[i] - sup[i] * b[i + 1]) / diag[i];
            }
        }

        #endregion

        private void PointOnMouseUp(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            var thumbPoint = (ControlPoint)sender;
            if (Equals(_activeThumbPoint, thumbPoint))
            {
                _activeThumbPoint = null;
                thumbPoint.Active = false;
                return;
            }

            if (_activeThumbPoint != null)
            {
                _activeThumbPoint.Active = false;
            }
            TextBoxFocusCapture.Focus();
            _activeThumbPoint = thumbPoint;
            _activeThumbPoint.Active = true;
        }

        private void PointOnRemove(ControlPoint point)
        {
            _curve.RemovePoint(point.PointData);
            Canvas1.Children.Remove(point);
            RefreshGraph();
        }

        private void PointOnValueChanged()
        {
            RefreshGraph();
        }
        
        private void Canvas1_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                var canvas = (Canvas)sender;
                var pos = e.GetPosition(canvas);
                InsertPoint(pos.X, pos.Y);
            }
        }

        private void Canvas1_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var cp = e.Source as ControlPoint;

            cp?.UpdateContextMenuItems();
        }

        public void KeyPressed(KeyEventArgs e)
        {
            if (_activeThumbPoint == null) return;

            if (e.Key == Key.Left)
            {
                _activeThumbPoint.MoveLeft();
            }
            else if (e.Key == Key.Right)
            {
                _activeThumbPoint.MoveRight();
            }
            else if (e.Key == Key.Up)
            {
                _activeThumbPoint.MoveUp();
            }
            else if (e.Key == Key.Down)
            {
                _activeThumbPoint.MoveDown();
            }
        }

        private class Line
        {
            public int X0 { get; }
            public int Y0 { get; }
            public int X1 { get; }
            public int Y1 { get; }

            public Line(int x0, int y0, int x1, int y1)
            {
                X0 = x0;
                Y0 = y0;
                X1 = x1;
                Y1 = y1;
            }
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
    }
}
