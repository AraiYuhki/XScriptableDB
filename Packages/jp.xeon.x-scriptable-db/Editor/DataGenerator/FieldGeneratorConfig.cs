using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// フィールド生成設定。
    /// </summary>
    [Serializable]
    public class FieldGeneratorConfig
    {
        [SerializeField]
        private string fieldName;

        [SerializeField]
        private GeneratorRule rule = GeneratorRule.Random;

        [SerializeField]
        private string minValue = "0";

        [SerializeField]
        private string maxValue = "100";

        [SerializeField]
        private List<string> choices = new List<string>();

        [SerializeField]
        private string pattern = "{0}";

        [SerializeField]
        private string fixedValue = "";

        [SerializeField]
        private int startValue = 1;

        public string FieldName
        {
            get => fieldName;
            set => fieldName = value;
        }

        public GeneratorRule Rule
        {
            get => rule;
            set => rule = value;
        }

        public string MinValue
        {
            get => minValue;
            set => minValue = value;
        }

        public string MaxValue
        {
            get => maxValue;
            set => maxValue = value;
        }

        public List<string> Choices
        {
            get => choices;
            set => choices = value;
        }

        public string Pattern
        {
            get => pattern;
            set => pattern = value;
        }

        public string FixedValue
        {
            get => fixedValue;
            set => fixedValue = value;
        }

        public int StartValue
        {
            get => startValue;
            set => startValue = value;
        }
    }
}
