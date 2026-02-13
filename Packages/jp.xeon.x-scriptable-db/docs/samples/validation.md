# Validation サンプル仕様書

## 概要

データ検証（バリデーション）機能のサンプルです。
属性ベースのバリデーションを使用して、マスターデータの品質を担保する方法を示します。

## 学習目標

1. バリデーション属性の使い方（Required, Range, StringLength等）
2. カスタムバリデーションの作成方法
3. 外部キー参照の検証
4. バリデーションエラーの表示と修正ワークフロー
5. 一括バリデーションの実行

---

## データ構造

### SkillRecord（スキルデータ）

| フィールド名 | 型 | バリデーション | 説明 |
|-------------|-----|---------------|------|
| id | int | PrimaryKey | スキルID |
| name | string | Required, StringLength(1-30) | スキル名 |
| description | string | StringLength(max: 200) | 説明文 |
| mpCost | int | Range(0-999) | MP消費量 |
| cooldown | float | Range(0-300) | クールダウン秒数 |
| power | int | Range(1-9999) | 威力 |
| elementType | ElementType | - | 属性タイプ（enum） |
| targetType | TargetType | - | 対象タイプ（enum） |
| requiredLevel | int | Range(1-100), Compare(<=, maxLevel) | 習得レベル |
| maxLevel | int | Range(1-100) | 最大強化レベル |

### WeaponRecord（武器データ）- 外部キー検証用

| フィールド名 | 型 | バリデーション | 説明 |
|-------------|-----|---------------|------|
| id | int | PrimaryKey | 武器ID |
| name | string | Required | 武器名 |
| skillId | int | ForeignKey(SkillTable) | 付与スキルID |
| attack | int | Range(1-9999) | 攻撃力 |
| rarity | int | Range(1-5) | レアリティ |

### Enum定義

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

## バリデーション属性一覧

| 属性 | 用途 | パラメータ例 |
|------|------|-------------|
| `[Required]` | 必須入力 | - |
| `[Range(min, max)]` | 数値範囲 | `[Range(0, 100)]` |
| `[StringLength(max)]` | 文字列長 | `[StringLength(30)]` |
| `[StringLength(min, max)]` | 文字列長範囲 | `[StringLength(1, 30)]` |
| `[RegularExpression(pattern)]` | 正規表現 | `[RegularExpression(@"^[A-Z]")]` |
| `[Unique]` | テーブル内一意 | - |
| `[ForeignKey(type)]` | 外部キー参照 | `[ForeignKey(typeof(SkillTable))]` |
| `[Compare(field, op)]` | フィールド比較 | `[Compare("maxLevel", CompareOperator.LessThanOrEqual)]` |

---

## サンプルデータ

### skills.csv（正常データ）

```csv
ID,スキル名,説明,MP消費,クールダウン,威力,属性,対象,習得Lv,最大Lv
1,ファイアボール,炎の弾を放つ,10,3.0,120,Fire,SingleEnemy,1,10
2,ヒール,HPを回復する,15,5.0,80,Light,SingleAlly,1,10
3,ブリザード,氷の嵐を起こす,30,10.0,200,Water,AllEnemies,15,5
4,プロテクト,防御力を上げる,20,0,0,None,SingleAlly,5,3
5,メテオ,隕石を落とす,100,60.0,999,Fire,AllEnemies,50,1
```

### skills_invalid.csv（エラーを含むデータ）

```csv
ID,スキル名,説明,MP消費,クールダウン,威力,属性,対象,習得Lv,最大Lv
1,,短すぎる説明,10,3.0,120,Fire,SingleEnemy,1,10
2,あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほ,説明文が長すぎるテスト用のデータです。これは200文字を超える説明文のテストです。,15,5.0,80,Light,SingleAlly,1,10
3,ブリザード,氷の嵐,-5,10.0,200,Water,AllEnemies,15,5
4,無効スキル,威力が範囲外,20,0,10000,None,SingleAlly,5,3
5,レベルエラー,習得Lv > 最大Lv,100,60.0,999,Fire,AllEnemies,50,30
```

エラー内容:
- ID 1: `name` が空（Required違反）
- ID 2: `name` が30文字超過（StringLength違反）、`description` が200文字超過
- ID 3: `mpCost` が負数（Range違反）
- ID 4: `power` が9999超過（Range違反）
- ID 5: `requiredLevel` > `maxLevel`（Compare違反）

### weapons.csv（外部キー検証用）

```csv
ID,武器名,スキルID,攻撃力,レアリティ
1,炎の剣,1,150,3
2,癒しの杖,2,50,2
3,氷の槍,3,180,4
4,無効武器,999,100,3
```

エラー内容:
- ID 4: `skillId` = 999 はSkillTableに存在しない（ForeignKey違反）

---

## GUI要件

### メインパネル

```
┌─────────────────────────────────────────────────────────┐
│ Validation サンプル                                     │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ [テーブル選択] SkillTable ▼    [バリデーション実行]    │
│                                                         │
│ ─── バリデーション結果 ─────────────────────────────   │
│                                                         │
│ ステータス: ⚠ 5件のエラー、2件の警告                   │
│                                                         │
│ [フィルタ] ○ 全て  ● エラーのみ  ○ 警告のみ          │
│                                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ❌ エラー: ID=1 [name] 必須フィールドが空です      │ │
│ │    → レコードを選択して修正                         │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ ❌ エラー: ID=2 [name] 30文字を超えています (35文字)│ │
│ │    → 現在値: "あいうえおかきくけこ..."             │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ ❌ エラー: ID=3 [mpCost] 範囲外です (0-999)        │ │
│ │    → 現在値: -5                                     │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ ❌ エラー: ID=4 [power] 範囲外です (1-9999)        │ │
│ │    → 現在値: 10000                                  │ │
│ ├─────────────────────────────────────────────────────┤ │
│ │ ❌ エラー: ID=5 [requiredLevel] maxLevelより大きい │ │
│ │    → requiredLevel=50, maxLevel=30                  │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ ─── 外部キー検証 ───────────────────────────────────   │
│                                                         │
│ [テーブル選択] WeaponTable ▼   [外部キー検証]          │
│                                                         │
│ ┌─────────────────────────────────────────────────────┐ │
│ │ ❌ エラー: ID=4 [skillId] 参照先が存在しません     │ │
│ │    → SkillTable に ID=999 が見つかりません         │ │
│ └─────────────────────────────────────────────────────┘ │
│                                                         │
│ ─── 一括バリデーション ─────────────────────────────   │
│                                                         │
│ [全テーブルを検証]                                      │
│                                                         │
│ 検証済み: 5/5 テーブル                                  │
│ 結果: SkillTable (5エラー), WeaponTable (1エラー)      │
│       ItemTable (OK), CharacterTable (OK), ...         │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

### エラー詳細ポップアップ（エラー行クリック時）

```
┌─────────────────────────────────────────────────────────┐
│ エラー詳細: SkillRecord ID=5                            │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ フィールド: requiredLevel                               │
│ バリデーション: Compare("maxLevel", <=)                 │
│                                                         │
│ エラー内容:                                             │
│   requiredLevel (50) は maxLevel (30) 以下である       │
│   必要があります。                                      │
│                                                         │
│ 現在の値:                                               │
│   requiredLevel = 50                                    │
│   maxLevel = 30                                         │
│                                                         │
│ 修正案:                                                 │
│   ○ requiredLevel を 30 に変更                         │
│   ○ maxLevel を 50 に変更                              │
│   ○ 手動で修正                                         │
│                                                         │
│ [キャンセル]  [修正を適用]                              │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 使用するAPI

### バリデーション実行

```csharp
// 単一テーブルのバリデーション（静的メソッド）
var results = RecordValidator.ValidateTable<SkillRecord>(skillTable);

foreach (var recordResult in results.RecordResults)
{
    foreach (var error in recordResult.Errors)
    {
        Debug.LogError($"[{recordResult.RecordIndex}] {error.FieldName}: {error.Message}");
    }
}

// 外部キー検証（静的メソッド）
var fkResults = ForeignKeyValidator.ValidateForeignKeys<WeaponRecord>(weaponTable, context);
```

### バリデーション属性の使用

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

## 想定するユースケース

### ユースケース1: CSVインポート前の検証

1. CSVファイルを準備
2. プレビューでインポート
3. バリデーションエラーを確認
4. CSVファイルを修正して再インポート

### ユースケース2: データ編集後の検証

1. Table Editorでデータを編集
2. 保存前にバリデーション実行
3. エラーがあれば修正
4. 全てパスしたら保存

### ユースケース3: リリース前の一括チェック

1. 「全テーブルを検証」を実行
2. 全テーブルのエラーを一覧表示
3. 各エラーを修正
4. 再検証して全てパスを確認

---

## ファイル構成

```
Samples~/Validation/
├── README.md                    # サンプルの説明
├── Scripts/
│   ├── SkillRecord.cs           # スキルレコード（バリデーション属性付き）
│   ├── SkillTable.cs            # スキルテーブル
│   ├── WeaponRecord.cs          # 武器レコード（外部キー参照）
│   ├── WeaponTable.cs           # 武器テーブル
│   ├── ElementType.cs           # 属性タイプenum
│   ├── TargetType.cs            # 対象タイプenum
│   └── ValidationSample.cs      # サンプルロジック
├── Data/
│   ├── skills.csv               # 正常データ
│   ├── skills_invalid.csv       # エラーを含むデータ
│   └── weapons.csv              # 外部キー検証用
└── Resources/
    ├── SkillTable.asset
    └── WeaponTable.asset
```

---

## 注意事項

1. **パフォーマンス**: 大量データの場合、バリデーションは非同期で実行することを推奨
2. **外部キー検証**: 参照先テーブルが未ロードの場合はスキップされる
3. **カスタムバリデーション**: `IValidatable` インターフェースで独自ロジックを追加可能
4. **エラーレベル**: Error（必須修正）、Warning（推奨修正）、Info（参考情報）
