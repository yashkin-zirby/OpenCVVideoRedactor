using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Helpers
{
    public static class MatExtension
    {
        public static Mat DrawMat(this Mat source, Mat image, Point point)
        {
            var result = source.Clone();
            var x = Math.Max(0, point.X);
            var y = Math.Max(0, point.Y);
            var overlayX = -Math.Min(0, point.X);
            var overlayY = -Math.Min(0, point.Y);
            if (point.X >= source.Width || point.Y >= source.Height) { return result; }
            if (overlayX >= image.Width || overlayY >= image.Height) { return result; }
            var w = image.Width - overlayX;
            var h = image.Height - overlayY;
            var drawSpace = result[new Rect(x, y, Math.Min(w, result.Width - x), Math.Min(h, result.Height - y))];
            var overlay = image[new Rect(overlayX, overlayY, drawSpace.Width, drawSpace.Height)];
            if (image.Channels() == 4)
            {
                for (int i = 0; i < overlay.Rows; i++)
                {
                    for (int j = 0; j < overlay.Cols; j++)
                    {
                        var o = overlay.At<Vec4b>(i, j);
                        var s = drawSpace.At<Vec3b>(i, j);
                        byte r = (byte)((o.Item0 * o.Item3 + s.Item0 * (255 - o.Item3)) / 255);
                        byte g = (byte)((o.Item1 * o.Item3 + s.Item1 * (255 - o.Item3)) / 255);
                        byte b = (byte)((o.Item2 * o.Item3 + s.Item2 * (255 - o.Item3)) / 255);
                        drawSpace.At<Vec3b>(i, j) = new Vec3b(r, g, b);
                    }
                }
            }
            else
            {
                overlay.CopyTo(drawSpace);
            }
            return result;
        }
    }
}
