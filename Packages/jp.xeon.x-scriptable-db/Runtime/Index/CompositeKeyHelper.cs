using System;
using System.Globalization;
using System.Text;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// 複合キーの生成とハッシュ値の計算のためのヘルパークラス。
    /// ハッシュの衝突を完全に回避するための文字列キーベースのルックアップを提供します。
    /// </summary>
    public static class CompositeKeyHelper
    {
        private const char KeyDelimiter = '\x1F';
        private const char EscapeChar = '\x1E';
        private const string NullPlaceholder = "\x00NULL\x00";

        /// <summary>
        /// 複合キーの文字列表現を生成します。
        /// 衝突を避けるためにユニットセパレータ（ASCII 31）を区切り文字として使用します。
        /// 特殊文字はエスケープされます。
        /// </summary>
        /// <param name="keys">キー値の配列</param>
        /// <returns>文字列表現</returns>
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
        /// 値を文字列に変換し、特殊文字をエスケープします。
        /// カルチャに依存しない変換を使用します。
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
        /// カルチャに依存しない文字列変換を実行します。
        /// float/doubleは精度を保つためにラウンドトリップ形式を使用します。
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
        /// デバッグ用に人間が読める文字列表現を生成します。
        /// カルチャに依存しない変換を使用します。
        /// </summary>
        /// <param name="keys">キー値の配列</param>
        /// <returns>人間が読める文字列表現</returns>
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
        /// 決定論的ハッシュコードを計算します。
        /// string.GetHashCode()は.NETランタイムの実装によって異なる値を返す可能性があるため、
        /// カスタムの決定論的ハッシュ関数を使用します。
        /// </summary>
        /// <param name="str">ハッシュ化する文字列</param>
        /// <returns>決定論的ハッシュ値</returns>
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
        /// 複合キーの決定論的ハッシュ値を計算します。
        /// </summary>
        /// <param name="keys">キー値の配列</param>
        /// <returns>決定論的ハッシュ値</returns>
        public static int ComputeCompositeHash(params object[] keys)
        {
            var compositeString = ComputeCompositeString(keys);
            return GetDeterministicHashCode(compositeString);
        }
    }
}
