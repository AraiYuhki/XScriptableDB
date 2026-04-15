using System.Text;

namespace Xeon.XScriptableDB
{
    public static class StringUtility
    {
        /// <summary>
        /// Converts a string to PascalCase (first letter uppercase).
        /// Example: "userName" → "UserName"
        /// </summary>
        public static string ToPascalCase(this string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;

            if (origin.Length == 1)
                return origin.ToUpper();

            return char.ToUpper(origin[0]) + origin.Substring(1);
        }

        /// <summary>
        /// Converts a string to camelCase (first letter lowercase).
        /// Example: "UserName" → "userName"
        /// </summary>
        public static string ToCamelCase(this string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;

            if (origin.Length == 1)
                return origin.ToLower();

            return char.ToLower(origin[0]) + origin.Substring(1);
        }

        /// <summary>
        /// Converts snake_case to PascalCase.
        /// Example: "created_at" → "CreatedAt"
        /// </summary>
        public static string SnakeToPascalCase(this string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;

            var sb = new StringBuilder();
            var capitalizeNext = true;

            foreach (var c in origin)
            {
                if (c == '_')
                {
                    capitalizeNext = true;
                    continue;
                }

                sb.Append(capitalizeNext ? char.ToUpper(c) : c);
                capitalizeNext = false;
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts snake_case to camelCase.
        /// Example: "created_at" → "createdAt"
        /// </summary>
        public static string SnakeToCamelCase(this string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;

            var pascalCase = origin.SnakeToPascalCase();
            return pascalCase.ToCamelCase();
        }

        /// <summary>
        /// Converts camelCase or PascalCase to snake_case.
        /// Example: "createdAt" → "created_at", "CreatedAt" → "created_at"
        /// </summary>
        public static string ToSnakeCase(this string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;

            var sb = new StringBuilder();

            for (var i = 0; i < origin.Length; i++)
            {
                var c = origin[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                        sb.Append('_');
                    sb.Append(char.ToLower(c));
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }
}
