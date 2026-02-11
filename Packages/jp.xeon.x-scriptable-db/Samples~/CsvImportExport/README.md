# CSV Import/Export サンプル

CSV/TSVファイルのインポート・エクスポート機能を実演するサンプルです。

## 概要

このサンプルでは以下の機能を学習できます：

- CSVファイルからテーブルへのインポート
- テーブルからCSVファイルへのエクスポート
- エンコーディングの指定（UTF-8, Shift-JIS, 自動検出）
- 区切り文字の切り替え（CSV/TSV）
- 変更プレビュー（Diff Viewer）

## セットアップ

1. Package Managerからこのサンプルをインポート
2. `Assets > Create > XScriptableDB > Samples > CsvImportExport > CharacterTable` でテーブルアセットを作成
3. `Data/characters.csv` をインポートして初期データを取り込む

## ファイル構成

```
CsvImportExport/
├── Scripts/
│   ├── CharacterRecord.cs       # キャラクターレコード定義
│   ├── CharacterTable.cs        # テーブルアセット
│   └── CsvImportExportSample.cs # サンプルロジック
├── Data/
│   ├── characters.csv           # 初期データ（UTF-8）
│   ├── characters.tsv           # TSV形式
│   └── characters_update.csv    # 更新用データ
└── README.md
```

## データ構造

### CharacterRecord

| フィールド | 型 | CSV列名 | 説明 |
|-----------|-----|---------|------|
| id | int | ID | 主キー |
| name | string | 名前 | キャラクター名 |
| level | int | レベル | レベル |
| hp | int | HP | ヒットポイント |
| attack | int | 攻撃力 | 攻撃力 |
| defense | int | 防御力 | 防御力 |
| characterClass | string | 職業 | 職業（SecondaryKey） |
| isPlayable | bool | プレイアブル | プレイアブルか |

## 使い方

### インポート

```csharp
var sample = GetComponent<CsvImportExportSample>();

// UTF-8でインポート
sample.ImportCsv("path/to/characters.csv", Encoding.UTF8);

// エンコーディング自動検出
sample.ImportCsv("path/to/characters.csv");

// TSVファイル
sample.ImportCsv("path/to/characters.tsv", delimiter: '\t');
```

### エクスポート

```csharp
// UTF-8でエクスポート
sample.ExportCsv("path/to/output.csv", Encoding.UTF8);

// Shift-JISでエクスポート（Excel互換）
sample.ExportCsv("path/to/output.csv", Encoding.GetEncoding("Shift_JIS"));

// TSV形式
sample.ExportCsv("path/to/output.tsv", Encoding.UTF8, delimiter: '\t');
```

### プレビュー

```csharp
var preview = sample.PreviewImport("path/to/characters_update.csv");

Debug.Log($"追加: {preview.AddedRecords.Count}件");
Debug.Log($"更新: {preview.UpdatedRecords.Count}件");
Debug.Log($"削除: {preview.DeletedRecords.Count}件");

// 変更詳細
foreach (var change in preview.UpdatedRecords)
{
    Debug.Log($"ID={change.Id}: {change.Name}");
    foreach (var field in change.ChangedFields)
    {
        Debug.Log($"  {field.FieldName}: {field.OldValue} → {field.NewValue}");
    }
}
```

## 注意事項

- UTF-8（BOM付き）を推奨（Excel互換性のため）
- 日本語環境でExcelを使用する場合はShift-JISも選択可能
- カンマを含む値は自動的にダブルクォートで囲まれます
