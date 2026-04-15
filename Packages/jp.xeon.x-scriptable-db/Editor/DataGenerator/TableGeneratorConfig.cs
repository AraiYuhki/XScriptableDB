using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// Table generation configuration.
    /// </summary>
    [Serializable]
    public class TableGeneratorConfig
    {
        [SerializeField]
        private string tableName;

        [SerializeField]
        private int recordCount = 100;

        [SerializeField]
        private List<FieldGeneratorConfig> fieldConfigs = new List<FieldGeneratorConfig>();

        public string TableName
        {
            get => tableName;
            set => tableName = value;
        }

        public int RecordCount
        {
            get => recordCount;
            set => recordCount = value;
        }

        public List<FieldGeneratorConfig> FieldConfigs
        {
            get => fieldConfigs;
            set => fieldConfigs = value;
        }
    }
}
