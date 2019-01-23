using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using PanoBeam.BlendControls.CurveControl.Enums;

namespace PanoBeam.BlendControls.CurveControl
{
    public class ControlPoint : Thumb
    {
        public delegate void RemoveDelegate(ControlPoint point);
        public event RemoveDelegate Remove;

        public delegate void PointTypeChangedDelegate();
        public event PointTypeChangedDelegate PointTypeChanged;

        private readonly ContextMenu _contextMenuControlPoint = new ContextMenu();

        private readonly ControlPointFix _fix;

        public static readonly DependencyProperty PointTypeProperty = DependencyProperty.Register(
            "PointType", typeof(ControlPointType), typeof(ControlPoint), new PropertyMetadata(default(ControlPointType)));

        public ControlPointType PointType
        {
            get => (ControlPointType)GetValue(PointTypeProperty);
            set => SetValue(PointTypeProperty, value);
        }
        /// <summary>
        /// Point Dependency Property
        /// </summary>
        public static readonly DependencyProperty PointProperty = DependencyProperty.Register(
            "Point",
            typeof(Point),
            typeof(ControlPoint),
            new FrameworkPropertyMetadata(new Point()));

        /// <summary>
        /// Gets or sets the Point property
        /// </summary>
        public Point Point
        {
            get => (Point)GetValue(PointProperty);
            set => SetValue(PointProperty, value);
        }

        static ControlPoint()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ControlPoint), new FrameworkPropertyMetadata(typeof(ControlPoint)));
        }

        private readonly PanoBeamLib.Blend.ControlPoint _pointData;
        public PanoBeamLib.Blend.ControlPoint PointData => _pointData;

        private readonly Brush _color = Brushes.Yellow;
        public ControlPoint(double x, double y, PanoBeamLib.Blend.ControlPoint pointData, ControlPointFix fix = ControlPointFix.None)
        {
            _pointData = pointData;
            _fix = fix;
            Color = _color;
            Point = new Point(x, y);
            if (fix != ControlPointFix.Both)
            {
                DragDelta += OnDragDelta;
            }
            PointType = ControlPointType.Spline;

            var menuItem = new MenuItem {Header = "Löschen"};
            menuItem.Click += DeleteControlPointClick;
            _contextMenuControlPoint.Items.Add(menuItem);

            menuItem = new MenuItem
            {
                Header = "Gerade",
                Tag = ControlPointType.Line
            };
            menuItem.Click += ChangeControlPointType;
            _contextMenuControlPoint.Items.Add(menuItem);

            menuItem = new MenuItem
            {
                Header = "Kurve",
                Tag = ControlPointType.Spline
            };
            menuItem.Click += ChangeControlPointType;
            _contextMenuControlPoint.Items.Add(menuItem);

            ContextMenu = _contextMenuControlPoint;
        }

        public void UpdateContextMenuItems()
        {
            if (ContextMenu == null) return;
            foreach (MenuItem menuItem in ContextMenu.Items)
            {
                if (menuItem.Tag == null) continue;
                var tag = (ControlPointType)menuItem.Tag;
                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                if (PointType == tag)
                {
                    menuItem.Visibility = Visibility.Collapsed;
                }
                else
                {
                    menuItem.Visibility = Visibility.Visible;
                }
            }
        }

        private void DeleteControlPointClick(object sender, RoutedEventArgs routedEventArgs)
        {
            Remove?.Invoke(this);
        }

        private void ChangeControlPointType(object sender, RoutedEventArgs routedEventArgs)
        {
            var menuItem = (MenuItem)sender;
            var contextMenu = (ContextMenu)menuItem.Parent;
            var cp = (ControlPoint)(contextMenu).PlacementTarget;
            cp.PointType = (ControlPointType)menuItem.Tag;
            PointData.PointType = Mapper.ConvertControlPointType(cp.PointType);
            PointTypeChanged?.Invoke();
        }

        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
            "Color", typeof(Brush), typeof(ControlPoint), new PropertyMetadata(default(Brush)));

        public Brush Color
        {
            get => (Brush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public static readonly DependencyProperty ActiveProperty = DependencyProperty.Register(
            "Active", typeof(bool), typeof(ControlPoint), new PropertyMetadata(default(bool)));

        private bool _active;

        public bool Active
        {
            get => _active;
            set
            {
                _active = value;
                Color = _active ? Brushes.CornflowerBlue : _color;
            }
        }

        public delegate void ValueChangedDelegate();
        public event ValueChangedDelegate ValueChanged;

        private void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            MovePoint((int)e.HorizontalChange, (int)e.VerticalChange);
        }

        private void MovePoint(int dx, int dy)
        {
            var x = Point.X;
            var y = Point.Y;
            if (_fix != ControlPointFix.X)
            {
                x += dx;
                if (NeighborLeft != null && x <= NeighborLeft.X)
                {
                    x = NeighborLeft.X;
                }
                if (NeighborRight != null && x >= NeighborRight.X)
                {
                    x = NeighborRight.X;
                }
                if (x <= 0)
                {
                    x = 0;
                }
                if (x >= 199)
                {
                    x = 199;
                }
            }
            if (_fix != ControlPointFix.Y)
            {
                y += dy;
                if (y <= 0)
                {
                    y = 0;
                }
                if (y >= 199)
                {
                    y = 199;
                }
            }

            Point = new Point(x, y);
            _pointData.Update(Point.X / 200d, Point.Y / 200d);
            ValueChanged?.Invoke();
        }

        public void MoveLeft()
        {
            MovePoint(-1, 0);
        }

        public void MoveRight()
        {
            MovePoint(1, 0);
        }

        public void MoveUp()
        {
            MovePoint(0, -1);
        }

        public void MoveDown()
        {
            MovePoint(0, 1);
        }

        public double X => Point.X;
        public double Y => Point.Y;

        public ControlPoint NeighborLeft { get; set; }
        public ControlPoint NeighborRight { get; set; }
    }
}
