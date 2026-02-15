# CsvParser ボトルネック調査メモ

## 調査結果
CsvParser の処理をコードレベルで確認した結果、主なボトルネックは以下でした。

1. **`CsvUtility.EscapeQuotedStrings` の繰り返し `Replace`**
   - 旧実装は、検出した quoted 文字列ごとに `result.Replace(...)` を実行していました。
   - CSV 全体文字列を毎回走査するため、データ量と引用文字列数に比例して負荷が急増します。

2. **行パース時の一時オブジェクト生成が多い**
   - ヘッダー処理後の各行で `Dictionary<string, string>` を都度生成していました。
   - LINQ (`Select`, `ToList`) を併用しており、GC Alloc が増えやすい構造でした。

3. **セルごとの反射探索**
   - 各セル代入で `type.GetProperty(...)` / `type.GetField(...)` を呼び出し、毎回メンバー探索を実施していました。
   - 行数 × 列数分の反射探索が発生するため、大規模 CSV で無視できないコストになります。

## 今回の改善
- quoted 文字列エスケープを **1 パスの `StringBuilder` ベース** に変更。
- ヘッダーから **列インデックス→メンバー** のマップを一度だけ構築し、行ごとの `Dictionary<string, string>` 生成を削減。
- 反射代入時は `MemberInfo` を `PropertyInfo` / `FieldInfo` に直接キャストして利用し、セルごとの再探索を削減。

## 期待される効果
- CSV 文字列の前処理（quoted string 退避）の時間短縮。
- パース中の GC Alloc 低減。
- 大規模データ（行数・列数が多いケース）でのスループット改善。

## 補足
- 仕様変更は行っておらず、既存 API (`Parse`, `ParseRecord`, `ToCSV`) は維持。
- さらなる調査としては Unity Profiler で `CsvParser.Parse` の CPU/GC を比較し、データサイズ別に差分を確認するのが有効です。
