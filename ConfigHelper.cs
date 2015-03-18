using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptEngine
{
    public static class ConfigHelper
    {
        public static string TargetDatabase { get { return GetConnectionString("TargetDatabase", "Data Source=\"(LOCAL)\"; Initial Catalog=\"{0}\"; Integrated Security=true;"); } }

        public static string LocalConnectionString { get { return GetConnectionString("LocalConnectionString"); } }

        public static string CustomerListDatabase { get { return GetConnectionString("CustomerListDatabase"); } }

        

        public static string TempPath { get { return GetString("TempPath", ""); } }

        public static string DBFileLocation { get { return GetString("DbFileLocation"); } }

        public static bool IsVerbose { get { return GetBool("IsVerbose", false); } }

        

        public static string SourceControlBaseLocation {
            get 
            {
                var dir = GetString("SourceControlBaseLocation", @"c:\temp\Scripts");
                return dir; 
            }
        }

        private static bool GetBool(string key, bool dft = true)
        {
            bool result = dft;
            if (ConfigurationManager.AppSettings.AllKeys.Length > 0
                && ConfigurationManager.AppSettings.AllKeys.Contains(key))
            {
                string tempRst = ConfigurationManager.AppSettings[key];
                if (string.IsNullOrEmpty(tempRst)
                    || !bool.TryParse(tempRst, out result))
                {
                    result = dft;
                }
            }
            return result;
        }

        private static string GetString(string key, string dft = null)
        {
            string result = dft;
            if (ConfigurationManager.AppSettings.AllKeys.Length > 0
                && ConfigurationManager.AppSettings.AllKeys.Contains(key))
            {
                result = ConfigurationManager.AppSettings[key];
            }
            return result;
        }

        private static string GetConnectionString(string key, string dft = null)
        {
            string result = dft;
            if (ConfigurationManager.ConnectionStrings.Count > 0
                && ConfigurationManager.ConnectionStrings[key] != null)
            {
                result = ConfigurationManager.ConnectionStrings[key].ConnectionString;
            }
            return result;
        }
    }
}
