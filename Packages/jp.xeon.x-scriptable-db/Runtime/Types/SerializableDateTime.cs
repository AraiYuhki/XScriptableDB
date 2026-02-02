using System;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// シリアライズ可能なDateTime構造体。
    /// UnityはDateTime型を直接シリアライズできないため、
    /// このラッパー構造体を使用してInspectorで編集可能にする。
    /// </summary>
    [Serializable]
    public struct SerializableDateTime : IEquatable<SerializableDateTime>, IComparable<SerializableDateTime>
    {
        [SerializeField]
        private long ticks;

        public SerializableDateTime(DateTime dateTime)
        {
            ticks = dateTime.Ticks;
        }

        public SerializableDateTime(long ticks)
        {
            this.ticks = ticks;
        }

        public SerializableDateTime(int year, int month, int day)
            : this(new DateTime(year, month, day)) { }

        public SerializableDateTime(int year, int month, int day, int hour, int minute, int second)
            : this(new DateTime(year, month, day, hour, minute, second)) { }

        public long Ticks => ticks;

        public DateTime DateTime => new DateTime(ticks);

        public int Year => DateTime.Year;
        public int Month => DateTime.Month;
        public int Day => DateTime.Day;
        public int Hour => DateTime.Hour;
        public int Minute => DateTime.Minute;
        public int Second => DateTime.Second;

        public static SerializableDateTime Now => new SerializableDateTime(DateTime.Now);
        public static SerializableDateTime UtcNow => new SerializableDateTime(DateTime.UtcNow);
        public static SerializableDateTime Today => new SerializableDateTime(DateTime.Today);
        public static SerializableDateTime MinValue => new SerializableDateTime(DateTime.MinValue);
        public static SerializableDateTime MaxValue => new SerializableDateTime(DateTime.MaxValue);

        public static implicit operator DateTime(SerializableDateTime serializableDateTime)
        {
            return serializableDateTime.DateTime;
        }

        public static implicit operator SerializableDateTime(DateTime dateTime)
        {
            return new SerializableDateTime(dateTime);
        }

        public override string ToString()
        {
            return DateTime.ToString("yyyy/MM/dd HH:mm:ss");
        }

        public string ToString(string format)
        {
            return DateTime.ToString(format);
        }

        public bool Equals(SerializableDateTime other)
        {
            return ticks == other.ticks;
        }

        public override bool Equals(object obj)
        {
            return obj is SerializableDateTime other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ticks.GetHashCode();
        }

        public int CompareTo(SerializableDateTime other)
        {
            return ticks.CompareTo(other.ticks);
        }

        public static bool operator ==(SerializableDateTime left, SerializableDateTime right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(SerializableDateTime left, SerializableDateTime right)
        {
            return !left.Equals(right);
        }

        public static bool operator <(SerializableDateTime left, SerializableDateTime right)
        {
            return left.ticks < right.ticks;
        }

        public static bool operator >(SerializableDateTime left, SerializableDateTime right)
        {
            return left.ticks > right.ticks;
        }

        public static bool operator <=(SerializableDateTime left, SerializableDateTime right)
        {
            return left.ticks <= right.ticks;
        }

        public static bool operator >=(SerializableDateTime left, SerializableDateTime right)
        {
            return left.ticks >= right.ticks;
        }

        public SerializableDateTime AddDays(double value)
        {
            return new SerializableDateTime(DateTime.AddDays(value));
        }

        public SerializableDateTime AddHours(double value)
        {
            return new SerializableDateTime(DateTime.AddHours(value));
        }

        public SerializableDateTime AddMinutes(double value)
        {
            return new SerializableDateTime(DateTime.AddMinutes(value));
        }

        public SerializableDateTime AddSeconds(double value)
        {
            return new SerializableDateTime(DateTime.AddSeconds(value));
        }

        public SerializableDateTime AddMonths(int months)
        {
            return new SerializableDateTime(DateTime.AddMonths(months));
        }

        public SerializableDateTime AddYears(int value)
        {
            return new SerializableDateTime(DateTime.AddYears(value));
        }
    }
}
