# Foreign Key Reference Sample

A practical sample demonstrating cross-table foreign key references.

## Overview

This sample covers the following features:

- How to define foreign key references
- Patterns for retrieving referenced records
- Simplifying reference lookups with extension methods
- Linking multiple tables

## Table Structure

```
CategoryTable ◄── ItemTable ──► RarityTable
                     │
                     ▼
               RecipeTable
```

- **CategoryTable**: Category master (weapons, armor, consumables, etc.)
- **RarityTable**: Rarity master (Common through Legendary)
- **ItemTable**: Item master (references category and rarity)
- **RecipeTable**: Recipe master (references items)

## Setup

1. Import this sample from Package Manager
2. Create each table asset from `Assets > Create > XScriptableDB > Samples > ForeignKeyReference`
3. Import the CSVs in the `Data/` folder

## File Structure

```
ForeignKeyReference/
├── Scripts/
│   ├── CategoryRecord.cs
│   ├── CategoryTable.cs
│   ├── RarityRecord.cs
│   ├── RarityTable.cs
│   ├── ItemRecord.cs
│   ├── ItemTable.cs
│   ├── RecipeRecord.cs
│   ├── RecipeTable.cs
│   ├── ItemExtensions.cs          # Extension methods
│   └── ForeignKeyReferenceSample.cs
├── Data/
│   ├── categories.csv
│   ├── rarities.csv
│   ├── items.csv
│   └── recipes.csv
└── README.md
```

## Defining Foreign Keys

```csharp
[Serializable]
public class ItemRecord
{
    [SerializeField, PrimaryKey]
    private int id;

    // Foreign key reference to CategoryTable
    [SerializeField, ForeignKey(typeof(CategoryTable))]
    private int categoryId;

    // Foreign key reference to RarityTable
    [SerializeField, ForeignKey(typeof(RarityTable))]
    private int rarityId;
}
```

## Retrieving Referenced Records

### Method 1: Direct lookup

```csharp
var item = itemTable.FindByKey(3);
var category = categoryTable.FindByKey(item.CategoryId);
var rarity = rarityTable.FindByKey(item.RarityId);
```

### Method 2: Extension methods

```csharp
var item = itemTable.FindByKey(3);
var category = item.GetCategory(categoryTable);
var rarity = item.GetRarity(rarityTable);
var actualPrice = item.GetActualPrice(rarityTable);
```

### Method 3: Reverse lookup via SecondaryKey

```csharp
// Get all items in the weapon category
var weapons = itemTable.FindAllBySecondaryKey("categoryId", 1);

// Get items with rarity 3 or higher
var rareItems = itemTable.All.Where(i => i.RarityId >= 3);
```

## Sample Usage

```csharp
var sample = GetComponent<ForeignKeyReferenceSample>();

var detail = sample.GetItemDetail(3);
Debug.Log($"{detail.Item.Name} ({detail.Category.Name})");
Debug.Log($"Rarity: {detail.Rarity.Name}");
Debug.Log($"Price: {detail.ActualPrice}G");

var recipeDetail = sample.GetRecipeDetail(2);
Debug.Log($"Result: {recipeDetail.ResultItem.Name} x{recipeDetail.ResultCount}");
foreach (var mat in recipeDetail.Materials)
    Debug.Log($"  Material: {mat.Item.Name} x{mat.Count}");
```

## Notes

- A foreign key value of 0 is treated as "not set"
- If the referenced record does not exist, `FindByKey()` returns null
- Avoid circular references
