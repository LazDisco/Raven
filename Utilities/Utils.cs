using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Raven.Utilities
{
    public static class Utils
    {
        /// <summary>Convert a word that is formatted in pascal case to have splits (by space) at each upper case letter.</summary>
        public static string SplitPascalCase(string convert)
        {
            return Regex.Replace(Regex.Replace(convert, @"(\P{Ll})(\P{Ll}\p{Ll})", "$1 $2"), @"(\p{Ll})(\P{Ll})", "$1 $2");
        }

        /// <summary>Calulate the next multiple of a specified number.</summary>
        /// <param name="num">The current number</param>
        /// <param name="mult">The number to find the next highest multiple of</param>
        public static int GetNextHighestMulitple(int num, int mult) => ((num + mult - 1) / mult) * mult;
    }
}
