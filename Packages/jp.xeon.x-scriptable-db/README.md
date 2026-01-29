# XScriptableDB

Unity用のScriptableObjectベースのデータベースパッケージです。マスターデータの管理、CSV/TSVインポート・エクスポート、SQLライクなクエリ機能を提供します。

## 特徴

- **ScriptableObjectベース**: Unityのアセットシステムと完全に統合
- **高速検索**: PrimaryKeyによるO(log n)のバイナリサーチ、SecondaryKeyによるO(1)のハッシュルックアップ
- **GC Alloc 0**: ref structを使用したメモリ効率の良いクエリ結果
- **CSV/TSV対応**: インポート・エクスポート機能、エンコーディング自動検出
- **Diff Viewer**: インポート前の変更プレビュー、選択的な適用
- **SQL Editor**: SQLライクなクエリでデータ検索・更新
- **Database Browser**: テーブル一覧とスキーマ確認

## 動作環境

- Unity 6000.0 以上
- .NET Standard 2.1

## インストール

### Package Manager (Git URL)

1. Unityで `Window > Package Manager` を開く
2. `+` ボタンをクリックし、`Add package from git URL...` を選択
3. 以下のURLを入力:
```
https://github.com/AraiYuhki/XScriptableDB.git?path=Packages/jp.xeon.x-scriptable-db
```

### manifest.json

`Packages/manifest.json` に以下を追加:
```json
{
  "dependencies": {
    "jp.xeon.x-scriptable-db": "https://github.com/AraiYuhki/XScriptableDB.git?path=Packages/jp.xeon.x-scriptable-db"
  }
}
```

## クイックスタート

### 1. レコードクラスの定義

```csharp
using System;
using Xeon.XScriptableDB;

[Serializable]
public class ItemRecord
{
    [PrimaryKey]
    public int Id;

    [SecondaryKey]
    public string Category;

    [CsvColumn("アイテム名")]
    public string Name;

    public int Price;
    public int Attack;
    public int Defense;
}
```

### 2. TableAssetの作成

```csharp
using UnityEngine;
using Xeon.XScriptableDB;

[CreateAssetMenu(fileName = "ItemTable", menuName = "Database/ItemTable")]
public class ItemTable : TableAsset<int, ItemRecord>
{
}
```

### 3. データの検索

```csharp
// PrimaryKeyで検索（O(log n)）
var item = itemTable.Find(1001);

// SecondaryKeyで検索（O(1)）
var weapons = itemTable.FindAllBySecondaryKey("Category", "Weapon");

// LINQライクなクエリ
using var result = itemTable.Where(r => r.Price > 1000);
foreach (ref readonly var item in result)
{
    Debug.Log(item.Name);
}
```

## 主要なコンポーネント

### 属性

| 属性 | 説明 |
|------|------|
| `[PrimaryKey]` | 主キーを指定。一意である必要があり、バイナリサーチで高速検索 |
| `[SecondaryKey]` | 副キーを指定。ハッシュインデックスで高速検索 |
| `[CsvColumn("名前")]` | CSV/TSVでのカラム名を指定 |

### TableAsset<TKey, TRecord>

ScriptableObjectベースのテーブルクラスです。

```csharp
// 基本的な検索
TRecord Find(TKey key);
bool TryFind(TKey key, out TRecord record);

// SecondaryKeyによる検索
TRecord FindBySecondaryKey<TSecondaryKey>(string keyName, TSecondaryKey key);
IEnumerable<TRecord> FindAllBySecondaryKey<TSecondaryKey>(string keyName, TSecondaryKey key);

// LINQライクなクエリ（GC Alloc 0）
QueryResult<TRecord> Where(Func<TRecord, bool> predicate);
```

### QueryResult<T>

GC Allocationなしでクエリ結果を扱うためのref structです。

```csharp
using var result = table.Where(r => r.IsActive);

// foreachで列挙
foreach (ref readonly var record in result)
{
    // ...
}

// インデクサでアクセス
var first = result[0];

// 件数
int count = result.Count;
```

## エディタ機能

### Table Editor

`Window > XScriptableDB > Table Editor`

- テーブルデータの編集
- CSV/TSVインポート・エクスポート
- 一括操作

### SQL Editor

`Window > XScriptableDB > SQL Editor`

SQLライクなクエリでデータを検索・更新できます。

```sql
-- 検索
SELECT * FROM ItemTable WHERE Category = 'Weapon' AND Price > 1000 ORDER BY Price DESC LIMIT 10

-- 更新
UPDATE ItemTable SET Price = 500 WHERE Id = 1001

-- 削除
DELETE FROM ItemTable WHERE Price = 0
```

**サポートされる構文:**
- SELECT: カラム指定, WHERE, ORDER BY (ASC/DESC), LIMIT, OFFSET
- UPDATE: SET, WHERE
- DELETE: WHERE
- 演算子: =, !=, <>, <, <=, >, >=, LIKE, IN, IS NULL, IS NOT NULL
- 論理演算子: AND, OR
- 括弧によるグループ化

### Database Browser

`Window > XScriptableDB > Database Browser`

- プロジェクト内のテーブル一覧
- スキーマ情報（カラム、キー）
- データプレビュー

### Diff Viewer

CSVインポート時に変更内容をプレビューし、選択的に適用できます。

- 追加されたレコード（緑）
- 削除されたレコード（赤）
- 変更されたレコード（黄）
- フィールドレベルの差分表示

## CSV/TSV形式

### インポート

```csharp
// Editorスクリプトから
TableImporter.ImportWithPreview(tableAsset, "path/to/data.csv");
```

### エクスポート

```csharp
var settings = new ExportSettings
{
    FilePath = "path/to/output.csv",
    Delimiter = ',',
    Encoding = Encoding.UTF8,
    SortByPrimaryKey = true
};
TableExporter.Export(tableAsset, settings);
```

### CSVフォーマット

```csv
Id,Category,Name,Price,Attack,Defense
1001,Weapon,鉄の剣,100,10,0
1002,Weapon,鋼の剣,500,25,0
1003,Armor,皮の鎧,80,0,5
```

## ベストプラクティス

### PrimaryKeyの選択

- 一意で変更されない値を使用
- intやlongなどの値型を推奨（比較が高速）

### SecondaryKeyの活用

- 頻繁に検索するカテゴリやタイプに設定
- 複数のSecondaryKeyを設定可能

### クエリの最適化

```csharp
// Good: ref structを使用してGC Allocを回避
using var result = table.Where(r => r.IsActive);

// 注意: ToList()はGC Allocが発生
var list = table.Where(r => r.IsActive).ToList();
```

### CSVインポートのワークフロー

1. CSVファイルを準備
2. Table Editorでインポート（プレビュー付き）
3. Diff Viewerで変更を確認
4. 必要なレコードのみ適用

## サンプル

`Packages/jp.xeon.x-scriptable-db/Samples~` フォルダにサンプルが含まれています。

Package Managerからインポートできます:
1. Package Managerで `XScriptableDB` を選択
2. `Samples` タブを開く
3. 必要なサンプルの `Import` をクリック

## ロードマップ

今後の開発予定については [ROADMAP.md](ROADMAP.md) を参照してください。

**計画中の機能:**
- v0.3.0: パフォーマンス最適化（キャッシュ、遅延ロード）
- v0.4.0: 高度なSQL機能（JOIN, 集計関数, GROUP BY）
- v0.5.0: 追加ツール（マイグレーション, CLI, バックアップ）

## ライセンス

MIT OR Apache-2.0

## 作者

Xeon ([@AraiYuhki](https://github.com/AraiYuhki))
