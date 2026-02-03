# CSV/TSV インポート・エクスポート ガイド

## 概要

XScriptableDBは、CSV（カンマ区切り）およびTSV（タブ区切り）形式でのデータのインポート・エクスポートをサポートしています。

## CsvColumn属性

`[CsvColumn]` 属性を使用して、CSVのカラム名とフィールドをマッピングします。

```csharp
using Xeon.XScriptableDB.IO;

[Serializable]
public class ItemRecord
{
    [CsvColumn("アイテムID")]
    public int Id;

    [CsvColumn("アイテム名")]
    public string Name;

    [CsvColumn("価格")]
    public int Price;

    // CsvColumnを省略すると、フィールド名がそのまま使用される
    public int Attack;
}
```

## エクスポート

### 基本的なエクスポート

```csharp
using Xeon.XScriptableDB.Editor;

var settings = new ExportSettings
{
    FilePath = "Assets/Data/items.csv"
};

TableExporter.Export(tableAsset, settings);
```

### エクスポート設定

```csharp
var settings = new ExportSettings
{
    // 出力先ファイルパス（必須）
    FilePath = "Assets/Data/items.csv",

    // 区切り文字（デフォルト: カンマ）
    Delimiter = ',',  // TSVの場合は '\t'

    // エンコーディング（デフォルト: UTF-8）
    Encoding = Encoding.UTF8,

    // PrimaryKeyでソートするか（デフォルト: true）
    SortByPrimaryKey = true,

    // BOMを出力するか（デフォルト: false）
    WriteBom = false,

    // カラムの順序（nullの場合は定義順）
    ColumnOrder = new[] { "Id", "Name", "Price" },

    // 除外するカラム
    ExcludeColumns = new[] { "InternalFlag" }
};
```

### Excel用エクスポート

Excelで開く場合は、BOMを含めるかShift-JISを使用：

```csharp
// UTF-8 with BOM（Excel 2016以降推奨）
var settings = new ExportSettings
{
    FilePath = "items.csv",
    Encoding = Encoding.UTF8,
    WriteBom = true
};

// Shift-JIS（古いExcel用）
var settings = new ExportSettings
{
    FilePath = "items.csv",
    Encoding = Encoding.GetEncoding("Shift_JIS")
};
```

## インポート

### 基本的なインポート

```csharp
using Xeon.XScriptableDB.Editor;

// プレビュー付きインポート（Diff Viewerが開く）
TableImporter.ImportWithPreview(tableAsset, "Assets/Data/items.csv");
```

### インポート設定

```csharp
var settings = new ImportSettings
{
    // ファイルパス
    FilePath = "Assets/Data/items.csv",

    // 区切り文字（自動検出も可能）
    Delimiter = ',',

    // エンコーディング（自動検出も可能）
    Encoding = Encoding.UTF8,

    // ヘッダー行をスキップするか
    HasHeader = true,

    // 空行をスキップするか
    SkipEmptyLines = true
};
```

### エンコーディングの自動検出

```csharp
// ファイルのエンコーディングを自動検出
var encoding = TableImporter.DetectEncoding("items.csv");

// 区切り文字を自動検出
var delimiter = TableImporter.DetectDelimiter("items.csv");
```

## Table Editor UI

エディタUIを使用したインポート・エクスポート：

1. `Window > XScriptableDB > Table Editor` を開く
2. テーブルアセットを選択
3. 「Export」または「Import」ボタンをクリック

### インポートワークフロー

1. CSVファイルを選択
2. Diff Viewerが開き、変更内容をプレビュー
3. 適用したい変更を選択
4. 「Apply Selected」または「Apply All」をクリック

## CSVフォーマット

### 基本形式

```csv
Id,Name,Price,Attack
1001,鉄の剣,100,10
1002,鋼の剣,500,25
```

### 特殊文字のエスケープ

```csv
Id,Name,Description
1001,鉄の剣,"初心者向け、安価な剣"
1002,"特殊な""剣""",説明文
```

- カンマを含む値はダブルクォートで囲む
- ダブルクォートは `""` でエスケープ
- 改行を含む値もダブルクォートで囲む

### 日本語カラム名

```csv
アイテムID,アイテム名,価格
1001,鉄の剣,100
1002,鋼の剣,500
```

`[CsvColumn]` 属性で日本語カラム名をマッピング。

## 対応する型

| 型 | CSVでの表現 |
|---|---|
| int, long | 整数 (例: `123`) |
| float, double | 小数 (例: `3.14`) |
| bool | `true` / `false` または `1` / `0` |
| string | テキスト |
| enum | 列挙値の名前 (例: `Weapon`) |
| DateTime | ISO 8601形式 (例: `2024-01-15`) |

## トラブルシューティング

### 文字化け

- Excelで保存したCSVはShift-JISになることがある
- UTF-8 with BOMで保存するか、エンコーディングを明示的に指定

```csharp
var settings = new ImportSettings
{
    Encoding = Encoding.GetEncoding("Shift_JIS")
};
```

### カラムが一致しない

- CSVのカラム名とCsvColumn属性の名前が一致しているか確認
- 大文字小文字は区別されない

### 値の変換エラー

- 数値フィールドに数値以外が入っていないか確認
- 空のセルはデフォルト値として扱われる

## 一括操作

### 全テーブルのエクスポート

```csharp
// プロジェクト内の全TableAssetをエクスポート
TableExporter.ExportAllTables("Assets/Export/");
```

### 全テーブルのインポート

```csharp
// 指定フォルダ内の全CSVをインポート
TableImporter.ImportAllFromFolder("Assets/Import/");
```
