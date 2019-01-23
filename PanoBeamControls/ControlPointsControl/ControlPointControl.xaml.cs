using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PanoBeam.Common;

namespace PanoBeam.Controls.ControlPointsControl
{
    /// <summary>
    /// Interaction logic for ControlPointControl.xaml
    /// </summary>
    public partial class ControlPointControl
    {
        public Action<ControlPointControl> ControlPointTypeChanged;
        private ControlPoint _controlPoint;
        private Action<ControlPointControl> _activateControlPoint;

        public ControlPointControl()
        {
            Color = new SolidColorBrush(System.Windows.Media.Color.FromArgb(50, 151, 0, 0));
            InitializeComponent();
            IsEnabled = false;
            SetValue(DragCanvas.CanBeDraggedProperty, false);
        }

        public void Initialize(ControlPoint controlPoint, Action<ControlPointControl> activateControlPoint)
        {
            _activateControlPoint = activateControlPoint;
            _controlPoint = controlPoint;
            UpdateXY();
        }

        public ControlPoint ControlPoint => _controlPoint;

        public bool UpdateControlPoint()
        {
            var x = (int)Canvas.GetLeft(this);
            var y = (int)Canvas.GetTop(this);

            if (_controlPoint.X == x && _controlPoint.Y == y)
            {
                return false;
            }
            _controlPoint.X = x;
            _controlPoint.Y = y;
            return true;
        }

        public void MoveY(int delta)
        {
            if (delta == 0) return;
            var y = _controlPoint.Y;
            y += delta;
            if (delta > 0)
            {
                if (!_controlPoint.ControlPointDirections.HasFlag(ControlPointDirections.Down))
                {
                    if (y > _controlPoint.V)
                    {
                        y = _controlPoint.V;
                    }
                }
            }
            else
            {
                if (!_controlPoint.ControlPointDirections.HasFlag(ControlPointDirections.Up))
                {
                    if (y < _controlPoint.V)
                    {
                        y = _controlPoint.V;
                    }
                }
            }

            _controlPoint.Y = y;
            UpdateY();
        }

        public void MoveX(int delta)
        {
            if (delta == 0) return;
            var x = _controlPoint.X;
            x += delta;
            if (delta > 0)
            {
                if (!_controlPoint.ControlPointDirections.HasFlag(ControlPointDirections.Right))
                {
                    if (x > _controlPoint.U)
                    {
                        x = _controlPoint.U;
                    }
                }
            }
            else
            {
                if (!_controlPoint.ControlPointDirections.HasFlag(ControlPointDirections.Left))
                {
                    if (x < _controlPoint.U)
                    {
                        x = _controlPoint.U;
                    }
                }
            }

            _controlPoint.X = x;
            UpdateX();
        }

        private void UpdateX()
        {
            Canvas.SetLeft(this, _controlPoint.X);
        }

        private void UpdateY()
        {
            Canvas.SetTop(this, _controlPoint.Y);
        }

        // ReSharper disable once InconsistentNaming
        public void UpdateXY()
        {
            UpdateX();
            UpdateY();
        }

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(Brush), typeof(ControlPointControl), new PropertyMetadata(default(Brush)));

        public Brush Color
        {
            get => (Brush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty ControlPointTypeProperty = DependencyProperty.Register(
            "ControlPointType", typeof(ControlPointType), typeof(ControlPointControl), new PropertyMetadata(default(ControlPointType)));

        public ControlPointType ControlPointType
        {
            get => (ControlPointType)GetValue(ControlPointTypeProperty);
            set
            {
                SetValue(ControlPointTypeProperty, value);
                SetValue(DragCanvas.CanBeDraggedProperty, ControlPointType != ControlPointType.Default);
            }
        }

        private void OnMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (!IsEnabled) return;
            if (e.ClickCount == 1)
            {
                if (_controlPoint.ControlPointType == ControlPointType.Default) return;
                _activateControlPoint(this);
            }
            else if (e.ClickCount == 2)
            {
                if (_controlPoint.ControlPointType == ControlPointType.IsEcke) return;
                if (_controlPoint.ControlPointType == ControlPointType.Default)
                {
                    _controlPoint.ControlPointType = ControlPointType.IsFix;
                    ControlPointType = _controlPoint.ControlPointType;
                    _activateControlPoint(this);
                    ControlPointTypeChanged?.Invoke(this);
                }
                else
                {
                    _controlPoint.ControlPointType = ControlPointType.Default;
                    ControlPointType = _controlPoint.ControlPointType;
                    _activateControlPoint(null);
                    ControlPointTypeChanged?.Invoke(this);
                }
            }
        }
    }
}
