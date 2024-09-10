using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BuildStatusPanel.Model;

namespace BuildStatusPanel.Content
{
    /// <summary>
    /// Interaction logic for BuildStatusListView.xaml
    /// </summary>
    public partial class BuildStatusListView : ListView
    {
        public BuildStatusListView()
        {
            InitializeComponent();
        }

        #region Event handling
        /// <summary>
        /// when the QueueNewBuild is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void QueueNewBuild_Click(object sender, RoutedEventArgs e)
        {
            if ((SelectedItem != null) && (SelectedItem is UserBuildStatus))
            {
                UserBuildStatus build = SelectedItem as UserBuildStatus;
                if (build != null)
                {
                    MessageBoxResult res = MessageBox.Show(string.Format("Queue a new build for '{0}'?", build.BuildName), "Queue new build", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.Yes)
                        build.QueuNewBuild();
                }
            }
        }

        /// <summary>
        /// User clicked to add current build to favourites
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddToFavourites_Click(object sender, RoutedEventArgs e)
        {
            if ((SelectedItem != null) && (SelectedItem is UserBuildStatus))
            {
                string selectedBuildName = (SelectedItem as UserBuildStatus).BuildName;
                if (!MainWindow.DefaultSettings.FavouriteBuilds.Contains(selectedBuildName))
                {
                    MainWindow.DefaultSettings.FavouriteBuilds.Add(selectedBuildName);
                }
            }
        }

        /// <summary>
        /// User clicked to remove build to favourites
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveFromtFavourites_Click(object sender, RoutedEventArgs e)
        {
            if ((SelectedItem != null) && (SelectedItem is UserBuildStatus))
            {
                string selectedBuildName = (SelectedItem as UserBuildStatus).BuildName;
                if (MainWindow.DefaultSettings.FavouriteBuilds.Contains(selectedBuildName))
                {
                    MainWindow.DefaultSettings.FavouriteBuilds.Remove(selectedBuildName);
                }
            }
        }

        /// <summary>
        /// Copy the selected BuildDetails to the clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyBuildInfo_Click(object sender, RoutedEventArgs e)
        {
            if ((SelectedItem != null) && (SelectedItem is UserBuildStatus))
            {
                UserBuildStatus build = (SelectedItem as UserBuildStatus);
                string info = build.Information;
                System.Windows.Clipboard.SetText(info);
            }

        }
        /// <summary>
        /// The drop folder link was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DropFolderLink_Click(object sender, RoutedEventArgs e)
        {
            string path = string.Empty;
            try
            {
                Hyperlink link = sender as Hyperlink;
                if (link != null)
                {
                    path = link.NavigateUri.AbsolutePath;
                    Uri uri = link.NavigateUri;
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = uri.AbsoluteUri;
                    Process.Start(startInfo);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("Error loading path.\r\n{0}\r\n{1}", path, ex.Message));
            }
        }

        #endregion

        /// <summary>
        /// handles the Key up events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_KeyUp(object sender, KeyEventArgs e)
        {
            bool isCtrl = (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl));
            if (e != null) 
            {
                switch (e.Key)
                {
                    case (Key.F5):
                        Refresh(!isCtrl);
                        break;
                }
            }

        }

        /// <summary>
        /// Refresh the selected item or all
        /// </summary>
        public void Refresh(bool selectedOnly=true)
        {
            if (selectedOnly)
            {
                if((SelectedItem != null) && (SelectedItem is UserBuildStatus))
                {
                    UserBuildStatus build = (SelectedItem as UserBuildStatus);
                    if (build != null)
                    {
                        build.Refresh();
                    }
                }
            }
            else
            {
                foreach (var item in Items)
                {
                    if (item is UserBuildStatus)
                    {
                        UserBuildStatus build = (item as UserBuildStatus);
                        if (build != null)
                        {
                            build.Refresh();
                        }
                    }
                }
            }
        }
    }


}
