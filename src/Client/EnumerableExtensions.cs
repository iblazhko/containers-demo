using System;
using System.Collections.Generic;
using System.Linq;

namespace Client.Random
{
    public static class EnumerableExtension
    {
        public static T TakeRandom<T>(this IEnumerable<T> source)
        {
            return source.TakeRandom(1).Single();
        }

        public static IEnumerable<T> TakeRandom<T>(this IEnumerable<T> source, int count)
        {
            return source.Shuffle().Take(count);
        }

        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => Guid.NewGuid());
        }
    }
}
