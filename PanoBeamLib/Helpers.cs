using AForge;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;

namespace PanoBeamLib
{
    public class Helpers
    {
        public static bool IsDevComputer => Environment.MachineName == "SURFACE" ||
                                            Environment.MachineName == "BUEROx";

        public static void InitTempDir()
        {
            Debug.WriteLine("Init temp dir");
            var path = TempDir;
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
            Debug.WriteLine("Create temp dir");
            Directory.CreateDirectory(path);
            Debug.WriteLine("Temp dir created");
        }

        public static string TempDir => Path.Combine(Path.GetTempPath(), "PanoBeam");

        public static bool CameraCalibration => true;

        public static bool IsInPolygon(int nvert, double[] vertx, double[] verty, double testx, double testy)
        {
            int i, j;
            bool c = false;
            for (i = 0, j = nvert - 1; i < nvert; j = i++)
            {
                if (((verty[i] > testy) != (verty[j] > testy)) &&
                 (testx < (vertx[j] - vertx[i]) * (testy - verty[i]) / (verty[j] - verty[i]) + vertx[i]))
                    c = !c;
            }
            return c;
        }

        public static Screen[] GetScreens()
        {
            return System.Windows.Forms.Screen.AllScreens.Select(s => new Screen
            {
                Primary = s.Primary,
                Bounds = s.Bounds
            }).ToArray();
        }

        public static bool IsInPolygon(int nvert, int[] vertx, int[] verty, int testx, int testy)
        {
            return IsInPolygon(nvert, vertx.Select(v => (double) v).ToArray(), verty.Select(v => (double) v).ToArray(), testx, testy);
        }

        internal static void ArrayFill<T>(T[] arrayToFill, T fillValue)
        {
            // if called with a single value, wrap the value in an array and call the main function
            ArrayFill(arrayToFill, new[] { fillValue });
        }

        internal static void ArrayFill<T>(T[] arrayToFill, T[] fillValue)
        {
            if (fillValue.Length >= arrayToFill.Length)
            {
                throw new ArgumentException("fillValue array length must be smaller than length of arrayToFill");
            }

            // set the initial array value
            Array.Copy(fillValue, arrayToFill, fillValue.Length);

            int arrayToFillHalfLength = arrayToFill.Length / 2;

            for (int i = fillValue.Length; i < arrayToFill.Length; i *= 2)
            {
                int copyLength = i;
                if (i > arrayToFillHalfLength)
                {
                    copyLength = arrayToFill.Length - i;
                }

                Array.Copy(arrayToFill, 0, arrayToFill, i, copyLength);
            }
        }

        public static void FillOutsideBlack(Bitmap bmp, IntPoint[] corners)
        {
            FillOutside(bmp, corners, Brushes.Black);
        }

        private static void FillOutside(Bitmap bmp, IntPoint[] corners, Brush brush)
        {
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillPolygon(brush, new[] {
                    new PointF(0,0),
                    new PointF(bmp.Width, 0),
                 new PointF(bmp.Width, bmp.Height),
                    new PointF(0, bmp.Height),
                 new PointF(corners[3].X - 20, corners[3].Y + 20),
                 new PointF(corners[2].X + 20, corners[2].Y + 20),
                 new PointF(corners[1].X + 20, corners[1].Y - 20),
                 new PointF(corners[0].X - 20, corners[0].Y - 20),
                 new PointF(corners[3].X - 20, corners[3].Y + 20),
                 new PointF(0, bmp.Height)
                });
            }
        }

        public static void SaveImageWithMarkers(Bitmap bmp, IntPoint[] points, string fileName, int markerSize)
        {
            var saveBitmap = (Bitmap)bmp.Clone();
            using (var g = Graphics.FromImage(saveBitmap))
            {
                foreach (var p in points)
                {
                    g.FillCircle(new SolidBrush(Color.FromArgb(150, 255, 0, 0)), p.X, p.Y, markerSize);
                }
            }
            saveBitmap.Save(fileName);
        }
    }
}