using System.Collections.Generic;
using System.Windows.Forms;

namespace Juggler
{
    internal static class ExtensionMethods
    {
        internal static ToolStripMenuItem Menu(this ToolStripItem[] menuItems, MenuIndex index)
        {
            return menuItems[(int)index] as ToolStripMenuItem;
        }

        internal static string FormattedTimePart(this int time, string unit)
        {
            return 1 > time ? string.Empty : (1 == time ? "1 " + unit : string.Format("{0} {1}s", time, unit));
        }
    }
}
