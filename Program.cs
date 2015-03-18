using ScriptEngine.Accessors;
using ScriptEngine.Managers;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptEngine
{
    class Program
    {
        private static string MISSING_ARGUMENT_TEXT = "Failed: Expected -{0} argument";
        private static string FAILED_TO_GET_VALUE = "Failed: Unable to retrieve -{0} value";


        static void Main(string[] args)
        {
            if (!Directory.Exists(@"c:\temp"))
                Directory.CreateDirectory(@"c:\temp");

            var argStr = new StringBuilder();
            foreach (var arg in args)
            {
                argStr.Append(arg + " ");
            }

            //Logger.Verbose("Kads.exe {0}", argStr.ToString());

            var dictionary = ReadArgs(args);
            // validate
            if (dictionary.ContainsKey("Exception"))
            {
                // uh oh
            }
            else
            {
                // parse args
                RunCommand(dictionary);
            }
        }

        private static string RunCommand(Dictionary<string, string> arguments)
        {
            if (!arguments.ContainsKey("m") && !arguments.ContainsKey("devme"))
            {
                //Logger.Log("You must specify a comment (-m)");
                //Logger.Error("You must specify a comment (-m)");
                return string.Empty;
            }
            else if (arguments.ContainsKey("devme"))
            {
                return DevMe(arguments);
            }
            else if (arguments.ContainsKey("c"))
            {
                return UpgradeDatabase(arguments);
            }
            else
            {
                var helptext = @"Avaliable commands:
Dev Me: -devme <customer_number> [-db <dbversion>] [-notest]
Upgrade: -c <customer_number> [-db <dbversion>] -m <comment>";

                return helptext; 
            }
        }

        private static Dictionary<string, string> ReadArgs(string[] args)
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            for (int i = 0; i < args.Count(); i++)
            {
                string key;
                string value = null;
                if (args[i].StartsWith("-"))
                {
                    key = args[i].Substring(1).ToLower();
                    if ((i + 1) != args.Count())
                    {
                        if (!args[i + 1].StartsWith("-"))
                        {
                            value = args[i + 1];
                            i++;
                        }
                    }
                }
                else
                {
                    key = "Exception";
                    value = "Invalid argument provided (" + args[i] + ")";
                }
                //Logger.Verbose("Adding arg '{0}' '{1}'", key, value);
                result.Add(key, value);
            }
            return result;
        }

        #region command logic
        private static string DevMe(Dictionary<string, string> arguments)
        {
            long customerId = 0;
            if (arguments["devme"] != null)
            {
                long.TryParse(arguments["devme"], out customerId);
            }
            decimal toDbSchemaVersion = decimal.MaxValue;
            if (arguments.ContainsKey("db"))
            {
                var stringSchema = string.Empty;
                if (arguments.TryGetValue("db", out stringSchema))
                {
                    decimal tempSchema;
                    if (decimal.TryParse(stringSchema, out tempSchema))
                        toDbSchemaVersion = tempSchema;
                }
            }
            bool testDb = true;
            if (arguments.ContainsKey("notest"))
            {
                testDb = false;
            }

            new DatabaseManager().DevMe(customerId, toDbSchemaVersion, testDb);
            return string.Empty;
        }

        private static string UpgradeDatabase(Dictionary<string, string> arguments)
        {
            // -c ##customer_number## [-db <dbversion>] -m "comment"
            string customer_value = "";
            string db_value = "";

            if (arguments.ContainsKey("c"))
            {
                if (!arguments.TryGetValue("c", out customer_value))
                    return string.Format(FAILED_TO_GET_VALUE, "c");
            }
            else
            {
                return string.Format(MISSING_ARGUMENT_TEXT, "c");
            }

            if (arguments.ContainsKey("db"))
            {
                if (!arguments.TryGetValue("db", out db_value))
                    db_value = null;
            }

            try
            {
                ////////////////
                //ACTUAL CALL
                ////////////////
                long customerId = long.Parse(customer_value);

                var customer = new CustomerAccessor().Find(customerId);

                if (string.IsNullOrEmpty(db_value))
                {
                    db_value = GetLatestDBVersion();
                }
                decimal typedDbVersion = decimal.Parse(db_value);

                var newDbVersion = new DatabaseManager().GetAndRunScripts(customer.ConnectionString,
                    customer.DbSchemaVersion, typedDbVersion, customerId, null);

                var success = newDbVersion == typedDbVersion;

                return "Completed " + (success ? "successfully." : "unsuccessfully.");
            }
            catch (Exception e)
            {
                return "Failed to complete action. Verify that all the parameters are valid.";
            }
        }

        private static string GetLatestDBVersion()
        {
            var scriptGetter = new ScriptRetrivalEngine(); 
            string scriptName = scriptGetter.GetLatestHistoryScript();

            int startOfScriptName = scriptName.LastIndexOfAny(new char[] { '\\', '/' }) + 1;
            int length = scriptName.LastIndexOf('.') - startOfScriptName;
            string dbVersion = scriptName.Substring(startOfScriptName, length);

            return dbVersion;
        }

        #endregion

    }
}
