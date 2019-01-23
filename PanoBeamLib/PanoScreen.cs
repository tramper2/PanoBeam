using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using PanoBeam.Common;
using PanoBeamLib.Delegates;
using Size = System.Drawing.Size;

namespace PanoBeamLib
{
    public class PanoScreen
    {
        public Size Resolution { get; set; }

        public int Overlap { get; set; }

        public Rectangle ClippingRectangle { get; set; }

        public Action<Action, Action, CalibrationSteps[]> AwaitProjectorsReady;
        public Action<string> ShowImage;
        public Action AwaitCalculationsReady;
        public Action CalibrationDone;
        public Action CalibrationCanceled;
        public Action<string> CalibrationError;
        public event ProgressDelegate CalculationProgress;
        public Action SaveCursorPosition;
        public Action RestoreCursorPosition;

        private Projector[] _projectors;
        private Size _projectorResolution;
        private float[] _white;
        private float[] _black;

        private int _patternSize;
        private Size _patternCount;
        private bool _keepCorners;
        private bool _controlPointsInsideOverlap;

        public int PatternSize => _patternSize;
        public Size PatternCount => _patternCount;
        public bool KeepCorners => _keepCorners;

        public Projector[] Projectors => _projectors;

        public bool ControlPointsAdjusted { get
            {
                foreach(var p in _projectors)
                {
                    foreach(var cp in p.ControlPoints)
                    {
                        if (cp.X != cp.U) return true;
                        if (cp.Y != cp.V) return true;
                    }
                }
                return false;
            }
        }

        public static void Initialize()
        {
            Helpers.InitTempDir();
            if (!Helpers.IsDevComputer)
            {
                var errorCode = NvApi.Initialize();
                HandleNvApiError(errorCode);
            }
        }

        public static MosaicInfo GetMosaicInfo()
        {
            if (Helpers.IsDevComputer)
            {
                return new MosaicInfo
                {
                    Overlap = 600,
                    ProjectorHeight = 1080,
                    ProjectorWidth = 1920
                };
            }
            var errorCode = NvApi.GetMosaicInfo(out var mosaicInfo);
            HandleNvApiError(errorCode);
            return mosaicInfo;
        }

        private static void HandleNvApiError(int errorCode)
        {
            if (errorCode == NvApi.NVAPI_OK) return;
            NvApi.GetError(errorCode, out var error);
            throw new Exception(error.Message);
        }

        public void AddProjectors(uint projectorId0, uint projectorId1)
        {
            _projectorResolution = new Size((Resolution.Width + Overlap) / 2, Resolution.Height);
            _projectors = new[]
            {
                new Projector(0, projectorId0, "Linker Beamer", _projectorResolution, Overlap),
                new Projector(1, projectorId1, "Rechter Beamer", _projectorResolution, Overlap)
            };
        }

        public void Update(int patternSize, Size patternCount, bool keepCorners, bool controlPointsInsideOverlap)
        {
            _keepCorners = keepCorners;
            SetPattern(patternSize, patternCount, controlPointsInsideOverlap, true);
        }

        public bool SetPattern(int patternSize, Size patternCount, bool controlPointsInsideOverlap, bool force)
        {
            if (!force && _controlPointsInsideOverlap == controlPointsInsideOverlap && _patternSize == patternSize && _patternCount.Width == patternCount.Width && _patternCount.Height == patternCount.Height)
            {
                return false;
            }
            _patternSize = patternSize;
            _patternCount = patternCount;
            _controlPointsInsideOverlap = controlPointsInsideOverlap;
            RefreshPattern(!force);
            return true;
        }

        public void UpdateProjectorsFromConfig(ProjectorData[] projectorData)
        {
            for(var i = 0;i<projectorData.Length;i++)
            {
                _projectors[i].UpdateFromProjectorData(projectorData[i]);
            }
        }

        public ProjectorData[] GetProjectorsData()
        {
            var projectorsData = new List<ProjectorData>();
            foreach(var p in _projectors)
            {
                projectorsData.Add(p.GetProjectorData());
            }
            return projectorsData.ToArray();
        }

        public void RefreshPattern(bool keepCorners)
        {
            var xList0 = new List<int> {
                0,
                _projectorResolution.Width - 1,
                _projectorResolution.Width - Overlap};

            if (_controlPointsInsideOverlap)
            {
                var dx = Overlap/(_patternCount.Width - 2);
                var x = _projectorResolution.Width - Overlap + dx;
                for (var i = 0; i < _patternCount.Width - 3; i++)
                {
                    xList0.Add(x);
                    x += dx;
                }
            }
            else
            {
                var countInOverlap = (int) Math.Ceiling((_patternCount.Width - 3)/2d);
                var countOutside = _patternCount.Width - 3 - countInOverlap;
                if (countOutside < 0) countOutside = 0;
                var dxInOverlap = Overlap/(countInOverlap + 1);
                var dxOutside = (_projectorResolution.Width - Overlap)/(countOutside + 1);
                var x = dxOutside;
                for (var i = 0; i < countOutside; i++)
                {
                    xList0.Add(x);
                    x += dxOutside;
                }
                x = _projectorResolution.Width - Overlap + dxInOverlap;
                for (var i = 0; i < countInOverlap; i++)
                {
                    xList0.Add(x);
                    x += dxInOverlap;
                }
            }
            xList0.Sort();

            var xList1 = new List<int>();
            foreach (var x0 in xList0)
            {
                var x1 = x0 - (_projectorResolution.Width - Overlap);
                if (x1 < 0)
                {
                    x1 = -x1 + Overlap;
                    if (x1 >= _projectorResolution.Width)
                    {
                        x1 = _projectorResolution.Width - 1;
                    }
                }
                xList1.Add(x1);
            }
            xList1.Sort();

            _projectors[0].SetPatternType(_patternSize, _patternCount, xList0.ToArray(), keepCorners);
            _projectors[1].SetPatternType(_patternSize, _patternCount, xList1.ToArray(), keepCorners);

            AssociatePoints();
            _projectors[0].InitializePatternShapes();
            _projectors[1].InitializePatternShapes();
            MakeCongruentPatternShapes();            
        }

        private void MakeCongruentPatternShapes()
        {
            foreach (var cp in _projectors[0].ControlPoints.Where(p => p.AssociatedPoint != null))
            {
                if (cp.PatternShape.W > cp.AssociatedPoint.PatternShape.W)
                {
                    cp.PatternShape.ShrinkToWidth(cp.AssociatedPoint.PatternShape.W, 0);
                }
                else if (cp.PatternShape.W < cp.AssociatedPoint.PatternShape.W)
                {
                    // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                    if (cp.ControlPointDirections.HasFlag(ControlPointDirections.Right))
                    {
                        cp.AssociatedPoint.PatternShape.ShrinkToWidth(-cp.PatternShape.W, 0);
                    }
                    else
                    {
                        cp.AssociatedPoint.PatternShape.ShrinkToWidth(-cp.PatternShape.W, 1);
                    }
                }
            }
        }

        private void AssociatePoints()
        {
            foreach (var cp0 in _projectors[0].ControlPoints)
            {
                if (cp0.U < _projectors[0].Resolution.Width - Overlap) continue;
                var cp1 = _projectors[1].FindControlPoint(cp0.U - (_projectors[0].Resolution.Width - Overlap), cp0.V);
                if (cp1 == null)
                {
                    throw new Exception($"Associated Point not found {cp0.U}x{cp0.V}");
                }

                if (cp0.AssociatedPoint != null)
                {
                    throw new Exception($"Already associated {cp0.U}x{cp0.V}");
                }
                cp0.AssociatedPoint = cp1;
                if (cp1.AssociatedPoint != null)
                {
                    throw new Exception($"Already associated {cp0.U}x{cp0.V}");
                }
                cp1.AssociatedPoint = cp0;
            }
        }

        public void Calibrate(bool initPattern)
        {
            if (initPattern)
            {
                SetPattern(_patternSize, _patternCount, _controlPointsInsideOverlap, true);
            }
            foreach (var p in _projectors)
            {
                p.GeneratePattern();
            }
            _black = Generate(0f);
            _white = Generate(1f);

            _black.SaveAsImage(_projectorResolution, Path.Combine(Helpers.TempDir, "black.png"));
            _white.SaveAsImage(_projectorResolution, Path.Combine(Helpers.TempDir, "white.png"));
            for (var i = 0; i < _projectors.Length; i++)
            {
                _projectors[i].SavePattern(Path.Combine(Helpers.TempDir, "p" + i + ".png"));
            }
            if(ShowImage != null)
            {
                SavePatternFull();
                SaveWhiteFull();
            }

            VideoCapture.Instance.Stop();
            // TODO Marco: Kamera oder File
            if (Helpers.CameraCalibration)
            {
                VideoCapture.Instance.Start(true);
            }
            else
            {
                VideoCapture.Instance.StartFromFile(true);
            }
            SaveCursorPositionIntern();
            ShowImage?.Invoke(Path.Combine(Helpers.TempDir, "whitefull.png"));
            if (!Helpers.IsDevComputer)
            {
                foreach(var p in _projectors)
                {
                    var errorCode = NvApi.ShowImage(p.DisplayId, _white, p.Resolution.Width, p.Resolution.Height);
                    HandleNvApiError(errorCode);
                }
            }
            RestoreCursorPositionIntern();
            AwaitProjectorsReady(WhiteWhiteDone, OnCalibrationCanceled, new [] { CalibrationSteps.White, CalibrationSteps.White});
        }

        private void SavePatternFull()
        {
            for(var i = 0;i<_projectors.Length;i++)
            {
                var source = new Bitmap(Path.Combine(Helpers.TempDir, $"p{i}.png"));
                var bmp = new Bitmap(Resolution.Width, Resolution.Height);
                var g = Graphics.FromImage(bmp);
                g.Clear(Color.White);
                var pos = 0;
                if(i == 1)
                {
                    pos = Resolution.Width - _projectors[i].Resolution.Width;
                }
                g.DrawImage(source, pos, 0);
                bmp.Save(Path.Combine(Helpers.TempDir, $"p{i}full.png"));
                bmp.Dispose();
                source.Dispose();
            }
        }

        private void SaveWhiteFull()
        {
            var bmp = new Bitmap(Resolution.Width, Resolution.Height);
            var g = Graphics.FromImage(bmp);
            g.Clear(Color.White);
            bmp.Save(Path.Combine(Helpers.TempDir, "whitefull.png"));
            bmp.Dispose();
        }

        private void SaveCursorPositionIntern()
        {
            SaveCursorPosition?.Invoke();
        }

        private void RestoreCursorPositionIntern()
        {
            RestoreCursorPosition?.Invoke();
        }

        public void Warp()
        { 
            SaveCursorPositionIntern();
            var data = GetWarpData();
            if (!Helpers.IsDevComputer)
            {
                // TODO Marco: überarbeiten
                //var ee = NvApi.InitWarp(_projectors[0].DisplayId, data.Vertices0, _projectors[1].DisplayId, data.Vertices1, data.NumVertices);
                //HandleNvApiError(ee);
                //ee = NvApi.InitWarp(_projectors[1].DisplayId, data.Vertices1, _projectors[1].DisplayId, data.Vertices1, data.NumVertices);
                //HandleNvApiError(ee);
                //var errorCode = NvApi.Warp(_projectors[0].DisplayId, data.Vertices0, data.NumVertices);
                //HandleNvApiError(errorCode);
                //errorCode = NvApi.Warp(_projectors[1].DisplayId, data.Vertices1, data.NumVertices);
                //HandleNvApiError(errorCode);

                var displayIds = new[] { _projectors[0].DisplayId, _projectors[1].DisplayId };
                var errorCode = NvApi.WarpMultiple(displayIds, displayIds.Length, data.Vertices0.Concat(data.Vertices1).ToArray(), data.NumVertices);
                HandleNvApiError(errorCode);

                //NvApi.InitWarp(data.Vertices0, data.Vertices1, data.NumVertices);
            }
            RestoreCursorPosition?.Invoke();
        }

        public void WarpBlend(bool generateBlendImages)
        {
            SaveCursorPositionIntern();
            var warpData = GetWarpData();
            var blendData = GetBlendData();
            if (generateBlendImages)
            {
                GenerateBlendImages(blendData);
            }
            if (!Helpers.IsDevComputer)
            {
                //var errorCode = NvApi.Blend(_projectors[0].DisplayId, blendData.blend0, blendData.offset0, blendData.width, _projectors[0].Resolution.Height);
                //HandleNvApiError(errorCode);
                //errorCode = NvApi.Blend(_projectors[1].DisplayId, blendData.blend1, blendData.offset1, blendData.width, _projectors[1].Resolution.Height);
                //HandleNvApiError(errorCode);
                //errorCode = NvApi.Warp(_projectors[0].DisplayId, warpData.Vertices0, warpData.NumVertices);
                //HandleNvApiError(errorCode);
                //errorCode = NvApi.Warp(_projectors[1].DisplayId, warpData.Vertices1, warpData.NumVertices);
                //HandleNvApiError(errorCode);
                //NvApi.WarpBlend(warpData.Vertices0, warpData.NumVertices, blendData.blend, blendData.blend1, blendData.offset0, blendData.offset1, blendData.width);

                var displayIds = new[] { _projectors[0].DisplayId, _projectors[1].DisplayId };
                var status = NvApi.UnWarp(displayIds, displayIds.Length);
                HandleNvApiError(status);
                status = NvApi.UnBlend(displayIds, displayIds.Length, blendData.Width, _projectors[0].Resolution.Height);
                HandleNvApiError(status);
                status = NvApi.WarpMultiple(displayIds, displayIds.Length, warpData.Vertices0.Concat(warpData.Vertices1).ToArray(), warpData.NumVertices);
                HandleNvApiError(status);
                status = NvApi.Blend(_projectors[0].DisplayId, blendData.Blend0, blendData.Offset0, blendData.Width, _projectors[0].Resolution.Height);
                HandleNvApiError(status);
                status = NvApi.Blend(_projectors[1].DisplayId, blendData.Blend1, blendData.Offset1, blendData.Width, _projectors[1].Resolution.Height);
                HandleNvApiError(status);
            }
            RestoreCursorPositionIntern();
        }

        public void UnWarp()
        {
            SaveCursorPositionIntern();
            if (!Helpers.IsDevComputer)
            {
                var displayIds = new[] { _projectors[0].DisplayId, _projectors[1].DisplayId };
                var errorCode = NvApi.UnWarp(displayIds, displayIds.Length);
                HandleNvApiError(errorCode);
            }
            RestoreCursorPositionIntern();
        }

        public void Blend()
        {
            SaveCursorPositionIntern();
            var data = GetBlendData();
            GenerateBlendImages(data);
            if (!Helpers.IsDevComputer)
            {
                var errorCode = NvApi.Blend(_projectors[0].DisplayId, data.Blend0, data.Offset0, data.Width, _projectors[0].Resolution.Height);
                HandleNvApiError(errorCode);
                errorCode = NvApi.Blend(_projectors[1].DisplayId, data.Blend1, data.Offset1, data.Width, _projectors[1].Resolution.Height);
                HandleNvApiError(errorCode);
                //NvApi.Blend(data.blend0, data.blend1, data.offset0, data.offset1, data.width);
            }
            RestoreCursorPositionIntern();
        }

        private void GenerateBlendImages(BlendData data)
        {
            var gen = new PngGenerator();
            gen.GenerateBlendImages(data.Blend0, data.Blend1, data.Offset0, data.Offset1, Projectors[0].Resolution.Width, Projectors[0].Resolution.Height);
        }

        public void UnBlend()
        {
            SaveCursorPositionIntern();
            if (!Helpers.IsDevComputer)
            {
                var displayIds = new[] { _projectors[0].DisplayId, _projectors[1].DisplayId };
                var errorCode = NvApi.UnBlend(displayIds, displayIds.Length, _projectors[0].Resolution.Width, _projectors[0].Resolution.Height);
                HandleNvApiError(errorCode);                
            }
            RestoreCursorPositionIntern();
        }

        private WarpData GetWarpData()
        {
            var panoScreen = Helpers.GetScreens().OrderByDescending(s => s.Bounds.Width).First();
            return new WarpData
            {
                Vertices0 = _projectors[0].GetVertices(panoScreen),
                Vertices1 = _projectors[1].GetVertices(panoScreen),
                NumVertices = _projectors[0].GetNumVertices()
            };
        }

        private BlendData GetBlendData()
        {
            var blendData = new BlendData
            {
                Width = _projectors[0].Resolution.Width
            };
            Parallel.For(0, 2, i =>
            {
                if (i == 0)
                {
                    blendData.Blend0 = _projectors[0].GetBlendData();
                }
                else
                {
                    blendData.Blend1 = _projectors[1].GetBlendData();
                }
            });

            var blackLevelData = GetBlackLevelData();

            blendData.Offset0 = blackLevelData.Offset0;
            blendData.Offset1 = blackLevelData.Offset1;

            return blendData;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private float[] GetBlackLevelData(int projector)
        {
            var blxList = new List<int>();
            var blyList = new List<int>();
            foreach (var i in _projectors[projector].BlacklevelIndexes)
            {
                blxList.Add(_projectors[projector].BlacklevelControlPoints[i].X);
                blyList.Add(_projectors[projector].BlacklevelControlPoints[i].Y);
            }
            var blx = blxList.ToArray();
            var bly = blyList.ToArray();

            var boundingControlPoints = _projectors[projector].BoundingControlPoints;
            var boundingX = boundingControlPoints.Select(b => b.X).ToArray();
            var boundingY = boundingControlPoints.Select(b => b.Y).ToArray();

            var offsetList = new float[_projectorResolution.Width * _projectorResolution.Height * 1];
            Helpers.ArrayFill(offsetList, 0.0f);

            var offset = (float)Projectors[projector].BlacklevelOffset;

            var bl2xList = new List<int>();
            var bl2yList = new List<int>();
            foreach (var i in _projectors[projector].BlacklevelIndexes)
            {
                bl2xList.Add(_projectors[projector].Blacklevel2ControlPoints[i].X);
                bl2yList.Add(_projectors[projector].Blacklevel2ControlPoints[i].Y);
            }
            var bl2x = bl2xList.ToArray();
            var bl2y = bl2yList.ToArray();

            var bmp = new Bitmap(_projectorResolution.Width, _projectorResolution.Height);
            var g = Graphics.FromImage(bmp);
            var points = new Point[bl2x.Length];
            for (var i = 0; i < bl2x.Length; i++)
            {
                points[i] = new Point(bl2x[i], bl2y[i]);
            }
            g.FillPolygon(Brushes.Black, points, FillMode.Winding);
            var bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            var data1 = new byte[bmp.Height * bmpData.Stride];
            Marshal.Copy(bmpData.Scan0, data1, 0, data1.Length);
            bmp.UnlockBits(bmpData);

            g.Clear(Color.Transparent);
            points = new Point[boundingX.Length];
            for (var i = 0; i < boundingX.Length; i++)
            {
                points[i] = new Point(boundingX[i], boundingY[i]);
            }
            g.FillPolygon(Brushes.Black, points, FillMode.Winding);
            bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            var data2 = new byte[bmp.Height * bmpData.Stride];
            Marshal.Copy(bmpData.Scan0, data2, 0, data2.Length);
            bmp.UnlockBits(bmpData);

            g.Clear(Color.Transparent);
            points = new Point[blx.Length];
            for (var i = 0; i < blx.Length; i++)
            {
                points[i] = new Point(blx[i], bly[i]);
            }
            g.FillPolygon(Brushes.Black, points, FillMode.Winding);
            bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, bmp.PixelFormat);
            var data3 = new byte[bmp.Height * bmpData.Stride];
            Marshal.Copy(bmpData.Scan0, data3, 0, data3.Length);
            bmp.UnlockBits(bmpData);

            var f = bmpData.Stride/bmp.Width;

            var offset2 = (float)Projectors[projector].Blacklevel2Offset;

            for(var y = 0;y<_projectorResolution.Height; y++)
            {
                var yw = y * _projectorResolution.Width;
                var ywf = y*_projectorResolution.Width * f;
                for(var x = 0;x<_projectorResolution.Width;x++)
                {
                    var i = x*f + ywf + 3;
                    if (offset2 > 0 && data1[i] > 0)
                    {
                        offsetList[(x + yw) * 1] = offset2;
                    }
                    else if (data2[i] > 0)
                    {
                        if (!(data3[i] > 0))
                        {
                            offsetList[(x + yw) * 1] = offset;
                        }
                    }
                }
            }

            return offsetList;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private BlackLevelData GetBlackLevelData()
        {
            var blacklevelData = new BlackLevelData();

            Parallel.For(0, 2, i =>
            {
                if (i == 0)
                {
                    blacklevelData.Offset0 = GetBlackLevelData(0);
                }
                else
                {
                    blacklevelData.Offset1 = GetBlackLevelData(1);
                }
            });

            return blacklevelData;
        }

        private void WhiteWhiteDone()
        {
            int count = 0;
            VideoCapture.Instance.SaveFrame = bitmap =>
            {
                count++;
                if (count > 2)
                {
                    VideoCapture.Instance.SaveFrame = null;
                    bitmap.Save(Path.Combine(Helpers.TempDir, "capture_white.png"), ImageFormat.Png);
                    bitmap.Dispose();
                    SaveCursorPositionIntern();
                    if (!Helpers.IsDevComputer)
                    {
                        var errorCode = NvApi.ShowImage(_projectors[0].DisplayId, _white, _projectors[0].Resolution.Width, _projectors[0].Resolution.Height);
                        HandleNvApiError(errorCode);
                         errorCode = NvApi.ShowImage(_projectors[1].DisplayId, _black, _projectors[1].Resolution.Width, _projectors[1].Resolution.Height);
                        HandleNvApiError(errorCode);
                    }
                    RestoreCursorPositionIntern();
                    AwaitProjectorsReady(WhiteBlackDone, OnCalibrationCanceled, new[] { CalibrationSteps.White, CalibrationSteps.Black });
                }
            };
        }

        private void WhiteBlackDone()
        {
            VideoCapture.Instance.SaveFrame = bitmap =>
            {
                VideoCapture.Instance.SaveFrame = null;
                bitmap.Save(Path.Combine(Helpers.TempDir, "capture_white0.png"), ImageFormat.Png);
                bitmap.Dispose();
                SaveCursorPositionIntern();
                if (!Helpers.IsDevComputer)
                {
                    var errorCode = NvApi.ShowImage(_projectors[1].DisplayId, _black, _projectors[1].Resolution.Width, _projectors[1].Resolution.Height);
                    HandleNvApiError(errorCode);
                    if (ShowImage == null)
                    {
                        errorCode = NvApi.ShowImage(_projectors[0].DisplayId, _projectors[0].Pattern, _projectors[0].Resolution.Width, _projectors[0].Resolution.Height);
                        HandleNvApiError(errorCode);
                    }
                    else
                    {
                        errorCode = NvApi.ShowImage(_projectors[0].DisplayId, _white, _projectors[0].Resolution.Width, _projectors[0].Resolution.Height);
                        HandleNvApiError(errorCode);
                        ShowImage(Path.Combine(Helpers.TempDir, "p0full.png"));
                    }
                }
                RestoreCursorPositionIntern();
                AwaitProjectorsReady(PatternBlackDone, OnCalibrationCanceled, new[] { CalibrationSteps.Pattern, CalibrationSteps.Black });
            };
        }

        private void PatternBlackDone()
        {
            VideoCapture.Instance.SaveFrame = bitmap =>
            {
                VideoCapture.Instance.SaveFrame = null;
                bitmap.Save(Path.Combine(Helpers.TempDir, "capture_pattern0.png"), ImageFormat.Png);
                bitmap.Dispose();
                SaveCursorPositionIntern();
                if (!Helpers.IsDevComputer)
                {
                    var errorCode = NvApi.ShowImage(_projectors[0].DisplayId, _black, _projectors[0].Resolution.Width, _projectors[0].Resolution.Height);
                    HandleNvApiError(errorCode);
                    errorCode = NvApi.ShowImage(_projectors[1].DisplayId, _white, _projectors[1].Resolution.Width, _projectors[1].Resolution.Height);
                    HandleNvApiError(errorCode);
                    ShowImage?.Invoke(Path.Combine(Helpers.TempDir, "whitefull.png"));
                }
                RestoreCursorPositionIntern();
                AwaitProjectorsReady(BlackWhiteDone, OnCalibrationCanceled, new[] { CalibrationSteps.Black, CalibrationSteps.White });
            };
        }

        private void BlackWhiteDone()
        {
            VideoCapture.Instance.SaveFrame = bitmap =>
            {
                VideoCapture.Instance.SaveFrame = null;
                bitmap.Save(Path.Combine(Helpers.TempDir, "capture_white1.png"), ImageFormat.Png);
                bitmap.Dispose();
                SaveCursorPositionIntern();
                if (!Helpers.IsDevComputer)
                {
                    var errorCode = NvApi.ShowImage(_projectors[0].DisplayId, _black, _projectors[0].Resolution.Width, _projectors[0].Resolution.Height);
                    HandleNvApiError(errorCode);
                    if (ShowImage == null)
                    {
                        errorCode = NvApi.ShowImage(_projectors[1].DisplayId, _projectors[1].Pattern, _projectors[1].Resolution.Width, _projectors[1].Resolution.Height);
                        HandleNvApiError(errorCode);
                    }
                    else
                    {
                        errorCode = NvApi.ShowImage(_projectors[1].DisplayId, _white, _projectors[1].Resolution.Width, _projectors[1].Resolution.Height);
                        HandleNvApiError(errorCode);
                        ShowImage(Path.Combine(Helpers.TempDir, "p1full.png"));
                    }
                }
                RestoreCursorPositionIntern();
                AwaitProjectorsReady(BlackPatternDone, OnCalibrationCanceled, new[] { CalibrationSteps.Black, CalibrationSteps.Pattern });
            };
        }

        private void BlackPatternDone()
        {
            VideoCapture.Instance.SaveFrame = bitmap =>
            {
                VideoCapture.Instance.SaveFrame = null;
                bitmap.Save(Path.Combine(Helpers.TempDir, "capture_pattern1.png"), ImageFormat.Png);
                bitmap.Dispose();
                SaveCursorPositionIntern();
                if (!Helpers.IsDevComputer)
                {
                    var displayIds = new[] { _projectors[0].DisplayId, _projectors[1].DisplayId };
                    var errorCode = NvApi.UnBlend(displayIds, displayIds.Length, _projectors[0].Resolution.Width, _projectors[0].Resolution.Height);
                    HandleNvApiError(errorCode);
                }
                RestoreCursorPositionIntern();
                Detect();
            };
        }
        
        private float[] Generate(float value)
        {
            var data = new float[_projectorResolution.Width * _projectorResolution.Height * 3];
            Helpers.ArrayFill(data, value);

            return data;
        }

        private void Detect()
        {
            try
            {
                var calibration = new Calibration();
                calibration.Initialize(Resolution, Overlap, _projectors);
                calibration.DetectProgress += CalibrationOnDetectProgress;
                AwaitCalculationsReady();
                calibration.Detect(ClippingRectangle, _keepCorners);
                CalibrationDone();
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while(inner != null) {
                    message += Environment.NewLine + inner.Message;
                    inner = inner.InnerException;
                }
                CalibrationError(message);
            }
        }

        private void CalibrationOnDetectProgress(float progress)
        {
            CalculationProgress?.Invoke(progress);
        }

        private void OnCalibrationCanceled()
        {
            CalibrationCanceled();
        }
    }
}