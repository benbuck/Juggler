using System.Runtime.InteropServices;

namespace Juggler
{
    internal static class NativeMethods
    {
        #region Constants
        /// <summary>
        /// Constant for setting the desktop wallpaper.
        /// </summary>
        internal const int SPI_SETDESKWALLPAPER = 20;
        /// <summary>
        /// Constant for notification of ini data.
        /// </summary>
        internal const int SPIF_UPDATEINIFILE = 0x01;
        /// <summary>
        /// Another windows ini change constant.
        /// </summary>
        internal const int SPIF_SENDWININICHANGE = 0x02;
        #endregion

        /// <summary>
        /// Import def for systemParametersInfo
        /// </summary>
        /// <param name="uAction">The action</param>
        /// <param name="uParam">First param</param>
        /// <param name="lpvParam">general string param</param>
        /// <param name="fuWinIni">Windows ini stuff.</param>
        /// <returns>Sucess or failure.</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        internal static extern int SystemParametersInfo(
            int uAction, int uParam, string lpvParam, int fuWinIni);
    }
}
