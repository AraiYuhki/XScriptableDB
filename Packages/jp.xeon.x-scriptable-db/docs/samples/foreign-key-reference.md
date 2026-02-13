# Foreign Key Reference サンプル仕様書

## 概要

テーブル間の外部キー参照を活用したサンプルです。
複数のマスターデータを関連付けて使用する実践的なパターンを示します。

## 学習目標

1. 外部キー参照の定義方法
2. 参照先レコードの取得パターン
3. JOINライクなデータ結合
4. 整合性チェック
5. Inspector上での参照選択UI

---

## データ構造

### テーブル関連図

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│ CategoryTable│◄─────│  ItemTable  │─────►│ RarityTable │
│             │  1:N  │             │  N:1 │             │
│ id (PK)     │      │ id (PK)     │      │ id (PK)     │
│ name        │      │ name        │      │ name        │
│ description │      │ categoryId  │─┐    │ color       │
│ icon        │      │ rarityId    │─┘    │ multiplier  │
└─────────────┘      │ basePrice   │      └─────────────┘
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

### CategoryRecord（カテゴリマスタ）

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| id | int | カテゴリID（主キー） |
| name | string | カテゴリ名 |
| description | string | 説明 |
| sortOrder | int | 表示順 |

### RarityRecord（レアリティマスタ）

| フィールド名 | 型 | 説明 |
|-------------|-----|------|
| id | int | レアリティID（主キー） |
| name | string | レアリティ名（コモン、レア等） |
| color | Color | 表示色 |
| priceMultiplier | float | 価格倍率 |
| dropRate | float | ドロップ率 |

### ItemRecord（アイテムマスタ）

| フィールド名 | 型 | 外部キー | 説明 |
|-------------|-----|---------|------|
| id | int | - | アイテムID（主キー） |
| name | string | - | アイテム名 |
| description | string | - | 説明 |
| categoryId | int | CategoryTable | カテゴリID |
| rarityId | int | RarityTable | レアリティID |
| basePrice | int | - | 基本価格 |
| stackLimit | int | - | スタック上限 |

### RecipeRecord（レシピマスタ）

| フィールド名 | 型 | 外部キー | 説明 |
|-------------|-----|---------|------|
| id | int | - | レシピID（主キー） |
| name | string | - | レシピ名 |
| resultItemId | int | ItemTable | 完成アイテムID |
| resultCount | int | - | 完成数 |
| material1Id | int | ItemTable | 素材1 ID |
| material1Count | int | - | 素材1 必要数 |
| material2Id | int | ItemTable (nullable) | 素材2 ID |
| material2Count | int | - | 素材2 必要数 |
| material3Id | int | ItemTable (nullable) | 素材3 ID |
| material3Count | int | - | 素材3 必要数 |

---

## サンプルデータ

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

## GUI要件

### メインパネル

```
┌─────────────────────────────────────────────────────────┐
│ Foreign Key Reference サンプル                          │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ ─── アイテム一覧 ───────────────────────────────────   │
│                                                         │
│ フィルタ: [カテゴリ ▼] [レアリティ ▼]  [検索...]      │
│                                                         │
│ ┌────┬──────────┬────────┬──────────┬─────────┬──────┐ │
│ │ ID │ 名前     │カテゴリ│レアリティ│ 価格    │在庫数│ │
│ ├────┼──────────┼────────┼──────────┼─────────┼──────┤ │
│ │ 1  │ 木の剣   │ 武器   │ ●コモン │ 50G     │ 10   │ │
│ │ 2  │ 鉄の剣   │ 武器   │ ●アンコ │ 300G    │ 5    │ │
│ │ 3  │ 魔法の剣 │ 武器   │ ●レア   │ 3000G   │ 2    │ │
│ │ ...│ ...      │ ...    │ ...      │ ...     │ ...  │ │
│ └────┴──────────┴────────┴──────────┴─────────┴──────┘ │
│                                                         │
│ ─── アイテム詳細 ───────────────────────────────────   │
│                                                         │
│ ┌───────────────────────────────────────────────────┐   │
│ │ [アイコン]  魔法の剣                              │   │
│ │             ★★★ レア                            │   │
│ │                                                   │   │
│ │ カテゴリ: 武器                                    │   │
│ │ 説明: 魔力を帯びた剣                              │   │
│ │                                                   │   │
│ │ 基本価格: 1000G                                   │   │
│ │ 実売価格: 3000G (×3.0 レア補正)                  │   │
│ │                                                   │   │
│ │ ─── 製造レシピ ─────────────────                 │   │
│ │                                                   │   │
│ │ 素材:                                             │   │
│ │   ・鉄の剣 × 1                                   │   │
│ │   ・魔法の結晶 × 3                               │   │
│ │                                                   │   │
│ └───────────────────────────────────────────────────┘   │
│                                                         │
│ ─── レシピ一覧 ─────────────────────────────────────   │
│                                                         │
│ ┌────┬──────────┬────────────┬────────────────────────┐ │
│ │ ID │ レシピ名 │ 完成品     │ 必要素材               │ │
│ ├────┼──────────┼────────────┼────────────────────────┤ │
│ │ 1  │ 鉄の剣   │ 鉄の剣 ×1 │ 鉄鉱石 ×5             │ │
│ │ 2  │ 魔法の剣 │ 魔法の剣×1│ 鉄の剣×1, 魔法の結晶×3│ │
│ │ ...│ ...      │ ...        │ ...                    │ │
│ └────┴──────────┴────────────┴────────────────────────┘ │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### 参照選択ドロップダウン（Inspector用）

```
┌─────────────────────────────────────────┐
│ カテゴリID [武器 (ID:1)           ▼]   │
├─────────────────────────────────────────┤
│   武器 (ID:1)                           │
│   防具 (ID:2)                           │
│   消耗品 (ID:3)                         │
│   素材 (ID:4)                           │
│   その他 (ID:5)                         │
└─────────────────────────────────────────┘
```

---

## 使用するAPI

### 外部キー参照の定義

```csharp
[Serializable]
public class ItemRecord
{
    [SerializeField, PrimaryKey]
    private int id;

    [SerializeField]
    private string name;

    // 外部キー参照
    [SerializeField, ForeignKey(typeof(CategoryTable))]
    private int categoryId;

    [SerializeField, ForeignKey(typeof(RarityTable))]
    private int rarityId;
}
```

### 参照先レコードの取得

```csharp
// 方法1: 直接取得
var item = itemTable.FindByKey(3);
var category = categoryTable.FindByKey(item.CategoryId);
var rarity = rarityTable.FindByKey(item.RarityId);

// 方法2: 拡張メソッドを使用
var category = item.GetCategory(categoryTable);
var rarity = item.GetRarity(rarityTable);

// 方法3: JOINライクな結合（SQL Editor経由）
var sql = @"
    SELECT i.Name, c.Name AS CategoryName, r.Name AS RarityName
    FROM ItemTable i
    INNER JOIN CategoryTable c ON i.CategoryId = c.Id
    INNER JOIN RarityTable r ON i.RarityId = r.Id
    WHERE r.Id >= 3
";
var results = SqlExecutor.Execute(sql);
```

### 逆参照（1:N）

```csharp
// カテゴリに属するアイテムを全て取得
var weaponCategory = categoryTable.FindByKey(1);
var weapons = itemTable.FindAllBySecondaryKey("categoryId", weaponCategory.Id);

// レシピで素材として使用されているアイテムを検索
var recipes = recipeTable.All
    .Where(r => r.Material1Id == itemId
             || r.Material2Id == itemId
             || r.Material3Id == itemId);
```

### 価格計算（参照を使用）

```csharp
public int CalculatePrice(ItemRecord item, RarityTable rarityTable)
{
    var rarity = rarityTable.FindByKey(item.RarityId);
    return (int)(item.BasePrice * rarity.PriceMultiplier);
}
```

---

## 想定するユースケース

### ユースケース1: アイテム詳細表示

1. アイテムを選択
2. カテゴリとレアリティを外部キーから取得
3. レアリティの色でアイテム名を表示
4. 価格倍率を適用した実売価格を計算

### ユースケース2: フィルタリング

1. カテゴリドロップダウンで「武器」を選択
2. レアリティドロップダウンで「レア以上」を選択
3. 該当するアイテムのみを表示

### ユースケース3: レシピ表示

1. レシピを選択
2. 完成品アイテムを外部キーから取得
3. 各素材アイテムを外部キーから取得
4. 素材名と必要数を表示

### ユースケース4: 整合性チェック

1. 全アイテムのカテゴリIDを検証
2. 存在しないカテゴリIDを持つアイテムを検出
3. エラーレポートを生成

---

## ファイル構成

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
│   ├── ItemExtensions.cs        # 参照取得の拡張メソッド
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

## 注意事項

1. **循環参照**: 相互に参照し合うテーブルは避ける
2. **NULL参照**: オプショナルな外部キーは0またはnullable intを使用
3. **パフォーマンス**: 頻繁に参照するテーブルはキャッシュを検討
4. **削除時の整合性**: 参照先を削除する前に参照元を確認
5. **遅延ロード**: 参照先テーブルは必要な時にロードする設計も可能
