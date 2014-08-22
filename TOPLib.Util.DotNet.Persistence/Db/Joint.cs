using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    internal abstract class JointBase : ISql
    {
        internal Bamboo Context { get; set; }

        public Joint LowerJoint { get; internal set; }

        public abstract string ToSQL();
    }

    internal abstract class Joint : JointBase
    {
        internal T CreateUpper<T>()
            where T : JointBase, new()
        {
            var upper = new T();

            upper.Context = this.Context;
            upper.LowerJoint = this;

            return upper;
        }
    }

    internal class QueryBase : Joint, IQueryBase
    {
        public override string ToSQL()
        {
            return this.LowerJoint.ToSQL();
        }

        public IQuery Where(Constraint constraint)
        {
            var result = this.CreateUpper<Query>();
            result.WhereClaus = constraint;
            return result;
        }

        public IQuery FilterBy(IDictionary<string, object> mapping)
        {
            if (mapping.Count == 0)
            {
                throw new Exception("Is this a valid filter?");
            }


            ISingleSelectable schemaq = this.All.Select;

            foreach (var kv in mapping)
            {
                schemaq =
                    kv.Key.Contains(".")
                    ? schemaq.Exp(kv.Key)
                    : schemaq[kv.Key];
            }

            if (schemaq is IExtractable)
            {
                var schema = ((IExtractable)schemaq).GetSchema();
                Constraint c = null;

                foreach (var kv in mapping)
                {
                    var rs = schema.Where(s => s.FieldName == kv.Key);
                    if (rs.Count() > 0)
                    {
                        var paramStr = "__FilterBy_" + kv.Key.Replace(".", "_");
                        Context.SetParameter(paramStr, kv.Value);
                        if (c == null)
                            c = new SingleConstraint(((kv.Key.Contains(".") ? kv.Key : Context.LeftBracket + kv.Key + Context.RightBracket) + "=@" + paramStr));
                        else
                            c = c & (new SingleConstraint(((kv.Key.Contains(".") ? kv.Key : Context.LeftBracket + kv.Key + Context.RightBracket) + "=@" + paramStr)));
                    }
                    else
                    {
                        if (c == null)
                            c = new SingleConstraint((kv.Key.Contains(".") ? kv.Key : Context.LeftBracket + kv.Key + Context.RightBracket) + "='" + kv.Value.ToString() + "'");
                        else
                            c = c & (new SingleConstraint((kv.Key.Contains(".") ? kv.Key : Context.LeftBracket + kv.Key + Context.RightBracket) + "='" + kv.Value.ToString() + "'"));
                    }
                }
                return Where(c);
            }
            else
            {
                throw new Exception("Is this a valid filter?");
            }
        }

        public IQuery All
        {
            get
            {
                var result = this.CreateUpper<Query>();
                result.WhereClaus = null;
                return result;
            }
        }
    }

    internal class Query : Joint, IQuery
    {
        public Constraint WhereClaus { get; internal set; }

        public ISelectable Select
        {
            get
            {
                var result = this.CreateUpper<SelectHelper>();
                return result;
            }
        }

        public IGroupable GroupBy
        {
            get
            {
                var result = this.CreateUpper<GroupByHelper>();
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

        public IWriteOnly ToUpdate(string name)
        {
            var result = this.CreateUpper<UpdateQuery>();
            result.target = name;
            return result;
        }

        public IExecutable ToDelete(string name)
        {
            var result = this.CreateUpper<DeleteQuery>();
            result.toDelete = name;
            return result;
        }

        public override string ToSQL()
        {
            if (WhereClaus == null)
                return LowerJoint.ToSQL();
            else
                return LowerJoint.ToSQL() + "\nWHERE " + WhereClaus.ToString();
        }
    }

    internal class Table : QueryBase, ITable
    {
        internal Table(string name)
        {
            this.Name = name;
        }

        public override string ToSQL()
        {
            string result = "FROM ";
            if (Name.StartsWith(Context.LeftBracket) & Name.EndsWith(Context.RightBracket))
                result += Name;
            else
                result += Context.LeftBracket + Name + Context.RightBracket;
            return result;
        }

        public string Name
        {
            get;
            private set;
        }

        public IWriteOnly ToInsert
        {
            get
            {
                var result = this.CreateUpper<InsertQuery>();
                return result;
            }
        }

        public IAliasedTable As(string alias)
        {
            var result = this.CreateUpper<AliasedTable>();
            result.Alias = alias;
            return result;
        }

        private IEnumerable<IRowSchema> schema;

        public IEnumerable<IRowSchema> Schema
        {
            get
            {
                if (schema == null)
                {
                    schema = this.All.Select["*"].GetSchema();
                }
                return schema;
            }
        }


        public IExecutable Persist(IDictionary<string, object> data)
        {
            var filter = new Dictionary<string, object>();

            foreach (var keyName in this.Schema.Where(s => s.IsKey).Select(s => s.FieldName))
            {
                var keyValue = data[keyName];
                if (keyValue == null)
                {
                    throw new Exception(Context.LeftBracket + keyName + Context.RightBracket + " is a key field, which does not allow null value.");
                }
                else
                {
                    filter.Add(keyName, keyValue);
                }
            }


            DataTable testResult;
            var testquery = this.FilterBy(filter).Select.Exp("1").As("One");
            testResult = testquery.Extract();

            if (testResult.Rows.Count > 0)
            {
                //update
                var u = this.As("t").FilterBy(filter).ToUpdate("t");
                foreach (var d in data)
                {
                    var sl = schema.Where(s => s.FieldName == d.Key);
                    if (sl.Count() > 0)
                    {
                        if (!sl.First().IsKey)
                        {
                            u[d.Key] = d.Value;
                        }
                    }
                }
                return u;
            }
            else
            {
                //insert
                var i = this.ToInsert;
                foreach (var d in data)
                {
                    i[d.Key] = d.Value;
                }
                return i;
            }
        }
    }

    internal class JoinableQueryBase : QueryBase, IJoinable
    {

        public IJoinning<IAliasedLeftJoinning> LeftJoin(IRight query)
        {
            var result = this.CreateUpper<Joinning<AliasedLeftJoinning, IAliasedLeftJoinning>>();
            result.Right = query;
            return result;
        }

        public IJoinning<IAliasedInnerJoinning> InnerJoin(IRight query)
        {
            var result = this.CreateUpper<Joinning<AliasedInnerJoinning, IAliasedInnerJoinning>>();
            result.Right = query;
            return result;
        }

        public IJoinning<IAliasedCrossJoinning> CrossJoin(IRight query)
        {
            var result = this.CreateUpper<Joinning<AliasedCrossJoinning, IAliasedCrossJoinning>>();
            result.Right = query;
            return result;
        }

        public IJoinning<IAliasedLeftJoinning> LeftJoin(string tableName)
        {
            return LeftJoin(Context[tableName]);
        }

        public IJoinning<IAliasedInnerJoinning> InnerJoin(string tableName)
        {
            return InnerJoin(Context[tableName]);
        }

        public IJoinning<IAliasedCrossJoinning> CrossJoin(string tableName)
        {
            return CrossJoin(Context[tableName]);
        }
    }

    internal class AliasedTable : JoinableQueryBase, IAliasedTable
    {
        public string Alias { get; set; }

        public override string ToSQL()
        {
            return LowerJoint.ToSQL() + " AS " + Context.LeftBracket + Alias + Context.RightBracket;
        }
    }

    internal class Joinning<T, I> : Joint, IJoinning<I>
        where I : IAliasedJoinning<I>
        where T : Joint, I, IAliasedJoinning<T>, new()
    {
        public IRight Right { get; internal set; }

        public override string ToSQL()
        {
            var result = LowerJoint.ToSQL();

            if (typeof(T) == typeof(AliasedLeftJoinning))
                result += "\nLEFT JOIN ";
            if (typeof(T) == typeof(AliasedInnerJoinning))
                result += "\nINNER JOIN ";
            if (typeof(T) == typeof(AliasedCrossJoinning))
                result += "\nCROSS JOIN ";

            if (this.Right is ITable)
                result += this.Context.LeftBracket + ((ITable)this.Right).Name + this.Context.RightBracket;
            else
                result += "(\n" + this.Right.ToSQL().Indentation() + "\n)";

            return result;
        }

        public I As(string alias)
        {
            var result = this.CreateUpper<T>();
            result.Alias = alias;
            return result;
        }
    }

    internal abstract class AbstractAliasedJoinning<T> : Joint, IAliasedJoinning<T>
    {
        public string Alias { get; set; }

        public override string ToSQL()
        {
            return LowerJoint.ToSQL() + " AS " + Context.LeftBracket + Alias + Context.RightBracket;
        }
    }

    internal abstract class AbstractJoinQueryBase<T> : AbstractAliasedJoinning<T>, IJoinQueryBase<T>
        where T : AbstractJoinQueryBase<T>, new()
    {
        public IJoinQuery On(Constraint constraint)
        {
            var result = this.CreateUpper<JoinQuery>();
            result.OnClause = constraint;
            return result;
        }

        public override string ToSQL()
        {
            return LowerJoint.ToSQL() + " AS " + Context.LeftBracket + Alias + Context.RightBracket;
        }
    }

    internal class AliasedLeftJoinning : AbstractJoinQueryBase<AliasedLeftJoinning>, IAliasedLeftJoinning { }

    internal class AliasedInnerJoinning : AbstractJoinQueryBase<AliasedInnerJoinning>, IAliasedInnerJoinning { }

    internal class JoinQuery : QueryBase, IJoinQuery
    {
        public Constraint OnClause { get; internal set; }

        public override string ToSQL()
        {
            var result = LowerJoint.ToSQL();
            if (OnClause != null)
                result += " ON " + OnClause.ToString();
            return result;
        }
    }

    internal class AliasedCrossJoinning : JoinQuery, IAliasedCrossJoinning, IAliasedJoinning<AliasedCrossJoinning>
    {
        public string Alias { get; set; }

        public override string ToSQL()
        {
            return LowerJoint.ToSQL() + " AS " + Context.LeftBracket + Alias + Context.RightBracket;
        }
    }
}
