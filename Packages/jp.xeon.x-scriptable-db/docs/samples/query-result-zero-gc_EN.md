# QueryResult (Zero GC) Sample Specification

## Overview

This sample demonstrates query patterns that produce no GC allocations.
It shows how to maximize performance in high-frequency data lookups at runtime.

## Learning Objectives

1. How to use `QueryResult<T>` (ref struct)
2. Proper resource management with `using` statements
3. Patterns for avoiding GC allocations
4. Performance comparison (Zero GC vs standard LINQ)
5. How to verify results with profiling

---

## Technical Background

### Traditional LINQ (with GC allocations)

```csharp
// Calling this every frame will cause GC
var results = table.All.Where(r => r.IsActive).ToList();
foreach (var item in results) { ... }
```

Issues:
- `Where()` creates an Enumerable
- `ToList()` creates a new List
- Calling every frame causes GC spikes

### QueryResult (Zero GC)

```csharp
// QueryBySecondaryKey returns QueryResult<T> (ref struct) — no GC allocation
using var results = table.QueryBySecondaryKey<EnemyRecord, int, bool>("isBoss", true);
foreach (ref readonly var item in results) { ... }
```

Characteristics:
- Value-type-only processing via `ref struct`
- Internal buffer reuse
- Guaranteed resource release with `using`

### Where (IEnumerable)

```csharp
// Where() returns IEnumerable<T> — no using required
var results = table.Where(r => r.IsActive);
foreach (var item in results) { ... }
```

Characteristics:
- Flexible condition specification
- Minimal GC allocation as long as ToList() is not called

---

## Data Structure

### EnemyRecord (Enemy Data)

| Field Name | Type | Description |
|------------|------|-------------|
| id | int | Enemy ID (primary key) |
| name | string | Enemy name |
| hp | int | HP |
| attack | int | Attack power |
| defense | int | Defense power |
| level | int | Level (SecondaryKey) |
| areaId | int | Spawn area ID (SecondaryKey) |
| isBoss | bool | Boss flag (SecondaryKey) |
| dropRate | float | Drop rate |

### Large Dataset

10,000+ records are prepared for performance verification.

---

## Sample Data

### enemies.csv (excerpt)

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

## GUI Requirements

### Main Panel

```
┌─────────────────────────────────────────────────────────┐
│ QueryResult (Zero GC) Sample                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ ─── Query Execution ───────────────────────────────    │
│                                                         │
│ Query Type:                                             │
│   ○ Level Range Search                                  │
│   ○ Area Search                                         │
│   ○ Bosses Only                                         │
│   ○ Compound Condition                                  │
│                                                         │
│ Parameters:                                             │
│   Min Level: [___10___]  Max Level: [___50___]         │
│   Area ID: [____2____]                                  │
│                                                         │
│ Execution Mode:                                         │
│   ○ Zero GC (QueryResult)                               │
│   ○ Standard LINQ (ToList)                              │
│                                                         │
│ [Run Once]  [Loop 100x]  [Loop 1000x]                   │
│                                                         │
│ ─── Results ───────────────────────────────────────    │
│                                                         │
│ Count: 523                                              │
│ Execution Time: 0.042ms                                 │
│ GC Allocation: 0 bytes                                  │
│                                                         │
│ ─── Performance Comparison ────────────────────────    │
│                                                         │
│ [Run Benchmark]                                         │
│                                                         │
│ ┌───────────────────────────────────────────────────┐   │
│ │ Method         │ Per Call  │ 1000 Total │ GC       │   │
│ ├────────────────┼───────────┼────────────┼──────────┤   │
│ │ Zero GC        │ 0.042ms   │ 42ms       │ 0 bytes  │   │
│ │ Standard LINQ  │ 0.156ms   │ 156ms      │ 2.4 MB   │   │
│ │ SecondaryKey   │ 0.001ms   │ 1ms        │ 0 bytes  │   │
│ └───────────────┴───────────┴────────────┴──────────┘   │
│                                                         │
│ ─── Search Results Preview (first 10) ─────────────    │
│                                                         │
│ ┌────┬──────────┬─────┬─────┬─────┬──────┬──────┐       │
│ │ ID │ Name     │ HP  │ ATK │ DEF │ Lv   │ Area │       │
│ ├────┼──────────┼─────┼─────┼─────┼──────┼──────┤       │
│ │ 15 │ Wolf     │ 50  │ 12  │ 5   │ 10   │ 2    │       │
│ │ 23 │ Ogre     │ 120 │ 25  │ 15  │ 15   │ 2    │       │
│ │ ...│ ...      │ ... │ ... │ ... │ ...  │ ...  │       │
│ └────┴──────────┴─────┴─────┴─────┴──────┴──────┘       │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Memory Profiler Display

```
┌─────────────────────────────────────────────────────────┐
│ Memory Usage                                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ Current Heap Size: 64.2 MB                              │
│ In Use: 48.5 MB                                         │
│                                                         │
│ GC Collection Count:                                    │
│   Gen0: 15   Gen1: 3   Gen2: 0                          │
│                                                         │
│ [Run GC.Collect]  [Reset Counters]                      │
│                                                         │
│ ─── Real-time Graph ───                                 │
│                                                         │
│ MB                                                      │
│ 50 ┤     ╭─╮                                           │
│ 40 ┤  ╭──╯ ╰──╮                                        │
│ 30 ┤──╯       ╰───────────────────                     │
│ 20 ┤                                                    │
│    └────────────────────────────────► Time              │
│                                                         │
│ * No spikes occur in Zero GC mode                       │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## APIs Used

### QueryResult Basics (QueryBySecondaryKey)

```csharp
// Get a Zero GC QueryResult<T> via QueryBySecondaryKey
using var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);

// Enumerate results (ref readonly avoids value copying)
foreach (ref readonly var enemy in results)
{
    Debug.Log(enemy.Name);
}

// Indexer access
var first = results[0];

// Get count
int count = results.Count;
```

### Conditional Search with Where

```csharp
// Where() returns IEnumerable<T> — no using required
var results = table.Where(r => r.Level >= 10 && r.Level <= 50);

foreach (var enemy in results)
{
    Debug.Log(enemy.Name);
}
```

### Combining with SecondaryKey

```csharp
// FindAllBySecondaryKey returns IEnumerable<T>
var areaEnemies = table.FindAllBySecondaryKey("areaId", 2);
var bosses = areaEnemies.Where(e => e.IsBoss);
```

### Performance Measurement

```csharp
// Method 1: Stopwatch (QueryBySecondaryKey)
var sw = Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    using var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);
    // Use the results
    var count = results.Count;
}
sw.Stop();
Debug.Log($"Time: {sw.ElapsedMilliseconds}ms");

// Method 2: Profiler API
Profiler.BeginSample("ZeroGC Query");
using var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);
Profiler.EndSample();

// Method 3: Measure GC allocation
long before = GC.GetTotalMemory(false);
using var results2 = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);
long after = GC.GetTotalMemory(false);
Debug.Log($"Allocation: {after - before} bytes");
```

---

## Code Examples: Pattern Comparison

### Patterns to Avoid (GC occurs)

```csharp
// Pattern 1: GC allocation from ToList()
var list = table.All.Where(r => r.IsActive).ToList();

// Pattern 2: Using QueryResult without using
var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);
// results is never disposed
```

### Recommended Patterns (Zero GC)

```csharp
// Pattern 1: QueryBySecondaryKey + using + ref readonly
using var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);
foreach (ref readonly var item in results)
{
    // Access without value copying
}

// Pattern 2: Get array with FindAllBySecondaryKeyAsArray
var byArea = table.FindAllBySecondaryKeyAsArray("areaId", 2);

// Pattern 3: Flexible conditional search with Where() (IEnumerable)
var filtered = table.Where(r => r.Level > 10);
```

---

## Expected Use Cases

### Use Case 1: Per-Frame Enemy Search

```csharp
void Update()
{
    // Where() returns IEnumerable<T>
    var nearbyEnemies = enemyTable.Where(e =>
        Vector3.Distance(e.Position, player.Position) < detectionRange);

    foreach (var enemy in nearbyEnemies)
    {
        // AI processing
    }
}
```

### Use Case 2: Zero GC Search with SecondaryKey

```csharp
void ProcessAreaEnemies(int areaId)
{
    // Get QueryResult<T> (ref struct, Zero GC) via QueryBySecondaryKey
    using var areaEnemies = enemyTable.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", areaId);

    foreach (ref readonly var enemy in areaEnemies)
    {
        // Processing
    }
}
```

### Use Case 3: Drop Calculation

```csharp
void CalculateDrops(int areaId)
{
    // FindAllBySecondaryKey returns IEnumerable<T>
    var areaEnemies = enemyTable.FindAllBySecondaryKey("areaId", areaId);
    var eligibleEnemies = areaEnemies.Where(e =>
        Random.value < e.DropRate);

    foreach (var enemy in eligibleEnemies)
    {
        // Drop processing
    }
}
```

---

## File Structure

```
Samples~/QueryResultZeroGC/
├── README.md
├── Scripts/
│   ├── EnemyRecord.cs
│   ├── EnemyTable.cs
│   ├── QueryResultZeroGCSample.cs
│   └── PerformanceProfiler.cs      # Performance measurement utility
├── Data/
│   └── enemies.csv                  # 10,000 enemy records
└── Resources/
    └── EnemyTable.asset
```

---

## Notes

1. **Don't forget using**: `QueryResult` is a `ref struct`, so wrap it with `using` (when obtained via `QueryBySecondaryKey`)
2. **Use ref readonly**: Use `foreach (ref readonly var item in results)` to avoid value copying (only for `QueryResult`)
3. **Mind the scope**: `ref struct` cannot be stored in fields
4. **Cannot be used with async/await**: `ref struct` cannot be used in asynchronous methods
5. **Where() returns IEnumerable**: Since `Where()` returns `IEnumerable<T>`, neither `using` nor `ref readonly` is needed
6. **Note when debugging**: Verify actual GC allocations in the Profiler window

## Performance Reference

| Operation | Expected Time | GC Allocation |
|-----------|--------------|---------------|
| PrimaryKey lookup | O(log n) ~0.001ms | 0 bytes |
| SecondaryKey lookup | O(1) ~0.001ms | 0 bytes |
| Where (10,000 records) | O(n) ~0.05ms | 0 bytes |
| Where + ToList | O(n) ~0.2ms | ~few KB |
