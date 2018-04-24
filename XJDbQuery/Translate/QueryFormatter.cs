using System;
using System.Collections.Generic;

using System.Linq;
using System.Linq.Expressions;
using System.Text;
namespace XJDbQuery.Translate
{
    using Expressions;
    using Common;
    public class QueryFormatter : DbExpressionVisitor
    {
        StringBuilder sb;

        int indent = 2;
        int depth;

        public QueryFormatter() { }

        public string FormatExpression(Expression exp)
        {
            this.sb = new StringBuilder();
            this.Visit(exp);
            return sb.ToString();
        }

        protected enum Indentation
        {
            Same,
            Inner,
            Outer
        }

        protected int IndentationWidth
        {
            get { return this.indent; }
            set { this.indent = value; }
        }

        private void AppendNewLine(Indentation style)
        {
            sb.AppendLine();
            if (style == Indentation.Inner)
            {
                this.depth++;
            }
            else if (style == Indentation.Outer)
            {
                this.depth--;
            }
            else
            {
                for (int i = 0, n = this.depth * this.indent; i < n; i++) sb.Append(" ");
            }
        }

        protected override Expression VisitColumn(ColumnExpression column)
        {
            if (!string.IsNullOrEmpty(column.Alias))
            {
                sb.Append(column.Alias);
                sb.Append(".");
            }
            sb.Append(column.Name);
            return column;
        }
        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            throw new NotSupportedException(" 不支持");
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            switch (u.NodeType)
            {
                case ExpressionType.Not:
                    sb.Append(" NOT ");
                    this.Visit(u.Operand);
                    break;
                default:
                    throw new NotSupportedException(" 不支持 ");
            }
            return u;
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            sb.Append(" ( ");
            this.Visit(b.Left);
            switch (b.NodeType)
            {
                case ExpressionType.Add:
                    sb.Append(" Add ");
                    break;
                case ExpressionType.Or:
                    sb.Append(" OR ");
                    break;
                case ExpressionType.Equal:
                    sb.Append(" = ");
                    break;
                case ExpressionType.NotEqual:
                    sb.Append(" <> ");
                    break;
                case ExpressionType.LessThan:
                    sb.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    sb.Append(" <= ");
                    break;
                case ExpressionType.GreaterThan:
                    sb.Append(">");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    sb.Append(" >= ");
                    break;
                default:
                    throw new NotSupportedException(" 不支持 ");

            }
            this.Visit(b.Right);
            sb.Append(" ) ");
            return b;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            if (c.Value == null) sb.Append(" NULL ");
            else
            {
                switch (Type.GetTypeCode(c.Value.GetType()))
                {
                    case TypeCode.Boolean:
                        this.sb.Append((bool)c.Value ? 1 : 0);
                        break;
                    case TypeCode.String:
                        this.sb.Append("'");
                        this.sb.Append(c.Value);
                        this.sb.Append("'");
                        break;
                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", c.Value));
                    case TypeCode.Single:
                    case TypeCode.Double:
                        string str = c.Value.ToString();
                        if (!str.Contains('.'))
                        {
                            str += ".0";
                        }
                        this.sb.Append(str);
                        break;
                    default:
                        this.sb.Append(c.Value);
                        break;
                }
            }
            return c;

        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            sb.Append("Select ");
            for (int i = 0, n = select.Columns.Count; i < n; i++)
            {
                ColumnDeclaration column = select.Columns[i];
                if (i > 0)
                {
                    sb.Append(" ,");
                }
                ColumnExpression c = this.Visit(column.Expression) as ColumnExpression;
                if (c == null || c.Name != select.Columns[i].Name)
                {
                    sb.Append(" As ");
                    sb.Append(column.Name);
                }
            }
            if (select.From != null)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append(" FROM ");
                this.VisitSource(select.From);
            }
            if (select.Where != null)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append(" WHERE ");
                this.Visit(select.Where);
            }
            if (select.OrderBy != null && select.OrderBy.Count > 0)
            {
                this.AppendNewLine(Indentation.Same);
                sb.Append(" ORDER BY ");
                //当有多个排序字段时
                for (int i = 0, n = select.OrderBy.Count; i < n; i++)
                {
                    OrderExpression expr = select.OrderBy[i];
                    if (i > 0) sb.Append(",");
                    this.Visit(expr.Expression);
                    if (expr.OrderType != OrderType.Ascending)
                        sb.Append(" DESC ");
                }

            }
            return select;
        }

        protected override Expression VisitJoin(JoinExpression join)
        {
            this.VisitSource(join.Left);
            this.AppendNewLine(Indentation.Same);
            switch (join.Join)
            {
                case JoinType.CrossJoin:
                    sb.Append("CROSS JOIN ");
                    break;
                case JoinType.InnerJoin:
                    sb.Append(" INNER JOIN ");
                    break;
                case JoinType.CrossApply:
                    sb.Append(" CROSS APPLY ");
                    break;
            }
            this.VisitSource(join.Right);

            if (join.Condition != null)
            {
                this.AppendNewLine(Indentation.Inner);
                sb.Append(" ON ");
                this.Visit(join.Condition);
                this.AppendNewLine(Indentation.Outer);
            }
            return join;
        }
        protected override Expression VisitSource(Expression source)
        {
            switch ((DbExpressionType)source.NodeType)
            {
                case DbExpressionType.Table:
                    TableExpression table = (TableExpression)source;
                    sb.Append(table.Name);
                    sb.Append(" AS ");
                    sb.Append(table.Alias);
                    break;
                case DbExpressionType.Select:
                    SelectExpression select = (SelectExpression)source;
                    sb.Append("( ");
                    this.AppendNewLine(Indentation.Inner);
                    this.Visit(select);
                    this.AppendNewLine(Indentation.Outer);
                    sb.Append(")");
                    sb.Append(" AS ");
                    sb.Append(select.Alias);
                    break;
                case DbExpressionType.Join:
                    this.VisitJoin((JoinExpression)source);
                    break;
                default:
                    throw new NotSupportedException(" select 不是有效的数据源 ");
            }
            return source;
        }
    }
}
