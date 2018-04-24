using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
namespace XJDbQuery.Translate
{
    using Expressions;
    using Common;
    public sealed class ProjectedColumns
    {
        Expression projector;
        ReadOnlyCollection<ColumnDeclaration> columns;
        public ProjectedColumns(Expression projector, ReadOnlyCollection<ColumnDeclaration> columns)
        {
            this.projector = projector;
            this.columns = columns;
        }

        public Expression Projector
        {
            get { return this.projector; }
        }

        public ReadOnlyCollection<ColumnDeclaration> Columns
        {
            get { return this.columns; }
        }

    }

    public enum ProjectionAffinity
    {
        /// <summary>
        /// Prefer expression computation on the client
        /// </summary>
        Client,

        /// <summary>
        /// Prefer expression computation on the server
        /// </summary>
        Server
    }

    public class ColumnProjector : DbExpressionVisitor
    {
        Nominator nominator;
        Dictionary<ColumnExpression, ColumnExpression> map;
        List<ColumnDeclaration> columns;
        HashSet<string> columnNames;
        HashSet<Expression> candidates;
        HashSet<string> existAlias;
        string newAlias;
        int iColumns;

        public ColumnProjector(Func<Expression, bool> fnCanBeColumn)
        {
            this.nominator = new Nominator(fnCanBeColumn);
        }
        public ProjectedColumns ProjectColumns(Expression expression, string newAlias, params string[] existAlias)
        {
            this.map = new Dictionary<ColumnExpression, ColumnExpression>();
            this.columns = new List<ColumnDeclaration>();
            this.columnNames = new HashSet<string>();
            this.newAlias = newAlias;
            this.existAlias = new HashSet<string>(existAlias);
            this.candidates = this.nominator.Nominate(expression);
            return new ProjectedColumns(this.Visit(expression), this.columns.ToReadOnly());
        }

        protected override Expression Visit(Expression exp)
        {
            if (this.candidates.Contains(exp))
            {
                if (exp.NodeType == (ExpressionType)DbExpressionType.Column)
                {
                    ColumnExpression column = (ColumnExpression)exp;
                    ColumnExpression mapped;
                    if (this.map.TryGetValue(column, out mapped))
                    {
                        return mapped;
                    }
                    //如果别名重复
                    if (this.existAlias.Contains(column.Alias.ToString()))
                    {
                        int ordinal = this.columns.Count;
                        string columnName = this.GetUniqueColumnName(column.Name);

                        this.columns.Add(new ColumnDeclaration(columnName, column));
                        //改变 alias 
                        mapped = new ColumnExpression(column.Type, this.newAlias, columnName, ordinal);
                        //映射到一个新的列
                        this.map[column] = mapped;
                        this.columnNames.Add(columnName);
                        // 返回新映射的列
                        return mapped;
                    }
                    return column;
                }
            }
            return base.Visit(exp);
        }

        /// <summary>
        /// 获取唯一的列名
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private string GetUniqueColumnName(string name)
        {
            string baseName = name;
            int suffix = 1;
            while (this.IsColumnNameInUse(name))
            {
                name = baseName + (suffix++);
            }
            return name;
        }
        /// <summary>
        /// 判断列名是否已被占用
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool IsColumnNameInUse(string name)
        {
            return this.columnNames.Contains(name);
        }
        /// <summary>
        /// 获取下一个列名
        /// </summary>
        /// <returns></returns>
        private string GetNextColumnName()
        {
            return this.GetUniqueColumnName("c" + (iColumns++));
        }

    }

    class Nominator : DbExpressionVisitor
    {
        Func<Expression, bool> fnCanBeColumn;
        bool isBlocked;
        HashSet<Expression> candidates;

        internal Nominator(Func<Expression, bool> fnCanBeColumn)
        {
            this.fnCanBeColumn = fnCanBeColumn;
        }

        internal HashSet<Expression> Nominate(Expression expression)
        {
            this.candidates = new HashSet<Expression>();
            this.isBlocked = false;
            this.Visit(expression);
            return this.candidates;
        }
        protected override Expression Visit(Expression exp)
        {
            if (exp == null) return null;
            bool saveIsBlocked = this.isBlocked;
            this.isBlocked = false;
            base.Visit(exp);

            if (!this.isBlocked)
            {
                if (this.fnCanBeColumn(exp))
                {
                    this.candidates.Add(exp);
                }
                else
                {
                    this.isBlocked = true;
                }
            }
            this.isBlocked |= saveIsBlocked;
            return exp;
        }
    }
}