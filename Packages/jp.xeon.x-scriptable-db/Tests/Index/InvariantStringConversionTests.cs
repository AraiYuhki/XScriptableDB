using NUnit.Framework;
using System;
using System.Globalization;
using System.Threading;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// カルチャ非依存の文字列変換と、float/doubleのラウンドトリップ精度のテスト。
    /// </summary>
    public class InvariantStringConversionTests
    {
        private CultureInfo originalCulture;
        private CultureInfo originalUICulture;

        [SetUp]
        public void SetUp()
        {
            originalCulture = Thread.CurrentThread.CurrentCulture;
            originalUICulture = Thread.CurrentThread.CurrentUICulture;
        }

        [TearDown]
        public void TearDown()
        {
            Thread.CurrentThread.CurrentCulture = originalCulture;
            Thread.CurrentThread.CurrentUICulture = originalUICulture;
        }

        #region Test Data Classes

        private class FloatKeyRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("FloatKey")]
            public float Value { get; set; }
        }

        private class DoubleKeyRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("DoubleKey")]
            public double Value { get; set; }
        }

        private class DecimalKeyRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("DecimalKey")]
            public decimal Value { get; set; }
        }

        #endregion

        #region Float Round-Trip Precision Tests

        [Test]
        public void FloatKey_RoundTripPrecision_PreservesDistinctValues()
        {
            var f1 = 1.0000001f;
            var f2 = 1.0000002f;
            Assert.That(f1, Is.Not.EqualTo(f2));

            var records = new[]
            {
                new FloatKeyRecord { Id = 1, Value = f1 },
                new FloatKeyRecord { Id = 2, Value = f2 }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("FloatKey");

            var result1 = index.FindByKey(f1);
            var result2 = index.FindByKey(f2);

            Assert.That(result1.Length, Is.EqualTo(1), "Float f1は正確に1つのレコードを見つけるはず");
            Assert.That(result2.Length, Is.EqualTo(1), "Float f2は正確に1つのレコードを見つけるはず");
            Assert.That(result1[0], Is.EqualTo(0), "Float f1はインデックス0のレコードを見つけるはず");
            Assert.That(result2[0], Is.EqualTo(1), "Float f2はインデックス1のレコードを見つけるはず");
        }

        [Test]
        public void DoubleKey_RoundTripPrecision_PreservesDistinctValues()
        {
            var d1 = 1.00000000000001d;
            var d2 = 1.00000000000002d;
            Assert.That(d1, Is.Not.EqualTo(d2));

            var records = new[]
            {
                new DoubleKeyRecord { Id = 1, Value = d1 },
                new DoubleKeyRecord { Id = 2, Value = d2 }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("DoubleKey");

            var result1 = index.FindByKey(d1);
            var result2 = index.FindByKey(d2);

            Assert.That(result1.Length, Is.EqualTo(1), "Double d1は正確に1つのレコードを見つけるはず");
            Assert.That(result2.Length, Is.EqualTo(1), "Double d2は正確に1つのレコードを見つけるはず");
            Assert.That(result1[0], Is.EqualTo(0), "Double d1はインデックス0のレコードを見つけるはず");
            Assert.That(result2[0], Is.EqualTo(1), "Double d2はインデックス1のレコードを見つけるはず");
        }

        [Test]
        public void FloatKey_VeryCloseValues_RemainDistinct()
        {
            var baseValue = 123.456789f;
            var records = new[]
            {
                new FloatKeyRecord { Id = 1, Value = baseValue },
                new FloatKeyRecord { Id = 2, Value = BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(baseValue) + 1) }
            };

            Assert.That(records[0].Value, Is.Not.EqualTo(records[1].Value));

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("FloatKey");

            var result1 = index.FindByKey(records[0].Value);
            var result2 = index.FindByKey(records[1].Value);

            Assert.That(result1.Length, Is.EqualTo(1));
            Assert.That(result2.Length, Is.EqualTo(1));
            Assert.That(result1[0], Is.Not.EqualTo(result2[0]));
        }

        [Test]
        public void DoubleKey_VeryCloseValues_RemainDistinct()
        {
            var baseValue = 123.456789012345d;
            var records = new[]
            {
                new DoubleKeyRecord { Id = 1, Value = baseValue },
                new DoubleKeyRecord { Id = 2, Value = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(baseValue) + 1) }
            };

            Assert.That(records[0].Value, Is.Not.EqualTo(records[1].Value));

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("DoubleKey");

            var result1 = index.FindByKey(records[0].Value);
            var result2 = index.FindByKey(records[1].Value);

            Assert.That(result1.Length, Is.EqualTo(1));
            Assert.That(result2.Length, Is.EqualTo(1));
            Assert.That(result1[0], Is.Not.EqualTo(result2[0]));
        }

        #endregion

        #region Culture-Invariant Tests

        [Test]
        public void FloatKey_FrenchCulture_UsesInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");

            var testValue = 1.5f;
            Assert.That(testValue.ToString(), Is.EqualTo("1,5"), "フランスのカルチャではカンマを使用するはず");

            var records = new[]
            {
                new FloatKeyRecord { Id = 1, Value = testValue }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("FloatKey");

            var result = index.FindByKey(testValue);
            Assert.That(result.Length, Is.EqualTo(1), "インバリアントな検索を使用してレコードを見つけるはず");
            Assert.That(result[0], Is.EqualTo(0));
        }

        [Test]
        public void DoubleKey_FrenchCulture_UsesInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");

            var testValue = 1.5d;
            Assert.That(testValue.ToString(), Is.EqualTo("1,5"), "フランスのカルチャではカンマを使用するはず");

            var records = new[]
            {
                new DoubleKeyRecord { Id = 1, Value = testValue }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("DoubleKey");

            var result = index.FindByKey(testValue);
            Assert.That(result.Length, Is.EqualTo(1), "インバリアントな検索を使用してレコードを見つけるはず");
            Assert.That(result[0], Is.EqualTo(0));
        }

        [Test]
        public void DecimalKey_GermanCulture_UsesInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("de-DE");

            var testValue = 1234.56m;
            Assert.That(testValue.ToString(), Is.EqualTo("1234,56"), "ドイツのカルチャではカンマを使用するはず");

            var records = new[]
            {
                new DecimalKeyRecord { Id = 1, Value = testValue }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("DecimalKey");

            var result = index.FindByKey(testValue);
            Assert.That(result.Length, Is.EqualTo(1), "インバリアントな検索を使用してレコードを見つけるはず");
            Assert.That(result[0], Is.EqualTo(0));
        }

        [Test]
        public void Index_BuildInOneCulture_SearchInAnother_Works()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            var testValue = 3.14159f;
            var records = new[]
            {
                new FloatKeyRecord { Id = 1, Value = testValue }
            };
            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("FloatKey");

            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("fr-FR");

            var result = index.FindByKey(testValue);
            Assert.That(result.Length, Is.EqualTo(1), "現在のカルチャに関係なくレコードを見つけるはず");
            Assert.That(result[0], Is.EqualTo(0));
        }

        #endregion

        #region CompositeKeyHelper Tests

        [Test]
        public void CompositeKeyHelper_Float_RoundTripPrecision()
        {
            var f1 = 1.0000001f;
            var f2 = 1.0000002f;

            var key1 = CompositeKeyHelper.ComputeCompositeString("Test", f1);
            var key2 = CompositeKeyHelper.ComputeCompositeString("Test", f2);

            Assert.That(key1, Is.Not.EqualTo(key2), "異なるfloat値は異なるキーを生成するはず");
        }

        [Test]
        public void CompositeKeyHelper_Double_RoundTripPrecision()
        {
            var d1 = 1.00000000000001d;
            var d2 = 1.00000000000002d;

            var key1 = CompositeKeyHelper.ComputeCompositeString("Test", d1);
            var key2 = CompositeKeyHelper.ComputeCompositeString("Test", d2);

            Assert.That(key1, Is.Not.EqualTo(key2), "異なるdouble値は異なるキーを生成するはず");
        }

        [Test]
        public void CompositeKeyHelper_Float_FrenchCulture_UsesInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

            var key = CompositeKeyHelper.ComputeCompositeString("Test", 1.5f);

            Assert.That(key, Does.Contain("1.5"));
            Assert.That(key, Does.Not.Contain("1,5"));
        }

        [Test]
        public void CompositeKeyHelper_Double_FrenchCulture_UsesInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

            var key = CompositeKeyHelper.ComputeCompositeString("Test", 1.5d);

            Assert.That(key, Does.Contain("1.5"));
            Assert.That(key, Does.Not.Contain("1,5"));
        }

        [Test]
        public void CompositeKeyHelper_ComputeReadableString_UsesInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

            var readable = CompositeKeyHelper.ComputeReadableString("Test", 1.5f, 2.5d);

            Assert.That(readable, Does.Contain("1.5"));
            Assert.That(readable, Does.Contain("2.5"));
            Assert.That(readable, Does.Not.Contain("1,5"));
            Assert.That(readable, Does.Not.Contain("2,5"));
        }

        #endregion

        #region IndexData FindByKey Tests

        [Test]
        public void IndexData_FindByKey_Float_UsesInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

            var indexData = new IndexData("TestIndex", typeof(float));
            indexData.AddEntry(0, "1.5", new[] { 0 });

            var result = indexData.FindByKey(1.5f);
            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(0));
        }

        [Test]
        public void IndexData_FindByKey_Double_UsesInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");

            var indexData = new IndexData("TestIndex", typeof(double));
            indexData.AddEntry(0, "1.5", new[] { 0 });

            var result = indexData.FindByKey(1.5d);
            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(0));
        }

        [Test]
        public void IndexData_FindByKey_Decimal_UsesInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("it-IT");

            var indexData = new IndexData("TestIndex", typeof(decimal));
            indexData.AddEntry(0, "1.5", new[] { 0 });

            var result = indexData.FindByKey(1.5m);
            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(0));
        }

        [Test]
        public void IndexData_FindByKey_ObjectOverload_Float_UsesInvariantFormat()
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

            var indexData = new IndexData("TestIndex", typeof(float));
            indexData.AddEntry(0, "1.5", new[] { 0 });

            object key = 1.5f;
            var result = indexData.FindByKey(key);
            Assert.That(result.Length, Is.EqualTo(1));
            Assert.That(result[0], Is.EqualTo(0));
        }

        #endregion

        #region Edge Cases

        [Test]
        public void FloatKey_SpecialValues_HandleCorrectly()
        {
            var records = new[]
            {
                new FloatKeyRecord { Id = 1, Value = float.MaxValue },
                new FloatKeyRecord { Id = 2, Value = float.MinValue },
                new FloatKeyRecord { Id = 3, Value = float.Epsilon }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("FloatKey");

            Assert.That(index.FindByKey(float.MaxValue).Length, Is.EqualTo(1));
            Assert.That(index.FindByKey(float.MinValue).Length, Is.EqualTo(1));
            Assert.That(index.FindByKey(float.Epsilon).Length, Is.EqualTo(1));
        }

        [Test]
        public void DoubleKey_SpecialValues_HandleCorrectly()
        {
            var records = new[]
            {
                new DoubleKeyRecord { Id = 1, Value = double.MaxValue },
                new DoubleKeyRecord { Id = 2, Value = double.MinValue },
                new DoubleKeyRecord { Id = 3, Value = double.Epsilon }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("DoubleKey");

            Assert.That(index.FindByKey(double.MaxValue).Length, Is.EqualTo(1));
            Assert.That(index.FindByKey(double.MinValue).Length, Is.EqualTo(1));
            Assert.That(index.FindByKey(double.Epsilon).Length, Is.EqualTo(1));
        }

        [Test]
        public void FloatKey_ZeroValues_HandleCorrectly()
        {
            var records = new[]
            {
                new FloatKeyRecord { Id = 1, Value = 0.0f },
                new FloatKeyRecord { Id = 2, Value = -0.0f }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("FloatKey");

            var result = index.FindByKey(0.0f);
            Assert.That(result.Length, Is.GreaterThanOrEqualTo(1));
        }

        [Test]
        public void DoubleKey_ZeroValues_HandleCorrectly()
        {
            var records = new[]
            {
                new DoubleKeyRecord { Id = 1, Value = 0.0d },
                new DoubleKeyRecord { Id = 2, Value = -0.0d }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("DoubleKey");

            var result = index.FindByKey(0.0d);
            Assert.That(result.Length, Is.GreaterThanOrEqualTo(1));
        }

        #endregion
    }
}
