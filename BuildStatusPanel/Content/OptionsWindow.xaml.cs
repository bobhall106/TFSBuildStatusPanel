using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using BuildStatusPanel.Model;

namespace BuildStatusPanel.Content
{
    /// <summary>
    /// Interaction logic for OptionsWindow.xaml
    /// </summary>
    public partial class OptionsWindow : Window
    {
        public Settings UserSettings { get; set; }
        public ObservableCollection<string> AllBuildNames { get; set; }
        public ObservableCollection<string> FavouriteBuildNames { get; set; }
        static public ObservableCollection<string> ProjectNames { get; set; }
        public bool IsSaved { get; private set; }

        /// <summary>
        /// Main constructor for use
        /// </summary>
        /// <param name="allBuildNames"></param>
        public OptionsWindow(List<string> allBuildNames)
        {
            IsSaved = false;
            UserSettings =MainWindow.DefaultSettings.Clone() as Settings;
            AllBuildNames = new ObservableCollection<string>();
            FavouriteBuildNames = new ObservableCollection<string>();
            foreach (string build in allBuildNames)
            {
                AllBuildNames.Add(build);
            }

            foreach (string fave in UserSettings.FavouriteBuilds)
            {
                AllBuildNames.Remove(fave);
                FavouriteBuildNames.Add(fave);
            }
            FavouriteBuildNames= new ObservableCollection<string>(FavouriteBuildNames.OrderBy(s=>s.ToString()).ToList());
            AllBuildNames= new ObservableCollection<string>(AllBuildNames.OrderBy(s => s.ToString()).ToList());
            if (ProjectNames == null)
            {
                TFSBuildStatusHelper buildHelper = new TFSBuildStatusHelper();
                buildHelper.Init(UserSettings.TFSServerURIPath, UserSettings.DefaultProject);
                ProjectNames = new ObservableCollection<string>(buildHelper.GetProjects());
            }
            InitializeComponent();
            TFSProjects.SelectedValue = UserSettings.DefaultProject;
        }
        /// <summary>
        /// default ctor
        /// </summary>
        public OptionsWindow()
        {
            IsSaved = false;
            UserSettings =MainWindow.DefaultSettings.Clone() as Settings;
            InitializeComponent();
        }
        /// <summary>
        /// user clicked ok so save 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            UserSettings.FavouriteBuilds.Clear();
            foreach (string build in FavouriteBuildNames)
            {
                UserSettings.FavouriteBuilds.Add(build);
            }
            UserSettings.FavouriteBuilds.Sort();
            IsSaved = true;
            Close();
        }

        /// <summary>
        /// user is cancelling
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        /// <summary>
        /// User wants to add items to favourites
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddFavourite_Click(object sender, RoutedEventArgs e)
        {
            if (AllBuildsView.SelectedItems != null)
            {
                List<string> itemsToMove = new List<string>();
                foreach (var item in AllBuildsView.SelectedItems)
                {
                    if (item != null)
                    {
                        string buildName = item as string;
                        if (!string.IsNullOrEmpty(buildName))
                        {

                            itemsToMove.Add(item as string);
                        }
                    }
                }
                foreach (var item in itemsToMove)
                {
                    ((ObservableCollection<string>)AllBuildsView.ItemsSource).Remove(item);
                    ((ObservableCollection<string>)FavouritesBuildsView.ItemsSource).Add(item);
                }
            }

        }

        /// <summary>
        /// user wants to remove selected items from favourites
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveFavourite_Click(object sender, RoutedEventArgs e)
        {
            if (FavouritesBuildsView.SelectedItems != null)
            {
                List<string> itemsToMove = new List<string>();
                foreach (var item in FavouritesBuildsView.SelectedItems)
                {
                    if (item != null)
                    {
                        string buildName = item as string;
                        if (!string.IsNullOrEmpty(buildName))
                        {

                            itemsToMove.Add(item as string);
                        }
                    }
                }
                foreach (var item in itemsToMove)
                {
                    ((ObservableCollection<string>)FavouritesBuildsView.ItemsSource).Remove(item);
                    ((ObservableCollection<string>)AllBuildsView.ItemsSource).Add(item);
                }
            }
        }
    }
}
