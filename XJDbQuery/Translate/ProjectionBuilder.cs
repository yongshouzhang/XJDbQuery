using System;
using System.Linq.Expressions;
using System.Reflection;

namespace XJDbQuery.Translate
{
    using Common;
    using Expressions;
    public class ProjectionBuilder : DbExpressionVisitor
    {
        ParameterExpression row;
        private static MethodInfo miGetValue;
        private static MethodInfo miExecuteSubQuery;
        public ProjectionBuilder()
        {
            if (miGetValue == null)
            {
                miGetValue = typeof(ProjectionRow).GetMethod("GetValue");
                miExecuteSubQuery = typeof(ProjectionRow).GetMethod("ExecuteSubQuery");
            }
        }

        public LambdaExpression Build(Expression expression)
        {
            this.row = Expression.Parameter(typeof(ProjectionRow), "row");
            Expression body = this.Visit(expression);
            return Expression.Lambda(body, this.row);

        }
        protected override Expression VisitColumn(ColumnExpression column)
        {
            return Expression.Convert(Expression.Call(this.row, miGetValue, Expression.Constant(column.Name)), column.Type);
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            LambdaExpression subQuery = Expression.Lambda(base.VisitProjection(projection), this.row);
            Type elementType = TypeHelper.GetElementType(subQuery.Body.Type);
            MethodInfo mi = miExecuteSubQuery.MakeGenericMethod(elementType);
            return Expression.Convert(Expression.Call(this.row, mi, Expression.Constant(subQuery)), projection.Type);
        }

    }
}
