using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using BuildStatusPanel.Content;
using Microsoft.TeamFoundation.Build.Client;

namespace BuildStatusPanel.Model
{
    public class RunningBuildEventArgs : EventArgs
    {
        public List<BuildDetails> UpdatedBuilds { get; protected set; }
        public List<BuildDetails> NewBuilds { get; protected set; }
        public RunningBuildEventArgs()
        {
            UpdatedBuilds = new List<BuildDetails>();
            NewBuilds = new List<BuildDetails>();
        }

    }

    public delegate void BuildsUpdated(object source, RunningBuildEventArgs e);
    /// <summary>
    /// Main object for holding the build information for a collection of builds
    /// </summary>
    public class UserBuildStatus : BuildDetails
    {

        private IBuildDefinition _buildDef;

        private string _status = string.Empty;

        #region Events

        public event BuildsUpdated OnBuildsUpdated;

        #endregion

        #region Properties

        public List<BuildDetails> AllBuildDetails { get; private set; }

        public override string BuildName
        {
            get
            {
                if (_buildDef == null)
                    return "Not defined";
                return _buildDef.Name;
            }
        }

        public override string Status
        {
            get
            {
                return _status;
            }
        }

        public string FinishTimeEx
        {
            get
            {
                if (string.IsNullOrEmpty(FinishTimeString))
                    return string.Empty;
                return string.Format("{0} ({1} mins)", FinishTimeString, BuildTimeMins);
            }
        }

        public override string Color
        {
            get
            {
                if (!_buildDef.Enabled)
                    return Colors.DarkGray.ToString();
                return base.Color;
            }
        }

        public string PercentageGoodBuilds
        {
            get; set;
        }

        public BuildDetails CurrentRunningBuild
        {
            get
            {
                if ((AllBuildDetails == null) || (AllBuildDetails.Count < 1))
                    return null;
                return AllBuildDetails.FirstOrDefault(x => x.IsRunning);
            }
        }
        public UserBuildStatus(IBuildDefinition def)
        {
            _buildDef = def;
            if (_buildDef != null)
            {
                LazyLoad();
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        /// Start a load on another thread
        /// </summary>
        private void LazyLoad()
        {
            Thread nodeThread = new Thread(() => ThreadGetBuildDetailsList());
            nodeThread.Start();
        }

        /// <summary>
        /// Thread to get the list of valid TFS users and populate the combo with them
        /// </summary>
        private void ThreadGetBuildDetailsList()
        {
            try
            {
                if (AllBuildDetails == null)
                    AllBuildDetails = new List<BuildDetails>();
                //make a copy of the details for the event later
                //lock (AllBuildDetails)//just in case another udpate thead comes in
                {
                    RunningBuildEventArgs eventArgs = null;
                    List<BuildDetails> previousBuilds = new List<BuildDetails>();
                    eventArgs = new RunningBuildEventArgs();
                    previousBuilds.AddRange(AllBuildDetails);
                    DateTime dtOneWeekAgo = DateTime.Now - new TimeSpan(7, 0, 0, 0);
                    DateTime sinceDate = dtOneWeekAgo;//get builds for the last week only
                    if (previousBuilds.Count > 0)
                    {
                        sinceDate = DateTime.Now.Date;//default just get updates on today only
                        bool hasPreviousRunningBuilds = previousBuilds.Any(b => b.IsRunning);
                        if (hasPreviousRunningBuilds)
                            sinceDate = previousBuilds.LastOrDefault(b => b.IsRunning).LatestTime;
                        else
                            sinceDate = previousBuilds.FirstOrDefault().LatestTime;
                    }
                    TFSBuildStatusHelper buildHelper = new TFSBuildStatusHelper();
                    buildHelper.Init(MainWindow.DefaultSettings.TFSServerURIPath, MainWindow.DefaultSettings.DefaultProject);
                    //var detailsAll = buildHelper.LoadBuildDetails(_buildDef);
                    var detailsUpdates = buildHelper.LoadBuildDetailsUpdates(_buildDef, sinceDate);
                    //AllBuildDetails.Clear();
                    _defaultBuild = null;
                    if (detailsUpdates != null)
                    {
                        foreach (IBuildDetail det in detailsUpdates)
                        {
                            BuildDetails bd = AllBuildDetails.FirstOrDefault(b => (b.IsSameBuild(det)));
                            if (bd != null)
                            {
                                //existing build
                                if (bd.IsUpdated(bd))
                                {
                                    bd.Update(det);
                                    if (OnBuildsUpdated != null)
                                        eventArgs.UpdatedBuilds.Add(bd);
                                }
                            }
                            else
                            { //new build
                                bd = new BuildDetails(det) { BuildName = this.BuildName };
                                AllBuildDetails.Insert(0, bd);//insert at start as it is new
                                if (OnBuildsUpdated != null)
                                    eventArgs.NewBuilds.Add(bd);//a brand new build has arrived
                            }
                        }
                        //get a list with the latest finished at the top
                        var ordered = AllBuildDetails.OrderByDescending(d => d.FinishTime);
                        //get the last weeks worth of builds
                        var lastWeeksBuilds = from det in ordered
                                              where det.FinishTime > dtOneWeekAgo
                                              select det;
                        decimal total = lastWeeksBuilds.Count();
                        var finishedBuilds = from det in ordered where det.BuildFinished select det;
                        var successfulBuilds = from det in lastWeeksBuilds
                                               where det.Status == BuildStatus.Succeeded.ToString()
                                               select det;
                        decimal successfulBuildsCount = successfulBuilds.Count();
                        decimal percentage = 0;
                        if (total > 0)
                        {
                            percentage = ((successfulBuildsCount / total) * 100);
                        }
                        PercentageGoodBuilds = string.Format("{0}", (int)percentage);
                        _defaultBuild = finishedBuilds.FirstOrDefault().DefaultBuild;
                        _status = _defaultBuild.Status.ToString();
                        var currentRunningBuild = ordered.FirstOrDefault(d => d.IsRunning);
                        if (currentRunningBuild == null)
                        {
                            RunningTime = "N";
                        }
                        else
                        {
                            TimeSpan runningTime = DateTime.Now - currentRunningBuild.StartTime;
                            RunningTime = string.Format("{0} mins", (runningTime.Days * 24 * 60 + runningTime.Hours * 60 + runningTime.Minutes).ToString());
                        }
                        NotifyPropertiesChanged();
                        if (OnBuildsUpdated != null)
                        {
                            if ((eventArgs.UpdatedBuilds.Count > 0) || (eventArgs.NewBuilds.Count > 0))
                            {
                                //tell the world if they are listening
                                OnBuildsUpdated(this, eventArgs);
                            }
                        }
                    }
                    else
                    {
                        _status = "No Builds";
                        OnPropertyChanged("Status");
                    }
                }
            }
            catch (Exception ex)
            {
                _status = "No Builds";// ex.Message;
                OnPropertyChanged("Status");
            }
        }

        /// <summary>
        /// Tell the Gui stuff has changed
        /// </summary>
        protected override void NotifyPropertiesChanged()
        {
            base.NotifyPropertiesChanged();
            OnPropertyChanged("Name");
            OnPropertyChanged("PercentageGoodBuilds");
        }

        //Force a refresh
        internal void Refresh()
        {
            LazyLoad();
        }

        /// <summary>
        /// Queue a new build
        /// </summary>
        internal void QueuNewBuild()
        {
            TFSBuildStatusHelper buildHelper = new TFSBuildStatusHelper();
            buildHelper.Init(MainWindow.DefaultSettings.TFSServerURIPath, MainWindow.DefaultSettings.DefaultProject);
            buildHelper.QueueNewBuild(_buildDef);
            Refresh();
        }
        #endregion Methods
    }
}