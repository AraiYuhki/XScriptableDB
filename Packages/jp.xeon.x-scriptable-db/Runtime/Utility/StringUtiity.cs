namespace Xeon.XScriptableDB
{
    public static class StringUtiity
    {
        public static string ToCamelCase(this string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;

            if (origin.Length == 1)
                return origin.ToLower();

            return char.ToUpper(origin[0]) + origin.Substring(1);
        }

        public static string ToPascalCase(this string origin)
        {
            if (string.IsNullOrEmpty(origin))
                return origin;

            if (origin.Length == 1)
                return origin.ToLower();

            return char.ToLower(origin[0]) + origin.Substring(1);
        }
    }
}
