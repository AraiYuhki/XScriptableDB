using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// バリデーション属性の基底クラス。
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public abstract class ValidationAttribute : Attribute
    {
        /// <summary>エラーメッセージ</summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// 値を検証する。
        /// </summary>
        /// <param name="value">検証する値</param>
        /// <param name="fieldName">フィールド名</param>
        /// <returns>検証結果</returns>
        public abstract ValidationResult Validate(object value, string fieldName);

        /// <summary>
        /// デフォルトのエラーメッセージを取得する。
        /// </summary>
        protected abstract string GetDefaultErrorMessage(string fieldName);

        /// <summary>
        /// エラーメッセージを取得する。
        /// </summary>
        protected string GetErrorMessage(string fieldName)
        {
            return string.IsNullOrEmpty(ErrorMessage)
                ? GetDefaultErrorMessage(fieldName)
                : ErrorMessage;
        }
    }
}
