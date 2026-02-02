using System;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// 複合キーの値を表す構造体。
    /// </summary>
    public readonly struct CompositeKeyValue : IEquatable<CompositeKeyValue>
    {
        private readonly object[] values;
        private readonly int hashCode;

        public CompositeKeyValue(params object[] values)
        {
            this.values = values ?? Array.Empty<object>();
            hashCode = ComputeHashCode(this.values);
        }

        public int PartCount => values?.Length ?? 0;

        public object GetPart(int index)
        {
            if (values == null || index < 0 || index >= values.Length)
                return null;
            return values[index];
        }

        private static int ComputeHashCode(object[] values)
        {
            if (values == null || values.Length == 0)
                return 0;

            unchecked
            {
                var hash = 17;
                foreach (var value in values)
                    hash = hash * 31 + (value?.GetHashCode() ?? 0);
                return hash;
            }
        }

        public bool Equals(CompositeKeyValue other)
        {
            if (values == null && other.values == null)
                return true;
            if (values == null || other.values == null)
                return false;
            if (values.Length != other.values.Length)
                return false;

            for (var i = 0; i < values.Length; i++)
            {
                if (!Equals(values[i], other.values[i]))
                    return false;
            }
            return true;
        }

        public override int GetHashCode() => hashCode;
        public override bool Equals(object obj) => obj is CompositeKeyValue other && Equals(other);
        public override string ToString() => $"({string.Join(", ", values ?? Array.Empty<object>())})";

        public static bool operator ==(CompositeKeyValue left, CompositeKeyValue right) => left.Equals(right);
        public static bool operator !=(CompositeKeyValue left, CompositeKeyValue right) => !left.Equals(right);
    }
}
