using HttpFileDownloaderLib;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ScriptEngine.Accessors
{
    public class DatabaseAccessor
    {
        public bool RestoreDb(string backupBak, string destinationConnString)
        {
            var result = false;

            Console.WriteLine(string.Format("Get database from {0} to {1} ", backupBak, destinationConnString));

            try
            {
                var targetDbName = TargetDatabaseName(destinationConnString);
                var masterConnString = MasterDatabaseString(destinationConnString);
                
                DropIfExists(masterConnString, targetDbName);

                RestoreBackup(masterConnString, targetDbName, backupBak);

                result = true;

            }
            catch (Exception ex)
            {
                
            }

            return result;
        }

        private string TargetDatabaseName(string connString)
        {
            var builder = new SqlConnectionStringBuilder(connString);
            return builder.InitialCatalog;
        }

        private string MasterDatabaseString(string connString)
        {
            var builder = new SqlConnectionStringBuilder(connString);
            builder.InitialCatalog = "master";
            return builder.ToString();
        }

        private void DropIfExists(string connString, string dbName)
        {
           
            Console.WriteLine("DropIfExists: " + dbName);;

            var sql = string.Format(@"
                IF EXISTS(SELECT name FROM sys.databases WHERE name = '{0}')
                BEGIN
                    alter database {0} set single_user with rollback immediate;
                    DROP DATABASE {0}
                END ", dbName);

            try
            {
                using (var conn = new SqlConnection(connString))
                {
                    conn.Open();

                    using (var cmd = new SqlCommand(sql, conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception e)
            {
                
            }
        }   

        private void RestoreBackup(string masterConnString, string targetDbName, string unzippedBak)
        {
            try
            {
                Console.WriteLine(string.Format("RestoreBackup({0}, {1}, {2})", masterConnString, targetDbName, unzippedBak));
                using (var conn = new SqlConnection(masterConnString))
                {
                    conn.Open();

                    var fileList = string.Format("restore filelistonly from disk='{0}'", unzippedBak);

                    var dataName = "";
                    var logName = "";

                    using (var cmd2 = new SqlCommand(fileList, conn))
                    {
                        cmd2.CommandTimeout = 600000;
                        using (var reader = cmd2.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var t = reader["Type"] as string;
                                var name = reader["LogicalName"] as string;

                                if (t == "D")
                                    dataName = name;
                                else
                                    logName = name;
                            }
                        }

                    }

                    var dataFilePath = Path.Combine(ConfigHelper.DBFileLocation, targetDbName + "_data.mdf");
                    var logFilePath = Path.Combine(ConfigHelper.DBFileLocation, targetDbName + "_log.ldf");

                    string restoreSql = string.Format(@"
                restore database {0} from disk='{1}'
                with move '{2}' to '{3}', 
                move '{4}' to '{5}'",
                    targetDbName, unzippedBak,
                    dataName, dataFilePath,
                    logName, logFilePath);

                    
                    using (var cmd = new SqlCommand(restoreSql, conn))
                    {
                        cmd.CommandTimeout = 600000;
                        cmd.ExecuteNonQuery();
                    }

                    var shrinkCmd = string.Format("DBCC SHRINKDATABASE ({0}, 2)", targetDbName);

                    using (var cmd3 = new SqlCommand(shrinkCmd, conn))
                    {
                        cmd3.CommandTimeout = 600000;
                        cmd3.ExecuteNonQuery();
                    }
                }

            }
            catch (Exception ex)
            {
            }
        }
    }
}
