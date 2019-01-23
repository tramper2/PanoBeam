using System.Collections.Generic;
using System.Drawing;
using AForge;
using math = Accord.Math;

namespace PanoBeamLib
{
    public class Calculations
    {
        public static List<IntPoint> FilterOutbounded(List<IntPoint> points, Rectangle bounds)
        {
            var pointsFiltered = new List<IntPoint>();
            foreach (var point in points)
            {
                if (bounds.Contains(point.X, point.Y))
                {
                    pointsFiltered.Add(point);
                }
            }
            return pointsFiltered;
        }

        public static Bounds GetBoundryRectangle(List<IntPoint> points)
        {
            var x1 = int.MaxValue;
            var y1 = int.MaxValue;
            var x2 = 0;
            var y2 = 0;
            foreach (var point in points)
            {
                if (point.X < x1)
                {
                    x1 = point.X;
                }
                if (point.X > x2)
                {
                    x2 = point.X;
                }
                if (point.Y < y1)
                {
                    y1 = point.Y;
                }
                if (point.Y > y2)
                {
                    y2 = point.Y;
                }
            }

            return new Bounds(x1, y1, x2, y2);
        }

        public static IntPoint[] SortCorners(IntPoint[] corners)
        {
            // Die Ecken sind im Uhrzeigersinn sortiert. Aber der erste (index 0) ist nicht,
            // immer oben links, sondern jeweils derjenige mit dem kleinsten X.
            var d = double.MaxValue;
            var offset = 0;
            for (var i = 0; i < corners.Length; i++)
            {
                var tmp = math.Distance.SquareEuclidean(0, corners[i].X, 0, corners[i].Y);
                if (tmp < d)
                {
                    d = tmp;
                    offset = i;
                }
            }

            return new[] {
                corners[(0 + offset) % 4],
                corners[(1 + offset) % 4],
                corners[(2 + offset) % 4],
                corners[(3 + offset) % 4],
            };
        }

        //public static IntPoint[] GetCorners(List<IntPoint> points, Bounds bounds)
        //{
        //    var d0 = double.MaxValue;
        //    var d1 = double.MaxValue;
        //    var d2 = double.MaxValue;
        //    var d3 = double.MaxValue;
        //    var c0 = new IntPoint();
        //    var c1 = new IntPoint();
        //    var c2 = new IntPoint();
        //    var c3 = new IntPoint();

        //    foreach (var p in points)
        //    {
        //        var tmp = math.Distance.SquareEuclidean(bounds.X1, p.X, bounds.Y1, p.Y);
        //        if (tmp < d0)
        //        {
        //            d0 = tmp;
        //            c0 = p;
        //        }

        //        tmp = math.Distance.SquareEuclidean(bounds.X2, p.X, bounds.Y1, p.Y);
        //        if (tmp < d1)
        //        {
        //            d1 = tmp;
        //            c1 = p;
        //        }

        //        tmp = math.Distance.SquareEuclidean(bounds.X2, p.X, bounds.Y2, p.Y);
        //        if (tmp < d2)
        //        {
        //            d2 = tmp;
        //            c2 = p;
        //        }

        //        tmp = math.Distance.SquareEuclidean(bounds.X1, p.X, bounds.Y2, p.Y);
        //        if (tmp < d3)
        //        {
        //            d3 = tmp;
        //            c3 = p;
        //        }
        //    }

        //    return new[] { c0, c1, c2, c3 };
        //}
    }
}