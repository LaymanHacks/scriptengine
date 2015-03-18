using ScriptEngine.Accessors;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptEngine.DataContracts
{
       
    public class Customer : IReadable
    {
        public Customer()
        {

        }

        public long Id { get; set; }

        public string Name { get; set; }

        public string ConnectionString { get; set; }

        public long AppVersion { get; set; }

        public decimal DbSchemaVersion { get; set; }           

        public bool IsProduction { get; set; }

        public string LastBackup { get; set; }

        public bool IsAutoBuild { get; set; }


        public string IdString
        {
            get
            {
                return this.Id.ToString();
            }
        }

        public void FromReader(System.Data.SqlClient.SqlDataReader reader)
        {
            this.Id = Convert.ToInt64(reader["Id"]);
            this.Name = reader["Name"] as string;
            this.ConnectionString = reader["ConnectionString"] as string;
            this.AppVersion = ReadInt64(reader, "AppVersion");          
            this.DbSchemaVersion = ReadDecimal(reader, "DbSchemaVersion");
            this.IsProduction = ReadBool(reader, "IsProduction", false);
            this.LastBackup = reader["LastBackup"] as string;
            this.IsAutoBuild = ReadBool(reader, "IsAutoBuild", false);
                
        }

        private decimal ReadDecimal(SqlDataReader reader, string columnName, decimal defaultValue = 0)
        {
            if (reader[columnName] == null || reader[columnName] == DBNull.Value)
                return defaultValue;

            return Convert.ToDecimal(reader[columnName]);
        }

        private long ReadInt64(SqlDataReader reader, string columnName, long defaultValue = 0)
        {
            if (reader[columnName] == null || reader[columnName] == DBNull.Value)
                return defaultValue;

            return Convert.ToInt64(reader[columnName]);
        }

        private bool ReadBool(SqlDataReader reader, string columnName, bool defaultValue = false)
        {
            if (reader[columnName] == null || reader[columnName] == DBNull.Value)
                return defaultValue;

            return Convert.ToBoolean(reader[columnName]);
        }
    }
}
