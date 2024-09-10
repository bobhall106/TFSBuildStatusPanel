using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using BuildStatusPanel.Content;
using BuildStatusPanel.Model;
using Microsoft.TeamFoundation.Build.Client;

namespace BuildStatusPanel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string MY_FAVOURITES = "My Favourites";
        private DispatcherTimer _dispatcherTimer;
        private List<UserBuildStatus> _userBuildStatuses;
        private List<string> DynamicBranchNames = new List<string>();
        private List<string> AllBuildNames = new List<string>();

        #region Properties

        public static Settings DefaultSettings { get; set; }

        public bool IsAutoRefresh
        {
            get { return DefaultSettings.IsAutoRefresh; }
            set
            {
                DefaultSettings.IsAutoRefresh = value;
            }
        }

        public bool IsShowDisabled
        {
            get { return DefaultSettings.IsShowDisabled; }
            set
            {
                DefaultSettings.IsShowDisabled = value;
            }
        }

        public int AutoRefreshMins
        {
            get { return DefaultSettings.AutoRefreshMins; }
            set
            {
                DefaultSettings.AutoRefreshMins = value;
                UpdateIntervalText();
                ResetTimer();
            }
        }

        #endregion Properties

        #region CTOR

        /// <summary>
        /// Main constrctor
        /// </summary>
        public MainWindow()
        {
            Settings savedSettings = Settings.LoadSettings();
            DefaultSettings = savedSettings == null ? new Settings() : savedSettings;
            InitializeComponent();
            UpdateIntervalText();
            AutoRefreshCheckBox.IsChecked = DefaultSettings.IsAutoRefresh;
            ShowDisabledCheckBox.IsChecked = DefaultSettings.IsShowDisabled;
            PopulateBranches();
            GetBuildDefinitionsOnThread();
            CheckForNewVersion();
        }

        /// <summary>
        /// populate the branches combo
        /// </summary>
        private void PopulateBranches()
        {
            DynamicBranchNames.Clear();
            BranchName.Items.Clear();
            AddBranchNames();
            if (DefaultSettings.FavouriteBuilds.Count > 0)
                BranchName.SelectedIndex = 0;//faves
            else
                BranchName.SelectedIndex = 1;//first branch
        }

        #endregion CTOR

        /// <summary>
        /// Load some default branches to get us started and have them at the top of the list
        /// </summary>
        private void AddBranchNames()
        {
            ////have a default list of branches so we can find common ones easily at the top
            DynamicBranchNames.Add(MY_FAVOURITES);
            if (DefaultSettings.IsEBS)
            {
                DynamicBranchNames.Add("master");
                DynamicBranchNames.Add("main");
            }
            AddDynamicBranchNames();
        }

        /// <summary>
        /// Add in new branch names that have been added recently or are less important
        /// </summary>
        private void AddDynamicBranchNames(bool sort = false)
        {
            bool hasAdded = false;
            List<string> branches = new List<string>();
            branches.AddRange(DynamicBranchNames);
            if (sort)
                branches.Sort();
            foreach (string name in branches)
            {
                if (!BranchName.Items.Contains(name))
                {
                    //only add the branch if it doesn't exist already
                    BranchName.Items.Add(name);
                    hasAdded = true;
                }
            }
            if (hasAdded)
                BranchName.UpdateLayout();
        }

        /// <summary>
        /// Get the BuildDefs on a new thread
        /// </summary>
        /// <param name="rootItem"></param>
        private void CheckForNewVersion()
        {
            Thread nodeThread = new Thread(() => ThreadCheckForNewVersion());
            nodeThread.Start();
        }

        /// <summary>
        /// Thread to check if there is a new version available
        /// </summary>
        private void ThreadCheckForNewVersion()
        {
            try
            {
                TFSBuildStatusHelper buildHelper = new TFSBuildStatusHelper();
                buildHelper.Init(DefaultSettings.TFSServerURIPath, DefaultSettings.DefaultProject);
                string comments = buildHelper.GetThisAppChangesetComments(DefaultSettings.TFSHomeForApp, DefaultSettings.CheckedInAfterDate, 20, false);
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    VersionUpgradeAlertBox.ToolTip = comments;
                    VersionUpgradeAlertBox.Visibility = string.IsNullOrEmpty(comments) ? Visibility.Hidden : Visibility.Visible;
                }));
            }
            catch
            {
            }
        }

        /// <summary>
        /// Get the BuildDefs on a new thread
        /// </summary>
        /// <param name="rootItem"></param>
        private void GetBuildDefinitionsOnThread()
        {
            string branchName = BranchName.Text;
            Thread nodeThread = new Thread(() => ThreadBuildDefinitionsList(branchName, IsShowDisabled));
            nodeThread.Start();
        }

        /// <summary>
        /// Thread to get the list of valid TFS users and populate the combo with them
        /// </summary>
        private void ThreadBuildDefinitionsList(string branchName, bool showDisabled)
        {
            try
            {
                if (_userBuildStatuses != null)
                {
                    //detach events as we don't care anymore
                    foreach (UserBuildStatus bs in _userBuildStatuses)
                    {
                        bs.OnBuildsUpdated -= UserStatus_OnBuildsUpdated;
                    }
                }
                bool hasNewDynamicBranches = false;
                TFSBuildStatusHelper buildHelper = new TFSBuildStatusHelper();
                buildHelper.Init(DefaultSettings.TFSServerURIPath, DefaultSettings.DefaultProject);
                buildHelper.LoadBuildDefinitions();
                //find dynamic branches
                AllBuildNames.Clear();
                var buildNames = (from def in buildHelper.BuildDefinitions where (!string.IsNullOrEmpty(def.Name)) select def.Name).ToList<string>();
                AllBuildNames.AddRange(buildNames);
                AllBuildNames.Sort();
                StringComparer invICCmp = StringComparer.InvariantCultureIgnoreCase;//IEqualityComparer string comparer to ignore case
                var extraBranches = (from def in buildHelper.BuildDefinitions
                                     where !DynamicBranchNames.Contains((def.Name.Contains(DefaultSettings.BranchGroupingDelimiter) ? def.Name.Split(DefaultSettings.BranchGroupingDelimiter)[0] : def.Name).ToLower(), invICCmp)
                                     select def)
                                     .ToList();
                foreach (IBuildDefinition buildDef in extraBranches)
                {
                    string buildBranchName = buildDef.Name.Contains(DefaultSettings.BranchGroupingDelimiter) ? buildDef.Name.Split(DefaultSettings.BranchGroupingDelimiter)[0] : buildDef.Name;
                    if (!DynamicBranchNames.Contains(buildBranchName, invICCmp))
                    {
                        DynamicBranchNames.Add(buildBranchName);
                        hasNewDynamicBranches = true;
                    }
                }
                if (hasNewDynamicBranches)
                {
                    //only come in here if we have new branches to add
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                    {
                        AddDynamicBranchNames(true);
                    }));
                }
                // get a list builds to load
                bool useFavouritesList = branchName == MY_FAVOURITES;
                List<IBuildDefinition> userdefs = null;
                if (useFavouritesList)
                {
                    userdefs = (from def in buildHelper.BuildDefinitions where DefaultSettings.FavouriteBuilds.Contains(def.Name) select def).ToList();
                }
                else
                {
                    //find the build defs for the selected branch
                    userdefs = (from def in buildHelper.BuildDefinitions where def.Name.ToLower().StartsWith(branchName.ToLower()) select def).ToList();
                }
                _userBuildStatuses = new List<UserBuildStatus>();
                foreach (IBuildDefinition buildDef in userdefs)
                {
                    if ((showDisabled) || (buildDef.Enabled))
                    {
                        UserBuildStatus us = new UserBuildStatus(buildDef);
                        _userBuildStatuses.Add(us);
                        us.OnBuildsUpdated += UserStatus_OnBuildsUpdated;
                        //break;//single item for testing
                    }
                }
                _userBuildStatuses = _userBuildStatuses.OrderBy(b => b.BuildName).ToList();//order them so they are predictable where then show and group nicely on favourites
                //now pass this list the GUI thread to populate the combon in the background
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    UserBuildStatusesView.ItemsSource = _userBuildStatuses;
                    UserBuildStatusesView.UpdateLayout();
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// catch the event when a build gets an updated status so we can notify the relevant panels
        /// </summary>
        /// <param name="source"></param>
        /// <param name="e"></param>
        private void UserStatus_OnBuildsUpdated(object source, RunningBuildEventArgs e)
        {
            //push into dispatcher as this comes in from a background thread
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                if (source is UserBuildStatus && e != null && UserBuildStatusesView.SelectedItem != null)
                {
                    UserBuildStatus usUpdated = source as UserBuildStatus;
                    UserBuildStatus usSelected = UserBuildStatusesView.SelectedItem as UserBuildStatus;
                    if ((usUpdated != null) && (usSelected != null) && (usUpdated.BuildName == usSelected.BuildName))
                    {
                        //we have updates to the currently selected build definition
                        //so let the past build list know
                        PastBuildsPanel.UpdateBuilds(e.NewBuilds, e.UpdatedBuilds);
                        CurrentBuildDetailsPanel.UpdateFromLatestBuilds(e.UpdatedBuilds);
                        BuildResultsTabControl.UpdateFromLatestBuilds(e.UpdatedBuilds);
                    }
                }
            }));
        }

        /// <summary>
        /// Display the past builds in a the window
        /// </summary>
        /// <param name="buildStatus"></param>
        private void DisplayPastBuilds(UserBuildStatus buildStatus)
        {
            PastBuildsPanel.Children.Clear();
            BuildDetails latestbuild = null;
            if ((buildStatus != null) && (buildStatus.AllBuildDetails != null) && (buildStatus.AllBuildDetails.Count > 0))
            {
                List<BuildDetails> orderedList = new List<BuildDetails>();
                orderedList.AddRange(buildStatus.AllBuildDetails.OrderByDescending(x => x.LatestTime));
                //orderedList.Reverse();
                double maxSize = PastBuildsGrid.ActualWidth > 40 ? PastBuildsGrid.ActualWidth - 30 : 40;//100.00
                latestbuild = PastBuildsPanel.DisplayPastBuilds(orderedList, maxSize, false);
                PastBuildsPanel.UpdateLayout();
                ClearBuildResultsTab();
                if (buildStatus.CurrentRunningBuild != null)
                {
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                    {
                        UpdateBuildDetailsPanel(buildStatus.CurrentRunningBuild);
                        UpdateBuildResults(buildStatus.CurrentRunningBuild);
                    }));
                }
                else
                {
                    UpdateBuildDetailsPanel(latestbuild);
                    UpdateBuildResults(buildStatus.CurrentRunningBuild);
                }
            }
        }

        /// <summary>
        /// update the details panel
        /// </summary>
        /// <param name="details"></param>
        public void UpdateBuildDetailsPanel(BuildDetails details)
        {
            CurrentBuildDetailsPanel.Init(details);
        }

        /// <summary>
        /// Update the changesets displayed in the bottom list view
        /// </summary>
        /// <param name="details"></param>
        public void UpdateBuildResults(BuildDetails details)
        {
            if (details != null)
            {
                BuildDetails previousBuild = PastBuildsPanel.FindPreviousBuild(details, false);
                BuildResultsTabControl.UpdateBuildDetails(details, previousBuild);
            }
        }

        /// <summary>
        /// Do a full refresh
        /// </summary>
        private void Refresh()
        {
            UserBuildStatusesView.ItemsSource = null;
            if (_userBuildStatuses != null)
                _userBuildStatuses.Clear();
            ClearBuildDetailsPanel(null, null);
            ClearBuildResultsTab();
            GetBuildDefinitionsOnThread();
            StartTimer();
        }

        /// <summary>
        /// Clear the changeset views
        /// </summary>
        private void ClearBuildResultsTab()
        {
            BuildResultsTabControl.ClearTabs();
        }

        /// <summary>
        /// refresh the current builds showing
        /// </summary>
        private void RefreshStatus()
        {
            if ((_userBuildStatuses != null) && (IsAutoRefresh))
            {
                foreach (UserBuildStatus stat in _userBuildStatuses)
                {
                    stat.Refresh();
                }
            }
        }

        #region Event Handlers

        /// <summary>
        /// Update the details panel to the build info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void BuildButton_UpdateDetailsPanel(object sender, RoutedEventArgs e)
        {
            if (sender is BuildDetailsButton)
            {
                BuildDetailsButton bdButton = sender as BuildDetailsButton;
                if (bdButton != null)
                {
                    UpdateBuildDetailsPanel(bdButton.BuildDetails);
                }
            }
        }

        /// <summary>
        /// user has selected a build in the main grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserBuildStatuses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserBuildStatus buildStatus = UserBuildStatusesView.SelectedItem as UserBuildStatus;

            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                DisplayPastBuilds(buildStatus);
            }));
        }

        /// <summary>
        /// Clear the details panel of the build info
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearBuildDetailsPanel(object sender, RoutedEventArgs e)
        {
            UpdateBuildDetailsPanel(null);
        }

        /// <summary>
        /// User wants a manual refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Refesh_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        /// <summary>
        /// User has canged the show disabled checkbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsShowDisabled_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            IsShowDisabled = cb.IsChecked.Value;
            if (_userBuildStatuses != null)
                Refresh();
        }

        /// <summary>
        /// user has changed the auto refresh check box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IsAutoRefresh_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = sender as CheckBox;
            IsAutoRefresh = cb.IsChecked.Value;
            IntervalPanel.IsEnabled = IsAutoRefresh;
            ResetTimer();
        }

        /// <summary>
        /// User has changed the branch combo selection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BranchName_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_userBuildStatuses != null)
            {
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    Refresh();
                }));
            }
        }

        #region Timer

        /// <summary>
        /// Reset the timer
        /// </summary>
        private void ResetTimer()
        {
            StopTimer();
            StartTimer();
        }

        /// <summary>
        /// Start the timer
        /// </summary>
        private void StartTimer()
        {
            if ((IsAutoRefresh) && (_dispatcherTimer == null))
            {
                _dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
                _dispatcherTimer.Tick += dispatcherTimer_Tick;
                _dispatcherTimer.Interval = new TimeSpan(0, 0, DefaultSettings.AutoRefreshMins * 60);
                _dispatcherTimer.Start();
            }
        }

        /// <summary>
        /// Stop the timer
        /// </summary>
        private void StopTimer()
        {
            if (_dispatcherTimer != null)
            {
                _dispatcherTimer.Tick -= dispatcherTimer_Tick;
                _dispatcherTimer = null;
            }
        }

        /// <summary>
        /// Timer has reached the end of its period
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            RefreshStatus();
        }

        #endregion Timer

        #endregion Event Handlers

        /// <summary>
        /// Called when the window is closing
        /// </summary>
        /// <param name="e"></param>
        protected override void OnClosing(CancelEventArgs e)
        {
            DefaultSettings.Save();
            base.OnClosing(e);
        }

        /// <summary>
        /// Show the options dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Options_Click(object sender, RoutedEventArgs e)
        {
            OptionsWindow options = new OptionsWindow(AllBuildNames);
            options.ShowDialog();
            if (options.IsSaved)
            {
                MainWindow.DefaultSettings = options.UserSettings.Clone() as Settings;
                PopulateBranches();
                Refresh();
            }
        }

        /// <summary>
        /// Show the checkin times graph window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckinTimesGraph_Click(object sender, RoutedEventArgs e)
        {
            CheckinTimesGraphWindow cgw = new CheckinTimesGraphWindow();
            cgw.ShowDialog();
        }

        /// <summary>
        /// Copies a UI element to the clipboard as an image.
        /// </summary>
        /// <param name="element">The element to copy.</param>
        public static void CopyUIElementToClipboard(FrameworkElement element)
        {
            double width = element.ActualWidth;
            double height = element.ActualHeight;
            RenderTargetBitmap bmpCopied = new RenderTargetBitmap((int)Math.Round(width), (int)Math.Round(height), 96, 96, PixelFormats.Default);
            DrawingVisual dv = new DrawingVisual();
            using (DrawingContext dc = dv.RenderOpen())
            {
                VisualBrush vb = new VisualBrush(element);
                dc.DrawRectangle(vb, null, new Rect(new Point(), new Size(width, height)));
            }
            bmpCopied.Render(dv);
            Clipboard.SetImage(bmpCopied);
        }

        /// <summary>
        /// update the refresh interval slide and label
        /// </summary>
        private void UpdateIntervalText()
        {
            refreshIntervalSlider.ToolTip = string.Format("Refresh every {0} mins", DefaultSettings.AutoRefreshMins);
            intervalLabel.Content = string.Format("Interval: {0}", DefaultSettings.AutoRefreshMins);
        }
    }
}