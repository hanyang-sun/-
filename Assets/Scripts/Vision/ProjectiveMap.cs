using System;
using BrickDefense.Core;

namespace BrickDefense.Vision
{
    // Project-specific adapter: solve the eight homography coefficients from four correspondences.
    // Four-point mapping interface researched in glfx.js; no upstream matrix code is copied.
    public sealed class ProjectiveMap
    {
        public double[] Coefficients { get; }
        public ProjectiveMap(double[] coefficients) { Coefficients = coefficients; }
        public ImagePoint Project(double x, double y)
        {
            double[] m = Coefficients;
            double d = m[6] * x + m[7] * y + m[8];
            return new ImagePoint((m[0] * x + m[1] * y + m[2]) / d,
                (m[3] * x + m[4] * y + m[5]) / d);
        }

        // 四对点给出 8 个方程；令齐次矩阵最后一项为 1，消元求其余系数。
        public static bool TryFit(ImagePoint[] source, ImagePoint[] target, out ProjectiveMap result)
        {
            result = null;
            if (source == null || target == null || source.Length != 4 || target.Length != 4) return false;
            var a = new double[8,9];
            for (int i = 0; i < 4; i++)
            {
                double x = source[i].X, y = source[i].Y, u = target[i].X, v = target[i].Y;
                a[2*i,0] = x; a[2*i,1] = y; a[2*i,2] = 1;
                a[2*i,6] = -u*x; a[2*i,7] = -u*y; a[2*i,8] = u;
                a[2*i+1,3] = x; a[2*i+1,4] = y; a[2*i+1,5] = 1;
                a[2*i+1,6] = -v*x; a[2*i+1,7] = -v*y; a[2*i+1,8] = v;
            }
            // 部分选主元的高斯消元；退化四边形会在主元过小时被拒绝。
            for (int col = 0; col < 8; col++)
            {
                int pivot = col;
                for (int r = col + 1; r < 8; r++)
                    if (Math.Abs(a[r,col]) > Math.Abs(a[pivot,col])) pivot = r;
                if (Math.Abs(a[pivot,col]) < 1e-9) return false;
                for (int c = col; c < 9; c++)
                { double t = a[col,c]; a[col,c] = a[pivot,c]; a[pivot,c] = t; }
                double divisor = a[col,col];
                for (int c = col; c < 9; c++) a[col,c] /= divisor;
                for (int r = 0; r < 8; r++)
                {
                    if (r == col) continue;
                    double factor = a[r,col];
                    for (int c = col; c < 9; c++) a[r,c] -= factor*a[col,c];
                }
            }
            var m = new double[9]; m[8] = 1;
            for (int i = 0; i < 8; i++)
            {
                m[i] = a[i,8];
                if (double.IsNaN(m[i]) || double.IsInfinity(m[i])) return false;
            }
            result = new ProjectiveMap(m); return true;
        }

        public static readonly ImagePoint[] UnitSquare =
        { new ImagePoint(0,0), new ImagePoint(1,0), new ImagePoint(1,1), new ImagePoint(0,1) };
    }
}
