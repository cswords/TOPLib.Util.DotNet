using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using MySql.Data.MySqlClient;
using System.Data.Common;
using TOPLib.Util.DotNet.Debug;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    
    public class BAM
    {
        public static BAM BOO(string conn, DataBaseType dbType)
        {
            return new BAM
            {
                DbConnStr = conn,
                DbType = dbType
            };
        }

        public enum DataBaseType { MsSql, MySql }

        public DataBaseType DbType { get; internal set; }

        public string DbConnStr { get; internal set; }

        public bool Execute(string sql)
        {
            switch (DbType)
            {
                case DataBaseType.MsSql:
                    return ExecuteFromMsSql(sql);
                case DataBaseType.MySql:
                    return ExecuteFromMySql(sql);
            }
            return false;
        }

        public DataTable Extract(string sql)
        {
            switch (DbType)
            {
                case DataBaseType.MsSql:
                    return ExtractFromMsSql(sql);
                case DataBaseType.MySql:
                    return ExtractFromMySql(sql);
            }
            return null;
        }

        public static IEnumerable<IDictionary<string,object>> ToLocalData(DataTable dataTable)
        {
            var result = new LinkedList<IDictionary<string, object>>();
            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    var rowDict = new Dictionary<string, object>();
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        rowDict[column.ColumnName] = row[column.ColumnName];
                    }
                    result.AddLast(rowDict);
                }
            }
            return result;
        }

        public bool DetectTable(string objectName)
        {
            DbConnection conn;

            switch (DbType)
            {
                case DataBaseType.MsSql:
                    conn = new SqlConnection(DbConnStr);
                    break;
                case DataBaseType.MySql:
                    conn = new MySqlConnection(DbConnStr);
                    break;
                default:
                    return false;
            }
            using (conn)
            {
                conn.Open();
                var restrictions = new string[] { null, null, objectName };
                var tableInfo = conn.GetSchema("Tables", restrictions);
                return tableInfo.Rows.Count > 0;
            }
        }

        public DataTable GetSchemaTable(string sql)
        {
            switch (DbType)
            {
                case DataBaseType.MsSql:
                    return GetSchemaFromMsSql(sql);
                case DataBaseType.MySql:
                    return GetSchemaFromMySql(sql);
            }
            return null;
        }

        private bool ExecuteFromMsSql(string sql)
        {
            try
            {

#if DEBUG
                Console.WriteLine(sql);
#endif

                using (var conn = new SqlConnection(DbConnStr))
                {
                    conn.Open();
                    var cmd = new SqlCommand(sql) { Connection = conn };
                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Default.Write("Failed MsSql execution.", e);
                return false;
            }
        }

        private bool ExecuteFromMySql(string sql)
        {
            try
            {
                sql = "SET SQL_SAFE_UPDATES=0;" + sql;
                if (!sql.EndsWith(";"))
                    sql += ";SET SQL_SAFE_UPDATES=1;";
                else
                    sql += "SET SQL_SAFE_UPDATES=1;";

#if DEBUG
                Console.WriteLine(sql);
#endif

                using (var conn = new MySqlConnection(DbConnStr))
                {
                    conn.Open();
                    var cmd = new MySqlCommand(sql) { Connection = conn };
                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch(Exception e)
            {
                Logger.Default.Write("Failed MySql execution.", e);
                return false;
            }

        }

        private DataTable ExtractFromMsSql(string sql)
        {
            try
            {
                var conn = new SqlConnection(DbConnStr);

                var adapter = new SqlDataAdapter();
                adapter.SelectCommand = new SqlCommand(sql) { Connection = conn };

                DataSet ds = new DataSet();
                adapter.Fill(ds);
                if (ds.Tables.Count > 0)
                    return ds.Tables[0];
            }
            catch (Exception e)
            {
                Logger.Default.Write("Failed MsSql extraction.", e); 
            }
            return null;
        }

        private DataTable ExtractFromMySql(string sql)
        {
            try
            {
                using (var conn = new MySqlConnection(DbConnStr))
                {
                    conn.Open();
                    var adapter = new MySqlDataAdapter();
                    adapter.SelectCommand = new MySqlCommand(sql) { Connection = conn };

                    DataSet ds = new DataSet();
                    adapter.Fill(ds);
                    if (ds.Tables.Count > 0)
                        return ds.Tables[0];
                }
            }
            catch (Exception e)
            {
                Logger.Default.Write("Failed MySql extraction.", e);
            }
            return null;

        }

        private DataTable GetSchemaFromMySql(string sql)
        {
            DataTable tblSchema;
            using (var conn = new MySqlConnection(DbConnStr))
            {
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    using (MySqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.KeyInfo))
                    {
                        tblSchema = rdr.GetSchemaTable();
                    }
                    conn.Close();
                }
            }
            return tblSchema;
        }

        private DataTable GetSchemaFromMsSql(string sql)
        {
            DataTable tblSchema;
            using (var conn = new SqlConnection(DbConnStr))
            {
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    conn.Open();
                    using (SqlDataReader rdr = cmd.ExecuteReader(CommandBehavior.KeyInfo))
                    {
                        tblSchema = rdr.GetSchemaTable();
                    }
                    conn.Close();
                }
            }
            return tblSchema;
        }

        public SingleTable From(string tableName)
        {
            return new SingleTable
            {
                Db = this,
                TextClause = tableName
            };
        }
    }

}
