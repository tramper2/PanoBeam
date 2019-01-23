using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace PanoBeamLib.Blend
{
    public class Curve
    {
        private readonly List<ControlPoint> _points;

        public Curve()
        {
            _points = new List<ControlPoint>(new[]
            {
                new ControlPoint(0, 1, ControlPointType.Line),
                new ControlPoint(1, 0, ControlPointType.Line)
            });
        }

        public ControlPoint[] ControlPoints => _points.ToArray();

        public CurvePoint[] GetCurvePoints()
        {
            return _points.Select(MapControlPoint).ToArray();
        }

        private static CurvePoint MapControlPoint(ControlPoint controlPoint)
        {
            return new CurvePoint
            {
                Type = Mapper.MapControlPointType(controlPoint.PointType),
                X = controlPoint.X,
                Y = controlPoint.Y
            };
        }

        public void InitFromConfig(CurvePoint[] curvePoints)
        {
            _points.Clear();
            foreach (var curvePoint in curvePoints)
            {
                _points.Add(new ControlPoint(curvePoint.X, curvePoint.Y, Mapper.MapCurvePointType(curvePoint.Type)));
            }
        }

        public void RemovePoint(ControlPoint point)
        {
            _points.Remove(point);
            UpdateNeighbors();
        }

        public void InsertPoint(ControlPoint point)
        {
            var np = _points.Count;
            int i;
            for (i = 0; i <= np - 1 && _points[i].X < point.X; i++)
            {
            }
            if (i <= np - 1)
            {
                _points.Insert(i, point);
            }
            else
            {
                _points.Add(point);
            }
            UpdateNeighbors();
        }

        private void UpdateNeighbors()
        {
            _points[0].NeighborRight = _points[1];
            for (var i = 1; i < _points.Count - 1; i++)
            {
                _points[i].NeighborLeft = _points[i - 1];
                _points[i].NeighborRight = _points[i + 1];
            }
            _points[_points.Count - 1].NeighborLeft = _points[_points.Count - 2];
        }

        public double GetY(double x)
        {
            double y;
            var knownSamples = new List<KeyValuePair<double, ControlPoint>>();
            foreach (var p in _points)
            {
                if (p.PointType == ControlPointType.Line)
                {
                    if (x <= p.X)
                    {
                        knownSamples.Add(new KeyValuePair<double, ControlPoint>(p.X, p));
                        y = SpLine(knownSamples, x);
                        if (double.IsInfinity(y))
                        {
                            return 1;
                        }
                        return y;
                    }
                    else
                    {
                        knownSamples.Clear();
                        knownSamples.Add(new KeyValuePair<double, ControlPoint>(p.X, p));
                    }
                }
                else
                {
                    knownSamples.Add(new KeyValuePair<double, ControlPoint>(p.X, p));
                }
            }
            y = SpLine(knownSamples, x);
            if (double.IsInfinity(y))
            {
                return 1;
            }
            return y;
        }

        [SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
        private double SpLine(List<KeyValuePair<double, ControlPoint>> knownSamples, double z)
        {
            var np = knownSamples.Count;
            if (np > 1)
            {
                var a = new double[np];
                var h = new double[np];
                for (int i = 1; i <= np - 1; i++)
                {
                    h[i] = knownSamples[i].Key - knownSamples[i - 1].Key;
                }
                if (np > 2)
                {
                    var sub = new double[np - 1];
                    var diag = new double[np - 1];
                    var sup = new double[np - 1];
                    for (var i = 1; i <= np - 2; i++)
                    {
                        diag[i] = (h[i] + h[i + 1]) / 3;
                        sup[i] = h[i + 1] / 6;
                        sub[i] = h[i] / 6;
                        if (h[i + 1] == 0 || h[i] == 0)
                        {
                            a[i] = 0;
                        }
                        else
                        {
                            a[i] = (knownSamples[i + 1].Value.Y - knownSamples[i].Value.Y) / h[i + 1] -
                                   (knownSamples[i].Value.Y - knownSamples[i - 1].Value.Y) / h[i];
                        }
                    }
                    // SolveTridiag is a support function, see Marco Roello's original code
                    // for more information at
                    // http://www.codeproject.com/useritems/SplineInterpolation.asp
                    SolveTridiag(sub, diag, sup, ref a, np - 2);
                }

                var gap = 0;
                var previous = double.MinValue;
                // At the end of this iteration, "gap" will contain the index of the interval
                // between two known values, which contains the unknown z, and "previous" will
                // contain the biggest z value among the known samples, left of the unknown z
                for (var i = 0; i < knownSamples.Count; i++)
                {
                    if (knownSamples[i].Key == z)
                    {
                        return knownSamples[i].Value.Y;
                    }

                    if (knownSamples[i].Key < z && knownSamples[i].Key > previous)
                    {
                        previous = knownSamples[i].Key;
                        gap = i + 1;
                    }
                }
                var x1 = z - previous;
                var x2 = h[gap] - x1;
                var y = ((-a[gap - 1] / 6 * (x2 + h[gap]) * x1 + knownSamples[gap - 1].Value.Y) * x2 +
                            (-a[gap] / 6 * (x1 + h[gap]) * x2 + knownSamples[gap].Value.Y) * x1) / h[gap];
                if (y > 1)
                {
                    return 1;
                }
                if (y < 0)
                {
                    return 0;
                }
                return y;
            }
            else
            {
                return knownSamples[0].Value.Y;
            }
        }

        private void SolveTridiag(double[] sub, double[] diag, double[] sup, ref double[] b, int n)
        {
            /*                  solve linear system with tridiagonal n by n matrix a
                                using Gaussian elimination *without* pivoting
                                where   a(i,i-1) = sub[i]  for 2<=i<=n
                                        a(i,i)   = diag[i] for 1<=i<=n
                                        a(i,i+1) = sup[i]  for 1<=i<=n-1
                                (the values sub[1], sup[n] are ignored)
                                right hand side vector b[1:n] is overwritten with solution 
                                NOTE: 1...n is used in all arrays, 0 is unused */
            int i;
            /*                  factorization and forward substitution */
            for (i = 2; i <= n; i++)
            {
                sub[i] = sub[i] / diag[i - 1];
                diag[i] = diag[i] - sub[i] * sup[i - 1];
                b[i] = b[i] - sub[i] * b[i - 1];
            }
            b[n] = b[n] / diag[n];
            for (i = n - 1; i >= 1; i--)
            {
                b[i] = (b[i] - sup[i] * b[i + 1]) / diag[i];
            }
        }
    }
}