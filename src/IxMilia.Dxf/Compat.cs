using System;
using System.Collections.Generic;

namespace IxMilia.Dxf
{
    /// <summary>Compatibility layer to support net35, but use modern APIs where possible.</summary>
    internal static class Compat
    {
        /// <summary>Indicates where the given string is <c>null</c>, empty, or consists only of white-space characters.</summary>
        /// <param name="value">The string to test.</param>
        /// <returns>true if <c>null</c>, empty, or whitespace.</returns>
        public static bool IsNullOrWhiteSpace(string value)
        {
#if NET35
            if (value != null)
            {
                foreach (char c in value)
                {
                    if (!char.IsWhiteSpace(c))
                    {
                        return false;
                    }
                }
            }
            
            return true;
#else
            return string.IsNullOrWhiteSpace(value);
#endif
        }

        /// <summary>Converts the string representation to a <see cref="Guid" /> (success) or <see cref="Guid.Empty" />.</summary>
        /// <param name="s">The string to parse.</param>
        /// <returns>The guid.</returns>
        public static Guid GuidString(string s)
        {
#if NET35
            try
            {
                return new Guid(s);
            }
            catch
            {
                return Guid.Empty;
            }
#else
            if (Guid.TryParse(s, out Guid result))
            {
                return result;
            }

            return Guid.Empty;
#endif
        }
    }
}