using AForge;
using AForge.Imaging;
using AForge.Math.Geometry;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using AForge.Imaging.Filters;

namespace PanoBeamLib
{
    public class Recognition
    {
        public static IntPoint[] DetectSurface(Bitmap image)
        {
            var blobCounter = new BlobCounter
            {
                FilterBlobs = true,
                MinHeight = 10,
                MinWidth = 10
            };

            var shapes = new List<Shape>();
            var shapeChecker = new SimpleShapeChecker();

            var threshold = GetThreshold(image);
            blobCounter.BackgroundThreshold = Color.FromArgb(255, threshold, threshold, threshold);
            blobCounter.ProcessImage(image);
            var blobs = blobCounter.GetObjectsInformation();
            foreach (var blob in blobs)
            {
                if(blob.Fullness >= 1) continue;
                var edgePoints = blobCounter.GetBlobsEdgePoints(blob);
                if (shapeChecker.IsQuadrilateral(edgePoints, out List<IntPoint> corners))
                {
                    shapes.Add(new Shape { Blob = blob, Corners = corners });
                }
            }

            var bestShape = shapes.OrderBy(s => s.Blob.Area)
                .Skip(shapes.Count / 2)
                .Take(1).FirstOrDefault();

            return bestShape?.Corners?.ToArray();
        }

        public static int GetThreshold(Bitmap image)
        {
            var unmanagedImage = UnmanagedImage.FromManagedImage(image);
            UnmanagedImage grayImage;
            if (unmanagedImage.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                grayImage = unmanagedImage;
            }
            else
            {
                grayImage = UnmanagedImage.Create(unmanagedImage.Width, unmanagedImage.Height,
                    PixelFormat.Format8bppIndexed);
                Grayscale.CommonAlgorithms.BT709.Apply(unmanagedImage, grayImage);
                unmanagedImage.Dispose();
            }

            var otsuThresholdFilter = new OtsuThreshold();
            otsuThresholdFilter.ApplyInPlace(grayImage);
            var threshold = otsuThresholdFilter.ThresholdValue;
            grayImage.Dispose();
            return threshold;
        }

        private class Shape
        {
            public Blob Blob { get; set; }

            public List<IntPoint> Corners { get; set; }
        }
    }
}
