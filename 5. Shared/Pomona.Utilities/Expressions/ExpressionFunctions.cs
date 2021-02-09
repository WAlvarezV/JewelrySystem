using System;
using System.Linq.Expressions;

namespace Pomona.Utilities.Expressions
{
    internal class ExpressionFunctions : ExpressionVisitor
    {
        public static Expression<Func<T, bool>> AndAlso<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        => Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left.Body, new ExpressionParameterReplacer(
            right.Parameters, left.Parameters).Visit(right.Body)), left.Parameters);

        public static Expression<Func<T, bool>> OrElse<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        => Expression.Lambda<Func<T, bool>>(Expression.OrElse(left.Body, new ExpressionParameterReplacer(
            right.Parameters, left.Parameters).Visit(right.Body)), left.Parameters);
    }
}
