# TableAsset ガイド

## 概要

`TableAsset<TRecord, TKey>` は、XScriptableDBの中核となるクラスです。ScriptableObjectを継承しており、Unityのアセットシステムと完全に統合されています。

## 基本的な使い方

### 1. レコードクラスの定義

まず、テーブルに格納するデータの構造を定義します。

```csharp
using System;
using Xeon.XScriptableDB;

[Serializable]
public class EnemyRecord
{
    [PrimaryKey]
    public int Id;

    public string Name;
    public int Hp;
    public int Attack;
    public int Defense;
}
```

### 2. TableAssetクラスの作成

レコードクラスを使用して、具体的なテーブルクラスを作成します。

```csharp
using UnityEngine;
using Xeon.XScriptableDB;

[CreateAssetMenu(fileName = "EnemyTable", menuName = "Database/EnemyTable")]
public class EnemyTable : TableAsset<EnemyRecord, int>
{
}
```

### 3. テーブルアセットの作成

Unityエディタで:
1. Projectウィンドウで右クリック
2. `Create > Database > EnemyTable` を選択
3. 作成されたアセットにデータを入力

## PrimaryKey

`[PrimaryKey]` 属性を付けたフィールドは、レコードを一意に識別するキーとなります。

### 特徴

- **一意性**: 同じキーを持つレコードは存在できません
- **高速検索**: バイナリサーチによるO(log n)の検索
- **型の制限**: IComparableを実装した型（int, string, enum等）

### 検索メソッド

```csharp
// キーで検索（見つからない場合はnull）
var enemy = enemyTable.FindByKey(1001);

// 安全な検索（見つかったかどうかを返す）
if (enemyTable.TryFindByKey(1001, out var enemy))
{
    Debug.Log(enemy.Name);
}

// インデクサでも検索可能
var enemy = enemyTable[1001];
```

## SecondaryKey

`[SecondaryKey]` 属性を付けたフィールドは、副キーとしてインデックス化されます。

### 特徴

- **高速検索**: ハッシュインデックスによるO(1)の検索
- **複数値**: 同じキー値を持つ複数のレコードを検索可能
- **複数設定可能**: 1つのレコードに複数のSecondaryKeyを設定可能

### 使用例

```csharp
[Serializable]
public class EnemyRecord
{
    [PrimaryKey]
    public int Id;

    [SecondaryKey]
    public string Type;  // "Slime", "Goblin", "Dragon" など

    [SecondaryKey]
    public int AreaId;   // 出現エリアID

    public string Name;
    public int Hp;
}
```

### 検索メソッド

```csharp
// SecondaryKeyで1件検索
var slime = enemyTable.FindBySecondaryKey("Type", "Slime");

// SecondaryKeyで複数件検索
var goblins = enemyTable.FindAllBySecondaryKey("Type", "Goblin");

// TryFind版
if (enemyTable.TryFindBySecondaryKey("AreaId", 5, out var enemy))
{
    Debug.Log(enemy.Name);
}
```

## テーブルのプロパティ

```csharp
// レコード数
int count = table.Count;

// 全レコード（IEnumerable<TRecord>）
foreach (var record in table.Records)
{
    Debug.Log(record.Name);
}

// レコードの型
Type recordType = table.RecordType;

// キーの型
Type keyType = table.KeyType;
```

## エディタ機能（UNITY_EDITOR内のみ）

```csharp
#if UNITY_EDITOR
// 新しいレコードを作成
var newRecord = table.CreateNewRecord();

// レコードを追加
table.AddRecordObject(newRecord);

// インデックスでレコードを削除
table.RemoveRecordAt(0);

// 重複キーをチェック
var duplicates = table.FindDuplicateKeysAsObjects();
#endif
```

## ベストプラクティス

### PrimaryKeyの選択

```csharp
// Good: 不変の識別子
[PrimaryKey]
public int Id;

// Good: 列挙型
[PrimaryKey]
public ItemType Type;

// Avoid: 変更される可能性のある値
[PrimaryKey]
public string Name;  // 名前は変更される可能性がある
```

### SecondaryKeyの活用

```csharp
// 頻繁に検索するフィールドにSecondaryKeyを設定
[SecondaryKey]
public string Category;  // カテゴリ別検索が多い場合

[SecondaryKey]
public int Level;  // レベル別検索が多い場合
```

### メモリ効率

```csharp
// 大量のデータを扱う場合は、必要なときだけ読み込む
[SerializeField]
private EnemyTable enemyTable;

private void Start()
{
    // Addressablesを使用した遅延読み込みも検討
}
```
