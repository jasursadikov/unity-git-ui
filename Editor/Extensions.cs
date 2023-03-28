using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Abuksigun.PackageShortcuts
{
    using static Const;
    public static class Extensions
    {
        static readonly List<Task> shownExceptions = new ();

        public static T GetResultOrDefault<T>(this Task<T> task, T defaultValue = default)
        {
            if (task.IsCompletedSuccessfully)
                return task.Result;
            if (task.Exception != null && !shownExceptions.Contains(task))
            {
                shownExceptions.Add(task);
                throw task.Exception;
            }
            return defaultValue;
        }
        public static string WrapUp(this string self, string wrapLeft = "\"", string wrapRight = null)
        {
            return wrapLeft + self + wrapRight ?? wrapLeft;
        }
        public static string[] SplitLines(this string self)
        {
            return self.Split(new[] { '\n', '\r' }, RemoveEmptyEntries);
        }
        public static string Join(this IEnumerable<string> values)
        {
            return string.Join(string.Empty, values);
        }
        public static string Join(this IEnumerable<string> values, char separator)
        {
            return string.Join(separator, values);
        }
        public static string Join(this IEnumerable<string> values, string separator)
        {
            return string.Join(separator, values);
        }
        public static string AfterLast(this string self, char separator)
        {
            var index = self.LastIndexOf(separator);
            return index == -1 ? self : self[(index + 1)..];
        }
        public static string NormalizeSlashes(this string self)
        {
            return self.Replace('\\', '/');
        }
        public static T When<T>(this T self, bool condition) where T : class
        {
            return condition ? self : default;
        }
        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> createNew)
        {
            return dict.TryGetValue(key, out var value) ? value : dict[key] = createNew();
        }
    }
}
