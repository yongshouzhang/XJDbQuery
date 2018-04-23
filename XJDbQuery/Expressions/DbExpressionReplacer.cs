using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace XJDbQuery.Expressions
{
   public  class DbExpressionReplacer:DbExpressionVisitor
    {
        Expression searchFor;
        Expression replaceWith;
        public Expression Replace(Expression expression, Expression searchFor, Expression replaceWith)
        {
            this.searchFor = searchFor;
            this.replaceWith = replaceWith;
            return this.Visit(expression);
        }
        protected override Expression Visit(Expression exp)
        {
            if (exp == this.searchFor)
            {
                return this.replaceWith;
            }
            return base.Visit(exp);
        }
    }
}
