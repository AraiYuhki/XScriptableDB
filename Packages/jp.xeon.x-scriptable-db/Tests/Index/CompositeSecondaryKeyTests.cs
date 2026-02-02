using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Xeon.XScriptableDB.Tests
{
    public class CompositeSecondaryKeyTests
    {
        private enum Rarity
        {
            Common,
            Rare,
            Epic
        }

        private enum Element
        {
            Fire,
            Ice,
            Wind
        }

        private class CompositeRecord
        {
            [PrimaryKey]
            public int Id { get; set; }

            [SecondaryKey("RarityElement", 0, AllowDuplicates = false)]
            public Rarity Rarity { get; set; }

            [SecondaryKey("RarityElement", 1, AllowDuplicates = false)]
            public Element Element { get; set; }

            [SecondaryKey("Category")]
            public string Category { get; set; }

            [SecondaryKey("TagElement", 0)]
            public string Tag { get; set; }

            [SecondaryKey("TagElement", 1)]
            public string ElementName { get; set; }
        }

        private class CompositeTable : TableAsset<CompositeRecord, int>
        {
            public static CompositeTable Create(CompositeRecord[] records)
            {
                var asset = ScriptableObject.CreateInstance<CompositeTable>();
                asset.records = records;
                asset.EnsureSorted();
                asset.secondaryIndices = IndexBuilder.BuildIndices(records);
                return asset;
            }
        }

        [Test]
        public void GroupSecondaryKeyMembers_SortsByOrder()
        {
            var groups = IndexBuilder.GroupSecondaryKeyMembers(typeof(CompositeRecord));

            Assert.That(groups.ContainsKey("RarityElement"), Is.True);
            var members = groups["RarityElement"];
            Assert.That(members.Count, Is.EqualTo(2));
            Assert.That(members[0].member.Name, Is.EqualTo(nameof(CompositeRecord.Rarity)));
            Assert.That(members[1].member.Name, Is.EqualTo(nameof(CompositeRecord.Element)));
        }

        [Test]
        public void FindByCompositeSecondaryKey_ReturnsMatchingRecord()
        {
            var table = CompositeTable.Create(new[]
            {
                new CompositeRecord { Id = 1, Rarity = Rarity.Common, Element = Element.Fire, Category = "Weapon" },
                new CompositeRecord { Id = 2, Rarity = Rarity.Rare, Element = Element.Ice, Category = "Armor" },
                new CompositeRecord { Id = 3, Rarity = Rarity.Epic, Element = Element.Wind, Category = "Weapon" }
            });

            var result = table.FindBySecondaryKey("RarityElement", Rarity.Rare, Element.Ice);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Id, Is.EqualTo(2));
        }

        [Test]
        public void FindAllByCompositeSecondaryKey_ReturnsAllMatches()
        {
            var table = CompositeTable.Create(new[]
            {
                new CompositeRecord { Id = 1, Rarity = Rarity.Common, Element = Element.Fire, Category = "Weapon" },
                new CompositeRecord { Id = 2, Rarity = Rarity.Common, Element = Element.Fire, Category = "Armor" },
                new CompositeRecord { Id = 3, Rarity = Rarity.Rare, Element = Element.Ice, Category = "Weapon" }
            });

            var results = table.FindAllBySecondaryKey("RarityElement", Rarity.Common, Element.Fire).ToList();

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results.All(record => record.Rarity == Rarity.Common && record.Element == Element.Fire), Is.True);
        }

        [Test]
        public void BuildCompositeIndex_RespectsAllowDuplicates()
        {
            var table = CompositeTable.Create(new[]
            {
                new CompositeRecord { Id = 1, Rarity = Rarity.Common, Element = Element.Fire },
                new CompositeRecord { Id = 2, Rarity = Rarity.Common, Element = Element.Fire }
            });

            var index = table.SecondaryIndices.GetIndex("RarityElement");
            var entries = index.GetAllEntries();

            Assert.That(entries.Count, Is.EqualTo(1));
            Assert.That(entries[0].recordIndices.Length, Is.EqualTo(1));
        }

        [Test]
        public void BuildCompositeIndex_SkipsNullParts()
        {
            var table = CompositeTable.Create(new[]
            {
                new CompositeRecord { Id = 1, Tag = "A", ElementName = "Fire" },
                new CompositeRecord { Id = 2, Tag = "B", ElementName = null }
            });

            var result = table.FindBySecondaryKey("TagElement", "B", null);

            Assert.That(result, Is.Null);
        }
    }
}
