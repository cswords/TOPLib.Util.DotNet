using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using TOPLib.Util.DotNet.Debug;
using TOPLib.Util.DotNet.Persistence.Util;

namespace TOPLib.Util.DotNet.Persistence.Db
{

    internal class Database<T> : IDatabase
        where T:Bamboo, new()
    {
        public System.Globalization.CultureInfo DomesticCulture { get; private set; }

        internal Database(string conn)
        {
            DomesticCulture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            this.DbConnStr = conn;
        }

        public string DbConnStr { get; private set; }

        public bool DetectTable(string objectName)
        {
            using (Bamboo db = ((Bamboo)this.CreateContext()))
            {
                try
                {
                    if (objectName.Contains("."))
                    {
                        DataTable countOne;
                        countOne = db[objectName].All.Select.Exp("COUNT(1)").As("One").Extract();
                        return countOne.Rows[0]["One"] != null;
                    }
                    else
                    {
                        var restrictions = new string[] { null, null, objectName };
                        var tableInfo = db.Connection.GetSchema("Tables", restrictions);
                        return tableInfo.Rows.Count > 0;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public ISqlContext CreateContext()
        {
            var result = new T { Db = this };
            result.Init();
            return result;
        }
    }

    public abstract class Bamboo : ISqlContext
    {
        internal DbConnection Connection { get; private set; }
        internal DbCommand Command { get; private set; }
        internal DbDataAdapter Adapter { get; private set; }

        internal void Init()
        {
            this.Connection = this.Connect();
            this.Connection.Open();
            this.Command = this.Connection.CreateCommand();
            this.Adapter = this.NewAdapter();
            this.ContextUid = Guid.NewGuid();
        }

        internal Guid ContextUid { get; private set; }

        public abstract string LeftBracket { get; }
        public abstract string RightBracket { get; }
        
        public bool WriteLog { get; set; }

        public IDatabase Db { get; internal set; }

        internal abstract DbConnection Connect();
        internal abstract DbDataAdapter NewAdapter();

        public virtual bool Execute(string sql, int timeout = 30)
        {
            try
            {
                sql = "--Execute " + ContextUid.ToString() + "\r\n" + sql;
#if DEBUG
                Console.WriteLine(sql);
#endif
                this.Command.CommandTimeout = timeout;
                this.Command.CommandText = sql;
                ApplyParameters(this.Command, this.Parameters);

                this.Command.ExecuteNonQuery();
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

        public virtual DataTable Extract(string sql, int timeout = 30)
        {
            try
            {
                DataSet ds = new DataSet();
                sql = "--Exetract " + ContextUid.ToString() + "\r\n" + sql;
#if DEBUG
                Console.WriteLine(sql);
#endif
                this.Command.CommandTimeout = timeout;
                this.Command.CommandText = sql;
                ApplyParameters(this.Command, this.Parameters);

                this.Adapter.SelectCommand = this.Command;
                this.Adapter.Fill(ds);
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

        public DataTable GetSchemaTable(string sql, int timeout = 30)
        {
            DataTable tblSchema;

            sql = "--Schema " + ContextUid.ToString() + "\r\n" + sql;
#if DEBUG
            Console.WriteLine(sql);
#endif
            this.Command.CommandTimeout = timeout;
            this.Command.CommandText = sql;
            ApplyParameters(this.Command, this.Parameters);

            //this.Command.CommandType = CommandType.Text;
            using (IDataReader rdr = this.Command.ExecuteReader(CommandBehavior.KeyInfo))
            {
                tblSchema = rdr.GetSchemaTable();
            }
            return tblSchema;
        }

        public abstract string Fetch(IFetchedExtractable query);

        protected void ApplyParameters(DbCommand command, ReadOnlyDictionary<string, object> parameters)
        {
            var c = command;
            if (parameters != null)
            {
                foreach (var kv in parameters)
                {
                    var existing = c.Parameters.IndexOf(kv.Key);
                    if (existing>=0)
                    {
                        var par = c.Parameters[existing];
                        par.Value = kv.Value == null ? DBNull.Value : kv.Value;
                    }
                    else
                    {
                        var par = c.CreateParameter();
                        par.ParameterName = kv.Key;
                        par.Value = kv.Value == null ? DBNull.Value : kv.Value;
                        c.Parameters.Add(par);
                    }
                }
            }
        }

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

        public void Dispose()
        {
            this.Adapter.Dispose();
            this.Adapter = null;
            this.Command.Dispose();
            this.Command = null;
            this.Connection.Close();
            this.Connection.Dispose();
            this.Connection = null;
            this.parameters = null;
            this.Db = null;
        }

        private IDictionary<string, object> parameters=null;

        public ReadOnlyDictionary<string, object> Parameters
        {
            get
            {
                if (parameters == null)
                    parameters = new Dictionary<string, object>();
                return new ReadOnlyDictionary<string, object>(parameters);
            }
        }

        public void SetParameter(string name, object value)
        {
            if (parameters == null)
            {
                parameters = new Dictionary<string, object>();
                parameters.Add(name, value);
            }
            else
            {
                if (parameters.ContainsKey(name))
                    parameters[name] = value;
                else
                    parameters.Add(name, value);
            }
        }

        public void TakeOver<T>(T query)
            where T : ISql
        {
            if (query is JointBase)
            {
                var j = (JointBase)(object)query;
                while (j.LowerJoint != null)
                {
                    j.Context = this;
                }
            }
        }
    }
    
    public class MsSQLDb : Bamboo
    {

        internal override DbConnection Connect()
        {
            return new System.Data.SqlClient.SqlConnection(Db.DbConnStr);
        }

        internal override DbDataAdapter NewAdapter()
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
            return new MySql.Data.MySqlClient.MySqlConnection(Db.DbConnStr);
        }

        internal override DbDataAdapter NewAdapter()
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

        public override bool Execute(string sql, int timeout = 30)
        {
            var sql2 = "SET SQL_SAFE_UPDATES=0;" + sql;
            if (!sql.EndsWith(";"))
                sql2 += ";SET SQL_SAFE_UPDATES=1;";
            else
                sql2 += "SET SQL_SAFE_UPDATES=1;";
            return base.Execute(sql2, timeout);
        }

        public override string Fetch(IFetchedExtractable query)
        {
            var result = "SELECT * FROM (\n" + query.ToSQL().Indentation() + "\n) T";
            result += "\nLIMIT " + query.Skip.ToString() + ", " + query.Take.ToString();
            return result;
        }
    }

    public class PgSQLDb : Bamboo
    {

        internal override DbConnection Connect()
        {
            return new Npgsql.NpgsqlConnection(Db.DbConnStr);
        }

        internal override DbDataAdapter NewAdapter()
        {
            return new Npgsql.NpgsqlDataAdapter();
        }

        public override string LeftBracket
        {
            get { return "\""; }
        }

        public override string RightBracket
        {
            get { return "\""; }
        }

        public override string Fetch(IFetchedExtractable query)
        {
            throw new NotSupportedException();
        }
    }
}
