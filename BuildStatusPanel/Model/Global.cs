using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildStatusPanel.Model
{
    internal static class GlobalDefaults
    {
        public static string DefaultProject = "";
        public static string CompanyName = "mycompany";
        public static string DefaultWorkspace = "C:\\MyWorkspace";
        public static string VisualStudioPath86 = @"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\devenv.exe";
        public static string VisualStudioPath64 = @"C:\Program Files\Microsoft Visual Studio 12.0\Common7\IDE\devenv.exe";
        public static string TFSServerURIPath = $"http://tfs.{CompanyName}.net:8080/tfs/defaultcollection";
        public static string InstallerRootPath = "\\\\my-archive-s\\TFS_BUILDARCHIVE\\";
        public static char BranchGroupingDelimiter = '-';
        public static string BranchGroupingSeparator = "/";
        public static string BuildDefinitionName = "MyBuildDefinition";
    }
}