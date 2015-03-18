using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScriptEngine.Accessors
{
    public struct DbParam
    {
        public DbParam(string uniqueKey, object value, bool isId = false)
        {
            if (!uniqueKey.StartsWith("@"))
                uniqueKey = '@' + uniqueKey;

            this.UniqueKey = uniqueKey;
            this.Value = value;
            this.IsId = isId;
        }

        public bool IsId;
        public string UniqueKey;
        public object Value;
    }

    public interface IReadable
    {
        void FromReader(SqlDataReader reader);
    }

    public static class RawDbHelper
    {

        public static T[] ExecuteReader<T>(string connString, string sqlText, params DbParam[] paramList)
            where T : class, IReadable
        {
            if (ConfigHelper.IsVerbose)
                Console.WriteLine("Executing " + sqlText + " on " + connString);
            List<T> result = new List<T>();
            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    AddParameters(cmd, paramList);

                    cmd.CommandText = sqlText;

                    conn.Open();
                    using (var rdr = cmd.ExecuteReader())
                    {
                        while (rdr.Read())
                        {
                            result.Add(ParseReader<T>(rdr));
                        }
                    }
                }
            }

            return result.ToArray();
        }

        #region workers
        private static T ParseReader<T>(SqlDataReader rdr) where T : class, IReadable
        {
            T newObj = Activator.CreateInstance(typeof(T)) as T;
            newObj.FromReader(rdr);
            return newObj;
        }

        private static void AddParameters(SqlCommand cmd, DbParam[] paramList)
        {
            if (paramList != null
                && paramList.Length > 0)
            {
                foreach (var param in paramList)
                    cmd.Parameters.AddWithValue(param.UniqueKey, param.Value);
            }
        }
        #endregion
    }
}
