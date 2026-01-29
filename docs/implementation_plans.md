# XScriptableDB 実装計画書

## 1. 目的・コンセプト

### 1.1 目的

* Unity において **非エンジニアでも扱える DB ライクなマスターデータ管理環境**を提供する
* ScriptableObject を永続ストレージとしつつ、
  **検索・編集・差分確認・CSV連携**を Editor 上で完結させる

### 1.2 基本方針

* ランタイムは **高速・シンプル**
* 複雑さは **Editor 側に集約**
* MasterMemory の思想（Immutable / Index / 高速検索）を踏襲

---

## 2. 全体アーキテクチャ概要

```
+-----------------------------+
|       Editor Layer          |
|-----------------------------|
| - DB Browser                |
| - Table Editor              |
| - SQL Query Editor          |
| - CSV Import / Export       |
| - Diff Viewer               |
+--------------↑--------------+
               |
+--------------↓--------------+
|     Data Definition Layer   |
|-----------------------------|
| - Table Schema              |
| - PrimaryKey / SecondaryKey |
| - Validation Rules          |
+--------------↑--------------+
               |
+--------------↓--------------+
|      Storage Layer          |
|-----------------------------|
| - ScriptableObject Assets   |
| - Serialized Index Data     |
+--------------↑--------------+
               |
+--------------↓--------------+
|      Runtime Layer          |
|-----------------------------|
| - Read-only Table API       |
| - Indexed Lookup            |
+-----------------------------+
```

---

## 3. 機能別 実装計画

---

## 3.1 データ管理（ScriptableObject）

### 3.1.1 基本構造

* 1テーブル = 1 ScriptableObject
* レコードは **PrimaryKey でソートされた配列**として保持

```csharp
class XTableAsset<T> : ScriptableObject
{
    T[] Records; // PrimaryKey順
    IndexData Indexes;
}
```

### 3.1.2 制約

* Runtime では **完全 ReadOnly**
* Editor のみで変更可能

---

## 3.2 テーブルスキーマ定義

### 3.2.1 PrimaryKey

* 必須
* 一意
* ソート基準

### 3.2.2 SecondaryKey

* 複数指定可能
* 各 SecondaryKey ごとに **インデックス配列を生成・保存**

---

## 3.3 SecondaryKey インデックス設計（重要）

### 3.3.1 保存形式

* PrimaryKey ソート済み配列の **インデックス番号**を基準とする

```
Primary Records (sorted)
Index: 0   1   2   3   4
Data:  A   B   C   D   E
```

SecondaryKey ごとに：

```
SecondaryKeyIndex:
KeyValue -> [recordIndex, recordIndex, ...]
```

### 3.3.2 利点

* ScriptableObject サイズ最小化
* キャッシュフレンドリー
* MasterMemory互換の検索性能

---

## 3.4 Editor: データ編集ツール

### 3.4.1 XScriptableDB Browser

* テーブル一覧
* レコード数
* Key 情報表示

### 3.4.2 Table Editor

* 行・列ベースの編集 UI
* PrimaryKey 重複チェック
* Validation 表示

※ 非エンジニア向けに
**Excelライク操作を最優先**

---

## 3.5 CSV / TSV インポート・エクスポート

### 3.5.1 エクスポート

* 現在のテーブル状態を CSV / TSV 出力
* PrimaryKey 順を保証

### 3.5.2 インポートフロー

```
CSV選択
  ↓
差分解析
  ↓
Diff Viewer 表示
  ↓
適用可否チェック
  ↓
反映
```

---

## 3.6 差分表示ツール（Diff Viewer）

### 3.6.1 比較単位

* レコード単位（PrimaryKey基準）
* フィールド単位差分

### 3.6.2 表示分類

* 追加
* 削除
* 変更

### 3.6.3 操作

* 差分ごとに Apply / Ignore
* 一括適用

---

## 3.7 SQLベース Editor 拡張

### 3.7.1 目的

* 非エンジニアでも条件指定検索を可能にする
* 内部実装の複雑さを隠蔽

### 3.7.2 想定SQLサブセット

```sql
SELECT * FROM Table
WHERE SecondaryKey = 'value'
ORDER BY PrimaryKey
```

* UPDATE / DELETE は Editor 限定
* Runtime では SELECT のみ

### 3.7.3 実装方針

* 独自パーサ or 軽量SQLパーサ
* 内部では Index Lookup に変換

---

## 3.8 Runtime API

### 3.8.1 提供機能

* PrimaryKey Lookup
* SecondaryKey Lookup
* All Records Enumerate

### 3.8.2 制約

* GC Alloc 最小化
* LINQ 非依存

---

## 4. フェーズ分割（実装ロードマップ）

### Phase 1（MVP）

* ScriptableObject テーブル
* PrimaryKey 管理
* Editor Table Editor（最小）

### Phase 2

* SecondaryKey インデックス
* 高速検索 Runtime API

### Phase 3

* CSV Import / Export
* Diff Viewer

### Phase 4

* SQL Editor
* DB Browser 統合

### Phase 5（Asset Store向け）

* UI磨き込み
* ドキュメント
* Samples 作成

---

## 5. 非機能要件

* Unity 2021 LTS 以上
* Assembly Definition 分離
* GC Alloc 0 を目標（Runtime）
* 大規模マスター（数万件）対応

---

## 6. 将来拡張余地（計画外）

* Google Spreadsheet 連携
* Validation Rule Editor
* 権限・ロール管理
* Runtime SQL Query

---

## 7. 成果物

* XScriptableDB Core
* Editor Tools
* Samples
* Documentation
* Asset Store Package
