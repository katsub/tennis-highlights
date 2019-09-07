using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace TennisHighlights
{
    /// <summary>
    /// The file manager
    /// </summary>
    public class FileManager
    {
        /// <summary>
        /// The rally folder
        /// </summary>
        public const string RallyFolder = "rally";

        /// <summary>
        /// The frame folder
        /// </summary>
        public const string FrameFolder = "frame";

        /// <summary>
        /// The rally videos folder
        /// </summary>
        public const string RallyVideosFolder = "rallyVideos";

        /// <summary>
        /// The temporary data path
        /// </summary>
        public static string TempDataPath => _settings.TempDataPath + "\\Temp\\";
        /// <summary>
        /// The persistent data path
        /// </summary>
        public static string PersistentDataPath => _settings.TempDataPath + "\\Persistent\\";
        /// <summary>
        /// The settings
        /// </summary>
        private static GeneralSettings _settings;

        /// <summary>
        /// Generals the settings.
        /// </summary>
        /// <returns></returns>
        public static void Initialize(GeneralSettings settings)
        {
            _settings = settings;

            try
            {
                Directory.CreateDirectory(PersistentDataPath);
                Directory.CreateDirectory(TempDataPath);

                var testFile = PersistentDataPath + "test.txt";

                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (Exception e)
            {
                Logger.Instance.Log(LogType.Error, "Could not write or create file in temp data path: " + TempDataPath 
                                                   + ". Ensure that parameter is properly set in the settings.xml and new files can be created in that location.\n" + e.ToString()); 
            }
        }

        /// <summary>
        /// Cleans this instance.
        /// </summary>
        public static void Clean()
        {
            try
            {
                Logger.Instance.Log(LogType.Information, "Cleaning folders...");

                if (_settings.RegenerateFrames)
                {
                    CleanFolder(FrameFolder);
                }

                CleanFolder(RallyFolder);
                //CleanFolder(RallyVideosFolder);

                Logger.Instance.Log(LogType.Information, "Cleaned.");
            }
            catch { }
        }

        public static void DeleteFolder(string folderName)
        {
            try
            {
                var folderPath = TempDataPath + "\\" + folderName;

                Directory.Delete(folderPath);
            }
            catch { }
        }

        public static string GetUnusedFilePathInFolderFromFileName(string filePath, string folder, string newExtension)
        {
            if (!folder.EndsWith("//"))
            {
                folder += "//";
            }

            var i = 0;

            var fileName = new FileInfo(filePath).Name;

            fileName = fileName.Substring(0, fileName.Length - 4);

            string getUnusedPath() => folder + fileName + (i == 0 ? "" : ("_" + i)) + newExtension;

            var unusedPath = getUnusedPath();

            while (File.Exists(unusedPath))
            {
                i++;

                unusedPath = getUnusedPath();
            }

            return unusedPath;
        }

        /// <summary>
        /// Cleans the folder.
        /// </summary>
        /// <param name="folderName">Name of the folder.</param>
        public static void CleanFolder(string folderName)
        {
            try
            {
                var folderPath = TempDataPath + "\\" + folderName;

                if (Directory.Exists(folderPath))
                {
                    foreach (var file in Directory.GetFiles(folderPath))
                    {
                        File.Delete(file);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Writes the temporary file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="textData">The text data.</param>
        public static void WriteTempFile(string fileName, string textData) => File.WriteAllText(TempDataPath + fileName, textData);

        /// <summary>
        /// Writes the temporary file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="data">The data.</param> 
        public static void WriteTempFile(string fileName, byte[] data) => File.WriteAllBytes(TempDataPath + fileName, data);

        /// <summary>
        /// The JPEG encoder
        /// </summary>
        private static ImageCodecInfo _jpegEncoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        private static EncoderParameters _jpegEncParams;
        /// <summary>
        /// Gets the encoder parameters.
        /// </summary>
        private static EncoderParameters JPEGEncoderParameters
        {
            get
            {
                if (_jpegEncParams == null)
                {
                    _jpegEncParams = new EncoderParameters(1);
                    _jpegEncParams.Param[0] = new EncoderParameter(Encoder.Quality, 30L);
                }

                return _jpegEncParams;
            }
        }

        /// <summary>
        /// Writes the temporary file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="data">The data.</param>
        public static void WriteTempFile(string fileName, Bitmap data, string folder = "")
        {
            var folderPath = !string.IsNullOrEmpty(folder) ? Path.Combine(TempDataPath, folder + "\\") : TempDataPath;

            if (!string.IsNullOrEmpty(folder) && !Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            var filePath = folderPath + fileName;

            if (File.Exists(filePath)) { File.Delete(filePath); }

            if (filePath.EndsWith(".jpg"))
            {
                data.Save(filePath, _jpegEncoder, JPEGEncoderParameters);
            }
            else
            {
                data.Save(filePath);
            }
        }

        /// <summary>
        /// Reads the temporary bitmap file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public static Bitmap ReadTempBitmapFile(string fileName, string folder = "")
        {
            var folderPath = !string.IsNullOrEmpty(folder) ? Path.Combine(TempDataPath, folder + "\\") : TempDataPath;

            var filePath = folderPath + fileName;

            if (File.Exists(filePath))
            {
                return new Bitmap(filePath);
            }

            return null;
        }

        /// <summary>
        /// Writes the temporary file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="data">The data.</param>
        /// <param name="folder">The folder.</param>
        public static void WriteTempFile(string fileName, Mat data, string folder)
        {
            using (var bitmap = BitmapConverter.ToBitmap(data))
            {
                WriteTempFile(fileName, bitmap, folder);
            }
        }

        /// <summary>
        /// Writes the persistent file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="data">The data.</param>
        public static void WritePersistentFile(string fileName, Bitmap data)
        {
            if (!Directory.Exists(PersistentDataPath))
            {
                Directory.CreateDirectory(PersistentDataPath);
            }

            data.Save(PersistentDataPath + fileName);
        }

        /// <summary>
        /// Writes the persistent file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="data">The data.</param>
        public static void WritePersistentFile(string fileName, Mat data)
        {
            if (!Directory.Exists(PersistentDataPath))
            {
                Directory.CreateDirectory(PersistentDataPath);
            }

            data.ImWrite(PersistentDataPath + fileName);
        }
        /// <summary>
        /// Writes the persistent file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="textData">The text data.</param>
        public static void WritePersistentFile(string fileName, string textData) => File.WriteAllText(PersistentDataPath + fileName, textData);

        /// <summary>
        /// Reads the persistent file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public static string ReadPersistentFile(string fileName)
        {
            var filePath = PersistentDataPath + fileName;

            if (File.Exists(filePath))
            {
                var file = File.ReadAllText(filePath);

                return file;
            }

            return string.Empty;
        }

        /// <summary>
        /// Reads the persistent file.
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public static Bitmap ReadPersistentBitmapFile(string fileName)
        {
            var filePath = PersistentDataPath + fileName;

            if (File.Exists(filePath))
            {
                return new Bitmap(filePath);
            }

            return null;
        }
    }
}
