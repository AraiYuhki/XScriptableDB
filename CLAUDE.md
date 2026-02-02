# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Coding Guidelines (Unity / C# – for Claude)

This project is based on **Unity and C#**.
The following rules exist to prevent common issues related to **readability, performance, debugging, and long-term maintenance in Unity projects**.

When generating code, you **must follow these rules and respect their intent**, not just their literal wording.

### Naming Rules

* Do **not** use the `_` prefix for private methods.

**Reason**

* C# already clearly expresses visibility via access modifiers (`private`, `public`, etc.).
* In Unity projects, `_`-prefixed names are often confused with temporary variables, internal hacks, or auto-generated code.
* Avoids noise during refactoring and improves IDE autocomplete readability.

### Nesting Limits

* Excluding `namespace`, nesting is limited to **a maximum of 3 levels**.
* A single nested block (`{}`) must not exceed **100 lines**.

  * If it does, **split the logic into meaningful methods**.

**Reason**

* MonoBehaviour classes contain many lifecycle methods; deep nesting makes behavior hard to reason about.
* Deeply nested code complicates debugging, breakpoints, and stack tracing.
* Encourages code that communicates **intent**, not just execution flow.

### switch Statement Restrictions

* Inside a `switch` `case`, **do not use**:

  * `if`
  * `for`
  * `foreach`
  * `switch`

* Each `case` must be limited to **a maximum of 5 lines**, excluding `break`.

**Reason**

* `switch` statements should only represent **explicit state or value branching**.
* Embedding logic inside `case` blocks hides control flow and state transitions.
* Complex behavior should be delegated to **dedicated methods**, keeping each `case` simple and readable.

### Design Priority Order

When making implementation decisions, always prioritize in the following order:

1. **Readability**
2. **Performance**
3. **Robustness**
4. **Extensibility**

**Reason**

* Unity projects often require frequent iteration, tuning, and debugging.
* Performance matters early in Unity due to GC allocation, frame timing, and platform constraints.
* Optimized but unreadable code quickly becomes a maintenance risk.
* Extensibility is important, but not at the cost of clarity or runtime performance.

### Control Statement Style

* Even if an `if`, `for`, or `foreach` can be written on a single line, **always use two lines**.
* Omitting braces `{}` is allowed, but **single-line control statements are forbidden**.
* Braces are recommended for non-trivial logic.
* This rule applies to `switch` statements, not `switch` expressions.

#### Example

```csharp
if (flag)
    return;

foreach (var element in array)
    Process(element);
```

**Reason**

* Unity debugging frequently involves adding logs or breakpoints inside control blocks.
* Prevents future bugs caused by forgetting to add braces when extending logic.
* Produces cleaner diffs and improves code review clarity.

### Data Class Field Design

* Data class fields **must be `private`** by default.
* Provide **read-only accessors (getters)** for external access.
* Use `[SerializeField]` attribute to allow Unity serialization of private fields.
* For Editor-only mutability, use `[ReadOnly]` attribute — TableEditor generates code that allows editing only in Editor.

#### Example

```csharp
[Serializable]
public class ItemInfo : CsvData
{
    [SerializeField, CsvColumn("id")]
    private int id;

    [SerializeField, CsvColumn("name")]
    private string name;

    [SerializeField, CsvColumn("price")]
    private int price;

    public int Id => id;
    public string Name => name;
    public int Price => price;
}
```

**Reason**

* Master data must be **immutable at runtime** to ensure data integrity.
* Prevents accidental modification of shared data across the application.
* TableEditor uses `[ReadOnly]` to enable Editor-only editing while maintaining runtime immutability.
* Follows MasterMemory's philosophy of immutable, indexed, fast lookup.

---

## Project Overview

XScriptableDB is a Unity package providing a **ScriptableObject-based master data management system** for non-engineers. It follows MasterMemory's philosophy (Immutable, Indexed, Fast Lookup) while keeping complexity in the Editor.

- **Unity Version**: 6000.0+ required
- **Target Framework**: .NET Standard 2.1, C# 9.0+
- **Package Path**: `Packages/jp.xeon.x-scriptable-db/`
- **Dependencies**:
  - Addressables 2.3.7+
  - UniTask (optional)

### Core Concepts

| Concept | Description |
|---------|-------------|
| **Table** | ScriptableObject containing records sorted by PrimaryKey |
| **Record** | Data row implementing `IXRecord` or `CsvData` |
| **PrimaryKey** | Unique identifier, basis for sorting and O(log n) lookup |
| **SecondaryKey** | Optional indexes for O(1) lookup |
| **Index** | Pre-computed lookup table stored in ScriptableObject |

---

## Build & Test Commands

This is a Unity project. Use Unity Editor or Unity CLI for building and testing.

### Running Tests

**Via Unity Editor:**
- Window > General > Test Runner
- Edit Mode tab for unit tests
- Play Mode tab for integration tests

**Via CLI:**
```bash
# Edit Mode tests
Unity -runTests -testPlatform editmode -projectPath .

# Play Mode tests
Unity -runTests -testPlatform playmode -projectPath .
```

### Test Structure
- `Packages/jp.xeon.x-scriptable-db/Tests/` - Unit tests (CSV parsing, interfaces, etc.)

---

## Architecture

### Namespace Structure

```
Xeon.XScriptableDB
├── IO                    # CSV/TSV parsing
├── Schema                # Attribute definitions (planned)
├── Index                 # Index management (planned)
├── Query                 # Search API (planned)
└── Editor                # Editor tools
```

### Key Directories in `Packages/jp.xeon.x-scriptable-db/`

| Directory | Namespace | Purpose |
|-----------|-----------|---------|
| `Runtime/Core/` | `Xeon.XScriptableDB` | DB, TableBase, Interfaces |
| `Runtime/CSVParser/` | `Xeon.XScriptableDB.IO` | CSV/TSV parsing, encoding detection |
| `Runtime/Interface/` | `Xeon.XScriptableDB` | IIdentifiable, IImportable, IExportable |
| `Runtime/Attribute/` | `Xeon.XScriptableDB` | ReadOnly, AddressableObject attributes |
| `Runtime/Table/` | `Xeon.XScriptableDB` | TableBase<T> generic table class |
| `Editor/` | `Xeon.XScriptableDB.Editor` | DatabaseEditor, Generators, Inspectors |
| `Editor/Inspector/` | `Xeon.XScriptableDB.Editor` | Property drawers for references |
| `Tests/` | `Xeon.XScriptableDB.Tests` | Unit tests |

---

## Key Components

### CSV Parser (`Xeon.XScriptableDB.IO`)

```csharp
// Parse CSV string
var records = CsvParser.Parse<MyRecord>(csvString);

// Parse CSV file with auto-encoding detection
var records = CsvParser.ParseFile<MyRecord>(filePath);

// Export to CSV
var csv = CsvParser.ToCSV(records);
```

### Table Definition (Current)

```csharp
using Xeon.XScriptableDB;
using Xeon.XScriptableDB.IO;

[Serializable]
public class ItemInfo : CsvData
{
    [SerializeField, CsvColumn("id")]
    private int id;

    [SerializeField, CsvColumn("name")]
    private string name;

    [SerializeField, CsvColumn("price")]
    private int price;

    public int Id => id;
    public string Name => name;
    public int Price => price;
}

public class ItemTable : TableBase<ItemInfo>
{
    private Dictionary<int, ItemInfo> idIndex;

    protected override void Initialize()
    {
        idIndex = data.ToDictionary(x => x.Id);
    }

    public ItemInfo FindById(int id) => idIndex.GetValueOrDefault(id);
}
```

### Database Access

```csharp
using Xeon.XScriptableDB;

// Get table instance
var itemTable = DB.Get<ItemTable>();

// Access records
var allItems = itemTable.All;
var item = itemTable.FindById(5);
```

### Interfaces

```csharp
// For records with unique ID
public interface IIdentifiable
{
    int Id { get; }
    string Name { get; }
}

// For records with group classification
public interface IGroupIdentifiable
{
    int GroupId { get; }
    string Name { get; }
}

// For CSV export capability
public interface IExportable
{
    void Export(string filePath, Encoding encoding = null);
}

// For CSV import capability
public interface IImportable
{
    void Import(string filePath);
}
```

### Attributes

```csharp
// Mark private field as CSV column (with SerializeField for Unity serialization)
[SerializeField, CsvColumn("column_name")]
private int fieldName;

// Mark field as read-only in Inspector (Editor-only editing via TableEditor)
[SerializeField, ReadOnly]
private int readOnlyField;

// Mark field for Addressable asset reference
[SerializeField, AddressableObject]
private GameObject prefab;
```

---

## Editor Tools

### Database Editor
- Menu: `Tools/Master/データベースエディタ`
- Features: Table selection, record editing, TSV import/export

### Script Generators
- Menu: `Assets/Create/Scripting/Database/Database`
- Menu: `Assets/Create/Scripting/Database/Table`

### Inspector Extensions
- `MasterReferenceInspector`: Dropdown for IIdentifiable references
- `GroupReferenceInspector`: Dropdown for IGroupIdentifiable references
- `AddressableObjectInspector`: Addressable asset picker with address display

---

## Assembly Definitions

- `Xeon.XScriptableDB.asmdef` - Runtime library (planned)
- `Xeon.XScriptableDB.Editor.asmdef` - Editor-only code (planned)
- `Xeon.XScriptableDB.Tests.asmdef` - Tests (planned)

---

## Supported Types in CSV

| Type | Example CSV Value |
|------|-------------------|
| `int` | `123` |
| `float` | `1.5` |
| `double` | `1.5` |
| `bool` | `True`, `False` |
| `string` | `"text with spaces"` |
| `enum` | `EnumValue` |

---

## Development Roadmap

See `docs/roadmap.md` for detailed implementation phases:

0. **Phase 0 (Current Priority)**: Existing code improvements
   - CSV Parser bug fixes and test coverage
   - TableBase<T> generalization and redesign
   - DB class generalization and redesign
   - Assembly Definition setup
1. **Phase 1 (MVP)**: XTableAsset, PrimaryKey management, basic Editor
2. **Phase 2**: SecondaryKey indexes, high-speed search API
3. **Phase 3**: CSV import/export improvements, Diff Viewer
4. **Phase 4**: SQL Editor, DB Browser
5. **Phase 5**: Polish, documentation, Asset Store release

---

## Performance Goals

| Operation | Target | Notes |
|-----------|--------|-------|
| PrimaryKey lookup | O(log n) | Binary search |
| SecondaryKey lookup | O(1) | Hash-based index |
| Full scan | O(n) | Using Span<T> |
| GC Allocation | 0 | Runtime queries |

---

## Language

The codebase uses **Japanese** for:
- Comments
- Documentation
- Editor UI labels
- README.md

Code identifiers (class names, method names, variables) are in **English**.
