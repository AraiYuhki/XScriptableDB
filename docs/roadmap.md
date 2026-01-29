# XScriptableDB 実装ロードマップ

## 概要

本ドキュメントは XScriptableDB の実装ロードマップを定義します。
各フェーズの目標、タスク、依存関係、完了条件を明確にします。

---

## タイムライン概要

```
Phase 1 (MVP)          Phase 2             Phase 3            Phase 4           Phase 5
基盤整備               インデックス        CSV/Diff           SQL/Browser        製品化
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
[████████████]        [████████████]      [████████████]     [████████████]    [████████████]
                              ↑                  ↑                  ↑
                              │                  │                  │
                        Phase 1完了後      Phase 2完了後      Phase 3完了後
```

---

## 現在の実装状況

### 実装済み (✅)

| コンポーネント | 状態 | 備考 |
|---------------|------|------|
| CSV/TSV パーサー | ✅ | `Xeon.XScriptableDB.IO` |
| 基本インターフェース | ✅ | IIdentifiable, IImportable, IExportable |
| TableBase<T> | ✅ | 基本的なテーブル機能 |
| DB シングルトン | ✅ | 基本的なアクセス機能 |
| DatabaseEditor | ✅ | 基本的な編集UI |
| Inspector 拡張 | ✅ | Reference系Inspector |
| Generator | ✅ | Script/Table Generator |
| 名前空間統一 | ✅ | `Xeon.XScriptableDB.*` |

### 未実装 (❌)

| コンポーネント | Phase | 優先度 |
|---------------|-------|--------|
| XTableAsset<T> | 1 | 高 |
| PrimaryKey管理 | 1 | 高 |
| SecondaryKeyインデックス | 2 | 高 |
| 高速検索API | 2 | 高 |
| Diff Viewer | 3 | 中 |
| CSV Import改善 | 3 | 中 |
| SQL Editor | 4 | 中 |
| DB Browser | 4 | 中 |

---

## Phase 1: MVP（基盤整備）

### 目標
- 新しいテーブルアーキテクチャの確立
- PrimaryKey による自動管理
- 基本的な編集UI

### タスク一覧

#### 1.1 Core 実装
- [ ] **1.1.1** `IXRecord` インターフェース定義
  - 優先度: 高
  - 工数目安: 0.5日
  - ファイル: `Runtime/Core/IXRecord.cs`

- [ ] **1.1.2** `PrimaryKeyAttribute` 実装
  - 優先度: 高
  - 工数目安: 0.5日
  - 依存: 1.1.1
  - ファイル: `Runtime/Schema/Attributes/PrimaryKeyAttribute.cs`

- [ ] **1.1.3** `XTableAsset<T>` 基底クラス実装
  - 優先度: 高
  - 工数目安: 2日
  - 依存: 1.1.1, 1.1.2
  - ファイル: `Runtime/Core/XTableAsset.cs`
  - 機能:
    - レコード配列の管理
    - PrimaryKeyソート
    - 二分探索による検索
    - ReadOnlyアクセス

- [ ] **1.1.4** `XDatabase` マネージャー実装
  - 優先度: 高
  - 工数目安: 1日
  - 依存: 1.1.3
  - ファイル: `Runtime/Core/XDatabase.cs`
  - 機能:
    - テーブル自動登録
    - 型安全なアクセスAPI

#### 1.2 Editor 基盤
- [ ] **1.2.1** `XTableEditorWindow` 基本実装
  - 優先度: 高
  - 工数目安: 2日
  - 依存: 1.1.3
  - ファイル: `Editor/TableEditor/XTableEditorWindow.cs`
  - 機能:
    - テーブル選択
    - レコード一覧表示
    - レコード追加/削除

- [ ] **1.2.2** PrimaryKey重複チェック
  - 優先度: 高
  - 工数目安: 0.5日
  - 依存: 1.2.1
  - 機能:
    - 保存時の重複検出
    - 警告表示

- [ ] **1.2.3** RecordDrawer 実装
  - 優先度: 中
  - 工数目安: 1日
  - 依存: 1.2.1
  - ファイル: `Editor/TableEditor/RecordDrawer.cs`

#### 1.3 マイグレーション
- [ ] **1.3.1** TableBase → XTableAsset マイグレーションガイド
  - 優先度: 低
  - 工数目安: 0.5日
  - ファイル: `docs/migration_guide.md`

### 完了条件
- [ ] XTableAsset<T> でテーブルを定義できる
- [ ] PrimaryKeyで自動ソートされる
- [ ] PrimaryKey検索がO(log n)で動作する
- [ ] Editorでレコードの追加/削除/編集ができる
- [ ] ユニットテストが通る

### 成果物
- `Runtime/Core/` 配下の新規ファイル
- `Runtime/Schema/Attributes/` 配下の新規ファイル
- `Editor/TableEditor/` 配下の新規ファイル

---

## Phase 2: インデックス・高速検索

### 目標
- SecondaryKeyによるインデックス構築
- O(1)検索の実現
- GC Alloc 0 の検索API

### 前提条件
- Phase 1 完了

### タスク一覧

#### 2.1 インデックス実装
- [ ] **2.1.1** `SecondaryKeyAttribute` 実装
  - 優先度: 高
  - 工数目安: 0.5日
  - ファイル: `Runtime/Schema/Attributes/SecondaryKeyAttribute.cs`

- [ ] **2.1.2** `XIndexData` シリアライズ構造
  - 優先度: 高
  - 工数目安: 1日
  - 依存: 2.1.1
  - ファイル: `Runtime/Index/XIndexData.cs`
  - 機能:
    - SecondaryKey → レコードインデックス配列
    - シリアライズ対応

- [ ] **2.1.3** `XIndexBuilder` 実装
  - 優先度: 高
  - 工数目安: 1.5日
  - 依存: 2.1.2
  - ファイル: `Runtime/Index/XIndexBuilder.cs`
  - 機能:
    - リフレクションによるKey抽出
    - インデックス構築
    - 増分更新

- [ ] **2.1.4** Editor保存時インデックス再構築
  - 優先度: 高
  - 工数目安: 1日
  - 依存: 2.1.3
  - ファイル: `Editor/Index/IndexRebuildProcessor.cs`

#### 2.2 検索API
- [ ] **2.2.1** `XQueryResult<T>` 構造体
  - 優先度: 高
  - 工数目安: 1日
  - ファイル: `Runtime/Query/XQueryResult.cs`
  - 機能:
    - ref struct による GC Alloc 0
    - Enumerator実装

- [ ] **2.2.2** `XTableQuery` 拡張メソッド
  - 優先度: 高
  - 工数目安: 1.5日
  - 依存: 2.1.2, 2.2.1
  - ファイル: `Runtime/Query/XTableQuery.cs`
  - 機能:
    - FindBySecondaryKey
    - FindInRange
    - FindAll (条件指定)

#### 2.3 テスト
- [ ] **2.3.1** インデックステスト
  - 優先度: 中
  - 工数目安: 1日
  - ファイル: `Tests/Runtime/IndexTests.cs`

- [ ] **2.3.2** 検索パフォーマンステスト
  - 優先度: 中
  - 工数目安: 0.5日
  - ファイル: `Tests/Runtime/QueryPerformanceTests.cs`

### 完了条件
- [ ] SecondaryKeyでインデックスが自動生成される
- [ ] SecondaryKey検索がO(1)で動作する
- [ ] 10万件で検索が1ms以内
- [ ] GC Alloc が 0

### 成果物
- `Runtime/Index/` 配下の新規ファイル
- `Runtime/Query/` 配下の新規ファイル
- `Editor/Index/` 配下の新規ファイル

---

## Phase 3: CSV連携・Diff Viewer

### 目標
- CSVインポート/エクスポートの改善
- 差分確認・適用UI

### 前提条件
- Phase 2 完了

### タスク一覧

#### 3.1 CSV連携改善
- [ ] **3.1.1** `XTableExporter` 実装
  - 優先度: 高
  - 工数目安: 1日
  - ファイル: `Editor/IO/XTableExporter.cs`
  - 機能:
    - PrimaryKeyソート保証
    - カラム順序設定
    - エンコーディング選択

- [ ] **3.1.2** `XTableImporter` 実装
  - 優先度: 高
  - 工数目安: 1.5日
  - ファイル: `Editor/IO/XTableImporter.cs`
  - 機能:
    - カラムマッピング
    - バリデーション
    - エラーハンドリング

- [ ] **3.1.3** `ImportPreview` UI
  - 優先度: 中
  - 工数目安: 1日
  - 依存: 3.1.2
  - ファイル: `Editor/IO/ImportPreview.cs`

#### 3.2 Diff Viewer
- [ ] **3.2.1** `DiffCalculator` 実装
  - 優先度: 高
  - 工数目安: 1.5日
  - ファイル: `Editor/DiffViewer/DiffCalculator.cs`
  - 機能:
    - レコード単位差分
    - フィールド単位差分
    - 追加/削除/変更分類

- [ ] **3.2.2** `DiffEntry` データ構造
  - 優先度: 高
  - 工数目安: 0.5日
  - ファイル: `Editor/DiffViewer/DiffEntry.cs`

- [ ] **3.2.3** `XDiffViewerWindow` UI
  - 優先度: 高
  - 工数目安: 2日
  - 依存: 3.2.1, 3.2.2
  - ファイル: `Editor/DiffViewer/XDiffViewerWindow.cs`
  - 機能:
    - 差分一覧表示
    - フィルタリング
    - 個別/一括適用

#### 3.3 テスト
- [ ] **3.3.1** DiffCalculatorテスト
  - 優先度: 中
  - 工数目安: 1日
  - ファイル: `Tests/Editor/DiffCalculatorTests.cs`

### 完了条件
- [ ] CSVエクスポートがPrimaryKey順で出力される
- [ ] CSVインポート前にプレビューが表示される
- [ ] Diff Viewerで差分が確認できる
- [ ] 個別/一括で差分を適用できる

### 成果物
- `Editor/IO/` 配下の新規ファイル
- `Editor/DiffViewer/` 配下の新規ファイル

---

## Phase 4: SQL Editor・DB Browser

### 目標
- SQL風クエリエディタ
- 統合データベースブラウザ

### 前提条件
- Phase 3 完了

### タスク一覧

#### 4.1 SQL Editor
- [ ] **4.1.1** `SqlParser` 実装
  - 優先度: 高
  - 工数目安: 3日
  - ファイル: `Editor/SqlEditor/SqlParser.cs`
  - 機能:
    - SELECT文パース
    - WHERE句（=, <, >, IN, LIKE）
    - ORDER BY句
    - UPDATE/DELETE（Editor限定）

- [ ] **4.1.2** `SqlExecutor` 実装
  - 優先度: 高
  - 工数目安: 2日
  - 依存: 4.1.1
  - ファイル: `Editor/SqlEditor/SqlExecutor.cs`
  - 機能:
    - AST → 検索API変換
    - 結果セット生成

- [ ] **4.1.3** `SqlSyntaxHighlighter` 実装
  - 優先度: 中
  - 工数目安: 1日
  - ファイル: `Editor/SqlEditor/SqlSyntaxHighlighter.cs`

- [ ] **4.1.4** `XSqlEditorWindow` UI
  - 優先度: 高
  - 工数目安: 2日
  - 依存: 4.1.1, 4.1.2, 4.1.3
  - ファイル: `Editor/SqlEditor/XSqlEditorWindow.cs`
  - 機能:
    - クエリ入力
    - 結果表示
    - クエリ履歴
    - オートコンプリート

#### 4.2 DB Browser
- [ ] **4.2.1** `TableListView` 実装
  - 優先度: 高
  - 工数目安: 1日
  - ファイル: `Editor/Browser/TableListView.cs`

- [ ] **4.2.2** `TableInfoPanel` 実装
  - 優先度: 中
  - 工数目安: 1日
  - ファイル: `Editor/Browser/TableInfoPanel.cs`
  - 機能:
    - レコード数
    - Key情報
    - インデックス状態

- [ ] **4.2.3** `XDatabaseBrowserWindow` UI
  - 優先度: 高
  - 工数目安: 2日
  - 依存: 4.2.1, 4.2.2
  - ファイル: `Editor/Browser/XDatabaseBrowserWindow.cs`
  - 機能:
    - テーブル一覧
    - 詳細パネル
    - クイック検索
    - 各エディタへのナビゲーション

#### 4.3 テスト
- [ ] **4.3.1** SqlParserテスト
  - 優先度: 高
  - 工数目安: 1.5日
  - ファイル: `Tests/Editor/SqlParserTests.cs`

### 完了条件
- [ ] SQL文で検索ができる
- [ ] シンタックスハイライトが機能する
- [ ] DB Browserで全テーブルが一覧できる
- [ ] 各エディタへスムーズに遷移できる

### 成果物
- `Editor/SqlEditor/` 配下の新規ファイル
- `Editor/Browser/` 配下の新規ファイル

---

## Phase 5: 製品化

### 目標
- UI/UX の洗練
- ドキュメント完備
- Asset Store 公開準備

### 前提条件
- Phase 4 完了

### タスク一覧

#### 5.1 UI/UX改善
- [ ] **5.1.1** 統一デザインシステム適用
- [ ] **5.1.2** ダークモード対応
- [ ] **5.1.3** ショートカットキー設定
- [ ] **5.1.4** Undo/Redo完全対応
- [ ] **5.1.5** ドラッグ&ドロップ対応
- [ ] **5.1.6** ツールチップ追加

#### 5.2 ドキュメント
- [ ] **5.2.1** API リファレンス生成
- [ ] **5.2.2** Getting Started ガイド
- [ ] **5.2.3** チュートリアル（5本程度）
- [ ] **5.2.4** FAQ
- [ ] **5.2.5** トラブルシューティング

#### 5.3 サンプル
- [ ] **5.3.1** BasicUsage サンプル
- [ ] **5.3.2** AdvancedQueries サンプル
- [ ] **5.3.3** EditorIntegration サンプル

#### 5.4 Asset Store準備
- [ ] **5.4.1** パッケージ構成最終確認
- [ ] **5.4.2** デモシーン作成
- [ ] **5.4.3** スクリーンショット/動画作成
- [ ] **5.4.4** Asset Store説明文作成
- [ ] **5.4.5** 利用規約/ライセンス最終確認

### 完了条件
- [ ] すべての機能にツールチップがある
- [ ] ドキュメントが完備している
- [ ] サンプルプロジェクトが動作する
- [ ] Asset Store 審査に提出できる状態

---

## 依存関係図

```
Phase 1 (MVP)
    │
    ├── 1.1 Core実装
    │   ├── 1.1.1 IXRecord
    │   ├── 1.1.2 PrimaryKeyAttribute ──────────┐
    │   ├── 1.1.3 XTableAsset<T> ←─────────────┤
    │   └── 1.1.4 XDatabase ←──────────────────┘
    │
    └── 1.2 Editor基盤
        ├── 1.2.1 XTableEditorWindow ←── 1.1.3
        ├── 1.2.2 PrimaryKey重複チェック ←── 1.2.1
        └── 1.2.3 RecordDrawer ←── 1.2.1
                    │
                    ▼
Phase 2 (インデックス)
    │
    ├── 2.1 インデックス実装
    │   ├── 2.1.1 SecondaryKeyAttribute
    │   ├── 2.1.2 XIndexData ←── 2.1.1
    │   ├── 2.1.3 XIndexBuilder ←── 2.1.2
    │   └── 2.1.4 IndexRebuildProcessor ←── 2.1.3
    │
    └── 2.2 検索API
        ├── 2.2.1 XQueryResult<T>
        └── 2.2.2 XTableQuery ←── 2.1.2, 2.2.1
                    │
                    ▼
Phase 3 (CSV/Diff)
    │
    ├── 3.1 CSV連携
    │   ├── 3.1.1 XTableExporter
    │   ├── 3.1.2 XTableImporter
    │   └── 3.1.3 ImportPreview ←── 3.1.2
    │
    └── 3.2 Diff Viewer
        ├── 3.2.1 DiffCalculator
        ├── 3.2.2 DiffEntry
        └── 3.2.3 XDiffViewerWindow ←── 3.2.1, 3.2.2
                    │
                    ▼
Phase 4 (SQL/Browser)
    │
    ├── 4.1 SQL Editor
    │   ├── 4.1.1 SqlParser
    │   ├── 4.1.2 SqlExecutor ←── 4.1.1
    │   ├── 4.1.3 SqlSyntaxHighlighter
    │   └── 4.1.4 XSqlEditorWindow ←── 4.1.1, 4.1.2, 4.1.3
    │
    └── 4.2 DB Browser
        ├── 4.2.1 TableListView
        ├── 4.2.2 TableInfoPanel
        └── 4.2.3 XDatabaseBrowserWindow ←── 4.2.1, 4.2.2
                    │
                    ▼
Phase 5 (製品化)
    ├── 5.1 UI/UX改善
    ├── 5.2 ドキュメント
    ├── 5.3 サンプル
    └── 5.4 Asset Store準備
```

---

## チェックリスト（進捗管理用）

### Phase 1
- [ ] 1.1.1 IXRecord
- [ ] 1.1.2 PrimaryKeyAttribute
- [ ] 1.1.3 XTableAsset<T>
- [ ] 1.1.4 XDatabase
- [ ] 1.2.1 XTableEditorWindow
- [ ] 1.2.2 PrimaryKey重複チェック
- [ ] 1.2.3 RecordDrawer
- [ ] 1.3.1 マイグレーションガイド

### Phase 2
- [ ] 2.1.1 SecondaryKeyAttribute
- [ ] 2.1.2 XIndexData
- [ ] 2.1.3 XIndexBuilder
- [ ] 2.1.4 IndexRebuildProcessor
- [ ] 2.2.1 XQueryResult<T>
- [ ] 2.2.2 XTableQuery
- [ ] 2.3.1 インデックステスト
- [ ] 2.3.2 パフォーマンステスト

### Phase 3
- [ ] 3.1.1 XTableExporter
- [ ] 3.1.2 XTableImporter
- [ ] 3.1.3 ImportPreview
- [ ] 3.2.1 DiffCalculator
- [ ] 3.2.2 DiffEntry
- [ ] 3.2.3 XDiffViewerWindow
- [ ] 3.3.1 DiffCalculatorテスト

### Phase 4
- [ ] 4.1.1 SqlParser
- [ ] 4.1.2 SqlExecutor
- [ ] 4.1.3 SqlSyntaxHighlighter
- [ ] 4.1.4 XSqlEditorWindow
- [ ] 4.2.1 TableListView
- [ ] 4.2.2 TableInfoPanel
- [ ] 4.2.3 XDatabaseBrowserWindow
- [ ] 4.3.1 SqlParserテスト

### Phase 5
- [ ] 5.1 UI/UX改善（6項目）
- [ ] 5.2 ドキュメント（5項目）
- [ ] 5.3 サンプル（3項目）
- [ ] 5.4 Asset Store準備（5項目）
