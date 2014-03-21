using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Db
{

    public abstract class FromClause
    {
        public string TextClause { get; internal set; }

        public BAM Db { get; internal set; }

        public T Where<T>(Constraint c) where T : QueryBase<T>, new()
        {
            return new T { From = this, WhereClause = c };
        }

        public DataTable Extract()
        {
            var sql = "SELECT * FROM " + TextClause;
            return Db.Extract(sql);
        }

        public JoinedTable InnerJoin(string tableName, Constraint c)
        {
            var result = new JoinedTable
            {
                Db = this.Db,
                TextClause = this.TextClause + " INNER JOIN " + tableName + " ON " + c
            };
            return result;
        }

        public JoinedTable LeftJoin(string tableName, Constraint c)
        {
            var result = new JoinedTable
            {
                Db = this.Db,
                TextClause = this.TextClause + " LEFT JOIN " + tableName + " ON " + c
            };
            return result;
        }

        public JoinedTable RightJoin(string tableName, Constraint c)
        {
            var result = new JoinedTable
            {
                Db = this.Db,
                TextClause = this.TextClause + " RIGHT JOIN " + tableName + " ON " + c
            };
            return result;
        }
    }

    public class JoinedTable : FromClause
    {

        public NonUpdatableQuery Where(Constraint c)
        {
            return Where<NonUpdatableQuery>(c);
        }

        public NonUpdatableQuery All()
        {
            return Where<NonUpdatableQuery>(null);
        }

        public IndexedJoinedTable IndexBy(params string[] keyFields)
        {
            return new IndexedJoinedTable
            {
                Table = this,
                KeyFields = keyFields
            };
        }
    }

    public class SingleTable : FromClause
    {

        public UpdatableQuery Where(Constraint c)
        {
            return Where<UpdatableQuery>(c);
        }

        public UpdatableQuery All()
        {
            return Where<UpdatableQuery>(null);
        }

        public IndexedSingleTable IndexBy(params string[] keyFields)
        {
            return new IndexedSingleTable
            {
                Table = this,
                KeyFields = keyFields
            };
        }

    }


    public class IndexedJoinedTable
    {
        public FromClause Table { get; internal set; }

        public string[] KeyFields { get; internal set; }

        public NonUpdatableQuery this[params object[] keyValues]
        {
            get
            {
                Constraint c = null;
                for (int i = 0; i < KeyFields.Length; i++)
                {
                    var cons = BOO.M(KeyFields[i], keyValues[i].ToString());
                    if (c == null)
                        c = cons;
                    else
                        c = c & cons;
                }
                return new NonUpdatableQuery
                {
                    From = this.Table,
                    WhereClause = c
                };
            }
        }
    }

    public class IndexedSingleTable
    {
        public FromClause Table { get; internal set; }

        public string[] KeyFields { get; internal set; }

        public UpdatableQuery this[params object[] keyValues]
        {
            get
            {
                Constraint c = null;
                for (int i = 0; i < KeyFields.Length; i++)
                {
                    var cons = BOO.M(KeyFields[i], keyValues[i].ToString());
                    if (c == null)
                        c = cons;
                    else
                        c = c & cons;
                }
                return new UpdatableQuery
                {
                    From = this.Table,
                    WhereClause = c
                };
            }
        }
    }
}
