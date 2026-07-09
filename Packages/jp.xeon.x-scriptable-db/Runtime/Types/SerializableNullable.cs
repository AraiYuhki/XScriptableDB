using System;
using UnityEngine;

namespace Xeon.XScriptableDB
{
    /// <summary>
    /// SerializableNullable の非ジェネリックアクセス用インターフェース。
    /// CsvParser や Validator がリフレクションなしで値の有無と中身を取得するために使用します。
    /// </summary>
    public interface ISerializableNullable
    {
        /// <summary>値が設定されているか。</summary>
        bool HasValue { get; }

        /// <summary>ボックス化された中身の値。HasValue が false の場合は null。</summary>
        object BoxedValue { get; }
    }

    /// <summary>
    /// シリアライズ可能な Nullable 構造体。
    /// Unity は Nullable&lt;T&gt;（int? など）をシリアライズできず、ドメインリロードで値が消失するため、
    /// nullable なカラムにはこのラッパー構造体を使用します。
    /// CSV では空文字または "null" が「値なし」として扱われます。
    /// </summary>
    [Serializable]
    public struct SerializableNullable<T> : ISerializableNullable, IEquatable<SerializableNullable<T>>
        where T : struct
    {
        [SerializeField]
        private bool hasValue;

        [SerializeField]
        private T value;

        public SerializableNullable(T value)
        {
            hasValue = true;
            this.value = value;
        }

        /// <summary>値なしを表すインスタンス。</summary>
        public static SerializableNullable<T> None => default;

        public bool HasValue => hasValue;

        public T Value
        {
            get
            {
                if (!hasValue)
                    throw new InvalidOperationException("値が設定されていません");
                return value;
            }
        }

        object ISerializableNullable.BoxedValue => hasValue ? value : null;

        public T GetValueOrDefault() => value;

        public T GetValueOrDefault(T defaultValue) => hasValue ? value : defaultValue;

        /// <summary>Nullable&lt;T&gt; へ変換します。</summary>
        public T? ToNullable() => hasValue ? value : null;

        public static implicit operator SerializableNullable<T>(T value)
            => new SerializableNullable<T>(value);

        public static implicit operator SerializableNullable<T>(T? value)
            => value.HasValue ? new SerializableNullable<T>(value.Value) : None;

        public static implicit operator T?(SerializableNullable<T> value)
            => value.ToNullable();

        public bool Equals(SerializableNullable<T> other)
        {
            if (hasValue != other.hasValue)
                return false;
            return !hasValue || value.Equals(other.value);
        }

        public override bool Equals(object obj)
            => obj is SerializableNullable<T> other && Equals(other);

        public override int GetHashCode()
            => hasValue ? value.GetHashCode() : 0;

        public override string ToString()
            => hasValue ? value.ToString() : "null";

        public static bool operator ==(SerializableNullable<T> left, SerializableNullable<T> right)
            => left.Equals(right);

        public static bool operator !=(SerializableNullable<T> left, SerializableNullable<T> right)
            => !left.Equals(right);
    }
}
