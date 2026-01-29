using NUnit.Framework;
using System;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Tests
{
    public class CsvParserEdgeCaseTests
    {
        [Serializable]
        private class SimpleData : CsvData
        {
            [CsvColumn("id")]
            public int id;

            [CsvColumn("name")]
            public string name;
        }

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

        [Test]
        [Description("文字列内の引用符がエスケープされることをテスト")]
        public void ToCsv_StringWithQuotes_EscapesQuotes()
        {
            var value = "He said \"Hello\"";
            var escaped = value.ToCsv();
            Assert.That(escaped, Is.EqualTo("\"He said \"\"Hello\"\"\""));
        }

        [Test]
        [Description("エスケープされた引用符がパースできることをテスト")]
        public void FromCsv_EscapedQuotes_UnescapesCorrectly()
        {
            var escaped = "\"He said \"\"Hello\"\"\"";
            var result = escaped.FromCsv();
            Assert.That(result, Is.EqualTo("He said \"Hello\""));
        }

        [Test]
        [Description("引用符を含む文字列の往復変換テスト")]
        public void CsvRoundTrip_StringWithQuotes_PreservesValue()
        {
            var csv = "id,name\n1,\"He said \"\"Hello\"\"\"\n";
            var result = CsvParser.Parse<SimpleData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].name, Is.EqualTo("He said \"Hello\""));
        }

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
        [Description("カンマを含む文字列がパースできることをテスト")]
        public void Parse_StringWithComma_ParsesCorrectly()
        {
            var csv = "id,name\n1,\"Hello, World\"\n";
            var result = CsvParser.Parse<SimpleData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].name, Is.EqualTo("Hello, World"));
        }

        [Test]
        [Description("改行を含む文字列がパースできることをテスト")]
        public void Parse_StringWithNewline_ParsesCorrectly()
        {
            var csv = "id,name\n1,\"Line1\nLine2\"\n";
            var result = CsvParser.Parse<SimpleData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].name, Is.EqualTo("Line1\nLine2"));
        }

        [Test]
        [Description("日本語文字列がパースできることをテスト")]
        public void Parse_JapaneseString_ParsesCorrectly()
        {
            var csv = "id,name\n1,\"こんにちは世界\"\n";
            var result = CsvParser.Parse<SimpleData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].name, Is.EqualTo("こんにちは世界"));
        }

        [Test]
        [Description("不正な数値がエラーなく処理されることをテスト")]
        public void Parse_InvalidNumber_HandlesGracefully()
        {
            var csv = "id,name\nnot_a_number,\"Test\"\n";
            var result = CsvParser.Parse<SimpleData>(csv);

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].id, Is.EqualTo(0));
        }
    }
}
