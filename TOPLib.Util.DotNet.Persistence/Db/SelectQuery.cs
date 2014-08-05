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
            return LowerJoint.ToSQL();
        }

        public IAscSorted this[string field]
        {
            get
            {
                var result = this.CreateUpper<AscSorted>();
                result.field = Context.LeftBracket + field + Context.RightBracket;
                result.clause += " " + result.field;
                return result;
            }
        }


        public IAscSorted Exp(string exp)
        {
            var result = this.CreateUpper<AscSorted>();
            result.field = exp;
            result.clause += " " + result.field;
            return result;
        }
    }

    internal abstract class AbstractSorted : Joint, ISorted
    {
        internal string clause = "ORDER BY";

        internal string field;

        public ISelectable Select
        {
            get
            {
                var result = this.CreateUpper<SelectHelper>();
                return result;
            }
        }

        public IAscSorted this[string field]
        {
            get
            {
                var result = this.CreateUpper<AscSorted>();
                result.field = Context.LeftBracket + field + Context.RightBracket;
                result.clause += ", " + result.field;
                return result;
            }
        }


        public IAscSorted Exp(string exp)
        {
            var result = this.CreateUpper<AscSorted>();
            result.field = exp;
            result.clause += ", " + result.field;
            return result;
        }

        public override string ToSQL()
        {
            var result = LowerJoint.ToSQL();
            if (this.LowerJoint is OrderByHelper)
            {
                result += "\n" + clause;
            }
            else
            {
                result = result.Replace(((AbstractSorted)this.LowerJoint).clause, this.clause);
            }
            return result;
        }
    }

    internal class AscSorted : AbstractSorted, IAscSorted
    {
        public IDescSorted Desc
        {
            get
            {
                var result = this.CreateUpper<DescSorted>();
                result.clause = this.clause + " DESC";
                return result;
            }
        }
    }

    internal class DescSorted : AbstractSorted, IDescSorted
    {
    }

    internal abstract class AbstractSelectQuery : Joint, IExtractable, ISingleSelectable
    {
        //internal IList<string> tohide = new List<string>();

        internal IDictionary<string, string> mapping;

        public DataTable Extract()
        {
            var result = Context.Extract(this.ToSQL());
            //if (result != null)
            //{
            //    foreach (var c in tohide)
            //    {
            //        if (result.Columns.Contains(c))
            //            result.Columns.Remove(c);
            //    }
            //}
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
                    result += " " + kv.Value;
                    if (Context.LeftBracket + kv.Key + Context.RightBracket != kv.Value)
                    {
                        result += " AS " + Context.LeftBracket + kv.Key + Context.RightBracket;
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
                result.mapping.Add(field, exp);
                result.selecting = result.mapping.Single(p => p.Key == field);
                return result;
            }
        }

        public INoAliaseExtractable Exp(string exp)
        {
            var result = this.CreateUpper<NoAliasSelectQuery>();
            result.mapping = this.mapping;
            result.mapping.Add(exp, exp);
            result.selecting = result.mapping.Single(p => p.Key == exp);
            return result;
        }

        public IFillingQuery Fill(string tableName)
        {
            var result = this.CreateUpper<FillingQuery>();
            result.tableName = tableName;
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
            return Context.Fetch(this);
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
                result.mapping.Add(field, exp);
                result.selecting = result.mapping.Single(p => p.Key == field);
                return result;
            }
        }

        public INoAliaseExtractable Exp(string exp)
        {
            var result = this.CreateUpper<NoAliasSelectQuery>();
            result.mapping = new Dictionary<string, string>();
            result.mapping.Add(exp, exp);
            result.selecting = result.mapping.Single(p => p.Key == exp);
            return result;
        }

        public IFetchable this[IDictionary<string, string> fields]
        {
            get
            {
                var result = this.CreateUpper<PagableSelectQuery>();

                IEnumerable<IRowSchema> tableSchema;

                Joint q = this;
                while (!(q is Query))
                {
                    q = q.LowerJoint;
                }
                tableSchema = ((Query)q).Select["*"].GetSchema();

                result.mapping = new Dictionary<string, string>();
                foreach (var kv in fields)
                {
                    var s = tableSchema.Where(f => f.FieldName == kv.Value);
                    if (s.Count() > 0)
                    {
                        result.mapping.Add(kv.Key, Context.LeftBracket + kv.Value + Context.RightBracket);
                    }
                    else
                    {
                        result.mapping.Add(kv.Key, "'" + kv.Value + "'");
                    }
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
        internal KeyValuePair<string, string> selecting;

        public IAliasedExtractable As(string alias)
        {
            var result = this.CreateUpper<AliasedSelectQuery>();
            result.mapping = this.mapping;
            result.mapping.Add(alias, selecting.Value);
            result.mapping.Remove(selecting);
            result.LowerJoint = this.LowerJoint;
            return result;
        }
    }
    
    internal class FillingQuery: Leaf, IFillingQuery
    {
        internal string tableName;

        public override string ToSQL()
        {
            var baseSql = LowerJoint.ToSQL();
            var result = string.Empty;
            var tableQuery=(tableName.StartsWith(Context.LeftBracket)&tableName.EndsWith(Context.RightBracket))?tableName:Context.LeftBracket+tableName+Context.RightBracket;
            if (Context.DetectTable(tableName))
            {
                //insert into select
                var sourceSchema = ((IExtractable)LowerJoint).GetSchema().Where(s => !s.IsHidden).ToArray();
                var targetSchema = ((IExtractable)Context[tableName].All.Select["*"]).GetSchema().ToArray();

                if(sourceSchema.Length!=targetSchema.Length)
                    throw new Exception("U suck!");

                result += "INSERT INTO " + tableQuery;
                result += "\nSELECT";

                for (int i = 0; i < sourceSchema.Length; i++)
                {
                    result += " CAST(" + Context.LeftBracket + sourceSchema[i].FieldName + Context.RightBracket + " AS " + targetSchema[i].SqlType + ") AS "
                        + Context.LeftBracket + targetSchema[i].FieldName + Context.RightBracket;
                    if (i + 1 < sourceSchema.Length)
                        result += ",";
                }

                result += "\nFROM (";
                result += "\n" + baseSql.Indentation();
                result += "\n) TTTT";
                
            }
            else
            {
                //select * into
                result += "SELECT *";
                result += "\nINTO " + tableQuery;
                result += "\nFROM (";
                result += "\n" + baseSql.Indentation();
                result += "\n) TTTT";
            }
            return result;
        }
    }
}
