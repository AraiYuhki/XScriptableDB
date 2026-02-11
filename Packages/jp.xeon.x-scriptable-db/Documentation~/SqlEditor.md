# SQL Editor ガイド

## 概要

SQL Editorは、SQLライクな構文でテーブルデータを検索・更新できるエディタツールです。

**メニュー**: `Window > XScriptableDB > SQL Editor`

## 基本操作

- **実行**: F5キー または Ctrl+Enter
- **履歴**: Ctrl+↑/↓ で過去のクエリを呼び出し
- **テーブル挿入**: ツールバーの「Insert Table」からテーブル名を挿入

## SELECT文

### 基本構文

```sql
SELECT columns FROM table_name [WHERE conditions] [ORDER BY columns] [LIMIT n] [OFFSET n]
```

### 全カラム取得

```sql
SELECT * FROM ItemTable
```

### カラム指定

```sql
SELECT Id, Name, Price FROM ItemTable
```

### エイリアス

```sql
SELECT Id, Name AS ItemName FROM ItemTable
```

### WHERE句

```sql
-- 等価比較
SELECT * FROM ItemTable WHERE Category = 'Weapon'

-- 数値比較
SELECT * FROM ItemTable WHERE Price > 1000

-- 文字列比較（大文字小文字を区別しない）
SELECT * FROM ItemTable WHERE Name = 'iron sword'
```

### 比較演算子

| 演算子 | 説明 | 例 |
|--------|------|-----|
| `=` | 等しい | `Price = 100` |
| `!=` または `<>` | 等しくない | `Price != 0` |
| `<` | より小さい | `Price < 100` |
| `<=` | 以下 | `Price <= 100` |
| `>` | より大きい | `Price > 100` |
| `>=` | 以上 | `Price >= 100` |

### LIKE演算子

```sql
-- 部分一致（%は0文字以上の任意の文字列）
SELECT * FROM ItemTable WHERE Name LIKE '%剣%'

-- 前方一致
SELECT * FROM ItemTable WHERE Name LIKE '鉄の%'

-- 後方一致
SELECT * FROM ItemTable WHERE Name LIKE '%ソード'

-- 任意の1文字（_）
SELECT * FROM ItemTable WHERE Name LIKE '鉄の_'
```

### IN演算子

```sql
SELECT * FROM ItemTable WHERE Id IN (1001, 1002, 1003)
SELECT * FROM ItemTable WHERE Category IN ('Weapon', 'Armor')
```

### IS NULL / IS NOT NULL

```sql
SELECT * FROM ItemTable WHERE Description IS NULL
SELECT * FROM ItemTable WHERE Description IS NOT NULL
```

### 論理演算子

```sql
-- AND
SELECT * FROM ItemTable WHERE Category = 'Weapon' AND Price > 1000

-- OR
SELECT * FROM ItemTable WHERE Category = 'Weapon' OR Category = 'Armor'

-- 括弧で優先順位を指定
SELECT * FROM ItemTable WHERE (Category = 'Weapon' OR Category = 'Armor') AND Price > 500
```

### ORDER BY句

```sql
-- 昇順（デフォルト）
SELECT * FROM ItemTable ORDER BY Price

-- 昇順（明示的）
SELECT * FROM ItemTable ORDER BY Price ASC

-- 降順
SELECT * FROM ItemTable ORDER BY Price DESC

-- 複数カラム
SELECT * FROM ItemTable ORDER BY Category ASC, Price DESC
```

### LIMIT / OFFSET

```sql
-- 上位10件
SELECT * FROM ItemTable LIMIT 10

-- 11件目から10件（ページング）
SELECT * FROM ItemTable LIMIT 10 OFFSET 10

-- ORDER BYと組み合わせ
SELECT * FROM ItemTable ORDER BY Price DESC LIMIT 5
```

## UPDATE文

### 基本構文

```sql
UPDATE table_name SET column = value [, column = value ...] [WHERE conditions]
```

### 単一カラム更新

```sql
UPDATE ItemTable SET Price = 500 WHERE Id = 1001
```

### 複数カラム更新

```sql
UPDATE ItemTable SET Price = 500, Attack = 30 WHERE Id = 1001
```

### 条件付き一括更新

```sql
-- カテゴリがWeaponのアイテムの価格を2倍に
UPDATE ItemTable SET Price = Price * 2 WHERE Category = 'Weapon'
```

### 注意: WHERE句なしの更新

```sql
-- 全レコードが更新される！
UPDATE ItemTable SET IsActive = 1
```

## DELETE文

### 基本構文

```sql
DELETE FROM table_name [WHERE conditions]
```

### 条件付き削除

```sql
DELETE FROM ItemTable WHERE Id = 1001
DELETE FROM ItemTable WHERE Price = 0
DELETE FROM ItemTable WHERE Category = 'Deprecated'
```

### 注意: WHERE句なしの削除

```sql
-- 全レコードが削除される！
DELETE FROM ItemTable
```

## リテラル

### 文字列

```sql
-- シングルクォート
WHERE Name = 'アイテム名'

-- ダブルクォート
WHERE Name = "アイテム名"

-- エスケープ（クォートを含む文字列）
WHERE Name = 'It''s a test'
```

### 数値

```sql
-- 整数
WHERE Id = 1001

-- 小数
WHERE Rate = 1.5

-- 負数
WHERE Modifier = -10
```

### ブール値

```sql
-- 数値として
WHERE IsActive = 1
WHERE IsActive = 0

-- ブール値は1/0で比較
```

### NULL

```sql
WHERE Value = NULL  -- 常にfalse（IS NULLを使用）
WHERE Value IS NULL -- 正しい書き方
```

## テーブル名

テーブル名はScriptableObjectアセットの名前です。

```sql
-- アセット名: ItemTable
SELECT * FROM ItemTable

-- 大文字小文字は区別しない
SELECT * FROM itemtable
SELECT * FROM ITEMTABLE
```

## 実行結果

### SELECT

- 結果がテーブル形式で表示
- 最大1000行まで表示
- 実行時間（ミリ秒）を表示

### UPDATE / DELETE

- 影響を受けたレコード数を表示
- 実行時間（ミリ秒）を表示

## エラーメッセージ

| エラー | 原因 |
|--------|------|
| `Table not found: XXX` | 指定したテーブルが存在しない |
| `Parse error: Expected XXX` | SQL構文エラー |
| `Execution error: XXX` | 実行時エラー |

## Tips

### パフォーマンス

```sql
-- SecondaryKeyがあるカラムで絞り込むと高速
SELECT * FROM ItemTable WHERE Category = 'Weapon'

-- LIMITを使って結果を制限
SELECT * FROM ItemTable WHERE Price > 0 LIMIT 100
```

### デバッグ

```sql
-- まず件数を確認
SELECT * FROM ItemTable WHERE Category = 'Test'

-- 問題なければ更新/削除
DELETE FROM ItemTable WHERE Category = 'Test'
```

### プレビュー

UPDATE/DELETEの前にSELECTで確認：

```sql
-- 1. 対象を確認
SELECT * FROM ItemTable WHERE Price = 0

-- 2. 問題なければ削除
DELETE FROM ItemTable WHERE Price = 0
```
