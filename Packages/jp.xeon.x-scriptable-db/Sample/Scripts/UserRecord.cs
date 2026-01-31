using System;
using UnityEngine;

namespace Xeon.XScriptableDB.Sample
{
    [Serializable]
    public class UserRecord
    {
        [SerializeField, PrimaryKey]
        private int id;
        [SerializeField]
        private string name;
        [SerializeField]
        private int age;
        [SerializeField, SecondaryKey]
        private bool isMale = true;
        [SerializeField]
        private DateTime createdAt;

        public int Id
        {
            get => id;
            set => id = value;
        }

        public string Name
        {
            get => name;
            set => name = value;
        }

        public int Age
        {
            get => age;
            set => age = value;
        }
        public bool IsMale
        {
            get => isMale;
            set => isMale = value;
        }
        public DateTime CreatedAt
        {
            get => createdAt;
            set => createdAt = value;
        }
    }
}
