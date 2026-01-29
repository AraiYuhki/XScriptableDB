# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.3.0] - 2026-01-29

### Added

#### Cache System
- `LruCache<TKey, TValue>` - LRU（Least Recently Used）キャッシュ
  - 容量制限、自動エビクション
  - ヒット率・統計情報の追跡
- `QueryCache` - クエリ結果専用キャッシュ
  - テーブル型・クエリ種別・キー値によるキャッシング
  - バージョンベースの自動無効化
- `CacheManager` - グローバルキャッシュ管理
  - キャッシュの有効/無効切り替え
  - テーブル単位の無効化

#### Lazy Loading
- `LazyTableReference<T>` - Addressablesベースの遅延ロード参照
  - 同期・非同期ロード
  - 参照カウントによる自動解放
- `TableLoader` - 複数テーブルの一括管理
  - 登録ベースのロード
  - 並列非同期ロード
  - 参照カウントによるライフサイクル管理

#### Performance Profiling
- `QueryProfiler` - クエリ実行時間の計測
  - 実行時間、結果数、タイムスタンプの記録
  - 遅いクエリの検出
  - キャッシュヒットの追跡
- `MemoryProfiler` - メモリ使用量の推定
  - 型サイズの推定
  - テーブル単位のメモリ情報
  - GCスナップショット

#### Editor Tools
- **Performance Window** (`Window > XScriptableDB > Performance`)
  - キャッシュ統計表示
  - メモリ使用量の可視化
  - クエリ履歴表示
  - 手動GC実行

---

## [0.2.0] - 2026-01-29

### Added

#### Validation System
- `[Required]` 属性 - 必須フィールドの指定
- `[Range(min, max)]` 属性 - 数値の範囲制限
- `[StringLength(max)]` 属性 - 文字列長の制限（MinimumLengthオプション付き）
- `[RegularExpression(pattern)]` 属性 - 正規表現パターン検証
- `[Unique]` 属性 - 一意性制約
- `[ForeignKey(type)]` 属性 - 外部キー制約
- `[Compare(field, operator)]` 属性 - フィールド間の比較検証
- `RecordValidator` - レコード単位・テーブル単位のバリデーション
- `ForeignKeyValidator` - 外部キー参照の検証
- `ValidationResult`, `TableValidationResult` - 検証結果クラス

#### Editor Tools
- **Validation Window** (`Window > XScriptableDB > Validation`)
  - テーブル一覧とバリデーション状態表示
  - 一括バリデーション
  - エラー詳細表示

---

## [0.1.0] - 2026-01-29

### Added

#### Core Features
- `TableAsset<TKey, TRecord>` - ScriptableObjectベースのテーブルクラス
- `[PrimaryKey]` 属性 - 主キーの指定（バイナリサーチによるO(log n)検索）
- `[SecondaryKey]` 属性 - 副キーの指定（ハッシュインデックスによるO(1)検索）
- `[CsvColumn]` 属性 - CSV/TSVカラム名のマッピング

#### Query System
- `QueryResult<T>` - GC Alloc 0のref structクエリ結果
- `Where()`, `FirstOrDefault()`, `Any()`, `All()`, `Count()`, `Select()`, `Skip()`, `Take()` 拡張メソッド
- SecondaryKeyによる検索メソッド (`FindBySecondaryKey`, `FindAllBySecondaryKey`)

#### CSV/TSV Support
- CSVインポート・エクスポート機能
- TSVサポート
- エンコーディング自動検出（UTF-8, Shift-JIS, etc.）
- BOM対応

#### Editor Tools
- **Table Editor** (`Window > XScriptableDB > Table Editor`)
  - テーブルデータの編集
  - CSV/TSVインポート・エクスポート
  - 一括操作

- **SQL Editor** (`Window > XScriptableDB > SQL Editor`)
  - SELECT/UPDATE/DELETE文のサポート
  - シンタックスハイライト
  - クエリ履歴
  - F5/Ctrl+Enterで実行

- **Database Browser** (`Window > XScriptableDB > Database Browser`)
  - プロジェクト内のテーブル一覧
  - スキーマ情報表示
  - データプレビュー

- **Diff Viewer**
  - インポート前の変更プレビュー
  - 追加/削除/変更のカラー表示
  - 選択的な変更適用

#### SQL Support
- SELECT: カラム指定, WHERE, ORDER BY, LIMIT, OFFSET
- UPDATE: SET, WHERE
- DELETE: WHERE
- 比較演算子: =, !=, <>, <, <=, >, >=, LIKE, IN, IS NULL, IS NOT NULL
- 論理演算子: AND, OR
- 括弧によるグループ化

### Dependencies
- Unity 6000.0+
- com.unity.addressables 2.3.7
