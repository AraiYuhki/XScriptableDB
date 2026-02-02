using System;
using UnityEngine;
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

namespace Xeon.XScriptableDB.Sample
{
    [Serializable]
    public partial class EventRecord
    {
        [SerializeField, CsvColumn("id"), PrimaryKey]
        private int id;

        [SerializeField, CsvColumn("name")]
        private string name;

        [SerializeField, CsvColumn("description")]
        private string description;

        [SerializeField, CsvColumn("event_type"), SecondaryKey]
        private int event_type;

        [SerializeField, CsvColumn("start_date")]
        private SerializableDateTime start_date;

        [SerializeField, CsvColumn("end_date")]
        private SerializableDateTime end_date;

        [SerializeField, CsvColumn("is_active"), SecondaryKey]
        private bool is_active;

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

        public int Event_type
        {
            get => event_type;
#if UNITY_EDITOR
            set => event_type = value;
#endif
        }

        public SerializableDateTime Start_date
        {
            get => start_date;
#if UNITY_EDITOR
            set => start_date = value;
#endif
        }

        public SerializableDateTime End_date
        {
            get => end_date;
#if UNITY_EDITOR
            set => end_date = value;
#endif
        }

        public bool Is_active
        {
            get => is_active;
#if UNITY_EDITOR
            set => is_active = value;
#endif
        }
    }
}