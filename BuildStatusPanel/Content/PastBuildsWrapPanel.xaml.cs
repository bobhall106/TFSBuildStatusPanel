using System;
using System.Collections.Generic;
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
    /// Interaction logic for PastBuildsWrapPanel.xaml
    /// </summary>
    public partial class PastBuildsWrapPanel : WrapPanel
    {
        List<BuildDetails> _pastBuilds = null;
        private int _lastMaxBuildTime;
        private double _lastMaxButtonSize;

        public PastBuildsWrapPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Display all the builds in the wrap panel
        /// </summary>
        /// <param name="orderedList"></param>
        /// <param name="_lastMaxButtonSize"></param>
        /// <param name="isHorizontal"></param>
        /// <returns></returns>
        public BuildDetails DisplayPastBuilds(List<BuildDetails> orderedList, double maxButtonSize, bool isHorizontal=true)
        {
            BuildDetails latestbuild = null;
            _lastMaxBuildTime = orderedList.Max(x => x.BuildTimeMins);
            _lastMaxBuildTime = _lastMaxBuildTime == 0 ? 1 : _lastMaxBuildTime;//protect from div zero
            _lastMaxButtonSize = maxButtonSize;
            double scaleFactor = _lastMaxButtonSize / _lastMaxBuildTime;
            DateTime currentDay = DateTime.MinValue;
            double dayTotal = 0.0;
            double daySuccess = 0.0;
            BuildDayWrapPanel currentDayWrap = null;
            foreach (BuildDetails bd in orderedList)
            {
                DateTime latestTime = bd.LatestTime;
                if (latestTime > DateTime.MinValue)
                {
                    if (latestTime.Date != currentDay.Date)
                    {
                        dayTotal = 0.0;
                        daySuccess = 0.0;
                        currentDay = latestTime.Date;
                        BuildDayWrapPanel dayWrap = new BuildDayWrapPanel(currentDay, _lastMaxButtonSize, isHorizontal);
                        this.Children.Add(dayWrap);
                        currentDayWrap = dayWrap;
                    }

                    dayTotal += 1;
                    if (bd.IsSuccessful)
                        daySuccess += 1;
                    if (currentDayWrap != null)
                    {
                        double percent = (daySuccess / dayTotal);
                        currentDayWrap.SuccessRate = percent;
                    }
                    if(latestbuild==null)
                        latestbuild = bd;
                    BuildDetailsButton bdButton = new BuildDetailsButton(bd,  _lastMaxBuildTime , _lastMaxButtonSize, isHorizontal);
                    bdButton.Click += BuildDetailsButton_Activated;
                    bdButton.GotFocus += BuildDetailsButton_Activated;
                    currentDayWrap.Panel.Children.Add(bdButton);
                }
            }
            _pastBuilds = orderedList;//we will need this later
            return latestbuild;
        }

        /// <summary>
        /// Build details button has been activated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BuildDetailsButton_Activated(object sender, RoutedEventArgs e)
        {
            if (sender is BuildDetailsButton)
            {
                BuildDetailsButton bdButton = sender as BuildDetailsButton;
                if (bdButton != null)
                {
                    BuildStatusPanel.MainWindow main = Application.Current.MainWindow as BuildStatusPanel.MainWindow;
                    if (main != null)
                    {
                        main.UpdateBuildDetailsPanel(bdButton.BuildDetails);
                        main.UpdateBuildResults(bdButton.BuildDetails);
                    }
                }
            }
        }
        /// <summary>
        /// Find the previous build after the current one supplied
        /// </summary>
        /// <param name="currentDetails"></param>
        /// <param name="findSuccessfullOnly">only look for successful builds</param>
        /// <returns></returns>
        public BuildDetails FindPreviousBuild(BuildDetails currentDetails, bool findSuccessfullOnly=false)
        {
            BuildDetails previous = null;
            if (_pastBuilds != null)
            {
                //_pastBuilds is ordered in a date order with newest at the top
                bool foundCurrent = false;
                foreach (BuildDetails bd in _pastBuilds)
                {
                    if (foundCurrent)
                    {
                        if (!findSuccessfullOnly)
                            return bd;//any build will do
                        if (bd.IsSuccessful)
                            return bd;
                        previous = bd;
                    }
                    else if (currentDetails.BuildNumber == bd.BuildNumber)
                    {
                        foundCurrent = true;//next time around we will start looking for the next build
                    }
                }
            }
            return previous;//default to the last one in the list if no direct matches found
        }

        /// <summary>
        /// Update the panel with the new and updated build info
        /// </summary>
        /// <param name="newBuilds"></param>
        /// <param name="updatedBuilds"></param>
        internal void UpdateBuilds(List<BuildDetails> newBuilds, List<BuildDetails> updatedBuilds)
        {
            //Update existing Builds
            foreach (BuildDetails upd in updatedBuilds)
            {
                foreach (object child in Children)
                {
                    if (child is BuildDayWrapPanel)
                    {
                        BuildDayWrapPanel panel = child as BuildDayWrapPanel;
                        if ((panel != null) && (panel.Date ==upd.StartTime.Date))
                        {
                            panel.UpdateExistingBuild(upd);
                            break;
                        }
                    }
                }
            }
            foreach (BuildDetails newBuild in newBuilds)
            {
                foreach (object child in Children)
                {
                    if (child is BuildDayWrapPanel)
                    {
                        BuildDayWrapPanel panel = child as BuildDayWrapPanel;
                        if ((panel != null) && (panel.Date == newBuild.StartTime.Date))
                        {
                            panel.AddNewBuild(newBuild, _lastMaxBuildTime, _lastMaxButtonSize, true);
                            panel.UpdateLayout();
                            break;
                        }
                    }
                }
            }
        }
    }
}
