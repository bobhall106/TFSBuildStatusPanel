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
using System.Windows.Threading;
using BuildStatusPanel.Model;

namespace BuildStatusPanel.Content
{
    /// <summary>
    /// Interaction logic for BuildResultsTabControl.xaml
    /// </summary>
    public partial class BuildResultsTabControl : TabControl
    {
        private BuildDetails _currentDetails = null;
        public BuildResultsTabControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Clear the changeset views
        /// </summary>
        public void ClearTabs()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                _currentDetails = null;
                BuildChangesetsTab.ToolTip = "Select a build to see the changesets";
                BuildChangesetsTab.Header = "Changesets";
                UnBuiltChangesetsTab.ToolTip = "Select a build to see the changesets";
                UnBuiltChangesetsTab.Header = "Changesets not in a Build yet";
                BuildChangesetsView.Clear();
                UnBuiltChangesetsView.Clear();
                ClearBuildSummary();
            }));
        }

        /// <summary>
        /// Update the current build details
        /// </summary>
        /// <param name="details"></param>
        /// <param name="previousBuild"></param>
        public void UpdateBuildDetails(BuildDetails details, BuildDetails previousBuild)
        {
            UpdateBuildDetailsChangesets(details, previousBuild);
            UpdateUpdateUnBuiltChangesets(details);
            UpdateBuildSummary(details);
            _currentDetails = details;
        }


        /// <summary>
        /// Update the changesets displayed in the bottom list view
        /// </summary>
        /// <param name="details"></param>
        private void UpdateBuildDetailsChangesets(BuildDetails details, BuildDetails previousBuild)
        {
            if (details != null)
            {
                if (previousBuild != null)
                {
                    string tooltip = string.Format("Changesets in Build:{0}{1}\r\n\r\n{2}", details.BuildName, details.BuildNumber, details.Information);
                    string buildNum = details.BuildNumber;
                    BuildChangesetsView.Populate(previousBuild, details);
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                    {
                        BuildChangesetsTab.ToolTip = tooltip;
                        BuildChangesetsTab.Header = string.Format("Changesets for {0}", buildNum);
                    }));
                }
            }
        }

        /// <summary>
        /// Update the Unbuilt changesets displayed in the bottom list view
        /// </summary>
        /// <param name="details"></param>
        private void UpdateUpdateUnBuiltChangesets(BuildDetails details)
        {
            if (details != null)
            {
                string tooltip = string.Format("Changesets since Build:{0}{1}\r\n\r\n{2}", details.BuildName, details.BuildNumber, details.Information);
                string buildNum = details.BuildNumber;
                UnBuiltChangesetsView.Populate(details, null);
                this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                {
                    UnBuiltChangesetsTab.ToolTip = tooltip;
                    UnBuiltChangesetsTab.Header = string.Format("Changesets since {0}", buildNum);
                }));

            }
        }
        /// <summary>
        /// Update the Build Summary
        /// </summary>
        /// <param name="details"></param>
        private void UpdateBuildSummary(BuildDetails details, bool scrollToHome=true)
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                string contents = null;
                BuildSummaryText.Text = "Loading...";
                if (details != null)
                {
                    try
                    {
                        contents = details.LoadSummaryLog();
                        HTMLReportButton.IsEnabled = details.HTMLReportLocationExists;
                        if (!string.IsNullOrEmpty(contents))
                        {
                            BuildSummaryText.Text = contents;
                            if (scrollToHome)
                                SummaryScroll.ScrollToHome();
                            else
                                SummaryScroll.ScrollToEnd();
                            return;
                        }
                    }
                    catch { }

                }
                BuildSummaryText.Text = "";
            }));
        }
        /// <summary>
        /// Clear the Build Summary
        /// </summary>
        private void ClearBuildSummary()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                BuildSummaryText.Text = "...";
                HTMLReportButton.IsEnabled = false;
            }));
        }


        private void HTMLReportButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentDetails != null)
            {
                _currentDetails.LoadHTMLReport();
            }
        }

        /// <summary>
        /// update the details panel from a list of potential udpates
        /// </summary>
        /// <param name="updatedBuilds"></param>
        internal void UpdateFromLatestBuilds(List<BuildDetails> updatedBuilds)
        {
            BuildDetails current = _currentDetails;
            BuildDetails matchingBuild = updatedBuilds.FirstOrDefault(b => b.IsSameBuild(current));
            if (matchingBuild != null)
            {
                _currentDetails = matchingBuild;
                UpdateBuildSummary(matchingBuild, false);
            }
        }

        /// <summary>
        /// Copy text to clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopySummary_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Clipboard.SetText(BuildSummaryText.Text);
        }
    }
}
