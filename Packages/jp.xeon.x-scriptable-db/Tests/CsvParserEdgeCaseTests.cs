using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// CSVパーサーのエッジケーステスト
    /// レビューで発見された問題を再現・検証するためのテスト
    /// </summary>
    public class CsvParserEdgeCaseTests
    {
        #region Test Data Classes

        [Serializable]
        private class SimpleData : CsvData
        {
            [CsvColumn("id")]
            public int id;

            [CsvColumn("name")]
            public string name;
        }

        [Serializable]
        private class DataWithList : CsvData
        {
            [CsvColumn("id")]
            public int id;

            [CsvColumn("items")]
            public List<string> items = new();
        }

        [Serializable]
        private class DataWithIntList : CsvData
        {
            [CsvColumn("id")]
            public int id;

            [CsvColumn("values")]
            public List<int> values = new();
        }

        #endregion

        #region Empty and Header-Only Tests

        [Test]
        public void Parse_EmptyCsv_ReturnsEmptyList()
        {
            var csv = "";
            var result = CsvParser.Parse<SimpleData>(csv);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Parse_HeaderOnly_ReturnsEmptyList()
        {
            var csv = "id,name\n";
            var result = CsvParser.Parse<SimpleData>(csv);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void Parse_HeaderOnlyNoNewline_ReturnsEmptyList()
        {
            var csv = "id,name";
            var result = CsvParser.Parse<SimpleData>(csv);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Count, Is.EqualTo(0));
        }

        #endregion

        #region Quote Escape Tests - EXPECTED TO FAIL UNTIL FIXED

        [Test]
        [Description("文字列内の引用符がエスケープされることをテスト")]
        public void ToCsv_StringWithQuotes_EscapesQuotes()
        {
            // 問題: ToCsv で " が "" にエスケープされない
            var value = "He said \"Hello\"";
            var escaped = value.ToCsv();

            // 期待: "He said ""Hello"""
            // 実際: "He said "Hello"" (不正なCSV)
            Assert.That(escaped, Is.EqualTo("\"He said \"\"Hello\"\"\""),
                "引用符は\"\"にエスケープされるべき");
        }

        [Test]
        [Description("エスケープされた引用符がパースできることをテスト")]
        public void FromCsv_EscapedQuotes_UnescapesCorrectly()
        {
            // 問題: FromCsv で "" が " に変換されない
            var escaped = "\"He said \"\"Hello\"\"\"";
            var result = escaped.FromCsv();

            // 期待: He said "Hello"
            Assert.That(result, Is.EqualTo("He said \"Hello\""),
                "\"\"は\"にアンエスケープされるべき");
        }

        [Test]
        [Description("引用符を含む文字列の往復変換テスト")]
        public void CsvRoundTrip_StringWithQuotes_PreservesValue()
        {
            var csv = @"id,name
1,""He said """"Hello""""""
";
            var result = CsvParser.Parse<SimpleData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].name, Is.EqualTo("He said \"Hello\""),
                "エスケープされた引用符が正しくパースされるべき");
        }

        #endregion

        #region Empty List Tests

        [Test]
        [Description("空のリストがパースできることをテスト")]
        public void Parse_EmptyList_ReturnsEmptyList()
        {
            var csv = @"id,items
1,[]
";
            var result = CsvParser.Parse<DataWithList>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].items, Is.Not.Null);
            Assert.That(result[0].items.Count, Is.EqualTo(0),
                "空の[]は空のListとしてパースされるべき");
        }

        [Test]
        [Description("空のリストの往復変換テスト")]
        public void CsvRoundTrip_EmptyList_PreservesEmpty()
        {
            var data = new List<DataWithList>
            {
                new DataWithList { id = 1, items = new List<string>() }
            };

            var csv = CsvParser.ToCSV(data);
            var result = CsvParser.Parse<DataWithList>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].items.Count, Is.EqualTo(0));
        }

        #endregion

        #region Nested Bracket Tests - EXPECTED TO FAIL UNTIL FIXED

        [Test]
        [Description("ネストされた括弧が正しく処理されることをテスト")]
        public void EscapeBrackets_NestedBrackets_HandlesCorrectly()
        {
            // 問題: ネストされた括弧に対応していない
            var csv = "{outer,{inner},after}";
            var escapedData = new Dictionary<string, string>();
            var result = CsvUtility.EscapeObject(csv, escapedData);

            // 全体が1つのオブジェクトとしてエスケープされるべき
            Assert.That(escapedData.Count, Is.EqualTo(1),
                "ネストされた括弧も含めて1つのオブジェクトとして認識されるべき");
        }

        #endregion

        #region Null Handling Tests

        [Test]
        [Description("null値のToCsv処理をテスト")]
        public void ToString_NullValue_HandlesGracefully()
        {
            // 問題: null チェックがない
            Assert.DoesNotThrow(() =>
            {
                var result = CsvSupport.ToString(null);
            }, "nullを渡しても例外が発生しないべき");
        }

        #endregion

        #region Separator Tests

        [Test]
        [Description("TSVのパースができることをテスト")]
        public void Parse_TsvFormat_ParsesCorrectly()
        {
            var tsv = "id\tname\n1\tTest Name\n";
            var result = CsvParser.Parse<SimpleData>(tsv, "\t");

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].id, Is.EqualTo(1));
            Assert.That(result[0].name, Is.EqualTo("Test Name"));
        }

        [Test]
        [Description("TSVでリストを含むデータがパースできることをテスト")]
        public void Parse_TsvWithList_ParsesCorrectly()
        {
            // 問題: List内のセパレータがハードコードされている
            var tsv = "id\tvalues\n1\t[1,2,3]\n";
            var result = CsvParser.Parse<DataWithIntList>(tsv, "\t");

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].values.Count, Is.EqualTo(3),
                "TSVでもList内のカンマ区切りが正しくパースされるべき");
        }

        #endregion

        #region Special Character Tests

        [Test]
        [Description("カンマを含む文字列がパースできることをテスト")]
        public void Parse_StringWithComma_ParsesCorrectly()
        {
            var csv = @"id,name
1,""Hello, World""
";
            var result = CsvParser.Parse<SimpleData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].name, Is.EqualTo("Hello, World"));
        }

        [Test]
        [Description("改行を含む文字列がパースできることをテスト")]
        public void Parse_StringWithNewline_ParsesCorrectly()
        {
            // 問題: 正規表現が複数行に対応していない
            var csv = "id,name\n1,\"Line1\nLine2\"\n";
            var result = CsvParser.Parse<SimpleData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].name, Is.EqualTo("Line1\nLine2"),
                "改行を含む文字列が正しくパースされるべき");
        }

        [Test]
        [Description("日本語文字列がパースできることをテスト")]
        public void Parse_JapaneseString_ParsesCorrectly()
        {
            var csv = @"id,name
1,""こんにちは世界""
";
            var result = CsvParser.Parse<SimpleData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].name, Is.EqualTo("こんにちは世界"));
        }

        #endregion

        #region Parse Failure Tests

        [Test]
        [Description("不正な数値がエラーなく処理されることをテスト")]
        public void Parse_InvalidNumber_HandlesGracefully()
        {
            // 問題: パース失敗時にサイレントに無視される
            var csv = @"id,name
not_a_number,""Test""
";
            // 現状: 例外なく0が設定される
            // 期待: エラーログが出力されるか、適切な例外が発生する
            var result = CsvParser.Parse<SimpleData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            // 現状はデフォルト値(0)が設定される
            Assert.That(result[0].id, Is.EqualTo(0),
                "パース失敗時はデフォルト値が設定される（現状の動作）");
        }

        #endregion

        #region Vector Tests

        [Test]
        [Description("スペースを含むVector形式がパースできることをテスト")]
        public void ToVector3_WithSpaces_ParsesCorrectly()
        {
            var vectorString = "(1.0, 2.0, 3.0)";
            var result = vectorString.ToVector3();

            Assert.That(result.x, Is.EqualTo(1.0f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(2.0f).Within(0.001f));
            Assert.That(result.z, Is.EqualTo(3.0f).Within(0.001f));
        }

        #endregion
    }
}
