using System.Collections.Generic;
using System.Linq.Expressions;
namespace XJDbQuery.Translate
{
    public abstract class ProjectionRow
    {
        public abstract object GetValue(string columnName);
        public abstract IEnumerable<T> ExcuteSubQuery<T>(LambdaExpression query);
    }
    public class ColumnProjection
    {
        public string Columns { get; set; }
        public Expression Selector { get; set; }
    }

}
