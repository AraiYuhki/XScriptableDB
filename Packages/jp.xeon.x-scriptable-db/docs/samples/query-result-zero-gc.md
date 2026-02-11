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
// GCアロケーションなし
using var results = table.Where(r => r.IsActive);
foreach (ref readonly var item in results) { ... }
```

特徴:
- `ref struct` による値型のみの処理
- 内部バッファの再利用
- `using` で確実にリソース解放

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

### QueryResult の基本

```csharp
// 条件検索（Zero GC）
using var results = table.Where(r => r.Level >= 10 && r.Level <= 50);

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

### SecondaryKey との組み合わせ

```csharp
// SecondaryKeyで高速フィルタ後、さらに条件絞り込み
var areaEnemies = table.FindAllBySecondaryKey("areaId", 2);
using var bosses = areaEnemies.Where(e => e.IsBoss);
```

### パフォーマンス計測

```csharp
// 方法1: Stopwatch
var sw = Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    using var results = table.Where(r => r.Level >= 10);
    // 結果を使用
    var count = results.Count;
}
sw.Stop();
Debug.Log($"Time: {sw.ElapsedMilliseconds}ms");

// 方法2: Profiler API
Profiler.BeginSample("ZeroGC Query");
using var results = table.Where(r => r.Level >= 10);
Profiler.EndSample();

// 方法3: GCアロケーション計測
long before = GC.GetTotalMemory(false);
using var results = table.Where(r => r.Level >= 10);
long after = GC.GetTotalMemory(false);
Debug.Log($"Allocation: {after - before} bytes");
```

---

## コード例: パターン比較

### ❌ 避けるべきパターン（GC発生）

```csharp
// パターン1: ToList()の使用
var list = table.Where(r => r.IsActive).ToList();

// パターン2: usingなしで使用
var results = table.Where(r => r.IsActive);
// resultsが破棄されない

// パターン3: 値コピー
foreach (var item in results)  // 値コピーが発生
{
    // ...
}
```

### ✓ 推奨パターン（Zero GC）

```csharp
// パターン1: using + ref readonly
using var results = table.Where(r => r.IsActive);
foreach (ref readonly var item in results)
{
    // 値コピーなしでアクセス
}

// パターン2: 即時使用
using var count = table.Where(r => r.Level > 50).Count;

// パターン3: SecondaryKey優先
var byArea = table.FindAllBySecondaryKey("areaId", 2);
using var filtered = byArea.Where(r => r.Level > 10);
```

---

## 想定するユースケース

### ユースケース1: 毎フレームの敵検索

```csharp
void Update()
{
    // プレイヤー周辺の敵を検索（毎フレーム）
    using var nearbyEnemies = enemyTable.Where(e =>
        Vector3.Distance(e.Position, player.Position) < detectionRange);

    foreach (ref readonly var enemy in nearbyEnemies)
    {
        // AI処理
    }
}
```

### ユースケース2: バトル中の対象選択

```csharp
void SelectTarget()
{
    // HP50%以下の敵を優先ターゲット
    using var weakEnemies = activeEnemies.Where(e =>
        e.CurrentHp < e.MaxHp * 0.5f);

    if (weakEnemies.Count > 0)
        target = weakEnemies[0];
}
```

### ユースケース3: ドロップ判定

```csharp
void CalculateDrops(int areaId)
{
    // エリアの敵からドロップアイテムを計算
    var areaEnemies = enemyTable.FindAllBySecondaryKey("areaId", areaId);
    using var eligibleEnemies = areaEnemies.Where(e =>
        Random.value < e.DropRate);

    foreach (ref readonly var enemy in eligibleEnemies)
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

1. **usingを忘れずに**: `QueryResult` は `ref struct` なので、`using` で囲む
2. **ref readonly の活用**: `foreach (ref readonly var item in results)` で値コピーを回避
3. **スコープに注意**: `ref struct` はフィールドに保持できない
4. **async/awaitとの併用不可**: `ref struct` は非同期メソッドで使用できない
5. **デバッグ時の注意**: Profilerウィンドウで実際のGCアロケーションを確認

## パフォーマンス目安

| 操作 | 想定時間 | GCアロケーション |
|------|----------|-----------------|
| PrimaryKey検索 | O(log n) ~0.001ms | 0 bytes |
| SecondaryKey検索 | O(1) ~0.001ms | 0 bytes |
| Where (10,000件) | O(n) ~0.05ms | 0 bytes |
| Where + ToList | O(n) ~0.2ms | ~数KB |
