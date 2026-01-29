using System.Collections.Generic;

namespace Xeon.XScriptableDB.IO
{
    public static class CsvUtility
    {
        public static string Escape(string csv, Dictionary<string, string> escapedData)
            => EscapeQuotedStrings(csv, escapedData);

        public static string EscapeQuotedStrings(string csv, Dictionary<string, string> escapedData)
        {
            var result = csv;
            var replaceTexts = new List<string>();
            var startIndex = -1;

            for (var index = 0; index < csv.Length; index++)
            {
                var current = csv[index];

                if (startIndex < 0)
                {
                    if (current != '"')
                        continue;
                    startIndex = index;
                    continue;
                }

                if (current != '"')
                    continue;

                if (index + 1 < csv.Length && csv[index + 1] == '"')
                {
                    index++;
                    continue;
                }

                var endIndex = index + 1;
                var target = csv[startIndex..endIndex];
                var escapeIndex = replaceTexts.IndexOf(target);
                if (escapeIndex < 0)
                {
                    escapeIndex = escapedData.Count;
                    replaceTexts.Add(target);
                }
                var replaceText = $"<escaped string>{escapeIndex}</escaped string>";
                result = result.Replace(target, replaceText);
                if (!escapedData.ContainsKey(replaceText))
                    escapedData.Add(replaceText, target);
                startIndex = -1;
            }
            return result;
        }
    }
}
