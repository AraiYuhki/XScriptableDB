using System.Text;

namespace Xeon.XScriptableDB
{
    public static class StringUtility
    {
        /// <summary>
        /// 文字列をPascalCase（最初の文字を大文字）に変換します。
        /// 例: "userName" → "UserName"
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
        /// 文字列をcamelCase（最初の文字を小文字）に変換します。
        /// 例: "UserName" → "userName"
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
        /// snake_caseをPascalCaseに変換します。
        /// 例: "created_at" → "CreatedAt"
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
        /// snake_caseをcamelCaseに変換します。
        /// 例: "created_at" → "createdAt"
        /// </summary>
        public static string SnakeToCamelCase(this string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;

            var pascalCase = origin.SnakeToPascalCase();
            return pascalCase.ToCamelCase();
        }

        /// <summary>
        /// camelCaseまたはPascalCaseをsnake_caseに変換します。
        /// 例: "createdAt" → "created_at", "CreatedAt" → "created_at"
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
