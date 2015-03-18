using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptEngine
{
    class ScriptRetrivalEngine
    {
        public string[] GetHistoryScripts(decimal fromVersion, decimal toVersion)
        {
            List<string> result = new List<string>();

            decimal[] allFiles = GetAllHistoryScripts(ConfigHelper.SourceControlBaseLocation, "History");

            var filesToReturn = allFiles
                .Where(f => f > fromVersion && f <= toVersion)
                .OrderBy(f => f)
                .ToList();

            filesToReturn.ForEach(f => result.Add(Path.Combine(ConfigHelper.SourceControlBaseLocation, "History", f + ".sql")));

            return result.ToArray();
        }

        public string[] GetHoldingScripts()
        {
            List<string> result = new List<string>();
            var files = Directory.GetFiles(Path.Combine(ConfigHelper.SourceControlBaseLocation, "Holding"), "*.sql");

            Dictionary<int, string> fileDict = ParseHoldingToInts(files);

            foreach (var key in fileDict.Keys.OrderBy(k => k))
            {
                result.Add(fileDict[key]);
            }

            return result.ToArray();
        }

        public string[] GetScripts(string folderName)
        {
            string gitScriptLoc = Path.Combine(ConfigHelper.SourceControlBaseLocation);

            List<string> result = new List<string>();

            decimal[] allFiles = GetAllHistoryScripts(gitScriptLoc, folderName);

            var filesToReturn = allFiles
                .OrderBy(f => f)
                .ToList();

            filesToReturn.ForEach(f => result.Add(Path.Combine(gitScriptLoc, folderName, f + ".sql")));

            return result.ToArray();
        }

        private decimal[] GetAllHistoryScripts(string scriptLocation, string directory)
        {
            var files = Directory.GetFiles(Path.Combine(scriptLocation, directory), "*.sql");

            List<decimal> typedFiles = new List<decimal>();
            foreach (var file in files)
            {
                decimal tempDec;
                int startLoc = file.LastIndexOfAny(new char[] { '\\', '/' }) + 1;
                int length = file.LastIndexOf('.') - startLoc;
                string decToParse = file.Substring(startLoc, length);
                if (decimal.TryParse(decToParse, out tempDec))
                {
                    typedFiles.Add(tempDec);
                }
                else
                {
                    return null;
                }
            }

            return typedFiles.ToArray();
        }

        public string GetLatestHistoryScript()
        {
            decimal[] allFiles = GetAllHistoryScripts(ConfigHelper.SourceControlBaseLocation, "History");

            var fileToReturn = allFiles.Max();
            string result = Path.Combine(ConfigHelper.SourceControlBaseLocation, "History", fileToReturn + ".sql");

            return result;
        }

        private Dictionary<int, string> ParseHoldingToInts(string[] files)
        {
            Dictionary<int, string> result = new Dictionary<int, string>();
            foreach (var file in files)
            {
                int key = ParseHoldingToInt(file);
                if (key >= 0 && !result.ContainsKey(key))
                    result.Add(key, file);
            }
            return result;
        }

        private int ParseHoldingToInt(string file)
        {
            int result = -1;

            if (!string.IsNullOrEmpty(file) && file.Length > 2)
            {
                string sub = file.Replace('/', '\\');
                if (sub.Contains('\\'))
                    sub = sub.Substring(file.LastIndexOf('\\') + 1);

                if (!string.IsNullOrEmpty(sub)
                    && sub.Contains('_'))
                {
                    var intStr = sub.Substring(0, sub.IndexOf('_'));
                    int tempInt;
                    if (int.TryParse(intStr, out tempInt))
                        result = tempInt;
                }
            }

            return result;
        }       
    }
}
