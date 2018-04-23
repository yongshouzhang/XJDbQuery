using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
namespace XJDbQuery.Common
{
    public static class ReadOnlyExtensions
    {
        public static ReadOnlyCollection<T> ToReadOnly<T>(this IEnumerable<T> collection)
        {
            ReadOnlyCollection<T> roc = collection as ReadOnlyCollection<T>;
            return roc == null ? new List<T>().AsReadOnly() : roc.ToList().AsReadOnly();
        }
    }
}
