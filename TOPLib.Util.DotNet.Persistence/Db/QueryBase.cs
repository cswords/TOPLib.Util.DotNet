using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Text.RegularExpressions;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    public abstract class QueryBase<T>:ISelectable
        where T : QueryBase<T>
    {
        public FromClause From { get; internal set; }

        public Constraint WhereClause { get; internal set; }

        #region Retrieve Data

        public virtual object this[string valueField]
        {
            get
            {
                var table = this.Select(new string[] { valueField });
                if (table.Count() != 1)
                    return null;
                else
                {
                    var queryField = QueryField.Parse(valueField);
                    return table.First()[queryField.Alias];
                }
            }
        }

        public IDictionary<string, object> this[params string[] valueFields]
        {
            get
            {
                var table = this.Select(valueFields);
                if (table.Count() != 1)
                    return null;
                else
                {
                    var result = table.First();
                    return result;
                }
            }
        }

        #endregion

    }

    public interface ISelectable { }

    public interface ISortable : ISelectable { }

    public interface IFetchable : ISelectable { }

    public interface IUpdatable : ISortable, IFetchable { }
}
