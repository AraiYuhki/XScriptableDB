# Validation Sample

A sample demonstrating attribute-based data validation.

## Overview

This sample covers the following features:

- How to use validation attributes (Required, Range, StringLength, etc.)
- Foreign key reference validation (ForeignKey)
- Cross-field comparison (Compare)
- Reviewing and fixing validation errors

## Setup

1. Import this sample from Package Manager
2. Create table assets from `Assets > Create > XScriptableDB > Samples > Validation`:
   - SkillTable
   - WeaponTable
3. Import `Data/skills.csv` and `Data/weapons.csv`

## File Structure

```
Validation/
├── Scripts/
│   ├── ElementType.cs       # Element type enum
│   ├── TargetType.cs        # Target type enum
│   ├── SkillRecord.cs       # Skill record (with validation attributes)
│   ├── SkillTable.cs        # Skill table
│   ├── WeaponRecord.cs      # Weapon record (with foreign key reference)
│   ├── WeaponTable.cs       # Weapon table
│   └── ValidationSample.cs  # Sample logic
├── Data/
│   ├── skills.csv           # Valid data
│   ├── skills_invalid.csv   # Data containing errors
│   └── weapons.csv          # For foreign key validation
└── README.md
```

## Validation Attribute Examples

### SkillRecord

```csharp
[Serializable]
public class SkillRecord
{
    [SerializeField, PrimaryKey]
    private int id;

    // Required, 1–30 characters
    [SerializeField, Required, StringLength(1, 30)]
    private string name;

    // Max 200 characters
    [SerializeField, StringLength(200)]
    private string description;

    // Range 0–999
    [SerializeField, Range(0, 999)]
    private int mpCost;

    // Range 1–100, and must be <= maxLevel
    [SerializeField, Range(1, 100)]
    [Compare("maxLevel", CompareOperator.LessThanOrEqual)]
    private int requiredLevel;

    [SerializeField, Range(1, 100)]
    private int maxLevel;
}
```

### WeaponRecord (Foreign Key)

```csharp
[Serializable]
public class WeaponRecord
{
    [SerializeField, PrimaryKey]
    private int id;

    [SerializeField, Required]
    private string name;

    // Foreign key reference to SkillTable
    [SerializeField, ForeignKey(typeof(SkillTable))]
    private int skillId;
}
```

## Usage

```csharp
var skillResult = RecordValidator.ValidateTable<SkillRecord>(skillTable);
if (!skillResult.IsValid)
{
    foreach (var recordResult in skillResult.RecordResults)
    {
        foreach (var error in recordResult.Errors)
            Debug.LogError($"[{recordResult.RecordIndex}] {error.FieldName}: {error.Message}");
    }
}

var weaponResult = ForeignKeyValidator.ValidateForeignKeys<WeaponRecord>(weaponTable, context);
```

## Testing with Error Data

`Data/skills_invalid.csv` contains the following errors:

| ID | Error |
|----|-------|
| 1 | `name` is empty (Required violation) |
| 2 | `name` exceeds 30 characters, `description` exceeds 200 characters |
| 3 | `mpCost` is negative (Range violation) |
| 4 | `power` exceeds 9999 (Range violation) |
| 5 | `requiredLevel` > `maxLevel` (Compare violation) |

`Data/weapons.csv` ID=5 references a non-existent skill ID (999).

## Validation Attributes

| Attribute | Description |
|-----------|-------------|
| `[Required]` | Required field (null/empty not allowed) |
| `[Range(min, max)]` | Numeric range restriction |
| `[StringLength(max)]` | Maximum string length |
| `[StringLength(min, max)]` | String length range |
| `[ForeignKey(type)]` | Foreign key reference validation |
| `[Compare(field, op)]` | Comparison with another field |
| `[Unique]` | Uniqueness within the table |
| `[RegularExpression(pattern)]` | Regex-based validation |
