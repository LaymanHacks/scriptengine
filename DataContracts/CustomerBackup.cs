using ScriptEngine.Accessors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptEngine.DataContracts
{
    public class CustomerBackup : IReadable
    {
        public long Id { get; set; }

        public long CustomerId { get; set; }

        public string BackupLocation { get; set; }

        public decimal BackupSchemaVersion { get; set; }

        public void FromReader(System.Data.SqlClient.SqlDataReader reader)
        {
            this.Id = Convert.ToInt64(reader["Id"]);
            this.CustomerId = Convert.ToInt64(reader["CustomerId"]);
            this.BackupLocation = reader["BackupLocation"] as string;
            this.BackupSchemaVersion = Convert.ToDecimal(reader["BackupSchemaVersion"]);
        }
    }

    public class CustomerBackupContainer
    {
        public CustomerBackup[] Backups { get; set; }
    }
}
