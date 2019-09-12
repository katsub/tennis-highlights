using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using TennisHighlights;

/// <summary>
/// The image processing utils
/// </summary>
public class ImageUtils
{
    /// <summary>
    /// Builds the square mask mat.
    /// </summary>
    /// <param name="size">The size.</param>
    public static Mat BuildSquareMaskMat(int size) => new Mat(size, size, MatType.CV_8U, new Scalar(1));

    /// <summary>
    /// Builds the circle mask mat.
    /// </summary>
    /// <param name="circleSize">Size of the circle.</param>
    public static MatOfByte BuildCircleMaskMat(int size)
    {
        var radius = size / 2d;
        var circle = new MatOfByte(size, size, 0);

        for (int i = 0; i < size; i++)
        {
            var x = -radius + 2 * radius * i / size;

            for (int j = 0; j < size; j++)
            {
                var y = -radius + 2 * radius * j / size;

                if (Math.Pow(x, 2) + Math.Pow(y, 2) <= Math.Pow(radius, 2))
                {
                    circle.Set(i, j, 1);
                }
            }
        }

        return circle;
    }

    /// <summary>
    /// Draws the circles.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="circles">The circles.</param>
    /// <param name="circleSize">Size of the circle.</param>
    public static void DrawCircles(Bitmap image, List<Accord.Point> circles, float circleSize, Brush brush)
    {
        if (circles == null) { return; }

        using (Graphics gr = Graphics.FromImage(image))
        {
            gr.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var circle in circles)
            {
                var minX = (int)Math.Max(0d, circle.X - circleSize);
                var minY = (int)Math.Max(0d, circle.Y - circleSize);
                var maxX = (int)Math.Min(image.Width, circle.X + circleSize);
                var maxY = (int)Math.Min(image.Height, circle.Y + circleSize);

                var rect = new Rectangle(minX, minY, maxX - minX, maxY - minY);
                gr.FillEllipse(brush, rect);
            }
        }
    }

    /// <summary>
    /// Draws the circles.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="text">The text.</param>
    /// <param name="position">The position.</param>
    /// <param name="size">The size.</param>
    /// <param name="brush">The brush.</param>
    public static void DrawText(Bitmap image, string text, Accord.Point position, int size, Brush brush)
    {
        using (Graphics g = Graphics.FromImage(image))
        {
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
            g.DrawString(text,
                         new Font("Tahoma", size),
                         Brushes.Red,
                         new PointF((float)position.X, (float)position.Y));
        }
    }

    /// <summary>
    /// Draws the rectangles.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="rectangles">The rectangles.</param>
    /// <param name="brush">The brush.</param>
    public static void DrawRectangles(Bitmap image, List<Boundary> rectangles, Pen pen, Brush brush = null)
    {
        using (Graphics gr = Graphics.FromImage(image))
        {
            gr.SmoothingMode = SmoothingMode.AntiAlias;

            foreach (var rectangle in rectangles)
            {
                var minX = (int)Math.Max(0d, rectangle.minX);
                var minY = (int)Math.Max(0d, rectangle.minY);
                var maxX = (int)Math.Min(image.Width, rectangle.maxX);
                var maxY = (int)Math.Min(image.Height, rectangle.maxY);

                var rect = new Rectangle(minX, minY, maxX - minX, maxY - minY);

                if (brush != null)
                {
                    gr.FillRectangle(brush, rect);
                }
                else
                {
                    gr.DrawRectangle(pen, rect);
                }
            }
        }
    }
}
