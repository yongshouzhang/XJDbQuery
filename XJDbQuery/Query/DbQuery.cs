using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
namespace XJDbQuery.Query
{
    public class DbQuery<T> : IQueryable<T>, IQueryable, IEnumerable, IEnumerable<T>, IOrderedQueryable, IOrderedQueryable<T>
    {
        IQueryProvider provider;
        Expression expression;

        public DbQuery() { }
        public DbQuery(IQueryProvider provider, Expression expression)
        {
            this.provider = provider;
            this.expression = expression;
        }

        public DbQuery(IQueryProvider provider)
        {
            this.provider = provider;
            this.expression = Expression.Constant(this);
        }

        public Expression Expression { get { return this.expression; } }

        public IQueryProvider Provider { get { return this.provider; } }

        public Type ElementType { get { return typeof(T); } }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>)this.provider.Execute(this.expression)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)this.provider.Execute(this.expression)).GetEnumerator();
        }
        
    }
}
