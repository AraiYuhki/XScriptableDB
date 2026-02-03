# Validation サンプル

属性ベースのデータ検証（バリデーション）機能を実演するサンプルです。

## 概要

このサンプルでは以下の機能を学習できます：

- バリデーション属性の使い方（Required, Range, StringLength等）
- 外部キー参照の検証（ForeignKey）
- フィールド間比較（Compare）
- バリデーションエラーの確認と修正

## セットアップ

1. Package Managerからこのサンプルをインポート
2. `Assets > Create > XScriptableDB > Samples > Validation` からテーブルアセットを作成
   - SkillTable
   - WeaponTable
3. `Data/skills.csv` と `Data/weapons.csv` をインポート

## ファイル構成

```
Validation/
├── Scripts/
│   ├── ElementType.cs       # 属性タイプenum
│   ├── TargetType.cs        # 対象タイプenum
│   ├── SkillRecord.cs       # スキルレコード（バリデーション属性付き）
│   ├── SkillTable.cs        # スキルテーブル
│   ├── WeaponRecord.cs      # 武器レコード（外部キー参照）
│   ├── WeaponTable.cs       # 武器テーブル
│   └── ValidationSample.cs  # サンプルロジック
├── Data/
│   ├── skills.csv           # 正常データ
│   ├── skills_invalid.csv   # エラーを含むデータ
│   └── weapons.csv          # 外部キー検証用
└── README.md
```

## バリデーション属性の例

### SkillRecord

```csharp
[Serializable]
public class SkillRecord
{
    [SerializeField, PrimaryKey]
    private int id;

    // 必須、1-30文字
    [SerializeField, Required, StringLength(1, 30)]
    private string name;

    // 最大200文字
    [SerializeField, StringLength(200)]
    private string description;

    // 0-999の範囲
    [SerializeField, Range(0, 999)]
    private int mpCost;

    // 1-100、かつmaxLevel以下であること
    [SerializeField, Range(1, 100)]
    [Compare("maxLevel", CompareOperator.LessThanOrEqual)]
    private int requiredLevel;

    [SerializeField, Range(1, 100)]
    private int maxLevel;
}
```

### WeaponRecord（外部キー）

```csharp
[Serializable]
public class WeaponRecord
{
    [SerializeField, PrimaryKey]
    private int id;

    [SerializeField, Required]
    private string name;

    // SkillTableへの外部キー参照
    [SerializeField, ForeignKey(typeof(SkillTable))]
    private int skillId;
}
```

## 使い方

```csharp
var sample = GetComponent<ValidationSample>();

// スキルテーブルの検証
var skillResult = sample.ValidateSkillTable();
if (!skillResult.IsValid)
{
    foreach (var error in skillResult.Errors)
    {
        Debug.LogError($"ID={error.RecordId}: {error.Message}");
    }
}

// 武器テーブルの検証（外部キー含む）
var weaponResult = sample.ValidateWeaponTable();

// 全テーブル一括検証
var allResults = sample.ValidateAll();
```

## エラーデータのテスト

`Data/skills_invalid.csv` には以下のエラーが含まれています：

| ID | エラー内容 |
|----|-----------|
| 1 | `name` が空（Required違反） |
| 2 | `name` が30文字超過、`description` が200文字超過 |
| 3 | `mpCost` が負数（Range違反） |
| 4 | `power` が9999超過（Range違反） |
| 5 | `requiredLevel` > `maxLevel`（Compare違反） |

`Data/weapons.csv` の ID=5 は存在しないスキルID(999)を参照しています。

## バリデーション属性一覧

| 属性 | 説明 |
|------|------|
| `[Required]` | 必須フィールド（null/空文字禁止） |
| `[Range(min, max)]` | 数値の範囲制限 |
| `[StringLength(max)]` | 文字列の最大長 |
| `[StringLength(min, max)]` | 文字列の長さ範囲 |
| `[ForeignKey(type)]` | 外部キー参照の検証 |
| `[Compare(field, op)]` | 他フィールドとの比較 |
| `[Unique]` | テーブル内での一意性 |
| `[RegularExpression(pattern)]` | 正規表現による検証 |
