using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Planar.Validation
{
    public class StringIgnoreCaseComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            return x.Equals(y, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode([DisallowNull] string obj)
        {
            return obj == null ? 0 : obj.GetHashCode();
        }

        public static StringIgnoreCaseComparer Instance => new StringIgnoreCaseComparer();
    }
}