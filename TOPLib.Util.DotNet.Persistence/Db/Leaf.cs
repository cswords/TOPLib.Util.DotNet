using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    internal abstract class Leaf : JointBase { }

    internal abstract class ExecutableLeaf : Leaf, IExecutable
    {
        public void Execute()
        {
            this.Context.Execute(this.ToSQL());
        }
    }

    internal abstract class WriteOnlyLeaf : ExecutableLeaf, IWriteOnly
    {
        internal IDictionary<string, object> values = new Dictionary<string, object>();

        public object this[string field]
        {
            set
            {
                if (values.ContainsKey(field))
                    values[field] = value;
                else
                    values.Add(field, value);
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
                result += "'" + v.Value + "'";
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
            var result = "UPDATE " + Context.LeftBracket + target + Context.RightBracket;
            result += "\nSET";
            int i = values.Count;
            foreach (var v in values)
            {
                result += " " + Context.LeftBracket + v.Key + Context.RightBracket + " = '" + v.Value.ToString() + "'";
                i--;
                if (i != 0) result += ",";
            }
            result += "\n" + LowerJoint.ToSQL();
            return result;
        }
    }

    internal class DeleteQuery : ExecutableLeaf
    {
        public override string ToSQL()
        {
            var result = LowerJoint.ToSQL();
            result = "DELETE\n" + result;
            return result;
        }
    }
}
