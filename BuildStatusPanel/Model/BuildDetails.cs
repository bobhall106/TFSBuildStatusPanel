using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Microsoft.TeamFoundation.Build.Client;

namespace BuildStatusPanel.Model
{
    public class BuildDetails : INotifyPropertyChanged
    {
        protected IBuildDetail _defaultBuild;
        private string _status = string.Empty;
        private bool? _dropLocationExists;
        private bool? _installerLocationExists;
        private List<ChangeSetItem> _changesetsInBuild = null;
        private List<ChangeSetItem> _changesetsSinceThisBuild = null;

        #region Properties

        public IBuildDetail DefaultBuild
        {
            get
            {
                return _defaultBuild;
            }
        }

        public virtual string BuildName
        {
            get; set;
        }

        public virtual string BuildNumber
        {
            get
            {
                if ((_defaultBuild == null) || (string.IsNullOrEmpty(_defaultBuild.BuildNumber)))
                    return string.Empty;

                return _defaultBuild.BuildNumber.Replace(BuildName, "");
            }
        }

        public bool IsSuccessful
        {
            get
            {
                return Status == BuildStatus.Succeeded.ToString();
            }
        }

        public virtual string Status
        {
            get
            {
                if (_defaultBuild == null)
                    return string.Empty;
                return _defaultBuild.Status.ToString();
            }
        }

        public virtual string RunningTime
        {
            get; set;
        }

        public virtual bool IsRunning
        {
            get
            {
                if (_defaultBuild == null)
                    return false;
                return ((_defaultBuild.Status == BuildStatus.InProgress) && (_defaultBuild.FinishTime < _defaultBuild.StartTime));
            }
        }

        public virtual bool HasFullBuildNumber
        {
            get
            {
                if (_defaultBuild == null)
                    return false;
                return ((!string.IsNullOrEmpty(_defaultBuild.BuildNumber)) && (_defaultBuild.BuildNumber.Contains(".")));//queued builds do not have a . in them
            }
        }

        public bool BuildFinished
        {
            get
            {
                if (_defaultBuild == null)
                    return true;
                return _defaultBuild.BuildFinished;
            }
        }

        public Color ColorValue
        {
            get
            {
                if (_defaultBuild == null)
                    return Colors.Black;

                switch (_defaultBuild.Status)
                {
                    case (BuildStatus.Succeeded): return Colors.Green;
                    case (BuildStatus.Failed): return Colors.Red;
                    case (BuildStatus.InProgress): return IsRunning ? (HasFullBuildNumber ? Colors.Blue : Colors.DarkCyan) : Colors.Black;
                }
                return Colors.Black;
            }
        }

        public virtual string Color
        {
            get
            {
                if (_defaultBuild == null)
                    return Colors.Black.ToString();

                switch (_defaultBuild.Status)
                {
                    case (BuildStatus.Succeeded): return Colors.Green.ToString();
                    case (BuildStatus.Failed): return Colors.Red.ToString();
                }
                return Colors.Black.ToString();
            }
        }

        public virtual int BuildTimeMins
        {
            get
            {
                if (_defaultBuild == null)
                    return 0;
                TimeSpan runningTime = _defaultBuild.FinishTime - _defaultBuild.StartTime;
                if (IsRunning)
                {
                    runningTime = DateTime.Now - _defaultBuild.StartTime;
                }
                return (runningTime.Days * 24 * 60 + runningTime.Hours * 60 + runningTime.Minutes);
            }
        }

        public DateTime LatestTime
        {
            get
            {
                if (_defaultBuild == null)
                    return DateTime.MinValue;
                if (_defaultBuild.FinishTime > DateTime.MinValue)
                    return _defaultBuild.FinishTime;
                return _defaultBuild.StartTime;
            }
        }

        public virtual string FinishTimeString
        {
            get
            {
                if (_defaultBuild == null)
                    return string.Empty;
                return string.Format("{0}", _defaultBuild.FinishTime.ToString());
            }
        }

        public virtual string StartTimeString
        {
            get
            {
                if (_defaultBuild == null)
                    return string.Empty;
                return string.Format("{0}", _defaultBuild.StartTime.ToString());
            }
        }

        public virtual DateTime StartTime
        {
            get
            {
                if (_defaultBuild == null)
                    return DateTime.MinValue;
                return _defaultBuild.StartTime;
            }
        }

        public virtual DateTime FinishTime
        {
            get
            {
                if (_defaultBuild == null)
                    return DateTime.MinValue;
                return _defaultBuild.FinishTime;
            }
        }

        public virtual string RequestedBy
        {
            get
            {
                if (_defaultBuild == null)
                    return string.Empty;
                if (!_defaultBuild.RequestedBy.Contains("TFS-Service"))
                    return _defaultBuild.RequestedBy.Replace($"{GlobalDefaults.CompanyName}\\", "");
                return _defaultBuild.RequestedFor.Replace($"{GlobalDefaults.CompanyName}\\", "");
            }
        }

        public virtual string DropLocation
        {
            get
            {
                if (_defaultBuild == null)
                    return string.Empty;
                return _defaultBuild.DropLocation;
            }
        }

        public virtual string HTMLReportLocation
        {
            get
            {
                if (_defaultBuild == null)
                    return string.Empty;
                return string.Format("{0}\\{1}.html", _defaultBuild.DropLocation, BuildName);
            }
        }

        public virtual string Changeset
        {
            get
            {
                if (_defaultBuild == null)
                    return string.Empty;
                return _defaultBuild.SourceGetVersion;
            }
        }

        public virtual string Information
        {
            get
            {
                return string.Format("Build Name: {8}\r\nBuild Number: {5}\r\nBuild Time: {0} mins\r\nStarted: {1}\r\nFinished: {2}\r\nRequested by: {3}\r\nStatus: {4}\r\nChangeset: {6}\r\nBuild Type: {7}", BuildTimeMins, StartTimeString, (IsRunning ? (HasFullBuildNumber ? "Running..." : "Queued..") : FinishTimeString), RequestedBy, Status, BuildNumber, Changeset, Type, BuildName);
            }
        }

        public virtual string InstallerLocation
        {
            get
            {
                if (_defaultBuild == null)
                    return string.Empty;
                return Path.Combine(MainWindow.DefaultSettings.InstallerRootPath, _defaultBuild.BuildNumber);
            }
        }

        public bool DropLocationExists
        {
            get
            {
                if (!_dropLocationExists.HasValue)
                {
                    //simple performance tweak to stop multiple slow checks on network
                    _dropLocationExists = Settings.RootDropLocationExists && Directory.Exists(DropLocation);
                }
                return _dropLocationExists.Value;
            }
        }

        public bool HTMLReportLocationExists
        {
            get
            {
                //this file may not exist at the start of the build, but should appear later, so no caching
                return Settings.RootDropLocationExists && File.Exists(HTMLReportLocation);
            }
        }

        public bool InstallLocationExists
        {
            get
            {
                if (!_installerLocationExists.HasValue)
                {
                    //simple performance tweak to stop multiple slow checks on network
                    _installerLocationExists = Settings.RootInstallLocationExists && Directory.Exists(InstallerLocation);
                }
                return _installerLocationExists.Value;
            }
        }

        public virtual string Type
        {
            get
            {
                if (_defaultBuild == null)
                    return string.Empty;
                return _defaultBuild.Reason.ToString();
            }
        }

        #endregion Properties

        #region Constructors

        public BuildDetails()
        {
            _defaultBuild = null;
        }

        public BuildDetails(IBuildDetail tfsDetail)
        {
            _defaultBuild = tfsDetail;
        }

        #endregion Constructors

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        //Create OnPropertyChanged method to raise event
        protected virtual void OnPropertyChanged(string PropertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(PropertyName));
        }

        #endregion INotifyPropertyChanged Members

        /// <summary>
        /// launch the drop folder
        /// </summary>
        public void LoadDropFolder()
        {
            if (!string.IsNullOrEmpty(DropLocation))
            {
                if (!Directory.Exists(DropLocation))
                {
                    MessageBox.Show(string.Format("Path does not exist.\r\n{0}", DropLocation));
                    return;
                }
                try
                {
                    Uri uri = new Uri(DropLocation);
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = uri.AbsoluteUri;
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Error loading path.\r\n{0}\r\n{1}", DropLocation, ex.Message));
                }
            }
        }

        /// <summary>
        /// launch the Installer folder
        /// </summary>
        public void LoadInstallerLocation()
        {
            if (!string.IsNullOrEmpty(InstallerLocation))
            {
                if (!Directory.Exists(InstallerLocation))
                {
                    MessageBox.Show(string.Format("Path does not exist.\r\n{0}", InstallerLocation));
                    return;
                }
                try
                {
                    Uri uri = new Uri(InstallerLocation);
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = uri.AbsoluteUri;
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Error loading path.\r\n{0}\r\n{1}", InstallerLocation, ex.Message));
                }
            }
        }

        /// <summary>
        /// launch the html build report
        /// </summary>
        public void LoadHTMLReport()
        {
            if (HTMLReportLocationExists)
            {
                try
                {
                    Uri uri = new Uri(HTMLReportLocation);
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.FileName = uri.AbsoluteUri;
                    Process.Start(startInfo);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format("Error loading path.\r\n{0}\r\n{1}", HTMLReportLocation, ex.Message));
                }
            }
        }

        /// <summary>
        /// Tell the Gui stuff has changed
        /// </summary>
        protected virtual void NotifyPropertiesChanged()
        {
            OnPropertyChanged("BuildNumber");
            OnPropertyChanged("FinishTimeString");
            OnPropertyChanged("FinishTimeEx");
            OnPropertyChanged("RequestedBy");
            OnPropertyChanged("DropLocation");
            OnPropertyChanged("Status");
            OnPropertyChanged("Color");
            OnPropertyChanged("RunningTime");
            OnPropertyChanged("Name");
            OnPropertyChanged("PercentageGoodBuilds");
            OnPropertyChanged("Information");
        }

        /// <summary>
        /// Get a list of TFS paths from the build definition
        /// </summary>
        /// <returns></returns>
        public List<string> GetTFSPathsFromDefinition()
        {
            if (_defaultBuild == null)
                return null;
            return TFSBuildStatusHelper.GetTFSPathsFromDefinition(_defaultBuild.BuildDefinition);
        }

        /// <summary>
        /// Stop running build
        /// </summary>
        internal void StopBuild()
        {
            if (IsRunning)
            {
                TFSBuildStatusHelper buildHelper = new TFSBuildStatusHelper();
                buildHelper.Init(MainWindow.DefaultSettings.TFSServerURIPath, MainWindow.DefaultSettings.DefaultProject);
                buildHelper.StopBuild(_defaultBuild);
            }
        }

        public bool IsSameBuild(BuildDetails newBuild)
        {
            if ((newBuild == null) || (newBuild.DefaultBuild == null))
                return false;
            return IsSameBuild(newBuild.DefaultBuild);
        }

        public bool IsSameBuild(IBuildDetail newBuild)
        {
            if (newBuild == null)
                return false;
            if ((newBuild.BuildNumber == BuildNumber)
                || (newBuild.BuildNumber.EndsWith(BuildNumber))
                || (BuildNumber.EndsWith(newBuild.BuildNumber)))
                return true;
            //not the same build number
            //if a build is queued but waiting to be run then it has queue number not a build number
            //this is then changed later to a build x.x number, so we need to check for these
            if ((IsRunning)
                && (newBuild.StartTime == StartTime)
                && (newBuild.SourceGetVersion == Changeset)
                )
            {
                return true;
            }
            return false;//not the same
        }

        public bool IsUpdated(BuildDetails previousBuild)
        {
            if (!IsSameBuild(previousBuild))
                return false;//not the same build so not updated
            if (previousBuild.IsRunning)
                return true;//always give updates to running builds
            bool isUpdated = ((previousBuild.Status != Status)
                                || (previousBuild.LatestTime < LatestTime));
            return isUpdated;
        }

        internal void Update(IBuildDetail det)
        {
            _defaultBuild = det;
            _installerLocationExists = null;
            _dropLocationExists = null;
        }

        /// <summary>
        /// function to get the changesets included in this build
        /// </summary>
        /// <param name="previousBuild"></param>
        /// <returns>list of changesets</returns>
        public List<ChangeSetItem> GetChangesetsInThisBuild(BuildDetails previousBuild)
        {
            List<ChangeSetItem> list = new List<ChangeSetItem>();//give out a new list as they may clear it
            if (_changesetsInBuild != null)
            {
                list.AddRange(_changesetsInBuild);
                return list; //if we have already got them then just return the list as it will not change
            }
            List<string> tfsPaths = GetTFSPathsFromDefinition();
            if ((tfsPaths != null) && (tfsPaths.Count > 0))
            {
                string toChangset = Changeset;
                //TODO: put this in a thread
                TFSBuildStatusHelper helper = new TFSBuildStatusHelper();
                helper.Init(MainWindow.DefaultSettings.TFSServerURIPath, MainWindow.DefaultSettings.DefaultProject);
                List<ChangeSetItem> csItems = helper.GetChangesets(tfsPaths, previousBuild.Changeset, toChangset);
                if (csItems != null)
                {
                    _changesetsInBuild = csItems;
                    list.AddRange(_changesetsInBuild);
                }
            }
            return list;
        }

        /// <summary>
        /// function to get the changesets checked in since this build
        /// </summary>
        /// <param name="previousBuild"></param>
        /// <returns>list of changesets</returns>
        public List<ChangeSetItem> GetChangesetsSinceThisBuild()
        {
            DateTime fromWhen = DateTime.MinValue;
            List<ChangeSetItem> list = new List<ChangeSetItem>();//give out a new list as they may clear it
            if (_changesetsSinceThisBuild != null)
            {
                //find the latest changeset time and then get from there this time
                ChangeSetItem csiLatest = _changesetsSinceThisBuild.FirstOrDefault();//first should be the latest
                if (csiLatest != null)
                {
                    fromWhen = csiLatest.CreationDateTime > fromWhen ? csiLatest.CreationDateTime : fromWhen;
                }
                else
                {
                    fromWhen = this.StartTime.AddTicks(1);
                }
            }
            else
            {
                _changesetsSinceThisBuild = new List<ChangeSetItem>();
            }
            List<string> tfsPaths = GetTFSPathsFromDefinition();
            if ((tfsPaths != null) && (tfsPaths.Count > 0))
            {
                string toChangset = Changeset;
                TFSBuildStatusHelper helper = new TFSBuildStatusHelper();
                helper.Init(MainWindow.DefaultSettings.TFSServerURIPath, MainWindow.DefaultSettings.DefaultProject);
                List<ChangeSetItem> csItems = null;
                //be smart about what to get so we get as small as possible
                if (fromWhen == DateTime.MinValue)
                    csItems = helper.GetChangesets(tfsPaths, Changeset, null);//get everything
                else
                    csItems = helper.GetChangesets(tfsPaths, fromWhen, DateTime.Now);//get all since the last changeset
                if ((csItems != null) && (csItems.Count > 0))
                {
                    //now spladd any new ones into the list
                    foreach (ChangeSetItem csNew in csItems)
                    {
                        ChangeSetItem csFound = _changesetsSinceThisBuild.FirstOrDefault(existing => existing.ChangesetId == csNew.ChangesetId);
                        if (csFound == null)
                            _changesetsSinceThisBuild.Add(csNew);
                    }
                }
                //make sure they are ordered
                _changesetsSinceThisBuild = _changesetsSinceThisBuild.OrderByDescending(cs => cs.ChangesetId).ToList();
                list.AddRange(_changesetsSinceThisBuild);
            }
            return list;
        }

        /// <summary>
        /// Load a file into a string
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private string LoadFileText(string fileName)
        {
            StringBuilder sb = new StringBuilder();
            using (StreamReader sr = new StreamReader(fileName))
            {
                String line;
                while ((line = sr.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Load the Build summary log
        /// </summary>
        /// <returns></returns>
        public string LoadSummaryLog()
        {
            if (DropLocationExists)
            {
                string filename = Path.Combine(this.DropLocation, "___Build_Summary___.log");
                if (File.Exists(filename))
                {
                    string contents = LoadFileText(filename);
                    return contents;
                }
            }
            return null;
        }
    }
}