namespace Xeon.XScriptableDB
{
    public static class StringUtility
    {
        /// <summary>
        /// 文字列をPascalCase（先頭大文字）に変換します。
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
        /// 文字列をcamelCase（先頭小文字）に変換します。
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
    }
}
