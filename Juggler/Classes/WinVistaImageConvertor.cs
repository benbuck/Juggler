namespace Juggler
{
    internal class WinVistaImageConvertor : ImageConvertor
    {
        internal override string GetImage(string path)
        {
            //The passed file name is JPG. Vista supports JPG wallpapers.
            return path;
        }
    }
}