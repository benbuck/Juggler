using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Juggler
{
    internal class WinXpImageConvertor : ImageConvertor
    {
        internal override string GetImage(string path)
        {
            //Windows XP doesn't support JPG wallpapers. Save the image as BMP and return the path
            Bitmap img = new Bitmap(path);
            string bmpPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\JugglerWallpaper.bmp";

            img.Save(bmpPath, ImageFormat.Bmp);
            return bmpPath;
        }
    }
}