namespace Xeon.XScriptableDB.IO
{
    public static class CsvSupport
    {
        public static string ToCsv(this string value)
            => $"\"{value?.Replace("\"", "\"\"")}\"";

        public static string FromCsv(this string self)
        {
            if (string.IsNullOrEmpty(self))
                return self;
            var result = self;
            if (result.StartsWith("\""))
                result = result.Substring(1);
            if (result.EndsWith("\""))
                result = result.Substring(0, result.Length - 1);
            return result.Replace("\"\"", "\"");
        }
    }
}