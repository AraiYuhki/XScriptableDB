# QueryResult (Zero GC) Sample

A sample demonstrating query patterns that produce zero GC allocation.

## Overview

This sample covers the following features:

- How to use `QueryResult<T>` (ref struct)
- Proper resource management with `using`
- Patterns for avoiding GC allocation
- Performance comparison

## Setup

1. Import this sample from Package Manager
2. Create a table via `Assets > Create > XScriptableDB > Samples > QueryResultZeroGC > EnemyTable`
3. Import `Data/enemies.csv`

## File Structure

```
QueryResultZeroGC/
├── Scripts/
│   ├── EnemyRecord.cs
│   ├── EnemyTable.cs
│   ├── QueryResultZeroGCSample.cs
│   └── PerformanceProfiler.cs
├── Data/
│   └── enemies.csv               # 100 enemy records
└── README.md
```

## Basic Usage

### Zero GC Pattern (QueryBySecondaryKey — recommended)

```csharp
// QueryBySecondaryKey returns QueryResult<T> (ref struct)
using var results = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);

// Use ref readonly to avoid value copies
foreach (ref readonly var enemy in results)
{
    Debug.Log(enemy.Name);
}
```

### Where Pattern (IEnumerable)

```csharp
// Where() returns IEnumerable<T> (no using required)
var results = table.Where(r => r.Level >= 10 && r.Level <= 50);

foreach (var enemy in results)
{
    Debug.Log(enemy.Name);
}
```

### Pattern to Avoid

```csharp
// ToList() causes GC allocation
var list = table.All.Where(r => r.Level >= 10).ToList();
```

## Performance Comparison

```csharp
var result = sample.RunBenchmark(1000);
Debug.Log(result);

// Example output:
// === Zero GC (QueryResult) ===
// Average: 0.042ms / Memory: 0 bytes
//
// === Standard LINQ (ToList) ===
// Average: 0.156ms / Memory: 2,400,000 bytes
//
// Speedup: 3.71x
```

## Search Method Comparison

| Method | Complexity | GC Allocation | Use Case |
|--------|------------|---------------|----------|
| PrimaryKey search | O(log n) | 0 bytes | Search by ID |
| SecondaryKey search | O(1) | 0 bytes | Search by category, etc. |
| QueryBySecondaryKey (Zero GC) | O(1) | 0 bytes | SecondaryKey search |
| Where (IEnumerable) | O(n) | Minimal | Complex condition search |
| Where + ToList | O(n) | ~few KB | When results need to be persisted |

## Use Cases

### Per-Frame Search

```csharp
void Update()
{
    var nearby = enemyTable.Where(e =>
        Vector3.Distance(e.Position, player.Position) < range);

    foreach (var enemy in nearby)
    {
        // AI processing
    }
}
```

### Zero GC Search by SecondaryKey

```csharp
using var areaEnemies = table.QueryBySecondaryKey<EnemyRecord, int, int>("areaId", 2);

foreach (ref readonly var enemy in areaEnemies)
{
    // Process area enemies
}
```

## Notes

1. `QueryResult` is a `ref struct` — always wrap `QueryBySecondaryKey` results in `using`
2. Use `foreach (ref readonly var item in results)` to avoid value copies (only for `QueryResult`)
3. `ref struct` cannot be stored in fields
4. Cannot be used with `async/await`
5. `Where()` returns `IEnumerable<T>`, so `using` and `ref readonly` are not required
6. Use `FindAllBySecondaryKeyAsArray()` to get a `T[]` when results need to be persisted
