using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Linq;


namespace XJDbQuery.Expressions
{
   public abstract class DbExpressionVisitor:ExpressionVisitor
    {
        protected override Expression Visit(Expression exp)
        {
            if (exp == null) return null;

            switch ((DbExpressionType)exp.NodeType)
            {
                case DbExpressionType.Column:
                    return this.VisitColumn((ColumnExpression)exp);
                case DbExpressionType.Select:
                    return this.VisitSelect((SelectExpression)exp);
                case DbExpressionType.Table:
                    return this.VisitTable((TableExpression)exp);
                case DbExpressionType.Join:
                    return this.VisitJoin((JoinExpression)exp);
                case DbExpressionType.Projection:
                    return this.VisitProjection((ProjectionExpression)exp);
                default:
                    return base.Visit(exp);
            }
        }
        protected virtual Expression VisitTable(TableExpression table)
        {
            return table;
        }

        protected virtual Expression VisitColumn(ColumnExpression column)
        {
            return column;
        }
        protected virtual Expression VisitSelect(SelectExpression select)
        {
            Expression from = this.VisitSource(select.From);
            Expression where = this.Visit(select.Where);
            ReadOnlyCollection<ColumnDeclaration> columns = this.VisitColumnDeclaration(select.Columns);
            ReadOnlyCollection<OrderExpression> orders = this.VisitOrderBy(select.OrderBy);

            if (from != select.From || where != select.Where || columns != select.Columns || orders != select.OrderBy)
            {
                return new SelectExpression(select.Type, select.Alias, columns, from, where, orders);
            }
            return select;
        }

        protected virtual Expression VisitJoin(JoinExpression join)
        {
            Expression left = this.Visit(join.Left);
            Expression right = this.Visit(join.Right);
            Expression condition = this.Visit(join.Condition);

            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.Type, join.Join, left, right, condition);
            }
            return join;
        }

        protected virtual ReadOnlyCollection<OrderExpression> VisitOrderBy(ReadOnlyCollection<OrderExpression> order)
        {
            if (order != null)
            {
                List<OrderExpression> alternate = null;

                for (int i = 0, n = order.Count; i < n; i++)
                {
                    OrderExpression expr = order[i];

                    Expression e = this.Visit(expr.Expression);

                    if (alternate == null && e != expr.Expression)
                    {
                        alternate = order.Take(i).ToList();
                    }
                    if (alternate != null)
                    {
                        alternate.Add(new OrderExpression(expr.OrderType, e));
                    }
                }
                if (alternate != null)
                {
                    return alternate.AsReadOnly();
                }

            }
            return order;

        }
        protected virtual Expression VisitProjection(ProjectionExpression projection)
        {
            SelectExpression source = (SelectExpression)this.Visit(projection.Source);
            Expression projector = this.Visit(projection.Projector);

            //查看是否又变化,如果有变化就重新构造表达式
            if (source != projection.Source || projector != projection.Projector)
                return new ProjectionExpression(source, projector);
            return projection;
        }

        protected virtual Expression VisitSource(Expression source)
        {
            return this.Visit(source);
        }

        protected ReadOnlyCollection<ColumnDeclaration> VisitColumnDeclaration(ReadOnlyCollection<ColumnDeclaration> columns)
        {
            List<ColumnDeclaration> alternate = null;
            for (int i = 0, n = columns.Count; i < n; i++)
            {
                ColumnDeclaration column = columns[i];
                Expression e = this.Visit(column.Expression);
                if (alternate == null && e != column.Expression)
                {
                    alternate = columns.Take(i).ToList();
                }
                if (alternate != null)
                {
                    alternate.Add(new ColumnDeclaration(column.Name, e));
                }
            }
            if (alternate != null)
            {
                return alternate.AsReadOnly();
            }
            return columns;
        }

    }
}
