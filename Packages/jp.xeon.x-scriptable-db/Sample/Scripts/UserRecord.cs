using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Sample
{
    [Serializable]
    public partial class UserRecord
    {
        [SerializeField, CsvColumn("id"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("name")]
        private string name;

        [SerializeField, CsvColumn("age")]
        private int age;

        [SerializeField, CsvColumn("is_male"), SecondaryKey]
        private bool isMale;

        [SerializeField, CsvColumn("created_at")]
        private SerializableDateTime createdAt;

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

        public int Age
        {
            get => age;
#if UNITY_EDITOR
            set => age = value;
#endif
        }

        public bool IsMale
        {
            get => isMale;
#if UNITY_EDITOR
            set => isMale = value;
#endif
        }

        public SerializableDateTime CreatedAt
        {
            get => createdAt;
#if UNITY_EDITOR
            set => createdAt = value;
#endif
        }
    }
}