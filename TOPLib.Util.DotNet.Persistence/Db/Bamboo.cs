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

        public string DbConnStr { get; internal set; }

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
                    cmd.ExecuteNonQuery();
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.Default.Write("Failed execution.", e);
                return false;
            }
        }

        public virtual DataTable Extract(string sql)
        {
            try
            {
                var conn = this.Connect();

                var adapter = this.NewAdapter();
                adapter.SelectCommand = conn.CreateCommand();
                adapter.SelectCommand.CommandText = sql;

                DataSet ds = new DataSet();
                adapter.Fill(ds);
                if (ds.Tables.Count > 0)
                    return ds.Tables[0];
            }
            catch (Exception e)
            {
                Logger.Default.Write("Failed extraction.", e);
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
                var restrictions = new string[] { null, null, objectName };
                var tableInfo = conn.GetSchema("Tables", restrictions);
                return tableInfo.Rows.Count > 0;
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

        public abstract string Fetch(IFetchable query, long skip, long take);
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

        public override string Fetch(IFetchable query, long skip, long take)
        {
            //OFFSET is implemented in SQL Server 2012
            //var result = "SELECT * FROM (\n" + sql.Indentation() + "\n) T";
            //result += "\nOFFSET " + (pageNo * pageSize).ToString() + " ROWS";
            //result += "\nFETCH NEXT " + pageSize.ToString() + " ROWS ONLY";

            var ridName = "RID_" + DateTime.Now.Second.ToString();
            var q = (AbstractSelectQuery)query;
            q.tohide.Add(ridName);

            var orderByClause = string.Empty;
            Joint q2 = q;
            while (q2.LowerJoint!=null&!(q2 is ISorted))
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
                q.tohide.Add(conName);
            }
            //else
            //{
            //    while (q2.LowerJoint != null & q2 is ISorted)
            //    {
            //        q2 = q2.LowerJoint;
            //    }
            //    query = (IFetchable)q2;
            //}

            var result = string.Empty;
            if (q.mapping.Count == 1 & q.mapping.First().Key == "*")
            {
                result = "SELECT * FROM (";
            }
            else
            {
                result = "SELECT";
                int i = q.mapping.Count;
                foreach (var kv in q.mapping)
                {
                    if (!q.tohide.Contains(kv.Key))
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
            result += query.ToSQL().Replace(orderByClause, string.Empty).Trim().Indentation().Indentation() + "\n\t) T\n) TT) TTT";
            result += "\nWHERE " + LeftBracket + ridName + RightBracket + " BETWEEN " + (skip + 1).ToString() + " AND " + (skip + take).ToString();
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

        public override string Fetch(IFetchable query, long skip, long take)
        {
            var result = "SELECT * FROM (\n" + query.ToSQL().Indentation() + "\n) T";
            result += "\nLIMIT " + skip.ToString() + ", " + take.ToString();
            return result;
        }
    }
}
