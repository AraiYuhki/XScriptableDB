# QueryResult ガイド

## 概要

`QueryResult<T>` は、クエリ結果を格納するためのref structです。GC Allocationを発生させずに、効率的にクエリ結果を扱うことができます。

## なぜref structなのか

通常のLINQクエリでは、結果を格納するためにヒープメモリが確保されます。これはゲーム開発において、フレーム毎のGC Allocationを引き起こす原因となります。

`QueryResult<T>` はスタック上に確保されるため、GC Allocationが発生しません。

```csharp
// 従来のLINQ - GC Allocが発生
var list = records.Where(r => r.IsActive).ToList();

// QueryResult - GC Alloc 0
using var result = table.Where(r => r.IsActive);
```

## 基本的な使い方

### Where句

```csharp
// 条件に一致するレコードを検索
using var result = table.Where(r => r.Price > 100);

// 件数を確認
Debug.Log($"Found: {result.Count} records");

// 列挙
foreach (ref readonly var record in result)
{
    Debug.Log(record.Name);
}
```

### usingステートメント

`QueryResult` は `IDisposable` を実装しているため、`using` ステートメントを使用します。

```csharp
// 推奨: usingステートメント
using var result = table.Where(r => r.IsActive);
// resultはスコープ終了時に自動的にDispose

// または明示的なDispose
var result = table.Where(r => r.IsActive);
try
{
    // 処理
}
finally
{
    result.Dispose();
}
```

## プロパティとメソッド

### Count

```csharp
using var result = table.Where(r => r.Hp > 100);
int count = result.Count;
```

### インデクサ

```csharp
using var result = table.Where(r => r.IsActive);
var first = result[0];
var last = result[result.Count - 1];
```

### GetEnumerator

```csharp
using var result = table.Where(r => r.IsActive);
foreach (ref readonly var record in result)
{
    // ref readonlyでコピーを避ける
    Debug.Log(record.Name);
}
```

## 拡張メソッド

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
// 射影（変換）
using var names = table.Select(r => r.Name);
foreach (var name in names)
{
    Debug.Log(name);
}
```

### Skip / Take

```csharp
// ページング
using var result = table.Where(r => r.IsActive);
using var page = result.Skip(10).Take(10);  // 11-20件目
```

## チェーン

拡張メソッドをチェーンして複雑なクエリを構築できます。

```csharp
// アクティブなレコードの中から、価格が100以上のものを
// 価格順にソートして、上位5件を取得
using var result = table
    .Where(r => r.IsActive && r.Price >= 100)
    .OrderBy(r => r.Price)
    .Take(5);
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
    using var result = table.Where(r => r.IsActive);  // コンパイルエラー
}
```

### ToList() / ToArray()

結果を永続化する必要がある場合は、`ToList()` や `ToArray()` を使用します。ただし、これはGC Allocが発生します。

```csharp
// GC Allocが発生するが、結果を保存できる
List<Item> items = table.Where(r => r.IsActive).ToList();

// 配列として取得
Item[] itemArray = table.Where(r => r.IsActive).ToArray();
```

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
using var result = table.Where(r =>
    r.IsActive &&      // bool比較は高速
    r.Type == "A" &&   // enum/string比較
    ExpensiveCheck(r)  // 重い処理は最後
);
```

### インデックスの活用

SecondaryKeyで絞り込んでからQueryを使用：

```csharp
// SecondaryKeyで絞り込み → 追加条件でフィルタ
var weapons = table.FindAllBySecondaryKey("Category", "Weapon");
using var result = weapons.AsQueryResult().Where(r => r.Price > 1000);
```
