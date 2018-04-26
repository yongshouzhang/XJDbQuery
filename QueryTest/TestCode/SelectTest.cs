using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using XJDbQuery.Provider;
using XJDbQuery.Query;
using System.Configuration;
using System.Data.SqlClient;
namespace QueryTest.TestCode
{
    using Model;
    public class SelectTest
    {
        public static void Run()
        {
            new SelectTest().TestMethod();
        }
        private void TestMethod()
        {
            string connStr = ConfigurationManager.ConnectionStrings["example"].ConnectionString;
            SqlConnection conn = new SqlConnection(connStr);
            DbQueryProvider provider = new DbQueryProvider(conn);

            DbQuery<Student> student = new DbQuery<Student>(provider);

            student.Where(obj => obj.ID > 5).Select(obj => new { StudentID = obj.ID, StudentName = obj.Name })
                .OrderBy(obj => obj.StudentID).ToList().ForEach(obj =>
                {
                    Console.WriteLine(" StudentID:{0} \t StudentName:{1} ", obj.StudentID, obj.StudentName);
                });
        }
    }
}
