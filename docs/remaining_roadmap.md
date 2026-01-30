# XScriptableDB 残りロードマップ詳細実装計画

## 概要

本ドキュメントはPhase 8（v0.4.0）完了後の残りの実装計画を詳細に記述します。

### 完了済みフェーズ

| Version | Phase | 内容 | 状態 |
|---------|-------|------|------|
| v0.1.0 | Phase 1-5 | Core, Index, CSV, SQL, Docs | ✅ 完了 |
| v0.2.0 | Phase 6 | Validation | ✅ 完了 |
| v0.3.0 | Phase 7 | Performance (Cache, LazyLoad, Profiler) | ✅ 完了 |
| v0.4.0 | Phase 8 | 大量データ対応 (VirtualScroll, Streaming, Batch) | ✅ 完了 |

---

## Phase 9: v0.5.0 - 高度なSQL機能

### 目標
SQLの表現力を拡張し、より複雑なクエリをサポートする。

### 9.1 JOIN機能

#### 9.1.1 INNER JOIN
- **優先度**: 高
- **工数目安**: 2日
- **ファイル**: `Editor/SqlEditor/SqlJoinExecutor.cs`
- **実装内容**:
  - JOINキーワードのパース
  - ON条件の評価
  - 結合結果の生成

```sql
SELECT i.Name, c.CategoryName
FROM ItemTable i
INNER JOIN CategoryTable c ON i.CategoryId = c.Id
```

#### 9.1.2 LEFT/RIGHT JOIN
- **優先度**: 中
- **工数目安**: 1日
- **依存**: 9.1.1
- **実装内容**:
  - NULL値の処理
  - 片側マッチの結果生成

#### 9.1.3 CROSS JOIN
- **優先度**: 低
- **工数目安**: 0.5日
- **依存**: 9.1.1
- **実装内容**:
  - デカルト積の生成

### 9.2 集計関数

#### 9.2.1 基本集計関数
- **優先度**: 高
- **工数目安**: 2日
- **ファイル**: `Editor/SqlEditor/SqlAggregateFunctions.cs`
- **関数一覧**:
  - `COUNT()` - レコード数
  - `SUM()` - 合計
  - `AVG()` - 平均
  - `MIN()` / `MAX()` - 最小/最大

```sql
SELECT COUNT(*), SUM(Price), AVG(Price) FROM ItemTable
```

#### 9.2.2 GROUP BY / HAVING
- **優先度**: 高
- **工数目安**: 2日
- **依存**: 9.2.1
- **ファイル**: `Editor/SqlEditor/SqlGroupExecutor.cs`
- **実装内容**:
  - グループ化ロジック
  - HAVING条件フィルタ

```sql
SELECT Category, COUNT(*) as Count
FROM ItemTable
GROUP BY Category
HAVING COUNT(*) > 5
```

### 9.3 サブクエリ

#### 9.3.1 WHERE句のサブクエリ
- **優先度**: 中
- **工数目安**: 2日
- **ファイル**: `Editor/SqlEditor/SqlSubqueryExecutor.cs`
- **実装内容**:
  - ネストされたSELECT文のパース
  - サブクエリ結果の評価

```sql
SELECT * FROM ItemTable
WHERE Price > (SELECT AVG(Price) FROM ItemTable)
```

#### 9.3.2 FROM句のサブクエリ
- **優先度**: 低
- **工数目安**: 1日
- **依存**: 9.3.1

### 9.4 その他のSQL機能

#### 9.4.1 DISTINCT
- **優先度**: 中
- **工数目安**: 0.5日
- **実装内容**: 重複除去

#### 9.4.2 UNION / INTERSECT / EXCEPT
- **優先度**: 低
- **工数目安**: 1日
- **実装内容**: 集合演算

#### 9.4.3 CASE式
- **優先度**: 中
- **工数目安**: 1日
- **実装内容**: 条件分岐式

```sql
SELECT Name,
  CASE WHEN Price > 1000 THEN 'High' ELSE 'Low' END as PriceLevel
FROM ItemTable
```

#### 9.4.4 文字列関数
- **優先度**: 低
- **工数目安**: 1日
- **関数一覧**: UPPER, LOWER, CONCAT, SUBSTRING, TRIM, LENGTH

### 9.5 テスト

#### 9.5.1 JOIN テスト
- **ファイル**: `Tests/SqlEditor/SqlJoinTests.cs`

#### 9.5.2 集計関数テスト
- **ファイル**: `Tests/SqlEditor/SqlAggregateTests.cs`

### 完了条件
- [ ] INNER JOINが動作する
- [ ] COUNT, SUM, AVG, MIN, MAXが動作する
- [ ] GROUP BY / HAVINGが動作する
- [ ] サブクエリが動作する
- [ ] 全テストがパスする

---

## Phase 10: v0.6.0 - 追加ツール

### 目標
開発効率を向上させる追加ツールを提供する。

### 10.1 スキーマ比較ツール

#### 10.1.1 スキーマ差分検出
- **優先度**: 高
- **工数目安**: 2日
- **ファイル**: `Editor/Schema/SchemaComparer.cs`
- **実装内容**:
  - フィールド追加/削除/変更の検出
  - 型変更の検出
  - 属性変更の検出

#### 10.1.2 スキーマ比較Window
- **優先度**: 高
- **工数目安**: 1日
- **ファイル**: `Editor/Schema/SchemaCompareWindow.cs`
- **機能**:
  - 2つのテーブル/型の選択
  - 差分の視覚的表示
  - マイグレーションコード生成

### 10.2 データマイグレーション

#### 10.2.1 マイグレーション定義
- **優先度**: 中
- **工数目安**: 2日
- **ファイル**:
  - `Editor/Migration/MigrationDefinition.cs`
  - `Editor/Migration/MigrationRunner.cs`
- **実装内容**:
  - バージョン番号管理
  - Up/Down処理
  - ロールバック機能

#### 10.2.2 自動マイグレーション生成
- **優先度**: 中
- **工数目安**: 2日
- **依存**: 10.1.1, 10.2.1
- **実装内容**:
  - スキーマ差分からマイグレーションコード自動生成

### 10.3 バックアップ・リストア

#### 10.3.1 スナップショット機能
- **優先度**: 中
- **工数目安**: 1日
- **ファイル**: `Editor/Backup/SnapshotManager.cs`
- **実装内容**:
  - テーブル状態のスナップショット作成
  - スナップショットからの復元

#### 10.3.2 差分バックアップ
- **優先度**: 低
- **工数目安**: 1日
- **依存**: 10.3.1
- **実装内容**:
  - 変更分のみをバックアップ
  - 増分復元

#### 10.3.3 自動バックアップ
- **優先度**: 低
- **工数目安**: 1日
- **依存**: 10.3.1
- **実装内容**:
  - 保存時の自動バックアップ
  - バックアップ世代管理

### 10.4 コマンドラインインターフェース

#### 10.4.1 CLIコア
- **優先度**: 中
- **工数目安**: 2日
- **ファイル**: `Editor/CLI/XScriptableDbCli.cs`
- **コマンド**:
  - `export` - CSVエクスポート
  - `import` - CSVインポート
  - `validate` - バリデーション実行
  - `schema` - スキーマ操作

```bash
# 使用例
Unity -executeMethod XScriptableDbCli.Export --table ItemTable --output items.csv
Unity -executeMethod XScriptableDbCli.Import --file items.csv --table ItemTable
Unity -executeMethod XScriptableDbCli.Validate --table ItemTable
```

#### 10.4.2 CI/CD連携
- **優先度**: 低
- **工数目安**: 1日
- **依存**: 10.4.1
- **実装内容**:
  - 終了コードの適切な設定
  - JSON形式の出力オプション
  - GitHub Actions サンプル

### 10.5 データ生成ツール

#### 10.5.1 テストデータ生成
- **優先度**: 低
- **工数目安**: 2日
- **ファイル**: `Editor/DataGenerator/TestDataGenerator.cs`
- **実装内容**:
  - ランダムデータ生成
  - シード値による再現性
  - カスタム生成ルール

```csharp
// 使用例
var generator = new TestDataGenerator<ItemRecord>(seed: 42);
generator.SetRule(r => r.Name, () => Faker.Name());
generator.SetRule(r => r.Price, () => Random.Range(100, 10000));
var testData = generator.Generate(1000);
```

### 完了条件
- [ ] スキーマ比較ツールが動作する
- [ ] マイグレーション機能が動作する
- [ ] バックアップ/リストアが動作する
- [ ] CLIが動作する
- [ ] データ生成ツールが動作する

---

## Phase 11: v1.0.0 - 安定版リリース

### 目標
製品品質の安定版をリリースする。

### 11.1 品質向上

#### 11.1.1 テストカバレッジ向上
- **目標**: 90%以上
- **工数目安**: 3日
- **内容**:
  - 未テストコードの洗い出し
  - エッジケーステスト追加
  - 統合テスト追加

#### 11.1.2 パフォーマンス最適化
- **工数目安**: 2日
- **内容**:
  - ボトルネック特定
  - 最適化実施
  - ベンチマーク結果公開

#### 11.1.3 バグ修正
- **工数目安**: 継続的
- **内容**:
  - 既知バグの修正
  - エッジケース対応

### 11.2 ドキュメント整備

#### 11.2.1 APIリファレンス
- **工数目安**: 2日
- **内容**:
  - 全public APIのドキュメント
  - XMLドキュメントコメント完備

#### 11.2.2 チュートリアル
- **工数目安**: 2日
- **内容**:
  - Getting Started
  - 基本的な使い方
  - 高度な使い方
  - トラブルシューティング

#### 11.2.3 サンプルプロジェクト
- **工数目安**: 2日
- **内容**:
  - BasicUsage（既存）
  - AdvancedQueries
  - PerformanceOptimization
  - EditorIntegration

### 11.3 リリース準備

#### 11.3.1 Asset Store対応
- **工数目安**: 1日
- **内容**:
  - パッケージ構成確認
  - スクリーンショット作成
  - 説明文作成

#### 11.3.2 ライセンス確認
- **工数目安**: 0.5日
- **内容**:
  - 依存ライブラリのライセンス確認
  - LICENSE.md最終確認

### 完了条件
- [ ] テストカバレッジ90%以上
- [ ] 既知バグがない
- [ ] 全APIにドキュメントがある
- [ ] サンプルプロジェクトが動作する
- [ ] Asset Store審査提出可能な状態

---

## 検討中機能（優先度未定）

以下の機能は要望に応じて検討します。

### リアルタイム同期
- 複数エディタ間でのデータ同期
- 競合解決メカニズム
- **複雑度**: 高

### 暗号化
- センシティブデータの暗号化保存
- AES暗号化
- **複雑度**: 中

### 圧縮
- 大規模データの圧縮保存
- LZ4/Gzip対応
- **複雑度**: 中

### ネットワーク対応
- リモートデータベースとの連携
- REST API対応
- **複雑度**: 高

### ビジュアルクエリビルダー
- GUIベースのクエリ作成
- ドラッグ&ドロップ
- **複雑度**: 高

### データビジュアライゼーション
- グラフ・チャート表示
- 統計情報の可視化
- **複雑度**: 中

### Excelアドイン
- Excelからの直接編集
- リアルタイム同期
- **複雑度**: 高

### 他フォーマット対応
- JSON, XML, SQLite連携
- インポート/エクスポート
- **複雑度**: 中

---

## スケジュール概要

```
Phase 9 (v0.5.0): 高度なSQL機能
├── 9.1 JOIN機能           [3.5日]
├── 9.2 集計関数           [4日]
├── 9.3 サブクエリ         [3日]
├── 9.4 その他SQL機能      [4日]
└── 9.5 テスト             [2日]
合計: 約16.5日

Phase 10 (v0.6.0): 追加ツール
├── 10.1 スキーマ比較      [3日]
├── 10.2 マイグレーション  [4日]
├── 10.3 バックアップ      [3日]
├── 10.4 CLI               [3日]
└── 10.5 データ生成        [2日]
合計: 約15日

Phase 11 (v1.0.0): 安定版リリース
├── 11.1 品質向上          [5日+]
├── 11.2 ドキュメント      [6日]
└── 11.3 リリース準備      [1.5日]
合計: 約12.5日+

総計: 約44日+
```

---

## ファイル構成（Phase 9-11 完成形）

```
Packages/jp.xeon.x-scriptable-db/
├── Editor/
│   ├── SqlEditor/
│   │   ├── SqlJoinExecutor.cs        # Phase 9
│   │   ├── SqlAggregateFunctions.cs  # Phase 9
│   │   ├── SqlGroupExecutor.cs       # Phase 9
│   │   └── SqlSubqueryExecutor.cs    # Phase 9
│   ├── Schema/
│   │   ├── SchemaComparer.cs         # Phase 10
│   │   └── SchemaCompareWindow.cs    # Phase 10
│   ├── Migration/
│   │   ├── MigrationDefinition.cs    # Phase 10
│   │   └── MigrationRunner.cs        # Phase 10
│   ├── Backup/
│   │   └── SnapshotManager.cs        # Phase 10
│   ├── CLI/
│   │   └── XScriptableDbCli.cs       # Phase 10
│   └── DataGenerator/
│       └── TestDataGenerator.cs      # Phase 10
├── Tests/
│   └── SqlEditor/
│       ├── SqlJoinTests.cs           # Phase 9
│       └── SqlAggregateTests.cs      # Phase 9
└── Samples~/
    ├── BasicUsage/                   # 既存
    ├── AdvancedQueries/              # Phase 11
    ├── PerformanceOptimization/      # Phase 11
    └── EditorIntegration/            # Phase 11
```
