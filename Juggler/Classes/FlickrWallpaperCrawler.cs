using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Juggler
{
    class FlickrWallpaperCrawler : WallpaperCrawler
    {
        protected override string GetImageName(Match match)
        {
            throw new System.NotImplementedException();
        }

        protected override string GetBrowsingUrl(int pageNo)
        {
            throw new System.NotImplementedException();
        }

        internal override Resolution[] GetResolutions()
        {
            //Create a list of different resolutions.
            List<Resolution> myResolutions = new List<Resolution>();
            int TotalScreenWidth, MaxScreenHeight;

            //Loop through each display and get the individual bounds
            Screen[] screens = System.Windows.Forms.Screen.AllScreens;
            foreach(System.Windows.Forms.Screen NewScreen in screens)
            {
                myResolutions.Add(new Resolution(NewScreen.Bounds.Width,NewScreen.Bounds.Height,ResolutionType.Any));
            }

            //Get the biggest size possible dependant on sum of widths and max height
            if (screens.Count() > 1)
            {
                TotalScreenWidth = screens.Sum(screen => screen.Bounds.Width);
                MaxScreenHeight = screens.Max(screen => screen.Bounds.Height);
                myResolutions.Add(new Resolution(TotalScreenWidth, MaxScreenHeight, ResolutionType.Any));
            }

            return myResolutions.ToArray();
        }

        internal override string[] GetSortOptions()
        {
            return new string[] { "N/A" };
        }
    }
}
