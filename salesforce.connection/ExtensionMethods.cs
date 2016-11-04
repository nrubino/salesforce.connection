using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace salesforce.connection
{
    /// <summary>
    /// Class ExtensionMethods.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Partitions the specified size.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source">The source.</param>
        /// <param name="size">The size.</param>
        /// <returns>IEnumerable&lt;List&lt;T&gt;&gt;.</returns>
        public static IEnumerable<List<T>> Partition<T>(this IList<T> source, Int32 size)
        {
            for (int i = 0; i < (source.Count / size) + (source.Count % size > 0 ? 1 : 0); i++)
                yield return new List<T>(source.Skip(size * i).Take(size));
        }
    }
}
