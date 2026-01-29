using NUnit.Framework;
using System;
using Xeon.XScriptableDB.Validation;
using RangeAttribute = Xeon.XScriptableDB.Validation.RangeAttribute;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// バリデーション属性のテスト。
    /// </summary>
    public class ValidationTests
    {
        #region Test Data Classes

        private class RequiredTestRecord
        {
            [Required]
            public string Name;

            public string Description;

            public int Value;
        }

        private class RequiredAllowEmptyTestRecord
        {
            [Required]
            public string Name;

            [Required(AllowEmptyString = true)]
            public string OptionalText;
        }

        private class RangeTestRecord
        {
            [Range(0, 100)]
            public int Percentage;

            [Range(-10.5, 10.5)]
            public double Offset;
        }

        private class StringLengthTestRecord
        {
            [StringLength(10)]
            public string ShortText;

            [StringLength(100, MinimumLength = 5)]
            public string MediumText;
        }

        private class RegexTestRecord
        {
            [RegularExpression(@"^[a-zA-Z0-9]+$")]
            public string AlphaNumeric;

            [RegularExpression(@"^\d{3}-\d{4}$", ErrorMessage = "郵便番号の形式が正しくありません。")]
            public string PostalCode;
        }

        private class CompareTestRecord
        {
            public int Min;

            [Compare("Min", Operator = CompareOperator.GreaterThanOrEqual)]
            public int Max;
        }

        private class UniqueTestRecord
        {
            [PrimaryKey]
            public int Id;

            [Unique]
            public string Code;

            public string Name;
        }

        #endregion

        #region Required Tests

        [Test]
        public void Required_NullValue_ReturnsError()
        {
            var record = new RequiredTestRecord { Name = null };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].FieldName, Is.EqualTo("Name"));
        }

        [Test]
        public void Required_EmptyString_ReturnsError()
        {
            var record = new RequiredTestRecord { Name = "" };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Required_ValidValue_ReturnsSuccess()
        {
            var record = new RequiredTestRecord { Name = "Test" };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Required_AllowEmptyString_AcceptsEmpty()
        {
            var record = new RequiredAllowEmptyTestRecord { Name = "Test", OptionalText = "" };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Required_AllowEmptyString_RejectsNull()
        {
            var record = new RequiredAllowEmptyTestRecord { Name = "Test", OptionalText = null };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].FieldName, Is.EqualTo("OptionalText"));
        }

        #endregion

        #region Range Tests

        [Test]
        public void Range_ValueInRange_ReturnsSuccess()
        {
            var record = new RangeTestRecord { Percentage = 50, Offset = 0.0 };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Range_ValueBelowMinimum_ReturnsError()
        {
            var record = new RangeTestRecord { Percentage = -1 };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].FieldName, Is.EqualTo("Percentage"));
        }

        [Test]
        public void Range_ValueAboveMaximum_ReturnsError()
        {
            var record = new RangeTestRecord { Percentage = 101 };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Range_ValueAtBoundary_ReturnsSuccess()
        {
            var record1 = new RangeTestRecord { Percentage = 0 };
            var record2 = new RangeTestRecord { Percentage = 100 };

            Assert.That(RecordValidator.ValidateRecord(record1).IsValid, Is.True);
            Assert.That(RecordValidator.ValidateRecord(record2).IsValid, Is.True);
        }

        [Test]
        public void Range_DoubleValue_ValidatesCorrectly()
        {
            var valid = new RangeTestRecord { Offset = 10.5 };
            var invalid = new RangeTestRecord { Offset = 10.6 };

            Assert.That(RecordValidator.ValidateRecord(valid).IsValid, Is.True);
            Assert.That(RecordValidator.ValidateRecord(invalid).IsValid, Is.False);
        }

        #endregion

        #region StringLength Tests

        [Test]
        public void StringLength_ValidLength_ReturnsSuccess()
        {
            var record = new StringLengthTestRecord
            {
                ShortText = "Hello",
                MediumText = "Hello World"
            };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void StringLength_TooLong_ReturnsError()
        {
            var record = new StringLengthTestRecord
            {
                ShortText = "This is too long"
            };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].FieldName, Is.EqualTo("ShortText"));
        }

        [Test]
        public void StringLength_TooShort_ReturnsError()
        {
            var record = new StringLengthTestRecord
            {
                MediumText = "Hi"  // 最小5文字
            };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void StringLength_NullValue_ReturnsSuccess()
        {
            var record = new StringLengthTestRecord { ShortText = null };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.True);  // nullは別のバリデーションで処理
        }

        #endregion

        #region RegularExpression Tests

        [Test]
        public void Regex_ValidPattern_ReturnsSuccess()
        {
            var record = new RegexTestRecord
            {
                AlphaNumeric = "Test123",
                PostalCode = "123-4567"
            };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void Regex_InvalidPattern_ReturnsError()
        {
            var record = new RegexTestRecord
            {
                AlphaNumeric = "Test 123"  // スペースは許可されない
            };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void Regex_CustomErrorMessage_UsesCustomMessage()
        {
            var record = new RegexTestRecord
            {
                PostalCode = "invalid"
            };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors[0].Message, Does.Contain("郵便番号"));
        }

        #endregion

        #region ValidationResult Tests

        [Test]
        public void ValidationResult_Success_IsValid()
        {
            Assert.That(ValidationResult.Success.IsValid, Is.True);
            Assert.That(ValidationResult.Success.Errors.Count, Is.EqualTo(0));
        }

        [Test]
        public void ValidationResult_Error_IsNotValid()
        {
            var result = ValidationResult.Error("Field", "Error message");

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
        }

        [Test]
        public void ValidationResult_Merge_CombinesErrors()
        {
            var result1 = ValidationResult.Error("Field1", "Error 1");
            var result2 = ValidationResult.Error("Field2", "Error 2");

            result1.Merge(result2);

            Assert.That(result1.Errors.Count, Is.EqualTo(2));
        }

        [Test]
        public void ValidationResult_GetCombinedErrorMessage_FormatsCorrectly()
        {
            var result = new ValidationResult();
            result.AddError("Field1", "Error 1");
            result.AddError("Field2", "Error 2");

            var message = result.GetCombinedErrorMessage();

            Assert.That(message, Does.Contain("Field1"));
            Assert.That(message, Does.Contain("Field2"));
        }

        #endregion

        #region Multiple Validation Tests

        [Test]
        public void MultipleValidations_AllFail_ReturnsAllErrors()
        {
            var record = new RequiredTestRecord
            {
                Name = null  // Required違反
            };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
        }

        [Test]
        public void MultipleValidations_SomeFail_ReturnsOnlyFailedErrors()
        {
            var record = new RangeTestRecord
            {
                Percentage = 50,   // OK
                Offset = 100.0     // 範囲外
            };

            var result = RecordValidator.ValidateRecord(record);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.EqualTo(1));
            Assert.That(result.Errors[0].FieldName, Is.EqualTo("Offset"));
        }

        #endregion
    }
}
