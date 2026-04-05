# CSV Import/Export Sample Specification

## Overview

This sample demonstrates CSV/TSV file import and export functionality.
It shows the basic workflow for importing master data edited in Excel or similar tools by planners and data managers.

## Learning Objectives

1. How to import from a CSV file into a table
2. How to export from a table to a CSV file
3. How to specify encoding (UTF-8, Shift-JIS, etc.)
4. How to switch delimiters (CSV/TSV)
5. Previewing changes with the Diff Viewer

---

## Data Structure

### CharacterRecord (Character Data)

| Field Name | Type | Description | CSV Column Name |
|-------------|------|-------------|-----------------|
| id | int | Character ID (primary key) | ID |
| name | string | Character name | 名前 |
| level | int | Level | レベル |
| hp | int | HP | HP |
| attack | int | Attack power | 攻撃力 |
| defense | int | Defense power | 防御力 |
| characterClass | string | Job class | 職業 |
| isPlayable | bool | Whether playable | プレイアブル |

### About CSV Column Names

- Japanese column names are used (to make it easy for planners to edit)
- Mapped via the `[CsvColumn("Japanese name")]` attribute

---

## Sample CSV Files

### characters.csv (UTF-8)

```csv
ID,名前,レベル,HP,攻撃力,防御力,職業,プレイアブル
1,勇者アレン,1,100,15,10,戦士,True
2,魔法使いリナ,1,60,8,5,魔法使い,True
3,僧侶マリア,1,80,5,12,僧侶,True
4,盗賊カイト,1,70,12,7,盗賊,True
5,ゴブリン,3,30,8,3,モンスター,False
6,スライム,1,10,3,1,モンスター,False
7,オーク,5,80,15,8,モンスター,False
8,ドラゴン,50,500,80,60,ボス,False
9,騎士レオン,10,150,25,30,騎士,True
10,弓使いエルフ,5,65,20,8,弓使い,True
```

### characters_update.csv (for updates)

```csv
ID,名前,レベル,HP,攻撃力,防御力,職業,プレイアブル
1,勇者アレン,5,130,22,15,戦士,True
2,魔法使いリナ,5,80,18,8,魔法使い,True
3,僧侶マリア,5,100,10,18,僧侶,True
11,賢者ソフィア,20,120,35,25,賢者,True
```

This update CSV contains the following changes:
- ID 1, 2, 3: Stat changes from leveling up
- ID 11: New character added

---

## GUI Requirements

### Main Panel

```
┌─────────────────────────────────────────────────────────┐
│ CSV Import/Export Sample                                │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ [Table Selection] CharacterTable ▼                     │
│                                                         │
│ ─── Import ───────────────────────────────────────     │
│                                                         │
│ File Path: [________________________] [Browse...]       │
│                                                         │
│ Encoding:        ○ UTF-8 (Recommended)                  │
│                   ○ Shift-JIS                           │
│                   ○ Auto-detect                         │
│                                                         │
│ Delimiter:       ○ Comma (CSV)                          │
│                   ○ Tab (TSV)                            │
│                                                         │
│ [Preview]  [Run Import]                                 │
│                                                         │
│ ─── Export ───────────────────────────────────────     │
│                                                         │
│ Output Path: [________________________] [Browse...]     │
│                                                         │
│ Encoding:        ○ UTF-8 (Recommended)  ○ Shift-JIS    │
│ Delimiter:       ○ Comma (CSV)          ○ Tab (TSV)    │
│ □ Sort by primary key                                   │
│                                                         │
│ [Run Export]                                            │
│                                                         │
│ ─── Current Data ─────────────────────────────────     │
│                                                         │
│ Record Count: 10                                        │
│                                                         │
│ ┌────┬──────────┬──────┬─────┬────────┬────────┬──────┐ │
│ │ ID │ Name     │ Lv   │ HP  │ Attack │ Defense│ Class│ │
│ ├────┼──────────┼──────┼─────┼────────┼────────┼──────┤ │
│ │ 1  │ Hero A.. │ 1    │ 100 │ 15     │ 10     │Warr..│ │
│ │ 2  │ Mage L.. │ 1    │ 60  │ 8      │ 5      │Mage  │ │
│ │ ...│ ...      │ ...  │ ... │ ...    │ ...    │ ...  │ │
│ └────┴──────────┴──────┴─────┴────────┴────────┴──────┘ │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Diff Viewer (displayed during preview)

```
┌─────────────────────────────────────────────────────────┐
│ Change Preview                                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ Change Summary:                                         │
│   Added: 1   Updated: 3   Deleted: 0                    │
│                                                         │
│ ─── Records to Add (green) ───────────────────────     │
│ ┌────┬──────────┬──────┬─────┬──────┬──────┬────────┐ │
│ │ 11 │ Sage S.. │ 20   │ 120 │ 35   │ 25   │ Sage   │ │
│ └────┴──────────┴──────┴─────┴──────┴──────┴────────┘ │
│                                                         │
│ ─── Records to Update (yellow) ───────────────────     │
│ ID=1: Hero Allen                                        │
│   Level: 1 → 5                                          │
│   HP: 100 → 130                                         │
│   Attack: 15 → 22                                       │
│   Defense: 10 → 15                                      │
│                                                         │
│ ID=2: Mage Lina                                         │
│   Level: 1 → 5                                          │
│   HP: 60 → 80                                           │
│   Attack: 8 → 18                                        │
│   Defense: 5 → 8                                        │
│                                                         │
│ ID=3: Cleric Maria                                      │
│   Level: 1 → 5                                          │
│   HP: 80 → 100                                          │
│   Attack: 5 → 10                                        │
│   Defense: 12 → 18                                      │
│                                                         │
│ [□ Select All]                                          │
│                                                         │
│ [Cancel]  [Apply Selected Changes]                      │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## APIs Used

### Import

```csharp
// Basic import
var records = CsvParser.ParseFile<CharacterRecord>(filePath);
characterTable.SetRecords(records);

// With encoding specified
var records = CsvParser.ParseFile<CharacterRecord>(filePath, Encoding.UTF8);
var records = CsvParser.ParseFile<CharacterRecord>(filePath, Encoding.GetEncoding("Shift_JIS"));

// For TSV
var records = CsvParser.ParseFile<CharacterRecord>(filePath, "\t");

// Import with preview (Editor)
TableImporter.ImportWithPreview(characterTable, filePath);
```

### Export

```csharp
// Basic export
var settings = new ExportSettings
{
    FilePath = outputPath,
    Delimiter = ",",
    Encoding = Encoding.UTF8,
    SortByPrimaryKey = true
};
TableExporter.Export(characterTable, settings);

// TSV export
settings.Delimiter = "\t";
settings.FilePath = "characters.tsv";
```

---

## Expected Use Cases

### Use Case 1: Initial Data Import

1. Planner creates character data in Excel
2. Save as a CSV file (UTF-8)
3. Select the file in the sample GUI
4. Verify the content in the preview
5. Run import

### Use Case 2: Updating Data

1. Export the existing table
2. Edit in Excel
3. Import the updated CSV (verify diff in preview)
4. Apply only the necessary changes

### Use Case 3: Multi-language Support

1. Japanese environment: use Shift-JIS CSV
2. International environment: use UTF-8 CSV
3. Auto-detect encoding automatically

---

## File Structure

```
Samples~/CsvImportExport/
├── README.md                    # Sample description
├── Scripts/
│   ├── CharacterRecord.cs       # Record class
│   ├── CharacterTable.cs        # Table class
│   └── CsvImportExportSample.cs # Sample logic (non-GUI portion)
├── Data/
│   ├── characters.csv           # Initial data (UTF-8)
│   ├── characters_sjis.csv      # Shift-JIS version
│   ├── characters.tsv           # TSV version
│   └── characters_update.csv    # Update data
└── Resources/
    └── CharacterTable.asset     # Pre-built table asset
```

---

## Notes

1. **BOM (Byte Order Mark)**: UTF-8 with BOM is recommended (Excel compatibility)
2. **Line endings**: Both CRLF and LF are supported
3. **Quoting**: Values containing commas are automatically enclosed in double quotes
4. **Escaping**: Double quotes within double-quoted values are doubled
5. **Empty values**: Empty strings are treated as null/default values
