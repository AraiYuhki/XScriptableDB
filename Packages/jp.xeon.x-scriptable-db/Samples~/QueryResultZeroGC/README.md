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

### Zero GC パターン（推奨）

```csharp
// using文でQueryResultを囲む
using var results = table.Where(r => r.Level >= 10 && r.Level <= 50);

// ref readonlyで値コピーを回避
foreach (ref readonly var enemy in results)
{
    Debug.Log(enemy.Name);
}
```

### 避けるべきパターン

```csharp
// ❌ ToList()でGCアロケーション発生
var list = table.All.Where(r => r.Level >= 10).ToList();

// ❌ usingなしで使用
var results = table.Where(r => r.Level >= 10);

// ❌ 値コピーが発生
foreach (var enemy in results)  // ref readonly なし
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
| Where (Zero GC) | O(n) | 0 bytes | 複雑な条件検索 |
| Where + ToList | O(n) | ~数KB | 結果を保持したい場合 |

## ユースケース

### 毎フレームの検索

```csharp
void Update()
{
    // 毎フレーム実行してもGCスパイクが発生しない
    using var nearby = enemyTable.Where(e =>
        Vector3.Distance(e.Position, player.Position) < range);

    foreach (ref readonly var enemy in nearby)
    {
        // AI処理
    }
}
```

### SecondaryKeyとの組み合わせ

```csharp
// SecondaryKeyで高速フィルタ後、さらに条件絞り込み
var areaEnemies = table.FindAllBySecondaryKey("areaId", 2);
using var bosses = areaEnemies.Where(e => e.IsBoss);
```

## 注意事項

1. `QueryResult` は `ref struct` なので必ず `using` で囲む
2. `foreach (ref readonly var item in results)` で値コピーを回避
3. `ref struct` はフィールドに保持できない
4. `async/await` とは併用できない
5. 結果を保持したい場合のみ `ToList()` を使用
