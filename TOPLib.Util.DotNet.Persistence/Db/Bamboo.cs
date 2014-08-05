using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using TOPLib.Util.DotNet.Debug;

namespace TOPLib.Util.DotNet.Persistence.Db
{

    public abstract class Bamboo : IDatabase
    {
        public System.Globalization.CultureInfo DomesticCulture { get; private set; }

        public Bamboo()
        {
            DomesticCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
        }

        public abstract string LeftBracket { get; }
        public abstract string RightBracket { get; }

        public ITable this[string name]
        {
            get
            {
                var result = new Table(name);
                result.Context = this;
                result.LowerJoint = null;
                return result;
            }
        }

        public bool WriteLog { get; set; }

        public string DbConnStr { get; internal set; }

        internal class BambooParameter : IParameter
        {
            public IRowSchema RowSchema { get; internal set; }
            public object Value { get; internal set; }
        }

        internal IDictionary<string, IParameter> parameters = new Dictionary<string, IParameter>();

        public virtual IParameter SetParameter(string name, IRowSchema rowSchema, object value)
        {
            name = name == null ? string.Empty : name;
            var key = name.StartsWith("@") ? name : "@" + name;
            var result = new BambooParameter
            {
                RowSchema = rowSchema,
                Value = value
            };
            if (parameters.ContainsKey(key))
                parameters[key] = result;
            else
                parameters.Add(key, result);

            return result;
        }

        public void ClearParameters()
        {
            parameters.Clear();
        }

        public virtual bool Execute(string sql)
        {
            try
            {
#if DEBUG
                Console.WriteLine(sql);
#endif
                using (var conn = this.Connect())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    ApplyParameters(cmd);

                    cmd.ExecuteNonQuery();
                }

                parameters.Clear();
                return true;
            }
            catch (Exception e)
            {
                if (WriteLog)
                {
                    Logger.Default.Write("Failed execution.", e);
                    return false;
                }
                else throw e;
            }
        }

        public virtual DataTable Extract(string sql)
        {
            try
            {
                DataSet ds = new DataSet();
                using (var conn = this.Connect())
                {
                    conn.Open();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    ApplyParameters(cmd);

                    var adapter = this.NewAdapter();
                    adapter.SelectCommand = cmd;

                    adapter.Fill(ds);
                }
                parameters.Clear();
                if (ds.Tables.Count > 0)
                    return ds.Tables[0];
            }
            catch (Exception e)
            {
                if (WriteLog)
                    Logger.Default.Write("Failed extraction.", e);
                else throw e;
            }
            return null;
        }

        internal abstract DbConnection Connect();
        internal abstract IDbDataAdapter NewAdapter();

        public bool DetectTable(string objectName)
        {
            DbConnection conn = this.Connect();

            using (conn)
            {
                conn.Open();
                try
                {
                    if (objectName.Contains("."))
                    {
                        var countOne = this[objectName].All.Select.Exp("COUNT(1)").As("One").Extract();
                        return countOne.Rows[0]["One"] != null;
                    }
                    else
                    {
                        var restrictions = new string[] { null, null, objectName };
                        var tableInfo = conn.GetSchema("Tables", restrictions);
                        return tableInfo.Rows.Count > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public DataTable GetSchemaTable(string sql)
        {
            DataTable tblSchema;
            using (var conn = this.Connect())
            {
                using (DbCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.CommandType = CommandType.Text;
                    ApplyParameters(cmd);
                    conn.Open();
                    using (IDataReader rdr = cmd.ExecuteReader(CommandBehavior.KeyInfo))
                    {
                        tblSchema = rdr.GetSchemaTable();
                    }
                    conn.Close();
                }
            }
            return tblSchema;
        }

        public abstract string Fetch(IFetchedExtractable query);
        
        public void ApplyParameters(DbCommand command)
        {
            var c = command;
            foreach (var kv in parameters)
            {
                var par = c.CreateParameter();
                par.ParameterName = kv.Key;
                par.Value = kv.Value.Value == null ? DBNull.Value : kv.Value.Value;
                c.Parameters.Add(par);
            }
        }
    }


    public class MsSQLDb : Bamboo
    {

        internal override DbConnection Connect()
        {
            return new System.Data.SqlClient.SqlConnection(DbConnStr);
        }

        internal override IDbDataAdapter NewAdapter()
        {
            return new System.Data.SqlClient.SqlDataAdapter();
        }

        public override string LeftBracket
        {
            get { return "["; }
        }

        public override string RightBracket
        {
            get { return "]"; }
        }

        public override string Fetch(IFetchedExtractable query)
        {
            //OFFSET is implemented in SQL Server 2012
            //var result = "SELECT * FROM (\n" + sql.Indentation() + "\n) T";
            //result += "\nOFFSET " + (pageNo * pageSize).ToString() + " ROWS";
            //result += "\nFETCH NEXT " + pageSize.ToString() + " ROWS ONLY";

            var ridName = "RID_" + DateTime.Now.Second.ToString();
            var q = (AbstractSelectQuery)query;
            //q.tohide.Clear();

            //q.tohide.Add(ridName);

            var orderByClause = string.Empty;
            Joint q2 = q;
            while (q2.LowerJoint != null & !(q2 is ISorted))
            {
                q2 = q2.LowerJoint;
            }
            if (q2 is ISorted)
                orderByClause = ((AbstractSorted)q2).clause;


            string conName = null;
            if (orderByClause == string.Empty)
            {
                conName = "CON_" + ridName.Substring(4);
                orderByClause = "ORDER BY " + LeftBracket + conName + RightBracket;
                //q.tohide.Add(conName);
            }

            var result = string.Empty;
            if (q.mapping.Count == 1 & q.mapping.First().Key == "*")
            {
                result = "SELECT";
                var schema = ((IExtractable)q.LowerJoint).GetSchema();
                int i = schema.Count();
                foreach (var rs in schema)
                {
                    result += " " + this.LeftBracket + rs.FieldName + this.RightBracket;
                    i--;
                    if (i > 0) result += ",";
                }
                result += " FROM (";
            }
            else
            {
                result = "SELECT";
                int i = q.mapping.Count;
                foreach (var kv in q.mapping)
                {
                    //if (!q.tohide.Contains(kv.Key))
                    result += " " + this.LeftBracket + kv.Key + this.RightBracket;
                    i--;
                    if (i > 0) result += ",";
                }
                result += " FROM (";
            }
            result += "\nSELECT *, ";
            result += "\n\tROW_NUMBER() OVER (" + orderByClause + ")";
            result += "\n\t\tAS " + LeftBracket + ridName + RightBracket;
            result += "\nFROM (";
            result += "\n\tSELECT *";
            if (conName != null)
                result += ", \n\t\t1 AS " + LeftBracket + conName + RightBracket;
            result += "\n\tFROM (\n";
            result += q.LowerJoint.ToSQL().Replace(orderByClause, string.Empty).Trim().Indentation().Indentation() + "\n\t) T\n) TT) TTT";
            result += "\nWHERE " + LeftBracket + ridName + RightBracket + " BETWEEN " + (query.Skip + 1).ToString() + " AND " + (query.Skip + query.Take).ToString();
            if (conName == null)
                result += "\n" + orderByClause;
            return result;
        }

    }

    public class MySQLDb : Bamboo
    {

        internal override DbConnection Connect()
        {
            return new MySql.Data.MySqlClient.MySqlConnection(DbConnStr);
        }

        internal override IDbDataAdapter NewAdapter()
        {
            return new MySql.Data.MySqlClient.MySqlDataAdapter();
        }

        public override string LeftBracket
        {
            get { return "`"; }
        }

        public override string RightBracket
        {
            get { return "`"; }
        }

        public override bool Execute(string sql)
        {
            var sql2 = "SET SQL_SAFE_UPDATES=0;" + sql;
            if (!sql.EndsWith(";"))
                sql2 += ";SET SQL_SAFE_UPDATES=1;";
            else
                sql2 += "SET SQL_SAFE_UPDATES=1;";
            return base.Execute(sql2);
        }

        public override string Fetch(IFetchedExtractable query)
        {
            var result = "SELECT * FROM (\n" + query.ToSQL().Indentation() + "\n) T";
            result += "\nLIMIT " + query.Skip.ToString() + ", " + query.Take.ToString();
            return result;
        }
    }

}
