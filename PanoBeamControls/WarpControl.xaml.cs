using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PanoBeam.Controls.ControlPointsControl;
using PanoBeamLib;
using ControlPoint = PanoBeamLib.ControlPoint;
using ControlPointType = PanoBeamLib.ControlPointType;
using PanoBeam.Configuration;

namespace PanoBeam.Controls
{
    /// <summary>
    /// Interaction logic for WarpControl.xaml
    /// </summary>
    public partial class WarpControl
    {
        private readonly ProjectorContainer[] _projectors;

        public WarpControl()
        {
            InitializeComponent();
            _projectors = new[]
            {
                new ProjectorContainer { ProjectorControl = Projector0, BlacklevelControl = Blacklevel0, Blacklevel2Control = Blacklevel20, BlendRegionControl = BlendRegion0},
                new ProjectorContainer { ProjectorControl = Projector1, BlacklevelControl = Blacklevel1, Blacklevel2Control = Blacklevel21, BlendRegionControl = BlendRegion1}
            };
        }

        public void Initialize(PanoScreen screen)
        {
            var projectorWidth = (screen.Resolution.Width + screen.Overlap)/2;
            _projectors[1].ProjectorControl.Margin = new Thickness(projectorWidth - screen.Overlap, 0, 0, 0);
            _projectors[1].BlacklevelControl.Margin = new Thickness(projectorWidth - screen.Overlap, 0, 0, 0);
            _projectors[1].Blacklevel2Control.Margin = new Thickness(projectorWidth - screen.Overlap, 0, 0, 0);
            _projectors[1].BlendRegionControl.Margin = new Thickness(projectorWidth - screen.Overlap, 0, 0, 0);
            for (var i = 0; i < _projectors.Length; i++)
            {
                var projector = _projectors[i];
                projector.Projector = screen.Projectors[i];
                projector.ProjectorControl.Width = projectorWidth;
                projector.ProjectorControl.Height = screen.Resolution.Height;
                projector.ProjectorControl.WireframeType = WireframeType.Trianglestrip;
                projector.ProjectorControl.Wireframe = projector.Projector.TriangleStrip;
                projector.ProjectorControl.Initialize(projectorWidth, screen.Resolution.Height);
                projector.ProjectorControl.ControlPointTypeChanged += OnControlPointTypeChanged;
                projector.ProjectorControl.ControlPointMoved += OnControlPointMoved;
                projector.ProjectorControl.ControlPointActivated += UpdateWarpInfo;

                projector.BlacklevelControl.Width = projectorWidth;
                projector.BlacklevelControl.Height = screen.Resolution.Height;
                projector.BlacklevelControl.WireframeType = WireframeType.Connect;
                projector.BlacklevelControl.Wireframe = projector.Projector.BlacklevelIndexes;
                projector.BlacklevelControl.Initialize(projectorWidth, screen.Resolution.Height);
                projector.BlacklevelControl.ControlPointTypeChanged += OnBlacklevelControlPointTypeChanged;
                projector.BlacklevelControl.ControlPointMoved += OnBlacklevelControlPointMoved;
                projector.BlacklevelControl.ControlPointActivated += UpdateWarpInfo;

                projector.Blacklevel2Control.Width = projectorWidth;
                projector.Blacklevel2Control.Height = screen.Resolution.Height;
                projector.Blacklevel2Control.WireframeType = WireframeType.Connect;
                projector.Blacklevel2Control.Wireframe = projector.Projector.Blacklevel2Indexes;
                projector.Blacklevel2Control.Initialize(projectorWidth, screen.Resolution.Height);
                projector.Blacklevel2Control.ControlPointTypeChanged += OnBlacklevel2ControlPointTypeChanged;
                projector.Blacklevel2Control.ControlPointMoved += OnBlacklevel2ControlPointMoved;
                projector.Blacklevel2Control.ControlPointActivated += UpdateWarpInfo;

                projector.BlendRegionControl.Width = projectorWidth;
                projector.BlendRegionControl.Height = screen.Resolution.Height;
                projector.BlendRegionControl.WireframeType = WireframeType.Connect;
                projector.BlendRegionControl.Wireframe = projector.Projector.BlendRegionIndexes;
                projector.BlendRegionControl.Initialize(projectorWidth, screen.Resolution.Height);
                projector.BlendRegionControl.ControlPointTypeChanged += OnBlendRegionControlPointTypeChanged;
                projector.BlendRegionControl.ControlPointMoved += OnBlendRegionControlPointMoved;
                projector.BlendRegionControl.ControlPointActivated += UpdateWarpInfo;
            }
        }

        private void OnControlPointTypeChanged(ControlPointsControl.ControlPoint controlPointData)
        {
            OnControlPointTypeChanged(controlPointData, ActiveProjector.Projector.ControlPoints,
                ActiveProjector.Projector.InterpolateControlPoints, ActiveProjector.ProjectorControl.UpdateControlPoints);
        }

        private void OnBlacklevelControlPointTypeChanged(ControlPointsControl.ControlPoint controlPointData)
        {
            OnControlPointTypeChanged(controlPointData, ActiveProjector.Projector.BlacklevelControlPoints,
                ActiveProjector.Projector.InterpolateBlacklevelControlPoints, ActiveProjector.BlacklevelControl.UpdateControlPoints);
        }

        private void OnBlacklevel2ControlPointTypeChanged(ControlPointsControl.ControlPoint controlPointData)
        {
            OnControlPointTypeChanged(controlPointData, ActiveProjector.Projector.Blacklevel2ControlPoints,
                ActiveProjector.Projector.InterpolateBlacklevel2ControlPoints, ActiveProjector.Blacklevel2Control.UpdateControlPoints);
        }

        private void OnBlendRegionControlPointTypeChanged(ControlPointsControl.ControlPoint controlPointData)
        {
            OnControlPointTypeChanged(controlPointData, ActiveProjector.Projector.BlendRegionControlPoints,
                ActiveProjector.Projector.InterpolateBlendRegionControlPoints, ActiveProjector.BlendRegionControl.UpdateControlPoints);
        }

        private void OnControlPointTypeChanged(ControlPointsControl.ControlPoint controlPointData, List<ControlPoint> controlPoints, Action interpolate, Action<ControlPointsControl.ControlPoint[]> updateControlPoints)
        {
            var cp = controlPoints.First(p => p.U == controlPointData.U && p.V == controlPointData.V);
            cp.ControlPointType = ConvertControlPointType(controlPointData.ControlPointType);
            if (cp.ControlPointType == ControlPointType.Default)
            {
                interpolate();
                updateControlPoints(controlPoints.Select(ConvertControlPoint).ToArray());
            }
        }

        private void OnControlPointMoved(ControlPointsControl.ControlPoint controlPointData)
        {
            OnControlPointMoved(controlPointData, ActiveProjector.Projector.ControlPoints, 
                ActiveProjector.Projector.InterpolateControlPoints, ActiveProjector.ProjectorControl.UpdateControlPoints);
        }

        private void OnBlacklevelControlPointMoved(ControlPointsControl.ControlPoint controlPointData)
        {
            OnControlPointMoved(controlPointData, ActiveProjector.Projector.BlacklevelControlPoints, 
                ActiveProjector.Projector.InterpolateBlacklevelControlPoints, ActiveProjector.BlacklevelControl.UpdateControlPoints);
        }

        private void OnBlacklevel2ControlPointMoved(ControlPointsControl.ControlPoint controlPointData)
        {
            OnControlPointMoved(controlPointData, ActiveProjector.Projector.Blacklevel2ControlPoints,
                ActiveProjector.Projector.InterpolateBlacklevel2ControlPoints, ActiveProjector.Blacklevel2Control.UpdateControlPoints);
        }

        private void OnBlendRegionControlPointMoved(ControlPointsControl.ControlPoint controlPointData)
        {
            OnControlPointMoved(controlPointData, ActiveProjector.Projector.BlendRegionControlPoints,
                ActiveProjector.Projector.InterpolateBlendRegionControlPoints, ActiveProjector.BlendRegionControl.UpdateControlPoints);
        }

        private void OnControlPointMoved(ControlPointsControl.ControlPoint controlPointData, List<ControlPoint> controlPoints, Action interpolate, Action<ControlPointsControl.ControlPoint[]> updateControlPoints)
        {
            UpdateWarpInfo(controlPointData);
            var cp = controlPoints.First(p => p.U == controlPointData.U && p.V == controlPointData.V);
            cp.X = controlPointData.X;
            cp.Y = controlPointData.Y;
            if (controlPointData.ControlPointType == ControlPointsControl.ControlPointType.IsEcke)
            {
                interpolate();
                updateControlPoints(controlPoints.Select(ConvertControlPoint).ToArray());
            }
        }

        private void UpdateWarpInfo(ControlPointsControl.ControlPoint controlPointData)
        {
            if (ActiveProjector == null)
            {
                WarpInfo.Visibility = Visibility.Hidden;
                return;
            }
            WarpInfo.Visibility = Visibility.Visible;
            WarpInfo.Update(controlPointData);

            var distanceFromProjector = 50;
            if (ActiveProjector == _projectors[0])
            {
                Canvas.SetLeft(WarpInfo, ActiveProjector.Projector.Resolution.Width + distanceFromProjector);
            }
            else
            {
                Canvas.SetLeft(WarpInfo, ActiveProjector.Projector.Resolution.Width - ActiveProjector.Projector.Overlap - distanceFromProjector - WarpInfo.Width);
            }
        }

        private ProjectorContainer ActiveProjector
        {
            get
            {
                foreach (var p in _projectors)
                {
                    if (p.ProjectorControl.IsActive) return p;
                }
                return null;
            }
        }

        public void SetActiveProjector(int projector)
        {
            for (var i = 0; i < _projectors.Length; i++)
            {
                if (i == projector)
                {
                    _projectors[i].ProjectorControl.Activate();
                    Panel.SetZIndex(_projectors[i].ProjectorControl, 10);
                    _projectors[i].BlacklevelControl.Activate();
                    Panel.SetZIndex(_projectors[i].BlacklevelControl, 11);
                    _projectors[i].Blacklevel2Control.Activate();
                    Panel.SetZIndex(_projectors[i].Blacklevel2Control, 12);
                    _projectors[i].BlendRegionControl.Activate();
                    Panel.SetZIndex(_projectors[i].BlendRegionControl, 13);
                }
                else
                {
                    _projectors[i].ProjectorControl.DeActivate();
                    Panel.SetZIndex(_projectors[i].ProjectorControl, 1);
                    _projectors[i].BlacklevelControl.DeActivate();
                    Panel.SetZIndex(_projectors[i].BlacklevelControl, 2);
                    _projectors[i].Blacklevel2Control.DeActivate();
                    Panel.SetZIndex(_projectors[i].Blacklevel2Control, 3);
                    _projectors[i].BlendRegionControl.DeActivate();
                    Panel.SetZIndex(_projectors[i].BlendRegionControl, 4);
                }
            }
            UpdateWarpInfo(null);
        }

        public void DeactivateProjectors()
        {
            foreach(var p in _projectors)
            {
                p.ProjectorControl.DeActivate();
                p.BlacklevelControl.DeActivate();
                p.Blacklevel2Control.DeActivate();
                p.BlendRegionControl.DeActivate();
            }
            UpdateWarpInfo(null);
        }

        public void SetVisibility(ControlPointsMode controlPointsMode, bool wireframeVisible)
        {
            foreach (var p in _projectors)
            {
                p.ProjectorControl.SetVisibility(controlPointsMode == ControlPointsMode.Calibration, wireframeVisible && controlPointsMode == ControlPointsMode.Calibration);
                p.BlacklevelControl.SetVisibility(controlPointsMode == ControlPointsMode.Blacklevel, wireframeVisible && controlPointsMode == ControlPointsMode.Blacklevel);
                p.Blacklevel2Control.SetVisibility(controlPointsMode == ControlPointsMode.Blacklevel2, wireframeVisible && controlPointsMode == ControlPointsMode.Blacklevel2);
                p.BlendRegionControl.SetVisibility(controlPointsMode == ControlPointsMode.Blendregion, wireframeVisible && controlPointsMode == ControlPointsMode.Blendregion);
            }
        }

        public void UpdateWarpControl(ControlPointsMode controlPointsMode)
        {
            foreach (var p in _projectors)
            {
                p.ProjectorControl.Wireframe = p.Projector.TriangleStrip;
                p.ProjectorControl.ResetControlPoints(p.Projector.ControlPoints.Select(ConvertControlPoint).ToArray(), controlPointsMode == ControlPointsMode.Calibration);
                p.BlacklevelControl.Wireframe = p.Projector.BlacklevelIndexes;
                p.BlacklevelControl.ResetControlPoints(p.Projector.BlacklevelControlPoints.Select(ConvertControlPoint).ToArray(), controlPointsMode == ControlPointsMode.Blacklevel);
                p.Blacklevel2Control.Wireframe = p.Projector.Blacklevel2Indexes;
                p.Blacklevel2Control.ResetControlPoints(p.Projector.Blacklevel2ControlPoints.Select(ConvertControlPoint).ToArray(), controlPointsMode == ControlPointsMode.Blacklevel2);
                p.BlendRegionControl.Wireframe = p.Projector.BlendRegionIndexes;
                p.BlendRegionControl.ResetControlPoints(p.Projector.BlendRegionControlPoints.Select(ConvertControlPoint).ToArray(), controlPointsMode == ControlPointsMode.Blendregion);
            }
        }

        private ControlPointsControl.ControlPoint ConvertControlPoint(ControlPoint controlPoint)
        {
            return new ControlPointsControl.ControlPoint(controlPoint.X, controlPoint.Y, controlPoint.U, controlPoint.V,
                ConvertControlPointType(controlPoint.ControlPointType), controlPoint.ControlPointDirections);
        }

        private ControlPointType ConvertControlPointType(ControlPointsControl.ControlPointType controlPointType)
        {
            if (controlPointType == ControlPointsControl.ControlPointType.Default)
            {
                return ControlPointType.Default;
            }
            if (controlPointType == ControlPointsControl.ControlPointType.IsEcke)
            {
                return ControlPointType.IsEcke;
            }
            if (controlPointType == ControlPointsControl.ControlPointType.IsFix)
            {
                return ControlPointType.IsFix;
            }
            throw new Exception($"Unknown ControlPointType {controlPointType}");
        }

        private ControlPointsControl.ControlPointType ConvertControlPointType(ControlPointType controlPointType)
        {
            if (controlPointType == ControlPointType.Default)
            {
                return ControlPointsControl.ControlPointType.Default;
            }
            if (controlPointType == ControlPointType.IsEcke)
            {
                return ControlPointsControl.ControlPointType.IsEcke;
            }
            if (controlPointType == ControlPointType.IsFix)
            {
                return ControlPointsControl.ControlPointType.IsFix;
            }
            throw new Exception($"Unknown ControlPointType {controlPointType}");
        }

        public void KeyPressed(KeyEventArgs e, bool shift)
        {
            ActiveProjector?.ProjectorControl.KeyPressed(e, shift);
            ActiveProjector?.BlacklevelControl.KeyPressed(e, shift);
            ActiveProjector?.Blacklevel2Control.KeyPressed(e, shift);
            ActiveProjector?.BlendRegionControl.KeyPressed(e, shift);
        }
    }
}
