using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Juggler.Properties;
using Microsoft.Win32;

namespace Juggler {
    internal class Wallpaper {
        private Settings settings;
        private Random rnd;
        ImageConvertor imageConvertor;

        internal enum Fit : int { FullScreen, LetterBox, PillarBox }

        public Wallpaper()
        {
            settings = Settings.Default;
            rnd = new Random();
            if (Program.IsVista)
            {
                imageConvertor = new WinVistaImageConvertor();
            }
            else
            {
                imageConvertor = new WinXpImageConvertor();
            }
        }

        /// <summary>
        /// Find an image and call the API functions
        /// </summary>
        public bool Juggle() {
            // Find an image file
            string path = DetermineImageFile();

            if (path == null || path.Length < 1) {
                return false;
            }
            else {
                // Set the image
                SetWallpaper(path, (Style)settings.Positioning);
            }

            // Save the settings
            settings.Save();

            return true;
        }

        /// <summary>
        /// Grab list of files, filter out bad choices, return good one randomly
        /// </summary>
        /// <returns></returns>
        private string DetermineImageFile() {
            // Specify top-level or sub-folders
            SearchOption opt = SearchOption.TopDirectoryOnly;
            if (settings.SubFolders)
            {
                opt = SearchOption.AllDirectories;
            }

            // Grab complete list of files from all folders
            List<string> wallpapers = new List<string>();
            foreach (string folder in settings.Folders.Split(new char[] { '|' }))
            {
                if (folder.Length == 0 || !Directory.Exists(folder)) continue;
                wallpapers.AddRange(Directory.GetFiles(folder, "*.jpg", opt));
            }

            // Make sure there is at least one wallpaper
            if (wallpapers.Count == 0) return null;

            // Randomly grab a file
            string filename = wallpapers[rnd.Next(wallpapers.Count)];

            // Remember last image shown
            settings.LastWallpaper = filename;

            return filename;
        }

        #region Constants
        /// <summary>
        /// Registry key for specifying the wallpaper style.
        /// </summary>
        private const string WallpaperStyle = @"WallpaperStyle";
        /// <summary>
        /// Registry key for specifying the wallpaper tiles.
        /// </summary>
        private const string Tile = @"TileWallpaper";
        /// <summary>
        /// The subkey to set the desktop key.
        /// </summary>
        private const string SubKey = @"Control Panel\Desktop";
        #endregion

        /// <summary>
        /// Set wallpaper and return status.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="style">The style.</param>
        /// <remarks>
        /// Set the desktop wallpaper, and notify the 
        /// system that it has changed.
        /// </remarks>
        public bool SetWallpaper(string path, Style style)
        {
            // override path for testing
            //path = "D:\\Wallpapers\\6a0105371bb32c970b014e89fdf603970d.jpg";

            // Check if the file existing.
            if (!System.IO.File.Exists(path))
                return false;

            RegistryKey key = Registry.CurrentUser.OpenSubKey(SubKey, true);

            switch (style)
            {
                case Style.FitToScreen:
                    key.SetValue(WallpaperStyle, "2");
                    key.SetValue(Tile, "0");
                    break;
                case Style.BestFit:
                    key.SetValue(WallpaperStyle,"0");
                    key.SetValue(Tile,"1");
                    break;
                case Style.Centered:
                    key.SetValue(WallpaperStyle, "1");
                    key.SetValue(Tile, "0");
                    break;
                case Style.Tiled:
                    key.SetValue(WallpaperStyle, "1");
                    key.SetValue(Tile, "1");
                    break;
            }

            if(style==Style.BestFit)
                path = resizeImageToBestFit(path,
                                   new Size(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                                            System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height));
            else
                path = imageConvertor.GetImage(path);
            

            return NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETDESKWALLPAPER, 0, path, NativeMethods.SPIF_UPDATEINIFILE | NativeMethods.SPIF_SENDWININICHANGE) != 0;
        }

        private static Double colorDistance(Color color1, Color color2)
        {
            int rDelta = color1.R - color2.R;
            int gDelta = color1.G - color2.G;
            int bDelta = color1.B - color2.B;
            return Math.Sqrt((rDelta * rDelta) + (gDelta * gDelta) + (bDelta * bDelta));
        }

        private static bool hasSolidEdges(Bitmap bi, Fit fit, out Color matteColor)
        {
            const UInt32 nEdgeSampleSize = 3;
            const Double nColorDistanceTolerance = 25.0;
            const float nColorMismatchTolerance = 0.10f;

            matteColor = Color.FromArgb(0, 0, 0);

            if (fit == Fit.LetterBox)
            {
                Color color;
                UInt32 count;

                // calculate average edge color
                count = 0;
                UInt64 redAccumulator = 0, greenAccumulator = 0, blueAccumulator = 0;
                for (Int32 x = 0; x < bi.Width; ++x)
                {
                    for (Int32 y = 0; (y < nEdgeSampleSize) && (y < bi.Height); ++y)
                    {
                        color = bi.GetPixel(x, y);
                        redAccumulator += color.R; greenAccumulator += color.G; blueAccumulator += color.B;
                        ++count;

                        color = bi.GetPixel(x, bi.Height - 1 - y);
                        redAccumulator += color.R; greenAccumulator += color.G; blueAccumulator += color.B;
                        ++count;
                    }
                }
                matteColor = Color.FromArgb(
                    (Int32)(redAccumulator / count),
                    (Int32)(greenAccumulator / count),
                    (Int32)(blueAccumulator / count));

                // check if there is too much deviation from the average
                count = 0;
                int mismatchCount = 0;
                for (Int32 x = 0; x < bi.Width; ++x)
                {
                    for (Int32 y = 0; (y < nEdgeSampleSize) && (y < bi.Height); ++y)
                    {
                        color = bi.GetPixel(x, y);
                        if (colorDistance(matteColor, color) >= nColorDistanceTolerance)
                            ++mismatchCount;
                        ++count;

                        color = bi.GetPixel(x, bi.Height - 1 - y);
                        if (colorDistance(matteColor, color) >= nColorDistanceTolerance)
                            ++mismatchCount;
                        ++count;
                    }
                }
                if (mismatchCount >= (count * nColorMismatchTolerance))
                    return false;
            }
            else if (fit == Fit.PillarBox)
            {
                Color color;
                UInt32 count;

                // calculate average edge color
                count = 0;
                UInt64 redAccumulator = 0, greenAccumulator = 0, blueAccumulator = 0;
                for (Int32 y = 0; y < bi.Height; ++y)
                {
                    for (Int32 x = 0; (x < nEdgeSampleSize) && (x < bi.Width); ++x)
                    {
                        color = bi.GetPixel(x, y);
                        redAccumulator += color.R; greenAccumulator += color.G; blueAccumulator += color.B;
                        ++count;

                        color = bi.GetPixel(bi.Width - 1 - x, y);
                        redAccumulator += color.R; greenAccumulator += color.G; blueAccumulator += color.B;
                        ++count;
                    }
                }
                matteColor = Color.FromArgb(
                    (Int32)(redAccumulator / count),
                    (Int32)(greenAccumulator / count),
                    (Int32)(blueAccumulator / count));

                // check if there is too much deviation from the average
                count = 0;
                int mismatchCount = 0;
                for (Int32 y = 0; y < bi.Height; ++y)
                {
                    for (Int32 x = 0; (x < nEdgeSampleSize) && (x < bi.Width); ++x)
                    {
                        color = bi.GetPixel(x, y);
                        if (colorDistance(matteColor, color) >= nColorDistanceTolerance)
                            ++mismatchCount;
                        ++count;

                        color = bi.GetPixel(bi.Width - 1 - x, y);
                        if (colorDistance(matteColor, color) >= nColorDistanceTolerance)
                            ++mismatchCount;
                        ++count;
                    }
                }
                if (mismatchCount >= (count * nColorMismatchTolerance))
                    return false;
            }

            return true;
        }

        private static Color pickMatteColor(Bitmap bi)
        {
            UInt32 red = 0, green = 0, blue = 0, pixels = 0;
            Color color;
            Int32 subdivisions = 256;
            Int32 yIncrement = (bi.Height > subdivisions) ? bi.Height / subdivisions : 1;
            Int32 xIncrement = (bi.Width > subdivisions) ? bi.Width / subdivisions : 1;
            for (Int32 y = 0; y < bi.Height; y += yIncrement)
            {
                for (Int32 x = 0; x < bi.Width; x += xIncrement)
                {
                    color = bi.GetPixel(x, y);
                    red += color.R;
                    green += color.G;
                    blue += color.B;
                    ++pixels;
                }
            }
            // a straight average looks aesthetically too bright, so tone it down a bit
            float toneFactor = 0.5f;
            return Color.FromArgb(
                (Int32)(red / pixels * toneFactor),
                (Int32)(green / pixels * toneFactor),
                (Int32)(blue / pixels * toneFactor));
        }

        /// <summary>
        /// Resizes a given file path with aspect-ratio maintained for given file within given size. returns new path.
        /// </summary>
        /// <param name="path">The file path to the image.</param>
        /// <param name="size">The size to resize to (maximum).</param>
        /// <remarks>
        /// Resizes image to best aspect-ratio compliant size, 
        /// resizes the image, saves it and returns the path.
        /// </remarks>
        private static string resizeImageToBestFit(string path, Size size)
        {
            //If the file is corrupt it throws an Out Of Mem Exception
            Image imgToResize;
            try
            {
                imgToResize = Image.FromFile(path);
            }
            catch(OutOfMemoryException)
            {
                return path;
            }

            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;

            //Calculate Percentage of change for both dimensions
            float nPercentW = ((float)size.Width / (float)sourceWidth);
            float nPercentH = ((float)size.Height / (float)sourceHeight);

            //float npw = nPercentW;
            //float nph = nPercentH;

            //Scale both dimensions to lowest Percentage
            //unless they are within a certain tolerance of each other
            Fit fit = Fit.FullScreen;
            float nScaleRatio = nPercentW / nPercentH;
            const float nScaleTolerance = 0.10f;
            const float nCropTolerance = 0.25f;
            if ((nScaleRatio < (1.0f - nScaleTolerance)) || (nScaleRatio > (1.0f + nScaleTolerance)))
            {
                if ((nScaleRatio >= (1.0f - nCropTolerance)) && (nScaleRatio <= (1.0f + nCropTolerance)))
                {
                    if (nPercentH < nPercentW)
                        nPercentH = nPercentW;
                    else
                        nPercentW = nPercentH;
                }
                else
                {
                    if (nPercentH < nPercentW)
                    {
                        fit = Fit.PillarBox;
                        nPercentW = nPercentH;
                    }
                    else
                    {
                        fit = Fit.LetterBox;
                        nPercentH = nPercentW;
                    }
                }
            }

            //Calculate dimensions of resized image
            int destWidth = (int)(sourceWidth * nPercentW);
            int destHeight = (int)(sourceHeight * nPercentH);

            //Create new bitmap and graphics
            Bitmap b = new Bitmap(size.Width, size.Height);
            Graphics g = Graphics.FromImage((Image)b);

            //Calculate border color based on image contents
            bool solidEdges = false;
            if (fit != Fit.FullScreen)
            {
                Bitmap bi = new Bitmap(imgToResize);

                Color matteColor;
                solidEdges = hasSolidEdges(bi, fit, out matteColor);
                if (solidEdges)
                {
                    g.Clear(matteColor);
                }
                else
                {
                    matteColor = pickMatteColor(bi);
                    g.Clear(matteColor);
                }

                bi.Dispose();
            }

            //Center the image and draw it
            int left = (size.Width - destWidth)/2;
            int top = (size.Height - destHeight)/2;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            g.DrawImage(imgToResize, left, top, destWidth, destHeight);

            //System.Drawing.Font font = new System.Drawing.Font("Arial", 12);
            //System.Drawing.SolidBrush white = new System.Drawing.SolidBrush(System.Drawing.Color.White);
            //System.Drawing.SolidBrush black = new System.Drawing.SolidBrush(System.Drawing.Color.Black);
            //string str;
            //str = String.Format("Scale Factor W: {0:f} -> {1:f}", npw, nPercentW);
            //g.DrawString(str, font, black, 102, 102);
            //g.DrawString(str, font, white, 100, 100);
            //str = String.Format("Scale Factor H: {0:f} -> {1:f}", nph, nPercentH);
            //g.DrawString(str, font, black, 102, 122);
            //g.DrawString(str, font, white, 100, 120);
            //str = String.Format("Scale Ratio: {0:f}", nScaleRatio);
            //g.DrawString(str, font, black, 102, 142);
            //g.DrawString(str, font, white, 100, 140);
            //str = String.Format("Fit: {0}", fit);
            //g.DrawString(str, font, black, 102, 162);
            //g.DrawString(str, font, white, 100, 160);
            //str = String.Format("SolidEdges: {0}", solidEdges);
            //g.DrawString(str, font, black, 102, 182);
            //g.DrawString(str, font, white, 100, 180);

            //Save the image.  JPG for Vista, BMP for XP
            string newPath;
            if (Program.IsVista)
            {
                newPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                          @"\JugglerWallpaper.jpg";
                b.Save(newPath, ImageFormat.Jpeg);
            }
            else
            {
                newPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                          @"\JugglerWallpaper.bmp";
                b.Save(newPath, ImageFormat.Bmp);
            }

            g.Dispose();
            b.Dispose();
            imgToResize.Dispose();

            return newPath;
        }
    }
}
