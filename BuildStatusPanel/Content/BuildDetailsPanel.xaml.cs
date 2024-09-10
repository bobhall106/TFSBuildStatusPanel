using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
    /// Interaction logic for BuildDetailsPanel.xaml
    /// </summary>
    public partial class BuildDetailsPanel : WrapPanel
    {

        #region Properties
        public BuildDetails BuildDetails { get; set; }
        
        #endregion
        #region CTOR
        /// <summary>
        /// Main CTOR
        /// </summary>
        public BuildDetailsPanel()
        {
            InitializeComponent();
            Init(null);
        } 
        #endregion
        #region Methods
        /// <summary>
        /// Init the control
        /// </summary>
        /// <param name="details"></param>
        public void Init(BuildDetails details)
        {
            BuildDetails = details;
            BuildNumber.Text = BuildDetails == null ? string.Empty : string.Format("{0} ({1})", BuildDetails.BuildNumber, BuildDetails.Changeset);
            BuildFinishTime.Text = BuildDetails == null ? string.Empty : (BuildDetails.IsRunning? (BuildDetails.HasFullBuildNumber ? "Running..." : "Queued..") : BuildDetails.FinishTimeString);
            BuildStartTime.Text = BuildDetails == null ? string.Empty : BuildDetails.StartTimeString;
            Status.Text = BuildDetails == null ? string.Empty : BuildDetails.Status;
            RequestedBy.Text = BuildDetails == null ? string.Empty : BuildDetails.RequestedBy;
            BuildTimeMins.Text = BuildDetails == null ? string.Empty : string.Format("{0} mins", BuildDetails.BuildTimeMins);
            DropLocation.IsEnabled = ((BuildDetails != null) && (!string.IsNullOrEmpty(BuildDetails.DropLocation)) && (BuildDetails.DropLocationExists));
            DropLocation.ToolTip = ((BuildDetails == null) || (string.IsNullOrEmpty(BuildDetails.DropLocation))) ? string.Empty : BuildDetails.DropLocation.ToString();
            InstallerLocation.IsEnabled = ((BuildDetails != null) && (!string.IsNullOrEmpty(BuildDetails.InstallerLocation)) && (BuildDetails.InstallLocationExists));
            InstallerLocation.ToolTip = ((BuildDetails == null) || (string.IsNullOrEmpty(BuildDetails.InstallerLocation))) ? string.Empty : BuildDetails.InstallerLocation.ToString();
            UpdateLayout();
        }

        /// <summary>
        /// handle when the button is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DropLocation_Click(object sender, RoutedEventArgs e)
        {
            if (BuildDetails != null)
                BuildDetails.LoadDropFolder();
        }
        #endregion

        /// <summary>
        /// Load the installer Location
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void InstallerLocation_Click(object sender, RoutedEventArgs e)
        {
            if (BuildDetails != null)
                BuildDetails.LoadInstallerLocation();
        }

        /// <summary>
        /// update the details panel from a list of potential udpates
        /// </summary>
        /// <param name="details"></param>
        public void UpdateFromLatestBuilds(List<BuildDetails> detailsList)
        {
            BuildDetails current = BuildDetails;
            BuildDetails matchingBuild = detailsList.FirstOrDefault(b => b.IsSameBuild(current));
            if(matchingBuild!=null)
                Init(matchingBuild);
        }

    }
}
