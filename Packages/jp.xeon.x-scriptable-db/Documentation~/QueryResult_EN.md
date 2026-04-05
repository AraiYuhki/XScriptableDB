# QueryResult Guide

## Overview

`QueryResult<T>` is a ref struct used to store query results. It allows you to work with query results efficiently without causing GC allocation.

## Why a ref struct?

Standard LINQ queries allocate heap memory to store results. In game development, this causes per-frame GC allocation.

Because `QueryResult<T>` is allocated on the stack, it produces zero GC allocation.

```csharp
// Traditional LINQ - causes GC allocation
var list = records.Where(r => r.IsActive).ToList();

// QueryBySecondaryKey - zero GC allocation
var result = table.QueryBySecondaryKey("Type", "Slime");
```

## How to Obtain a QueryResult

`QueryResult<T>` is returned by the `QueryBySecondaryKey()` method.

```csharp
// Zero GC search by SecondaryKey
var result = table.QueryBySecondaryKey("Type", "Slime");
```

> **Note**: The `Where()` method returns `IEnumerable<T>`, not `QueryResult<T>`.

## Basic Usage

### QueryBySecondaryKey

```csharp
var result = table.QueryBySecondaryKey("Type", "Slime");

Debug.Log($"Found: {result.Count} records");

foreach (var record in result)
{
    Debug.Log(record.Name);
}
```

### Where (IEnumerable version)

```csharp
// Returns IEnumerable<T> (no using required)
var result = table.Where(r => r.Price > 100);

foreach (var record in result)
{
    Debug.Log(record.Name);
}
```

## Properties and Methods

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
var first = result.First;  // First record, or null if empty
```

### Indexer

```csharp
var result = table.QueryBySecondaryKey("Type", "Goblin");
var first = result[0];
var last = result[result.Count - 1];
```

## Table-Level Extension Methods

### FirstOrDefault

```csharp
var enemy = table.FirstOrDefault(r => r.Name == "Goblin");
if (enemy != null)
    Debug.Log(enemy.Hp);
```

### Any / All / Count

```csharp
bool hasStrongEnemy = table.Any(r => r.Attack > 100);
bool allActive = table.All(r => r.IsActive);
int weakEnemyCount = table.Count(r => r.Hp < 50);
```

### Select / Skip / Take

```csharp
var names = table.Select(r => r.Name);
var page = table.Skip(10).Take(10);  // Records 11–20
```

## ToList() / ToArray()

Use these when you need to persist results. Note that they cause GC allocation.

```csharp
var result = table.QueryBySecondaryKey("Type", "Goblin");
var array = result.ToArray();  // GC allocation
var list = result.ToList();    // GC allocation
```

## Constraints

### ref struct Limitations

```csharp
// Not allowed: cannot be stored as a class field
class MyClass
{
    QueryResult<Item> result;  // Compile error
}

// Not allowed: cannot be used inside lambda expressions
Action action = () => { var r = result[0]; };  // Compile error

// Not allowed: cannot be used inside async methods
async Task ProcessAsync()
{
    var result = table.QueryBySecondaryKey("Type", "Slime");  // Compile error
}
```

### Where() vs QueryBySecondaryKey()

| Method | Return Type | GC Alloc | Use Case |
|--------|-------------|----------|----------|
| `Where()` | `IEnumerable<T>` | Yes | Filtering with arbitrary conditions |
| `QueryBySecondaryKey()` | `QueryResult<T>` | Zero | Fast lookup by SecondaryKey |

## Performance Tips

### Early Return

```csharp
var first = table.FirstOrDefault(r => r.Name == "Target");

if (table.Any(r => r.IsActive))
{
    // ...
}
```

### Condition Ordering

```csharp
// Place faster conditions first
var result = table.Where(r =>
    r.IsActive &&      // bool comparison is fast
    r.Type == "A" &&   // enum/string comparison
    ExpensiveCheck(r)  // expensive check goes last
);
```

### Leveraging Indexes

```csharp
// Filter by SecondaryKey first (zero GC allocation)
var result = table.QueryBySecondaryKey("Category", "Weapon");

// Then apply additional LINQ filter
var expensiveWeapons = result.ToArray().Where(r => r.Price > 1000);
```
