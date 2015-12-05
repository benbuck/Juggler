using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Juggler.Properties;

namespace Juggler
{
    static class Program
    {
        //Declaration
        private static NotifyIcon appNotifyIcon;
        private static ToolStripItem[] menuItems;
        private static Timer Ticker { get; set; }
        public delegate int ThreadCallback(string Message, int Index);

        internal static bool IsVista { get; set; }
        internal static Regex IntervalPattern { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            IntervalPattern = new Regex(@"^([0-9]{1,3})\s{0,2}([MH])[a-z]{0,6}\s{0,2}([0-9]{0,2})\s{0,2}(M?)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

            //Code to check if the application is already running
            bool isFirstInstance = false;
            using (System.Threading.Mutex mut = new System.Threading.Mutex(true, "WallpaperJuggler-87EB3B4D-DAFE-4c3f-A286-13A3E0D40540", out isFirstInstance))
            {
                if (isFirstInstance)
                {
                    InitializeApp();
                    //Start listener over names pipes to recieve notification about second instance.
                    MultiInstanceHandler.StartListner();
                    Application.ApplicationExit +=
                        delegate
                        {
                            //Stop listener when application exits.
                            MultiInstanceHandler.StopListner();
                        };
                    Application.Run();
                }
                else
                {
                    //Notify the existing instance regarding second instance and exit.
                    MultiInstanceHandler.NotifyExistingInstanxce();
                    Application.Exit();
                }
            }

        }

        private static void InitializeApp()
        {
            //Check if running in Vista
            if (Environment.OSVersion.Version.Major == 6)
            {
                //Vista
                IsVista = true;
            }
            else
            {
                //XP, may be earlier
                IsVista = false;
            }

            Ticker = new Timer();
            SetTickerInterval(Settings.Default);
            Ticker.Tick +=
                delegate
                {
                    ChangeWallpaper();
                };

            appNotifyIcon = new NotifyIcon();
            appNotifyIcon.ContextMenuStrip = new ContextMenuStrip();

            menuItems = new ToolStripItem[] 
                {
                    //new ToolStripMenuItem("Last Updated Wallpaper:"),
                    new ToolStripMenuItem("(none)",
                        null, 
                        delegate
                        { 
                            string lastWallpaper = Settings.Default.LastWallpaper;
                            if (File.Exists(lastWallpaper))
                            {
                                ProcessStartInfo proc = new ProcessStartInfo("explorer.exe");
                                proc.WindowStyle = ProcessWindowStyle.Maximized;
                                proc.Arguments = "/e, /select,\"" + lastWallpaper + "\"";

                                Process.Start(proc);
                            }
                            else
                            {
                                ShowBalloonTip("Last changed wallpaper [" + lastWallpaper + "] could not be located!");
                            }
                        }),
                    new ToolStripMenuItem("Juggle Now", 
                        null, 
                        delegate 
                        { 
                            ChangeWallpaper(); 
                        }),
                    new ToolStripMenuItem("Delete Current Wallpaper", 
                        null, 
                        delegate 
                        { 
                            string lastWallpaper = Settings.Default.LastWallpaper;

                            if(MessageBox.Show("Are you sure you want to delete current wallpaper? You will not be able to recover it once deleted." + Environment.NewLine + Environment.NewLine + "Wallpaper: " + lastWallpaper,
                                "Confirm Deletion",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question) == DialogResult.Yes)
                            {
                                ChangeWallpaper();

                                if (File.Exists(lastWallpaper))
                                    {
                                        try
                                        {
                                            File.Delete(lastWallpaper);
                                        }
                                        catch(Exception ex)
                                        {
                                            ShowBalloonTip("Could not delete wallpaper. Error: " + ex.Message);
                                        }                                        
                                    }
                                else
                                {
                                    ShowBalloonTip("Last changed wallpaper [" + lastWallpaper + "] could not be located!");
                                }
                            }
                        }),
                    new ToolStripMenuItem("Active",
                        null, 
                        delegate { 
                            bool active = !menuItems.Menu(MenuIndex.Active).Checked;
                            Settings.Default.Active = active;
                            Settings.Default.Save();

                            SetActiveMenu(active); 
                        }),
                    new ToolStripMenuItem("Preferences", 
                        null, 
                        delegate 
                        { 
                            ShowWindow(PreferencesForm.Instance);
                        }),
                    new ToolStripMenuItem("Exit", 
                        null, 
                        delegate 
                        { 
                            AppExit();
                        })
                };

            //menuItems.Menu(MenuIndex.LastUpdCaption).Enabled = false;
            menuItems.Menu(MenuIndex.LastWallpaper).Enabled = false;
            menuItems.Menu(MenuIndex.Delete).Enabled = false;
            menuItems.Menu(MenuIndex.LastWallpaper).Font = new Font(appNotifyIcon.ContextMenuStrip.Font, FontStyle.Bold);
            menuItems.Menu(MenuIndex.Active).Checked = Settings.Default.Active;

            appNotifyIcon.ContextMenuStrip.Items.AddRange(menuItems);
            appNotifyIcon.Icon = Properties.Resources.Picture;
            appNotifyIcon.Visible = true;
            appNotifyIcon.MouseDoubleClick +=
                delegate(object sender, MouseEventArgs e)
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        ChangeWallpaper();
                    }
                };
            SetIconText();
        }

        private static void AppExit()
        {
            appNotifyIcon.Visible = false;
            appNotifyIcon.Dispose();
            Application.Exit();
        }

        private static void ShowWindow(Form win)
        {
            win.Icon = Properties.Resources.Picture;
            SetWindowPosition(win);
            win.Show();
            win.Activate();
        }

        internal static void ChangeWallpaper()
        {
            Wallpaper changer = new Wallpaper();
            if (!changer.Juggle())
            {
                ShowBalloonTip("Juggler couldn't change the wallpaper.\n\nPlease use 'Preferences' dialog to configure wallpaper directory and make sure there is at least one wallpaper in there.");//\n\nYou can use 'Download Wallpapers' dialog to get new wallpapers.");
            }
            SetIconText();
        }

        internal static void SetActiveMenu(bool checkedFlag)
        {
            menuItems.Menu(MenuIndex.Active).Checked = checkedFlag;
            SetIconText();
        }

        internal static void ShowBalloonTip(string message)
        {
            appNotifyIcon.ShowBalloonTip(5000, "Juggler", message, ToolTipIcon.Info);
        }

        internal static void SetIconText()
        {
            //Reset timer
            Ticker.Stop();
            Ticker.Enabled = false;

            string tooltip = string.Empty;
            string lastWallpaper = Settings.Default.LastWallpaper;

            if (Settings.Default.Active)
            {
                Ticker.Enabled = true;
                Ticker.Start();
                tooltip = "Next juggle at " + DateTime.Now.AddMilliseconds(Ticker.Interval).ToString("hh:mm:ss tt, dd MMM");
            }
            else
            {
                tooltip = "Not Active";
            }

            //Update tooltip
            appNotifyIcon.Text = "Wallpaper Juggler\n\n" + tooltip;

            menuItems.Menu(MenuIndex.LastWallpaper).Text = (lastWallpaper.Length < 1 ? "(none)" : lastWallpaper.Substring(lastWallpaper.LastIndexOf('\\') + 1));
            if (lastWallpaper.Length > 0)
            {
                menuItems.Menu(MenuIndex.LastWallpaper).Enabled = true;
                menuItems.Menu(MenuIndex.Delete).Enabled = true;
            }

        }

        internal static void SetTickerInterval(Settings settings)
        {
            int minutes = GetIntervalInMiutes(settings.Interval);

            if (!IsValidInterval(minutes))
            {
                minutes = 30;
                settings.Interval = "30 Mins";
            }
            else
            {
                string hrsPart = (minutes / 60).FormattedTimePart("Hr"), minPart = (minutes % 60).FormattedTimePart("Min");

                if (hrsPart.Length > 0 && minPart.Length > 0)
                {
                    settings.Interval = hrsPart + " " + minPart;
                }
                else
                {
                    settings.Interval = hrsPart.Length > 0 ? hrsPart : minPart;
                }

            }
            settings.Save();

            Ticker.Interval = 1000 * 60 * minutes;
        }

        internal static bool IsValidInterval(string interval)
        {
            return IsValidInterval(GetIntervalInMiutes(interval));
        }

        private static bool IsValidInterval(int interval)
        {
            return interval > 0 && interval / 60 < 500;
        }

        internal static int GetIntervalInMiutes(string interval)
        {
            int hours = 0, mins = 0;

            Match match = IntervalPattern.Match(interval);
            if (match.Success)
            {
                if (match.Groups[2].Value.ToUpper().Equals("M"))
                {
                    mins = int.Parse(match.Groups[1].Value);
                }
                else
                {
                    hours = int.Parse(match.Groups[1].Value);
                    if (!string.IsNullOrEmpty(match.Groups[4].Value) && match.Groups[4].Value.ToUpper().Equals("M") && !string.IsNullOrEmpty(match.Groups[3].Value))
                    {
                        mins = int.Parse(match.Groups[3].Value);
                    }
                }
            }
            return hours * 60 + mins;
        }

        private static void SetWindowPosition(Form win)
        {
            win.Top = Screen.PrimaryScreen.WorkingArea.Height - win.Height - 10;
            win.Left = Screen.PrimaryScreen.WorkingArea.Width - win.Width - 10;
        }
    }
}