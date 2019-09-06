using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using TennisHighlights;

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
    public static Mat BuildCircleMaskMat(int size)
    {
        var radius = size / 2d;
        var circle = new Mat(size, size, MatType.CV_8U, new Scalar(0));

        for (int i = 0; i < size; i++)
        {
            var x = -radius + 2 * radius * i / size;

            for (int j = 0; j < size; j++)
            {
                var y = -radius + 2 * radius * j / size;

                if (Math.Pow(x, 2) + Math.Pow(y, 2) <= Math.Pow(radius, 2))
                {
                    circle.Set<int>(i, j, 1);
                }
            }
        }

        return circle;
    }


    private static Bitmap Blur(Bitmap image, Rectangle rectangle, Int32 blurSize)
    {
        Bitmap blurred = new Bitmap(image.Width, image.Height);

        // make an exact copy of the bitmap provided
        using (Graphics graphics = Graphics.FromImage(blurred))
            graphics.DrawImage(image, new Rectangle(0, 0, image.Width, image.Height),
                new Rectangle(0, 0, image.Width, image.Height), GraphicsUnit.Pixel);

        // look at every pixel in the blur rectangle
        for (int xx = rectangle.X; xx < rectangle.X + rectangle.Width; xx++)
        {
            for (int yy = rectangle.Y; yy < rectangle.Y + rectangle.Height; yy++)
            {
                int avgR = 0, avgG = 0, avgB = 0;
                int blurPixelCount = 0;

                // average the color of the red, green and blue for each pixel in the
                // blur size while making sure you don't go outside the image bounds
                for (int x = xx; (x < xx + blurSize && x < image.Width); x++)
                {
                    for (int y = yy; (y < yy + blurSize && y < image.Height); y++)
                    {
                        Color pixel = blurred.GetPixel(x, y);

                        avgR += pixel.R;
                        avgG += pixel.G;
                        avgB += pixel.B;

                        blurPixelCount++;
                    }
                }

                avgR = avgR / blurPixelCount;
                avgG = avgG / blurPixelCount;
                avgB = avgB / blurPixelCount;

                // now that we know the average for the blur size, set each pixel to that color
                for (int x = xx; x < xx + blurSize && x < image.Width && x < rectangle.Width; x++)
                    for (int y = yy; y < yy + blurSize && y < image.Height && y < rectangle.Height; y++)
                        blurred.SetPixel(x, y, Color.FromArgb(avgR, avgG, avgB));
            }
        }

        return blurred;
    }

    /// <summary>
    /// Resizes the image.
    /// </summary>
    /// <param name="image">The image.</param>
    /// <param name="width">The width.</param>
    /// <param name="height">The height.</param>
    public static Bitmap ResizeImage(Image image, int width, int height) => ResizeImage(image, width, height, Rectangle.Empty, null);

    /// <summary>
    /// Resize the image to the specified width and height. If this is called a lot, it's better to supply rectangle and bitmap in order to avoid
    /// memory allocation.
    /// </summary>
    /// <param name="image">The image to resize.</param>
    /// <param name="width">The width to resize to.</param>
    /// <param name="height">The height to resize to.</param>
    /// <returns>The resized image.</returns>
    public static Bitmap ResizeImage(Image image, int width, int height, Rectangle destRect, Bitmap destImage = null)
    {
        if (destRect != Rectangle.Empty)
        {
            destRect = new Rectangle(0, 0, width, height);
        }
        if (destImage == null)
        {
            destImage = new Bitmap(width, height, image.PixelFormat);
        }

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighSpeed;
            graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
            graphics.SmoothingMode = SmoothingMode.HighSpeed;
            graphics.PixelOffsetMode = PixelOffsetMode.HighSpeed;

            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }

        return destImage;
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

    /// <summary>
    /// Fasts the get pixels.
    /// </summary>
    /// <param name="processedBitmap">The processed bitmap.</param>
    public static int[][][] FastGetPixels(Bitmap processedBitmap)
    {
        var bitmapData = processedBitmap.LockBits(new Rectangle(0, 0, processedBitmap.Width, processedBitmap.Height), ImageLockMode.ReadWrite, processedBitmap.PixelFormat);

        var bytesPerPixel = Bitmap.GetPixelFormatSize(processedBitmap.PixelFormat) / 8;
        var byteCount = bitmapData.Stride * processedBitmap.Height;
        var pixels = new byte[byteCount];
        var ptrFirstPixel = bitmapData.Scan0;
        Marshal.Copy(ptrFirstPixel, pixels, 0, pixels.Length);
        var heightInPixels = bitmapData.Height;
        var widthInBytes = bitmapData.Width * bytesPerPixel;

        var colors = new int[heightInPixels][][];

        for (int y = 0; y < heightInPixels; y++)
        {
            colors[y] = new int[bitmapData.Width][];

            int currentLine = y * bitmapData.Stride;

            var trueX = 0;
            for (int x = 0; x < widthInBytes; x = x + bytesPerPixel)
            {
                var oldBlue = pixels[currentLine + x];
                var oldGreen = pixels[currentLine + x + 1];
                var oldRed = pixels[currentLine + x + 2];

                colors[y][trueX] = new int[] { oldRed, oldGreen, oldBlue };

                trueX++;
            }
        }

        processedBitmap.UnlockBits(bitmapData);

        return colors;
    }

    /*
    public static FastReadBMP()
    {
        Bitmap bmp = new Bitmap("SomeImage");

        // Lock the bitmap's bits.  
        Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
        BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

        // Get the address of the first line.
        IntPtr ptr = bmpData.Scan0;

        // Declare an array to hold the bytes of the bitmap.
        int bytes = bmpData.Stride * bmp.Height;
        byte[] rgbValues = new byte[bytes];
        byte[] r = new byte[bytes / 3];
        byte[] g = new byte[bytes / 3];
        byte[] b = new byte[bytes / 3];

        // Copy the RGB values into the array.
        Marshal.Copy(ptr, rgbValues, 0, bytes);

        int count = 0;
        int stride = bmpData.Stride;

        for (int column = 0; column < bmpData.Height; column++)
        {
            for (int row = 0; row < bmpData.Width; row++)
            {
                b[count] = (byte)(rgbValues[(column * stride) + (row * 3)]);
                g[count] = (byte)(rgbValues[(column * stride) + (row * 3) + 1]);
                r[count++] = (byte)(rgbValues[(column * stride) + (row * 3) + 2]);
            }
        }
    }/*

    /*
    public static void DrawEllipse(Texture2D texture, Point2f center, Size2f radius, float rotationAngle, int resolution, Color color, int thickness)
    {
        for (int i = 0; i < thickness; i++)
        {
            DrawEllipse(texture, center, new Size2f(radius.Width + i, radius.Height + i * (radius.Height / radius.Width)), rotationAngle, resolution, color);
        }
    }

    private static void DrawEllipse(Texture2D texture, Point2f center, Size2f radius, float rotationAngle, int resolution, Color color)
    {
        var points = GetEllipsePoints(center, radius, rotationAngle, resolution);

        foreach (var point in points)
        {
            texture.SetPixel((int)point.x, (int)point.y, color);
        }

        texture.Apply();
    }

    /// <summary>
    /// Draws the circles on texture.
    /// </summary>
    /// <param name="texture">The texture.</param>
    /// <param name="detectedBalls">The detected balls.</param>
    /// <param name="ballsRadius">The balls radius.</param>
    public static void DrawCirclesOnTexture(Texture2D texture, List<Vector3> detectedBalls, List<float> ballsRadius, Color? color = null)
    {
        if (texture == null) { return; }

        if (color == null) { color = Color.red; }

        if (detectedBalls != null)
        {
            var i = 0;
            foreach (var ball in detectedBalls)
            {
                var trueY = ball.y;

                try
                {
                    texture.DrawCircle((int)ball.x, (int)trueY, (int)ballsRadius[i] - 1, color.Value);
                    texture.DrawCircle((int)ball.x, (int)trueY, (int)ballsRadius[i], color.Value);
                    texture.DrawCircle((int)ball.x, (int)trueY, (int)ballsRadius[i] + 1, color.Value);
                }
                catch
                {
                    Debug.Log("ERROR: " + texture == null + " " + ball + " " + ballsRadius[i] + " " + color + " " + trueY);
                }

                i++;
            }

            texture.Apply();
        }
    }*/
}
