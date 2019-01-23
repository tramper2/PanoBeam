using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media.Imaging;

namespace PanoBeamLib
{
    internal static class Extensions
    {
        internal static BitmapSource GetBitmapSource(this Bitmap image)
        {
            var ms = new MemoryStream();
            image.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            bi.Freeze();
            return bi;
        }

        internal static void SaveAsImage(this float[] data, Size resolution, string filename)
        {
            var bmp = new Bitmap(resolution.Width, resolution.Height);
            var bitmapData = bmp.LockBits(new Rectangle(0, 0, resolution.Width, resolution.Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            var size = bitmapData.Stride * bitmapData.Height;
            var buffer = new byte[size];

            Marshal.Copy(bitmapData.Scan0, buffer, 0, size);

            for (var i = 0; i < data.Length; i += 3)
            {
                var val = (byte)(data[i] * 255);
                buffer[i + 0] = val; // blue
                buffer[i + 1] = val; // green
                buffer[i + 2] = val; // red
            }

            Marshal.Copy(buffer, 0, bitmapData.Scan0, buffer.Length);
            bmp.UnlockBits(bitmapData);
            bmp.Save(filename);
        }

        public static void FillCircle(this Graphics g, Brush brush, float x, float y, int radius)
        {
            g.FillCircle(brush, (int)x, (int)y, radius);
        }

        public static void FillCircle(this Graphics g, Brush brush, int x, int y, int radius)
        {
            g.FillEllipse(brush, x - radius, y - radius, radius * 2, radius * 2);
        }
    }
}