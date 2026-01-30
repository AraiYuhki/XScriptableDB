using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Tests
{
    public class CsvParseTest
    {
        private enum TestEnum
        {
            Zero,
            One,
            Two,
        }

        [Serializable]
        private class TestData : CsvData
        {
            [CsvColumn("int_value")]
            public int intValue;

            [CsvColumn("float_value")]
            public float floatValue;

            [CsvColumn("string_value")]
            public string stringValue;

            [CsvColumn("bool_value")]
            public bool boolValue;

            [CsvColumn("enum_value")]
            public TestEnum enumValue;
        }

        [Test]
        public void ToCsvTest()
        {
            var testData = new TestData
            {
                intValue = 10,
                floatValue = 123.45f,
                stringValue = "abcdefg",
                boolValue = false,
                enumValue = TestEnum.Two
            };
            var csv = CsvParser.ToCSV(new List<TestData> { testData });
            // 改行コードを正規化して比較（プラットフォーム非依存）
            var normalizedCsv = csv.Replace("\r\n", "\n");
            var expect = "int_value,float_value,string_value,bool_value,enum_value\n10,123.45,\"abcdefg\",False,Two\n";
            Assert.That(normalizedCsv, Is.EqualTo(expect));
        }

        [Test]
        public void ParseTest()
        {
            var csv = "int_value,float_value,string_value,bool_value,enum_value\n10,123.45,\"abcdefg\",False,Two\n";
            var result = CsvParser.Parse<TestData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            var data = result.First();
            Assert.That(data.intValue, Is.EqualTo(10));
            Assert.That(data.floatValue, Is.EqualTo(123.45f));
            Assert.That(data.stringValue, Is.EqualTo("abcdefg"));
            Assert.That(data.boolValue, Is.EqualTo(false));
            Assert.That(data.enumValue, Is.EqualTo(TestEnum.Two));
        }

        [Test]
        public void RoundTripTest()
        {
            var original = new List<TestData>
            {
                new TestData { intValue = 1, floatValue = 1.5f, stringValue = "test1", boolValue = true, enumValue = TestEnum.One },
                new TestData { intValue = 2, floatValue = 2.5f, stringValue = "test2", boolValue = false, enumValue = TestEnum.Two }
            };

            var csv = CsvParser.ToCSV(original);
            var parsed = CsvParser.Parse<TestData>(csv);

            Assert.That(parsed.Count, Is.EqualTo(original.Count));
            for (var i = 0; i < original.Count; i++)
            {
                Assert.That(parsed[i].intValue, Is.EqualTo(original[i].intValue));
                Assert.That(parsed[i].floatValue, Is.EqualTo(original[i].floatValue));
                Assert.That(parsed[i].stringValue, Is.EqualTo(original[i].stringValue));
                Assert.That(parsed[i].boolValue, Is.EqualTo(original[i].boolValue));
                Assert.That(parsed[i].enumValue, Is.EqualTo(original[i].enumValue));
            }
        }
    }
}
