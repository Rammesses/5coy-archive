using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Flatten<T>(
            this IEnumerable<T> e,
            Func<T,IEnumerable<T>> f) => e.SelectMany(c => f(c).Flatten(f)).Concat(e);
    }
}