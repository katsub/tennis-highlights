using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using TennisHighlights;
using TennisHighlights.ImageProcessing;

/// <summary>
/// The image processing utils
/// </summary>
public class ImageUtils
{
    /// <summary>
    /// Gets the keypoint links.
    /// </summary>
    public static readonly List<(int keypoint1, int keypoint2)> KeypointLinks = new List<(int keypoint1, int keypoint2)> { 
        (3,4), (2,3), (0,1), (7,6), (6,5), (2,1), (5,1), (1, 14), (14, 8), (14, 11), (8,9), (9,10), (11, 12), (12, 13) }; 

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
    /// Clusters the 2d points and returns their labels.
    /// </summary>
    /// <param name="points">The points.</param>
    /// <param name="numberOfClusters">The number of clusters.</param>
    /// <param name="error">The error.</param>
    public static Mat Cluster2DPoints(List<Accord.Point> points, int numberOfClusters, out double error)
    {
        using (var samplesMat = new MatOfFloat(points.Count, 2))
        using (var resultMat = new MatOfByte())
        using (var resultMat2 = new MatOfByte())
        {
            var samplesIndexer = samplesMat.GetIndexer();

            for (int i = 0; i < points.Count; i++)
            {
                samplesIndexer[i, 0] = points[i].X;
                samplesIndexer[i, 1] = points[i].Y;
            }

            error = Cv2.Kmeans(samplesMat, numberOfClusters, resultMat, TermCriteria.Both(5, 0.001), 3, KMeansFlags.PpCenters);

            return resultMat;
        }
    }

    /// <summary>
    /// Draws the circles.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="circles">The circles.</param>
    /// <param name="circleSize">Size of the circle.</param>
    /// <param name="labelCircles">If true, labels the circles with their index</param>
    public static void DrawCircles(Bitmap image, List<Accord.Point> circles, float circleSize, Brush brush, bool labelCircles = false)
    {
        if (circles == null) { return; }

        using (Graphics gr = Graphics.FromImage(image))
        {
            gr.SmoothingMode = SmoothingMode.AntiAlias;

            var i = 0;
            foreach (var circle in circles)
            {
                var minX = (int)Math.Max(0d, circle.X - circleSize);
                var minY = (int)Math.Max(0d, circle.Y - circleSize);
                var maxX = (int)Math.Min(image.Width, circle.X + circleSize);
                var maxY = (int)Math.Min(image.Height, circle.Y + circleSize);

                var rect = new Rectangle(minX, minY, maxX - minX, maxY - minY);

                gr.FillEllipse(brush, rect);

                if (labelCircles)
                {
                    DrawText(gr, i.ToString(), circle - new Accord.Point(5,5), (int)circleSize, Brushes.Black);
                }

                i++;
            }
        }
    }

    /// <summary>
    /// Draws the keypoints.
    /// </summary>
    /// <param name="keypoints">The keypoints.</param>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="resizeMat">The resize mat.</param>
    public static void DrawKeypoints(List<Accord.Point> keypoints, string fileName, Mat resizeMat)
    {
        var inputBitmap = BitmapConverter.ToBitmap(resizeMat);

        DrawCircles(inputBitmap, keypoints, 7, Brushes.Red, true);

        FileManager.WriteTempFile(fileName, inputBitmap, "keypoints");
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
            DrawText(g, text, position, size, brush);
        }
    }

    /// <summary>
    /// Draws the text.
    /// </summary>
    /// <param name="g">The g.</param>
    /// <param name="text">The text.</param>
    /// <param name="position">The position.</param>
    /// <param name="size">The size.</param>
    /// <param name="brush">The brush.</param>
    private static void DrawText(Graphics g, string text, Accord.Point position, int size, Brush brush = null)
    {
        if (brush == null)
        {
            brush = Brushes.Red;
        }

        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
        g.DrawString(text,
                     new Font("Tahoma", size),
                     brush,
                     new PointF((float)position.X, (float)position.Y));
    }

    /// <summary>
    /// Draws the line.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="line">The line.</param>
    public static void DrawLine(Bitmap image, Line line, Pen pen)
    {
        using (Graphics gr = Graphics.FromImage(image))
        {
            gr.SmoothingMode = SmoothingMode.AntiAlias;

            gr.DrawLine(pen, line.Point0.X, line.Point0.Y, line.Point1.X, line.Point1.Y);
            gr.DrawLine(pen, line.Point0.X + 1, line.Point0.Y, line.Point1.X + 1, line.Point1.Y);
            gr.DrawLine(pen, line.Point0.X - 1, line.Point0.Y, line.Point1.X - 1, line.Point1.Y);
            gr.DrawLine(pen, line.Point0.X, line.Point0.Y + 1, line.Point1.X, line.Point1.Y + 1);
            gr.DrawLine(pen, line.Point0.X, line.Point0.Y - 1, line.Point1.X, line.Point1.Y - 1);
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
