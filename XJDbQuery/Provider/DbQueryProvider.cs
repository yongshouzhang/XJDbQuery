using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Data;
using System.Data.Common;
using System.Reflection;
namespace XJDbQuery.Provider
{
    using Translate;
    using Common;
    public class DbQueryProvider : QueryProvider
    {
        IDbConnection connection;
        public DbQueryProvider(IDbConnection connection)
        {
            this.connection = connection;
        }

        public override object Execute(Expression expression)
        {
            //此处，翻译expression  并将查询语句发送到服务器端执行查询，
            // 并用自动的适配器读取结果
            // 翻译表达式
            TranslateResult result = this.Translate(expression);
            //创建SQL命令 
            DbCommand cmd = this.connection.CreateCommand() as DbCommand;
            cmd.CommandText = result.CommandText;

            if (connection.State == ConnectionState.Closed)
            {
                connection.Open();
            }
            try
            {
                //读取结果
                DbDataReader reader = cmd.ExecuteReader();
                Type elementType = TypeHelper.GetElementType(expression.Type);
                Delegate projector = result.Projector.Compile();

                return Activator.CreateInstance(typeof(ProjectionReader<>).MakeGenericType(elementType),
                          BindingFlags.Instance | BindingFlags.NonPublic, null,
                        new object[] { reader, projector, this }, null);

            }
            catch (Exception e)
            {
                throw new Exception();
            }
            finally
            {
                //connection.Close();
            }
        }

        private TranslateResult Translate(Expression expression)
        {
            //expression = PartialEvaluator.Eval(expression);

            return new Translator().Translate(expression);
        }
    }
   
}
