using System;

namespace Xeon.XScriptableDB.Validation
{
    /// <summary>
    /// Base class for validation attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public abstract class ValidationAttribute : Attribute
    {
        /// <summary>Error message</summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Validates the value.
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <param name="fieldName">Field name</param>
        /// <returns>Validation result</returns>
        public abstract ValidationResult Validate(object value, string fieldName);

        /// <summary>
        /// Returns the default error message.
        /// </summary>
        protected abstract string GetDefaultErrorMessage(string fieldName);

        /// <summary>
        /// Returns the error message.
        /// </summary>
        protected string GetErrorMessage(string fieldName)
        {
            return string.IsNullOrEmpty(ErrorMessage)
                ? GetDefaultErrorMessage(fieldName)
                : ErrorMessage;
        }
    }
}
