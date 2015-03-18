using ScriptEngine.Accessors;
using ScriptEngine.DataContracts;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptEngine.Managers
{
    class DatabaseManager
    {

        public void DevMe(long customerId, decimal maxScriptVersion, bool testDb)
        {
            var scriptRunner = new ScriptRunner();
            var scriptRetrievalEngine = new ScriptRetrivalEngine();
            var scripts = scriptRetrievalEngine.GetScripts("DevPrep");

            // create dev databases
            var replacements = new Dictionary<string, string>();
            replacements.Add("##MODE##", "DEV");

            scriptRunner.RunScripts(scripts, ConfigHelper.LocalConnectionString, replacements);

            // script to current 
            decimal minValue = -1;

            var historyScripts = scriptRetrievalEngine.GetHistoryScripts(minValue, maxScriptVersion);
            var holdingScripts = scriptRetrievalEngine.GetHoldingScripts();
            if (maxScriptVersion == decimal.MaxValue)
                maxScriptVersion = GetDbVersionFromFileName(historyScripts[historyScripts.Length - 1]);

            var builder = new SqlConnectionStringBuilder(ConfigHelper.LocalConnectionString);
            builder.InitialCatalog = "Demo_DEV";

            var customerAccessor = new CustomerAccessor();
            CustomerBackup[] backupRecords = customerAccessor.FindAllCustomerBackups();
            CustomerBackup foundCustomer = backupRecords.Where(br => br.CustomerId == customerId).FirstOrDefault();
            var success = false;

            if (foundCustomer != null)
            {
                string bak = foundCustomer.BackupLocation;
                    
                success = new DatabaseManager().CreateDatabaseFromBak(bak, builder.InitialCatalog, customerId, foundCustomer.BackupSchemaVersion, maxScriptVersion, replacements);

                if (!success)
                {
                    Console.WriteLine("FAILED DEV_HIST");
                    return;
                }

                if (holdingScripts != null && holdingScripts.Length > 0)
                {
                    var lastHoldScript = scriptRunner.RunScripts(holdingScripts, builder.ToString(), replacements);

                    if (lastHoldScript != holdingScripts[holdingScripts.Length - 1])
                    {
                        Console.WriteLine("FAILED DEV_HOLD");
                        return;
                    }
                }

                if (!success)
                {
                    Console.WriteLine("FAILED DEV_CUST");
                    return;
                }
            }
            else
            {
                var lastHistScript = scriptRunner.RunScripts(historyScripts, builder.ToString(), replacements);

                if (lastHistScript != historyScripts[historyScripts.Length - 1])
                {
                    Console.WriteLine("FAILED DEV_HIST");
                    return;
                }

                if (holdingScripts != null && holdingScripts.Length > 0)
                {
                    var lastHoldScript = scriptRunner.RunScripts(holdingScripts, builder.ToString(), replacements);

                    if (lastHoldScript != holdingScripts[holdingScripts.Length - 1])
                    {
                        Console.WriteLine("FAILED DEV_HOLD");
                        return;
                    }
                }
            }

            var devPostScripts = scriptRetrievalEngine.GetScripts("DevPost");
            replacements.Clear();
            replacements.Add("##MODE##", "DEV");
            var lastDevPost = scriptRunner.RunScripts(devPostScripts, builder.ToString(), replacements);

            if (devPostScripts != null && devPostScripts.Length > 0 && lastDevPost != devPostScripts[devPostScripts.Length - 1])
            {
                Console.WriteLine("FAILED DEV_POST");
                return;
            }

            var currentWorkingDirectory = Environment.CurrentDirectory;

            // ** TEST **
            if (testDb)
            {
                Environment.CurrentDirectory = currentWorkingDirectory;

                replacements.Clear();
                // now do same things for test
                replacements = new Dictionary<string, string>();
                replacements.Add("##MODE##", "TEST");
                scriptRunner.RunScripts(scripts, ConfigHelper.LocalConnectionString, replacements);

                // script to current 
                builder = new SqlConnectionStringBuilder(ConfigHelper.LocalConnectionString);
                builder.InitialCatalog = "Demo_Test";
                historyScripts = scriptRetrievalEngine.GetHistoryScripts(decimal.MinValue, maxScriptVersion);
                scriptRunner.RunScripts(historyScripts, builder.ToString(), replacements);
                scriptRunner.RunScripts(holdingScripts, builder.ToString(), replacements);
                scriptRunner.RunScripts(devPostScripts, builder.ToString(), replacements);
            }
        }

        public decimal GetAndRunScripts(string connectionString, decimal from, decimal to, long customerId, Dictionary<string, string> replacements)
        {
            var scriptEngine = new ScriptRetrivalEngine();
            string[] allScripts = scriptEngine.GetHistoryScripts(from, to);

            if (allScripts == null || allScripts.Length == 0)
            {
                Console.WriteLine("No scripts to run betwen {0} & {1}", from, to);
                return to;
            }

            var scriptRunner = new ScriptRunner();
            var lastSuccessfulScript = scriptRunner.RunScripts(allScripts, connectionString, replacements);
            var success = lastSuccessfulScript.Equals(allScripts[allScripts.Length - 1]);
            decimal lastSuccessfulDbVersion = GetDbVersionFromFileName(lastSuccessfulScript);

            if (success)
                lastSuccessfulDbVersion = to; // return decimal.Max if that was the 'to' requested.
            else
                Console.WriteLine("Last successful file: {0}", lastSuccessfulScript);
            return lastSuccessfulDbVersion;
        }

        public bool CreateDatabaseFromBak(string bak, string targetName, long customerId, decimal fromScript, decimal toScript, Dictionary<string, string> replacesments)
        {
            Console.WriteLine(string.Format("CreateDatabaseFromBak({0}, {1})", bak, targetName));
            bool success = false;
            try
            {
                var connString = FormatConnString(targetName);

                new DatabaseAccessor().RestoreDb(bak, connString);

                // BELOW SHOULD BE 10, WE DON'T WANT CREATE DB SCRIPTS, WHICH ARE 0.1.SQL
                var lastSuccessfulDbVersion = GetAndRunScripts(connString, fromScript, toScript, customerId, replacesments);
                success = lastSuccessfulDbVersion == toScript;
            }
            catch (Exception ex)
            {
                success = false;
            }

            return success;
        }


        #region helpers
        private static decimal GetDbVersionFromFileName(string scriptName)
        {
            decimal result = 0;
            scriptName = scriptName.Replace("\\", "/");
            scriptName = scriptName.Substring(scriptName.LastIndexOf('/') + 1);
            scriptName = scriptName.Substring(0, scriptName.LastIndexOf('.'));
            result = decimal.Parse(scriptName);
            return result;
        }
        private string FormatConnString(string input)
        {
            string result = input;
            if (!input.Contains("Data Source"))
            {
                result = string.IsNullOrEmpty(input) ? "" :
                    string.Format(ConfigHelper.TargetDatabase, input);
            }
            return result;
        }

        #endregion
    }
}
