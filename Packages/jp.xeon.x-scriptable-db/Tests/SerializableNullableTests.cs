using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Xeon.XScriptableDB.IO;
using Xeon.XScriptableDB.Validation;
using Range = Xeon.XScriptableDB.Validation.RangeAttribute;

namespace Xeon.XScriptableDB.Tests
{
    public class SerializableNullableTests
    {
        [Serializable]
        private class TestData : CsvData
        {
            [CsvColumn("id")]
            public int id;

            [CsvColumn("nullable_int")]
            public SerializableNullable<int> nullableInt;

            [CsvColumn("nullable_float")]
            public SerializableNullable<float> nullableFloat;
        }

        [Serializable]
        private class ValidatedData
        {
            [Range(1, 10)]
            public SerializableNullable<int> rangedValue;
        }

        [Test]
        public void 空文字はHasValueがfalseとしてパースされる()
        {
            var csv = "id,nullable_int,nullable_float\n1,,\n";
            var result = CsvParser.Parse<TestData>(csv);

            Assert.That(result[0].nullableInt.HasValue, Is.False);
            Assert.That(result[0].nullableFloat.HasValue, Is.False);
        }

        [Test]
        public void nullという文字列もHasValueがfalseとしてパースされる()
        {
            var csv = "id,nullable_int,nullable_float\n1,null,NULL\n";
            var result = CsvParser.Parse<TestData>(csv);

            Assert.That(result[0].nullableInt.HasValue, Is.False);
            Assert.That(result[0].nullableFloat.HasValue, Is.False);
        }

        [Test]
        public void 値ありのセルはHasValueがtrueで値が入る()
        {
            var csv = "id,nullable_int,nullable_float\n1,42,1.5\n";
            var result = CsvParser.Parse<TestData>(csv);

            Assert.That(result[0].nullableInt.HasValue, Is.True);
            Assert.That(result[0].nullableInt.Value, Is.EqualTo(42));
            Assert.That(result[0].nullableFloat.Value, Is.EqualTo(1.5f));
        }

        [Test]
        public void エクスポートで値なしはnull文字列になり値ありは数値になる()
        {
            var data = new TestData
            {
                id = 1,
                nullableInt = 42,
                nullableFloat = SerializableNullable<float>.None,
            };
            var csv = CsvParser.ToCSV(new List<TestData> { data }).Replace("\r\n", "\n");

            Assert.That(csv, Is.EqualTo("id,nullable_int,nullable_float\n1,42,null\n"));
        }

        [Test]
        public void CSV往復で値の有無が保存される()
        {
            var original = new TestData
            {
                id = 1,
                nullableInt = SerializableNullable<int>.None,
                nullableFloat = 2.5f,
            };
            var csv = CsvParser.ToCSV(new List<TestData> { original });
            var restored = CsvParser.Parse<TestData>(csv)[0];

            Assert.That(restored.nullableInt.HasValue, Is.False);
            Assert.That(restored.nullableFloat.Value, Is.EqualTo(2.5f));
        }

        [Test]
        public void Unityシリアライズで値の有無が保存される()
        {
            // JsonUtility は Unity のシリアライズ規則に従うため、SerializeField の永続化を検証できる
            var original = new TestData
            {
                id = 1,
                nullableInt = 42,
                nullableFloat = SerializableNullable<float>.None,
            };
            var restored = JsonUtility.FromJson<TestData>(JsonUtility.ToJson(original));

            Assert.That(restored.nullableInt.HasValue, Is.True);
            Assert.That(restored.nullableInt.Value, Is.EqualTo(42));
            Assert.That(restored.nullableFloat.HasValue, Is.False);
        }

        [Test]
        public void Nullableとの相互変換ができる()
        {
            SerializableNullable<int> fromValue = 5;
            SerializableNullable<int> fromNull = (int?)null;
            int? backToNullable = fromValue;

            Assert.That(fromValue.HasValue, Is.True);
            Assert.That(fromNull.HasValue, Is.False);
            Assert.That(backToNullable, Is.EqualTo(5));
            Assert.That(fromValue.GetValueOrDefault(-1), Is.EqualTo(5));
            Assert.That(fromNull.GetValueOrDefault(-1), Is.EqualTo(-1));
        }

        [Test]
        public void 値なしはRange検証をスキップし値ありは範囲チェックされる()
        {
            var noValue = new ValidatedData { rangedValue = SerializableNullable<int>.None };
            var inRange = new ValidatedData { rangedValue = 5 };
            var outOfRange = new ValidatedData { rangedValue = 99 };

            Assert.That(RecordValidator.ValidateRecord(noValue).IsValid, Is.True);
            Assert.That(RecordValidator.ValidateRecord(inRange).IsValid, Is.True);
            Assert.That(RecordValidator.ValidateRecord(outOfRange).IsValid, Is.False);
        }
    }
}
