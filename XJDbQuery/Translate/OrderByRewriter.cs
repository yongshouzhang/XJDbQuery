using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.ObjectModel;
namespace XJDbQuery.Translate
{
    using Expressions;
    public class OrderByRewriter : DbExpressionVisitor
    {
        IEnumerable<OrderExpression> gatheredOrderings;
        bool isOuterMostSelect;
        private OrderByRewriter()
        {
            this.isOuterMostSelect = true;
        }
        public static Expression Rewrite(Expression expression)
        {
            return new OrderByRewriter().Visit(expression);
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            bool saveIsOuterMostSelect = this.isOuterMostSelect;
            try
            {
                this.isOuterMostSelect = false;
                select = (SelectExpression)base.VisitSelect(select);

                bool hasOrderBy = select.OrderBy != null && select.OrderBy.Count > 0;
                if (hasOrderBy)
                {
                    this.PrependOrderings(select.OrderBy);
                }
                bool canHaveOrderBy = saveIsOuterMostSelect;
                bool canPassOnOrderings = !saveIsOuterMostSelect;
                IEnumerable<OrderExpression> orderings = (canHaveOrderBy) ? this.gatheredOrderings : null;
                ReadOnlyCollection<ColumnDeclaration> columns = select.Columns;

                if (this.gatheredOrderings != null)
                {
                    if (canPassOnOrderings)
                    {
                        HashSet<string> produceAliases = DeclaredAliasGatherer.Gather(select.From);
                        BindResult project = this.RebindOrderings(this.gatheredOrderings, select.Alias, produceAliases, select.Columns);
                        this.gatheredOrderings = project.Orderings;
                        columns = project.Columns;
                    }
                    else
                    {
                        this.gatheredOrderings = null;
                    }
                }
                if (orderings != select.OrderBy || columns != select.Columns)
                {
                    select = new SelectExpression(select.Type, select.Alias, columns, select.From, select.Where, orderings);
                }
                return select;
            }
            finally
            {
                this.isOuterMostSelect = saveIsOuterMostSelect;
            }
        }

        protected override Expression VisitProjection(ProjectionExpression projection)
        {
            return new ProjectionExpression((SelectExpression)base.Visit(projection.Source), projection.Projector);
        }
        protected override Expression VisitJoin(JoinExpression join)
        {
            Expression left = this.VisitSource(join.Left);
            IEnumerable<OrderExpression> leftOrders = this.gatheredOrderings;
            this.gatheredOrderings = null;
            Expression right = this.VisitSource(join.Right);
            this.PrependOrderings(leftOrders);
            Expression condition = this.Visit(join.Condition);
            if (left != join.Left || right != join.Right || condition != join.Condition)
            {
                return new JoinExpression(join.Type, join.Join, left, right, condition);
            }
            return join;
        }
        protected void PrependOrderings(IEnumerable<OrderExpression> newOrderings)
        {
            if (newOrderings != null)
            {
                if (this.gatheredOrderings == null)
                {
                    this.gatheredOrderings = newOrderings;
                }
                else
                {
                    List<OrderExpression> list = this.gatheredOrderings.ToList();
                    list.InsertRange(0, newOrderings);
                    HashSet<string> unique = new HashSet<string>();
                    //剔除相同的列名
                    for (int i = 0; i < list.Count;)
                    {
                        ColumnExpression column = list[i].Expression as ColumnExpression;
                        if (column != null)
                        {
                            string hash = column.Alias + ":" + column.Name;
                            if (unique.Contains(hash))
                            {
                                list.RemoveAt(i);
                                // 直接跳过，不增加i值 
                                continue;
                            }
                            else
                            {
                                unique.Add(hash);
                            }
                        }
                        i++;
                    }
                    this.gatheredOrderings = list;
                }
            }
        }

        protected virtual BindResult RebindOrderings(IEnumerable<OrderExpression> orderings, string alias, HashSet<string> existingAlias, IEnumerable<ColumnDeclaration> existingColumns)
        {
            List<ColumnDeclaration> newColumns = null;
            List<OrderExpression> newOrderings = new List<OrderExpression>();

            foreach (OrderExpression ordering in orderings)
            {
                Expression expr = ordering.Expression;
                ColumnExpression column = expr as ColumnExpression;
                if (column == null ||
                    (existingAlias != null &&
                      existingAlias.Contains(column.Alias)))
                {
                    int iOrdinal = 0;
                    foreach (ColumnDeclaration decl in existingColumns)
                    {
                        ColumnExpression declColumn = decl.Expression as ColumnExpression;
                        if (decl.Expression == ordering.Expression ||
                            (column != null && declColumn != null &&
                            column.Alias == declColumn.Alias &&
                            column.Name == declColumn.Name))
                        {
                            expr = new ColumnExpression(column.Type, alias, decl.Name, iOrdinal);
                            break;
                        }
                        iOrdinal++;
                    }
                    if (expr == ordering.Expression)
                    {
                        if (newColumns == null)
                        {
                            newColumns = new List<ColumnDeclaration>(existingColumns);
                            existingColumns = newColumns;
                        }
                        string colName = column != null ? column.Name : "c" + iOrdinal;
                        newColumns.Add(new ColumnDeclaration(colName, ordering.Expression));
                        expr = new ColumnExpression(expr.Type, alias, colName, iOrdinal);
                    }
                    newOrderings.Add(new OrderExpression(ordering.OrderType, expr));
                }
            }
            return new BindResult(existingColumns, newOrderings);
        }

        protected class BindResult
        {
            ReadOnlyCollection<ColumnDeclaration> columns;
            ReadOnlyCollection<OrderExpression> orderings;

            public BindResult(IEnumerable<ColumnDeclaration> columns, IEnumerable<OrderExpression> orders)
            {
                this.columns = columns as ReadOnlyCollection<ColumnDeclaration>;
                if (this.columns == null)
                {
                    this.columns = new List<ColumnDeclaration>(columns).AsReadOnly();
                }
                this.orderings = orderings = orderings as ReadOnlyCollection<OrderExpression>;
                if (this.orderings == null)
                {
                    this.orderings = orders.ToList().AsReadOnly();// new List<OrderExpression>(orderings).AsReadOnly();
                }

            }
            public ReadOnlyCollection<ColumnDeclaration> Columns
            {
                get { return this.columns; }
            }

            public ReadOnlyCollection<OrderExpression> Orderings
            {
                get { return this.orderings; }
            }
        }

    }
}
