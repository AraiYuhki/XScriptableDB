# XScriptableDB Roadmap

This document describes the development roadmap for XScriptableDB.

## Released

### v0.1.0 (2026-01-29)

#### Phase 1: Core Features
- [x] `TableAsset<TRecord, TKey>` - ScriptableObject-based table
- [x] `[PrimaryKey]` attribute - O(log n) search via binary search
- [x] `[CsvColumn]` attribute - CSV column name mapping
- [x] Basic CRUD operations

#### Phase 2: Index & Query
- [x] `[SecondaryKey]` attribute - O(1) search via hash index
- [x] `QueryResult<T>` - zero GC allocation ref struct query result
- [x] Extension methods (Where, FirstOrDefault, Any, All, Count, Select, Skip, Take)
- [x] Multiple SecondaryKey support

#### Phase 3: CSV Integration & Diff Viewer
- [x] CSV/TSV import/export
- [x] Automatic encoding detection
- [x] Diff Viewer - change preview with selective apply
- [x] Table Editor UI

#### Phase 4: SQL Editor & DB Browser
- [x] SQL Parser (SELECT, UPDATE, DELETE)
- [x] SQL Executor
- [x] SQL Editor Window (syntax highlighting, history)
- [x] Database Browser (table list, schema display)

#### Phase 5: Documentation & Samples
- [x] README.md
- [x] Detailed documentation (TableAsset, QueryResult, CSV, SQL)
- [x] Sample code (BasicUsage)
- [x] CHANGELOG.md

### v0.2.0 (2026-01-29)

#### Phase 6: Data Validation
- [x] `[Required]` attribute - mark required fields
- [x] `[Range(min, max)]` attribute - numeric range restriction
- [x] `[StringLength(max)]` attribute - string length limit
- [x] `[Unique]` attribute - uniqueness constraint (non-PrimaryKey)
- [x] `[ForeignKey]` attribute - foreign key constraint (cross-table reference)
- [x] `[RegularExpression]` attribute - regex pattern validation
- [x] `[Compare]` attribute - cross-field comparison validation
- [x] Custom validators (`IRecordValidator<T>`)
- [x] `RecordValidator` - per-record and per-table validation
- [x] `ForeignKeyValidator` - foreign key reference validation
- [x] Validation Window - Editor validation execution and result display

---

### v0.3.0 (2026-01-29)

#### Phase 7: Performance Optimization
- [x] Query result cache
  - `LruCache<TKey, TValue>` - LRU cache (capacity limit, automatic eviction)
  - `QueryCache` - dedicated query result cache (version-based invalidation)
  - `CacheManager` - global cache management
- [x] Lazy loading
  - `LazyTableReference<T>` - Addressables-based lazy loading
  - `TableLoader` - batch management of multiple tables
  - Automatic unload via reference counting
- [x] Performance measurement
  - `QueryProfiler` - query execution time measurement
  - `MemoryProfiler` - memory usage estimation
  - Performance Window - Editor visualization

#### Usage Example
```csharp
// Cached query
var cache = CacheManager.QueryCache;
var result = cache.GetOrAdd<Item>(
    typeof(ItemTable), "FindByKey", 1,
    () => itemTable.FindByKey(1)
);

// Lazy loading
var loader = new TableLoader();
loader.Register<ItemTable>("Tables/ItemTable");
var table = await loader.GetAsync<ItemTable>();

// Profiling
var profiler = new QueryProfiler();
var items = profiler.Profile("SearchItems", typeof(ItemTable),
    () => itemTable.All.ToList());
```

---

### v0.4.0 (2026-01-30)

#### Phase 8: Large Dataset Support
- [x] Virtual scroll (Table Editor)
  - `VirtualizedListView<T>` - general-purpose virtual scroll component
  - `VirtualizedPropertyListView` - SerializedProperty variant
  - Integrated into TableEditorWindow (auto-enabled for 100+ records)
- [x] Streaming import
  - `StreamingImporter` - chunk-based large dataset import
  - Progress display, cancel, error handling
  - `StreamingImportWindow` - dedicated Editor window
- [x] Batch Processing API (Editor only)
  - `BatchProcessor<TRecord, TKey>` - main batch operations class
  - `AddRange()`, `UpdateRange()`, `DeleteRange()`, `UpsertRange()`
  - `DeleteWhere()`, `UpdateWhere()`, `ReplaceAll()`
  - Extension method `CreateBatchProcessor()` for easy creation
- [x] Benchmark test suite
  - `BenchmarkTests` - search, sort, CSV, and memory benchmarks
  - `BenchmarkWindow` - interactive benchmark execution

#### Usage Example
```csharp
// Batch processing
var processor = itemTable.CreateBatchProcessor(r => r.id);
var result = processor.UpsertRange(newRecords);
Debug.Log($"Added: {result.AddedCount}, Updated: {result.UpdatedCount}");

// Bulk delete by condition
processor.DeleteWhere(r => r.price < 100);

// Bulk update by condition
processor.UpdateWhere(
    r => r.category == "Sale",
    r => r.price = (int)(r.price * 0.9f)
);
```

---

### v0.5.0 - Advanced SQL Features (planned)

SQL feature expansion for more expressive queries.

#### Features
- [ ] JOIN: INNER JOIN, LEFT JOIN, RIGHT JOIN, CROSS JOIN
- [ ] Aggregate functions: COUNT(), SUM(), AVG(), MIN(), MAX()
- [ ] GROUP BY / HAVING
- [ ] Subqueries
- [ ] DISTINCT
- [ ] UNION / INTERSECT / EXCEPT
- [ ] CASE expressions
- [ ] Functions (UPPER, LOWER, CONCAT, SUBSTRING, etc.)

#### Usage Example
```sql
-- JOIN
SELECT i.Name, c.CategoryName
FROM ItemTable i
INNER JOIN CategoryTable c ON i.CategoryId = c.Id

-- Aggregation
SELECT Category, COUNT(*) as Count, AVG(Price) as AvgPrice
FROM ItemTable
GROUP BY Category
HAVING COUNT(*) > 5

-- Subquery
SELECT * FROM ItemTable
WHERE Price > (SELECT AVG(Price) FROM ItemTable)
```

---

### v0.6.0 - Additional Tools (planned)

Additional tools to improve development efficiency.

#### Features
- [ ] Schema comparison tool - structural diff display, migration script generation
- [ ] Data migration - version management, automatic migration, rollback
- [ ] Backup & restore - snapshots, incremental backup, automatic scheduling
- [ ] Command line interface - CI/CD integration, batch import/export, schema validation
- [ ] Data generation tool - automatic test data generation, seed-based reproduction

#### Usage Example
```bash
xscriptabledb export --table ItemTable --output items.csv
xscriptabledb import --file items.csv --table ItemTable --validate
xscriptabledb schema compare --source ItemTable --target ItemTable_v2
xscriptabledb migrate --from v1 --to v2
```

---

### v1.0.0 - Stable Release

#### Goals
- [ ] Stabilize all features
- [ ] Comprehensive test coverage (90%+)
- [ ] Published performance benchmarks
- [ ] Complete API documentation
- [ ] Multiple sample projects
- [ ] Community feedback incorporated

---

## Under Consideration (priority TBD)

- [ ] **Real-time sync** - data synchronization across multiple editors
- [ ] **Encryption** - encrypted storage for sensitive data
- [ ] **Compression** - compressed storage for large datasets
- [ ] **Network support** - integration with remote databases
- [ ] **Visual query builder** - GUI-based query construction
- [ ] **Data visualization** - graph and chart display
- [ ] **Excel add-in** - direct editing from Excel
- [ ] **Other formats** - JSON, XML, SQLite integration

---

## Feedback

Feature requests and bug reports are welcome via GitHub Issues:
https://github.com/AraiYuhki/XScriptableDB/issues

Roadmap priorities are adjusted based on community feedback.
