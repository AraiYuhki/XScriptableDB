using System.Collections.Generic;
using System.Text;

namespace Xeon.XScriptableDB.IO
{
    public static class CsvUtility
    {
        public static string Escape(string csv, Dictionary<string, string> escapedData)
            => EscapeQuotedStrings(csv, escapedData);

        public static string EscapeQuotedStrings(string csv, Dictionary<string, string> escapedData)
        {
            if (string.IsNullOrEmpty(csv))
                return csv;

            var result = new StringBuilder(csv.Length);
            var escapedIndexes = new Dictionary<string, int>();

            for (var index = 0; index < csv.Length; index++)
            {
                var current = csv[index];
                if (current != '"')
                {
                    result.Append(current);
                    continue;
                }

                var startIndex = index;
                index++;

                var foundClose = false;
                while (index < csv.Length)
                {
                    if (csv[index] == '"')
                    {
                        if (index + 1 < csv.Length && csv[index + 1] == '"')
                        {
                            index += 2;
                            continue;
                        }

                        index++;
                        foundClose = true;
                        break;
                    }

                    index++;
                }

                if (!foundClose)
                {
                    result.Append(csv[startIndex..index]);
                    index--;
                    continue;
                }

                var endIndex = index;
                var target = csv[startIndex..endIndex];

                if (!escapedIndexes.TryGetValue(target, out var escapeIndex))
                {
                    escapeIndex = escapedIndexes.Count;
                    escapedIndexes[target] = escapeIndex;
                }

                var replaceText = $"<escaped string>{escapeIndex}</escaped string>";
                result.Append(replaceText);
                if (!escapedData.ContainsKey(replaceText))
                    escapedData.Add(replaceText, target);

                index--;
            }

            return result.ToString();
        }
    }
}
