# CSV/TSV Import & Export Guide

## Overview

XScriptableDB supports importing and exporting data in CSV (comma-separated) and TSV (tab-separated) formats.

## CsvColumn Attribute

Use the `[CsvColumn]` attribute to map CSV column names to fields.

```csharp
using Xeon.XScriptableDB.IO;

[Serializable]
public class ItemRecord
{
    [CsvColumn("ItemId")]
    public int Id;

    [CsvColumn("ItemName")]
    public string Name;

    [CsvColumn("Price")]
    public int Price;

    // Without CsvColumn, the field name is used as-is
    public int Attack;
}
```

## Export

### Basic Export

```csharp
using Xeon.XScriptableDB.Editor;

var settings = new ExportSettings
{
    FilePath = "Assets/Data/items.csv"
};

TableExporter.Export(tableAsset, settings);
```

### Export Settings

```csharp
var settings = new ExportSettings
{
    // Output file path (required)
    FilePath = "Assets/Data/items.csv",

    // Delimiter (default: comma)
    Delimiter = ',',  // Use '\t' for TSV

    // Encoding (default: UTF-8)
    Encoding = Encoding.UTF8,

    // Sort by PrimaryKey (default: true)
    SortByPrimaryKey = true,

    // Write BOM (default: false)
    WriteBom = false,

    // Column order (null = definition order)
    ColumnOrder = new[] { "Id", "Name", "Price" },

    // Columns to exclude
    ExcludeColumns = new[] { "InternalFlag" }
};
```

### Export for Excel

To open in Excel, include BOM or use Shift-JIS encoding:

```csharp
// UTF-8 with BOM (recommended for Excel 2016+)
var settings = new ExportSettings
{
    FilePath = "items.csv",
    Encoding = Encoding.UTF8,
    WriteBom = true
};

// Shift-JIS (for older Excel versions)
var settings = new ExportSettings
{
    FilePath = "items.csv",
    Encoding = Encoding.GetEncoding("Shift_JIS")
};
```

## Import

### Basic Import

```csharp
using Xeon.XScriptableDB.Editor;

// Import with preview (opens Diff Viewer)
TableImporter.ImportWithPreview(tableAsset, "Assets/Data/items.csv");
```

### Import Settings

```csharp
var settings = new ImportSettings
{
    // File path
    FilePath = "Assets/Data/items.csv",

    // Delimiter (auto-detection also available)
    Delimiter = ',',

    // Encoding (auto-detection also available)
    Encoding = Encoding.UTF8,

    // Whether the file has a header row
    HasHeader = true,

    // Skip empty lines
    SkipEmptyLines = true
};
```

### Auto-detection

```csharp
// Auto-detect file encoding
var encoding = TableImporter.DetectEncoding("items.csv");

// Auto-detect delimiter
var delimiter = TableImporter.DetectDelimiter("items.csv");
```

## Table Editor UI

Import and export via the editor UI:

1. Open `Window > XScriptableDB > Table Editor`
2. Select a table asset
3. Click the **Export** or **Import** button

### Import Workflow

1. Select a CSV file
2. The Diff Viewer opens with a preview of changes
3. Select the changes to apply
4. Click **Apply Selected** or **Apply All**

## CSV Format

### Basic Format

```csv
Id,Name,Price,Attack
1001,Iron Sword,100,10
1002,Steel Sword,500,25
```

### Escaping Special Characters

```csv
Id,Name,Description
1001,Iron Sword,"Beginner-friendly, affordable sword"
1002,"Special ""Sword""",Description text
```

- Values containing commas must be enclosed in double quotes
- Double quotes are escaped as `""`
- Values containing line breaks must also be enclosed in double quotes

## Supported Types

| Type | CSV Representation |
|------|-------------------|
| int, long | Integer (e.g. `123`) |
| float, double | Decimal (e.g. `3.14`) |
| bool | `true` / `false` or `1` / `0` |
| string | Text |
| enum | Enum value name (e.g. `Weapon`) |
| DateTime | ISO 8601 format (e.g. `2024-01-15`) |

## Troubleshooting

### Garbled Text

- CSV files saved from Excel may use Shift-JIS encoding
- Save as UTF-8 with BOM, or specify the encoding explicitly:

```csharp
var settings = new ImportSettings
{
    Encoding = Encoding.GetEncoding("Shift_JIS")
};
```

### Column Name Mismatch

- Verify that CSV column names match the names specified in the `[CsvColumn]` attribute
- Column name matching is case-insensitive

### Value Conversion Errors

- Check that numeric fields do not contain non-numeric values
- Empty cells are treated as the default value for the field type

## Bulk Operations

### Export All Tables

```csharp
// Export all TableAssets in the project
TableExporter.ExportAllTables("Assets/Export/");
```

### Import All Tables

```csharp
// Import all CSVs from the specified folder
TableImporter.ImportAllFromFolder("Assets/Import/");
```
