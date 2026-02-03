# XScriptableDB - Asset Store 説明文（日本語参考版）

## 短い説明（250文字以内）

Unity向け高性能ScriptableObjectデータベース。O(1)ハッシュ検索、GCゼロクエリ、JOIN/GROUP BY対応SQLエディタ、差分プレビュー付きCSVインポート、スキーママイグレーション、包括的なバリデーション機能を搭載。マスターデータ管理に最適。

---

## 完全な説明

### XScriptableDB - Unity向けプロフェッショナルマスターデータ管理

XScriptableDBは、UnityのScriptableObjectシステム上に構築された強力で本番対応のデータベースソリューションです。外部依存なしで効率的なマスターデータ管理を必要とするゲーム開発者向けに設計されています。

---

### 主な機能

**高性能検索**
- PrimaryKeyによるO(log n)バイナリサーチ
- SecondaryKeyと複合SecondaryKeyによるO(1)ハッシュルックアップ
- ref structを使用したGCゼロのクエリ

**SQLエディタ**
- 完全なSQLサポート：SELECT、UPDATE、DELETE
- JOIN操作：INNER、LEFT、RIGHT、CROSS
- 集計関数：COUNT、SUM、AVG、MIN、MAX
- GROUP BY、HAVING、ORDER BY、LIMIT、OFFSET
- CASE式とサブクエリ
- 文字列関数：UPPER、LOWER、CONCAT、SUBSTRING、TRIM

**CSV/TSVインポート＆エクスポート**
- 自動エンコーディング検出（UTF-8、Shift-JIS等）
- 差分ビューワー：適用前に変更をプレビュー
- 選択的インポート：更新するレコードを選択
- 大規模データセット向けバッチ処理

**スキーマ管理**
- テーブルバージョン間のスキーマ比較
- マイグレーションコードの自動生成
- バックアップと復元機能

**データバリデーション**
- 属性ベースのバリデーション：Required、Range、StringLength、Regex
- 外部キー整合性チェック
- ユニーク制約の強制

**開発者ツール**
- データベースブラウザ：全テーブルとスキーマを表示
- テストデータジェネレーター：サンプルデータを自動生成
- パフォーマンスプロファイラー：クエリパフォーマンスを監視
- バッチ操作用CLIサポート

---

### なぜXScriptableDBか？

| 機能 | XScriptableDB | JSONファイル | SQLite |
|------|---------------|--------------|--------|
| Unity統合 | ネイティブ | 手動 | プラグイン |
| エディタツール | 豊富 | なし | 限定的 |
| ランタイムGC | ゼロ | 高 | 中 |
| 検索速度 | O(1)/O(log n) | O(n) | O(log n) |
| 外部依存なし | はい | はい | いいえ |

---

### こんな用途に最適

- RPGのアイテム/スキル/モンスターデータベース
- ローカライゼーションテーブル
- ゲーム設定データ
- レベル/ステージ定義
- キャラクターステータステーブル
- ショップ/インベントリシステム

---

### クイックスタート

```csharp
// 1. レコードを定義
[Serializable]
public class ItemRecord
{
    [PrimaryKey]
    public int Id;

    [SecondaryKey]
    public string Category;

    public string Name;
    public int Price;
}

// 2. テーブルアセットを作成
[CreateAssetMenu]
public class ItemTable : TableAsset<int, ItemRecord> { }

// 3. 超高速で検索
var item = itemTable.Find(1001);                           // O(log n)
var weapons = itemTable.FindAllBySecondaryKey("Category", "Weapon");  // O(1)

// 4. GCゼロクエリ
using var result = itemTable.Where(r => r.Price > 1000);
foreach (ref readonly var record in result)
{
    Debug.Log(record.Name);
}
```

---

### 動作要件

- Unity 6000.0（Unity 6）以降
- .NET Standard 2.1

### 依存関係

- Addressables 2.3.7以降（Unityに含まれています）

---

### サポート

- ドキュメント：パッケージに含まれています
- メール：xeon.lagunas@gmail.com
- GitHub：https://github.com/AraiYuhki/XScriptableDB

---

## 必要なスクリーンショット

1. **SQLエディタ** - SQLクエリ実行と結果表示
2. **テーブルエディタ** - レコード編集インターフェース
3. **差分ビューワー** - 変更がハイライトされたCSVインポートプレビュー
4. **データベースブラウザ** - テーブル一覧とスキーマビュー
5. **コード例** - 属性ベースのレコード定義
6. **パフォーマンスウィンドウ** - キャッシュ統計とプロファイリング表示

---

## プロモーション用テキスト

**Twitter/X用：**
XScriptableDB - Unity向け高性能マスターデータ管理！O(1)ハッシュ検索、JOIN/GROUP BY対応SQLエディタ、GCゼロクエリ、CSV差分ビューワー。RPGアイテムテーブル、ローカライゼーション、ゲーム設定に最適。#Unity #GameDev #AssetStore

**キャッチコピー：**
「Unity向けマスターデータ管理の決定版」
