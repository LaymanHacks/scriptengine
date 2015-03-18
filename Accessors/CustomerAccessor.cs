using ScriptEngine.DataContracts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ScriptEngine.Accessors
{
    class CustomerAccessor
    {
        public CustomerBackup[] FindAllCustomerBackups()
        {
            var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            var backupXml = string.Empty;

            foreach (var name in names)
            {
                if (name.EndsWith("CustomerBackup.xml"))
                {
                    using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var serializer = new XmlSerializer(typeof(CustomerBackupContainer));
                            var result = serializer.Deserialize(reader) as CustomerBackupContainer;
                            if (result != null)
                                return result.Backups;
                        }
                    }
                }
            }

            return null;
        }

        public Customer Find(long id)
        {
            try
            {
                var result = RawDbHelper.ExecuteReader<Customer>(ConfigHelper.CustomerListDatabase, @"select c.*
customers c 
where c.id = @id", new DbParam("@id", id, true));

                if (result == null)
                    return null;
                return result.FirstOrDefault();
            }
            catch (Exception e)
            {
                
            }
            return null;
        }
    }
}
