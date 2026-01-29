# XScriptableDB 詳細実装計画書

## 1. 現状分析

### 1.1 既存実装の概要

| コンポーネント | 名前空間 | 状態 | 説明 |
|---------------|---------|------|------|
| CSVParser | `Xeon.XScriptableDB.IO` | 実装済み | CSV/TSVの読み書き |
| TableBase | `Xeon.XScriptableDB` | 実装済み | テーブルの基底クラス |
| DB | `Xeon.XScriptableDB` | 実装済み | データベースアクセス（シングルトン） |
| Interfaces | `Xeon.XScriptableDB` | 実装済み | IIdentifiable, IGroupIdentifiable, IImportable, IExportable |
| Attributes | `Xeon.XScriptableDB` | 実装済み | ReadOnly, AddressableObject |
| Editor Tools | `Xeon.XScriptableDB.Editor` | 部分実装 | DatabaseEditor, TableGenerator等 |

### 1.2 既存コードの課題

1. **TableBase<T>** - PrimaryKey/SecondaryKeyの概念なし
2. **DB.cs** - テーブル登録が手動（ハードコーディング）
3. **検索機能** - インデックスベースの高速検索未実装
4. **Editor** - Excelライクな操作UI未実装
5. **Diff Viewer** - 未実装
6. **SQL Editor** - 未実装

---

## 2. アーキテクチャ設計

### 2.1 名前空間構成

```
Xeon.XScriptableDB
├── IO                    # CSV/TSVパーサー（既存）
├── Schema                # スキーマ定義（新規）
│   ├── Attributes
│   └── Validation
├── Index                 # インデックス管理（新規）
├── Query                 # クエリ/検索（新規）
└── Editor                # エディター拡張（既存拡張）
    ├── Browser
    ├── TableEditor
    ├── DiffViewer
    └── SqlEditor
```

### 2.2 クラス設計

#### 2.2.1 Core クラス

```csharp
// テーブルアセット（新規）
public abstract class XTableAsset<TRecord> : ScriptableObject
    where TRecord : IXRecord
{
    [SerializeField] private TRecord[] records;
    [SerializeField] private XIndexData indexData;

    public ReadOnlySpan<TRecord> Records => records;
    public XIndexData Indexes => indexData;
}

// レコードインターフェース（新規）
public interface IXRecord
{
    // PrimaryKeyを返す
    object GetPrimaryKey();
}

// インデックスデータ（新規）
[Serializable]
public class XIndexData
{
    public Dictionary<string, int[]> SecondaryIndexes;
}
```

#### 2.2.2 スキーマ定義 Attributes

```csharp
// PrimaryKey属性（新規）
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class PrimaryKeyAttribute : Attribute { }

// SecondaryKey属性（新規）
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
public class SecondaryKeyAttribute : Attribute
{
    public string IndexName { get; }
    public SecondaryKeyAttribute(string indexName = null) => IndexName = indexName;
}

// Validation属性（新規）
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ValidationAttribute : Attribute
{
    public ValidationType Type { get; }
    public object[] Parameters { get; }
}
```

---

## 3. 機能別詳細設計

### 3.1 Phase 1: MVP（基盤整備）

#### 3.1.1 XTableAsset<T> 実装

**目的**: 既存の`TableBase<T>`を置き換え、PrimaryKey管理を導入

**実装ファイル**:
- `Runtime/Core/XTableAsset.cs`
- `Runtime/Core/IXRecord.cs`
- `Runtime/Schema/Attributes/PrimaryKeyAttribute.cs`

**タスク**:
1. [ ] `IXRecord`インターフェース定義
2. [ ] `PrimaryKeyAttribute`実装
3. [ ] `XTableAsset<T>`基底クラス実装
4. [ ] PrimaryKeyによる自動ソート機能
5. [ ] PrimaryKey重複チェック（Editor）
6. [ ] 既存`TableBase<T>`からのマイグレーションパス

**API設計**:
```csharp
public abstract class XTableAsset<T> : ScriptableObject where T : IXRecord
{
    // 全レコード取得（ReadOnly）
    public ReadOnlySpan<T> All { get; }

    // PrimaryKeyで検索（O(log n)）
    public T FindByPrimaryKey<TKey>(TKey key);
    public bool TryFindByPrimaryKey<TKey>(TKey key, out T record);

    // レコード数
    public int Count { get; }
}
```

#### 3.1.2 基本Editor改善

**目的**: 最小限の編集UIを提供

**実装ファイル**:
- `Editor/TableEditor/XTableEditorWindow.cs`
- `Editor/TableEditor/RecordDrawer.cs`

**タスク**:
1. [ ] テーブル一覧表示
2. [ ] レコード追加/削除
3. [ ] PrimaryKey重複警告表示
4. [ ] 基本的なフィールド編集

---

### 3.2 Phase 2: インデックス・高速検索

#### 3.2.1 SecondaryKey インデックス

**目的**: SecondaryKeyによるO(1)検索を実現

**実装ファイル**:
- `Runtime/Schema/Attributes/SecondaryKeyAttribute.cs`
- `Runtime/Index/XIndexData.cs`
- `Runtime/Index/XIndexBuilder.cs`
- `Editor/Index/IndexRebuildProcessor.cs`

**タスク**:
1. [ ] `SecondaryKeyAttribute`実装
2. [ ] `XIndexData`シリアライズ可能クラス
3. [ ] `XIndexBuilder` - インデックス構築ロジック
4. [ ] Editor保存時の自動インデックス再構築
5. [ ] インデックス整合性チェック

**インデックス構造**:
```csharp
[Serializable]
public class XIndexData
{
    // SecondaryKey名 -> (キー値 -> レコードインデックス配列)
    [SerializeField]
    private SerializableDictionary<string, SerializableDictionary<string, int[]>> indexes;

    public int[] GetRecordIndexes(string keyName, object keyValue);
}
```

#### 3.2.2 Runtime 検索API

**目的**: GC Alloc最小の高速検索API

**実装ファイル**:
- `Runtime/Query/XTableQuery.cs`
- `Runtime/Query/XQueryResult.cs`

**タスク**:
1. [ ] SecondaryKey検索メソッド
2. [ ] 範囲検索（Range Query）
3. [ ] 複合条件検索
4. [ ] `Span<T>`ベースの結果返却

**API設計**:
```csharp
public static class XTableQuery
{
    // SecondaryKeyで検索
    public static XQueryResult<T> FindBySecondaryKey<T, TKey>(
        this XTableAsset<T> table,
        string keyName,
        TKey keyValue) where T : IXRecord;

    // 範囲検索
    public static XQueryResult<T> FindInRange<T, TKey>(
        this XTableAsset<T> table,
        string keyName,
        TKey min,
        TKey max) where T : IXRecord;
}

// GC Alloc 0 の結果構造体
public ref struct XQueryResult<T>
{
    private readonly ReadOnlySpan<T> source;
    private readonly ReadOnlySpan<int> indexes;

    public XQueryResultEnumerator<T> GetEnumerator();
}
```

---

### 3.3 Phase 3: CSV連携・Diff Viewer

#### 3.3.1 CSV Import/Export 改善

**目的**: 既存CSVパーサーを活用した堅牢なインポート/エクスポート

**実装ファイル**:
- `Editor/IO/XTableExporter.cs`
- `Editor/IO/XTableImporter.cs`
- `Editor/IO/ImportPreview.cs`

**タスク**:
1. [ ] 既存`CsvParser`との統合
2. [ ] エクスポート時のPrimaryKeyソート保証
3. [ ] インポートプレビュー機能
4. [ ] カラムマッピング設定
5. [ ] エンコーディング自動検出（既存`EncodeHelper`活用）

#### 3.3.2 Diff Viewer

**目的**: CSV取り込み前の差分確認UI

**実装ファイル**:
- `Editor/DiffViewer/XDiffViewerWindow.cs`
- `Editor/DiffViewer/DiffCalculator.cs`
- `Editor/DiffViewer/DiffEntry.cs`

**タスク**:
1. [ ] レコード差分計算ロジック
2. [ ] フィールド単位差分検出
3. [ ] 追加/削除/変更の色分け表示
4. [ ] 個別Apply/Ignore機能
5. [ ] 一括適用機能
6. [ ] 差分レポート出力

**UI設計**:
```
+------------------------------------------+
| Diff Viewer: ItemTable                   |
+------------------------------------------+
| Filter: [All ▼] [Added] [Deleted] [Modified] |
+------------------------------------------+
| □ | Type    | PrimaryKey | Field  | Before | After |
+---+---------+------------+--------+--------+-------+
| ☑ | Added   | 101        | -      | -      | -     |
| ☑ | Modified| 005        | Price  | 100    | 150   |
| ☑ | Deleted | 003        | -      | -      | -     |
+------------------------------------------+
| [Apply Selected] [Apply All] [Cancel]    |
+------------------------------------------+
```

---

### 3.4 Phase 4: SQL Editor・DB Browser

#### 3.4.1 SQL Query Editor

**目的**: SQL風構文での検索・編集（非エンジニア向け）

**実装ファイル**:
- `Editor/SqlEditor/XSqlEditorWindow.cs`
- `Editor/SqlEditor/SqlParser.cs`
- `Editor/SqlEditor/SqlExecutor.cs`
- `Editor/SqlEditor/SqlSyntaxHighlighter.cs`

**タスク**:
1. [ ] SQLサブセットパーサー実装
2. [ ] SELECT文の実行
3. [ ] WHERE句（=, <, >, IN, LIKE）
4. [ ] ORDER BY句
5. [ ] UPDATE文（Editor限定）
6. [ ] DELETE文（Editor限定）
7. [ ] シンタックスハイライト
8. [ ] オートコンプリート（テーブル名、カラム名）
9. [ ] クエリ履歴

**サポートSQL構文**:
```sql
-- 検索
SELECT * FROM ItemTable WHERE Category = 'Weapon'
SELECT Id, Name, Price FROM ItemTable WHERE Price > 100 ORDER BY Price DESC

-- 更新（Editor限定）
UPDATE ItemTable SET Price = 200 WHERE Id = 5

-- 削除（Editor限定）
DELETE FROM ItemTable WHERE Id = 10
```

#### 3.4.2 DB Browser

**目的**: 全テーブルの統合管理UI

**実装ファイル**:
- `Editor/Browser/XDatabaseBrowserWindow.cs`
- `Editor/Browser/TableListView.cs`
- `Editor/Browser/TableInfoPanel.cs`

**タスク**:
1. [ ] テーブル一覧表示
2. [ ] テーブル統計（レコード数、サイズ）
3. [ ] Key情報表示
4. [ ] インデックス状態表示
5. [ ] クイック検索
6. [ ] テーブル間リレーション表示

---

### 3.5 Phase 5: 製品化

#### 3.5.1 UI/UX改善

**タスク**:
1. [ ] 統一されたビジュアルデザイン
2. [ ] ダークモード対応
3. [ ] ショートカットキー
4. [ ] Undo/Redo完全対応
5. [ ] ドラッグ&ドロップ対応

#### 3.5.2 ドキュメント

**タスク**:
1. [ ] API リファレンス
2. [ ] チュートリアル
3. [ ] サンプルプロジェクト
4. [ ] FAQ

#### 3.5.3 Asset Store対応

**タスク**:
1. [ ] パッケージ構成最適化
2. [ ] デモシーン作成
3. [ ] Asset Store説明文・スクリーンショット
4. [ ] 利用規約・ライセンス整備

---

## 4. ファイル構成（完成形）

```
Packages/jp.xeon.x-scriptable-db/
├── package.json
├── README.md
├── CHANGELOG.md
├── LICENSE.md
│
├── Runtime/
│   ├── Xeon.XScriptableDB.asmdef
│   │
│   ├── Core/
│   │   ├── XTableAsset.cs
│   │   ├── IXRecord.cs
│   │   └── XDatabase.cs
│   │
│   ├── Schema/
│   │   ├── Attributes/
│   │   │   ├── PrimaryKeyAttribute.cs
│   │   │   ├── SecondaryKeyAttribute.cs
│   │   │   └── ValidationAttribute.cs
│   │   └── Validation/
│   │       ├── IValidator.cs
│   │       └── BuiltInValidators.cs
│   │
│   ├── Index/
│   │   ├── XIndexData.cs
│   │   └── XIndexBuilder.cs
│   │
│   ├── Query/
│   │   ├── XTableQuery.cs
│   │   └── XQueryResult.cs
│   │
│   ├── IO/                          # 既存
│   │   ├── CsvParser.cs
│   │   ├── CsvData.cs
│   │   ├── CsvColumn.cs
│   │   ├── CsvSupport.cs
│   │   ├── CsvUtility.cs
│   │   ├── ICsvSupport.cs
│   │   └── EncodeHelper.cs
│   │
│   └── Interfaces/                  # 既存
│       ├── IIdentifiable.cs
│       ├── IGroupIdentifiable.cs
│       ├── IImportable.cs
│       └── IExportable.cs
│
├── Editor/
│   ├── Xeon.XScriptableDB.Editor.asmdef
│   │
│   ├── Browser/
│   │   ├── XDatabaseBrowserWindow.cs
│   │   ├── TableListView.cs
│   │   └── TableInfoPanel.cs
│   │
│   ├── TableEditor/
│   │   ├── XTableEditorWindow.cs
│   │   ├── RecordDrawer.cs
│   │   └── RecordListView.cs
│   │
│   ├── DiffViewer/
│   │   ├── XDiffViewerWindow.cs
│   │   ├── DiffCalculator.cs
│   │   └── DiffEntry.cs
│   │
│   ├── SqlEditor/
│   │   ├── XSqlEditorWindow.cs
│   │   ├── SqlParser.cs
│   │   ├── SqlExecutor.cs
│   │   └── SqlSyntaxHighlighter.cs
│   │
│   ├── IO/
│   │   ├── XTableExporter.cs
│   │   ├── XTableImporter.cs
│   │   └── ImportPreview.cs
│   │
│   ├── Index/
│   │   └── IndexRebuildProcessor.cs
│   │
│   ├── Inspector/                   # 既存
│   │   ├── MasterReferenceInspector.cs
│   │   ├── GroupReferenceInspector.cs
│   │   └── AddressableObjectInspector.cs
│   │
│   └── Generator/                   # 既存
│       ├── ScriptGenerator.cs
│       └── TableGenerator.cs
│
├── Tests/
│   ├── Xeon.XScriptableDB.Tests.asmdef
│   ├── Runtime/
│   │   ├── XTableAssetTests.cs
│   │   ├── IndexTests.cs
│   │   └── QueryTests.cs
│   └── Editor/
│       ├── DiffCalculatorTests.cs
│       └── SqlParserTests.cs
│
└── Samples~/
    ├── BasicUsage/
    ├── AdvancedQueries/
    └── EditorIntegration/
```

---

## 5. 技術的考慮事項

### 5.1 パフォーマンス目標

| 操作 | 目標 | 備考 |
|-----|------|------|
| PrimaryKey検索 | O(log n) | 二分探索 |
| SecondaryKey検索 | O(1) + O(k) | ハッシュ + 結果走査 |
| 全件走査 | O(n) | Span<T>使用 |
| GC Alloc | 0 | Runtime検索時 |

### 5.2 互換性

- Unity 2021.3 LTS 以上
- .NET Standard 2.1
- IL2CPP 対応

### 5.3 既存コードとの互換性

- `TableBase<T>` → `XTableAsset<T>` マイグレーションガイド提供
- `CsvData` は引き続きサポート
- 既存の `IIdentifiable` 等は `IXRecord` と併用可能

---

## 6. リスクと対策

| リスク | 影響 | 対策 |
|-------|------|------|
| 大規模データでのEditor性能 | 編集UI遅延 | 仮想スクロール、遅延読み込み |
| SQLパーサーの複雑性 | 開発遅延 | サブセット限定、段階実装 |
| インデックス肥大化 | アセットサイズ増加 | 圧縮、必要時のみ生成 |
| 既存プロジェクトの移行 | 採用障壁 | マイグレーションツール提供 |

---

## 7. 成功指標

### 7.1 機能完成度
- [ ] Phase 1-4 全機能実装
- [ ] テストカバレッジ 80%以上
- [ ] ドキュメント完備

### 7.2 パフォーマンス
- [ ] 10万件レコードで検索 < 1ms
- [ ] Editor起動時間 < 3秒

### 7.3 ユーザビリティ
- [ ] 非エンジニアによるユーザーテスト合格
- [ ] Asset Store レビュー 4.5以上
