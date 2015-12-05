using System;
using System.Drawing;

namespace Juggler
{
    /// <summary>
    /// Wallpaper Convertor Base class. Derived classes will be for specific for OS
    /// </summary>
    internal abstract class ImageConvertor
    {
        internal abstract string GetImage(string path);
    }
}