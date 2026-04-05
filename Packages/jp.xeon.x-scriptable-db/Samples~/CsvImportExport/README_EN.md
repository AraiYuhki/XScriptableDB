# CSV Import/Export Sample

A sample demonstrating CSV/TSV file import and export functionality.

## Overview

This sample covers the following features:

- Importing data from CSV files into a table
- Exporting data from a table to a CSV file
- Specifying encoding (UTF-8, Shift-JIS, auto-detect)
- Switching delimiters (CSV/TSV)
- Change preview (Diff Viewer)

## Setup

1. Import this sample from Package Manager
2. Create a table asset via `Assets > Create > XScriptableDB > Samples > CsvImportExport > CharacterTable`
3. Import `Data/characters.csv` to load the initial data

## File Structure

```
CsvImportExport/
├── Scripts/
│   ├── CharacterRecord.cs       # Character record definition
│   ├── CharacterTable.cs        # Table asset
│   └── CsvImportExportSample.cs # Sample logic
├── Data/
│   ├── characters.csv           # Initial data (UTF-8)
│   ├── characters.tsv           # TSV format
│   └── characters_update.csv    # Update data
└── README.md
```

## Data Structure

### CharacterRecord

| Field | Type | CSV Column | Description |
|-------|------|------------|-------------|
| id | int | ID | Primary key |
| name | string | Name | Character name |
| level | int | Level | Level |
| hp | int | HP | Hit points |
| attack | int | Attack | Attack power |
| defense | int | Defense | Defense power |
| characterClass | string | Class | Class (SecondaryKey) |
| isPlayable | bool | Playable | Whether playable |

## Usage

### Import

```csharp
var sample = GetComponent<CsvImportExportSample>();

// Import with UTF-8
sample.ImportCsv("path/to/characters.csv", Encoding.UTF8);

// Auto-detect encoding
sample.ImportCsv("path/to/characters.csv");

// TSV file
sample.ImportCsv("path/to/characters.tsv", delimiter: "\t");
```

### Export

```csharp
// Export with UTF-8
sample.ExportCsv("path/to/output.csv", Encoding.UTF8);

// Export with Shift-JIS (Excel compatible)
sample.ExportCsv("path/to/output.csv", Encoding.GetEncoding("Shift_JIS"));

// TSV format
sample.ExportCsv("path/to/output.tsv", Encoding.UTF8, delimiter: "\t");
```

### Preview

```csharp
var preview = sample.PreviewImport("path/to/characters_update.csv");

Debug.Log($"Added: {preview.AddedRecords.Count}");
Debug.Log($"Updated: {preview.UpdatedRecords.Count}");
Debug.Log($"Deleted: {preview.DeletedRecords.Count}");

foreach (var change in preview.UpdatedRecords)
{
    Debug.Log($"ID={change.Id}: {change.Name}");
    foreach (var field in change.ChangedFields)
    {
        Debug.Log($"  {field.FieldName}: {field.OldValue} → {field.NewValue}");
    }
}
```

## Notes

- UTF-8 with BOM is recommended (for Excel compatibility)
- Shift-JIS can also be used in Japanese Excel environments
- Values containing commas are automatically enclosed in double quotes
