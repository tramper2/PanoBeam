using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PanoBeam.Controls.ControlPointsControl
{
    /// <summary>
    /// Interaction logic for ControlPointsControl.xaml
    /// </summary>
    public partial class ControlPointsControl
    {
        public event ControlPointTypeChangedDelegate ControlPointTypeChanged;
        public event ControlPointMovedDelegate ControlPointMoved;
        public Action<ControlPoint> ControlPointActivated;

        private readonly Brush _brushEnabled = new SolidColorBrush(Color.FromArgb(255, 151, 0, 0));
        private readonly Brush _brushDisabled = new SolidColorBrush(Color.FromArgb(50, 151, 0, 0));
        private readonly Brush _brushActive = new SolidColorBrush(Colors.DarkGoldenrod);
        private readonly Brush _brushLineEnabled = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        private readonly Brush _brushLineDisabled = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0));
        private readonly List<ControlPointControl> _controlPointControls = new List<ControlPointControl>();

        private bool _isActive;
        public bool IsActive => _isActive;

        public WireframeType WireframeType { get; set; }
        public int[] Wireframe { get; set; }

        private ControlPointControl _activeControlPointControl;

        public ControlPointsControl()
        {
            InitializeComponent();
        }

        public void Initialize(int width, int height)
        {
            Canvas0.Width = width;
            Canvas0.Height = height;
        }

        public void Activate()
        {
            _isActive = true;
            RefreshControlPoints();
            AddWireframe();
            Canvas0.AllowDragging = true;
        }

        public void DeActivate()
        {
            _isActive = false;
            RefreshControlPoints();
            AddWireframe();
            Canvas0.AllowDragging = false;
        }

        public void SetVisibility(bool controlPointsVisible, bool wireframeVisible)
        {
            foreach (var cp in _controlPointControls)
            {
                cp.Visibility = controlPointsVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            if (!controlPointsVisible)
            {
                if (_activeControlPointControl != null)
                {
                    _activeControlPointControl.Color = GetColor();
                    _activeControlPointControl = null;
                }
            }
            LineCanvas.Visibility = wireframeVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        public void UpdateControlPoints(ControlPoint[] controlPoints)
        {
            foreach (var cp in controlPoints)
            {
                var controlPoint = _controlPointControls.First(c => c.ControlPoint.U == cp.U && c.ControlPoint.V == cp.V);
                if (controlPoint.ControlPoint.X != cp.X || controlPoint.ControlPoint.Y != cp.Y)
                {
                    controlPoint.ControlPoint.X = cp.X;
                    controlPoint.ControlPoint.Y = cp.Y;
                    controlPoint.UpdateXY();
                }
            }
            AddWireframe();
        }

        public void ResetControlPoints(ControlPoint[] controlPoints, bool controlPointsVisible)
        {
            _controlPointControls.Clear();
            Canvas0.Children.Clear();

            foreach (var cp in controlPoints)
            {
                var control = new ControlPointControl
                {
                    ControlPointType = cp.ControlPointType,
                    Color = GetColor()
                };
                control.Initialize(cp, ActivateControlPoint);
                control.ControlPointTypeChanged += controlPoint =>
                {
                    if (controlPoint.ControlPointType == ControlPointType.Default)
                    {
                        
                    }
                    ControlPointTypeChanged?.Invoke(ConvertControlPointData(controlPoint));
                };
                Canvas0.Children.Add(control);
                _controlPointControls.Add(control);
            }

            foreach (var cp in _controlPointControls)
            {
                cp.Visibility = controlPointsVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            RefreshControlPoints();
            AddWireframe();
        }

        private void ActivateControlPoint(ControlPointControl cp)
        {
            if (!_isActive) return;
            if (_activeControlPointControl != null)
            {
                _activeControlPointControl.Color = GetColor();
            }
            _activeControlPointControl = cp;
            if (_activeControlPointControl != null)
            {
                _activeControlPointControl.Color = _brushActive;
                ControlPointActivated?.Invoke(ConvertControlPointData(_activeControlPointControl));
            }
        }

        private void RefreshControlPoints()
        {
            var color = GetColor();
            foreach (var cp in _controlPointControls)
            {
                cp.Color = color;
                cp.IsEnabled = _isActive;
            }
        }

        private Brush GetColor()
        {
            return _isActive
                ? _brushEnabled
                : _brushDisabled;
        }

        public void KeyPressed(KeyEventArgs e, bool shift)
        {
            if (_activeControlPointControl == null)
            {
                return;
            }
            int delta = 1;
            if (shift)
            {
                delta = 10;
            }

            if (e.Key == Key.Up)
            {
                _activeControlPointControl.MoveY(-delta);
                ControlPointMoved?.Invoke(ConvertControlPointData(_activeControlPointControl));
            }
            else if (e.Key == Key.Down)
            {
                _activeControlPointControl.MoveY(delta);
                ControlPointMoved?.Invoke(ConvertControlPointData(_activeControlPointControl));
            }
            else if (e.Key == Key.Left)
            {
                _activeControlPointControl.MoveX(-delta);
                ControlPointMoved?.Invoke(ConvertControlPointData(_activeControlPointControl));
            }
            else if (e.Key == Key.Right)
            {
                _activeControlPointControl.MoveX(delta);
                ControlPointMoved?.Invoke(ConvertControlPointData(_activeControlPointControl));
            }
        }

        private void Canvas0_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isActive) return;
            if (Canvas0.ElementBeingDragged == null) return;

            var c = (ControlPointControl)Canvas0.ElementBeingDragged;
            if (c.UpdateControlPoint())
            {
                ControlPointMoved?.Invoke(ConvertControlPointData(c));
                AddWireframe();
            }
        }

        private ControlPoint ConvertControlPointData(ControlPointControl control)
        {
            return new ControlPoint(
                control.ControlPoint.X,
                control.ControlPoint.Y,
                control.ControlPoint.U,
                control.ControlPoint.V,
                control.ControlPoint.ControlPointType,
                control.ControlPoint.ControlPointDirections
            );
        }

        private void AddWireframe()
        {
            var stroke = _isActive ? _brushLineEnabled : _brushLineDisabled;

            LineCanvas.Children.Clear();

            if (WireframeType == WireframeType.Trianglestrip)
            {
                for (var j = 0; j < Wireframe.Length - 2; j++)
                {
                    var p0 = _controlPointControls[Wireframe[j]].ControlPoint;
                    var p1 = _controlPointControls[Wireframe[j + 1]].ControlPoint;
                    var p2 = _controlPointControls[Wireframe[j + 2]].ControlPoint;
                    if (j == 0)
                    {
                        LineCanvas.Children.Add(new Line
                        {
                            Stroke = stroke,
                            StrokeThickness = 1,
                            X1 = p0.X,
                            X2 = p1.X,
                            Y1 = p0.Y,
                            Y2 = p1.Y
                        });
                    }
                    LineCanvas.Children.Add(new Line
                    {
                        Stroke = stroke,
                        StrokeThickness = 1,
                        X1 = p1.X,
                        X2 = p2.X,
                        Y1 = p1.Y,
                        Y2 = p2.Y
                    });

                    LineCanvas.Children.Add(new Line
                    {
                        Stroke = stroke,
                        StrokeThickness = 1,
                        X1 = p0.X,
                        X2 = p2.X,
                        Y1 = p0.Y,
                        Y2 = p2.Y
                    });
                }
            }
            else if (WireframeType == WireframeType.Connect)
            {
                var p0 = _controlPointControls[Wireframe[Wireframe.Length - 1]].ControlPoint;
                var p1 = _controlPointControls[Wireframe[0]].ControlPoint;
                LineCanvas.Children.Add(new Line
                {
                    Stroke = stroke,
                    StrokeThickness = 1,
                    X1 = p0.X,
                    X2 = p1.X,
                    Y1 = p0.Y,
                    Y2 = p1.Y
                });
                for (var j = 0; j < Wireframe.Length - 1; j++)
                {
                    p0 = _controlPointControls[Wireframe[j]].ControlPoint;
                    p1 = _controlPointControls[Wireframe[j + 1]].ControlPoint;
                    LineCanvas.Children.Add(new Line
                    {
                        Stroke = stroke,
                        StrokeThickness = 1,
                        X1 = p0.X,
                        X2 = p1.X,
                        Y1 = p0.Y,
                        Y2 = p1.Y
                    });
                }
            }
        }
    }
}
