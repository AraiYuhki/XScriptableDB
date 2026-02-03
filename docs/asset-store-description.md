# XScriptableDB - Asset Store Description

## Short Description (250 characters max)

High-performance ScriptableObject database for Unity. Features O(1) hash lookup, zero GC queries, SQL Editor with JOIN/GROUP BY, CSV import with diff preview, schema migration, and comprehensive validation. Perfect for master data management.

---

## Full Description

### XScriptableDB - Professional Master Data Management for Unity

XScriptableDB is a powerful, production-ready database solution built on Unity's ScriptableObject system. Designed for game developers who need efficient master data management without external dependencies.

---

### Key Features

**High-Performance Search**
- O(log n) binary search with PrimaryKey
- O(1) hash lookup with SecondaryKey and Composite SecondaryKey
- Zero GC allocation queries using ref struct

**SQL Editor**
- Full SQL support: SELECT, UPDATE, DELETE
- JOIN operations: INNER, LEFT, RIGHT, CROSS
- Aggregate functions: COUNT, SUM, AVG, MIN, MAX
- GROUP BY, HAVING, ORDER BY, LIMIT, OFFSET
- CASE expressions and subqueries
- String functions: UPPER, LOWER, CONCAT, SUBSTRING, TRIM

**CSV/TSV Import & Export**
- Automatic encoding detection (UTF-8, Shift-JIS, etc.)
- Diff Viewer: Preview changes before applying
- Selective import: Choose which records to update
- Batch processing for large datasets

**Schema Management**
- Schema comparison between table versions
- Automatic migration code generation
- Backup and restore functionality

**Data Validation**
- Attribute-based validation: Required, Range, StringLength, Regex
- Foreign key integrity checking
- Unique constraint enforcement

**Developer Tools**
- Database Browser: View all tables and schemas
- Test Data Generator: Auto-generate sample data
- Performance Profiler: Monitor query performance
- CLI support for batch operations

---

### Why XScriptableDB?

| Feature | XScriptableDB | JSON Files | SQLite |
|---------|---------------|------------|--------|
| Unity Integration | Native | Manual | Plugin |
| Editor Tools | Rich | None | Limited |
| Runtime GC | Zero | High | Medium |
| Search Speed | O(1)/O(log n) | O(n) | O(log n) |
| No External Dependencies | Yes | Yes | No |

---

### Perfect For

- RPG item/skill/monster databases
- Localization tables
- Game configuration data
- Level/stage definitions
- Character stat tables
- Shop/inventory systems

---

### Quick Start

```csharp
// 1. Define your record
[Serializable]
public class ItemRecord
{
    [PrimaryKey]
    public int Id;

    [SecondaryKey]
    public string Category;

    public string Name;
    public int Price;
}

// 2. Create table asset
[CreateAssetMenu]
public class ItemTable : TableAsset<int, ItemRecord> { }

// 3. Search with blazing speed
var item = itemTable.Find(1001);                           // O(log n)
var weapons = itemTable.FindAllBySecondaryKey("Category", "Weapon");  // O(1)

// 4. Zero GC queries
using var result = itemTable.Where(r => r.Price > 1000);
foreach (ref readonly var record in result)
{
    Debug.Log(record.Name);
}
```

---

### Requirements

- Unity 6000.0 (Unity 6) or later
- .NET Standard 2.1

### Dependencies

- Addressables 2.3.7+ (included in Unity)

---

### Support

- Documentation: Included in package
- Email: xeon.lagunas@gmail.com
- GitHub: https://github.com/AraiYuhki/XScriptableDB

---

### Version History

**v1.0.0** - Initial Release
- Complete master data management solution
- SQL Editor with advanced query support
- Composite SecondaryKey indexes
- Schema migration tools
- Comprehensive test coverage

---

## Keywords (for Asset Store tagging)

- database
- master data
- scriptable object
- CSV
- SQL
- data management
- editor tool
- query
- indexing
- game data

---

## Technical Specifications

| Specification | Value |
|---------------|-------|
| Unity Version | 6000.0+ |
| .NET Version | Standard 2.1 |
| Platforms | All (Editor tools are Editor-only) |
| File Size | ~500KB |
| Dependencies | Addressables (Unity built-in) |

---

## Screenshots Needed

1. **SQL Editor** - Show SQL query execution with results
2. **Table Editor** - Display record editing interface
3. **Diff Viewer** - Show CSV import preview with changes highlighted
4. **Database Browser** - Display table list and schema view
5. **Code Example** - Show attribute-based record definition
6. **Performance Window** - Display cache statistics and profiling

---

## Promotional Text (for social media)

**Twitter/X (280 chars):**
XScriptableDB - High-performance master data management for Unity! O(1) hash lookup, SQL Editor with JOIN/GROUP BY, zero GC queries, CSV diff viewer. Perfect for RPG item tables, localization, and game configs. #Unity #GameDev #AssetStore

**Short tagline:**
"Master Data Management, Perfected for Unity"
