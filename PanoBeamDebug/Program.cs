using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using AForge.Imaging;
using AForge.Imaging.Filters;
using PanoBeamLib;
using Image = System.Drawing.Image;

namespace PanoBeamDebug
{
    class Program
    {
        static void Main(string[] args)
        {
            //new Program().DetectSurface();
            new Program().DetectShapes();
        }

        public void DetectShapes()
        {
            int count = 40;
            int minSize = 5;
            int maxSize = 80;
            var image = (Bitmap)Image.FromFile(@"C:\Users\marco\Downloads\PanoBeam\capture_pattern0.png");
            Rectangle clippingRectangle = new Rectangle(new Point(563, 360), new Size(1156, 382));
            var clippingRectangleCorners = new[]
            {
                new AForge.IntPoint(clippingRectangle.X, clippingRectangle.Y),
                new AForge.IntPoint(clippingRectangle.X + clippingRectangle.Width, clippingRectangle.Y),
                new AForge.IntPoint(clippingRectangle.X + clippingRectangle.Width, clippingRectangle.Y + clippingRectangle.Height),
                new AForge.IntPoint(clippingRectangle.X, clippingRectangle.Y + clippingRectangle.Height)
            };
            Helpers.FillOutsideBlack(image, clippingRectangleCorners);

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

        }

        public void DetectSurface()
        {
            var _bmpWhite = (Bitmap)Image.FromFile(Path.Combine(@"C:\Users\marco\Downloads\PanoBeam", "capture_white.png"));
            Rectangle clippingRectangle = new Rectangle(new Point(563, 360), new Size(1156, 382));
            var clippingRectangleCorners = new[]
            {
                new AForge.IntPoint(clippingRectangle.X, clippingRectangle.Y),
                new AForge.IntPoint(clippingRectangle.X + clippingRectangle.Width, clippingRectangle.Y),
                new AForge.IntPoint(clippingRectangle.X + clippingRectangle.Width, clippingRectangle.Y + clippingRectangle.Height),
                new AForge.IntPoint(clippingRectangle.X, clippingRectangle.Y + clippingRectangle.Height)
            };
            Helpers.FillOutsideBlack(_bmpWhite, clippingRectangleCorners);
            //SaveBitmap(_bmpWhite, Path.Combine(@"C:\Users\marco\Downloads\PanoBeam\123", "outsideblack.png"));

            var corners = Recognition.DetectSurface(_bmpWhite);
            corners = Calculations.SortCorners(corners);
            Helpers.SaveImageWithMarkers(_bmpWhite, corners, Path.Combine(@"C:\Users\marco\Downloads\PanoBeam\123", "detect_white.png"), 5);
        }

        private static void SaveBitmap(Bitmap bmp, string fileName)
        {
            var saveBitmap = (Bitmap)bmp.Clone();
            saveBitmap.Save(fileName);
        }
    }
}
