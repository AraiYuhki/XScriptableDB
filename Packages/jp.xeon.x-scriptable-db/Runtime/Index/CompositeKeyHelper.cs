using System;
using System.Globalization;
using System.Text;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// Helper class for generating composite keys and computing hash values.
    /// Provides string-key-based lookup to completely avoid hash collisions.
    /// </summary>
    public static class CompositeKeyHelper
    {
        private const char KeyDelimiter = '\x1F';
        private const char EscapeChar = '\x1E';
        private const string NullPlaceholder = "\x00NULL\x00";

        /// <summary>
        /// Generates the string representation of a composite key.
        /// Uses Unit Separator (ASCII 31) as a delimiter to avoid collisions.
        /// Special characters are escaped.
        /// </summary>
        /// <param name="keys">Array of key values</param>
        /// <returns>String representation</returns>
        public static string ComputeCompositeString(params object[] keys)
        {
            if (keys == null || keys.Length == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (var i = 0; i < keys.Length; i++)
            {
                if (i > 0)
                    sb.Append(KeyDelimiter);

                if (keys[i] == null)
                    sb.Append(NullPlaceholder);
                else
                    sb.Append(EscapeValue(keys[i]));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Converts a value to a string and escapes special characters.
        /// Uses culture-invariant conversion.
        /// </summary>
        private static string EscapeValue(object value)
        {
            var str = ConvertToInvariantString(value);
            if (string.IsNullOrEmpty(str))
                return str;

            if (str.IndexOf(EscapeChar) < 0 && str.IndexOf(KeyDelimiter) < 0)
                return str;

            var sb = new StringBuilder(str.Length + 4);
            foreach (var c in str)
            {
                if (c == EscapeChar)
                    sb.Append(EscapeChar).Append(EscapeChar);
                else if (c == KeyDelimiter)
                    sb.Append(EscapeChar).Append('D');
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Performs culture-invariant string conversion.
        /// Uses round-trip format for float/double to preserve precision.
        /// </summary>
        private static string ConvertToInvariantString(object value)
        {
            return value switch
            {
                float f => f.ToString("R", CultureInfo.InvariantCulture),
                double d => d.ToString("R", CultureInfo.InvariantCulture),
                DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
                DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
                SerializableDateTime sdt => sdt.Ticks.ToString(CultureInfo.InvariantCulture),
                IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
                _ => value.ToString()
            };
        }

        /// <summary>
        /// Generates a human-readable string representation for debugging.
        /// Uses culture-invariant conversion.
        /// </summary>
        /// <param name="keys">Array of key values</param>
        /// <returns>Human-readable string representation</returns>
        public static string ComputeReadableString(params object[] keys)
        {
            if (keys == null || keys.Length == 0)
                return string.Empty;

            var parts = new string[keys.Length];
            for (var i = 0; i < keys.Length; i++)
                parts[i] = keys[i] == null ? "null" : ConvertToInvariantString(keys[i]);
            return string.Join("|", parts);
        }

        /// <summary>
        /// Computes a deterministic hash code.
        /// Uses a custom deterministic hash function because string.GetHashCode()
        /// may return different values depending on the .NET runtime implementation.
        /// </summary>
        /// <param name="str">String to hash</param>
        /// <returns>Deterministic hash value</returns>
        public static int GetDeterministicHashCode(string str)
        {
            if (string.IsNullOrEmpty(str))
                return 0;

            unchecked
            {
                var hash1 = 5381;
                var hash2 = hash1;

                for (var i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i + 1 < str.Length)
                        hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        /// <summary>
        /// Computes the deterministic hash value of a composite key.
        /// </summary>
        /// <param name="keys">Array of key values</param>
        /// <returns>Deterministic hash value</returns>
        public static int ComputeCompositeHash(params object[] keys)
        {
            var compositeString = ComputeCompositeString(keys);
            return GetDeterministicHashCode(compositeString);
        }
    }
}
