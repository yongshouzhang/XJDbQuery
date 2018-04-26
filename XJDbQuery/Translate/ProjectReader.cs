using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Common;
using System.Linq.Expressions;
using System.Collections;

namespace XJDbQuery.Translate
{
    using Expressions;
    using Common;
    public class ProjectionReader<T> : IEnumerable<T>, IEnumerable
    {
        Enumerator enumerator;

        private ProjectionReader(DbDataReader reader, Func<ProjectionRow, T> projector, IQueryProvider provider)
        {
            this.enumerator = new Enumerator(reader, projector, provider);
        }

        public IEnumerator<T> GetEnumerator()
        {
            Enumerator e = this.enumerator;
            if (e == null)
            {
                throw new InvalidOperationException("Cannot enumerate more than once");
            }
            this.enumerator = null;
            return e;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// 内部类
        /// </summary>
        class Enumerator : ProjectionRow, IEnumerator<T>, IEnumerator, IDisposable
        {
            DbDataReader reader;
            T current;
            Func<ProjectionRow, T> projector;
            IQueryProvider provider;
            internal Enumerator(DbDataReader reader, Func<ProjectionRow, T> projector, IQueryProvider provider)
            {
                this.reader = reader;
                this.projector = projector;
                this.provider = provider;
            }

            //重写projectrow 中的ExecuteSubQuery  方法
            public override IEnumerable<E> ExcuteSubQuery<E>(LambdaExpression query)
            {
                //干什么用的??
                // 将查询主体 替换为 查询参数
                //
                // Replace 参数 在query.Body 中搜索 query.parameter ，并用 Expression.Constant(this) 替换
                ProjectionExpression projection = (ProjectionExpression)new DbExpressionReplacer().
                    Replace(query.Body, query.Parameters[0], Expression.Constant(this));
                //计算 本地表达式,去除本地引用
                projection = PartialEvaluator.Eval(projection, CanEvaluateLocally) as ProjectionExpression;
                //执行查询
                IEnumerable<E> result = (IEnumerable<E>)this.provider.Execute(projection);
                List<E> list = new List<E>(result);
                // 如果是 IQueryable 类型，直接返回
                if (typeof(IQueryable<E>).IsAssignableFrom(query.Body.Type))
                {
                    return list.AsQueryable();
                }
                return list;
            }
            private static bool CanEvaluateLocally(Expression expression)
            {
                if (expression.NodeType == ExpressionType.Parameter ||
                    expression.NodeType.GetHashCode() > 1000)
                {
                    return false;
                }
                return true;
            }
            public override object GetValue(string colunmName)
            {
                if (string.IsNullOrEmpty(colunmName) || this.reader[colunmName] is DBNull)
                {
                    return null;
                }
                return reader[colunmName];
            }
            public T Current
            {
                get { return this.current; }
            }
            object IEnumerator.Current
            {
                get { return this.current; }
            }

            public bool MoveNext()
            {
                if (this.reader.Read())
                {
                    this.current = this.projector(this);
                    return true;
                }
                return false;
            }
            public void Reset()
            {
            }
            public void Dispose()
            {
                this.reader.Dispose();
            }
        }
    }
}
