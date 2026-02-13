# QueryResult (Zero GC) サンプル仕様書

## 概要

GCアロケーションを発生させないクエリパターンを示すサンプルです。
ランタイムでの高頻度なデータ検索において、パフォーマンスを最大化する方法を学習します。

## 学習目標

1. `QueryResult<T>` (ref struct) の使い方
2. `using` 文による適切なリソース管理
3. GCアロケーションを避けるパターン
4. パフォーマンス比較（Zero GC vs 通常のLINQ）
5. プロファイリングによる検証方法

---

## 技術的背景

### 従来のLINQ（GCアロケーションあり）

```csharp
// 毎フレーム呼び出すとGCが発生
var results = table.All.Where(r => r.IsActive).ToList();
foreach (var item in results) { ... }
```

問題点:
- `Where()` がEnumerableを生成
- `ToList()` が新しいListを生成
- 毎フレーム呼び出すとGCスパイクの原因に

### QueryResult（Zero GC）

```csharp
// QueryBySecondaryKeyはQueryResult<T>（ref struct）を返す（GCアロケーションなし）
using var results = table.QueryBySecondaryKey<EnemyRecord, int, bool>("isBoss", true);
foreach (ref readonly var item in results) { ... }
```

特徴:
- `ref struct` による値型のみの処理
- 内部バッファの再利用
- `using` で確実にリソース解放

### Where（IEnumerable）

```csharp
// Where()はIEnumerable<T>を返す（using不要）
var results = table.Where(r => r.IsActive);
foreach (var item in results) { ... }
```

特徴:
- 柔軟な条件指定が可能
- GCアロケーションはToList()しなければ最小限

---

## データ構造

### EnemyRecord（敵データ）

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| id | int | 敵ID（主キー） |
| name | string | 敵名 |
| hp | int | HP |
| attack | int | 攻撃力 |
| defense | int | 防御力 |
| level | int | レベル（SecondaryKey） |
| areaId | int | 出現エリアID（SecondaryKey） |
| isBoss | bool | ボスフラグ（SecondaryKey） |
| dropRate | float | ドロップ率 |

### 大量データ

パフォーマンス検証用に10,000件以上のレコードを用意。

---

## サンプルデータ

### enemies.csv（抜粋）

```csv
ID,名前,HP,攻撃力,防御力,レベル,エリアID,ボス,ドロップ率
1,スライム,10,3,1,1,1,False,0.5
2,ゴブリン,30,8,3,3,1,False,0.4
3,オーク,80,15,8,5,2,False,0.3
4,ゴブリンキング,200,30,20,10,1,True,0.8
...
10000,ダークドラゴン,9999,500,400,99,10,True,1.0
```

---

## GUI要件

### メインパネル

```
┌─────────────────────────────────────────────────────────┐
│ QueryResult (Zero GC) サンプル                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ ─── クエリ実行 ─────────────────────────────────────   │
│                                                         │
│ クエリタイプ:                                           │
│   ○ レベル範囲検索                                     │
│   ○ エリア検索                                         │
│   ○ ボスのみ                                           │
│   ○ 複合条件                                           │
│                                                         │
│ パラメータ:                                             │
│   最小レベル: [___10___]  最大レベル: [___50___]       │
│   エリアID: [____2____]                                │
│                                                         │
│ 実行モード:                                             │
│   ○ Zero GC (QueryResult)                              │
│   ○ 通常LINQ (ToList)                                  │
│                                                         │
│ [1回実行]  [100回ループ]  [1000回ループ]               │
│                                                         │
│ ─── 実行結果 ───────────────────────────────────────   │
│                                                         │
│ 件数: 523件                                             │
│ 実行時間: 0.042ms                                       │
│ GCアロケーション: 0 bytes                               │
│                                                         │
│ ─── パフォーマンス比較 ─────────────────────────────   │
│                                                         │
│ [ベンチマーク実行]                                      │
│                                                         │
│ ┌───────────────────────────────────────────────────┐   │
│ │ 方式          │ 1回あたり │ 1000回合計 │ GC       │   │
│ ├───────────────┼───────────┼────────────┼──────────┤   │
│ │ Zero GC       │ 0.042ms   │ 42ms       │ 0 bytes  │   │
│ │ 通常LINQ      │ 0.156ms   │ 156ms      │ 2.4 MB   │   │
│ │ SecondaryKey  │ 0.001ms   │ 1ms        │ 0 bytes  │   │
│ └───────────────┴───────────┴────────────┴──────────┘   │
│                                                         │
│ ─── 検索結果プレビュー（先頭10件）───────────────────   │
│                                                         │
│ ┌────┬──────────┬─────┬─────┬─────┬──────┬──────┐       │
│ │ ID │ 名前     │ HP  │ ATK │ DEF │ Lv   │ Area │       │
│ ├────┼──────────┼─────┼─────┼─────┼──────┼──────┤       │
│ │ 15 │ ウルフ   │ 50  │ 12  │ 5   │ 10   │ 2    │       │
│ │ 23 │ オーガ   │ 120 │ 25  │ 15  │ 15   │ 2    │       │
│ │ ...│ ...      │ ... │ ... │ ... │ ...  │ ...  │       │
│ └────┴──────────┴─────┴─────┴─────┴──────┴──────┘       │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### メモリプロファイラ表示

```
┌─────────────────────────────────────────────────────────┐
│ メモリ使用状況                                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ 現在のヒープサイズ: 64.2 MB                             │
│ 使用中: 48.5 MB                                         │
│                                                         │
│ GCコレクション回数:                                     │
│   Gen0: 15回  Gen1: 3回  Gen2: 0回                     │
│                                                         │
│ [GC.Collect実行]  [カウンターリセット]                  │
│                                                         │
│ ─── リアルタイムグラフ ───                             │
│                                                         │
│ MB                                                      │
│ 50 ┤     ╭─╮                                           │
│ 40 ┤  ╭──╯ ╰──╮                                        │
│ 30 ┤──╯       ╰───────────────────                     │
│ 20 ┤                                                    │
│    └────────────────────────────────► Time              │
│                                                         │
│ ※ Zero GCモードではスパイクが発生しない                │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 使用するAPI

### QueryResult の基本（QueryBySecondaryKey）

```csharp
// QueryBySecondaryKeyでZero GCのQueryResult<T>を取得
using var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);

// 結果の列挙（ref readonly で値コピーを回避）
foreach (ref readonly var enemy in results)
{
    Debug.Log(enemy.Name);
}

// インデクサアクセス
var first = results[0];

// 件数取得
int count = results.Count;
```

### Where による条件検索

```csharp
// Where()はIEnumerable<T>を返す（using不要）
var results = table.Where(r => r.Level >= 10 && r.Level <= 50);

foreach (var enemy in results)
{
    Debug.Log(enemy.Name);
}
```

### SecondaryKey との組み合わせ

```csharp
// FindAllBySecondaryKeyはIEnumerable<T>を返す
var areaEnemies = table.FindAllBySecondaryKey("areaId", 2);
var bosses = areaEnemies.Where(e => e.IsBoss);
```

### パフォーマンス計測

```csharp
// 方法1: Stopwatch（QueryBySecondaryKey）
var sw = Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    using var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);
    // 結果を使用
    var count = results.Count;
}
sw.Stop();
Debug.Log($"Time: {sw.ElapsedMilliseconds}ms");

// 方法2: Profiler API
Profiler.BeginSample("ZeroGC Query");
using var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);
Profiler.EndSample();

// 方法3: GCアロケーション計測
long before = GC.GetTotalMemory(false);
using var results2 = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);
long after = GC.GetTotalMemory(false);
Debug.Log($"Allocation: {after - before} bytes");
```

---

## コード例: パターン比較

### ❌ 避けるべきパターン（GC発生）

```csharp
// パターン1: ToList()でGCアロケーション発生
var list = table.All.Where(r => r.IsActive).ToList();

// パターン2: QueryResultをusingなしで使用
var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);
// resultsが破棄されない
```

### ✓ 推奨パターン（Zero GC）

```csharp
// パターン1: QueryBySecondaryKey + using + ref readonly
using var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);
foreach (ref readonly var item in results)
{
    // 値コピーなしでアクセス
}

// パターン2: FindAllBySecondaryKeyAsArrayで配列取得
var byArea = table.FindAllBySecondaryKeyAsArray("areaId", 2);

// パターン3: Where()で柔軟な条件検索（IEnumerable）
var filtered = table.Where(r => r.Level > 10);
```

---

## 想定するユースケース

### ユースケース1: 毎フレームの敵検索

```csharp
void Update()
{
    // Where()はIEnumerable<T>を返す
    var nearbyEnemies = enemyTable.Where(e =>
        Vector3.Distance(e.Position, player.Position) < detectionRange);

    foreach (var enemy in nearbyEnemies)
    {
        // AI処理
    }
}
```

### ユースケース2: SecondaryKeyでZero GC検索

```csharp
void ProcessAreaEnemies(int areaId)
{
    // QueryBySecondaryKeyでQueryResult<T>（ref struct、Zero GC）を取得
    using var areaEnemies = enemyTable.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", areaId);

    foreach (ref readonly var enemy in areaEnemies)
    {
        // 処理
    }
}
```

### ユースケース3: ドロップ判定

```csharp
void CalculateDrops(int areaId)
{
    // FindAllBySecondaryKeyはIEnumerable<T>を返す
    var areaEnemies = enemyTable.FindAllBySecondaryKey("areaId", areaId);
    var eligibleEnemies = areaEnemies.Where(e =>
        Random.value < e.DropRate);

    foreach (var enemy in eligibleEnemies)
    {
        // ドロップ処理
    }
}
```

---

## ファイル構成

```
Samples~/QueryResultZeroGC/
├── README.md
├── Scripts/
│   ├── EnemyRecord.cs
│   ├── EnemyTable.cs
│   ├── QueryResultZeroGCSample.cs
│   └── PerformanceProfiler.cs      # パフォーマンス計測ユーティリティ
├── Data/
│   └── enemies.csv                  # 10,000件の敵データ
└── Resources/
    └── EnemyTable.asset
```

---

## 注意事項

1. **usingを忘れずに**: `QueryResult` は `ref struct` なので、`using` で囲む（`QueryBySecondaryKey` で取得）
2. **ref readonly の活用**: `foreach (ref readonly var item in results)` で値コピーを回避（`QueryResult` のみ）
3. **スコープに注意**: `ref struct` はフィールドに保持できない
4. **async/awaitとの併用不可**: `ref struct` は非同期メソッドで使用できない
5. **Where()はIEnumerable**: `Where()` は `IEnumerable<T>` を返すため、`using` や `ref readonly` は不要
6. **デバッグ時の注意**: Profilerウィンドウで実際のGCアロケーションを確認

## パフォーマンス目安

| 操作 | 想定時間 | GCアロケーション |
|------|----------|-----------------|
| PrimaryKey検索 | O(log n) ~0.001ms | 0 bytes |
| SecondaryKey検索 | O(1) ~0.001ms | 0 bytes |
| Where (10,000件) | O(n) ~0.05ms | 0 bytes |
| Where + ToList | O(n) ~0.2ms | ~数KB |
