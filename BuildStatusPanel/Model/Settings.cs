using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Serialization;

namespace BuildStatusPanel.Model
{
    [Serializable]
    public class Settings : ICloneable
    {
        [XmlIgnore]
        public DateTime CheckedInAfterDate;

        public List<string> FavouriteBuilds { get; set; }

        ///where to find the exe
        public string TFSHomeForApp = "$/EBS/Tools/TFS/BuildStatusPanel/BuildStatusPanel/bin/Release";

        public string DefaultProject { get; set; }
        public string DefaultWorkspace { get; set; }
        public string VisualStudioPath86 { get; set; }
        public string VisualStudioPath64 { get; set; }
        public string TFSServerURIPath { get; set; }
        public int AutoRefreshMins { get; set; }
        public bool IsAutoRefresh { get; set; }
        public bool IsShowDisabled { get; set; }
        public string InstallerRootPath { get; set; }

        [XmlIgnore]
        public bool IsEBS
        { get { return DefaultProject == "EBS"; } }

        public char BranchGroupingDelimiter { get; set; }

        [NonSerialized]
        private static bool? _installerLocationExists;

        [NonSerialized]
        private static bool? _dropLocationExists;

        [XmlIgnore]
        public static bool RootInstallLocationExists
        {
            get
            {
#if DEBUG//don't want to bother with this in debug as it is expensive remotely
                return false;
#else
                return true;//need to review this to make more reliable
#endif
                //if (!_installerLocationExists.HasValue)
                //{
                //    //simple performance to stop multiple slow checks on network
                //    _installerLocationExists = Directory.Exists(@"\\ebs-archive-s\TFS_BUILDARCHIVE");
                //}
                //return _installerLocationExists.Value;
            }
        }

        [XmlIgnore]
        public static bool RootDropLocationExists
        {
            get
            {
                return true;//need to review this to make more reliable
                //if (!_dropLocationExists.HasValue)
                //{
                //    //simple performance to stop multiple slow checks on network
                //    _dropLocationExists = Directory.Exists(@"\\tfs-build01-s\AutomatedBuild\EBS.Nant\Logs");
                //}
                //return _dropLocationExists.Value;
            }
        }

        public Settings()
        {
            FavouriteBuilds = new List<string>();
            CheckedInAfterDate = GetLastCheckedInDate();
            //put in defaults for now, so we have something sensible
            DefaultProject = GlobalDefaults.DefaultProject;
            AutoRefreshMins = 1;
            IsAutoRefresh = true;
            IsShowDisabled = false;
            VisualStudioPath86 = GlobalDefaults.VisualStudioPath86;
            VisualStudioPath64 = GlobalDefaults.VisualStudioPath64;
            TFSServerURIPath = $"http://tfs.{GlobalDefaults.CompanyName}.net:8080/tfs/defaultcollection";
            InstallerRootPath = GlobalDefaults.InstallerRootPath;
            BranchGroupingDelimiter = GlobalDefaults.BranchGroupingDelimiter;
        }

        /// <summary>
        /// Make a clone of this object
        /// </summary>
        /// <returns></returns>
        public object Clone()
        {
            Settings clone = new Settings();
            clone.FavouriteBuilds = new List<string>();
            clone.FavouriteBuilds.AddRange(FavouriteBuilds);
            clone.CheckedInAfterDate = CheckedInAfterDate;
            clone.DefaultProject = DefaultProject;
            clone.AutoRefreshMins = AutoRefreshMins;
            clone.IsAutoRefresh = IsAutoRefresh;
            clone.IsShowDisabled = IsShowDisabled;
            clone.VisualStudioPath86 = VisualStudioPath86;
            clone.VisualStudioPath64 = VisualStudioPath64;
            clone.TFSServerURIPath = TFSServerURIPath;
            clone.InstallerRootPath = InstallerRootPath;
            clone.BranchGroupingDelimiter = BranchGroupingDelimiter;
            return clone;
        }

        public DateTime GetLastCheckedInDate()
        {
            return new DateTime(2015, 12, 17, 15, 0, 0);//look for changesets after this date
        }

        public System.Uri GetTFSServerName()
        {
            return new Uri(TFSServerURIPath);
        }

        /// <summary>
        /// save the settings to file
        /// </summary>
        public void Save()
        {
            try
            {
                StringBuilder xml = new StringBuilder();
                xml.Append(Serialize(this));
                string path = System.IO.Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "BuildStatusPanel.xml");
                System.IO.File.WriteAllText(path, xml.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Exception saving settings:" + ex.Message);
            }
        }

        /// <summary>
        /// load the settings from default file and from user setttings
        /// </summary>
        public static Settings LoadSettings()
        {
            //first load user settings
            Settings userSettings = null;
            string userSettingsPath = System.IO.Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData), "BuildStatusPanel.xml");
            if (System.IO.File.Exists(userSettingsPath))
            {
                string userSettingsXML = LoadFileText(userSettingsPath);
                if (!string.IsNullOrEmpty(userSettingsXML))
                {
                    object userSettingsObj = DeSerialize(userSettingsXML, typeof(Settings));
                    if ((userSettingsObj != null) && (userSettingsObj is Settings))
                    {
                        userSettings = userSettingsObj as Settings;
                    }
                }
            }
            else
            {
                userSettings = new Settings();
            }
            userSettings.CheckedInAfterDate = userSettings.GetLastCheckedInDate();
            return userSettings;
        }

        /// <summary>
        /// Load a file into a string
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string LoadFileText(string fileName)
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
        /// Serialize an object to xml
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string Serialize(object obj)
        {
            string xml = "";
            using (MemoryStream ms = new MemoryStream())
            {
                Type ty = obj.GetType();
                System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(ty);
                x.Serialize(ms, obj);
                ms.Seek(0, SeekOrigin.Begin);
                byte[] theBytes = new byte[ms.Length];
                ms.Read(theBytes, 0, (int)ms.Length);
                xml = Encoding.UTF8.GetString(theBytes);
            }
            return xml;
        }

        /// <summary>
        /// Deserialize xml to an object
        /// </summary>
        /// <param name="xml"></param>
        /// <param name="objType"></param>
        /// <returns></returns>
        public static object DeSerialize(string xml, Type objType)
        {
            object obj = null;
            try
            {
                using (StringReader ms = new StringReader(xml))
                {
                    System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(objType);//, overrides, null, null, null);
                    obj = (object)x.Deserialize(ms);
                }
            }
            catch (Exception ex)
            {
            }
            return obj;
        }
    }
}