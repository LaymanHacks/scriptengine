using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ScriptEngine
{
    public class ScriptRunner
    {
        public string RunScripts(string[] scriptPaths, string connectionString,
          Dictionary<string, string> inputReplacements)
        {
            if (inputReplacements == null)
                inputReplacements = new Dictionary<string, string>();

            string lastScriptPath = string.Empty;
            string lastSuccessfulScriptPath = string.Empty;

            try
            {
                var typedConnString = new SqlConnectionStringBuilder(connectionString);

                if (scriptPaths != null)
                {
                    using (var conn = new SqlConnection(typedConnString.ConnectionString))
                    {
                        conn.Open();

                        var success = true;
                        for (int i = 0; i < scriptPaths.Length && success; i++)
                        {
                            lastScriptPath = scriptPaths[i];

                            Console.WriteLine(string.Format("Running script {0}", lastScriptPath));
                            string contents = File.ReadAllText(lastScriptPath);

                            string replacedContents = this.ReplaceStrings(contents, inputReplacements);

                            if (!string.IsNullOrEmpty(replacedContents))
                            {
                                success = RunScriptWithExistingConnection(conn, replacedContents, lastScriptPath);
                            }
                            if (success)
                                lastSuccessfulScriptPath = lastScriptPath;
                        }
                    }
                }
            }
            catch (Exception e)
            {

            }

            return lastSuccessfulScriptPath;
        }

        public bool RunScriptWithExistingConnection(SqlConnection conn, string scriptContents, string scriptPath)
        {
            bool success = false;
            string lastSql = string.Empty;
            try
            {
                string[] commands = scriptContents.Split(
                        new string[] { 
                                "\r\nGO", "\r\ngo", "\r\nGo", "\r\ngo",
                                "\nGO", "\ngo", "\nGo", "\ngo",
                                "\rGO", "\rgo", "\rGo", "\rgo",
                                }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string c in commands)
                {
                    lastSql = c;

                    using (var command = new SqlCommand(c, conn))
                    {
                        command.CommandTimeout = 600000;
                        command.ExecuteNonQuery();
                    }
                }

                if (scriptPath.ToLower().Contains("history"))
                {
                    Console.Write("script complete: " + scriptPath); 
                }
                success = true;
            }
            catch (Exception e)
            {
                if (Environment.UserInteractive)
                {
                    MessageBox.Show(string.Format("Error while running script: {0} :: {1}", e.Message, scriptPath));
                }
                success = false;
            }

            return success;
        }

       

        public string ReplaceStrings(string script, Dictionary<string, string> replacements, bool recursive = true)
        {
            if (replacements != null && replacements.Count() > 0)
            {
                int replaceCount = 0;
                int maxReplaceCount = 50;
                string oldScript = script;
                do
                {
                    oldScript = script;
                    foreach (var replace in replacements)
                    {
                        script = script.Replace(replace.Key, replace.Value);
                    }
                    replaceCount++;
                }
                while (oldScript != script
                    && replaceCount < maxReplaceCount);

                if (oldScript != script)
                {
                    script = string.Empty;
                }
            }
            return script;
        }
    }
}
