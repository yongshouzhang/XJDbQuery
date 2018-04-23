using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Linq.Expressions;

namespace XJDbQuery.Provider
{
    using Common;
    using Query;
    public abstract class QueryProvider:IQueryProvider
    {
        protected QueryProvider() { }

        IQueryable<T> IQueryProvider.CreateQuery<T>(Expression expression)
        {
            return new DbQuery<T>(this, expression);
        }
        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            Type elementType = TypeHelper.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(DbQuery<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch(Exception e)
            {
                throw e.InnerException;
            }
        }
        T IQueryProvider.Execute<T>(Expression expression)
        {
            return (T)this.Execute(expression);
        }
        object IQueryProvider.Execute(Expression expression)
        {
            return this.Execute(expression);
        }
        public abstract object Execute(Expression expression);
    }
}
