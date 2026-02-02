using System;
using System.Text;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// 複合キーの生成とハッシュ計算を行うヘルパークラス。
    /// ハッシュ衝突を完全に回避するため、文字列キーベースの検索を提供する。
    /// </summary>
    public static class CompositeKeyHelper
    {
        private const char KeyDelimiter = '\x1F';
        private const string NullPlaceholder = "\x00NULL\x00";

        /// <summary>
        /// 複合キーの文字列表現を生成する。
        /// Unit Separator (ASCII 31) を区切り文字として使用し、衝突を回避する。
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
                    sb.Append(keys[i].ToString());
            }
            return sb.ToString();
        }

        /// <summary>
        /// デバッグ用の可読性のある文字列表現を生成する。
        /// </summary>
        /// <param name="keys">キー値の配列</param>
        /// <returns>可読性のある文字列表現</returns>
        public static string ComputeReadableString(params object[] keys)
        {
            if (keys == null || keys.Length == 0)
                return string.Empty;

            var parts = new string[keys.Length];
            for (var i = 0; i < keys.Length; i++)
                parts[i] = keys[i]?.ToString() ?? "null";
            return string.Join("|", parts);
        }

        /// <summary>
        /// 決定的なハッシュコードを計算する。
        /// string.GetHashCode() は .NET 実装によって異なる値を返す可能性があるため、
        /// 独自の決定的ハッシュ関数を使用する。
        /// </summary>
        /// <param name="str">ハッシュを計算する文字列</param>
        /// <returns>決定的なハッシュ値</returns>
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
        /// 複合キーの決定的ハッシュ値を計算する。
        /// </summary>
        /// <param name="keys">キー値の配列</param>
        /// <returns>決定的なハッシュ値</returns>
        public static int ComputeCompositeHash(params object[] keys)
        {
            var compositeString = ComputeCompositeString(keys);
            return GetDeterministicHashCode(compositeString);
        }
    }
}
