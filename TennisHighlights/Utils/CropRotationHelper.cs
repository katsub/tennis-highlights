using OpenCvSharp;
using System;

namespace TennisHighlights.Utils
{
    /// <summary>
    /// The crop rotation helper
    /// </summary>
    public static class CropRotationHelper
    {
        /// <summary>
        /// Gets the crop coordinates.
        /// </summary>
        /// <param name="angleInRadians">The angle in degrees.</param>
        /// <param name="imageDimensions">The image dimensions.</param>
        public static Rect GetCropCoordinates(double angleInDegrees, Rect imageDimensions)
        {
            var angleInRadians = angleInDegrees * Math.PI / 180d;
            var ang = angleInRadians;
            var img = imageDimensions;
            var pi = System.Math.PI;

            var quadrant = (int)(System.Math.Floor(ang / (pi / 2))) & 3;
            var sign_alpha = (quadrant & 1) == 0 ? ang : pi - ang;
            var alpha = (sign_alpha % pi + pi) % pi;

            var bb = (
                w: img.Width * Math.Cos(alpha) + img.Height * Math.Sin(alpha),
                h: img.Width * Math.Sin(alpha) + img.Height * Math.Cos(alpha)
            );

            var gamma = img.Width < img.Height ? Math.Atan2(bb.w, bb.h) : Math.Atan2(bb.h, bb.w);

            var delta = Math.PI - alpha - gamma;

            var length = img.Width < img.Height ? img.Height : img.Width;
            var d = length * Math.Cos(alpha);
            var a = d * Math.Sin(alpha) / Math.Sin(delta);

            var y = a * Math.Cos(gamma);
            var x = y * Math.Tan(gamma);

            return new Rect((int)x, (int)y, (int)(bb.w - 2 * x), (int)(bb.h - 2 * y));
        }
    }
}
