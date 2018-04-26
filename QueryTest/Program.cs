using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Configuration;
using XJDbQuery.Provider;
using XJDbQuery.Query;
namespace QueryTest
{
    using Model;
    using TestCode;
    class Program
    {
        static void Main(string[] args)
        {
           try
            {
                SelectTest.Run();
            }
            catch(Exception e)
            {
                throw e;
            }
            finally
            {
                Console.ReadKey();
            }
        }
    }
}
