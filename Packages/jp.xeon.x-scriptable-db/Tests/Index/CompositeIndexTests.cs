using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// Tests for composite index functionality.
    /// Tests for the implementation combining the best aspects of PR#3 and PR#4.
    /// </summary>
    public class CompositeIndexTests
    {
        #region Test Data Classes

        private class TestSingleKeyRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey]
            public string Category { get; set; }
        }

        private class TestCompositeRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("CategoryPrice", 0)]
            public string Category { get; set; }

            [SecondaryKey("CategoryPrice", 1)]
            public int Price { get; set; }

            [SecondaryKey("TypeRarity", 0)]
            public string Type { get; set; }

            [SecondaryKey("TypeRarity", 1)]
            public int Rarity { get; set; }
        }

        private class TestThreeKeyRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("ThreeKey", 0)]
            public int A { get; set; }

            [SecondaryKey("ThreeKey", 1)]
            public int B { get; set; }

            [SecondaryKey("ThreeKey", 2)]
            public int C { get; set; }
        }

        private class TestUniqueCompositeRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("UniqueKey", 0, AllowDuplicates = false)]
            public string Category { get; set; }

            [SecondaryKey("UniqueKey", 1, AllowDuplicates = false)]
            public int Price { get; set; }
        }

        #endregion

        #region SecondaryKeyAttribute Tests

        [Test]
        public void SecondaryKeyAttribute_DefaultValues_AreCorrect()
        {
            var attr = new SecondaryKeyAttribute();
            Assert.That(attr.Name, Is.Null);
            Assert.That(attr.Order, Is.EqualTo(0));
            Assert.That(attr.AllowDuplicates, Is.True);
        }

        [Test]
        public void SecondaryKeyAttribute_WithName_SetsCorrectly()
        {
            var attr = new SecondaryKeyAttribute("TestIndex");
            Assert.That(attr.Name, Is.EqualTo("TestIndex"));
            Assert.That(attr.Order, Is.EqualTo(0));
        }

        [Test]
        public void SecondaryKeyAttribute_WithNameAndOrder_SetsCorrectly()
        {
            var attr = new SecondaryKeyAttribute("TestIndex", 2);
            Assert.That(attr.Name, Is.EqualTo("TestIndex"));
            Assert.That(attr.Order, Is.EqualTo(2));
        }

        [Test]
        public void SecondaryKeyAttribute_AllowDuplicatesFalse_SetsCorrectly()
        {
            var attr = new SecondaryKeyAttribute("Unique") { AllowDuplicates = false };
            Assert.That(attr.AllowDuplicates, Is.False);
        }

        #endregion

        #region CompositeKeyHelper Tests

        [Test]
        public void ComputeCompositeString_SingleValue_ReturnsCorrectString()
        {
            var result = CompositeKeyHelper.ComputeCompositeString("A");
            Assert.That(result, Is.EqualTo("A"));
        }

        [Test]
        public void ComputeCompositeString_MultipleValues_UsesUnitSeparator()
        {
            var result = CompositeKeyHelper.ComputeCompositeString("A", 100, "B");
            var expected = "A" + (char)0x1F + "100" + (char)0x1F + "B";
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ComputeCompositeString_NullValue_UsesPlaceholder()
        {
            var result = CompositeKeyHelper.ComputeCompositeString("A", null, "B");
            Assert.That(result, Does.Contain("\x00NULL\x00"));
        }

        [Test]
        public void ComputeCompositeString_EmptyArray_ReturnsEmpty()
        {
            var result = CompositeKeyHelper.ComputeCompositeString();
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void ComputeCompositeString_HandlesSpecialCharacters()
        {
            var str1 = CompositeKeyHelper.ComputeCompositeString("A|B", 100);
            var str2 = CompositeKeyHelper.ComputeCompositeString("A", "B|100");
            Assert.That(str1, Is.Not.EqualTo(str2));
        }

        [Test]
        public void ComputeReadableString_FormatsCorrectly()
        {
            var result = CompositeKeyHelper.ComputeReadableString("A", 100, "B");
            Assert.That(result, Is.EqualTo("A|100|B"));
        }

        [Test]
        public void ComputeReadableString_NullValue_ShowsNull()
        {
            var result = CompositeKeyHelper.ComputeReadableString("A", null, "B");
            Assert.That(result, Is.EqualTo("A|null|B"));
        }

        [Test]
        public void GetDeterministicHashCode_SameInput_SameOutput()
        {
            var hash1 = CompositeKeyHelper.GetDeterministicHashCode("TestString");
            var hash2 = CompositeKeyHelper.GetDeterministicHashCode("TestString");
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void GetDeterministicHashCode_DifferentInput_DifferentOutput()
        {
            var hash1 = CompositeKeyHelper.GetDeterministicHashCode("String1");
            var hash2 = CompositeKeyHelper.GetDeterministicHashCode("String2");
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void GetDeterministicHashCode_EmptyString_ReturnsZero()
        {
            var hash = CompositeKeyHelper.GetDeterministicHashCode("");
            Assert.That(hash, Is.EqualTo(0));
        }

        [Test]
        public void GetDeterministicHashCode_NullString_ReturnsZero()
        {
            var hash = CompositeKeyHelper.GetDeterministicHashCode(null);
            Assert.That(hash, Is.EqualTo(0));
        }

        #endregion

        #region IndexBuilder Tests

        [Test]
        public void FindSecondaryKeyMembers_SingleKey_FindsCorrectly()
        {
            var members = IndexBuilder.FindSecondaryKeyMembers(typeof(TestSingleKeyRecord));
            Assert.That(members.Count, Is.EqualTo(1));
            Assert.That(members[0].member.Name, Is.EqualTo("Category"));
        }

        [Test]
        public void FindSecondaryKeyMembers_CompositeKey_FindsAllMembers()
        {
            var members = IndexBuilder.FindSecondaryKeyMembers(typeof(TestCompositeRecord));
            Assert.That(members.Count, Is.EqualTo(4));
        }

        [Test]
        public void GroupSecondaryKeyMembers_GroupsByIndexName()
        {
            var groups = IndexBuilder.GroupSecondaryKeyMembers(typeof(TestCompositeRecord));
            Assert.That(groups.Count, Is.EqualTo(2));
            Assert.That(groups.ContainsKey("CategoryPrice"), Is.True);
            Assert.That(groups.ContainsKey("TypeRarity"), Is.True);
            Assert.That(groups["CategoryPrice"].Count, Is.EqualTo(2));
            Assert.That(groups["TypeRarity"].Count, Is.EqualTo(2));
        }

        [Test]
        public void GroupSecondaryKeyMembers_OrdersByOrder()
        {
            var groups = IndexBuilder.GroupSecondaryKeyMembers(typeof(TestCompositeRecord));
            var categoryPriceMembers = groups["CategoryPrice"];
            Assert.That(categoryPriceMembers[0].member.Name, Is.EqualTo("Category"));
            Assert.That(categoryPriceMembers[1].member.Name, Is.EqualTo("Price"));
        }

        [Test]
        public void IsCompositeIndex_CompositeIndex_ReturnsTrue()
        {
            var result = IndexBuilder.IsCompositeIndex("CategoryPrice", typeof(TestCompositeRecord));
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsCompositeIndex_SingleIndex_ReturnsFalse()
        {
            var result = IndexBuilder.IsCompositeIndex("Category", typeof(TestSingleKeyRecord));
            Assert.That(result, Is.False);
        }

        [Test]
        public void BuildIndices_SingleKey_BuildsCorrectly()
        {
            var records = new[]
            {
                new TestSingleKeyRecord { Id = 1, Category = "A" },
                new TestSingleKeyRecord { Id = 2, Category = "B" },
                new TestSingleKeyRecord { Id = 3, Category = "A" }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("Category");

            Assert.That(index, Is.Not.Null);
            Assert.That(index.FindByString("A").Length, Is.EqualTo(2));
            Assert.That(index.FindByString("B").Length, Is.EqualTo(1));
        }

        [Test]
        public void BuildIndices_CompositeKey_BuildsCorrectly()
        {
            var records = new[]
            {
                new TestCompositeRecord { Id = 1, Category = "A", Price = 100, Type = "X", Rarity = 1 },
                new TestCompositeRecord { Id = 2, Category = "A", Price = 100, Type = "Y", Rarity = 2 },
                new TestCompositeRecord { Id = 3, Category = "A", Price = 200, Type = "X", Rarity = 1 }
            };

            var container = IndexBuilder.BuildIndices(records);

            Assert.That(container.Count, Is.EqualTo(2));
            Assert.That(container.GetIndex("CategoryPrice"), Is.Not.Null);
            Assert.That(container.GetIndex("TypeRarity"), Is.Not.Null);
        }

        [Test]
        public void BuildIndices_CompositeKey_GroupsDuplicates()
        {
            var records = new[]
            {
                new TestCompositeRecord { Id = 1, Category = "A", Price = 100 },
                new TestCompositeRecord { Id = 2, Category = "A", Price = 100 },
                new TestCompositeRecord { Id = 3, Category = "A", Price = 200 }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("CategoryPrice");

            var compositeKey = CompositeKeyHelper.ComputeCompositeString("A", 100);
            var indices = index.FindByString(compositeKey);
            Assert.That(indices.Length, Is.EqualTo(2));
            Assert.That(indices, Contains.Item(0));
            Assert.That(indices, Contains.Item(1));
        }

        [Test]
        public void BuildIndices_ThreeKeys_BuildsCorrectly()
        {
            var records = new[]
            {
                new TestThreeKeyRecord { Id = 1, A = 1, B = 2, C = 3 },
                new TestThreeKeyRecord { Id = 2, A = 1, B = 2, C = 3 },
                new TestThreeKeyRecord { Id = 3, A = 1, B = 2, C = 4 }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("ThreeKey");

            var key123 = CompositeKeyHelper.ComputeCompositeString(1, 2, 3);
            var key124 = CompositeKeyHelper.ComputeCompositeString(1, 2, 4);

            Assert.That(index.FindByString(key123).Length, Is.EqualTo(2));
            Assert.That(index.FindByString(key124).Length, Is.EqualTo(1));
        }

        [Test]
        public void BuildIndices_EmptyRecords_ReturnsEmptyContainer()
        {
            var records = Array.Empty<TestCompositeRecord>();
            var container = IndexBuilder.BuildIndices(records);

            Assert.That(container.Count, Is.EqualTo(2));
            Assert.That(container.GetIndex("CategoryPrice").Count, Is.EqualTo(0));
        }

        [Test]
        public void BuildIndices_NullValues_SkipsRecord()
        {
            var records = new[]
            {
                new TestCompositeRecord { Id = 1, Category = "A", Price = 100 },
                new TestCompositeRecord { Id = 2, Category = null, Price = 200 },
                new TestCompositeRecord { Id = 3, Category = "B", Price = 300 }
            };

            var container = IndexBuilder.BuildIndices(records);
            var index = container.GetIndex("CategoryPrice");

            Assert.That(index.Count, Is.EqualTo(2));
        }

        #endregion

        #region IndexData Tests

        [Test]
        public void IndexData_FindByString_ReturnsCorrectIndices()
        {
            var indexData = new IndexData("Test", typeof(string));
            indexData.AddEntry(0, "key1", new[] { 0, 1 });
            indexData.AddEntry(0, "key2", new[] { 2 });

            Assert.That(indexData.FindByString("key1"), Is.EqualTo(new[] { 0, 1 }));
            Assert.That(indexData.FindByString("key2"), Is.EqualTo(new[] { 2 }));
        }

        [Test]
        public void IndexData_FindByString_NotFound_ReturnsEmpty()
        {
            var indexData = new IndexData("Test", typeof(string));
            indexData.AddEntry(0, "key1", new[] { 0 });

            var result = indexData.FindByString("nonexistent");
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void IndexData_FindByKey_UsesStringKey()
        {
            var indexData = new IndexData("Test", typeof(int));
            indexData.AddEntry(0, "100", new[] { 0 });
            indexData.AddEntry(0, "200", new[] { 1 });

            Assert.That(indexData.FindByKey(100), Is.EqualTo(new[] { 0 }));
            Assert.That(indexData.FindByKey(200), Is.EqualTo(new[] { 1 }));
        }

        [Test]
        public void IndexData_Clear_RemovesAllEntries()
        {
            var indexData = new IndexData("Test", typeof(string));
            indexData.AddEntry(0, "key1", new[] { 0 });
            indexData.AddEntry(0, "key2", new[] { 1 });

            indexData.Clear();

            Assert.That(indexData.Count, Is.EqualTo(0));
            Assert.That(indexData.FindByString("key1"), Is.Empty);
        }

        #endregion

        #region Hash Collision Tests

        [Test]
        public void StringKeySearch_NoHashCollision()
        {
            var records = new List<TestCompositeRecord>();
            for (var i = 0; i < 1000; i++)
            {
                records.Add(new TestCompositeRecord
                {
                    Id = i,
                    Category = $"Cat{i % 10}",
                    Price = i * 10
                });
            }

            var container = IndexBuilder.BuildIndices(records.ToArray());
            var index = container.GetIndex("CategoryPrice");

            for (var i = 0; i < 1000; i++)
            {
                var key = CompositeKeyHelper.ComputeCompositeString($"Cat{i % 10}", i * 10);
                var found = index.FindByString(key);
                Assert.That(found.Length, Is.EqualTo(1));
                Assert.That(found[0], Is.EqualTo(i));
            }
        }

        [Test]
        public void StringKeySearch_UniqueKeys_NoCollision()
        {
            var key1 = CompositeKeyHelper.ComputeCompositeString("abc", 123);
            var key2 = CompositeKeyHelper.ComputeCompositeString("ab", "c123");

            Assert.That(key1, Is.Not.EqualTo(key2));
        }

        [Test]
        public void CompositeKeyHelper_EscapesDelimiterCharacter()
        {
            var delimiter = '\x1F';
            var key1 = CompositeKeyHelper.ComputeCompositeString($"A{delimiter}B", 100);
            var key2 = CompositeKeyHelper.ComputeCompositeString("A", $"B{delimiter}100");

            Assert.That(key1, Is.Not.EqualTo(key2));
        }

        [Test]
        public void CompositeKeyHelper_EscapesEscapeCharacter()
        {
            var escapeChar = '\x1E';
            var key1 = CompositeKeyHelper.ComputeCompositeString($"A{escapeChar}B", 100);
            var key2 = CompositeKeyHelper.ComputeCompositeString("A", $"B{escapeChar}100");

            Assert.That(key1, Is.Not.EqualTo(key2));
        }

        [Test]
        public void CompositeKeyHelper_InvariantCulture_FloatValues()
        {
            var key1 = CompositeKeyHelper.ComputeCompositeString("Test", 1.5f);
            Assert.That(key1, Does.Contain("1.5"));
            Assert.That(key1, Does.Not.Contain("1,5"));
        }

        [Test]
        public void CompositeKeyHelper_InvariantCulture_DoubleValues()
        {
            var key1 = CompositeKeyHelper.ComputeCompositeString("Test", 1.5d);
            Assert.That(key1, Does.Contain("1.5"));
            Assert.That(key1, Does.Not.Contain("1,5"));
        }

        [Test]
        public void CompositeKeyHelper_InvariantCulture_DecimalValues()
        {
            var key1 = CompositeKeyHelper.ComputeCompositeString("Test", 1.5m);
            Assert.That(key1, Does.Contain("1.5"));
            Assert.That(key1, Does.Not.Contain("1,5"));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void EndToEnd_CompositeIndex_WorksCorrectly()
        {
            var records = new[]
            {
                new TestCompositeRecord { Id = 1, Category = "Weapon", Price = 100, Type = "Sword", Rarity = 1 },
                new TestCompositeRecord { Id = 2, Category = "Weapon", Price = 100, Type = "Sword", Rarity = 2 },
                new TestCompositeRecord { Id = 3, Category = "Armor", Price = 200, Type = "Shield", Rarity = 1 },
                new TestCompositeRecord { Id = 4, Category = "Weapon", Price = 300, Type = "Axe", Rarity = 3 }
            };

            var container = IndexBuilder.BuildIndices(records);

            var categoryPriceIndex = container.GetIndex("CategoryPrice");
            var weaponPrice100Key = CompositeKeyHelper.ComputeCompositeString("Weapon", 100);
            var weaponPrice100 = categoryPriceIndex.FindByString(weaponPrice100Key);
            Assert.That(weaponPrice100.Length, Is.EqualTo(2));
            Assert.That(weaponPrice100, Contains.Item(0));
            Assert.That(weaponPrice100, Contains.Item(1));

            var typeRarityIndex = container.GetIndex("TypeRarity");
            var swordRarity1Key = CompositeKeyHelper.ComputeCompositeString("Sword", 1);
            var swordRarity1 = typeRarityIndex.FindByString(swordRarity1Key);
            Assert.That(swordRarity1.Length, Is.EqualTo(1));
            Assert.That(swordRarity1[0], Is.EqualTo(0));
        }

        #endregion
    }
}
