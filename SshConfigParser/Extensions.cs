using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace SshConfigParser
{
    internal static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var element in collection)
            {
                action(element);
            }
        }

        public static string RegexReplace(this string input, string pattern, string replacement)
        {
            return new Regex(pattern).Replace(input, replacement);
        }
    }
}