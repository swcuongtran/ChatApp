using System.Linq.Expressions;

namespace BuildingBlock.Specification
{
    public sealed class OrSpecification<T>(Specification<T> left, Specification<T> right) : Specification<T>
    {
        public override Expression<Func<T, bool>> ToExpression()
        {
            var leftExpr = left.ToExpression();
            var rightExpr = right.ToExpression();

            var param = Expression.Parameter(typeof(T));
            var body = Expression.OrElse(
                Expression.Invoke(leftExpr, param),
                Expression.Invoke(rightExpr, param));

            return Expression.Lambda<Func<T, bool>>(body, param);
        }
    }
}
