using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using BuildStatusPanel.Model;
using System.Diagnostics;
using Microsoft.TeamFoundation.Server;

namespace BuildStatusPanel
{
    public class TFSBuildStatusHelper
    {
        private TfsTeamProjectCollection _server;
        private IBuildServer _buildServer;
        private VersionControlServer _versionControlServer;

        public IBuildDefinition[] BuildDefinitions { get; set; }
        public string ProjectName { get; set; }
        public string ProjectServerPath { get; set; }

        /// <summary>
        /// Basic constructor
        /// </summary>
        public TFSBuildStatusHelper()
        {
        }

        /// <summary>
        /// Initialise with asking the user what uri and project
        /// </summary>
        public void InitWithDialog()
        {
            TeamProjectPicker tpp = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false);
            tpp.ShowDialog();

            if (tpp.SelectedTeamProjectCollection != null)
            {
                _server = tpp.SelectedTeamProjectCollection;
                _server.EnsureAuthenticated();
                ProjectName = tpp.SelectedProjects[0].Name;
                CreateBuildServer();
            }
        }

        /// <summary>
        /// get me an instance of the build server service
        /// </summary>
        private void CreateBuildServer()
        {
            _buildServer = (IBuildServer)_server.GetService(typeof(IBuildServer));
        }

        /// <summary>
        /// Get me an instance of the VCS service
        /// </summary>
        private void CreateVersionControlServer()
        {
            if (_server != null)
            {
                _versionControlServer = (VersionControlServer)_server.GetService(typeof(VersionControlServer));
            }
        }

        /// <summary>
        /// Initialise without any GUI
        /// </summary>
        /// <param name="serverPath"></param>
        /// <param name="projectName"></param>
        public void Init(string serverPath, string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                InitWithDialog();
                return;
            }
            _server = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(serverPath));
            _server.EnsureAuthenticated();
            ProjectName = projectName;
            CreateBuildServer();
        }

        /// <summary>
        /// Load the basic build definitions
        /// </summary>
        public void LoadBuildDefinitions()
        {
            //QueryBuildDefinitions(String)	Gets the build definitions for the specified team project.
            //QueryBuildDefinitions(IBuildDefinitionSpec)	Gets a single build definition query result for a specified build definition specification.
            //QueryBuildDefinitions(IBuildDefinitionSpec[])	Gets the build definition query results for a specified array of build definition specifications.
            //QueryBuildDefinitions(String, QueryOptions)	Gets the build definitions for the specified team project. The specified query options determine the amount of data that is retrieved in the query.
            // IBuildDefinition Interface -> http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.build.client.ibuilddefinition.aspx
            BuildDefinitions = _buildServer.QueryBuildDefinitions(ProjectName);
        }

        /// <summary>
        /// Get a list of projects
        /// </summary>
        /// <returns></returns>
        public List<string> GetProjects()
        {
            List<string> projects = new List<string>();
            if (_server != null)
            {
                var projectCollection = _server.GetService<ICommonStructureService>();

                foreach (var projectInfo in projectCollection.ListProjects())
                {
                    projects.Add(projectInfo.Name);
                }
                projects.Sort();
            }
            return projects;
        }

        public IBuildDetail[] LoadBuildDetails(IBuildDefinition def)
        {
            //IBuildDetail Interface -> http://msdn.microsoft.com/en-us/library/microsoft.teamfoundation.build.client.ibuilddetail.aspx
            return def.QueryBuilds();
        }

        public IBuildDetail[] LoadBuildDetailsUpdates(IBuildDefinition def, DateTime sinceDateTime)
        {
            IBuildDetailSpec spec = _buildServer.CreateBuildDetailSpec(ProjectName, def.Name);
            //spec.MaxBuildsPerDefinition = 5;//just get the last 5 to reduce some load
            spec.MinChangedTime = sinceDateTime;//just today's
            spec.QueryOrder = BuildQueryOrder.FinishTimeDescending;
            var builds = _buildServer.QueryBuilds(spec);
            return builds.Builds;
        }

        /// <summary>
        /// queue a new build
        /// </summary>
        /// <param name="def"></param>
        public void QueueNewBuild(IBuildDefinition def)
        {
            if (_buildServer == null)
                CreateBuildServer();
            IBuildDefinition buildDef = _buildServer.GetBuildDefinition(def.TeamProject, def.Name);
            IQueuedBuild qbuild = _buildServer.QueueBuild(buildDef);
        }

        /// <summary>
        /// Stop a running build
        /// </summary>
        /// <param name="def"></param>
        public void StopBuild(IBuildDetail det)
        {
            if (_buildServer == null)
                CreateBuildServer();
            IBuildDetail[] details = new IBuildDetail[1];
            details[0] = det;
            _buildServer.StopBuilds(details);
        }

        /// <summary>
        /// Get a list of TFS paths from the build definition
        /// </summary>
        /// <returns></returns>
        public static List<string> GetTFSPathsFromDefinition(IBuildDefinition buildDef)
        {
            List<string> tfsPaths = new List<string>();
            if ((buildDef != null)
                && (buildDef.Workspace != null)
                && (buildDef.Workspace.Mappings != null))
            {
                //walk through the mappings to get the tfs paths
                foreach (var mapping in buildDef.Workspace.Mappings)
                {
                    if ((mapping != null) && (!string.IsNullOrEmpty(mapping.ServerItem)))
                    {
                        if (!tfsPaths.Contains(mapping.ServerItem))
                            tfsPaths.Add(mapping.ServerItem);
                    }
                }
            }
            return tfsPaths;
        }

        /// <summary>
        /// Get a list of changesets from one build to another
        /// </summary>
        /// <param name="tfsPaths"></param>
        /// <param name="fromDateTime"></param>
        /// <param name="untilDateTime"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        public List<ChangeSetItem> GetChangesets(List<string> tfsPaths, string changeSetFrom, string changeSetTo, int maxCount = 1000)
        {
            List<ChangeSetItem> items = new List<ChangeSetItem>();

            if (_versionControlServer == null)
                CreateVersionControlServer();
            foreach (string path in tfsPaths)
            {
                var changes = _versionControlServer.QueryHistory(
                                        path,
                                        VersionSpec.Latest,
                                        0,
                                        RecursionType.Full,
                                        null,
                                        VersionSpec.ParseSingleSpec(changeSetFrom, null), // starting from changeset 100
                                        string.IsNullOrEmpty(changeSetTo) ? null : VersionSpec.ParseSingleSpec(changeSetTo, null), // ending with changeset 200
                                        maxCount,
                                        true,
                                        true);

                foreach (Changeset change in changes)
                {
                    bool isChangsetInPreviousBuild = ((changeSetFrom != null)
                                    && (changeSetFrom.EndsWith(change.ChangesetId.ToString())));//exclude the last changsest as it was in the previous build, not this one

                    if (!isChangsetInPreviousBuild)
                    {
                        //the same changeset may exist on multiple tfsPaths
                        bool existsAlready = items.Any(cs => cs.ChangesetId == change.ChangesetId.ToString());
                        if (!existsAlready)
                        {
                            ChangeSetItem item = new ChangeSetItem(change);
                            items.Add(item);
                        }
                    }
                }
            }
            //finally order them by changeset id with newest at top
            List<ChangeSetItem> orderedItems = items.OrderByDescending(c => c.ChangesetId).ToList();
            return orderedItems;
        }

        /// <summary>
        /// Get a list of changesets from one date to another
        /// </summary>
        /// <param name="tfsPaths"></param>
        /// <param name="fromDateTime"></param>
        /// <param name="untilDateTime"></param>
        /// <param name="maxCount"></param>
        /// <returns>list of changesets</returns>
        public List<ChangeSetItem> GetChangesets(List<string> tfsPaths, DateTime fromDateTime, DateTime untilDateTime, int maxCount = 1000)
        {
            List<ChangeSetItem> items = new List<ChangeSetItem>();

            if (_versionControlServer == null)
                CreateVersionControlServer();
            foreach (string path in tfsPaths)
            {
                var changes = _versionControlServer.QueryHistory(
                                        path,
                                        VersionSpec.Latest,
                                        0,
                                        RecursionType.Full,
                                        null,
                                        new DateVersionSpec(fromDateTime), // starting from changeset 100
                                        new DateVersionSpec(untilDateTime), // ending with changeset 200
                                        maxCount,
                                        true,
                                        false);

                foreach (Changeset change in changes)
                {
                    bool existsAlready = items.Any(cs => cs.ChangesetId == change.ChangesetId.ToString());
                    if (!existsAlready)
                    {
                        ChangeSetItem item = new ChangeSetItem(change);
                        items.Add(item);
                    }
                }
            }
            //finally order them by changeset id with newest at top
            List<ChangeSetItem> orderedItems = items.OrderByDescending(c => c.ChangesetId).ToList();
            return orderedItems;
        }

        /// <summary>
        /// Get path to VS
        /// </summary>
        /// <returns></returns>
        private string GetTFPath()
        {
            //todo... probably should get from registry..
            string vsPath = System.IO.Path.Combine(MainWindow.DefaultSettings.VisualStudioPath86, "tf.exe");
            if (System.IO.File.Exists(vsPath))
                return vsPath;
            vsPath = System.IO.Path.Combine(vsPath, "tf.exe");
            if (System.IO.File.Exists(vsPath))
                return vsPath;
            return string.Empty;
        }

        /// <summary>
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="localFolder"></param>
        private void RunTFSCommand(string arguments, string localFolder)
        {
            bool debug = false;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = localFolder;
            startInfo.FileName = GetTFPath();
            startInfo.Arguments = arguments;
            if (debug)
            {
                string tempBuildBatFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Mergme_debug_cmd.bat");
                //Build up a little batch file with the command in, and a pause at the end so we can see what fails
                StringBuilder batchCmd = new StringBuilder();
                batchCmd.Append("@echo on\r\n");
                batchCmd.Append(string.Format("TITLE {0}\r\n", tempBuildBatFile));
                batchCmd.Append(System.IO.Path.GetPathRoot(localFolder).Replace("\\", ""));
                batchCmd.Append("\r\n");
                batchCmd.Append(string.Format("cd \"{0}\"\r\n", localFolder));
                batchCmd.Append(string.Format("\"{0}\" {1}\r\n", startInfo.FileName, startInfo.Arguments));
                batchCmd.Append("@pause\r\n");
                //write to file
                System.IO.File.WriteAllText(tempBuildBatFile, batchCmd.ToString());
                startInfo.FileName = tempBuildBatFile;
                startInfo.Arguments = "";
            }
            Process.Start(startInfo);
        }

        /// <summary>
        /// View a specific changeset
        /// </summary>
        /// <param name="changesetId"></param>
        public void ViewChangeset(string changesetId)
        {
            //try to do this via the api....
            string localFolder = "c:\\";
            RunTFSCommand(string.Format("changeset {0}", changesetId), localFolder);
        }

        /// <summary>
        /// Get a list of changesets comments since a changeset
        /// </summary>
        /// <param name="tfsPaths"></param>
        /// <param name="changeSetFrom"></param>
        /// <param name="maxCount"></param>
        /// <returns></returns>
        public string GetThisAppChangesetComments(string tfsPath, DateTime changeSetFromDate, int maxCount = 10, bool discardFirstChangeset = true)
        {
            StringBuilder sb = new StringBuilder();

            if (_versionControlServer == null)
                CreateVersionControlServer();
            DateVersionSpec dateSpec = new DateVersionSpec(changeSetFromDate);
            var changes = _versionControlServer.QueryHistory(
                                    tfsPath,
                                    VersionSpec.Latest,
                                    0,
                                    RecursionType.Full,
                                    null,
                                    dateSpec, // starting from changeset 100
                                    null,
                                    maxCount,
                                    true,
                                    false);
            bool discarded = false;
            foreach (Changeset change in changes)
            {
                if ((discardFirstChangeset) && (!discarded))
                {
                    discarded = true;
                    //we do not want this one as it's probably the exe checkin and we can't get the changeset id until we checkin. catch22
                }
                else if ((change != null) && (!string.IsNullOrEmpty(change.Comment)))
                {
                    sb.Append(change.Comment);
                    sb.Append("\r\n");
                }
            }

            return sb.ToString();
        }
    }
}