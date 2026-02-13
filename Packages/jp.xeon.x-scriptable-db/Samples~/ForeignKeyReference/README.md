# Foreign Key Reference サンプル

テーブル間の外部キー参照を活用した実践的なサンプルです。

## 概要

このサンプルでは以下の機能を学習できます：

- 外部キー参照の定義方法
- 参照先レコードの取得パターン
- 拡張メソッドによる参照取得の簡略化
- 複数テーブルの関連付け

## テーブル構成

```
CategoryTable ◄── ItemTable ──► RarityTable
                     │
                     ▼
               RecipeTable
```

- **CategoryTable**: カテゴリマスタ（武器、防具、消耗品等）
- **RarityTable**: レアリティマスタ（コモン〜レジェンダリー）
- **ItemTable**: アイテムマスタ（カテゴリとレアリティを参照）
- **RecipeTable**: レシピマスタ（アイテムを参照）

## セットアップ

1. Package Managerからこのサンプルをインポート
2. `Assets > Create > XScriptableDB > Samples > ForeignKeyReference` から各テーブルアセットを作成
3. `Data/` フォルダ内のCSVをインポート

## ファイル構成

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
│   ├── ItemExtensions.cs          # 拡張メソッド
│   └── ForeignKeyReferenceSample.cs
├── Data/
│   ├── categories.csv
│   ├── rarities.csv
│   ├── items.csv
│   └── recipes.csv
└── README.md
```

## 外部キーの定義

```csharp
[Serializable]
public class ItemRecord
{
    [SerializeField, PrimaryKey]
    private int id;

    // CategoryTableへの外部キー参照
    [SerializeField, ForeignKey(typeof(CategoryTable))]
    private int categoryId;

    // RarityTableへの外部キー参照
    [SerializeField, ForeignKey(typeof(RarityTable))]
    private int rarityId;
}
```

## 参照の取得方法

### 方法1: 直接取得

```csharp
var item = itemTable.FindByKey(3);
var category = categoryTable.FindByKey(item.CategoryId);
var rarity = rarityTable.FindByKey(item.RarityId);
```

### 方法2: 拡張メソッド

```csharp
var item = itemTable.FindByKey(3);
var category = item.GetCategory(categoryTable);
var rarity = item.GetRarity(rarityTable);
var actualPrice = item.GetActualPrice(rarityTable);
```

### 方法3: SecondaryKeyで逆引き

```csharp
// 武器カテゴリのアイテムを全て取得
var weapons = itemTable.FindAllBySecondaryKey("categoryId", 1);

// レアリティ3以上のアイテムを取得
var rareItems = itemTable.All.Where(i => i.RarityId >= 3);
```

## サンプルの使い方

```csharp
var sample = GetComponent<ForeignKeyReferenceSample>();

// アイテム詳細を取得
var detail = sample.GetItemDetail(3);
Debug.Log($"{detail.Item.Name} ({detail.Category.Name})");
Debug.Log($"レアリティ: {detail.Rarity.Name}");
Debug.Log($"価格: {detail.ActualPrice}G");

// カテゴリでフィルタリング
var weapons = sample.GetItemsByCategory(1);

// レシピ詳細を取得
var recipeDetail = sample.GetRecipeDetail(2);
Debug.Log($"完成品: {recipeDetail.ResultItem.Name} × {recipeDetail.ResultCount}");
foreach (var mat in recipeDetail.Materials)
{
    Debug.Log($"  素材: {mat.Item.Name} × {mat.Count}");
}

// アイテムを使用しているレシピを検索
var recipes = sample.FindRecipesUsingItem(8); // 鉄鉱石を使うレシピ
```

## 注意事項

- 外部キーの値が0の場合は「未設定」として扱います
- 参照先が存在しない場合、`FindByKey()`はnullを返します
- 循環参照は避けてください
