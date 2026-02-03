# 複合インデックス実装計画書

## 概要

本ドキュメントは、XScriptableDBにおける複合インデックス（Composite Index）機能の実装計画を定義します。
複合インデックスは、複数のフィールドを組み合わせてSecondaryKeyインデックスを構築し、より柔軟な検索を可能にする機能です。

---

## 目的

- 複数フィールドの組み合わせによるグループ検索（複合SecondaryKey）
- 既存の単一SecondaryKeyインデックスとの互換性維持
- O(1)の検索性能維持
- TableEditorでの複合インデックス定義・編集サポート

---

## 設計方針

### PrimaryKeyについて

**PrimaryKeyは必ず1つのみとする**（現状維持）

- 複合PrimaryKeyは実装しない
- 複数フィールドで一意識別が必要な場合は、連結キーフィールドを別途定義するか、SecondaryKeyを活用する

### 複合SecondaryKeyの設計

**同一名のSecondaryKeyを複合キーとしてグループ化する**

```csharp
// 例: "RarityElement" という名前の複合インデックス
[SecondaryKey("RarityElement", 0)]  // 第1フィールド
private RarityType rarity;

[SecondaryKey("RarityElement", 1)]  // 第2フィールド
private ElementType element;

// 従来の単一インデックス（互換性維持）
[SecondaryKey]  // インデックス名 = フィールド名 "category"
private string category;

[SecondaryKey("ByName")]  // インデックス名 = "ByName"
private string name;
```

---

## 現状分析

### 実装済み機能

| コンポーネント | 状態 | 備考 |
|---------------|------|------|
| `PrimaryKeyAttribute` | ✅ | 単一フィールドのみ |
| `SecondaryKeyAttribute` | ✅ | 単一フィールドのみ、Name/AllowDuplicatesプロパティあり |
| `IndexBuilder` | ✅ | 単一フィールドのSecondaryKeyインデックス構築 |
| `IndexData` | ✅ | ハッシュベースのインデックスデータ構造 |
| `IndexContainer` | ✅ | 複数インデックスの管理 |
| `TableAsset<T, TKey>` | ✅ | PrimaryKey二分探索、SecondaryKey O(1)検索 |
| `TableEditorWindow` | ✅ | YAML定義からC#クラス生成（単一インデックスのみ） |
| `TableDefinition` | ⚠️ | `indices`は`List<string>`（TODO: 複合キー対応） |
| `ClassGenerator` | ✅ | SecondaryKey属性生成（単一のみ） |

### 現在の制約

1. **SecondaryKeyAttribute**: 1属性につき1フィールドのみ、Orderプロパティなし
2. **IndexBuilder**: 同一名のSecondaryKeyをグループ化するロジックなし
3. **TableDefinition**: インデックスは単一カラム名のリスト
4. **TableEditorWindow**: 複合インデックスの編集UIなし
5. **ClassGenerator**: 複合インデックス用の属性生成なし

---

## 実装タスク

### Phase 2.4: 複合インデックス実装

本機能はPhase 2（インデックス・高速検索）の拡張として位置づけます。

---

#### 2.4.1 SecondaryKeyAttribute拡張

**ファイル**: `Runtime/Schema/Attributes/SecondaryKeyAttribute.cs`

**変更内容**:
- `Order`プロパティを追加（複合キー内の順序指定）
- コンストラクタのオーバーロード追加

```csharp
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class SecondaryKeyAttribute : Attribute
{
    /// <summary>
    /// インデックスの名前。指定しない場合はメンバー名が使用される。
    /// 同じ名前を持つ複数のフィールドは複合インデックスとしてグループ化される。
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 複合インデックス内でのフィールド順序。
    /// 単一フィールドインデックスの場合は無視される。
    /// </summary>
    public int Order { get; set; } = 0;

    /// <summary>
    /// 同じキー値を持つ複数のレコードを許可するかどうか。
    /// </summary>
    public bool AllowDuplicates { get; set; } = true;

    /// <summary>
    /// 単一フィールドインデックス用コンストラクタ。
    /// インデックス名はフィールド名が使用される。
    /// </summary>
    public SecondaryKeyAttribute()
    {
        Name = null;
    }

    /// <summary>
    /// インデックス名を指定するコンストラクタ。
    /// </summary>
    /// <param name="name">インデックス名</param>
    public SecondaryKeyAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// 複合インデックス用コンストラクタ。
    /// </summary>
    /// <param name="name">インデックス名</param>
    /// <param name="order">複合キー内の順序</param>
    public SecondaryKeyAttribute(string name, int order)
    {
        Name = name;
        Order = order;
    }
}
```

---

#### 2.4.2 CompositeKeyValue構造体

**ファイル**: `Runtime/Core/CompositeKeyValue.cs`（新規）

**機能**:
- 複合キー値の保持
- ハッシュコード計算（複数値の組み合わせ）
- 等値比較

```csharp
using System;
using System.Collections.Generic;

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
                foreach (var v in values)
                    hash = hash * 31 + (v?.GetHashCode() ?? 0);
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
```

---

#### 2.4.3 IndexBuilder複合インデックス対応

**ファイル**: `Runtime/Index/IndexBuilder.cs`

**変更内容**:
- 同一インデックス名のSecondaryKeyをグループ化
- Order順でソート
- 複合キーのハッシュ計算
- 既存の単一SecondaryKeyとの互換性維持

```csharp
public static class IndexBuilder
{
    // 既存のメソッドは維持

    /// <summary>
    /// SecondaryKeyメンバーをインデックス名でグループ化して取得する。
    /// 同じインデックス名を持つメンバーは複合インデックスとして扱われる。
    /// </summary>
    public static Dictionary<string, List<(MemberInfo member, SecondaryKeyAttribute attribute)>>
        GroupSecondaryKeyMembers(Type type)
    {
        var result = new Dictionary<string, List<(MemberInfo, SecondaryKeyAttribute)>>();
        var allMembers = FindSecondaryKeyMembers(type);

        foreach (var (member, attr) in allMembers)
        {
            var indexName = attr.Name ?? member.Name;
            if (!result.TryGetValue(indexName, out var list))
            {
                list = new List<(MemberInfo, SecondaryKeyAttribute)>();
                result[indexName] = list;
            }
            list.Add((member, attr));
        }

        // 各グループをOrder順でソート
        foreach (var key in result.Keys.ToList())
            result[key] = result[key].OrderBy(m => m.attribute.Order).ToList();

        return result;
    }

    /// <summary>
    /// インデックスが複合キーかどうかを判定する。
    /// </summary>
    public static bool IsCompositeIndex(string indexName, Type type)
    {
        var groups = GroupSecondaryKeyMembers(type);
        return groups.TryGetValue(indexName, out var members) && members.Count > 1;
    }

    /// <summary>
    /// レコード配列からSecondaryKeyインデックスを構築する（複合キー対応版）。
    /// </summary>
    public static IndexContainer BuildIndices<T>(T[] records)
    {
        var container = new IndexContainer();
        var type = typeof(T);
        var groups = GroupSecondaryKeyMembers(type);

        foreach (var (indexName, members) in groups)
        {
            IndexData indexData;
            var allowDuplicates = members[0].attribute.AllowDuplicates;

            if (members.Count == 1)
            {
                // 単一フィールドインデックス（既存処理）
                var (member, _) = members[0];
                var keyType = GetMemberType(member);
                indexData = BuildSingleFieldIndex(records, member, indexName, keyType, allowDuplicates);
            }
            else
            {
                // 複合インデックス（新規処理）
                indexData = BuildCompositeIndex(records, indexName, members, allowDuplicates);
            }

            container.SetIndex(indexData);
        }

        return container;
    }

    /// <summary>
    /// 複合インデックスを構築する。
    /// </summary>
    private static IndexData BuildCompositeIndex<T>(
        T[] records,
        string indexName,
        List<(MemberInfo member, SecondaryKeyAttribute attribute)> members,
        bool allowDuplicates)
    {
        var indexData = new IndexData(indexName, typeof(CompositeKeyValue));
        var keyGroups = new Dictionary<int, List<int>>();
        var keyStrings = new Dictionary<int, string>();

        // 各メンバーのゲッターを事前に作成
        var getters = members.Select(m => CreateGetter<T>(m.member)).ToArray();

        for (var i = 0; i < records.Length; i++)
        {
            var record = records[i];
            if (record == null)
                continue;

            // 複合キー値を生成
            var keyParts = new object[members.Count];
            var hasNull = false;
            for (var j = 0; j < members.Count; j++)
            {
                keyParts[j] = getters[j](record);
                if (keyParts[j] == null)
                    hasNull = true;
            }

            // null値を含む場合はスキップ
            if (hasNull)
                continue;

            var compositeKey = new CompositeKeyValue(keyParts);
            var keyHash = compositeKey.GetHashCode();
            var keyString = compositeKey.ToString();

            if (!keyGroups.TryGetValue(keyHash, out var indices))
            {
                indices = new List<int>();
                keyGroups[keyHash] = indices;
                keyStrings[keyHash] = keyString;
            }

            if (!allowDuplicates && indices.Count > 0)
            {
                Debug.LogWarning(
                    $"Duplicate CompositeSecondaryKey '{indexName}' value '{keyString}' at index {i}. " +
                    $"Set AllowDuplicates=true to allow multiple records per key.");
                continue;
            }

            indices.Add(i);
        }

        foreach (var (keyHash, indices) in keyGroups)
            indexData.AddEntry(keyHash, keyStrings[keyHash], indices.ToArray());

        return indexData;
    }

    // 既存の BuildIndex を BuildSingleFieldIndex にリネーム（内部メソッド）
    private static IndexData BuildSingleFieldIndex<T>(/* 既存の実装 */) { /* ... */ }
}
```

---

#### 2.4.4 TableAsset複合SecondaryKey検索API

**ファイル**: `Runtime/Core/TableAsset.cs`

**追加メソッド**:

```csharp
/// <summary>
/// 複合SecondaryKeyでレコードを検索する（O(1)）。
/// </summary>
/// <param name="indexName">インデックス名</param>
/// <param name="keyParts">複合キーの各パート</param>
/// <returns>見つかったレコード、見つからない場合はnull</returns>
public T FindBySecondaryKey(string indexName, params object[] keyParts)
{
    if (keyParts == null || keyParts.Length == 0)
        return null;

    // 単一キーの場合は既存メソッドを使用
    if (keyParts.Length == 1)
        return FindBySecondaryKey<object>(indexName, keyParts[0]);

    var compositeKey = new CompositeKeyValue(keyParts);
    var index = secondaryIndices.GetIndex(indexName);
    if (index == null)
    {
        Debug.LogWarning($"Index '{indexName}' not found");
        return null;
    }

    var recordIndices = index.FindByHash(compositeKey.GetHashCode());
    if (recordIndices.Length == 0)
        return null;

    return records[recordIndices[0]];
}

/// <summary>
/// 複合SecondaryKeyで複数のレコードを検索する（O(1)）。
/// </summary>
/// <param name="indexName">インデックス名</param>
/// <param name="keyParts">複合キーの各パート</param>
/// <returns>見つかったレコードの列挙</returns>
public IEnumerable<T> FindAllBySecondaryKey(string indexName, params object[] keyParts)
{
    if (keyParts == null || keyParts.Length == 0)
        yield break;

    int[] recordIndices;

    if (keyParts.Length == 1)
    {
        // 単一キー
        var index = secondaryIndices.GetIndex(indexName);
        if (index == null)
        {
            Debug.LogWarning($"Index '{indexName}' not found");
            yield break;
        }
        recordIndices = index.FindByKey(keyParts[0]);
    }
    else
    {
        // 複合キー
        var compositeKey = new CompositeKeyValue(keyParts);
        var index = secondaryIndices.GetIndex(indexName);
        if (index == null)
        {
            Debug.LogWarning($"Index '{indexName}' not found");
            yield break;
        }
        recordIndices = index.FindByHash(compositeKey.GetHashCode());
    }

    foreach (var i in recordIndices)
    {
        if (i >= 0 && i < records.Length)
            yield return records[i];
    }
}

/// <summary>
/// 複合SecondaryKeyで複数のレコードを検索し、配列として返す（O(1)）。
/// </summary>
public T[] FindAllBySecondaryKeyAsArray(string indexName, params object[] keyParts)
{
    return FindAllBySecondaryKey(indexName, keyParts).ToArray();
}
```

---

### 2.4.5 TableEditor対応

#### 2.4.5.1 IndexDefinition クラス新規作成

**ファイル**: `Editor/TableEditor/IndexDefinition.cs`（新規）

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// SecondaryKeyインデックスの定義。
    /// 単一カラムまたは複数カラムの複合インデックスをサポート。
    /// </summary>
    [Serializable]
    public class IndexDefinition
    {
        [SerializeField]
        private string name;

        [SerializeField]
        private List<string> columns = new();

        [SerializeField]
        private bool allowDuplicates = true;

        /// <summary>
        /// インデックス名。
        /// </summary>
        public string Name
        {
            get => name;
            set => name = value;
        }

        /// <summary>
        /// インデックスに含まれるカラム名（順序付き）。
        /// 1つの場合は単一インデックス、2つ以上の場合は複合インデックス。
        /// </summary>
        public List<string> Columns
        {
            get => columns;
            set => columns = value ?? new List<string>();
        }

        /// <summary>
        /// 同じキー値の複数レコードを許可するかどうか。
        /// </summary>
        public bool AllowDuplicates
        {
            get => allowDuplicates;
            set => allowDuplicates = value;
        }

        /// <summary>
        /// 複合インデックスかどうか。
        /// </summary>
        public bool IsComposite => columns.Count > 1;

        public IndexDefinition()
        {
            columns = new List<string>();
        }

        public IndexDefinition(string name, params string[] columnNames)
        {
            this.name = name;
            columns = new List<string>(columnNames);
        }

        public override string ToString()
        {
            var columnsStr = string.Join(", ", columns);
            return IsComposite
                ? $"{name} ({columnsStr}) [Composite]"
                : $"{name} ({columnsStr})";
        }
    }
}
```

---

#### 2.4.5.2 TableDefinition修正

**ファイル**: `Editor/TableEditor/TableDefinition.cs`

**変更内容**:
- `List<string> indices` を `List<IndexDefinition> indices` に変更
- 後方互換性のためのマイグレーションメソッド追加

```csharp
[Serializable]
public class TableDefinition
{
    [SerializeField]
    private string tableName;

    [SerializeField]
    private bool isReadOnly = true;

    [SerializeField]
    private List<ColumnDefinition> columns = new();

    [SerializeField]
    private List<IndexDefinition> indices = new();

    // --- 後方互換性用（旧形式からの変換） ---

    /// <summary>
    /// 旧形式（List&lt;string&gt;）のインデックスを新形式に変換する。
    /// </summary>
    public void MigrateFromLegacyIndices(List<string> legacyIndices)
    {
        if (legacyIndices == null)
            return;

        indices.Clear();
        foreach (var columnName in legacyIndices)
        {
            if (string.IsNullOrEmpty(columnName))
                continue;
            indices.Add(new IndexDefinition(columnName, columnName));
        }
    }

    // 以下既存のプロパティ（Indicesの型をList<IndexDefinition>に変更）
    public List<IndexDefinition> Indices
    {
        get => indices;
        set => indices = value;
    }

    // 他のプロパティは変更なし
}
```

---

#### 2.4.5.3 TableEditorWindow修正

**ファイル**: `Editor/TableEditor/TableEditorWindow.cs`

**変更内容**:
- 複合インデックス編集UIの追加
- インデックス名と複数カラム選択のサポート

```csharp
// 追加するフィールド
private ReorderableList indicesView;
private int selectedIndexForEdit = -1;

// CreateIndicesView メソッドを修正
private void CreateIndicesView()
{
    indicesView = new ReorderableList(tableDefinition.Indices, typeof(IndexDefinition));
    indicesView.drawElementCallback = DrawIndexElement;
    indicesView.drawHeaderCallback = rect => EditorGUI.LabelField(rect, $"Indices({tableDefinition.Indices.Count})");
    indicesView.onAddCallback = OnAddIndex;
    indicesView.onRemoveCallback = OnRemoveIndex;
    indicesView.elementHeightCallback = GetIndexElementHeight;
}

private float GetIndexElementHeight(int index)
{
    if (index < 0 || index >= tableDefinition.Indices.Count)
        return EditorGUIUtility.singleLineHeight;

    var indexDef = tableDefinition.Indices[index];
    // 基本行 + カラム数分の行
    var lineCount = 2 + indexDef.Columns.Count;
    return EditorGUIUtility.singleLineHeight * lineCount + 8f;
}

private void DrawIndexElement(Rect rect, int index, bool isActive, bool isFocused)
{
    if (index < 0 || index >= tableDefinition.Indices.Count)
        return;

    var indexDef = tableDefinition.Indices[index];
    var lineHeight = EditorGUIUtility.singleLineHeight;
    var y = rect.y + 2f;
    var padding = 4f;

    // インデックス名
    var nameRect = new Rect(rect.x, y, rect.width * 0.5f - padding, lineHeight);
    indexDef.Name = EditorGUI.TextField(nameRect, "名前", indexDef.Name);

    // AllowDuplicates
    var duplicateRect = new Rect(rect.x + rect.width * 0.5f, y, rect.width * 0.5f, lineHeight);
    indexDef.AllowDuplicates = EditorGUI.Toggle(duplicateRect, "重複許可", indexDef.AllowDuplicates);
    y += lineHeight + padding;

    // カラムリスト
    EditorGUI.LabelField(new Rect(rect.x, y, 100, lineHeight), "カラム:");
    y += lineHeight;

    for (var i = 0; i < indexDef.Columns.Count; i++)
    {
        var columnRect = new Rect(rect.x + 20, y, rect.width - 80, lineHeight);
        var removeRect = new Rect(rect.x + rect.width - 55, y, 50, lineHeight);

        // カラム選択ポップアップ
        var currentColumn = indexDef.Columns[i];
        var selectedIdx = System.Array.IndexOf(columnsCache, currentColumn);
        if (selectedIdx < 0)
            selectedIdx = 0;

        EditorGUI.BeginChangeCheck();
        selectedIdx = EditorGUI.Popup(columnRect, $"[{i}]", selectedIdx, columnsCache);
        if (EditorGUI.EndChangeCheck() && columnsCache.Length > 0)
            indexDef.Columns[i] = columnsCache[selectedIdx];

        // カラム削除ボタン
        if (GUI.Button(removeRect, "-"))
        {
            indexDef.Columns.RemoveAt(i);
            break;
        }

        y += lineHeight;
    }

    // カラム追加ボタン
    var addRect = new Rect(rect.x + 20, y, 100, lineHeight);
    if (GUI.Button(addRect, "+ カラム追加"))
    {
        if (columnsCache.Length > 0)
            indexDef.Columns.Add(columnsCache[0]);
    }
}

private void OnAddIndex(ReorderableList list)
{
    var newIndex = new IndexDefinition
    {
        Name = "NewIndex"
    };
    if (columnsCache.Length > 0)
        newIndex.Columns.Add(columnsCache[0]);
    tableDefinition.Indices.Add(newIndex);
}

private void OnRemoveIndex(ReorderableList list)
{
    if (list.index >= 0 && list.index < tableDefinition.Indices.Count)
        tableDefinition.Indices.RemoveAt(list.index);
}

// Validate メソッドに複合インデックスのバリデーション追加
private void Validate()
{
    if (tableDefinition.Columns.Count(column => column.IsPrimaryKey) != 1)
        EditorGUILayout.HelpBox("プライマリーキーは必ず1つ設定してください", MessageType.Error);

    if (columnsCache != null && columnsCache.Length != columnsCache.Distinct().Count())
        EditorGUILayout.HelpBox("同名のカラムは作成できません", MessageType.Error);

    // インデックス名の重複チェック
    var indexNames = tableDefinition.Indices.Select(i => i.Name).ToList();
    if (indexNames.Count != indexNames.Distinct().Count())
        EditorGUILayout.HelpBox("同名のインデックスは作成できません", MessageType.Error);

    // 複合インデックス内のカラム重複チェック
    foreach (var idx in tableDefinition.Indices)
    {
        if (idx.Columns.Count != idx.Columns.Distinct().Count())
            EditorGUILayout.HelpBox($"インデックス '{idx.Name}' に同じカラムが複数含まれています", MessageType.Error);
    }
}
```

---

#### 2.4.5.4 ClassGenerator修正

**ファイル**: `Editor/TableEditor/ClassGenerator.cs`

**変更内容**:
- 複合インデックス用のSecondaryKey属性生成
- Order指定の対応

```csharp
private static string GenerateField(TableDefinition tableDefinition, ColumnDefinition column)
{
    var type = ConvertType(column.Type, column.IsNullable);
    var csvColumnName = column.Name;
    var fieldName = column.Name.SnakeToCamelCase();
    var sb = new StringBuilder();

    var attributes = new List<string>()
    {
        "SerializeField",
        $"CsvColumn(\"{csvColumnName}\")"
    };

    if (column.IsPrimaryKey)
        attributes.Add("PrimaryKey");

    // SecondaryKey属性の生成（複合インデックス対応）
    foreach (var indexDef in tableDefinition.Indices)
    {
        var columnIndex = indexDef.Columns.IndexOf(column.Name);
        if (columnIndex < 0)
            continue;

        if (indexDef.IsComposite)
        {
            // 複合インデックス: 名前とOrder指定
            var attrParts = new List<string> { $"\"{indexDef.Name}\"", columnIndex.ToString() };
            if (!indexDef.AllowDuplicates)
                attrParts.Add("AllowDuplicates = false");
            attributes.Add($"SecondaryKey({string.Join(", ", attrParts)})");
        }
        else
        {
            // 単一インデックス
            if (indexDef.Name == column.Name)
            {
                // 名前がカラム名と同じ場合は省略可能
                attributes.Add(indexDef.AllowDuplicates ? "SecondaryKey" : "SecondaryKey(AllowDuplicates = false)");
            }
            else
            {
                // 名前を明示的に指定
                var attrParts = new List<string> { $"\"{indexDef.Name}\"" };
                if (!indexDef.AllowDuplicates)
                    attrParts.Add("AllowDuplicates = false");
                attributes.Add($"SecondaryKey({string.Join(", ", attrParts)})");
            }
        }
    }

    sb.Append($"        [{string.Join(", ", attributes)}]\n");
    sb.Append($"        private {type} {fieldName};");

    return sb.ToString();
}
```

---

#### 2.4.5.5 DefinitionLoader修正

**ファイル**: `Editor/TableEditor/DefinitionLoader.cs`

**変更内容**:
- YAML形式の複合インデックス対応
- 後方互換性（旧形式からの自動マイグレーション）

```yaml
# 新しいYAML形式の例
table_name: equipment
is_read_only: true
columns:
  - name: id
    type: int
    is_primary_key: true
  - name: rarity
    type: RarityType
  - name: element
    type: ElementType
  - name: category
    type: string

indices:
  # 複合インデックス
  - name: RarityElement
    columns:
      - rarity
      - element
    allow_duplicates: true

  # 単一インデックス
  - name: category
    columns:
      - category
```

---

### 2.4.6 テスト

#### 2.4.6.1 複合SecondaryKeyテスト

**ファイル**: `Tests/Index/CompositeSecondaryKeyTests.cs`（新規）

**テストケース**:
- 同一インデックス名のSecondaryKeyがグループ化される
- Order順序の正確性
- 複合インデックス構築の正確性
- 複合キー検索の正確性
- 単一キーとの混在
- AllowDuplicatesの動作
- null値を含むレコードのスキップ

#### 2.4.6.2 TableEditor統合テスト

**ファイル**: `Tests/Editor/CompositeIndexEditorTests.cs`（新規）

**テストケース**:
- IndexDefinitionの作成・編集
- TableDefinitionの複合インデックス保持
- ClassGeneratorの複合インデックス属性生成
- YAML入出力の整合性

#### 2.4.6.3 パフォーマンステスト

**ファイル**: `Tests/Index/CompositeIndexPerformanceTests.cs`（新規）

**テストケース**:
- 10万件での複合インデックス構築時間
- 10万件での複合キー検索時間（< 1ms）
- メモリ使用量の確認

---

## API設計

### 使用例

```csharp
// レコード定義
[Serializable]
public class EquipmentRecord
{
    [SerializeField, PrimaryKey]
    private int id;

    // 複合インデックス "RarityElement" の第1フィールド
    [SerializeField, SecondaryKey("RarityElement", 0)]
    private RarityType rarity;

    // 複合インデックス "RarityElement" の第2フィールド
    [SerializeField, SecondaryKey("RarityElement", 1)]
    private ElementType element;

    // 単一インデックス（従来通り）
    [SerializeField, SecondaryKey]
    private string category;

    [SerializeField]
    private string name;
}

// 検索
var table = DB.Get<EquipmentTable>();

// 複合キー検索
var ssrFireItems = table.FindAllBySecondaryKey("RarityElement", RarityType.SSR, ElementType.Fire);

// 単一キー検索（従来通り）
var weapons = table.FindAllBySecondaryKey<string>("category", "Weapon");
```

---

## パフォーマンス目標

| 操作 | 目標 | 備考 |
|------|------|------|
| 複合SecondaryKey検索 | O(1) | ハッシュベース |
| 単一SecondaryKey検索 | O(1) | 既存動作維持 |
| インデックス構築 | O(n) | レコード走査 |
| GC Allocation（検索時） | 最小限 | paramsによる配列確保は許容 |

---

## 依存関係

```
Phase 2.4 複合インデックス
    │
    ├── 2.4.1 SecondaryKeyAttribute拡張
    │
    ├── 2.4.2 CompositeKeyValue構造体
    │
    ├── 2.4.3 IndexBuilder複合インデックス対応
    │          ↑ 依存: 2.4.1, 2.4.2
    │
    ├── 2.4.4 TableAsset複合SecondaryKey検索API
    │          ↑ 依存: 2.4.2, 2.4.3
    │
    ├── 2.4.5 TableEditor対応
    │   ├── 2.4.5.1 IndexDefinitionクラス
    │   ├── 2.4.5.2 TableDefinition修正
    │   │          ↑ 依存: 2.4.5.1
    │   ├── 2.4.5.3 TableEditorWindow修正
    │   │          ↑ 依存: 2.4.5.1, 2.4.5.2
    │   ├── 2.4.5.4 ClassGenerator修正
    │   │          ↑ 依存: 2.4.1, 2.4.5.2
    │   └── 2.4.5.5 DefinitionLoader修正
    │              ↑ 依存: 2.4.5.2
    │
    └── 2.4.6 テスト
        ├── 2.4.6.1 複合SecondaryKeyテスト
        │          ↑ 依存: 2.4.1〜2.4.4完了
        ├── 2.4.6.2 TableEditor統合テスト
        │          ↑ 依存: 2.4.5完了
        └── 2.4.6.3 パフォーマンステスト
                   ↑ 依存: 2.4.3, 2.4.4完了
```

---

## 前提条件

- Phase 2の基本SecondaryKey実装が完了していること
- `IndexBuilder`、`IndexData`、`IndexContainer`が正常に動作すること
- `TableAsset<T, TKey>`の基本検索機能が実装されていること

---

## 成果物

### 新規ファイル

| ファイル | 説明 |
|---------|------|
| `Runtime/Core/CompositeKeyValue.cs` | 複合キー値構造体 |
| `Editor/TableEditor/IndexDefinition.cs` | インデックス定義クラス |
| `Tests/Index/CompositeSecondaryKeyTests.cs` | 複合SecondaryKeyテスト |
| `Tests/Editor/CompositeIndexEditorTests.cs` | TableEditor統合テスト |
| `Tests/Index/CompositeIndexPerformanceTests.cs` | パフォーマンステスト |

### 修正ファイル

| ファイル | 修正内容 |
|---------|---------|
| `Runtime/Schema/Attributes/SecondaryKeyAttribute.cs` | Orderプロパティ追加 |
| `Runtime/Index/IndexBuilder.cs` | 複合インデックス構築ロジック追加 |
| `Runtime/Core/TableAsset.cs` | 複合キー検索API追加 |
| `Editor/TableEditor/TableDefinition.cs` | indices型変更 |
| `Editor/TableEditor/TableEditorWindow.cs` | 複合インデックス編集UI |
| `Editor/TableEditor/ClassGenerator.cs` | 複合インデックス属性生成 |
| `Editor/TableEditor/DefinitionLoader.cs` | YAML形式対応 |
| `Editor/Index/IndexRebuildProcessor.cs` | 複合インデックス再構築対応 |

---

## 完了条件

- [ ] 同一名のSecondaryKeyが複合インデックスとしてグループ化される
- [ ] 複合SecondaryKey検索がO(1)で動作する
- [ ] 単一SecondaryKeyの既存動作が維持される
- [ ] 10万件で検索が1ms以内
- [ ] TableEditorで複合インデックスの定義・編集ができる
- [ ] ClassGeneratorで正しい属性が生成される
- [ ] YAML入出力が正常に動作する
- [ ] 全ユニットテストが通る

---

## 補足: 設計上の考慮事項

### ハッシュ衝突への対応

複合キーのハッシュ値は衝突する可能性があります。現在の`IndexData`実装では、同一ハッシュ値に複数のレコードインデックスがマッピングされる設計になっているため、ハッシュ衝突時でも正確な検索結果を返すことができます。

より厳密な検索が必要な場合は、ハッシュ検索後に実際の値を比較する二段階検索を実装することも検討できます。

### 既存コードとの互換性

本実装は既存の単一SecondaryKeyインデックス機能と完全に互換性があります：

- 既存の`[SecondaryKey]`属性は変更なしで動作
- 既存の`FindBySecondaryKey<T>()`メソッドは維持
- 新しい`params object[]`オーバーロードは追加API

### TableEditorの後方互換性

- 旧形式（`List<string>`）のYAMLファイルは自動的に新形式に変換される
- 変換されたインデックスは単一カラムインデックスとして扱われる

---

## 付録: チェックリスト

### 2.4.1 SecondaryKeyAttribute拡張
- [ ] Orderプロパティ追加
- [ ] コンストラクタオーバーロード追加
- [ ] XMLドキュメント更新

### 2.4.2 CompositeKeyValue構造体
- [ ] 構造体実装
- [ ] ハッシュコード計算
- [ ] 等値比較

### 2.4.3 IndexBuilder複合インデックス対応
- [ ] GroupSecondaryKeyMembersメソッド
- [ ] BuildCompositeIndexメソッド
- [ ] BuildIndices修正

### 2.4.4 TableAsset複合SecondaryKey検索API
- [ ] FindBySecondaryKey(params)オーバーロード
- [ ] FindAllBySecondaryKey(params)オーバーロード
- [ ] FindAllBySecondaryKeyAsArray(params)オーバーロード

### 2.4.5 TableEditor対応
- [ ] IndexDefinitionクラス作成
- [ ] TableDefinition修正
- [ ] TableEditorWindow修正
- [ ] ClassGenerator修正
- [ ] DefinitionLoader修正

### 2.4.6 テスト
- [ ] 複合SecondaryKeyテスト
- [ ] TableEditor統合テスト
- [ ] パフォーマンステスト
