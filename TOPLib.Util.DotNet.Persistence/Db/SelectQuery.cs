using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    internal class GroupByHelper : Joint, IGroupable
    {
        public override string ToSQL()
        {
            return LowerJoint.ToSQL();
        }

        public IGrouped this[string field]
        {
            get
            {
                var result = this.CreateUpper<Grouped>();
                result.groupFields = new List<string>();
                result.groupFields.Insert(0, field);
                return result;
            }
        }
    }

    internal class Grouped : Joint, IGrouped
    {
        internal IList<string> groupFields;

        public override string ToSQL()
        {
            var result = LowerJoint.ToSQL();
            if (groupFields.Count > 0)
            {
                result += "\nGROUP BY";
                int i = groupFields.Count;
                foreach (var field in groupFields)
                {
                    result += " " + Context.LeftBracket + field + Context.RightBracket;
                    i--;
                    if (i != 0) result += ",";
                }
            }
            return result;
        }

        public IGrouped this[string field]
        {
            get
            {
                var result = this.CreateUpper<Grouped>();
                result.groupFields = this.groupFields;
                result.groupFields.Insert(groupFields.Count, field);
                return result;
            }
        }

        public ISortable OrderBy
        {
            get
            {
                var result = this.CreateUpper<OrderByHelper>();
                return result;
            }
        }

        public ISelectable Select
        {
            get
            {
                var result = this.CreateUpper<SelectHelper>();
                return result;
            }
        }

    }

    internal class OrderByHelper : Joint, ISortable
    {

        public override string ToSQL()
        {
            var result = LowerJoint.ToSQL();
            result += "\nORDER BY ";
            return result;
        }

        public IAscSorted this[string field]
        {
            get
            {
                var result = this.CreateUpper<AscSorted>();
                result.field = field;
                return result;
            }
        }
    }

    internal abstract class AbstractSorted : OrderByHelper, ISorted
    {
        internal string field;

        public ISelectable Select
        {
            get
            {
                var result = this.CreateUpper<SelectHelper>();
                return result;
            }
        }

        public abstract override string ToSQL();
    }

    internal class AscSorted : AbstractSorted, IAscSorted
    {
        public override string ToSQL()
        {
            var result = LowerJoint.ToSQL();
            if (LowerJoint is ISorted)
                result += ", ";
            result += Context.LeftBracket + field + Context.RightBracket;
            return result;
        }

        public IDescSorted Desc
        {
            get
            {
                var result = this.CreateUpper<DescSorted>();
                return result;
            }
        }
    }

    internal class DescSorted : AbstractSorted, IDescSorted
    {
        public override string ToSQL()
        {
            var result = LowerJoint.ToSQL();
            result += " DESC";
            return result;
        }
    }

    internal abstract class AbstractSelectQuery : Joint, IExtractable, ISingleSelectable
    {
        internal IList<string> tohide = new List<string>();

        internal IDictionary<string, string> mapping;

        public DataTable Extract()
        {
            var result= Context.Extract(this.ToSQL());
            foreach (var c in tohide)
            {
                result.Columns.Remove(c);
            }
            return result;
        }

        public override string ToSQL()
        {
            var result = string.Empty;
            if (mapping.Count == 1 & mapping.First().Key == "*")
            {
                result = "SELECT *\n" + LowerJoint.ToSQL();
            }
            else
            {
                result += "SELECT";
                int i = mapping.Count;
                foreach (var kv in mapping)
                {
                    result += " " + kv.Key;
                    if (kv.Key != Context.LeftBracket + kv.Value + Context.RightBracket)
                    {
                        result += " AS " + Context.LeftBracket + kv.Value + Context.RightBracket;
                    }
                    i--;
                    if (i > 0) result += ",";
                }
                var b = LowerJoint;
                while (b is AbstractSelectQuery)
                {
                    b = b.LowerJoint;
                }
                result += "\n" + b.ToSQL();
            }

            return result;
        }

        public INoAliaseExtractable this[string field]
        {
            get
            {
                var exp = field == "*" ? "*" : Context.LeftBracket + field + Context.RightBracket;
                var result = this.CreateUpper<NoAliasSelectQuery>();
                result.mapping = this.mapping;
                result.mapping.Add(exp, field);
                result.selecting = exp;
                return result;
            }
        }

        public INoAliaseExtractable Exp(string exp)
        {
            var result = this.CreateUpper<NoAliasSelectQuery>();
            result.mapping = this.mapping;
            result.mapping.Add(exp, "Nameless");
            result.selecting = exp;
            return result;
        }

    }

    internal class PagableSelectQuery : AbstractSelectQuery, IFetchable, ISingleSelectable
    {
        public IFetchedExtractable Fetch(long skip, long take)
        {
            var result = this.CreateUpper<PagedSelectQuery>();
            result.Skip = skip;
            result.Take = take;
            result.mapping = this.mapping;
            return result;
        }
    }

    internal class PagedSelectQuery : AbstractSelectQuery, IFetchedExtractable, ISingleSelectable
    {
        public long Skip { get; internal set; }

        public long Take { get; internal set; }

        public override string ToSQL()
        {
            return Context.Fetch(LowerJoint.ToSQL(), Skip, Take, this);
        }
    }

    internal class SelectHelper : Joint, ISelectable
    {
        public INoAliaseExtractable this[string field]
        {
            get
            {
                var exp = field == "*" ? "*" : Context.LeftBracket + field + Context.RightBracket;
                var result = this.CreateUpper<NoAliasSelectQuery>();
                result.mapping = new Dictionary<string, string>();
                result.mapping.Add(exp, field);
                result.selecting = exp;
                return result;
            }
        }

        public INoAliaseExtractable Exp(string exp)
        {
            var result = this.CreateUpper<NoAliasSelectQuery>();
            result.mapping = new Dictionary<string, string>();
            result.mapping.Add(exp, "Nameless");
            result.selecting = exp;
            return result;
        }

        public IExtractable this[IDictionary<string, string> fields]
        {
            get
            {
                var result = this.CreateUpper<PagableSelectQuery>();
                result.mapping = new Dictionary<string, string>();
                foreach (var kv in fields)
                {
                    result.mapping.Add(Context.LeftBracket + kv.Key + Context.RightBracket, kv.Value);
                }
                return result;
            }
        }

        public override string ToSQL()
        {
            return LowerJoint.ToSQL();
        }
    }

    internal class AliasedSelectQuery : PagableSelectQuery, IAliasedExtractable
    {
    }

    internal class NoAliasSelectQuery : PagableSelectQuery, INoAliaseExtractable
    {
        internal string selecting = null;

        public IAliasedExtractable As(string alias)
        {
            var result = this.CreateUpper<AliasedSelectQuery>();
            result.mapping = this.mapping;
            result.mapping[selecting] = alias;
            result.LowerJoint = this.LowerJoint;
            return result;
        }
    }

}
