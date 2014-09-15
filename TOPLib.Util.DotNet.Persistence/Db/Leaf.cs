using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using TOPLib.Util.DotNet.Persistence.Util;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    internal abstract class Leaf : JointBase { }

    internal abstract class ExecutableLeaf : Leaf, IExecutable
    {
        public void Execute(int timeout = 30)
        {
            this.Context.Execute(this.ToSQL(), timeout);
        }
    }

    internal abstract class WriteOnlyLeaf : ExecutableLeaf, IWriteOnly
    {
        protected IEnumerable<IRowSchema> schema = null;

        internal IDictionary<string, string> values = new Dictionary<string, string>();

        public object this[string field]
        {
            set
            {
                if (schema == null)
                {
                    IQuery q = null;
                    if (this.LowerJoint is IQuery)
                    {
                        q = ((IQuery)this.LowerJoint);
                    }
                    else if (this.LowerJoint is ITable)
                    {
                        q = ((ITable)this.LowerJoint).All;
                    }
                    if (q != null)
                        schema = q.Select["*"].GetSchema();
                }
                
                if (schema == null) return;

                var fschema = schema.Single(s => s.FieldName == field);

                if (value != null)
                {
                    if (value.GetType() != fschema.DataType)
                    {
                        if (!value.GetType().IsConvertableTo(fschema.DataType))
                            return;
                    }
                }

                var paramName = "__" + field.Replace(" ", "_");

                this.values.Add(field, paramName);

                Context.SetParameter(paramName, value);
            }
        }
    }

    internal class InsertQuery : WriteOnlyLeaf
    {
        public override string ToSQL()
        {
            var result = "INSERT INTO " + Context.LeftBracket + ((ITable)LowerJoint).Name + Context.RightBracket;
            result += "\n(";
            int i = values.Count;
            foreach (var v in values)
            {
                result += Context.LeftBracket + v.Key + Context.RightBracket;
                i--;
                if (i > 0) result += ", ";
            }
            result += ")";
            result += "\nVALUES";
            result += "\n(";
            i = values.Count;
            foreach (var v in values)
            {
                result += "@" + v.Value + "";
                i--;
                if (i > 0) result += ", ";
            }
            result += ")";
            return result;
        }
    }

    internal class UpdateQuery : WriteOnlyLeaf
    {
        internal string target;

        public override string ToSQL()
        {
            var result = "UPDATE " +
                ((target.StartsWith(Context.LeftBracket) & target.EndsWith(Context.RightBracket))
                ? target
                : (Context.LeftBracket + target + Context.RightBracket));
            result += "\nSET";
            int i = values.Count;
            foreach (var v in values)
            {
                result += " " + Context.LeftBracket + v.Key + Context.RightBracket + " = @" + v.Value + "";
                i--;
                if (i != 0) result += ",";
            }
            result += "\n" + LowerJoint.ToSQL();
            return result;
        }
    }

    internal class DeleteQuery : ExecutableLeaf
    {

        internal string toDelete;

        public override string ToSQL()
        {
            var result = LowerJoint.ToSQL();
            result = "DELETE " + Context.LeftBracket + toDelete + Context.RightBracket + "\n" + result;
            return result;
        }
    }
}
