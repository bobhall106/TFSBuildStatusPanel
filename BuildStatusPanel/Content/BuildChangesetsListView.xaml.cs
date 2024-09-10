using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using BuildStatusPanel.Model;

namespace BuildStatusPanel.Content
{
    /// <summary>
    /// Interaction logic for BuildChangesetsListView.xaml
    /// </summary>
    public partial class BuildChangesetsListView : ListView
    {
        List<ChangeSetItem> _selectedBuildChangesets;
        BuildDetails _fromAfterBuild;
        BuildDetails _toAllOfBuild;

        /// <summary>
        /// Basic Constructor
        /// </summary>
        public BuildChangesetsListView()
        {
            _selectedBuildChangesets = new List<ChangeSetItem>();
            InitializeComponent();
        }

        /// <summary>
        /// Clear out changesets
        /// </summary>
        public void Clear()
        {
            _fromAfterBuild = null;
            _toAllOfBuild = null;
            _selectedBuildChangesets.Clear();
            ItemsSource = null;
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                UpdateLayout();
            }));
        }



        /// <summary>
        /// rclick menu open changeset
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OpenChangeset_Click(object sender, RoutedEventArgs e)
        {
            TFSBuildStatusHelper helper = new TFSBuildStatusHelper();
            foreach (ChangeSetItem csItem in SelectedItems)
            {
                helper.ViewChangeset(csItem.ChangesetId);
            }
        }

        /// <summary>
        /// double click the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ViewSelectedChangeset();
        }

        /// <summary>
        /// View single selected changeset
        /// </summary>
        private void ViewSelectedChangeset()
        {
            if (SelectedItem is ChangeSetItem)
            {
                ChangeSetItem csItem = SelectedItem as ChangeSetItem;
                if ((csItem != null) && (!string.IsNullOrEmpty(csItem.ChangesetId)))
                {
                    TFSBuildStatusHelper helper = new TFSBuildStatusHelper();
                    helper.ViewChangeset(csItem.ChangesetId);
                }
            }
        }

        /// <summary>
        /// Copy the selected changeset details to the clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyChangeset_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItems != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (ChangeSetItem csItem in SelectedItems)
                {
                    sb.AppendFormat("{0}\t{1}\t{2}\t{3}\r\n", csItem.ChangesetId, csItem.Owner, csItem.Comment, csItem.PolicyOverrideComment);
                }
                System.Windows.Clipboard.SetText(sb.ToString());
            }

        }

        /// <summary>
        /// Copy the selected owners to the clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyOwners_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItems != null)
            {
                StringBuilder sb = new StringBuilder();
                List<string> owners = new List<string>();
                foreach (ChangeSetItem csItem in SelectedItems)
                {
                    if(!owners.Contains(csItem.Owner))
                        sb.AppendFormat("{0};", csItem.Owner);
                }
                System.Windows.Clipboard.SetText(sb.ToString());
            }

        }

        /// <summary>
        /// Copy the selected Ids to the clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyIds_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItems != null)
            {
                StringBuilder sb = new StringBuilder();
                foreach (ChangeSetItem csItem in SelectedItems)
                {
                    sb.AppendFormat("{0},", csItem.ChangesetId);
                }
                System.Windows.Clipboard.SetText(sb.ToString());
            }

        }

        /// <summary>
        /// Poplate the list with the changeset for the defined build
        /// </summary>
        /// <param name="fromAfterBuild"></param>
        /// <param name="toAllOfBuild"></param>
        /// <param name="forceRefresh"></param>
        public void Populate(BuildDetails fromAfterBuild, BuildDetails toAllOfBuild, bool forceRefresh = false)
        {
            if ((!forceRefresh) && (_toAllOfBuild == toAllOfBuild) && (_fromAfterBuild == fromAfterBuild))
            {
                return;//identical load so don't bother
            }
            _fromAfterBuild = fromAfterBuild;
            _toAllOfBuild = toAllOfBuild;
            _selectedBuildChangesets.Clear();
            ItemsSource = null;
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                UpdateLayout();
            }));
            PopulateChangesetsOnThread(fromAfterBuild, toAllOfBuild);
        }

        /// <summary>
        /// Populate changesets on a thread
        /// </summary>
        /// <param name="fromAfterBuild"></param>
        /// <param name="toAllOfBuild"></param>
        private void PopulateChangesetsOnThread(BuildDetails fromAfterBuild, BuildDetails toAllOfBuild)
        {
            Thread nodeThread = new Thread(() => ThreadPopulateChangesets(fromAfterBuild, toAllOfBuild));
            nodeThread.Start();
        }

        /// <summary>
        /// Thread function to populate the changesets
        /// </summary>
        /// <param name="fromAfterBuild"></param>
        /// <param name="toAllOfBuild"></param>
        private void ThreadPopulateChangesets(BuildDetails fromAfterBuild, BuildDetails toAllOfBuild)
        {
            List<string> tfsPaths = fromAfterBuild.GetTFSPathsFromDefinition();
            if ((tfsPaths != null) && (tfsPaths.Count > 0))
            {
                List<ChangeSetItem> csItems = null;
                if (toAllOfBuild == null)
                {
                    //this is looking for changesets since this build
                    csItems = fromAfterBuild.GetChangesetsSinceThisBuild();
                }
                else
                {
                    //this is looking for changesets between builds
                    csItems = toAllOfBuild.GetChangesetsInThisBuild(fromAfterBuild);
                }
                if (csItems != null)
                {
                    _selectedBuildChangesets = csItems;
                    this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
                    {
                        ItemsSource = _selectedBuildChangesets;
                        UpdateLayout();
                    }));
                }
            }
        }

    }
}
