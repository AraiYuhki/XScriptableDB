using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// CompositeIndexAttribute, CompositeIndexData, IndexBuilder (複合インデックス) のテスト。
    /// </summary>
    public class CompositeIndexTests
    {
        #region Test Data Classes

        [CompositeIndex("CategoryPrice", nameof(category), nameof(price))]
        [CompositeIndex("TypeRarity", nameof(type), nameof(rarity))]
        private class TestCompositeRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string category;
            public int price;
            public string type;
            public int rarity;

            public string Name { get; set; }
        }

        [CompositeIndex("CategoryPriceNoDup", nameof(category), nameof(price), AllowDuplicates = false)]
        private class TestUniqueCompositeRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string category;
            public int price;
        }

        [CompositeIndex("ThreeKey", nameof(a), nameof(b), nameof(c))]
        private class TestThreeKeyRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            public int a;
            public int b;
            public int c;
        }

        private class TestNoCompositeRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }
        }

        #endregion

        #region CompositeIndexAttribute Tests

        [Test]
        public void CompositeIndexAttribute_CreatesWithCorrectValues()
        {
            var attr = new CompositeIndexAttribute("TestIndex", "field1", "field2");
            Assert.That(attr.Name, Is.EqualTo("TestIndex"));
            Assert.That(attr.MemberNames.Length, Is.EqualTo(2));
            Assert.That(attr.MemberNames[0], Is.EqualTo("field1"));
            Assert.That(attr.MemberNames[1], Is.EqualTo("field2"));
            Assert.That(attr.AllowDuplicates, Is.True);
        }

        [Test]
        public void CompositeIndexAttribute_AllowDuplicatesFalse_SetsCorrectly()
        {
            var attr = new CompositeIndexAttribute("Unique", "a", "b") { AllowDuplicates = false };
            Assert.That(attr.AllowDuplicates, Is.False);
        }

        [Test]
        public void CompositeIndexAttribute_ThrowsOnNullName()
        {
            Assert.Throws<ArgumentException>(() => new CompositeIndexAttribute(null, "a", "b"));
        }

        [Test]
        public void CompositeIndexAttribute_ThrowsOnEmptyName()
        {
            Assert.Throws<ArgumentException>(() => new CompositeIndexAttribute("", "a", "b"));
        }

        [Test]
        public void CompositeIndexAttribute_ThrowsOnSingleMember()
        {
            Assert.Throws<ArgumentException>(() => new CompositeIndexAttribute("Test", "onlyOne"));
        }

        [Test]
        public void CompositeIndexAttribute_ThrowsOnNullMembers()
        {
            Assert.Throws<ArgumentException>(() => new CompositeIndexAttribute("Test", null));
        }

        #endregion

        #region IndexBuilder.HasCompositeIndices Tests

        [Test]
        public void HasCompositeIndices_WithCompositeIndex_ReturnsTrue()
        {
            Assert.That(IndexBuilder.HasCompositeIndices(typeof(TestCompositeRecord)), Is.True);
        }

        [Test]
        public void HasCompositeIndices_WithoutCompositeIndex_ReturnsFalse()
        {
            Assert.That(IndexBuilder.HasCompositeIndices(typeof(TestNoCompositeRecord)), Is.False);
        }

        #endregion

        #region IndexBuilder.FindCompositeIndexAttributes Tests

        [Test]
        public void FindCompositeIndexAttributes_FindsAllAttributes()
        {
            var attrs = IndexBuilder.FindCompositeIndexAttributes(typeof(TestCompositeRecord));
            Assert.That(attrs.Count, Is.EqualTo(2));

            var names = attrs.Select(a => a.Name).ToList();
            Assert.That(names, Contains.Item("CategoryPrice"));
            Assert.That(names, Contains.Item("TypeRarity"));
        }

        [Test]
        public void FindCompositeIndexAttributes_NoAttributes_ReturnsEmptyList()
        {
            var attrs = IndexBuilder.FindCompositeIndexAttributes(typeof(TestNoCompositeRecord));
            Assert.That(attrs.Count, Is.EqualTo(0));
        }

        #endregion

        #region IndexBuilder.BuildCompositeIndices Tests

        [Test]
        public void BuildCompositeIndices_CreatesCorrectIndices()
        {
            var records = new[]
            {
                new TestCompositeRecord { Id = 1, category = "A", price = 100, type = "X", rarity = 1 },
                new TestCompositeRecord { Id = 2, category = "B", price = 200, type = "X", rarity = 2 },
                new TestCompositeRecord { Id = 3, category = "A", price = 100, type = "Y", rarity = 1 }
            };

            var container = IndexBuilder.BuildCompositeIndices(records);

            Assert.That(container.Count, Is.EqualTo(2));
            Assert.That(container.GetIndex("CategoryPrice"), Is.Not.Null);
            Assert.That(container.GetIndex("TypeRarity"), Is.Not.Null);
        }

        [Test]
        public void BuildCompositeIndices_GroupsDuplicateKeys()
        {
            var records = new[]
            {
                new TestCompositeRecord { Id = 1, category = "A", price = 100 },
                new TestCompositeRecord { Id = 2, category = "A", price = 100 },
                new TestCompositeRecord { Id = 3, category = "A", price = 200 }
            };

            var container = IndexBuilder.BuildCompositeIndices(records);
            var index = container.GetIndex("CategoryPrice");

            // ("A", 100) should have 2 records
            var indices = index.FindByKeys("A", 100);
            Assert.That(indices.Length, Is.EqualTo(2));
            Assert.That(indices, Contains.Item(0));
            Assert.That(indices, Contains.Item(1));

            // ("A", 200) should have 1 record
            indices = index.FindByKeys("A", 200);
            Assert.That(indices.Length, Is.EqualTo(1));
            Assert.That(indices[0], Is.EqualTo(2));
        }

        [Test]
        public void BuildCompositeIndices_ThreeKeys_WorksCorrectly()
        {
            var records = new[]
            {
                new TestThreeKeyRecord { Id = 1, a = 1, b = 2, c = 3 },
                new TestThreeKeyRecord { Id = 2, a = 1, b = 2, c = 3 },
                new TestThreeKeyRecord { Id = 3, a = 1, b = 2, c = 4 }
            };

            var container = IndexBuilder.BuildCompositeIndices(records);
            var index = container.GetIndex("ThreeKey");

            var indices = index.FindByKeys(1, 2, 3);
            Assert.That(indices.Length, Is.EqualTo(2));

            indices = index.FindByKeys(1, 2, 4);
            Assert.That(indices.Length, Is.EqualTo(1));
        }

        [Test]
        public void BuildCompositeIndices_EmptyRecords_ReturnsEmptyContainer()
        {
            var records = Array.Empty<TestCompositeRecord>();
            var container = IndexBuilder.BuildCompositeIndices(records);

            Assert.That(container.Count, Is.EqualTo(2)); // Creates index definitions
            Assert.That(container.GetIndex("CategoryPrice").Count, Is.EqualTo(0));
        }

        [Test]
        public void BuildCompositeIndex_SpecificIndex_ReturnsCorrectIndex()
        {
            var records = new[]
            {
                new TestCompositeRecord { Id = 1, category = "A", price = 100 }
            };

            var index = IndexBuilder.BuildCompositeIndex(records, "CategoryPrice");

            Assert.That(index, Is.Not.Null);
            Assert.That(index.IndexName, Is.EqualTo("CategoryPrice"));
            Assert.That(index.MemberNames.Count, Is.EqualTo(2));
        }

        [Test]
        public void BuildCompositeIndex_NonExistentIndex_ReturnsNull()
        {
            var records = new[]
            {
                new TestCompositeRecord { Id = 1, category = "A", price = 100 }
            };

            var index = IndexBuilder.BuildCompositeIndex(records, "NonExistent");

            Assert.That(index, Is.Null);
        }

        #endregion

        #region CompositeIndexData Tests

        [Test]
        public void CompositeIndexData_AddEntry_AddsCorrectly()
        {
            var members = new List<(string, Type)>
            {
                ("field1", typeof(string)),
                ("field2", typeof(int))
            };
            var indexData = new CompositeIndexData("TestIndex", members);

            var compositeString = CompositeIndexData.ComputeCompositeString("A", 100);
            indexData.AddEntry(
                CompositeIndexData.ComputeCompositeHash("A", 100),
                compositeString,
                new[] { "A", "100" },
                new[] { 0, 1 });

            Assert.That(indexData.Count, Is.EqualTo(1));
            Assert.That(indexData.KeyCount, Is.EqualTo(2));
        }

        [Test]
        public void CompositeIndexData_FindByKeys_ReturnsCorrectIndices()
        {
            var members = new List<(string, Type)>
            {
                ("category", typeof(string)),
                ("price", typeof(int))
            };
            var indexData = new CompositeIndexData("TestIndex", members);

            indexData.AddEntry(
                CompositeIndexData.ComputeCompositeHash("A", 100),
                CompositeIndexData.ComputeCompositeString("A", 100),
                new[] { "A", "100" },
                new[] { 0, 2 });

            indexData.AddEntry(
                CompositeIndexData.ComputeCompositeHash("B", 200),
                CompositeIndexData.ComputeCompositeString("B", 200),
                new[] { "B", "200" },
                new[] { 1 });

            var aIndices = indexData.FindByKeys("A", 100);
            Assert.That(aIndices.Length, Is.EqualTo(2));
            Assert.That(aIndices, Contains.Item(0));
            Assert.That(aIndices, Contains.Item(2));

            var bIndices = indexData.FindByKeys("B", 200);
            Assert.That(bIndices.Length, Is.EqualTo(1));
            Assert.That(bIndices[0], Is.EqualTo(1));
        }

        [Test]
        public void CompositeIndexData_FindByKeys_NotFound_ReturnsEmptyArray()
        {
            var members = new List<(string, Type)>
            {
                ("category", typeof(string)),
                ("price", typeof(int))
            };
            var indexData = new CompositeIndexData("TestIndex", members);

            indexData.AddEntry(
                CompositeIndexData.ComputeCompositeHash("A", 100),
                CompositeIndexData.ComputeCompositeString("A", 100),
                new[] { "A", "100" },
                new[] { 0 });

            var result = indexData.FindByKeys("C", 999);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void CompositeIndexData_FindByKeys_WrongKeyCount_ReturnsEmptyArray()
        {
            var members = new List<(string, Type)>
            {
                ("category", typeof(string)),
                ("price", typeof(int))
            };
            var indexData = new CompositeIndexData("TestIndex", members);

            indexData.AddEntry(
                CompositeIndexData.ComputeCompositeHash("A", 100),
                CompositeIndexData.ComputeCompositeString("A", 100),
                new[] { "A", "100" },
                new[] { 0 });

            // Wrong number of keys
            var result = indexData.FindByKeys("A");
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void CompositeIndexData_FindByString_ReturnsCorrectIndices()
        {
            var members = new List<(string, Type)>
            {
                ("a", typeof(int)),
                ("b", typeof(int))
            };
            var indexData = new CompositeIndexData("TestIndex", members);

            var compositeString = CompositeIndexData.ComputeCompositeString(1, 2);
            indexData.AddEntry(123, compositeString, new[] { "1", "2" }, new[] { 0, 1 });

            var result = indexData.FindByString(compositeString);
            Assert.That(result.Length, Is.EqualTo(2));
        }

        [Test]
        public void CompositeIndexData_Clear_RemovesAllEntries()
        {
            var members = new List<(string, Type)>
            {
                ("a", typeof(int)),
                ("b", typeof(int))
            };
            var indexData = new CompositeIndexData("TestIndex", members);

            indexData.AddEntry(
                CompositeIndexData.ComputeCompositeHash(1, 2),
                CompositeIndexData.ComputeCompositeString(1, 2),
                new[] { "1", "2" },
                new[] { 0 });

            indexData.Clear();

            Assert.That(indexData.Count, Is.EqualTo(0));
            Assert.That(indexData.FindByKeys(1, 2).Length, Is.EqualTo(0));
        }

        [Test]
        public void CompositeIndexData_GetAllEntries_ReturnsAllEntries()
        {
            var members = new List<(string, Type)>
            {
                ("a", typeof(int)),
                ("b", typeof(int))
            };
            var indexData = new CompositeIndexData("TestIndex", members);

            indexData.AddEntry(
                CompositeIndexData.ComputeCompositeHash(1, 1),
                CompositeIndexData.ComputeCompositeString(1, 1),
                new[] { "1", "1" },
                new[] { 0 });

            indexData.AddEntry(
                CompositeIndexData.ComputeCompositeHash(1, 2),
                CompositeIndexData.ComputeCompositeString(1, 2),
                new[] { "1", "2" },
                new[] { 1, 2 });

            var entries = indexData.GetAllEntries();
            Assert.That(entries.Count, Is.EqualTo(2));
        }

        #endregion

        #region CompositeIndexData Static Methods Tests

        [Test]
        public void ComputeCompositeHash_DifferentKeys_DifferentHash()
        {
            var hash1 = CompositeIndexData.ComputeCompositeHash("A", 100);
            var hash2 = CompositeIndexData.ComputeCompositeHash("A", 200);
            var hash3 = CompositeIndexData.ComputeCompositeHash("B", 100);

            Assert.That(hash1, Is.Not.EqualTo(hash2));
            Assert.That(hash1, Is.Not.EqualTo(hash3));
            Assert.That(hash2, Is.Not.EqualTo(hash3));
        }

        [Test]
        public void ComputeCompositeHash_SameKeys_SameHash()
        {
            var hash1 = CompositeIndexData.ComputeCompositeHash("A", 100);
            var hash2 = CompositeIndexData.ComputeCompositeHash("A", 100);

            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void ComputeCompositeHash_IsDeterministic()
        {
            // 同じ入力に対して常に同じハッシュ値を返すことを確認
            var hash1 = CompositeIndexData.ComputeCompositeHash("Test", 123, "Value");
            var hash2 = CompositeIndexData.ComputeCompositeHash("Test", 123, "Value");
            var hash3 = CompositeIndexData.ComputeCompositeHash("Test", 123, "Value");

            Assert.That(hash1, Is.EqualTo(hash2));
            Assert.That(hash2, Is.EqualTo(hash3));
        }

        [Test]
        public void ComputeCompositeString_UsesUnitSeparator()
        {
            var str = CompositeIndexData.ComputeCompositeString("A", 100, "B");
            // Unit Separator (ASCII 31) を区切り文字として使用
            var expected = "A" + (char)0x1F + "100" + (char)0x1F + "B";
            Assert.That(str, Is.EqualTo(expected));
        }

        [Test]
        public void ComputeCompositeString_HandlesSpecialCharacters()
        {
            // パイプ文字を含む値でも衝突しない
            var str1 = CompositeIndexData.ComputeCompositeString("A|B", 100);
            var str2 = CompositeIndexData.ComputeCompositeString("A", "B|100");

            Assert.That(str1, Is.Not.EqualTo(str2));
        }

        [Test]
        public void ComputeCompositeString_NullValue_UsesPlaceholder()
        {
            var str = CompositeIndexData.ComputeCompositeString("A", null, "B");
            // Null placeholder: "\x00NULL\x00"
            var nullPlaceholder = (char)0x00 + "NULL" + (char)0x00;
            Assert.That(str, Does.Contain(nullPlaceholder));
        }

        [Test]
        public void ComputeReadableString_FormatsCorrectly()
        {
            var str = CompositeIndexData.ComputeReadableString("A", 100, "B");
            Assert.That(str, Is.EqualTo("A|100|B"));
        }

        [Test]
        public void ComputeReadableString_NullValue_ShowsNull()
        {
            var str = CompositeIndexData.ComputeReadableString("A", null, "B");
            Assert.That(str, Is.EqualTo("A|null|B"));
        }

        [Test]
        public void ComputeCompositeHash_EmptyKeys_ReturnsZero()
        {
            var hash = CompositeIndexData.ComputeCompositeHash();
            Assert.That(hash, Is.EqualTo(0));
        }

        [Test]
        public void ComputeCompositeString_EmptyKeys_ReturnsEmpty()
        {
            var str = CompositeIndexData.ComputeCompositeString();
            Assert.That(str, Is.EqualTo(string.Empty));
        }

        #endregion

        #region CompositeIndexContainer Tests

        [Test]
        public void CompositeIndexContainer_SetIndex_AddsNewIndex()
        {
            var container = new CompositeIndexContainer();
            var members = new List<(string, Type)>
            {
                ("a", typeof(int)),
                ("b", typeof(int))
            };
            var indexData = new CompositeIndexData("TestIndex", members);

            container.SetIndex(indexData);

            Assert.That(container.Count, Is.EqualTo(1));
            Assert.That(container.GetIndex("TestIndex"), Is.SameAs(indexData));
        }

        [Test]
        public void CompositeIndexContainer_SetIndex_UpdatesExistingIndex()
        {
            var container = new CompositeIndexContainer();
            var members = new List<(string, Type)>
            {
                ("a", typeof(int)),
                ("b", typeof(int))
            };
            var indexData1 = new CompositeIndexData("TestIndex", members);
            var indexData2 = new CompositeIndexData("TestIndex", members);

            container.SetIndex(indexData1);
            container.SetIndex(indexData2);

            Assert.That(container.Count, Is.EqualTo(1));
            Assert.That(container.GetIndex("TestIndex"), Is.SameAs(indexData2));
        }

        [Test]
        public void CompositeIndexContainer_GetIndex_NotFound_ReturnsNull()
        {
            var container = new CompositeIndexContainer();
            Assert.That(container.GetIndex("NonExistent"), Is.Null);
        }

        [Test]
        public void CompositeIndexContainer_RemoveIndex_RemovesCorrectly()
        {
            var container = new CompositeIndexContainer();
            var members = new List<(string, Type)>
            {
                ("a", typeof(int)),
                ("b", typeof(int))
            };
            container.SetIndex(new CompositeIndexData("Index1", members));
            container.SetIndex(new CompositeIndexData("Index2", members));

            var removed = container.RemoveIndex("Index1");

            Assert.That(removed, Is.True);
            Assert.That(container.Count, Is.EqualTo(1));
            Assert.That(container.GetIndex("Index1"), Is.Null);
            Assert.That(container.GetIndex("Index2"), Is.Not.Null);
        }

        [Test]
        public void CompositeIndexContainer_RemoveIndex_NotFound_ReturnsFalse()
        {
            var container = new CompositeIndexContainer();
            var removed = container.RemoveIndex("NonExistent");
            Assert.That(removed, Is.False);
        }

        [Test]
        public void CompositeIndexContainer_Clear_RemovesAllIndices()
        {
            var container = new CompositeIndexContainer();
            var members = new List<(string, Type)>
            {
                ("a", typeof(int)),
                ("b", typeof(int))
            };
            container.SetIndex(new CompositeIndexData("Index1", members));
            container.SetIndex(new CompositeIndexData("Index2", members));

            container.Clear();

            Assert.That(container.Count, Is.EqualTo(0));
        }

        [Test]
        public void CompositeIndexContainer_GetAllIndices_ReturnsAll()
        {
            var container = new CompositeIndexContainer();
            var members = new List<(string, Type)>
            {
                ("a", typeof(int)),
                ("b", typeof(int))
            };
            container.SetIndex(new CompositeIndexData("Index1", members));
            container.SetIndex(new CompositeIndexData("Index2", members));

            var indices = container.GetAllIndices();
            Assert.That(indices.Count, Is.EqualTo(2));
        }

        #endregion

        #region Integration Tests

        [Test]
        public void CompositeIndex_EndToEnd_SearchWorks()
        {
            // 完全な統合テスト：ビルド → 検索
            var records = new[]
            {
                new TestCompositeRecord { Id = 1, category = "Weapon", price = 100, type = "Sword", rarity = 1 },
                new TestCompositeRecord { Id = 2, category = "Weapon", price = 100, type = "Sword", rarity = 2 },
                new TestCompositeRecord { Id = 3, category = "Armor", price = 200, type = "Shield", rarity = 1 },
                new TestCompositeRecord { Id = 4, category = "Weapon", price = 300, type = "Axe", rarity = 3 }
            };

            var container = IndexBuilder.BuildCompositeIndices(records);

            // CategoryPrice インデックスで検索
            var categoryPriceIndex = container.GetIndex("CategoryPrice");
            var weaponPrice100 = categoryPriceIndex.FindByKeys("Weapon", 100);
            Assert.That(weaponPrice100.Length, Is.EqualTo(2));
            Assert.That(weaponPrice100, Contains.Item(0));
            Assert.That(weaponPrice100, Contains.Item(1));

            // TypeRarity インデックスで検索
            var typeRarityIndex = container.GetIndex("TypeRarity");
            var swordRarity1 = typeRarityIndex.FindByKeys("Sword", 1);
            Assert.That(swordRarity1.Length, Is.EqualTo(1));
            Assert.That(swordRarity1[0], Is.EqualTo(0));
        }

        #endregion
    }
}
