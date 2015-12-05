using System;
using System.Reflection;
using System.Windows.Forms;
using Juggler.Properties;
using Microsoft.Win32;

namespace Juggler
{
    /// <summary>
    /// Settings form for configurtion.
    /// </summary>
    /// <remarks>
    /// This class implements Singleton pattern. This makes it real easy to serve user with only one 
    /// settings window even if user clicks on Settings menu multiple times.
    /// </remarks>
    public partial class PreferencesForm : Form
    {
        private const string startUpKeyName = @"Juggler";
        private static PreferencesForm instace;

        private PreferencesForm()
        {
            InitializeComponent();

            cancelLinkLabel.LinkClicked +=
                delegate
                {
                    this.Close();
                };

            foldersListBox.SelectedIndexChanged +=
                delegate
                {
                    removeLinkLabel.Enabled = foldersListBox.SelectedItems.Count > 0;
                };

            removeLinkLabel.LinkClicked +=
                delegate
                {
                    int[] selectedIndices = new int[foldersListBox.SelectedIndices.Count];
                    foldersListBox.SelectedIndices.CopyTo(selectedIndices, 0);
                    //Sort it descending as we are going to remove items. Indices change for tailing items if an item is removed in middle.
                    //The default Sort implementation will sort array ascending. We have to provide Comparison<T> to get it dscending.
                    //This is an example of complex lambda expression
                    Array.Sort<int>(selectedIndices,
                        (first, second) =>
                        {
                            if (first < second) { return 1; }
                            else if (second < first) { return -1; }
                            else { return 0; }
                        }
                    );

                    //Remove selected items. This is an example of simple lambda expression
                    Array.ForEach<int>(selectedIndices, index => foldersListBox.Items.RemoveAt(index));
                    removeLinkLabel.Enabled = false;
                };
        }

        public static PreferencesForm Instance
        {
            get
            {
                if (instace == null)
                {
                    instace = new PreferencesForm();
                    //Set instance to null when object gets disposed. This will save us from using non-null but disposed object.
                    instace.Disposed +=
                        delegate
                        {
                            instace = null;
                        };
                }
                return instace;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            Settings settings = Settings.Default;

            //Read settings and update UI
            foldersListBox.Items.Clear();
            foreach (string folder in settings.Folders.Split(new char[] { '|' }))
            {
                if (folder.Length == 0) continue;
                foldersListBox.Items.Add(folder);
            }
            subFoldersCheckBox.Checked = settings.SubFolders;
            autostartCheckBox.Checked = settings.AutoStart;
            activeCheckBox.Checked = settings.Active;
            if (Program.IsValidInterval(settings.Interval))
            {
                intervalComboBox.Text = settings.Interval;
            }
            else
            {
                intervalComboBox.SelectedIndex = 0;
            }
            positioningComboBox.SelectedIndex = settings.Positioning;
        }

        private void addLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            FolderBrowserDialog pictureFolderBrowserDialog = new FolderBrowserDialog();
            pictureFolderBrowserDialog.SelectedPath = foldersListBox.Items.Count > 0 ? foldersListBox.Items[foldersListBox.Items.Count - 1].ToString() : string.Empty;

            if (pictureFolderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folder = pictureFolderBrowserDialog.SelectedPath;
                if (!foldersListBox.Items.Contains(folder))
                {
                    foldersListBox.Items.Add(folder);
                }
            }
        }

        private void saveLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (-1 == intervalComboBox.SelectedIndex)
            {
                if (!Program.IsValidInterval(intervalComboBox.Text))
                {
                    if (DialogResult.Yes == MessageBox.Show("The wallpaper frequency you entered doesn't seem right. Please enter 1 min to 500 hrs in [000 Hrs/Mins 000 Mins] format. \n\nDo you want to use default frequency [30 Mins] and proceed?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2))
                    {
                        intervalComboBox.SelectedIndex = 0;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            Settings settings = Settings.Default;

            //Assign all settings
            string[] folders = new string[foldersListBox.Items.Count];
            foldersListBox.Items.CopyTo(folders, 0);
            settings.Folders = string.Join("|", folders);
            settings.SubFolders = subFoldersCheckBox.Checked;
            settings.AutoStart = autostartCheckBox.Checked;
            settings.Active = activeCheckBox.Checked;

            settings.Interval = intervalComboBox.Text;

            settings.Positioning = positioningComboBox.SelectedIndex;

            settings.Save();

            //Update Ticker Interval
            Program.SetTickerInterval(settings);
            //Update the system tray tooltip and reset the ticker interval
            Program.SetActiveMenu(settings.Active);

            //Set the autostart key in registry
            if (autostartCheckBox.Checked)
            {
                AddStartupItem(startUpKeyName, Assembly.GetExecutingAssembly().Location);
            }
            else
            {
                RemoveStartupItem(startUpKeyName);
            }

            this.Close();
        }

        #region Add/Remove startup item functions
        /// <summary>
        /// Adds the named item and its path to the Current User startup in registry.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="path"></param>
        public static void AddStartupItem(string name, string path)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);

            key.SetValue(name, path);
        }

        /// <summary>
        /// Removes the named item from Current User startup in registry.
        /// </summary>
        /// <param name="name"></param>
        public static void RemoveStartupItem(string name)
        {
            RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true);

            key.DeleteValue(name, false);
        }
        #endregion
    }
}