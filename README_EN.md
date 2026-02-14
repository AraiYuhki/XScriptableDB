# XScriptableDB

A ScriptableObject-based database package for Unity. Provides master data management, CSV/TSV import/export, and SQL-like query functionality.

> **Note:** For Japanese documentation, see [README.md](README.md).

## Features

- **ScriptableObject-based**: Fully integrated with Unity's asset system
- **Fast Search**: O(log n) binary search with PrimaryKey, O(1) hash lookup with SecondaryKey
- **Zero GC Allocation**: Memory-efficient query results using ref struct
- **CSV/TSV Support**: Import/export with automatic encoding detection
- **Diff Viewer**: Preview changes before import, selective application
- **SQL Editor**: Advanced SQL features (JOIN, aggregate functions, GROUP BY, CASE, subqueries)
- **Database Browser**: Table list and schema inspection
- **Data Validation**: Attribute-based validation (Required, Range, ForeignKey, etc.)
- **Performance Optimization**: LRU cache, lazy loading, profiling
- **Schema Management**: Schema comparison, migration, automatic backup
- **Development Tools**: CLI, test data generation

## Requirements

- Unity 6000.0 or later
- .NET Standard 2.1

## Installation

### Package Manager (Git URL)

1. Open `Window > Package Manager` in Unity
2. Click the `+` button and select `Add package from git URL...`
3. Enter the following URL:
```
https://github.com/AraiYuhki/XScriptableDB.git?path=Packages/jp.xeon.x-scriptable-db
```

### manifest.json

Add the following to `Packages/manifest.json`:
```json
{
  "dependencies": {
    "jp.xeon.x-scriptable-db": "https://github.com/AraiYuhki/XScriptableDB.git?path=Packages/jp.xeon.x-scriptable-db"
  }
}
```

## Quick Start

### 1. Define a Record Class

```csharp
using System;
using Xeon.XScriptableDB;

[Serializable]
public class ItemRecord
{
    [PrimaryKey]
    public int Id;

    [SecondaryKey]
    public string Category;

    [CsvColumn("ItemName")]
    public string Name;

    public int Price;
    public int Attack;
    public int Defense;
}
```

### 2. Create a TableAsset

```csharp
using UnityEngine;
using Xeon.XScriptableDB;

[CreateAssetMenu(fileName = "ItemTable", menuName = "Database/ItemTable")]
public class ItemTable : TableAsset<ItemRecord, int>
{
}
```

### 3. Search Data

```csharp
// Search by PrimaryKey (O(log n))
var item = itemTable.FindByKey(1001);

// Search by SecondaryKey (O(1))
var weapons = itemTable.FindAllBySecondaryKeyAsArray("Category", "Weapon");

// LINQ-like query
var expensiveItems = itemTable.Where(r => r.Price > 1000);
foreach (var item in expensiveItems)
{
    Debug.Log(item.Name);
}
```

## Key Components

### Attributes

| Attribute | Description |
|-----------|-------------|
| `[PrimaryKey]` | Specifies primary key. Must be unique, enables fast binary search |
| `[SecondaryKey]` | Specifies secondary key. Enables fast hash-based lookup |
| `[CsvColumn("name")]` | Specifies column name for CSV/TSV |

### Validation Attributes

| Attribute | Description |
|-----------|-------------|
| `[Required]` | Required field (prohibits null/empty) |
| `[Range(min, max)]` | Numeric range restriction |
| `[StringLength(max)]` | String length restriction |
| `[RegularExpression(pattern)]` | Regex validation |
| `[Unique]` | Ensures uniqueness within table |
| `[ForeignKey(typeof(Table))]` | Foreign key reference validation |
| `[Compare(field, operator)]` | Comparison with other fields |

### TableAsset<TRecord, TKey>

ScriptableObject-based table class.

```csharp
// Basic search
TRecord FindByKey(TKey key);
bool TryFindByKey(TKey key, out TRecord record);

// SecondaryKey search
IEnumerable<TRecord> FindAllBySecondaryKey<TSecondaryKey>(string keyName, TSecondaryKey key);
TRecord[] FindAllBySecondaryKeyAsArray<TSecondaryKey>(string keyName, TSecondaryKey key);

// LINQ-like query
IEnumerable<TRecord> Where(Func<TRecord, bool> predicate);

// SecondaryKey query (Zero GC Allocation)
QueryResult<TRecord> QueryBySecondaryKey<TSecondaryKey>(string indexName, TSecondaryKey key);
```

### QueryResult<T>

A ref struct for handling query results without GC allocation. Returned by `QueryBySecondaryKey()`.

```csharp
var result = table.QueryBySecondaryKey("Category", "Weapon");

// Enumerate with foreach
foreach (var record in result)
{
    // ...
}

// Count
int count = result.Count;
```

## Editor Tools

### Table Editor

`Tools > XScriptableDB > Table Editor`

- Edit table data
- CSV/TSV import/export
- Bulk operations

### SQL Editor

`Tools > XScriptableDB > SQL Editor`

Query and update data with SQL-like syntax.

```sql
-- Basic search
SELECT * FROM ItemTable WHERE Category = 'Weapon' AND Price > 1000 ORDER BY Price DESC LIMIT 10

-- JOIN
SELECT i.Name, c.CategoryName
FROM ItemTable i
INNER JOIN CategoryTable c ON i.CategoryId = c.Id

-- Aggregate functions
SELECT Category, COUNT(*), SUM(Price), AVG(Price)
FROM ItemTable
GROUP BY Category
HAVING COUNT(*) > 5

-- CASE expression
SELECT Name,
  CASE WHEN Price > 1000 THEN 'High'
       WHEN Price > 500 THEN 'Medium'
       ELSE 'Low' END AS PriceLevel
FROM ItemTable

-- Subquery
SELECT * FROM ItemTable
WHERE Price > (SELECT AVG(Price) FROM ItemTable)

-- Update
UPDATE ItemTable SET Price = 500 WHERE Id = 1001

-- Delete
DELETE FROM ItemTable WHERE Price = 0
```

**Supported Syntax:**
- SELECT: Column specification, DISTINCT, WHERE, ORDER BY (ASC/DESC), LIMIT, OFFSET
- JOIN: INNER JOIN, LEFT JOIN, RIGHT JOIN, CROSS JOIN
- Aggregate functions: COUNT, SUM, AVG, MIN, MAX
- Grouping: GROUP BY, HAVING
- CASE expressions: CASE WHEN ... THEN ... ELSE ... END
- Subqueries: In WHERE clause, IN clause
- Arithmetic: +, -, *, /
- String functions: UPPER, LOWER, CONCAT, SUBSTRING, TRIM, LENGTH
- UPDATE: SET, WHERE
- DELETE: WHERE
- Operators: =, !=, <>, <, <=, >, >=, LIKE, IN, IS NULL, IS NOT NULL
- Logical operators: AND, OR
- Parentheses grouping

### Database Browser

`Tools > XScriptableDB > Database Browser`

- List of tables in project
- Schema information (columns, keys)
- Data preview

### Validation Window

`Tools > XScriptableDB > Validation`

- Validate table data
- List errors and warnings
- Foreign key integrity check

### Performance Window

`Tools > XScriptableDB > Performance`

- Cache statistics display
- Memory usage visualization
- Query profiling

### Diff Viewer

Preview and selectively apply changes when importing CSV.

- Added records (green)
- Deleted records (red)
- Modified records (yellow)
- Field-level diff display

### Schema Compare

`Tools > XScriptableDB > Schema Compare`

Compare schemas of two table types and display differences.

- Detect field additions/deletions/type changes
- Detect PrimaryKey/SecondaryKey changes
- Auto-generate migration code

### Backup Manager

`Tools > XScriptableDB > Backup Manager`

Manage backup and restore of table data.

- Create manual/automatic backups
- Restore from backup
- Backup generation management
- Filtering and search

### Test Data Generator

`Tools > XScriptableDB > Test Data Generator`

Automatically generate test data.

- Generation rules: sequential, random, pattern, etc.
- Auto-detection from field names (Name, Price, Level, etc.)
- Custom generation settings

### Benchmark

`Tools > XScriptableDB > Benchmark`

Run performance tests.

- Search benchmarks (linear, binary, hash)
- CSV benchmarks (parse, export)
- Memory usage measurement
- Report output

### Data Editor

`Tools > XScriptableDB > Data Editor`

Directly edit table data.

- Add, edit, delete records
- Field-by-field editing
- DateTime type support

## CLI (Command Line Interface)

Batch mode operations are available.

```bash
# Export table
Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Export -table=ItemTable -output=./items.csv

# Import CSV
Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Import -table=ItemTable -input=./items.csv

# Run validation
Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Validate -table=ItemTable

# Execute SQL query
Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Query -sql="SELECT * FROM ItemTable WHERE Price > 100"

# Create backup
Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Backup -table=ItemTable

# List tables
Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.ListTables

# Show schema info
Unity -batchmode -executeMethod Xeon.XScriptableDB.Editor.XScriptableDBCLI.Schema -table=ItemTable
```

## CSV/TSV Format

### Import

```csharp
// From Editor script
TableImporter.ImportWithPreview(tableAsset, "path/to/data.csv");
```

### Export

```csharp
var settings = new ExportSettings
{
    FilePath = "path/to/output.csv",
    Delimiter = ',',
    Encoding = Encoding.UTF8,
    SortByPrimaryKey = true
};
TableExporter.Export(tableAsset, settings);
```

### CSV Format Example

```csv
Id,Category,Name,Price,Attack,Defense
1001,Weapon,Iron Sword,100,10,0
1002,Weapon,Steel Sword,500,25,0
1003,Armor,Leather Armor,80,0,5
```

## YAML Definition File Format

Table Editor defines table schemas in YAML files. C# code (Record and Table classes) can be auto-generated from YAML files.

Menu: `Tools > XScriptableDB > Generate C# from YAML file` / `Generate C# from YAML folder`

### Basic Structure

```yaml
tableName: m_item              # Table name (snake_case recommended)
isReadOnly: true               # Immutability at runtime (default: true)
columns:                       # List of column definitions
  - name: id                   # Column name (snake_case)
    type: int                  # Data type (see type conversion table below)
    isPrimaryKey: true         # Primary key flag
    isNullable: false          # Nullable flag
  - name: name
    type: string
indices:                       # List of SecondaryKey indexes
  - name: category             # Index name
    columns:                   # Target columns (multiple for composite index)
      - category
    allowDuplicates: true      # Allow duplicate key values
```

### Column Definition

| Property | Type | Required | Default | Description |
|----------|------|:--------:|---------|-------------|
| `name` | string | Yes | - | Column name (snake_case recommended) |
| `type` | string | Yes | - | Data type |
| `isPrimaryKey` | bool | - | `false` | Designate as primary key |
| `isNullable` | bool | - | `false` | Generate as nullable type |

### Index Definition

| Property | Type | Required | Default | Description |
|----------|------|:--------:|---------|-------------|
| `name` | string | Yes | - | Index name |
| `columns` | string[] | Yes | - | List of target column names |
| `allowDuplicates` | bool | - | `true` | Allow duplicate values |

Specifying multiple columns in `columns` creates a composite index.

### Supported Types

| YAML Type | C# Type | Description |
|-----------|---------|-------------|
| `int`, `integer`, `mediumint` | `int` | 32-bit integer |
| `tinyint` | `byte` | 8-bit unsigned integer |
| `smallint` | `short` | 16-bit integer |
| `bigint` | `long` | 64-bit integer |
| `float`, `real` | `float` | 32-bit floating point |
| `double` | `double` | 64-bit floating point |
| `decimal`, `numeric` | `decimal` | High-precision decimal |
| `bool`, `boolean` | `bool` | Boolean |
| `char` | `char` | Character |
| `string`, `varchar`, `text`, `longtext`, `mediumtext`, `tinytext` | `string` | String |
| `datetime`, `timestamp`, `date` | `SerializableDateTime` | Date/time |

When `isNullable: true`, value types are generated as nullable (e.g., `int?`).

### Full Example

```yaml
tableName: m_item
isReadOnly: true
columns:
  - name: id
    type: int
    isPrimaryKey: true
  - name: name
    type: string
  - name: description
    type: string
    isNullable: true
  - name: category_id
    type: int
  - name: price
    type: int
  - name: rarity
    type: int
  - name: is_tradable
    type: bool
  - name: release_date
    type: datetime
indices:
  - name: category_id
    columns:
      - category_id
    allowDuplicates: true
  - name: rarity
    columns:
      - rarity
    allowDuplicates: true
```

This YAML auto-generates the following C# code:

```csharp
// MItemRecord.cs
[Serializable]
public partial class MItemRecord
{
    [SerializeField, CsvColumn("id"), PrimaryKey]
    private int id;

    [SerializeField, CsvColumn("name")]
    private string name;

    [SerializeField, CsvColumn("description")]
    private string description;

    [SerializeField, CsvColumn("category_id"), SecondaryKey]
    private int categoryId;

    [SerializeField, CsvColumn("price")]
    private int price;

    [SerializeField, CsvColumn("rarity"), SecondaryKey]
    private int rarity;

    [SerializeField, CsvColumn("is_tradable")]
    private bool isTradable;

    [SerializeField, CsvColumn("release_date")]
    private SerializableDateTime releaseDate;

    // Properties (when isReadOnly: true, setters are Editor-only)
    public int Id { get => id; }
    public string Name { get => name; }
    // ...
}
```

### Composite Index Example

```yaml
tableName: m_character
isReadOnly: true
columns:
  - name: id
    type: int
    isPrimaryKey: true
  - name: name
    type: string
  - name: class
    type: string
  - name: level
    type: int
indices:
  - name: class
    columns:
      - class
    allowDuplicates: true
  - name: class_level
    columns:
      - class
      - level
    allowDuplicates: true
  - name: name
    columns:
      - name
    allowDuplicates: false
```

### YAML Naming Convention

YAML files use **camelCase** following YamlDotNet's `CamelCaseNamingConvention`.

| C# Property | YAML Key |
|-------------|----------|
| `TableName` | `tableName` |
| `IsReadOnly` | `isReadOnly` |
| `IsPrimaryKey` | `isPrimaryKey` |
| `IsNullable` | `isNullable` |
| `AllowDuplicates` | `allowDuplicates` |

## Performance Features

### LRU Cache

```csharp
using Xeon.XScriptableDB.Cache;

// Cached query
var cache = CacheManager.QueryCache;
var result = cache.GetOrAdd<Item>(
    typeof(ItemTable), "FindById", 1,
    () => itemTable.FindById(1)
);

// Invalidate cache on table update
CacheManager.InvalidateTable<ItemTable>();
```

### Lazy Loading

```csharp
using Xeon.XScriptableDB.LazyLoad;

// Addressables-based lazy loading
var loader = new TableLoader();
loader.Register<ItemTable>("Tables/ItemTable");

// Async load
var table = await loader.GetAsync<ItemTable>();

// Release after use
loader.Release<ItemTable>();
```

### Profiling

```csharp
using Xeon.XScriptableDB.Performance;

var profiler = new QueryProfiler();
var items = profiler.Profile("SearchItems", typeof(ItemTable),
    () => itemTable.All.ToList());

// Get statistics
var stats = profiler.GetStatistics();
Debug.Log($"Total queries: {stats.QueryCount}, Avg: {stats.AverageMilliseconds}ms");
```

## Best Practices

### PrimaryKey Selection

- Use unique, immutable values
- Value types like int or long are recommended (faster comparison)

### SecondaryKey Usage

- Set for frequently searched categories or types
- Multiple SecondaryKeys can be set

### Query Optimization

```csharp
// Good: Use QueryBySecondaryKey to avoid GC allocation
var result = table.QueryBySecondaryKey("Category", "Weapon");

// Where() returns IEnumerable<T> (causes GC allocation)
var filtered = table.Where(r => r.IsActive);
```

### CSV Import Workflow

1. Prepare CSV file
2. Import with Table Editor (with preview)
3. Confirm changes in Diff Viewer
4. Apply only necessary records

## Samples

Samples are included in the `Packages/jp.xeon.x-scriptable-db/Samples~` folder.

You can import them from Package Manager:
1. Select `XScriptableDB` in Package Manager
2. Open the `Samples` tab
3. Click `Import` for the desired sample

## License

MIT OR Apache-2.0

## Author

Xeon ([@AraiYuhki](https://github.com/AraiYuhki))
