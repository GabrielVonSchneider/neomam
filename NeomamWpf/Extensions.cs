using System.Collections.Generic;

namespace NeomamWpf
{
    internal static class Extensions
    {
        public static void AddRange<T>(this ICollection<T> self, IEnumerable<T> range)
        {
            foreach (var item in range)
            {
                self.Add(item);
            }
        }
    }
}
