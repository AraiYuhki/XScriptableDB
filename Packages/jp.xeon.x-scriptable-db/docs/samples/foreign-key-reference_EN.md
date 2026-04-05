# Foreign Key Reference Sample Specification

## Overview

This sample demonstrates the use of foreign key references between tables.
It shows practical patterns for relating and using multiple master data tables together.

## Learning Objectives

1. How to define foreign key references
2. Patterns for retrieving referenced records
3. JOIN-like data joining
4. Referential integrity checks
5. Reference selection UI in the Inspector

---

## Data Structure

### Table Relationship Diagram

```
┌─────────────────┐      ┌─────────────┐      ┌─────────────┐
│  CategoryTable  │◄─────│  ItemTable  │─────►│ RarityTable │
│                 │  1:N  │             │  N:1 │             │
│ id (PK)         │      │ id (PK)     │      │ id (PK)     │
│ name            │      │ name        │      │ name        │
│ description     │      │ categoryId  │─┐    │ color       │
│ icon            │      │ rarityId    │─┘    │ multiplier  │
└─────────────────┘      │ basePrice   │      └─────────────┘
                         │ ...         │
                         └─────────────┘
                               │
                               │ 1:N
                               ▼
                         ┌─────────────┐
                         │ RecipeTable │
                         │             │
                         │ id (PK)     │
                         │ resultItemId│
                         │ material1Id │
                         │ material2Id │
                         │ material3Id │
                         └─────────────┘
```

### CategoryRecord (Category Master)

| Field Name | Type | Description |
|------------|------|-------------|
| id | int | Category ID (primary key) |
| name | string | Category name |
| description | string | Description |
| sortOrder | int | Display order |

### RarityRecord (Rarity Master)

| Field Name | Type | Description |
|------------|------|-------------|
| id | int | Rarity ID (primary key) |
| name | string | Rarity name (Common, Rare, etc.) |
| color | Color | Display color |
| priceMultiplier | float | Price multiplier |
| dropRate | float | Drop rate |

### ItemRecord (Item Master)

| Field Name | Type | Foreign Key | Description |
|------------|------|-------------|-------------|
| id | int | - | Item ID (primary key) |
| name | string | - | Item name |
| description | string | - | Description |
| categoryId | int | CategoryTable | Category ID |
| rarityId | int | RarityTable | Rarity ID |
| basePrice | int | - | Base price |
| stackLimit | int | - | Stack limit |

### RecipeRecord (Recipe Master)

| Field Name | Type | Foreign Key | Description |
|------------|------|-------------|-------------|
| id | int | - | Recipe ID (primary key) |
| name | string | - | Recipe name |
| resultItemId | int | ItemTable | Result item ID |
| resultCount | int | - | Result quantity |
| material1Id | int | ItemTable | Material 1 ID |
| material1Count | int | - | Material 1 required quantity |
| material2Id | int | ItemTable (nullable) | Material 2 ID |
| material2Count | int | - | Material 2 required quantity |
| material3Id | int | ItemTable (nullable) | Material 3 ID |
| material3Count | int | - | Material 3 required quantity |

---

## Sample Data

### categories.csv

```csv
ID,カテゴリ名,説明,表示順
1,武器,攻撃に使用する装備品,1
2,防具,防御に使用する装備品,2
3,消耗品,使用すると消費されるアイテム,3
4,素材,クラフトに使用する素材,4
5,その他,分類できないアイテム,99
```

### rarities.csv

```csv
ID,レアリティ名,カラーR,カラーG,カラーB,価格倍率,ドロップ率
1,コモン,0.8,0.8,0.8,1.0,0.5
2,アンコモン,0.2,0.8,0.2,1.5,0.3
3,レア,0.2,0.4,1.0,3.0,0.15
4,エピック,0.6,0.2,0.8,5.0,0.04
5,レジェンダリー,1.0,0.6,0.0,10.0,0.01
```

### items.csv

```csv
ID,アイテム名,説明,カテゴリID,レアリティID,基本価格,スタック上限
1,木の剣,初心者用の剣,1,1,50,1
2,鉄の剣,標準的な剣,1,2,200,1
3,魔法の剣,魔力を帯びた剣,1,3,1000,1
4,皮の鎧,軽量な防具,2,1,80,1
5,鉄の鎧,標準的な防具,2,2,300,1
6,回復薬,HPを50回復する,3,1,30,99
7,魔力の水,MPを30回復する,3,2,50,99
8,鉄鉱石,鍛冶の素材,4,1,10,999
9,魔法の結晶,魔法付与の素材,4,3,100,99
10,伝説の剣,伝説に語られる剣,1,5,50000,1
```

### recipes.csv

```csv
ID,レシピ名,完成品ID,完成数,素材1ID,素材1数,素材2ID,素材2数,素材3ID,素材3数
1,鉄の剣,2,1,8,5,0,0,0,0
2,魔法の剣,3,1,2,1,9,3,0,0
3,鉄の鎧,5,1,8,10,0,0,0,0
4,回復薬,6,3,0,0,0,0,0,0
5,魔力の水,7,2,9,1,0,0,0,0
```

---

## GUI Requirements

### Main Panel

```
┌─────────────────────────────────────────────────────────┐
│ Foreign Key Reference Sample                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ ─── Item List ─────────────────────────────────────    │
│                                                         │
│ Filter: [Category ▼] [Rarity ▼]  [Search...]           │
│                                                         │
│ ┌────┬──────────┬──────────┬──────────┬─────────┬──────┐ │
│ │ ID │ Name     │ Category │ Rarity   │ Price   │ Stock│ │
│ ├────┼──────────┼──────────┼──────────┼─────────┼──────┤ │
│ │ 1  │ Wood Sw..│ Weapon   │ ●Common │ 50G     │ 10   │ │
│ │ 2  │ Iron Sw..│ Weapon   │ ●Uncomm │ 300G    │ 5    │ │
│ │ 3  │ Magic S..│ Weapon   │ ●Rare   │ 3000G   │ 2    │ │
│ │ ...│ ...      │ ...      │ ...      │ ...     │ ...  │ │
│ └────┴──────────┴──────────┴──────────┴─────────┴──────┘ │
│                                                         │
│ ─── Item Details ──────────────────────────────────    │
│                                                         │
│ ┌───────────────────────────────────────────────────┐   │
│ │ [Icon]      Magic Sword                           │   │
│ │             ★★★ Rare                             │   │
│ │                                                   │   │
│ │ Category: Weapon                                  │   │
│ │ Description: A sword imbued with magic power      │   │
│ │                                                   │   │
│ │ Base Price: 1000G                                 │   │
│ │ Actual Price: 3000G (×3.0 Rare bonus)             │   │
│ │                                                   │   │
│ │ ─── Crafting Recipe ────────────────────         │   │
│ │                                                   │   │
│ │ Materials:                                        │   │
│ │   · Iron Sword × 1                               │   │
│ │   · Magic Crystal × 3                            │   │
│ │                                                   │   │
│ └───────────────────────────────────────────────────┘   │
│                                                         │
│ ─── Recipe List ───────────────────────────────────    │
│                                                         │
│ ┌────┬──────────────┬──────────────┬───────────────────┐ │
│ │ ID │ Recipe Name  │ Result       │ Required Materials│ │
│ ├────┼──────────────┼──────────────┼───────────────────┤ │
│ │ 1  │ Iron Sword   │ Iron Sword×1 │ Iron Ore×5        │ │
│ │ 2  │ Magic Sword  │ Magic Sw.×1  │ Iron Sw×1, Crys×3 │ │
│ │ ...│ ...          │ ...          │ ...               │ │
│ └────┴──────────────┴──────────────┴───────────────────┘ │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### Reference Selection Dropdown (for Inspector)

```
┌─────────────────────────────────────────────────────────┐
│ Category ID [Weapon (ID:1)                ▼]            │
├─────────────────────────────────────────────────────────┤
│   Weapon (ID:1)                                         │
│   Armor (ID:2)                                          │
│   Consumable (ID:3)                                     │
│   Material (ID:4)                                       │
│   Other (ID:5)                                          │
└─────────────────────────────────────────────────────────┘
```

---

## APIs Used

### Defining Foreign Key References

```csharp
[Serializable]
public class ItemRecord
{
    [SerializeField, PrimaryKey]
    private int id;

    [SerializeField]
    private string name;

    // Foreign key reference
    [SerializeField, ForeignKey(typeof(CategoryTable))]
    private int categoryId;

    [SerializeField, ForeignKey(typeof(RarityTable))]
    private int rarityId;
}
```

### Retrieving Referenced Records

```csharp
// Method 1: Direct lookup
var item = itemTable.FindByKey(3);
var category = categoryTable.FindByKey(item.CategoryId);
var rarity = rarityTable.FindByKey(item.RarityId);

// Method 2: Using extension methods
var category = item.GetCategory(categoryTable);
var rarity = item.GetRarity(rarityTable);

// Method 3: JOIN-like join (via SQL Editor)
var sql = @"
    SELECT i.Name, c.Name AS CategoryName, r.Name AS RarityName
    FROM ItemTable i
    INNER JOIN CategoryTable c ON i.CategoryId = c.Id
    INNER JOIN RarityTable r ON i.RarityId = r.Id
    WHERE r.Id >= 3
";
var results = SqlExecutor.Execute(sql);
```

### Reverse Reference (1:N)

```csharp
// Get all items belonging to a category
var weaponCategory = categoryTable.FindByKey(1);
var weapons = itemTable.FindAllBySecondaryKey("categoryId", weaponCategory.Id);

// Find items used as materials in recipes
var recipes = recipeTable.All
    .Where(r => r.Material1Id == itemId
             || r.Material2Id == itemId
             || r.Material3Id == itemId);
```

### Price Calculation (Using References)

```csharp
public int CalculatePrice(ItemRecord item, RarityTable rarityTable)
{
    var rarity = rarityTable.FindByKey(item.RarityId);
    return (int)(item.BasePrice * rarity.PriceMultiplier);
}
```

---

## Expected Use Cases

### Use Case 1: Item Detail Display

1. Select an item
2. Retrieve category and rarity from foreign keys
3. Display the item name in the rarity's color
4. Calculate the actual selling price with the price multiplier applied

### Use Case 2: Filtering

1. Select "Weapon" in the category dropdown
2. Select "Rare or higher" in the rarity dropdown
3. Display only matching items

### Use Case 3: Recipe Display

1. Select a recipe
2. Retrieve the result item from the foreign key
3. Retrieve each material item from the foreign key
4. Display material names and required quantities

### Use Case 4: Referential Integrity Check

1. Validate the category ID of all items
2. Detect items with a category ID that does not exist
3. Generate an error report

---

## File Structure

```
Samples~/ForeignKeyReference/
├── README.md
├── Scripts/
│   ├── CategoryRecord.cs
│   ├── CategoryTable.cs
│   ├── RarityRecord.cs
│   ├── RarityTable.cs
│   ├── ItemRecord.cs
│   ├── ItemTable.cs
│   ├── RecipeRecord.cs
│   ├── RecipeTable.cs
│   ├── ItemExtensions.cs        # Extension methods for retrieving references
│   └── ForeignKeyReferenceSample.cs
├── Data/
│   ├── categories.csv
│   ├── rarities.csv
│   ├── items.csv
│   └── recipes.csv
└── Resources/
    ├── CategoryTable.asset
    ├── RarityTable.asset
    ├── ItemTable.asset
    └── RecipeTable.asset
```

---

## Notes

1. **Circular references**: Avoid tables that mutually reference each other
2. **Null references**: Use 0 or nullable int for optional foreign keys
3. **Performance**: Consider caching frequently referenced tables
4. **Integrity on deletion**: Check referencing records before deleting a referenced record
5. **Lazy loading**: Referenced tables can also be designed to load on demand
