using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    public class NonUpdatableQuery : QueryBase<NonUpdatableQuery>, ISortable, IFetchable
    {
    }

    public class UpdatableQuery : QueryBase<UpdatableQuery>, IUpdatable, ISortable, IFetchable
    {

        public new object this[string valueField]
        {
            set
            {
                this.Update(valueField, value);
            }
            get
            {
                return base[valueField];
            }
        }

        public new IDictionary<string, object> this[params string[] valueFields]
        {
            set
            {
                this.Update(value);
            }
            get
            {
                return base[valueFields];
            }
        }
    }

    public class SortedQuery : QueryBase<SortedQuery>, IFetchable
    {
        public ISortable BaseQuery { get; internal set; }

        private IDictionary<string, bool> _OrderByClause = new Dictionary<string, bool>();

        public IDictionary<string, bool> OrderByClause { get { return _OrderByClause; } }
    }

    public class FetchedQuery : QueryBase<FetchedQuery>, ISelectable
    {
        public IFetchable BaseQuery { get; internal set; }

        public long Skip { get; internal set; }

        public long Take { get; internal set; }
    }
}
