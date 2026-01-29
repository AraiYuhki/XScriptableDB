using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Xeon.XScriptableDB.Tests
{
    /// <summary>
    /// SecondaryKey機能の統合テスト。
    /// TableAssetとIndexBuilderの連携をテストする。
    /// </summary>
    public class SecondaryKeyIntegrationTests
    {
        #region Test Data Classes

        private enum ItemRarity
        {
            Common,
            Uncommon,
            Rare,
            Epic,
            Legendary
        }

        private class ItemRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("Name", AllowDuplicates = false)]
            public string Name { get; set; }

            [SecondaryKey("Rarity")]
            public ItemRarity Rarity { get; set; }

            [SecondaryKey("Category")]
            public string Category { get; set; }

            public int Price { get; set; }
        }

        private class ItemTable : TableAsset<ItemRecord, int>
        {
            public static ItemTable Create(ItemRecord[] records)
            {
                var asset = ScriptableObject.CreateInstance<ItemTable>();
                asset.records = records;
                asset.EnsureSorted();
                asset.secondaryIndices = IndexBuilder.BuildIndices(records);
                return asset;
            }
        }

        #endregion

        private ItemTable CreateItemTable()
        {
            var records = new[]
            {
                new ItemRecord { Id = 1, Name = "Sword", Rarity = ItemRarity.Common, Category = "Weapon", Price = 100 },
                new ItemRecord { Id = 2, Name = "Shield", Rarity = ItemRarity.Uncommon, Category = "Armor", Price = 150 },
                new ItemRecord { Id = 3, Name = "Staff", Rarity = ItemRarity.Rare, Category = "Weapon", Price = 300 },
                new ItemRecord { Id = 4, Name = "Potion", Rarity = ItemRarity.Common, Category = "Consumable", Price = 50 },
                new ItemRecord { Id = 5, Name = "Helm", Rarity = ItemRarity.Rare, Category = "Armor", Price = 200 },
                new ItemRecord { Id = 6, Name = "Bow", Rarity = ItemRarity.Epic, Category = "Weapon", Price = 500 },
                new ItemRecord { Id = 7, Name = "Elixir", Rarity = ItemRarity.Legendary, Category = "Consumable", Price = 1000 }
            };
            return ItemTable.Create(records);
        }

        #region FindBySecondaryKey Tests

        [Test]
        public void FindBySecondaryKey_UniqueString_ReturnsCorrectRecord()
        {
            var table = CreateItemTable();

            var result = table.FindBySecondaryKey("Name", "Sword");

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(1));
            Assert.That(result.Name, Is.EqualTo("Sword"));
        }

        [Test]
        public void FindBySecondaryKey_NotFound_ReturnsNull()
        {
            var table = CreateItemTable();

            var result = table.FindBySecondaryKey("Name", "NonExistent");

            Assert.That(result, Is.Null);
        }

        [Test]
        public void FindBySecondaryKey_EnumKey_ReturnsCorrectRecord()
        {
            var table = CreateItemTable();

            var result = table.FindBySecondaryKey("Rarity", ItemRarity.Legendary);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Elixir"));
        }

        #endregion

        #region TryFindBySecondaryKey Tests

        [Test]
        public void TryFindBySecondaryKey_Found_ReturnsTrueAndRecord()
        {
            var table = CreateItemTable();

            var found = table.TryFindBySecondaryKey("Name", "Shield", out var record);

            Assert.That(found, Is.True);
            Assert.That(record, Is.Not.Null);
            Assert.That(record.Name, Is.EqualTo("Shield"));
        }

        [Test]
        public void TryFindBySecondaryKey_NotFound_ReturnsFalse()
        {
            var table = CreateItemTable();

            var found = table.TryFindBySecondaryKey("Name", "Unknown", out var record);

            Assert.That(found, Is.False);
            Assert.That(record, Is.Null);
        }

        #endregion

        #region FindAllBySecondaryKey Tests

        [Test]
        public void FindAllBySecondaryKey_DuplicateKey_ReturnsAllRecords()
        {
            var table = CreateItemTable();

            var results = table.FindAllBySecondaryKey("Category", "Weapon").ToList();

            Assert.That(results.Count, Is.EqualTo(3));
            Assert.That(results.All(r => r.Category == "Weapon"), Is.True);
        }

        [Test]
        public void FindAllBySecondaryKey_EnumKey_ReturnsAllRecords()
        {
            var table = CreateItemTable();

            var results = table.FindAllBySecondaryKey("Rarity", ItemRarity.Common).ToList();

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(r => r.Rarity == ItemRarity.Common), Is.True);
        }

        [Test]
        public void FindAllBySecondaryKey_NotFound_ReturnsEmpty()
        {
            var table = CreateItemTable();

            var results = table.FindAllBySecondaryKey("Category", "Mount").ToList();

            Assert.That(results.Count, Is.EqualTo(0));
        }

        #endregion

        #region FindAllBySecondaryKeyAsArray Tests

        [Test]
        public void FindAllBySecondaryKeyAsArray_ReturnsCorrectArray()
        {
            var table = CreateItemTable();

            var results = table.FindAllBySecondaryKeyAsArray("Category", "Armor");

            Assert.That(results.Length, Is.EqualTo(2));
            Assert.That(results.All(r => r.Category == "Armor"), Is.True);
        }

        [Test]
        public void FindAllBySecondaryKeyAsArray_NotFound_ReturnsEmptyArray()
        {
            var table = CreateItemTable();

            var results = table.FindAllBySecondaryKeyAsArray("Category", "Vehicle");

            Assert.That(results, Is.Not.Null);
            Assert.That(results.Length, Is.EqualTo(0));
        }

        #endregion

        #region Combined PrimaryKey and SecondaryKey Tests

        [Test]
        public void CombinedLookup_PrimaryKeyAndSecondaryKey_WorkCorrectly()
        {
            var table = CreateItemTable();

            // Primary key lookup
            var byPrimary = table.FindByKey(3);
            Assert.That(byPrimary.Name, Is.EqualTo("Staff"));

            // Secondary key lookup
            var bySecondary = table.FindBySecondaryKey("Name", "Staff");
            Assert.That(bySecondary.Id, Is.EqualTo(3));

            // Same record
            Assert.That(byPrimary, Is.SameAs(bySecondary));
        }

        #endregion

        #region Performance Characteristics Tests

        [Test]
        public void SecondaryKeyLookup_LargeDataset_PerformsWell()
        {
            // Create a large dataset
            var records = new ItemRecord[1000];
            for (var i = 0; i < 1000; i++)
            {
                records[i] = new ItemRecord
                {
                    Id = i + 1,
                    Name = $"Item_{i}",
                    Rarity = (ItemRarity)(i % 5),
                    Category = $"Category_{i % 10}",
                    Price = i * 10
                };
            }

            var table = ItemTable.Create(records);

            // Measure lookup time (should be O(1))
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (var i = 0; i < 100; i++)
            {
                var _ = table.FindBySecondaryKey("Name", "Item_500");
            }
            sw.Stop();

            // Should complete quickly (arbitrary threshold)
            Assert.That(sw.ElapsedMilliseconds, Is.LessThan(100));
        }

        [Test]
        public void SecondaryKeyLookup_MultipleIndices_AllWork()
        {
            var table = CreateItemTable();

            // All three indices should work
            var byName = table.FindBySecondaryKey("Name", "Bow");
            var byRarity = table.FindAllBySecondaryKey("Rarity", ItemRarity.Epic).First();
            var byCategory = table.FindAllBySecondaryKey("Category", "Weapon")
                                  .First(r => r.Name == "Bow");

            Assert.That(byName, Is.Not.Null);
            Assert.That(byRarity, Is.Not.Null);
            Assert.That(byCategory, Is.Not.Null);

            // All should be the same record
            Assert.That(byName.Id, Is.EqualTo(byRarity.Id));
            Assert.That(byRarity.Id, Is.EqualTo(byCategory.Id));
        }

        #endregion

        #region QueryResult Integration Tests

        [Test]
        public void QueryBySecondaryKey_ReturnsQueryResult()
        {
            var table = CreateItemTable();

            var result = table.QueryBySecondaryKey<ItemRecord, int, string>("Category", "Weapon");

            Assert.That(result.IsEmpty, Is.False);
            Assert.That(result.Count, Is.EqualTo(3));
            Assert.That(result.First, Is.Not.Null);
            Assert.That(result.First.Category, Is.EqualTo("Weapon"));
        }

        [Test]
        public void QueryBySecondaryKey_CanIterateWithNoAllocation()
        {
            var table = CreateItemTable();

            var result = table.QueryBySecondaryKey<ItemRecord, int, ItemRarity>("Rarity", ItemRarity.Rare);

            var count = 0;
            foreach (var record in result)
            {
                Assert.That(record.Rarity, Is.EqualTo(ItemRarity.Rare));
                count++;
            }

            Assert.That(count, Is.EqualTo(2));
        }

        [Test]
        public void QueryBySecondaryKey_ToArray_ReturnsCorrectRecords()
        {
            var table = CreateItemTable();

            var result = table.QueryBySecondaryKey<ItemRecord, int, string>("Category", "Consumable");
            var array = result.ToArray();

            Assert.That(array.Length, Is.EqualTo(2));
            Assert.That(array.All(r => r.Category == "Consumable"), Is.True);
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            var assets = Resources.FindObjectsOfTypeAll<ItemTable>();
            foreach (var asset in assets)
            {
                UnityEngine.Object.DestroyImmediate(asset);
            }
        }
    }
}
