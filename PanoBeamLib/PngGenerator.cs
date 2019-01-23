using System.IO;
using AForge;
using Hjg.Pngcs;

namespace PanoBeamLib
{
    class PngGenerator
    {
        //internal void GenerateWarpImages(double[] x0, double[] y0, double[] x1, double[] y1)
        //{
        //    int w = 1920;
        //    int h = 1080;
        //    var imageInfo0 = new ImageInfo(w, h, 8, false);
        //    var imageInfo1 = new ImageInfo(w, h, 8, false);
        //    var png0 = FileHelper.CreatePngWriter(@"C:\Temp\warp0.png", imageInfo0, true);
        //    var png1 = FileHelper.CreatePngWriter(@"C:\Temp\warp1.png", imageInfo1, true);
        //    bool b;
        //    for (var y = 0; y < 1080; y++)
        //    {
        //        var line0 = new ImageLine(imageInfo0);
        //        var line1 = new ImageLine(imageInfo1);
        //        for (var x = 0; x < 1920; x++)
        //        {
        //            b = Helpers.IsInPolygon(x0.Length, x0, y0, x, y);
        //            if (b)
        //            {
        //                ImageLineHelper.SetPixel(line0, x, 255, 255, 255);
        //            }
        //            b = Helpers.IsInPolygon(x1.Length, x1, y1, x, y);
        //            if (b)
        //            {
        //                ImageLineHelper.SetPixel(line1, x, 255, 255, 255);
        //            }
        //        }
        //        png0.WriteRow(line0, y);
        //        png1.WriteRow(line1, y);
        //    }
        //    png0.End();
        //    png1.End();
        //}

        internal void GenerateBlendImages(float[] blend0, float[] blend1, float[] offset0, float[] offset1, int width, int height)
        {
            Parallel.For(0, 4, i =>
            {
                if (i == 0) GenerateImage("blend0.png", blend0, 3, width, height);
                else if (i == 1) GenerateImage("blend1.png", blend1, 3, width, height);
                else if (i == 2) GenerateImage("offset0.png", offset0, 1, width, height);
                else if (i == 3) GenerateImage("offset1.png", offset1, 1, width, height);
            });
        }

        private void GenerateImage(string name, float[] data, int colors, int w, int h)
        {
            var imageInfo = new ImageInfo(w, h, 8, false);
            var png = FileHelper.CreatePngWriter(Path.Combine(Helpers.TempDir, name), imageInfo, true);
            for (int y = 0; y < h; y++)
            {
                var line = new ImageLine(imageInfo);
                for (int x = 0; x < w; x++)
                {
                    if (colors == 1)
                    {
                        var r = (int)(255f * data[x + y * w]);
                        ImageLineHelper.SetPixel(line, x, r, r, r);
                    }
                    else
                    {
                        var r = (int)(255f * data[(x + y * w) * 3 + 0]);
                        var g = (int)(255f * data[(x + y * w) * 3 + 1]);
                        var b = (int)(255f * data[(x + y * w) * 3 + 2]);
                        ImageLineHelper.SetPixel(line, x, r, g, b);
                    }
                }
                png.WriteRow(line, y);
            }
            png.End();
        }
    }
}
