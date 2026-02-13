# QueryResult (Zero GC) サンプル

GCアロケーションを発生させないクエリパターンを実演するサンプルです。

## 概要

このサンプルでは以下の機能を学習できます：

- `QueryResult<T>` (ref struct) の使い方
- `using` 文による適切なリソース管理
- GCアロケーションを避けるパターン
- パフォーマンス比較

## セットアップ

1. Package Managerからこのサンプルをインポート
2. `Assets > Create > XScriptableDB > Samples > QueryResultZeroGC > EnemyTable` でテーブルを作成
3. `Data/enemies.csv` をインポート

## ファイル構成

```
QueryResultZeroGC/
├── Scripts/
│   ├── EnemyRecord.cs
│   ├── EnemyTable.cs
│   ├── QueryResultZeroGCSample.cs
│   └── PerformanceProfiler.cs
├── Data/
│   └── enemies.csv               # 100件の敵データ
└── README.md
```

## 基本的な使い方

### Zero GC パターン（QueryBySecondaryKey、推奨）

```csharp
// QueryBySecondaryKeyはQueryResult<T>（ref struct）を返す
using var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);

// ref readonlyで値コピーを回避
foreach (ref readonly var enemy in results)
{
    Debug.Log(enemy.Name);
}
```

### Where パターン（IEnumerable）

```csharp
// Where()はIEnumerable<T>を返す（using不要）
var results = table.Where(r => r.Level >= 10 && r.Level <= 50);

foreach (var enemy in results)
{
    Debug.Log(enemy.Name);
}
```

### 避けるべきパターン

```csharp
// ❌ ToList()でGCアロケーション発生
var list = table.All.Where(r => r.Level >= 10).ToList();
```

## パフォーマンス比較

```csharp
var sample = GetComponent<QueryResultZeroGCSample>();

// ベンチマーク実行
var result = sample.RunBenchmark(1000);
Debug.Log(result);

// 出力例:
// === Zero GC (QueryResult) ===
// Average: 0.042ms
// Memory: 0 bytes
//
// === 通常LINQ (ToList) ===
// Average: 0.156ms
// Memory: 2,400,000 bytes
//
// Speedup: 3.71x
```

## 検索方式の比較

| 方式 | 計算量 | GCアロケーション | 用途 |
|------|--------|-----------------|------|
| PrimaryKey検索 | O(log n) | 0 bytes | ID指定検索 |
| SecondaryKey検索 | O(1) | 0 bytes | カテゴリ等での検索 |
| QueryBySecondaryKey (Zero GC) | O(1) | 0 bytes | SecondaryKeyでの検索 |
| Where (IEnumerable) | O(n) | 最小限 | 複雑な条件検索 |
| Where + ToList | O(n) | ~数KB | 結果を保持したい場合 |

## ユースケース

### 毎フレームの検索

```csharp
void Update()
{
    // Where()はIEnumerable<T>を返す
    var nearby = enemyTable.Where(e =>
        Vector3.Distance(e.Position, player.Position) < range);

    foreach (var enemy in nearby)
    {
        // AI処理
    }
}
```

### SecondaryKeyによるZero GC検索

```csharp
// QueryBySecondaryKeyでQueryResult<T>（ref struct、Zero GC）を取得
using var areaEnemies = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);

foreach (ref readonly var enemy in areaEnemies)
{
    // エリアの敵を処理
}

// FindAllBySecondaryKeyはIEnumerable<T>を返す
var bosses = table.FindAllBySecondaryKey("areaId", 2).Where(e => e.IsBoss);
```

## 注意事項

1. `QueryResult` は `ref struct` なので必ず `using` で囲む（`QueryBySecondaryKey` で取得）
2. `foreach (ref readonly var item in results)` で値コピーを回避（`QueryResult` のみ）
3. `ref struct` はフィールドに保持できない
4. `async/await` とは併用できない
5. `Where()` は `IEnumerable<T>` を返すため、`using` や `ref readonly` は不要
6. 結果を保持したい場合は `FindAllBySecondaryKeyAsArray()` で `T[]` を取得
