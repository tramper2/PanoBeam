using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Accord.Imaging;
using AForge;
using AForge.Math.Geometry;
using PanoBeam.Common;
using PanoBeamLib.Blend;
using PanoBeamLib.Delegates;
using Image = System.Drawing.Image;
using math = Accord.Math;

namespace PanoBeamLib
{
    public class Projector
    {
        public List<ControlPoint> ControlPoints { get; }
        public List<ControlPoint> BoundingControlPoints { get; }
        public List<ControlPoint> BlacklevelControlPoints { get; }
        public List<ControlPoint> Blacklevel2ControlPoints { get; }
        public List<ControlPoint> BlendRegionControlPoints { get; }

        private readonly int _index;
        private readonly int _overlap;
        private Size _resolution;
        private int _patternSize;
        private Size _patternCount;
        private float[] _pattern;
        public uint DisplayId { get; }

        private Bitmap _bmpWhite;
        private Bitmap _bmpPattern;
        private IntPoint[] _fullSurface;

        public string Title { get; }

        public int Index => _index;
        public int Overlap => _overlap;
        public Curve BlendCurve { get; }
        public double MaxBlend { get; set; }
        public double BlacklevelOffset { get; set; }
        public double Blacklevel2Offset { get; set; }

        private PointF[] _detectedCorner;

        internal event ProgressDelegate DetectProgress;
        private event ProgressDelegate DetectShapesProgress;

        internal Projector(int index, uint displayId, string title, Size resolution, int overlap)
        {
            _index = index;
            Title = title;
            _resolution = resolution;
            _overlap = overlap;
            DisplayId = displayId;
            ControlPoints = new List<ControlPoint>();
            BoundingControlPoints = new List<ControlPoint>();
            BlacklevelControlPoints = new List<ControlPoint>();
            Blacklevel2ControlPoints = new List<ControlPoint>();
            BlendRegionControlPoints = new List<ControlPoint>();
            InitBlacklevel();
            InitBlacklevel2();
            InitBlendRegion();
            BlendCurve = new Curve();
            MakeTriangleStrip();
            DetectShapesProgress += OnDetectShapesProgress;
        }

        private float _progressFactor;
        private float _progressOffset;
        private void OnDetectShapesProgress(float progress)
        {
            DetectProgress?.Invoke(progress * _progressFactor + _progressOffset);
        }

        public int[] BlacklevelIndexes { get; private set; }
        private void InitBlacklevel()
        {
            BlacklevelControlPoints.Clear();
            Enumerable.Range(0, 7).ToList().ForEach(p => BlacklevelControlPoints.Add(new ControlPoint()));
            BlacklevelControlPoints[0].X = BlacklevelControlPoints[3].X = _resolution.Width - _overlap;
            BlacklevelControlPoints[1].X = BlacklevelControlPoints[2].X = _resolution.Width;
            BlacklevelControlPoints[2].Y = BlacklevelControlPoints[3].Y = _resolution.Height;
            var dy = _resolution.Height / 4;
            for (var i = 0; i < 3; i++)
            {
                BlacklevelControlPoints[4 + i].X = BlacklevelControlPoints[0].X;
                BlacklevelControlPoints[4 + i].Y = _resolution.Height - (i + 1) * dy;
            }
            BlacklevelControlPoints[0].ControlPointType = ControlPointType.IsEcke;
            BlacklevelControlPoints[1].ControlPointType = ControlPointType.IsEcke;
            BlacklevelControlPoints[2].ControlPointType = ControlPointType.IsEcke;
            BlacklevelControlPoints[3].ControlPointType = ControlPointType.IsEcke;

            foreach (var cp in BlacklevelControlPoints)
            {
                cp.U = cp.X;
                cp.V = cp.Y;
                cp.AllowAllDirections();
            }
            if (_index == 1)
            {
                foreach (var cp in BlacklevelControlPoints)
                {
                    cp.X = cp.U = Resolution.Width - cp.X;
                }
            }
            BlacklevelIndexes = Enumerable.Range(0, BlacklevelControlPoints.Count).ToArray();
        }

        public int[] Blacklevel2Indexes { get; private set; }
        private void InitBlacklevel2()
        {
            Blacklevel2ControlPoints.Clear();
            Enumerable.Range(0, 10).ToList().ForEach(p => Blacklevel2ControlPoints.Add(new ControlPoint()));
            Blacklevel2ControlPoints[0].X = _resolution.Width - _overlap - _overlap / 4;
            for (var i = 6; i <= 9; i++)
            {
                Blacklevel2ControlPoints[i].X = Blacklevel2ControlPoints[0].X;
            }
            for (var i = 1; i <= 5; i++)
            {
                Blacklevel2ControlPoints[i].X = _resolution.Width;
            }

            var dy = _resolution.Height / 4;
            for (var i = 2; i <= 5; i++)
            {
                Blacklevel2ControlPoints[i].Y = (i - 1) * dy;
                Blacklevel2ControlPoints[11 - i].Y = Blacklevel2ControlPoints[i].Y;
            }
            Blacklevel2ControlPoints[0].ControlPointType = ControlPointType.IsEcke;
            Blacklevel2ControlPoints[1].ControlPointType = ControlPointType.IsEcke;
            Blacklevel2ControlPoints[5].ControlPointType = ControlPointType.IsEcke;
            Blacklevel2ControlPoints[6].ControlPointType = ControlPointType.IsEcke;

            foreach (var cp in Blacklevel2ControlPoints)
            {
                cp.U = cp.X;
                cp.V = cp.Y;
                cp.AllowAllDirections();
            }
            if (_index == 1)
            {
                foreach (var cp in Blacklevel2ControlPoints)
                {
                    cp.X = cp.U = Resolution.Width - cp.X;
                }
            }
            Blacklevel2Indexes = Enumerable.Range(0, Blacklevel2ControlPoints.Count).ToArray();
        }

        public int[] BlendRegionIndexes { get; private set; }
        private void InitBlendRegion()
        {
            BlendRegionControlPoints.Clear();
            Enumerable.Range(0, 10).ToList().ForEach(p => BlendRegionControlPoints.Add(new ControlPoint()));
            BlendRegionControlPoints[0].X = _resolution.Width - _overlap;
            for (var i = 6; i <= 9; i++)
            {
                BlendRegionControlPoints[i].X = BlendRegionControlPoints[0].X;
            }
            for (var i = 1; i <= 5; i++)
            {
                BlendRegionControlPoints[i].X = _resolution.Width;
            }

            var dy = _resolution.Height / 4;
            for (var i = 2; i <= 5; i++)
            {
                BlendRegionControlPoints[i].Y = (i - 1) * dy;
                BlendRegionControlPoints[11 - i].Y = BlendRegionControlPoints[i].Y;
            }
            BlendRegionControlPoints[0].ControlPointType = ControlPointType.IsEcke;
            BlendRegionControlPoints[1].ControlPointType = ControlPointType.IsEcke;
            BlendRegionControlPoints[5].ControlPointType = ControlPointType.IsEcke;
            BlendRegionControlPoints[6].ControlPointType = ControlPointType.IsEcke;

            foreach (var cp in BlendRegionControlPoints)
            {
                cp.U = cp.X;
                cp.V = cp.Y;
            }
            if (_index == 1)
            {
                foreach (var cp in BlendRegionControlPoints)
                {
                    cp.X = cp.U = Resolution.Width - cp.X;
                }
            }
            BlendRegionIndexes = Enumerable.Range(0, BlendRegionControlPoints.Count).ToArray();
        }

        public ProjectorData GetProjectorData()
        {
            return new ProjectorData
            {
                BlendData = new BlendSettings
                {
                    MaxBlend = MaxBlend,
                    BlacklevelOffset = BlacklevelOffset,
                    Blacklevel2Offset = Blacklevel2Offset,
                    CurvePoints = BlendCurve.GetCurvePoints()
                },
                ControlPoints = ControlPoints.ToArray(),
                BlendRegionControlPoints = BlendRegionControlPoints.ToArray(),
                BlacklevelControlPoints = BlacklevelControlPoints.ToArray(),
                Blacklevel2ControlPoints = Blacklevel2ControlPoints.ToArray()
            };
        }

        public void UpdateFromProjectorData(ProjectorData projectorData)
        {
            MaxBlend = projectorData.BlendData.MaxBlend;
            BlacklevelOffset = projectorData.BlendData.BlacklevelOffset;
            Blacklevel2Offset = projectorData.BlendData.Blacklevel2Offset;
            BlendCurve.InitFromConfig(projectorData.BlendData.CurvePoints);
            var controlPoints = projectorData.ControlPoints;
            if (controlPoints != null)
            {
                for (var i = 0; i < ControlPoints.Count; i++)
                {
                    ControlPoints[i].X = controlPoints[i].X;
                    ControlPoints[i].Y = controlPoints[i].Y;
                    if (controlPoints[i].ControlPointType == ControlPointType.IsFix)
                    {
                        ControlPoints[i].ControlPointType = ControlPointType.IsFix;
                    }
                }
            }
            var blacklevelControlPoints = projectorData.BlacklevelControlPoints;
            if (blacklevelControlPoints != null)
            {
                for (var i = 0; i < BlacklevelControlPoints.Count; i++)
                {
                    BlacklevelControlPoints[i].X = blacklevelControlPoints[i].X;
                    BlacklevelControlPoints[i].Y = blacklevelControlPoints[i].Y;
                }
            }

            var blacklevel2ControlPoints = projectorData.Blacklevel2ControlPoints;
            if (blacklevel2ControlPoints != null)
            {
                for (var i = 0; i < Blacklevel2ControlPoints.Count; i++)
                {
                    Blacklevel2ControlPoints[i].X = blacklevel2ControlPoints[i].X;
                    Blacklevel2ControlPoints[i].Y = blacklevel2ControlPoints[i].Y;
                }
            }

            var blendRegionControlPoints = projectorData.BlendRegionControlPoints;
            if (blendRegionControlPoints != null)
            {
                for (var i = 0; i < BlendRegionControlPoints.Count; i++)
                {
                    BlendRegionControlPoints[i].X = blendRegionControlPoints[i].X;
                    BlendRegionControlPoints[i].Y = blendRegionControlPoints[i].Y;
                }
            }
        }

        //public void InitFromConfig(ProjectorData projector, int patternSize, Size patternCount, bool controlPointsInsideOverlap)
        //{
        //    MaxBlend = projector.BlendData.MaxBlend;
        //    BlacklevelOffset = projector.BlendData.BlacklevelOffset;
        //    Blacklevel2Offset = projector.BlendData.Blacklevel2Offset;
        //    BlendCurve.InitFromConfig(projector.BlendData.CurvePoints);
        //    //var controlPoints = Mapper.MapControlPoints(projector.ControlPoints);
        //    for (var i = 0; i < ControlPoints.Count; i++)
        //    {
        //        ControlPoints[i].X = controlPoints[i].X;
        //        ControlPoints[i].Y = controlPoints[i].Y;
        //        if (controlPoints[i].ControlPointType == ControlPointType.IsFix)
        //        {
        //            ControlPoints[i].ControlPointType = ControlPointType.IsFix;
        //        }
        //    }
        //    var blacklevelControlPoints = Mapper.MapControlPoints(projector.BlacklevelControlPoints);
        //    if (blacklevelControlPoints != null)
        //    {
        //        for (var i = 0; i < BlacklevelControlPoints.Count; i++)
        //        {
        //            BlacklevelControlPoints[i].X = blacklevelControlPoints[i].X;
        //            BlacklevelControlPoints[i].Y = blacklevelControlPoints[i].Y;
        //        }
        //    }

        //    var blacklevel2ControlPoints = Mapper.MapControlPoints(projector.Blacklevel2ControlPoints);
        //    if (blacklevel2ControlPoints != null)
        //    {
        //        for (var i = 0; i < Blacklevel2ControlPoints.Count; i++)
        //        {
        //            Blacklevel2ControlPoints[i].X = blacklevel2ControlPoints[i].X;
        //            Blacklevel2ControlPoints[i].Y = blacklevel2ControlPoints[i].Y;
        //        }
        //    }

        //    var blendRegionControlPoints = Mapper.MapControlPoints(projector.BlendRegionControlPoints);
        //    if (blendRegionControlPoints != null)
        //    {
        //        for (var i = 0; i < BlendRegionControlPoints.Count; i++)
        //        {
        //            BlendRegionControlPoints[i].X = blendRegionControlPoints[i].X;
        //            BlendRegionControlPoints[i].Y = blendRegionControlPoints[i].Y;
        //        }
        //    }
        //}

        public int[] TriangleStrip { get; private set; }
        private void MakeTriangleStrip()
        {
            var pointsCountX = _patternCount.Width;
            var pointsCountY = _patternCount.Height;
            var triangleStrip = new List<int>();
            for (var y = 0; y < pointsCountY-1; y++)
            {
                var o = pointsCountX * y;
                if (y%2 == 0)
                {
                    for (var x = 0; x < pointsCountX; x++)
                    {
                        triangleStrip.Add(x + o);
                        triangleStrip.Add(x + o + pointsCountX);
                    }
                }
                else
                {
                    for (var x = pointsCountX - 1; x >= 0; x--)
                    {
                        triangleStrip.Add(x + o);
                        triangleStrip.Add(x + o + pointsCountX);
                    }
                }
            }

            TriangleStrip = triangleStrip.ToArray();
        }
        
        public Rectangle ClippingRectangle { get; set; }
        public Size Resolution => _resolution;

        public void LoadImages(string imagesPath)
        {
            _bmpWhite = (Bitmap)Image.FromFile(Path.Combine(imagesPath, $"capture_white{_index}.png"));
            _bmpPattern = (Bitmap)Image.FromFile(Path.Combine(imagesPath, $"capture_pattern{_index}.png"));
        }

        public void SetPatternType(int size, Size count, int[] xList, bool keepCorners)
        {
            _patternSize = size;
            _patternCount = count;

            var eckenVorher = ControlPoints.Where(c => c.ControlPointType == ControlPointType.IsEcke).ToArray();
            ControlPoints.Clear();
            BoundingControlPoints.Clear();
            Enumerable.Range(0, count.Width * count.Height).ToList().ForEach(p => ControlPoints.Add(new ControlPoint()));

            var dy = _resolution.Height / (count.Height - 1);
            var y = 0;
            for (var i = 0; i < ControlPoints.Count; i++)
            {
                if (i > 0 && i % count.Width == 0)
                {
                    y += dy;
                    if (y >= _resolution.Height) y = _resolution.Height - 1;
                }
                ControlPoints[i].Y = ControlPoints[i].V = y;
            }

            for (var iy = 0; iy < count.Height; iy++)
            {
                for (var ix = 0; ix < count.Width; ix++)
                {
                    var index = iy*count.Width + ix;
                    ControlPoints[index].X = ControlPoints[index].U = xList[ix];
                }
            }

            BoundingControlPoints.AddRange(ControlPoints.Take(count.Width));
            for (var iy = 1; iy < count.Height; iy++)
            {
                BoundingControlPoints.Add(ControlPoints[iy * count.Width + (count.Width - 1)]);
            }
            for (var ix = 1; ix < count.Width - 1; ix++)
            {
                BoundingControlPoints.Add(ControlPoints[count.Height * count.Width - ix - 1]);
            }
            for (var iy = count.Height - 1; iy > 0; iy--)
            {
                BoundingControlPoints.Add(ControlPoints[iy * count.Width]);
            }

            ControlPoints[0].ControlPointType = ControlPointType.IsEcke;
            ControlPoints[count.Width - 1].ControlPointType = ControlPointType.IsEcke;
            ControlPoints[count.Width * (count.Height - 1)].ControlPointType = ControlPointType.IsEcke;
            ControlPoints[count.Width * count.Height - 1].ControlPointType = ControlPointType.IsEcke;

            foreach (var cp in BlacklevelControlPoints)
            {
                cp.X = cp.U;
                cp.Y = cp.V;
            }

            if (keepCorners && eckenVorher.Any())
            {
                var ecken = ControlPoints.Where(c => c.ControlPointType == ControlPointType.IsEcke).ToArray();
                for (var i = 0; i < eckenVorher.Length; i++)
                {
                    ecken[i].X = eckenVorher[i].X;
                    ecken[i].Y = eckenVorher[i].Y;
                }
            }
            Interpolate(ControlPoints);

            MakeTriangleStrip();
            DetermineControlPointDirections();
        }

        public void InterpolateControlPoints()
        {
            Interpolate(ControlPoints);
        }

        public void InterpolateBlacklevelControlPoints()
        {
            Interpolate(BlacklevelControlPoints);
        }

        public void InterpolateBlacklevel2ControlPoints()
        {
            Interpolate(Blacklevel2ControlPoints);
        }

        public void InterpolateBlendRegionControlPoints()
        {
            Interpolate(BlendRegionControlPoints);
        }

        private static void Interpolate(List<ControlPoint> controlPoints)
        {
            var ecken = controlPoints.Where(cp => cp.ControlPointType == ControlPointType.IsEcke).ToArray();
            // ReSharper disable once InconsistentNaming
            var eckenXYF = ecken.Select(p => new PointF(p.X, p.Y)).ToArray();
            // ReSharper disable once InconsistentNaming
            var eckenUVF = ecken.Select(p => new PointF(p.U, p.V)).ToArray();

            var transformationMatrix = Tools.Homography(eckenUVF, eckenXYF);

            var controlPointsArray = controlPoints.Where(cp => cp.ControlPointType == ControlPointType.Default).ToArray();
            var points = controlPointsArray.Select(p => new PointF(p.U, p.V)).ToArray();

            var transformed = transformationMatrix.TransformPoints(points);

            for (var i = 0; i < controlPointsArray.Length; i++)
            {
                controlPointsArray[i].X = (int)Math.Round(transformed[i].X, MidpointRounding.AwayFromZero);
                controlPointsArray[i].Y = (int)Math.Round(transformed[i].Y, MidpointRounding.AwayFromZero);
            }

        }

        public int GetNumVertices()
        {
            return TriangleStrip.Length;
        }

        public float[] GetVertices(Screen screen)
        {
            var xOffset = 0;
            if (_index == 1)
            {
                xOffset = _resolution.Width - _overlap;
            }
            xOffset += screen.Bounds.X;
            var yOffset = screen.Bounds.Y;
            var vertices = new List<float>();
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < TriangleStrip.Length; i++)
            {
                vertices.AddRange(new float[]
                {
                    ControlPoints[TriangleStrip[i]].X,
                    ControlPoints[TriangleStrip[i]].Y,
                    ControlPoints[TriangleStrip[i]].U + xOffset,
                    ControlPoints[TriangleStrip[i]].V + yOffset,
                    0,1
                });
            }

            return vertices.ToArray();
        }

        public float[] GetBlendData()
        {
            var maxBlend = (float)MaxBlend;
            var blend3 = new float[_resolution.Width * _resolution.Height * 3];
            Helpers.ArrayFill(blend3, maxBlend);

            var blendXList = new List<int>();
            var blendYList = new List<int>();
            foreach (var i in BlendRegionIndexes)
            {
                blendXList.Add(BlendRegionControlPoints[i].X);
                blendYList.Add(BlendRegionControlPoints[i].Y);
            }
            var blendx = blendXList.ToArray();
            var blendy = blendYList.ToArray();

            var bmp = new Bitmap(_resolution.Width, _resolution.Height);
            var g = Graphics.FromImage(bmp);
            var points = new System.Drawing.Point[blendx.Length];
            for (var i = 0; i < blendx.Length; i++)
            {
                points[i] = new System.Drawing.Point(blendx[i], blendy[i]);
            }
            g.FillPolygon(Brushes.Black, points, System.Drawing.Drawing2D.FillMode.Winding);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            var data = new byte[bmp.Height * bmpData.Stride];
            Marshal.Copy(bmpData.Scan0, data, 0, data.Length);
            bmp.UnlockBits(bmpData);
            var f = bmpData.Stride / bmp.Width;

            double ox = -1;
            int w = -1;
            for (int y = 0; y < _resolution.Height; y++)
            {
                var yf = y * _resolution.Width * f;
                for (int x = 0; x < _resolution.Width; x++)
                {
                    if(data[x * f + yf + 3] > 0)
                    {
                        if (w == -1)
                        {
                            w = CalcWidth(data, f, _resolution.Width, x, y);
                            ox = x;
                        }
                        var value = maxBlend;
                        if (w > 0)
                        {
                            var x2 = 1d / w * (x - ox);
                            if (Index == 1)
                            {
                                x2 = 1d - x2;
                            }
                            value = (float)(BlendCurve.GetY(x2) * maxBlend);
                        }
                        blend3[(x + y * _resolution.Width) * 3 + 0] = value;
                        blend3[(x + y * _resolution.Width) * 3 + 1] = value;
                        blend3[(x + y * _resolution.Width) * 3 + 2] = value;
                    }
                    else
                    {
                        w = -1;
                    }
                }
            }
            
            return blend3.ToArray();
        }

        private int CalcWidth(byte[] data, int f, int width, int testx, int testy)
        {
            int w = 0;
            var yf = testy*width*f;
            for (var x = testx; x < _resolution.Width ; x++)
            {
                if (data[x * f + yf + 3] > 0)
                {
                    w++;
                }
                else
                {
                    break;
                }
            }
            return w;
        }

        public float[] Pattern => _pattern;

        public void SavePattern(string filename)
        {
            _pattern.SaveAsImage(_resolution, filename);
        }

        public void InitializePatternShapes()
        {
            foreach (var cp in ControlPoints)
            {
                var w = _patternSize;
                var h = _patternSize;
                int x, y;
                if (!cp.ControlPointDirections.HasFlag(ControlPointDirections.Up)) // erste Zeile
                {
                    y = cp.Y;
                    h = (int) Math.Round(_patternSize/2f, MidpointRounding.AwayFromZero);
                }
                else if (!cp.ControlPointDirections.HasFlag(ControlPointDirections.Down)) // letzte Zeile
                {
                    h = (int) Math.Round(_patternSize/2f, MidpointRounding.AwayFromZero);
                    y = cp.Y - h + 1;
                }
                else
                {
                    y = cp.Y - (int) Math.Round(_patternSize/2f, MidpointRounding.AwayFromZero);
                }

                if (!cp.ControlPointDirections.HasFlag(ControlPointDirections.Left)) // erste Spalte
                {
                    x = cp.X;
                    w = (int) Math.Round(_patternSize/2f, MidpointRounding.AwayFromZero);
                }
                else if (!cp.ControlPointDirections.HasFlag(ControlPointDirections.Right)) // letzte Spalte
                {
                    w = (int) Math.Round(_patternSize/2f, MidpointRounding.AwayFromZero);
                    x = cp.X - w + 1;
                }
                else
                {
                    x = cp.X - (int) Math.Round(_patternSize/2f, MidpointRounding.AwayFromZero);
                }

                cp.PatternShape = new PatternShape(x, y, w, h);
            }
        }

        internal void GeneratePattern()
        {
            var data = new float[_resolution.Width * _resolution.Height * 3];
            Helpers.ArrayFill(data, 0);

            foreach (var cp in ControlPoints)
            {
                for (var h = 0; h < cp.PatternShape.H; h++)
                {
                    for (var w = 0; w < cp.PatternShape.W; w++)
                    {
                        var x = cp.PatternShape.X + w;
                        if(x < 0 || x >= _resolution.Width) continue;
                        var y = cp.PatternShape.Y + h;
                        if (y < 0 || y >= _resolution.Height) continue;
                        data[(x + y * _resolution.Width) * 3 + 0] = 1f;
                        data[(x + y * _resolution.Width) * 3 + 1] = 1f;
                        data[(x + y * _resolution.Width) * 3 + 2] = 1f;
                    }
                }
            }
            _pattern = data;
        }

        private void DetermineControlPointDirections()
        {
            var firstRow = ControlPoints.Where(c => c.V == 0).ToArray();

            var firstCol = ControlPoints.Where(c => c.U == 0).ToArray();

            ControlPoints.ToList().ForEach(c => c.AllowAllDirections());
            firstRow.ToList().ForEach(c => c.ControlPointDirections &= ~ControlPointDirections.Up);
            firstCol.ToList().ForEach(c => c.ControlPointDirections &= ~ControlPointDirections.Left);
            ControlPoints.Where(c => c.V == _resolution.Height - 1).ToList().ForEach(c => c.ControlPointDirections &= ~ControlPointDirections.Down);
            ControlPoints.Where(c => c.U == _resolution.Width - 1).ToList().ForEach(c => c.ControlPointDirections &= ~ControlPointDirections.Right);
        }

        public void SetFullSurface(IntPoint[] surface)
        {
            _fullSurface = surface;
        }

        public ControlPoint FindControlPoint(int u, int v)
        {
            return ControlPoints.FirstOrDefault(p => p.U == u && p.V == v);
        }

        public PointF[] DetectedCorners => _detectedCorner;

        public void Detect()
        {
            _progressFactor = 0.5f;
            _progressOffset = 0f;
            Helpers.FillOutsideBlack(_bmpWhite, _fullSurface);

            var corners = Recognition.DetectSurface(_bmpWhite);
            if (corners == null)
            {
                throw new Exception("Corner detection failed.");
            }
            corners = Calculations.SortCorners(corners);
            Helpers.SaveImageWithMarkers(_bmpWhite, corners, Path.Combine(Helpers.TempDir, $"detect_white{Index}.png"), 5);

            Helpers.FillOutsideBlack(_bmpPattern, corners);

            _progressOffset = 0.5f;
            _detectedCorner = corners.Select(p => new PointF(p.X, p.Y)).ToArray();
            var eckenSoll = new[] {
                new PointF(0, 0),
                new PointF(_resolution.Width - 1, 0),
                new PointF(_resolution.Width - 1, _resolution.Height - 1),
                new PointF(0, _resolution.Height - 1)};
            var transformationMatrix = Tools.Homography(_detectedCorner, eckenSoll);

            var shapes = DetectShapes(ControlPoints.Count, _bmpPattern, 5, _patternSize);
            if (shapes.Count != ControlPoints.Count)
            {
                throw new Exception("Not all shapes detected");
            }
            var pointsF = shapes.Select(s => new PointF(s.Blob.CenterOfGravity.X, s.Blob.CenterOfGravity.Y)).ToArray();
            //var pointsF = points.Select(p => new PointF(p.X, p.Y)).ToArray();
            Helpers.SaveImageWithMarkers(_bmpPattern, pointsF.Select(p => new IntPoint((int)p.X, (int)p.Y)).ToArray(), Path.Combine(Helpers.TempDir, $"detect_pattern{Index}.png"), 2);
            var transformedPoints = transformationMatrix.TransformPoints(pointsF);
            for (var i = 0; i < shapes.Count; i++)
            {
                shapes[i].TransformedPoint = transformedPoints[i];
            }

            MapDetectedPointsToControlPoints(shapes);
        }

        private void MapDetectedPointsToControlPoints(List<Shape> detectedShapes)
        {
            foreach (var controlPoint in ControlPoints)
            {
                var nearestShape = FindNearestShape(detectedShapes, controlPoint);
                controlPoint.DetectedShape = nearestShape;
            }
        }

        private List<Shape> DetectShapes(int count, Bitmap image, int minSize, int maxSize)
        {
            var blobCounter = new AForge.Imaging.BlobCounter();
            AForge.Imaging.Blob[] blobs;
            blobCounter.FilterBlobs = true;
            blobCounter.MaxHeight = maxSize;
            blobCounter.MaxWidth = maxSize;
            blobCounter.MinHeight = minSize;
            blobCounter.MinWidth = minSize;
            var threshold = Recognition.GetThreshold(image);
            
            blobCounter.BackgroundThreshold = Color.FromArgb(255, threshold, threshold, threshold);
            blobCounter.ProcessImage(image);
            blobs = blobCounter.GetObjectsInformation();

            if (blobs.Length == count)
            {
                DetectShapesProgress?.Invoke(1f);
            }
            else
            {
                threshold = GetOptimizedThreshold(count, image, minSize, maxSize);
                blobCounter.BackgroundThreshold = Color.FromArgb(255, threshold, threshold, threshold);
                blobCounter.ProcessImage(image);
                blobs = blobCounter.GetObjectsInformation();
            }

            var shapes = new List<Shape>();
            foreach (var blob in blobs)
            {
                var edgePoints = blobCounter.GetBlobsEdgePoints(blob);
                // FindQuadrilateralCorners:
                // The first point in the list is the point with lowest X coordinate 
                // (and with lowest Y if there are several points with the same X value). 
                // The corners are provided in counter clockwise order
                var corners = PointsCloud.FindQuadrilateralCorners(edgePoints);

                shapes.Add(new Shape(corners.ToArray(), blob));
            }

            return shapes;
        }

        private int GetOptimizedThreshold(int count, Bitmap image, int minSize, int maxSize)
        {
            var blobCounter = new AForge.Imaging.BlobCounter();
            AForge.Imaging.Blob[] blobs;
            int thresholdMax = 255;
            int thresholdMin = 0;
            blobCounter.FilterBlobs = true;
            blobCounter.MaxHeight = maxSize;
            blobCounter.MaxWidth = maxSize;
            blobCounter.MinHeight = minSize;
            blobCounter.MinWidth = minSize;
            for (var i = 255; i >= 0; i--)
            {
                if (i % 10 == 0)
                {
                    DetectShapesProgress?.Invoke(0.5f / 255f * (255 - i));
                }
                blobCounter.BackgroundThreshold = Color.FromArgb(255, i, i, i);
                blobCounter.ProcessImage(image);
                blobs = blobCounter.GetObjectsInformation();
                if (blobs.Length == count)
                {
                    thresholdMax = i;
                    break;
                }
            }
            DetectShapesProgress?.Invoke(0.5f);
            for (var i = 0; i <= 255; i++)
            {
                if (i % 10 == 0)
                {
                    DetectShapesProgress?.Invoke(0.5f + 0.5f / 255f * i);
                }
                blobCounter.BackgroundThreshold = Color.FromArgb(255, i, i, i);
                blobCounter.ProcessImage(image);
                blobs = blobCounter.GetObjectsInformation();
                if (blobs.Length == count)
                {
                    thresholdMin = i;
                    break;
                }
            }
            DetectShapesProgress?.Invoke(1f);
            var thr = (thresholdMax + thresholdMin) / 2;
            return thr;
        }

        private static Shape FindNearestShape(List<Shape> detectedShapes, ControlPoint controlPoint)
        {
            var d = double.MaxValue;
            Shape nearestShape = null;
            foreach (var s in detectedShapes)
            {
                var tmp = math.Distance.SquareEuclidean(controlPoint.U, s.TransformedPoint.X, controlPoint.V, s.TransformedPoint.Y);
                if (tmp < d)
                {
                    d = tmp;
                    nearestShape = s;
                }
            }
            return nearestShape;
        }
    }
}