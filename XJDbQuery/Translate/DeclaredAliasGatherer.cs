using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace XJDbQuery.Translate
{
    using Expressions;
    public class DeclaredAliasGatherer : DbExpressionVisitor
    {
        HashSet<string> aliases;

        private DeclaredAliasGatherer()
        {
            this.aliases = new HashSet<string>();
        }

        public static HashSet<string> Gather(Expression source)
        {
            var gatherer = new DeclaredAliasGatherer();
            gatherer.Visit(source);
            return gatherer.aliases;
        }

        protected override Expression VisitSelect(SelectExpression select)
        {
            this.aliases.Add(select.Alias);
            return select;
        }

        protected override Expression VisitTable(TableExpression table)
        {
            this.aliases.Add(table.Alias);
            return table;
        }
    }
   
}
