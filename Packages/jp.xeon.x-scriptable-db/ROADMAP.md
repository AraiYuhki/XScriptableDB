# XScriptableDB ロードマップ

このドキュメントでは、XScriptableDBの開発ロードマップを説明します。

## リリース済み

### v0.1.0 (2026-01-29)

#### Phase 1: Core機能
- [x] `TableAsset<TKey, TRecord>` - ScriptableObjectベースのテーブル
- [x] `[PrimaryKey]` 属性 - バイナリサーチによるO(log n)検索
- [x] `[CsvColumn]` 属性 - CSVカラム名マッピング
- [x] 基本的なCRUD操作

#### Phase 2: インデックス・クエリ
- [x] `[SecondaryKey]` 属性 - ハッシュインデックスによるO(1)検索
- [x] `QueryResult<T>` - GC Alloc 0のref structクエリ結果
- [x] 拡張メソッド（Where, FirstOrDefault, Any, All, Count, Select, Skip, Take）
- [x] 複数SecondaryKeyのサポート

#### Phase 3: CSV連携・Diff Viewer
- [x] CSV/TSVインポート・エクスポート
- [x] エンコーディング自動検出
- [x] Diff Viewer - 変更プレビューと選択的適用
- [x] Table Editor UI

#### Phase 4: SQL Editor・DB Browser
- [x] SQL Parser（SELECT, UPDATE, DELETE）
- [x] SQL Executor
- [x] SQL Editor Window（シンタックスハイライト、履歴機能）
- [x] Database Browser（テーブル一覧、スキーマ表示）

#### Phase 5: ドキュメント・サンプル
- [x] README.md
- [x] 詳細ドキュメント（TableAsset, QueryResult, CSV, SQL）
- [x] サンプルコード（BasicUsage）
- [x] CHANGELOG.md

### v0.2.0 (2026-01-29)

#### Phase 6: データ検証・バリデーション
- [x] `[Required]` 属性 - 必須フィールドの指定
- [x] `[Range(min, max)]` 属性 - 数値の範囲制限
- [x] `[StringLength(max)]` 属性 - 文字列長の制限
- [x] `[Unique]` 属性 - 一意性制約（PrimaryKey以外）
- [x] `[ForeignKey]` 属性 - 外部キー制約（他テーブル参照）
- [x] `[RegularExpression]` 属性 - 正規表現パターン検証
- [x] `[Compare]` 属性 - フィールド間の比較検証
- [x] カスタムバリデーター（`IRecordValidator<T>`）
- [x] `RecordValidator` - レコード・テーブル単位のバリデーション
- [x] `ForeignKeyValidator` - 外部キー参照の検証
- [x] Validation Window - エディタでのバリデーション実行・結果表示

---

### v0.3.0 (2026-01-29)

#### Phase 7: パフォーマンス最適化
- [x] クエリ結果キャッシュ
  - `LruCache<TKey, TValue>` - LRUキャッシュ（容量制限、自動エビクション）
  - `QueryCache` - クエリ結果専用キャッシュ（バージョンベース無効化）
  - `CacheManager` - グローバルキャッシュ管理
- [x] 遅延ロード
  - `LazyTableReference<T>` - Addressablesベースの遅延ロード
  - `TableLoader` - 複数テーブルの一括管理
  - 参照カウントによる自動アンロード
- [x] パフォーマンス計測
  - `QueryProfiler` - クエリ実行時間の計測
  - `MemoryProfiler` - メモリ使用量の推定
  - Performance Window - エディタでの可視化

#### 使用例
```csharp
// キャッシュ付きクエリ
var cache = CacheManager.QueryCache;
var result = cache.GetOrAdd<Item>(
    typeof(ItemTable), "FindById", 1,
    () => itemTable.FindById(1)
);

// 遅延ロード
var loader = new TableLoader();
loader.Register<ItemTable>("Tables/ItemTable");
var table = await loader.GetAsync<ItemTable>();

// プロファイリング
var profiler = new QueryProfiler();
var items = profiler.Profile("SearchItems", typeof(ItemTable),
    () => itemTable.All.ToList());
```

---

### v0.4.0 (2026-01-30)

#### Phase 8: 大量データ対応
- [x] 仮想スクロール（Table Editor）
  - `VirtualizedListView<T>` - 汎用仮想スクロールコンポーネント
  - `VirtualizedPropertyListView` - SerializedProperty対応版
  - TableEditorWindowへの統合（100件以上で自動有効化）
- [x] ストリーミングインポート
  - `StreamingImporter` - チャンク単位での大量データインポート
  - 進捗表示、キャンセル機能、エラー処理
  - `StreamingImportWindow` - 専用Editorウィンドウ
- [x] バッチ処理API（Editor専用）
  - `BatchProcessor<TRecord, TKey>` - バッチ操作のメインクラス
  - `AddRange()`, `UpdateRange()`, `DeleteRange()`, `UpsertRange()`
  - `DeleteWhere()`, `UpdateWhere()`, `ReplaceAll()`
  - 拡張メソッド `CreateBatchProcessor()` で簡単に作成
- [x] ベンチマークテストスイート
  - `BenchmarkTests` - 検索・ソート・CSV・メモリのベンチマーク
  - `BenchmarkWindow` - インタラクティブなベンチマーク実行

#### 使用例
```csharp
// バッチ処理
var processor = itemTable.CreateBatchProcessor(r => r.id);
var result = processor.UpsertRange(newRecords);
Debug.Log($"Added: {result.AddedCount}, Updated: {result.UpdatedCount}");

// 条件付き一括削除
processor.DeleteWhere(r => r.price < 100);

// 条件付き一括更新
processor.UpdateWhere(
    r => r.category == "Sale",
    r => r.price = (int)(r.price * 0.9f)
);
```

---

### v0.5.0 - 高度なSQL機能（予定）

より表現力豊かなクエリのためのSQL機能拡張。

#### 機能
- [ ] JOIN
  - INNER JOIN
  - LEFT JOIN
  - RIGHT JOIN
  - CROSS JOIN
- [ ] 集計関数
  - COUNT()
  - SUM()
  - AVG()
  - MIN() / MAX()
- [ ] GROUP BY / HAVING
- [ ] サブクエリ
- [ ] DISTINCT
- [ ] UNION / INTERSECT / EXCEPT
- [ ] CASE式
- [ ] 関数（UPPER, LOWER, CONCAT, SUBSTRING等）

#### 使用例
```sql
-- JOIN
SELECT i.Name, c.CategoryName
FROM ItemTable i
INNER JOIN CategoryTable c ON i.CategoryId = c.Id

-- 集計
SELECT Category, COUNT(*) as Count, AVG(Price) as AvgPrice
FROM ItemTable
GROUP BY Category
HAVING COUNT(*) > 5

-- サブクエリ
SELECT * FROM ItemTable
WHERE Price > (SELECT AVG(Price) FROM ItemTable)
```

---

### v0.6.0 - 追加ツール（予定）

開発効率を向上させる追加ツール。

#### 機能
- [ ] スキーマ比較ツール
  - テーブル間の構造差分表示
  - マイグレーションスクリプト生成
- [ ] データマイグレーション
  - バージョン管理
  - 自動マイグレーション
  - ロールバック機能
- [ ] バックアップ・リストア
  - スナップショット作成
  - 差分バックアップ
  - 自動バックアップスケジュール
- [ ] コマンドラインインターフェース
  - CI/CD連携
  - バッチインポート/エクスポート
  - スキーマ検証
- [ ] データ生成ツール
  - テストデータ自動生成
  - シード値による再現可能な生成

#### 使用例
```bash
# CLI
xscriptabledb export --table ItemTable --output items.csv
xscriptabledb import --file items.csv --table ItemTable --validate
xscriptabledb schema compare --source ItemTable --target ItemTable_v2
xscriptabledb migrate --from v1 --to v2
```

---

### v1.0.0 - 安定版リリース

#### 目標
- [ ] 全機能の安定化
- [ ] 包括的なテストカバレッジ（90%以上）
- [ ] パフォーマンスベンチマーク公開
- [ ] 完全なAPIドキュメント
- [ ] 複数のサンプルプロジェクト
- [ ] コミュニティフィードバックの反映

---

## 検討中（優先度未定）

以下の機能は要望に応じて検討します：

- [ ] **リアルタイム同期** - 複数エディタ間でのデータ同期
- [ ] **暗号化** - センシティブデータの暗号化保存
- [ ] **圧縮** - 大規模データの圧縮保存
- [ ] **ネットワーク対応** - リモートデータベースとの連携
- [ ] **ビジュアルクエリビルダー** - GUIベースのクエリ作成
- [ ] **データビジュアライゼーション** - グラフ・チャート表示
- [ ] **Excelアドイン** - Excelからの直接編集
- [ ] **他フォーマット対応** - JSON, XML, SQLite連携

---

## フィードバック

機能リクエストやバグ報告は、GitHubのIssueでお願いします：
https://github.com/AraiYuhki/XScriptableDB/issues

ロードマップの優先順位は、コミュニティからのフィードバックに基づいて調整されます。
