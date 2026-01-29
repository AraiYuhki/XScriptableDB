using NUnit.Framework;
using System;
using System.Linq;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// IndexBuilder, IndexData, SecondaryKeyAttribute のテスト。
    /// </summary>
    public class IndexTests
    {
        #region Test Data Classes

        private class TestRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("Category")]
            public string Category { get; set; }

            [SecondaryKey]
            public int GroupId { get; set; }

            public string Name { get; set; }
        }

        private class RecordWithUniqueSecondaryKey
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("UniqueCode", AllowDuplicates = false)]
            public string UniqueCode { get; set; }
        }

        private class RecordWithPropertySecondaryKey
        {
            [PrimaryKey]
            public int Id { get; set; }

            private string _category;

            [SecondaryKey("CategoryProp")]
            public string Category
            {
                get => _category;
                set => _category = value;
            }
        }

        private class RecordWithoutSecondaryKey
        {
            [PrimaryKey]
            public int Id { get; set; }

            public string Name { get; set; }
        }

        private enum ItemType
        {
            Weapon,
            Armor,
            Consumable
        }

        private class RecordWithEnumSecondaryKey
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("Type")]
            public ItemType Type { get; set; }
        }

        #endregion

        #region SecondaryKeyAttribute Tests

        [Test]
        public void SecondaryKeyAttribute_DefaultName_UsesMemberName()
        {
            var attr = new SecondaryKeyAttribute();
            Assert.That(attr.Name, Is.Null);
            Assert.That(attr.AllowDuplicates, Is.True);
        }

        [Test]
        public void SecondaryKeyAttribute_WithName_UsesSpecifiedName()
        {
            var attr = new SecondaryKeyAttribute("CustomName");
            Assert.That(attr.Name, Is.EqualTo("CustomName"));
            Assert.That(attr.AllowDuplicates, Is.True);
        }

        [Test]
        public void SecondaryKeyAttribute_AllowDuplicatesFalse_SetsCorrectly()
        {
            var attr = new SecondaryKeyAttribute("Unique") { AllowDuplicates = false };
            Assert.That(attr.AllowDuplicates, Is.False);
        }

        #endregion

        #region IndexBuilder.HasSecondaryKeys Tests

        [Test]
        public void HasSecondaryKeys_WithSecondaryKey_ReturnsTrue()
        {
            Assert.That(IndexBuilder.HasSecondaryKeys(typeof(TestRecord)), Is.True);
        }

        [Test]
        public void HasSecondaryKeys_WithoutSecondaryKey_ReturnsFalse()
        {
            Assert.That(IndexBuilder.HasSecondaryKeys(typeof(RecordWithoutSecondaryKey)), Is.False);
        }

        [Test]
        public void HasSecondaryKeys_WithPropertySecondaryKey_ReturnsTrue()
        {
            Assert.That(IndexBuilder.HasSecondaryKeys(typeof(RecordWithPropertySecondaryKey)), Is.True);
        }

        #endregion

        #region IndexBuilder.FindSecondaryKeyMembers Tests

        [Test]
        public void FindSecondaryKeyMembers_FindsAllSecondaryKeys()
        {
            var members = IndexBuilder.FindSecondaryKeyMembers(typeof(TestRecord));
            Assert.That(members.Count, Is.EqualTo(2));

            var names = members.Select(m => m.attribute.Name ?? m.member.Name).ToList();
            Assert.That(names, Contains.Item("Category"));
            Assert.That(names, Contains.Item("GroupId"));
        }

        [Test]
        public void FindSecondaryKeyMembers_FindsPropertySecondaryKey()
        {
            var members = IndexBuilder.FindSecondaryKeyMembers(typeof(RecordWithPropertySecondaryKey));
            Assert.That(members.Count, Is.EqualTo(1));
            Assert.That(members[0].attribute.Name, Is.EqualTo("CategoryProp"));
        }

        #endregion

        #region IndexBuilder.BuildIndices Tests

        [Test]
        public void BuildIndices_CreatesCorrectIndices()
        {
            var records = new[]
            {
                new TestRecord { Id = 1, Category = "A", GroupId = 10, Name = "Record1" },
                new TestRecord { Id = 2, Category = "B", GroupId = 10, Name = "Record2" },
                new TestRecord { Id = 3, Category = "A", GroupId = 20, Name = "Record3" }
            };

            var container = IndexBuilder.BuildIndices(records);

            Assert.That(container.Count, Is.EqualTo(2));
            Assert.That(container.GetIndex("Category"), Is.Not.Null);
            Assert.That(container.GetIndex("GroupId"), Is.Not.Null);
        }

        [Test]
        public void BuildIndices_GroupsDuplicateKeys()
        {
            var records = new[]
            {
                new TestRecord { Id = 1, Category = "A", GroupId = 10 },
                new TestRecord { Id = 2, Category = "A", GroupId = 10 },
                new TestRecord { Id = 3, Category = "B", GroupId = 10 }
            };

            var container = IndexBuilder.BuildIndices(records);
            var categoryIndex = container.GetIndex("Category");

            // "A" should have 2 records
            var aIndices = categoryIndex.FindByKey("A");
            Assert.That(aIndices.Length, Is.EqualTo(2));
            Assert.That(aIndices, Contains.Item(0));
            Assert.That(aIndices, Contains.Item(1));

            // "B" should have 1 record
            var bIndices = categoryIndex.FindByKey("B");
            Assert.That(bIndices.Length, Is.EqualTo(1));
            Assert.That(bIndices[0], Is.EqualTo(2));
        }

        [Test]
        public void BuildIndices_IntKey_GroupsCorrectly()
        {
            var records = new[]
            {
                new TestRecord { Id = 1, GroupId = 10 },
                new TestRecord { Id = 2, GroupId = 10 },
                new TestRecord { Id = 3, GroupId = 20 }
            };

            var container = IndexBuilder.BuildIndices(records);
            var groupIndex = container.GetIndex("GroupId");

            var group10 = groupIndex.FindByKey(10);
            Assert.That(group10.Length, Is.EqualTo(2));

            var group20 = groupIndex.FindByKey(20);
            Assert.That(group20.Length, Is.EqualTo(1));
        }

        [Test]
        public void BuildIndices_EnumKey_WorksCorrectly()
        {
            var records = new[]
            {
                new RecordWithEnumSecondaryKey { Id = 1, Type = ItemType.Weapon },
                new RecordWithEnumSecondaryKey { Id = 2, Type = ItemType.Weapon },
                new RecordWithEnumSecondaryKey { Id = 3, Type = ItemType.Armor }
            };

            var container = IndexBuilder.BuildIndices(records);
            var typeIndex = container.GetIndex("Type");

            var weapons = typeIndex.FindByKey(ItemType.Weapon);
            Assert.That(weapons.Length, Is.EqualTo(2));

            var armors = typeIndex.FindByKey(ItemType.Armor);
            Assert.That(armors.Length, Is.EqualTo(1));
        }

        [Test]
        public void BuildIndices_EmptyRecords_ReturnsEmptyContainer()
        {
            var records = Array.Empty<TestRecord>();
            var container = IndexBuilder.BuildIndices(records);

            Assert.That(container.Count, Is.EqualTo(2)); // Still creates index definitions
            Assert.That(container.GetIndex("Category").Count, Is.EqualTo(0));
        }

        [Test]
        public void BuildIndices_WithPropertySecondaryKey_WorksCorrectly()
        {
            var records = new[]
            {
                new RecordWithPropertySecondaryKey { Id = 1, Category = "A" },
                new RecordWithPropertySecondaryKey { Id = 2, Category = "A" },
                new RecordWithPropertySecondaryKey { Id = 3, Category = "B" }
            };

            var container = IndexBuilder.BuildIndices(records);
            var categoryIndex = container.GetIndex("CategoryProp");

            Assert.That(categoryIndex, Is.Not.Null);
            Assert.That(categoryIndex.FindByKey("A").Length, Is.EqualTo(2));
        }

        #endregion

        #region IndexData Tests

        [Test]
        public void IndexData_AddEntry_AddsCorrectly()
        {
            var indexData = new IndexData("TestIndex", typeof(string));
            indexData.AddEntry("key1".GetHashCode(), "key1", new[] { 0, 1 });
            indexData.AddEntry("key2".GetHashCode(), "key2", new[] { 2 });

            Assert.That(indexData.Count, Is.EqualTo(2));
        }

        [Test]
        public void IndexData_FindByKey_String_ReturnsCorrectIndices()
        {
            var indexData = new IndexData("TestIndex", typeof(string));
            indexData.AddEntry("apple".GetHashCode(), "apple", new[] { 0, 2 });
            indexData.AddEntry("banana".GetHashCode(), "banana", new[] { 1 });

            var appleIndices = indexData.FindByKey("apple");
            Assert.That(appleIndices.Length, Is.EqualTo(2));
            Assert.That(appleIndices, Contains.Item(0));
            Assert.That(appleIndices, Contains.Item(2));

            var bananaIndices = indexData.FindByKey("banana");
            Assert.That(bananaIndices.Length, Is.EqualTo(1));
            Assert.That(bananaIndices[0], Is.EqualTo(1));
        }

        [Test]
        public void IndexData_FindByKey_NotFound_ReturnsEmptyArray()
        {
            var indexData = new IndexData("TestIndex", typeof(string));
            indexData.AddEntry("key1".GetHashCode(), "key1", new[] { 0 });

            var result = indexData.FindByKey("nonexistent");
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void IndexData_FindByKey_Int_ReturnsCorrectIndices()
        {
            var indexData = new IndexData("TestIndex", typeof(int));
            indexData.AddEntry(100.GetHashCode(), "100", new[] { 0, 1 });
            indexData.AddEntry(200.GetHashCode(), "200", new[] { 2 });

            var result100 = indexData.FindByKey(100);
            Assert.That(result100.Length, Is.EqualTo(2));

            var result200 = indexData.FindByKey(200);
            Assert.That(result200.Length, Is.EqualTo(1));
        }

        [Test]
        public void IndexData_FindByKey_NullKey_ReturnsEmptyArray()
        {
            var indexData = new IndexData("TestIndex", typeof(string));
            indexData.AddEntry("key1".GetHashCode(), "key1", new[] { 0 });

            var result = indexData.FindByKey<string>(null);
            Assert.That(result.Length, Is.EqualTo(0));
        }

        [Test]
        public void IndexData_Clear_RemovesAllEntries()
        {
            var indexData = new IndexData("TestIndex", typeof(string));
            indexData.AddEntry("key1".GetHashCode(), "key1", new[] { 0 });
            indexData.AddEntry("key2".GetHashCode(), "key2", new[] { 1 });

            indexData.Clear();

            Assert.That(indexData.Count, Is.EqualTo(0));
            Assert.That(indexData.FindByKey("key1").Length, Is.EqualTo(0));
        }

        [Test]
        public void IndexData_GetAllEntries_ReturnsAllEntries()
        {
            var indexData = new IndexData("TestIndex", typeof(string));
            indexData.AddEntry("key1".GetHashCode(), "key1", new[] { 0 });
            indexData.AddEntry("key2".GetHashCode(), "key2", new[] { 1, 2 });

            var entries = indexData.GetAllEntries();
            Assert.That(entries.Count, Is.EqualTo(2));
        }

        #endregion

        #region IndexContainer Tests

        [Test]
        public void IndexContainer_SetIndex_AddsNewIndex()
        {
            var container = new IndexContainer();
            var indexData = new IndexData("TestIndex", typeof(string));

            container.SetIndex(indexData);

            Assert.That(container.Count, Is.EqualTo(1));
            Assert.That(container.GetIndex("TestIndex"), Is.SameAs(indexData));
        }

        [Test]
        public void IndexContainer_SetIndex_UpdatesExistingIndex()
        {
            var container = new IndexContainer();
            var indexData1 = new IndexData("TestIndex", typeof(string));
            var indexData2 = new IndexData("TestIndex", typeof(string));

            container.SetIndex(indexData1);
            container.SetIndex(indexData2);

            Assert.That(container.Count, Is.EqualTo(1));
            Assert.That(container.GetIndex("TestIndex"), Is.SameAs(indexData2));
        }

        [Test]
        public void IndexContainer_GetIndex_NotFound_ReturnsNull()
        {
            var container = new IndexContainer();
            Assert.That(container.GetIndex("NonExistent"), Is.Null);
        }

        [Test]
        public void IndexContainer_RemoveIndex_RemovesCorrectly()
        {
            var container = new IndexContainer();
            container.SetIndex(new IndexData("Index1", typeof(string)));
            container.SetIndex(new IndexData("Index2", typeof(string)));

            var removed = container.RemoveIndex("Index1");

            Assert.That(removed, Is.True);
            Assert.That(container.Count, Is.EqualTo(1));
            Assert.That(container.GetIndex("Index1"), Is.Null);
            Assert.That(container.GetIndex("Index2"), Is.Not.Null);
        }

        [Test]
        public void IndexContainer_RemoveIndex_NotFound_ReturnsFalse()
        {
            var container = new IndexContainer();
            var removed = container.RemoveIndex("NonExistent");
            Assert.That(removed, Is.False);
        }

        [Test]
        public void IndexContainer_Clear_RemovesAllIndices()
        {
            var container = new IndexContainer();
            container.SetIndex(new IndexData("Index1", typeof(string)));
            container.SetIndex(new IndexData("Index2", typeof(int)));

            container.Clear();

            Assert.That(container.Count, Is.EqualTo(0));
        }

        [Test]
        public void IndexContainer_GetAllIndices_ReturnsAll()
        {
            var container = new IndexContainer();
            container.SetIndex(new IndexData("Index1", typeof(string)));
            container.SetIndex(new IndexData("Index2", typeof(int)));

            var indices = container.GetAllIndices();
            Assert.That(indices.Count, Is.EqualTo(2));
        }

        #endregion
    }
}
