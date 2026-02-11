# XScriptableDB Demo

XScriptableDBの主要機能を体験できるデモサンプルです。

## セットアップ手順

### 1. テーブルアセットの作成

1. Projectウィンドウで右クリック
2. `Create > XScriptableDB > Samples > DemoItemTable` を選択
3. 作成されたアセットを選択し、Inspectorで `DemoItems.csv` をインポート

### 2. シーンの作成

1. 新規シーンを作成
2. 空のGameObjectを作成し、`DemoManager` スクリプトをアタッチ
3. `DemoManager` の `Item Table` フィールドに作成したテーブルアセットを設定

### 3. UI（オプション）

より良い体験のために、以下のUIを作成することを推奨します：

```
Canvas
├── Panel (Left)
│   ├── Button - "全レコード表示"      → ShowAllRecords()
│   ├── Button - "PrimaryKey検索"      → SearchByPrimaryKey()
│   ├── Button - "SecondaryKey検索"    → SearchBySecondaryKey()
│   ├── Button - "複合Key検索"         → SearchByCompositeKey()
│   ├── Button - "Where検索"           → SearchWithWhere()
│   ├── Button - "範囲検索"            → SearchInRange()
│   ├── Button - "集計関数"            → ShowAggregation()
│   └── Button - "パフォーマンス比較"   → ShowPerformanceComparison()
├── InputField                         → searchInput
└── Panel (Right)
    └── Text (Scroll View)             → outputText
```

### 4. 実行

1. Playモードで実行
2. 各ボタンをクリックして機能を確認
3. InputFieldに値を入力して検索条件を変更

## 検索例

| 機能 | 入力例 | 説明 |
|------|--------|------|
| PrimaryKey検索 | `1001` | IDで検索 |
| SecondaryKey検索 | `Weapon` | カテゴリで検索 |
| 複合Key検索 | `Weapon/3` | カテゴリ/レアリティで検索 |
| Where検索 | `500` | 価格が指定値以上 |
| 範囲検索 | `1001-1010` | ID範囲で検索 |

## ファイル構成

- `DemoManager.cs` - デモのメインロジック
- `DemoItemRecord.cs` - レコード定義（複合SecondaryKey含む）
- `DemoItemTable.cs` - テーブル定義
- `DemoItems.csv` - サンプルデータ
