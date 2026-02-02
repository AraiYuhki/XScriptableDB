using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Sample
{
    [Serializable]
    public partial class ItemRecord
    {
        [SerializeField, CsvColumn("id"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("name")]
        private string name;

        [SerializeField, CsvColumn("description")]
        private string description;

        [SerializeField, CsvColumn("category_id"), SecondaryKey]
        private int categoryId;

        [SerializeField, CsvColumn("price")]
        private int price;

        [SerializeField, CsvColumn("rarity"), SecondaryKey]
        private int rarity;

        [SerializeField, CsvColumn("is_tradable")]
        private bool isTradable;

        [SerializeField, CsvColumn("release_date")]
        private SerializableDateTime releaseDate;

        public int Id
        {
            get => id;
#if UNITY_EDITOR
            set => id = value;
#endif
        }

        public string Name
        {
            get => name;
#if UNITY_EDITOR
            set => name = value;
#endif
        }

        public string Description
        {
            get => description;
#if UNITY_EDITOR
            set => description = value;
#endif
        }

        public int CategoryId
        {
            get => categoryId;
#if UNITY_EDITOR
            set => categoryId = value;
#endif
        }

        public int Price
        {
            get => price;
#if UNITY_EDITOR
            set => price = value;
#endif
        }

        public int Rarity
        {
            get => rarity;
#if UNITY_EDITOR
            set => rarity = value;
#endif
        }

        public bool IsTradable
        {
            get => isTradable;
#if UNITY_EDITOR
            set => isTradable = value;
#endif
        }

        public SerializableDateTime ReleaseDate
        {
            get => releaseDate;
#if UNITY_EDITOR
            set => releaseDate = value;
#endif
        }
    }
}