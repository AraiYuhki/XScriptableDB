# QueryResult ガイド

## 概要

`QueryResult<T>` は、クエリ結果を格納するためのref structです。GC Allocationを発生させずに、効率的にクエリ結果を扱うことができます。

## なぜref structなのか

通常のLINQクエリでは、結果を格納するためにヒープメモリが確保されます。これはゲーム開発において、フレーム毎のGC Allocationを引き起こす原因となります。

`QueryResult<T>` はスタック上に確保されるため、GC Allocationが発生しません。

```csharp
// 従来のLINQ - GC Allocが発生
var list = records.Where(r => r.IsActive).ToList();

// QueryBySecondaryKey - GC Alloc 0
var result = table.QueryBySecondaryKey("Type", "Slime");
```

## QueryResultの取得方法

`QueryResult<T>` は `QueryBySecondaryKey()` メソッドから取得します。

```csharp
// SecondaryKeyによるGC Alloc 0の検索
var result = table.QueryBySecondaryKey("Type", "Slime");
```

> **注意**: `Where()` メソッドは `IEnumerable<T>` を返します。`QueryResult<T>` ではありません。

## 基本的な使い方

### QueryBySecondaryKey

```csharp
// SecondaryKeyで検索し、QueryResultを取得
var result = table.QueryBySecondaryKey("Type", "Slime");

// 件数を確認
Debug.Log($"Found: {result.Count} records");

// 列挙
foreach (var record in result)
{
    Debug.Log(record.Name);
}
```

### Where句（IEnumerable版）

`Where()` は `IEnumerable<T>` を返すため、通常のforeachで使用します。

```csharp
// 条件に一致するレコードを検索（IEnumerable<T>を返す）
var result = table.Where(r => r.Price > 100);

// 通常のforeachで列挙
foreach (var record in result)
{
    Debug.Log(record.Name);
}
```

## プロパティとメソッド

### Count

```csharp
var result = table.QueryBySecondaryKey("AreaId", 5);
int count = result.Count;
```

### IsEmpty

```csharp
var result = table.QueryBySecondaryKey("Type", "Dragon");
if (!result.IsEmpty)
{
    Debug.Log("Dragons found!");
}
```

### First

```csharp
var result = table.QueryBySecondaryKey("Type", "Slime");
var first = result.First;  // 最初のレコード、空の場合はnull
```

### インデクサ

```csharp
var result = table.QueryBySecondaryKey("Type", "Goblin");
var first = result[0];
var last = result[result.Count - 1];
```

### GetEnumerator

```csharp
var result = table.QueryBySecondaryKey("Type", "Goblin");
foreach (var record in result)
{
    Debug.Log(record.Name);
}
```

## 拡張メソッド（テーブルレベル）

以下のメソッドはテーブルに対して直接使用できます。

### FirstOrDefault

```csharp
// 条件に一致する最初のレコードを取得
var enemy = table.FirstOrDefault(r => r.Name == "Goblin");
if (enemy != null)
{
    Debug.Log(enemy.Hp);
}
```

### Any

```csharp
// 条件に一致するレコードが存在するか
bool hasStrongEnemy = table.Any(r => r.Attack > 100);
```

### All

```csharp
// 全てのレコードが条件を満たすか
bool allActive = table.All(r => r.IsActive);
```

### Count

```csharp
// 条件に一致するレコード数
int weakEnemyCount = table.Count(r => r.Hp < 50);
```

### Select

```csharp
// 射影（変換）- IEnumerable<TResult>を返す
var names = table.Select(r => r.Name);
foreach (var name in names)
{
    Debug.Log(name);
}
```

### Skip / Take

```csharp
// ページング - IEnumerable<T>を返す
var page = table.Skip(10).Take(10);  // 11-20件目
```

## ToList() / ToArray()

結果を永続化する必要がある場合は、`ToList()` や `ToArray()` を使用します。ただし、これはGC Allocが発生します。

```csharp
// QueryResultから配列に変換（GC Allocが発生）
var result = table.QueryBySecondaryKey("Type", "Goblin");
var array = result.ToArray();

// QueryResultからリストに変換（GC Allocが発生）
var list = result.ToList();

// Whereの場合はLINQのToList/ToArrayを使用（GC Allocが発生）
List<Item> items = table.Where(r => r.IsActive).ToList();
Item[] itemArray = table.Where(r => r.IsActive).ToArray();
```

## 注意事項

### ref structの制約

ref structには以下の制約があります：

```csharp
// NG: クラスのフィールドに保存できない
class MyClass
{
    QueryResult<Item> result;  // コンパイルエラー
}

// NG: ラムダ式内で使用できない
Action action = () =>
{
    var r = result[0];  // コンパイルエラー
};

// NG: asyncメソッド内で使用できない
async Task ProcessAsync()
{
    var result = table.QueryBySecondaryKey("Type", "Slime");  // コンパイルエラー
}
```

### Where() と QueryBySecondaryKey() の違い

| メソッド | 戻り値の型 | GC Alloc | 用途 |
|----------|-----------|----------|------|
| `Where()` | `IEnumerable<T>` | 発生する | 条件によるフィルタリング |
| `QueryBySecondaryKey()` | `QueryResult<T>` | 0 | SecondaryKeyによる高速検索 |

## パフォーマンスのヒント

### 早期リターン

```csharp
// 最初の1件だけ必要な場合は FirstOrDefault を使用
var first = table.FirstOrDefault(r => r.Name == "Target");

// 存在確認だけなら Any を使用
if (table.Any(r => r.IsActive))
{
    // ...
}
```

### 条件の順序

```csharp
// 高速な条件を先に書く
var result = table.Where(r =>
    r.IsActive &&      // bool比較は高速
    r.Type == "A" &&   // enum/string比較
    ExpensiveCheck(r)  // 重い処理は最後
);
```

### インデックスの活用

SecondaryKeyで絞り込んでからフィルタリング：

```csharp
// SecondaryKeyで絞り込み（GC Alloc 0）
var result = table.QueryBySecondaryKey("Category", "Weapon");

// 配列に変換してからLINQで追加フィルタ
var expensiveWeapons = result.ToArray().Where(r => r.Price > 1000);
```
