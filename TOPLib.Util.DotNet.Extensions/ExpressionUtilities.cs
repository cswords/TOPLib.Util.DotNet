using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace TOPLib.Util.DotNet.Extensions
{
    public static class ExpressionUtilities
    {
        // reference: https://blogs.msdn.microsoft.com/meek/2008/05/02/linq-to-entities-combining-predicates/

        public static Expression<T> Compose<T>(this Expression<T> first, Expression<T> second, Func<Expression, Expression, Expression> merge)
        {
            // build parameter map (from parameters of second to parameters of first)
            var map = first.Parameters.Select((f, i) => new { f, s = second.Parameters[i] }).ToDictionary(p => p.s, p => p.f);
            // replace parameters in the second lambda expression with parameters from the first
            var secondBody = ParameterRebinder.ReplaceParameters(map, second.Body);
            // apply composition of lambda expression bodies to parameters from the first expression 
            return Expression.Lambda<T>(merge(first.Body, secondBody), first.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.And);
        }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> first, Expression<Func<T, bool>> second)
        {
            return first.Compose(second, Expression.Or);
        }

        public static Expression<Func<T, bool>> Not<T>(this Expression<Func<T, bool>> first)
        {
            return Expression.Lambda<Func<T, bool>>(Expression.Not(first.Body), first.Parameters[0]);
        }

        public static Func<Expression<Func<T, bool>>, Expression<Func<T, bool>>> GetNot<T>()
        {
            return new Func<Expression<Func<T, bool>>, Expression<Func<T, bool>>>((ex) => Not(ex));
        }

        public static Expression<Func<T, bool>> Is<T>(this Expression<Func<T, bool>> first)
        {
            return first;
        }

        public static Func<Expression<Func<T, bool>>, Expression<Func<T, bool>>> GetIs<T>()
        {
            return new Func<Expression<Func<T, bool>>, Expression<Func<T, bool>>>((ex) => Is(ex));
        }
    }

    public class ParameterRebinder : ExpressionVisitor
    {
        private readonly Dictionary<ParameterExpression, ParameterExpression> map;

        public ParameterRebinder(Dictionary<ParameterExpression, ParameterExpression> map)
        {
            this.map = map ?? new Dictionary<ParameterExpression, ParameterExpression>();
        }

        public static Expression ReplaceParameters(Dictionary<ParameterExpression, ParameterExpression> map, Expression exp)
        {
            return new ParameterRebinder(map).Visit(exp);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            ParameterExpression replacement;

            if (map.TryGetValue(p, out replacement))
            {
                p = replacement;
            }
            return base.VisitParameter(p);
        }
    }
}
