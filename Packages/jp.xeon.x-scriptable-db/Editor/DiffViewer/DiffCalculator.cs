using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// テーブルデータの差分を計算するクラス。
    /// </summary>
    public static class DiffCalculator
    {
        private const BindingFlags MemberFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        /// <summary>
        /// 2つのレコード配列の差分を計算する。
        /// </summary>
        /// <typeparam name="T">レコードの型</typeparam>
        /// <typeparam name="TKey">PrimaryKeyの型</typeparam>
        /// <param name="oldRecords">変更前のレコード配列</param>
        /// <param name="newRecords">変更後のレコード配列</param>
        /// <param name="keySelector">PrimaryKeyを取得する関数</param>
        /// <param name="tableName">テーブル名（オプション）</param>
        /// <returns>差分結果</returns>
        public static TableDiffResult Calculate<T, TKey>(
            IReadOnlyList<T> oldRecords,
            IReadOnlyList<T> newRecords,
            Func<T, TKey> keySelector,
            string tableName = null)
            where T : class
            where TKey : IEquatable<TKey>
        {
            var result = new TableDiffResult
            {
                TableName = tableName ?? typeof(T).Name,
                RecordTypeName = typeof(T).FullName
            };

            // PrimaryKeyでインデックスを作成
            var oldByKey = new Dictionary<TKey, (T record, int index)>();
            var newByKey = new Dictionary<TKey, (T record, int index)>();

            for (var i = 0; i < oldRecords.Count; i++)
            {
                var record = oldRecords[i];
                if (record == null) continue;
                var key = keySelector(record);
                oldByKey[key] = (record, i);
            }

            for (var i = 0; i < newRecords.Count; i++)
            {
                var record = newRecords[i];
                if (record == null) continue;
                var key = keySelector(record);
                newByKey[key] = (record, i);
            }

            // 削除・変更の検出
            foreach (var (key, (oldRecord, oldIndex)) in oldByKey)
            {
                if (newByKey.TryGetValue(key, out var newEntry))
                {
                    // 両方に存在 → 変更チェック
                    var fieldDiffs = CompareRecords(oldRecord, newEntry.record);
                    var hasChanges = false;
                    foreach (var diff in fieldDiffs)
                    {
                        if (diff.HasChanged)
                        {
                            hasChanges = true;
                            break;
                        }
                    }

                    result.Diffs.Add(new RecordDiff
                    {
                        PrimaryKey = key,
                        DiffType = hasChanges ? DiffType.Modified : DiffType.Unchanged,
                        OldRecord = oldRecord,
                        NewRecord = newEntry.record,
                        OldIndex = oldIndex,
                        NewIndex = newEntry.index,
                        FieldDiffs = fieldDiffs
                    });
                    continue;
                }
                
                // 新しいデータにない → 削除
                result.Diffs.Add(new RecordDiff
                {
                    PrimaryKey = key,
                    DiffType = DiffType.Removed,
                    OldRecord = oldRecord,
                    NewRecord = null,
                    OldIndex = oldIndex,
                    NewIndex = -1,
                    FieldDiffs = CreateFieldDiffsForRemoval(oldRecord)
                });
            }

            // 追加の検出
            foreach (var (key, (newRecord, newIndex)) in newByKey)
            {
                if (oldByKey.ContainsKey(key))
                    continue;
                result.Diffs.Add(new RecordDiff
                {
                    PrimaryKey = key,
                    DiffType = DiffType.Added,
                    OldRecord = null,
                    NewRecord = newRecord,
                    OldIndex = -1,
                    NewIndex = newIndex,
                    FieldDiffs = CreateFieldDiffsForAddition(newRecord)
                });
            }

            // PrimaryKeyでソート
            result.Diffs.Sort((a, b) =>
            {
                if (a.PrimaryKey is IComparable ca && b.PrimaryKey is IComparable cb)
                    return ca.CompareTo(cb);
                return string.Compare(a.PrimaryKey?.ToString(), b.PrimaryKey?.ToString(), StringComparison.Ordinal);
            });

            result.RecalculateCounts();
            return result;
        }

        /// <summary>
        /// ITableAssetとCSVデータの差分を計算する。
        /// </summary>
        /// <param name="tableAsset">現在のテーブルアセット</param>
        /// <param name="importedRecords">インポートするレコード配列</param>
        /// <returns>差分結果</returns>
        public static TableDiffResult Calculate(ITableAsset tableAsset, IList importedRecords)
        {
            if (tableAsset == null)
                throw new ArgumentNullException(nameof(tableAsset));

            var recordType = tableAsset.RecordType;
            var keyType = tableAsset.KeyType;

            // PrimaryKeyアクセサを作成
            var keyAccessor = CreateKeyAccessor(recordType);
            if (keyAccessor == null)
            {
                Debug.LogWarning($"Could not find PrimaryKey accessor for type {recordType.Name}");
                return new TableDiffResult
                {
                    TableName = tableAsset.GetType().Name,
                    RecordTypeName = recordType.FullName
                };
            }

            var result = new TableDiffResult
            {
                TableName = tableAsset.GetType().Name,
                RecordTypeName = recordType.FullName
            };

            // 現在のレコードをディクショナリに
            var oldByKey = new Dictionary<object, (object record, int index)>();
            var index = 0;
            foreach (var record in tableAsset.Records)
            {
                if (record == null) continue;
                var key = keyAccessor(record);
                if (key != null)
                    oldByKey[key] = (record, index);
                index++;
            }

            // 新しいレコードをディクショナリに
            var newByKey = new Dictionary<object, (object record, int index)>();
            index = 0;
            foreach (var record in importedRecords)
            {
                if (record == null) continue;
                var key = keyAccessor(record);
                if (key != null)
                    newByKey[key] = (record, index);
                index++;
            }

            // 削除・変更の検出
            foreach (var (key, (oldRecord, oldIndex)) in oldByKey)
            {
                if (newByKey.TryGetValue(key, out var newEntry))
                {
                    var fieldDiffs = CompareRecords(oldRecord, newEntry.record);
                    var hasChanges = false;
                    foreach (var diff in fieldDiffs)
                    {
                        if (diff.HasChanged)
                        {
                            hasChanges = true;
                            break;
                        }
                    }

                    result.Diffs.Add(new RecordDiff
                    {
                        PrimaryKey = key,
                        DiffType = hasChanges ? DiffType.Modified : DiffType.Unchanged,
                        OldRecord = oldRecord,
                        NewRecord = newEntry.record,
                        OldIndex = oldIndex,
                        NewIndex = newEntry.index,
                        FieldDiffs = fieldDiffs
                    });
                    continue;
                }
                result.Diffs.Add(new RecordDiff
                {
                    PrimaryKey = key,
                    DiffType = DiffType.Removed,
                    OldRecord = oldRecord,
                    NewRecord = null,
                    OldIndex = oldIndex,
                    NewIndex = -1,
                    FieldDiffs = CreateFieldDiffsForRemoval(oldRecord)
                });
            }

            // 追加の検出
            foreach (var (key, (newRecord, newIndex)) in newByKey)
            {
                if (oldByKey.ContainsKey(key))
                    continue;
                result.Diffs.Add(new RecordDiff
                {
                    PrimaryKey = key,
                    DiffType = DiffType.Added,
                    OldRecord = null,
                    NewRecord = newRecord,
                    OldIndex = -1,
                    NewIndex = newIndex,
                    FieldDiffs = CreateFieldDiffsForAddition(newRecord)
                });
            }

            // ソート
            result.Diffs.Sort((a, b) =>
            {
                if (a.PrimaryKey is IComparable ca && b.PrimaryKey is IComparable cb)
                    return ca.CompareTo(cb);
                return string.Compare(a.PrimaryKey?.ToString(), b.PrimaryKey?.ToString(), StringComparison.Ordinal);
            });

            result.RecalculateCounts();
            return result;
        }

        /// <summary>
        /// 2つのレコードのフィールドを比較する。
        /// </summary>
        public static List<FieldDiff> CompareRecords(object oldRecord, object newRecord)
        {
            var result = new List<FieldDiff>();

            if (oldRecord == null || newRecord == null)
                return result;

            var type = oldRecord.GetType();
            if (type != newRecord.GetType())
                return result;

            // フィールドを比較
            foreach (var field in type.GetFields(MemberFlags))
            {
                if (field.IsStatic || field.IsLiteral)
                    continue;

                var oldValue = field.GetValue(oldRecord);
                var newValue = field.GetValue(newRecord);
                var diffType = CompareValues(oldValue, newValue);

                result.Add(new FieldDiff(field.Name, oldValue, newValue, diffType));
            }

            // プロパティを比較（バッキングフィールドでないもの）
            foreach (var property in type.GetProperties(MemberFlags))
            {
                if (!property.CanRead)
                    continue;

                // インデクサーはスキップ
                if (property.GetIndexParameters().Length > 0)
                    continue;

                try
                {
                    var oldValue = property.GetValue(oldRecord);
                    var newValue = property.GetValue(newRecord);
                    var diffType = CompareValues(oldValue, newValue);

                    result.Add(new FieldDiff(property.Name, oldValue, newValue, diffType));
                }
                catch
                {
                    // プロパティの取得に失敗した場合はスキップ
                }
            }

            return result;
        }

        /// <summary>
        /// 2つの値を比較する。
        /// </summary>
        private static DiffType CompareValues(object oldValue, object newValue)
        {
            if (oldValue == null && newValue == null)
                return DiffType.Unchanged;

            if (oldValue == null)
                return DiffType.Added;

            if (newValue == null)
                return DiffType.Removed;

            // IEquatableを使用して比較
            if (oldValue.Equals(newValue))
                return DiffType.Unchanged;

            // コレクションの場合は要素を比較
            if (oldValue is ICollection oldCol && newValue is ICollection newCol)
            {
                if (oldCol.Count != newCol.Count)
                    return DiffType.Modified;

                var oldEnumerator = oldCol.GetEnumerator();
                var newEnumerator = newCol.GetEnumerator();

                while (oldEnumerator.MoveNext() && newEnumerator.MoveNext())
                {
                    if (!Equals(oldEnumerator.Current, newEnumerator.Current))
                        return DiffType.Modified;
                }

                return DiffType.Unchanged;
            }

            return DiffType.Modified;
        }

        /// <summary>
        /// 削除レコード用のフィールド差分を作成する。
        /// </summary>
        private static List<FieldDiff> CreateFieldDiffsForRemoval(object record)
        {
            var result = new List<FieldDiff>();
            if (record == null)
                return result;

            var type = record.GetType();

            foreach (var field in type.GetFields(MemberFlags))
            {
                if (field.IsStatic || field.IsLiteral)
                    continue;

                var value = field.GetValue(record);
                result.Add(new FieldDiff(field.Name, value, null, DiffType.Removed));
            }

            return result;
        }

        /// <summary>
        /// 追加レコード用のフィールド差分を作成する。
        /// </summary>
        private static List<FieldDiff> CreateFieldDiffsForAddition(object record)
        {
            var result = new List<FieldDiff>();
            if (record == null)
                return result;

            var type = record.GetType();

            foreach (var field in type.GetFields(MemberFlags))
            {
                if (field.IsStatic || field.IsLiteral)
                    continue;

                var value = field.GetValue(record);
                result.Add(new FieldDiff(field.Name, null, value, DiffType.Added));
            }

            return result;
        }

        /// <summary>
        /// PrimaryKeyアクセサを作成する。
        /// </summary>
        private static Func<object, object> CreateKeyAccessor(Type recordType)
        {
            // PrimaryKeyAttributeを持つフィールド/プロパティを探す
            foreach (var field in recordType.GetFields(MemberFlags))
            {
                if (field.GetCustomAttribute<PrimaryKeyAttribute>() != null)
                    return record => field.GetValue(record);
            }

            foreach (var property in recordType.GetProperties(MemberFlags))
            {
                if (property.GetCustomAttribute<PrimaryKeyAttribute>() != null && property.CanRead)
                    return record => property.GetValue(record);
            }

            // IRecord<TKey>を実装している場合
            var recordInterface = recordType.GetInterface("IRecord`1");
            if (recordInterface != null)
            {
                var keyProperty = recordType.GetProperty("PrimaryKey");
                if (keyProperty != null && keyProperty.CanRead)
                    return record => keyProperty.GetValue(record);
            }

            // "Id", "ID", "Key", "PrimaryKey" という名前のフィールド/プロパティを探す
            var commonNames = new[] { "Id", "ID", "Key", "PrimaryKey", "id", "key" };
            foreach (var name in commonNames)
            {
                var field = recordType.GetField(name, MemberFlags);
                if (field != null)
                    return record => field.GetValue(record);

                var property = recordType.GetProperty(name, MemberFlags);
                if (property != null && property.CanRead)
                    return record => property.GetValue(record);
            }

            return null;
        }
    }
}
