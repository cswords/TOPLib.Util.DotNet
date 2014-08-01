using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    public abstract class Constraint
    {
        public static Constraint operator !(Constraint c)
        {
            var result = new NotConstraint(c);
            return result;
        }

        public static Constraint operator &(Constraint l, Constraint r)
        {
            if (l != null)
            {
                var result = new MultiConstraint
                {
                    Left = l,
                    Operator = BinaryOperator.AND,
                    Right = r
                };
                return result;
            }
            else
            {
                return r;
            }
        }

        public static Constraint operator |(Constraint l, Constraint r)
        {
            if (l != null)
            {
                var result = new MultiConstraint
                {
                    Left = l,
                    Operator = BinaryOperator.OR,
                    Right = r
                };
                return result;
            }
            else
            {
                return null;
            }
        }
    }

    internal class SingleConstraint : Constraint
    {
        public string Expression { get; private set; }

        public SingleConstraint(string exp)
        {
            this.Expression = exp;
        }

        public override string ToString()
        {
            return Expression;
        }
    }

    internal class MultiConstraint : Constraint
    {
        public Constraint Left { get; internal set; }
        public BinaryOperator Operator { get; internal set; }
        public Constraint Right { get; internal set; }
        
        public override string ToString()
        {
            return "(" + Left + ") " + Operator + " (" + Right + ")";
        }
    }

    internal class NotConstraint : Constraint
    {
        public Constraint OriginalConstraint { get; private set; }
        internal NotConstraint(Constraint original)
        {
            this.OriginalConstraint = original;
        }

        public override string ToString()
        {
            return "NOT (" + OriginalConstraint.ToString() + ")";
        }
    }

    internal enum BinaryOperator
    {
        AND, OR
    }
}
