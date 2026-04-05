# Validation Sample Specification

## Overview

This sample demonstrates data validation functionality.
It shows how to ensure master data quality using attribute-based validation.

## Learning Objectives

1. How to use validation attributes (Required, Range, StringLength, etc.)
2. How to create custom validations
3. Foreign key reference validation
4. Displaying validation errors and the correction workflow
5. Running batch validation

---

## Data Structure

### SkillRecord (Skill Data)

| Field Name | Type | Validation | Description |
|------------|------|-----------|-------------|
| id | int | PrimaryKey | Skill ID |
| name | string | Required, StringLength(1-30) | Skill name |
| description | string | StringLength(max: 200) | Description text |
| mpCost | int | Range(0-999) | MP cost |
| cooldown | float | Range(0-300) | Cooldown in seconds |
| power | int | Range(1-9999) | Power |
| elementType | ElementType | - | Element type (enum) |
| targetType | TargetType | - | Target type (enum) |
| requiredLevel | int | Range(1-100), Compare(<=, maxLevel) | Level to learn |
| maxLevel | int | Range(1-100) | Maximum upgrade level |

### WeaponRecord (Weapon Data) — for foreign key validation

| Field Name | Type | Validation | Description |
|------------|------|-----------|-------------|
| id | int | PrimaryKey | Weapon ID |
| name | string | Required | Weapon name |
| skillId | int | ForeignKey(SkillTable) | Granted skill ID |
| attack | int | Range(1-9999) | Attack power |
| rarity | int | Range(1-5) | Rarity |

### Enum Definitions

```csharp
public enum ElementType
{
    None,
    Fire,
    Water,
    Wind,
    Earth,
    Light,
    Dark
}

public enum TargetType
{
    Self,
    SingleEnemy,
    AllEnemies,
    SingleAlly,
    AllAllies,
    All
}
```

---

## Validation Attribute Reference

| Attribute | Purpose | Example Parameter |
|-----------|---------|-------------------|
| `[Required]` | Mandatory input | - |
| `[Range(min, max)]` | Numeric range | `[Range(0, 100)]` |
| `[StringLength(max)]` | String length | `[StringLength(30)]` |
| `[StringLength(min, max)]` | String length range | `[StringLength(1, 30)]` |
| `[RegularExpression(pattern)]` | Regular expression | `[RegularExpression(@"^[A-Z]")]` |
| `[Unique]` | Unique within table | - |
| `[ForeignKey(type)]` | Foreign key reference | `[ForeignKey(typeof(SkillTable))]` |
| `[Compare(field, op)]` | Field comparison | `[Compare("maxLevel", CompareOperator.LessThanOrEqual)]` |

---

## Sample Data

### skills.csv (valid data)

```csv
ID,スキル名,説明,MP消費,クールダウン,威力,属性,対象,習得Lv,最大Lv
1,ファイアボール,炎の弾を放つ,10,3.0,120,Fire,SingleEnemy,1,10
2,ヒール,HPを回復する,15,5.0,80,Light,SingleAlly,1,10
3,ブリザード,氷の嵐を起こす,30,10.0,200,Water,AllEnemies,15,5
4,プロテクト,防御力を上げる,20,0,0,None,SingleAlly,5,3
5,メテオ,隕石を落とす,100,60.0,999,Fire,AllEnemies,50,1
```

### skills_invalid.csv (data with errors)

```csv
ID,スキル名,説明,MP消費,クールダウン,威力,属性,対象,習得Lv,最大Lv
1,,短すぎる説明,10,3.0,120,Fire,SingleEnemy,1,10
2,あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほ,説明文が長すぎるテスト用のデータです。これは200文字を超える説明文のテストです。,15,5.0,80,Light,SingleAlly,1,10
3,ブリザード,氷の嵐,-5,10.0,200,Water,AllEnemies,15,5
4,無効スキル,威力が範囲外,20,0,10000,None,SingleAlly,5,3
5,レベルエラー,習得Lv > 最大Lv,100,60.0,999,Fire,AllEnemies,50,30
```

Errors:
- ID 1: `name` is empty (Required violation)
- ID 2: `name` exceeds 30 characters (StringLength violation), `description` exceeds 200 characters
- ID 3: `mpCost` is negative (Range violation)
- ID 4: `power` exceeds 9999 (Range violation)
- ID 5: `requiredLevel` > `maxLevel` (Compare violation)

### weapons.csv (for foreign key validation)

```csv
ID,武器名,スキルID,攻撃力,レアリティ
1,炎の剣,1,150,3
2,癒しの杖,2,50,2
3,氷の槍,3,180,4
4,無効武器,999,100,3
```

Errors:
- ID 4: `skillId` = 999 does not exist in SkillTable (ForeignKey violation)

---

## GUI Requirements

### Main Panel

```
┌─────────────────────────────────────────────────────────┐
│ Validation Sample                                       │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ [Table Selection] SkillTable ▼    [Run Validation]     │
│                                                         │
│ ─── Validation Results ────────────────────────────    │
│                                                         │
│ Status: ⚠ 5 errors, 2 warnings                          │
│                                                         │
│ [Filter] ○ All  ● Errors Only  ○ Warnings Only          │
│                                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ❌ Error: ID=1 [name] Required field is empty       │ │
│ │    → Select the record to fix                       │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ ❌ Error: ID=2 [name] Exceeds 30 characters (35)   │ │
│ │    → Current value: "あいうえおかきくけこ..."       │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ ❌ Error: ID=3 [mpCost] Out of range (0-999)       │ │
│ │    → Current value: -5                              │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ ❌ Error: ID=4 [power] Out of range (1-9999)       │ │
│ │    → Current value: 10000                           │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ ❌ Error: ID=5 [requiredLevel] Greater than maxLevel│ │
│ │    → requiredLevel=50, maxLevel=30                  │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ ─── Foreign Key Validation ─────────────────────────   │
│                                                         │
│ [Table Selection] WeaponTable ▼   [Validate Foreign Keys]│
│                                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ❌ Error: ID=4 [skillId] Referenced record not found│ │
│ │    → ID=999 not found in SkillTable                 │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ ─── Batch Validation ───────────────────────────────   │
│                                                         │
│ [Validate All Tables]                                   │
│                                                         │
│ Validated: 5/5 tables                                   │
│ Results: SkillTable (5 errors), WeaponTable (1 error)  │
│          ItemTable (OK), CharacterTable (OK), ...      │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Error Detail Popup (shown when clicking an error row)

```
┌─────────────────────────────────────────────────────────┐
│ Error Details: SkillRecord ID=5                         │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ Field: requiredLevel                                    │
│ Validation: Compare("maxLevel", <=)                     │
│                                                         │
│ Error:                                                  │
│   requiredLevel (50) must be less than or equal to      │
│   maxLevel (30).                                        │
│                                                         │
│ Current Values:                                         │
│   requiredLevel = 50                                    │
│   maxLevel = 30                                         │
│                                                         │
│ Suggested Fix:                                          │
│   ○ Change requiredLevel to 30                          │
│   ○ Change maxLevel to 50                               │
│   ○ Fix manually                                        │
│                                                         │
│ [Cancel]  [Apply Fix]                                   │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## APIs Used

### Running Validation

```csharp
// Validate a single table (static method)
var results = RecordValidator.ValidateTable<SkillRecord>(skillTable);

foreach (var recordResult in results.RecordResults)
{
    foreach (var error in recordResult.Errors)
    {
        Debug.LogError($"[{recordResult.RecordIndex}] {error.FieldName}: {error.Message}");
    }
}

// Foreign key validation (static method)
var fkResults = ForeignKeyValidator.ValidateForeignKeys<WeaponRecord>(weaponTable, context);
```

### Using Validation Attributes

```csharp
[Serializable]
public class SkillRecord
{
    [SerializeField, PrimaryKey]
    private int id;

    [SerializeField, Required, StringLength(1, 30)]
    private string name;

    [SerializeField, StringLength(200)]
    private string description;

    [SerializeField, Range(0, 999)]
    private int mpCost;

    [SerializeField, Range(1, 100)]
    [Compare("maxLevel", CompareOperator.LessThanOrEqual)]
    private int requiredLevel;

    [SerializeField, Range(1, 100)]
    private int maxLevel;
}

public class WeaponRecord
{
    [SerializeField, ForeignKey(typeof(SkillTable))]
    private int skillId;
}
```

---

## Expected Use Cases

### Use Case 1: Validation Before CSV Import

1. Prepare a CSV file
2. Import via preview
3. Review validation errors
4. Fix the CSV file and re-import

### Use Case 2: Validation After Data Editing

1. Edit data in the Table Editor
2. Run validation before saving
3. Fix any errors found
4. Save once all checks pass

### Use Case 3: Pre-Release Batch Check

1. Run "Validate All Tables"
2. Display a list of errors across all tables
3. Fix each error
4. Re-validate and confirm all pass

---

## File Structure

```
Samples~/Validation/
├── README.md                    # Sample description
├── Scripts/
│   ├── SkillRecord.cs           # Skill record (with validation attributes)
│   ├── SkillTable.cs            # Skill table
│   ├── WeaponRecord.cs          # Weapon record (with foreign key reference)
│   ├── WeaponTable.cs           # Weapon table
│   ├── ElementType.cs           # Element type enum
│   ├── TargetType.cs            # Target type enum
│   └── ValidationSample.cs      # Sample logic
├── Data/
│   ├── skills.csv               # Valid data
│   ├── skills_invalid.csv       # Data with errors
│   └── weapons.csv              # For foreign key validation
└── Resources/
    ├── SkillTable.asset
    └── WeaponTable.asset
```

---

## Notes

1. **Performance**: For large datasets, it is recommended to run validation asynchronously
2. **Foreign key validation**: Skipped if the referenced table is not loaded
3. **Custom validation**: Custom logic can be added via the `IValidatable` interface
4. **Error levels**: Error (must fix), Warning (recommended fix), Info (informational)
