# Unity Test Runner

Unity Test Runnerを実行してテスト結果を取得・表示します。

## 引数

`$ARGUMENTS`

- 引数なし または `all`: EditModeとPlayModeの両方を実行
- `edit` または `editmode`: EditModeテストのみ実行
- `play` または `playmode`: PlayModeテストのみ実行

## 実行手順

### 1. Unityエディタのパスを確認

以下のコマンドでUnityのパスを確認:
```bash
ls "/c/Program Files/Unity/Hub/Editor/" 2>/dev/null || echo "Unity Hub not found"
```

このプロジェクトのUnityバージョン: **6000.3.5f1**

### 2. テストの実行

Unityが起動中でないことを確認してから、以下を実行:

**EditModeテスト:**
```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.5f1/Editor/Unity.exe" -runTests -batchmode -projectPath "C:/Projects/XScriptableDB" -testResults "C:/Projects/XScriptableDB/TestResults/editmode-results.xml" -testPlatform EditMode -logFile "C:/Projects/XScriptableDB/TestResults/editmode.log"
```

**PlayModeテスト:**
```bash
"/c/Program Files/Unity/Hub/Editor/6000.3.5f1/Editor/Unity.exe" -runTests -batchmode -projectPath "C:/Projects/XScriptableDB" -testResults "C:/Projects/XScriptableDB/TestResults/playmode-results.xml" -testPlatform PlayMode -logFile "C:/Projects/XScriptableDB/TestResults/playmode.log"
```

### 3. 結果の解析

テスト完了後、XMLファイルを読み込んで結果を解析:
- `TestResults/editmode-results.xml`
- `TestResults/playmode-results.xml`

PowerShellで解析:
```powershell
powershell -ExecutionPolicy Bypass -File ".claude/commands/parse-test-results.ps1" -ResultFile "TestResults/editmode-results.xml"
```

または、XMLを直接読み込んで以下の情報を抽出:
- `test-run` 要素の属性: `total`, `passed`, `failed`, `skipped`, `duration`
- 失敗したテストケース: `test-case[result='Failed']` の `fullname`, `failure/message`, `failure/stack-trace`

### 4. 結果の表示形式

```
========================================
  Unity Test Results (EditMode)
========================================

Total:   10
Passed:  8
Failed:  2
Skipped: 0
Duration: 5.23s

----------------------------------------
  Failed Tests
----------------------------------------

Test: Xeon.XScriptableDB.Tests.CsvParserTests.ParseWithQuotes
Message: Expected: "value" But was: value
Stack Trace:
  at CsvParserTests.ParseWithQuotes() in CsvParserTests.cs:line 42
```

## 注意事項

- Unityエディタが起動中の場合は `-quit` で終了させるか、手動で閉じてください
- バッチモードのため、ライセンス認証が必要な場合があります
- テスト実行には時間がかかります（タイムアウト: 10分を推奨）
- ログファイル (`TestResults/*.log`) でエラー詳細を確認できます

## トラブルシューティング

テストが実行されない場合:
1. Unityエディタのパスが正しいか確認
2. プロジェクトパスが正しいか確認
3. ログファイルでエラーを確認: `cat TestResults/editmode.log | tail -100`
