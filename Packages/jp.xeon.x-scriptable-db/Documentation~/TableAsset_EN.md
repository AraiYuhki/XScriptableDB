# TableAsset Guide

## Overview

`TableAsset<TRecord, TKey>` is the core class of XScriptableDB. It inherits from ScriptableObject and is fully integrated with Unity's asset system.

## Basic Usage

### 1. Define a Record Class

First, define the data structure for records stored in the table.

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

### 2. Create a TableAsset Class

Use the record class to create a concrete table class.

```csharp
using UnityEngine;
using Xeon.XScriptableDB;

[CreateAssetMenu(fileName = "EnemyTable", menuName = "Database/EnemyTable")]
public class EnemyTable : TableAsset<EnemyRecord, int>
{
}
```

### 3. Create a Table Asset

In the Unity Editor:
1. Right-click in the Project window
2. Select `Create > Database > EnemyTable`
3. Enter data into the created asset

## PrimaryKey

A field marked with `[PrimaryKey]` serves as the unique identifier for each record.

### Characteristics

- **Uniqueness**: No two records can share the same key
- **Fast lookup**: O(log n) binary search
- **Type constraint**: Must implement IComparable (int, string, enum, etc.)

### Search Methods

```csharp
// Find by key (returns null if not found)
var enemy = enemyTable.FindByKey(1001);

// Safe lookup (returns whether the record was found)
if (enemyTable.TryFindByKey(1001, out var enemy))
{
    Debug.Log(enemy.Name);
}

// Indexer access
var enemy = enemyTable[1001];
```

## SecondaryKey

A field marked with `[SecondaryKey]` is indexed as a secondary key.

### Characteristics

- **Fast lookup**: O(1) hash-based index
- **Multi-value**: Multiple records with the same key value can be retrieved
- **Multiple keys**: A single record can have multiple SecondaryKeys

### Example

```csharp
[Serializable]
public class EnemyRecord
{
    [PrimaryKey]
    public int Id;

    [SecondaryKey]
    public string Type;  // e.g. "Slime", "Goblin", "Dragon"

    [SecondaryKey]
    public int AreaId;   // Spawn area ID

    public string Name;
    public int Hp;
}
```

### Search Methods

```csharp
// Find one record by SecondaryKey
var slime = enemyTable.FindBySecondaryKey("Type", "Slime");

// Find multiple records by SecondaryKey
var goblins = enemyTable.FindAllBySecondaryKey("Type", "Goblin");

// TryFind variant
if (enemyTable.TryFindBySecondaryKey("AreaId", 5, out var enemy))
{
    Debug.Log(enemy.Name);
}
```

## Table Properties

```csharp
// Record count
int count = table.Count;

// All records (IEnumerable<TRecord>)
foreach (var record in table.Records)
{
    Debug.Log(record.Name);
}

// Record type
Type recordType = table.RecordType;

// Key type
Type keyType = table.KeyType;
```

## Editor-Only Operations (UNITY_EDITOR)

```csharp
#if UNITY_EDITOR
// Create a new record
var newRecord = table.CreateNewRecord();

// Add a record
table.AddRecordObject(newRecord);

// Remove a record by index
table.RemoveRecordAt(0);

// Check for duplicate keys
var duplicates = table.FindDuplicateKeysAsObjects();
#endif
```

## Best Practices

### Choosing a PrimaryKey

```csharp
// Good: immutable identifier
[PrimaryKey]
public int Id;

// Good: enum type
[PrimaryKey]
public ItemType Type;

// Avoid: values that may change
[PrimaryKey]
public string Name;  // Names can change
```

### Using SecondaryKeys

```csharp
// Apply SecondaryKey to frequently searched fields
[SecondaryKey]
public string Category;  // When category-based search is common

[SecondaryKey]
public int Level;  // When level-based search is common
```

### Memory Efficiency

```csharp
// For large datasets, consider loading only when needed
[SerializeField]
private EnemyTable enemyTable;

private void Start()
{
    // Consider Addressables-based lazy loading as well
}
```
