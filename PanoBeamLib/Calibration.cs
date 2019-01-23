using System;
using System.Drawing;
using System.IO;
using System.Linq;
using PanoBeam.Common;
using PanoBeamLib.Delegates;
using tasks = System.Threading.Tasks;
using AForge;

namespace PanoBeamLib
{
    public class Calibration
    {
        private Bitmap _bmpWhite;
        private Projector[] _projectors;

        internal event ProgressDelegate DetectProgress;

        internal void Initialize(Size screenResolution, int overlap, Projector[] projectors)
        {
            _projectors = projectors;
            // TODO Marco: Kamera oder File
            string imagePath;
            // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
            if(Helpers.CameraCalibration)
            {
                imagePath = Helpers.TempDir;
            }
            else
            {
                imagePath = @"C:\source\PanoBeam\src\PanoBeam\Calibration\3x3";
            }
            tasks.Parallel.ForEach(_projectors, p => {
                p.LoadImages(imagePath);
            });
            
            _bmpWhite = (Bitmap)Image.FromFile(Path.Combine(imagePath, "capture_white.png"));

            _projectors[0].DetectProgress += OnDetectProgress0;
            _projectors[1].DetectProgress += OnDetectProgress1;
        }

        private float _progress0;
        private float _progress1;

        private void OnDetectProgress0(float progress)
        {
            _progress0 = progress;
            DetectProgress?.Invoke((_progress0 + _progress1)/2f);
        }

        private void OnDetectProgress1(float progress)
        {
            _progress1 = progress;
            DetectProgress?.Invoke((_progress0 + _progress1) / 2f);
        }

        public void Detect(Rectangle clippingRectangle, bool keepCorners)
        {
            var clippingRectangleCorners = new[]
            {
                new IntPoint(clippingRectangle.X, clippingRectangle.Y),
                new IntPoint(clippingRectangle.X + clippingRectangle.Width, clippingRectangle.Y),
                new IntPoint(clippingRectangle.X + clippingRectangle.Width, clippingRectangle.Y + clippingRectangle.Height),
                new IntPoint(clippingRectangle.X, clippingRectangle.Y + clippingRectangle.Height)
            };
            Helpers.FillOutsideBlack(_bmpWhite, clippingRectangleCorners);
            var corners = Recognition.DetectSurface(_bmpWhite);
            if(corners == null)
            {
                throw new Exception("Corner detection failed.");
            }
            corners = Calculations.SortCorners(corners);
            Helpers.SaveImageWithMarkers(_bmpWhite, corners, Path.Combine(Helpers.TempDir, "detect_white.png"), 5);

            tasks.Parallel.ForEach(_projectors, p => {
                p.ClippingRectangle = clippingRectangle;
                p.SetFullSurface(corners);
                p.Detect();
                foreach (var cp in p.ControlPoints.Where(
                        point => point.ControlPointType != ControlPointType.IsEcke && point.AssociatedPoint != null))
                {
                    cp.ControlPointType = ControlPointType.IsFix;
                }
            });

            var scaleX0 = _projectors[0].Resolution.Width/
                          ((_projectors[0].DetectedCorners[1].X - _projectors[0].DetectedCorners[0].X +
                            _projectors[0].DetectedCorners[2].X - _projectors[0].DetectedCorners[3].X)/2f);
            var scaleX1 = _projectors[1].Resolution.Width/
                          ((_projectors[1].DetectedCorners[1].X - _projectors[1].DetectedCorners[0].X +
                            _projectors[1].DetectedCorners[2].X - _projectors[1].DetectedCorners[3].X)/2f);
            var scaleY0 = _projectors[0].Resolution.Height/
                          ((_projectors[0].DetectedCorners[3].Y - _projectors[0].DetectedCorners[0].Y +
                            _projectors[0].DetectedCorners[2].Y - _projectors[0].DetectedCorners[1].Y)/2f);
            var scaleY1 = _projectors[1].Resolution.Height/
                          ((_projectors[1].DetectedCorners[3].Y - _projectors[1].DetectedCorners[0].Y +
                            _projectors[1].DetectedCorners[2].Y - _projectors[1].DetectedCorners[1].Y)/2f);

            var scaleX = (scaleX0 + scaleX1)/2f;
            var scaleY = (scaleY0 + scaleY1)/2f;
            CalculateAdjustments(scaleX, scaleY);
            if (!keepCorners)
            {
                CalibrateCorners(scaleX, scaleY);
            }
            // TODO Marco: müssen Kontrollpunkte in einer Linie ausgerichtet werden? Wie?
            CalculateBlackLevelRegion();

            tasks.Parallel.ForEach(_projectors, p => {
                p.InterpolateControlPoints();
                p.InterpolateBlacklevelControlPoints();
            });

            var bmp = new Bitmap(_bmpWhite.Height * 3, _bmpWhite.Height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                foreach (var cp in _projectors[0].ControlPoints)
                {
                    if (cp.AssociatedPoint == null) continue;
                    g.FillCircle(Brushes.Red, cp.X, cp.Y, 10);
                    g.FillCircle(Brushes.Orange, cp.X, cp.Y, 10);
                }
            }
            bmp.Save(Path.Combine(Helpers.TempDir, "calib.png"));
        }

        private void CalculateBlackLevelRegion()
        {
            var cp1 = _projectors[0].BlacklevelControlPoints.First();
            var cp2 = _projectors[1].ControlPoints.First();
            var dx = (cp2.X - cp2.U)*2;
            cp1.X -= dx;

            var cp3 = _projectors[0].BlacklevelControlPoints.Last(cp => cp.U == cp1.U && cp.ControlPointType == ControlPointType.IsEcke);
            var cp4 = _projectors[1].ControlPoints.Last(cp => cp.U == cp2.U);
            dx = (cp4.X - cp4.U)*2;
            cp3.X -= dx;

            var cp5 = _projectors[1].BlacklevelControlPoints.First();
            var cp6 = _projectors[0].ControlPoints.Last(cp => cp.V == cp1.V);
            dx = (cp6.U - cp6.X)*2;
            cp5.X += dx;

            var cp7 = _projectors[1].BlacklevelControlPoints.Last(cp => cp.U == cp5.U && cp.ControlPointType == ControlPointType.IsEcke);
            var cp8 = _projectors[0].ControlPoints.Last();
            dx = (cp8.U - cp8.X)*2;
            cp7.X += dx;
        }

        private void CalibrateCorners(float scaleX, float scaleY)
        {
            var topleft = _projectors[0].ControlPoints.First();
            var bottomleft = _projectors[0].ControlPoints.Last(cp => cp.U == topleft.U);

            AlignX(scaleX, topleft, bottomleft);
            var firstOverlapPoint = _projectors[0].ControlPoints.First(cp => cp.AssociatedPoint != null);
            var dy = (int)Math.Round(scaleY * (firstOverlapPoint.DetectedShape.Blob.CenterOfGravity.Y - topleft.DetectedShape.Blob.CenterOfGravity.Y), MidpointRounding.AwayFromZero);
            if (dy == 0)
            {
            }
            else if (dy > 0)
            {
                topleft.Y += dy;
            }
            else
            {
                var points = _projectors[0].ControlPoints.Where(cp => cp.AssociatedPoint != null && cp.V == topleft.U);
                foreach (var cp in points)
                {
                    cp.Y -= dy;
                    cp.AssociatedPoint.Y -= dy;
                }
            }

            firstOverlapPoint = _projectors[0].ControlPoints.First(cp => cp.AssociatedPoint != null && cp.V == bottomleft.V);
            dy = (int)Math.Round(scaleY * (firstOverlapPoint.DetectedShape.Blob.CenterOfGravity.Y - bottomleft.DetectedShape.Blob.CenterOfGravity.Y), MidpointRounding.AwayFromZero);
            if (dy == 0)
            {
            }
            else if (dy > 0)
            {
                var points = _projectors[0].ControlPoints.Where(cp => cp.AssociatedPoint != null && cp.V == bottomleft.U);
                foreach (var cp in points)
                {
                    cp.Y -= dy;
                    cp.AssociatedPoint.Y -= dy;
                }
            }
            else
            {
                bottomleft.Y += dy;
            }

            var topright = _projectors[1].ControlPoints.Last(cp => cp.ControlPointType == ControlPointType.IsEcke && cp.V == topleft.U);
            var bottomright = _projectors[1].ControlPoints.Last();

            AlignX(scaleX, topright, bottomright);
            var lastOverlapPoint = _projectors[1].ControlPoints.Last(cp => cp.AssociatedPoint != null && cp.V == topright.V);
            dy = (int)Math.Round(scaleY * (lastOverlapPoint.DetectedShape.Blob.CenterOfGravity.Y - topright.DetectedShape.Blob.CenterOfGravity.Y), MidpointRounding.AwayFromZero);
            if (dy == 0)
            {
            }
            else if (dy > 0)
            {
                topright.Y += dy;
            }
            else
            {
                var points = _projectors[1].ControlPoints.Where(cp => cp.AssociatedPoint != null && cp.V == topright.U);
                foreach (var cp in points)
                {
                    cp.Y -= dy;
                    cp.AssociatedPoint.Y -= dy;
                }
            }

            lastOverlapPoint = _projectors[1].ControlPoints.Last(cp => cp.AssociatedPoint != null && cp.V == bottomright.V);
            dy = (int)Math.Round(scaleY * (lastOverlapPoint.DetectedShape.Blob.CenterOfGravity.Y - bottomright.DetectedShape.Blob.CenterOfGravity.Y), MidpointRounding.AwayFromZero);
            if (dy == 0)
            {
            }
            else if (dy > 0)
            {
                var points = _projectors[1].ControlPoints.Where(cp => cp.AssociatedPoint != null && cp.V == bottomright.U);
                foreach (var cp in points)
                {
                    cp.Y -= dy;
                    cp.AssociatedPoint.Y -= dy;
                }
            }
            else
            {
                bottomright.Y += dy;
            }
        }

        private void AlignX(float scaleX, ControlPoint controlPoint1, ControlPoint controlPoint2)
        {
            var dx = scaleX * (controlPoint2.DetectedShape.Blob.CenterOfGravity.X - controlPoint1.DetectedShape.Blob.CenterOfGravity.X);
            var dx2 = (int)Math.Round(dx / 2f, MidpointRounding.AwayFromZero);
            // ReSharper disable once InconsistentNaming
            var dx2r = (int)Math.Round(dx - dx2, MidpointRounding.AwayFromZero);
            if (dx2 == 0)
            {
            }
            else if (dx2 > 0)
            {
                controlPoint1.X += dx2;
                controlPoint2.X -= dx2r;

                if (!controlPoint1.ControlPointDirections.HasFlag(ControlPointDirections.Right) && controlPoint1.X > controlPoint1.U)
                {
                    controlPoint2.X -= controlPoint1.X - controlPoint1.U;
                    controlPoint1.X = controlPoint1.U;
                }
                else if(!controlPoint2.ControlPointDirections.HasFlag(ControlPointDirections.Left) && controlPoint2.X < controlPoint2.U)
                {
                    controlPoint1.X += controlPoint2.U - controlPoint2.X;
                    controlPoint2.X = controlPoint2.U;
                }
            }
            else
            {
                controlPoint1.X += dx2;
                controlPoint2.X -= dx2r;

                if (!controlPoint1.ControlPointDirections.HasFlag(ControlPointDirections.Left) && controlPoint1.X < controlPoint1.U)
                {
                    controlPoint2.X += controlPoint1.U - controlPoint1.X;
                    controlPoint1.X = controlPoint1.U;
                }
                else if(!controlPoint2.ControlPointDirections.HasFlag(ControlPointDirections.Right) && controlPoint2.X > controlPoint2.U)
                {
                    controlPoint1.X -= controlPoint2.X - controlPoint2.U;
                    controlPoint2.X = controlPoint2.U;
                }
            }
        }

        private void AlignY(float scaleY, ControlPoint controlPoint1, ControlPoint controlPoint2)
        {
            var dy = scaleY * (controlPoint2.DetectedShape.Blob.CenterOfGravity.Y - controlPoint1.DetectedShape.Blob.CenterOfGravity.Y);
            var dy2 = (int)Math.Round(dy / 2f, MidpointRounding.AwayFromZero);
            // ReSharper disable once InconsistentNaming
            var dy2r = (int)Math.Round(dy - dy2, MidpointRounding.AwayFromZero);

            if (dy2 == 0)
            {
            }
            else if (dy2 > 0)
            {
                controlPoint1.Y += dy2;
                controlPoint2.Y -= dy2r;
                if (!controlPoint1.ControlPointDirections.HasFlag(ControlPointDirections.Down) && controlPoint1.Y > controlPoint1.V)
                {
                    controlPoint2.Y -= controlPoint1.Y - controlPoint1.V;
                    controlPoint1.Y = controlPoint1.V;
                }
                else if (!controlPoint2.ControlPointDirections.HasFlag(ControlPointDirections.Up) && controlPoint2.Y < controlPoint2.V)
                {
                    controlPoint1.Y += controlPoint2.Y - controlPoint2.V;
                    controlPoint2.Y = controlPoint2.V;
                }
            }
            else
            {
                controlPoint1.Y += dy2;
                controlPoint2.Y -= dy2r;

                if (!controlPoint1.ControlPointDirections.HasFlag(ControlPointDirections.Up) && controlPoint1.Y < controlPoint1.V)
                {
                    controlPoint2.Y -= controlPoint1.V - controlPoint1.Y;
                    controlPoint1.Y = controlPoint1.V;
                }
                else if(!controlPoint2.ControlPointDirections.HasFlag(ControlPointDirections.Down) && controlPoint2.Y > controlPoint2.V)
                {
                    controlPoint1.Y -= controlPoint2.Y - controlPoint2.V;
                    controlPoint2.Y = controlPoint2.V;
                }
            }
        }

        private void CalculateAdjustments(float scaleX, float scaleY)
        {
            foreach (var cp in _projectors[0].ControlPoints.Where(p => p.AssociatedPoint != null))
            {
                AlignX(scaleX, cp, cp.AssociatedPoint);
                AlignY(scaleY, cp, cp.AssociatedPoint);
            }
        }
    }
}