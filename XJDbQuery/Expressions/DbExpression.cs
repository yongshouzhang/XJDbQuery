using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections.ObjectModel;

namespace XJDbQuery.Expressions
{

    public enum DbExpressionType
    {
        Table = 1000, // make sure these don't overlap with ExpressionType
        ClientJoin,
        Column,
        Select,
        Projection,
        Entity,
        Join,
        Aggregate,
        Scalar,
        Exists,
        In,
        Grouping,
        AggregateSubquery,
        IsNull,
        Between,
        RowCount,
        NamedValue,
        OuterJoined,
        Insert,
        Update,
        Delete,
        Batch,
        Function,
        Block,
        If,
        Declaration,
        Variable
    }

    public enum JoinType
    {
        CrossJoin,
        InnerJoin,
        CrossApply
    }

    public enum OrderType
    {
        Ascending,
        Descending
    }
    public class DbExpression : Expression
    {
        private readonly DbExpressionType expressionType;
        private readonly Type type;

        protected DbExpression(DbExpressionType expressionType, Type type)
            :base((ExpressionType)expressionType,type)
        {
            this.expressionType = expressionType;
            this.type = type;
        }
        public new ExpressionType NodeType
        {
            get
            {
                return (ExpressionType)(int)this.expressionType;
            }
        }

        public new Type Type
        {
            get
            {
                return this.type;
            }
        }
    }

    public class TableExpression : DbExpression
    {
        string alias;
        string name;
        public TableExpression(Type type, string alias, string name)
            : base(DbExpressionType.Table, type)
        {
            this.alias = alias;
            this.name = name;
        }
        public string Alias { get { return this.alias; } }
        public string Name { get { return this.name; } }
    }

    public class ColumnExpression : DbExpression
    {
        string alias;
        string name;
        int ordinal;
        public ColumnExpression(Type type, string alias, string name, int ordinal)
            : base(DbExpressionType.Column, type)
        {
            this.alias = alias;
            this.name = name;
            this.ordinal = ordinal;
        }

        public string Alias
        {
            get { return this.alias; }
        }
        public string Name
        {
            get { return this.name; }
        }

        public int Ordinal
        {
            get { return this.ordinal; }
        }

    }

    public class ColumnDeclaration
    {
        string name;
        Expression expression;

        public ColumnDeclaration(string name, Expression expression)
        {
            this.name = name;
            this.expression = expression;
        }
        public string Name { get { return this.name; } }

        public Expression Expression { get { return this.expression; } }

    }

    public class SelectExpression : DbExpression
    {
        string alias;
        ReadOnlyCollection<ColumnDeclaration> columns;
        Expression from;
        Expression where;
        ReadOnlyCollection<OrderExpression> orderBy;
        public SelectExpression(Type type, string alias, IEnumerable<ColumnDeclaration> columns, Expression from, Expression where,
            IEnumerable<OrderExpression> orderBy)
            : base(DbExpressionType.Select, type)
        {
            this.alias = alias;
            this.columns = columns as ReadOnlyCollection<ColumnDeclaration>;
            this.columns = this.columns == null ? columns.ToReadOnly<ColumnDeclaration>() : this.columns;
            this.from = from;
            this.where = where;
            if (this.orderBy == null && orderBy != null)
            {
                this.orderBy = orderBy.ToList().AsReadOnly();
            }
        }

        public string Alias
        {
            get { return this.alias; }
        }
        public ReadOnlyCollection<ColumnDeclaration> Columns
        {
            get { return this.columns; }
        }

        public Expression From
        {
            get { return this.from; }
        }

        public Expression Where
        {
            get { return this.where; }
        }

        public ReadOnlyCollection<OrderExpression> OrderBy
        {
            get { return this.orderBy; }
        }
    }

    public class ProjectionExpression : DbExpression
    {
        SelectExpression source;
        Expression projector;
        public ProjectionExpression(SelectExpression source, Expression projector)
            : base(DbExpressionType.Projection, projector.Type)
        {
            this.source = source;
            this.projector = projector;
        }
        public SelectExpression Source
        {
            get { return this.source; }
        }
        public Expression Projector
        {
            get { return this.projector; }
        }

    }

    public class JoinExpression : DbExpression
    {
        JoinType joinType;
        Expression left;
        Expression right;
        Expression condition;

        public JoinExpression(Type type, JoinType joinType, Expression left, Expression right, Expression condition)
            : base(DbExpressionType.Join, type)
        {
            this.joinType = joinType;
            this.left = left;
            this.right = right;
            this.condition = condition;
        }

        public JoinType Join { get { return this.joinType; } }
        public Expression Left { get { return this.left; } }
        public Expression Right { get { return this.right; } }
        public new Expression Condition { get { return this.condition; } }

    }

    public class OrderExpression
    {
        OrderType orderType;
        Expression expression;
        public OrderExpression(OrderType orderType, Expression expression)
        {
            this.orderType = orderType;
            this.expression = expression;
        }
        public OrderType OrderType
        {
            get { return this.orderType; }
        }
        public Expression Expression
        {
            get { return this.expression; }
        }
    }

    public class TableAlias
    {
        public TableAlias()
        {
        }

        public override string ToString()
        {
            return "A:" + this.GetHashCode();
        }
    }

  
}
