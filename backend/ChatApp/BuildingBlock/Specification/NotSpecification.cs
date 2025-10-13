using System.Linq.Expressions;

namespace BuildingBlock.Specification
{
    public sealed class NotSpecification<T>(Specification<T> specification) : Specification<T>
    {
        public override Expression<Func<T, bool>> ToExpression()
        {
            var expr = specification.ToExpression();
            var param = expr.Parameters[0];
            var body = Expression.Not(expr.Body);
            return Expression.Lambda<Func<T, bool>>(body, param);
        }
    }
}
