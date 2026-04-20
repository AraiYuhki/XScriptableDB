# XScriptableDB インストール手順 (BOOTH版)

BOOTHで配布されている `.tgz` ファイル（Unityパッケージ）を、Unity Package Manager (UPM) を使用してインストールする方法を説明します。

## 1. 前提条件

XScriptableDBは以下のパッケージに依存しています。事前にインストールされていることを確認してください。

- **Addressables** (com.unity.addressables)
    - Unity Editor の `Window > Package Manager` を開き、 `Unity Registry` から `Addressables` を選択して `Install` してください。

## 2. インストール手順

1.  Unity プロジェクトを開きます。
2.  メニューから **Window > Package Manager** を選択します。
3.  Package Manager ウィンドウの左上にある **「+」** ボタンをクリックします。
4.  **Add package from tarball...** を選択します。
5.  ダウンロードした `jp.xeon.x-scriptable-db-1.1.3.tgz`（または最新バージョン）を選択して「開く」をクリックします。
6.  インストールが完了すると、Package Manager のリストに `XScriptableDB` が表示されます。

## 3. サンプルの導入（任意）

XScriptableDB には多数のサンプルが含まれています。

1.  Package Manager で `XScriptableDB` を選択します。
2.  右側の詳細パネルにある **Samples** セクションを展開します。
3.  必要なサンプルの横にある **Import** ボタンをクリックします。
4.  サンプルはプロジェクトの `Assets/Samples/XScriptableDB/[Version]/[SampleName]` にインポートされます。

## 4. トラブルシューティング

- **エラーが出る場合**: Unity のコンソールを確認してください。依存関係（Addressables）が足りない場合にコンパイルエラーが発生することがあります。
- **再インストール**: 古いバージョンを削除してから、新しい `.tgz` ファイルを上記の手順で再度選択してください。

---
© 2026 Xeon - [GitHub Repository](https://github.com/AraiYuhki/XScriptableDB)
