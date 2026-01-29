using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Xeon.XScriptableDB
{
    public class DB
{
    private static DB instance = new DB();
    public static DB Instance
    {
        get
        {
            if (instance == null)
                instance = new DB();
            return instance;
        }
    }
    private Dictionary<Type, ScriptableObject> tables = null;

    private DB()
    {
        Reload();
    }

    ~DB()
    {
        foreach (var table in tables.Values)
            SafeRelease(table);
        tables.Clear();
    }

    public static T Get<T>() where T : ScriptableObject
        => Instance.tables[typeof(T)] as T;

    private void Reload()
    {
        if (tables != null)
        {
            foreach (var table in tables.Values)
                SafeRelease(table);
        }
        tables = new()
        {
        };
    }

    private ScriptableObject LoadTable(string key)
     => Addressables.LoadAssetAsync<ScriptableObject>(key).WaitForCompletion();

    private void SafeRelease(object target)
    {
        if (target == null) return;
        Addressables.Release(target);
    }

#if UNITY_EDITOR
    public Dictionary<Type, ScriptableObject> GetTableList() => tables;
    public string[] GetTableNames() => tables.Keys.Select(type => type.Name).ToArray();

    public ScriptableObject GetTable(string name) => tables.FirstOrDefault(pair => pair.Key.Name == name).Value;
    [UnityEditor.MenuItem("Tools/Master/DB再読み込み")]
    public static void ReloadDB()
    {
        Instance.Reload();
    }

    [UnityEditor.MenuItem("Tools/Master/DBアンロード")]
    public static void UnloadDB()
    {
        if (instance == null) return;
        instance.Unload();
        instance = null;
    }

    private void Unload()
    {
        foreach (var (_, table) in tables)
            SafeRelease(table);
        tables.Clear();
    }

    [UnityEditor.MenuItem("Tools/Master/TSVにエクスポート")]
    public static void ExportToTsv()
    {
        var folderPath = UnityEditor.EditorUtility.SaveFolderPanel("エクスポート先を選択", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Masters");
        if (string.IsNullOrEmpty(folderPath)) return;
        foreach (var table in Instance.tables.Values)
        {
            if (table is not IExportable exporter) continue;
            var filePath = Path.Combine(folderPath, table.name + ".tsv");
            exporter.Export(filePath, Encoding.UTF8);
        }
    }
    [UnityEditor.MenuItem("Tools/Master/TSVから一括インポート")]
    public static void ImportFromTsv()
    {
        var folderPath = UnityEditor.EditorUtility.OpenFolderPanel("インポート先を選択", Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Masters");
        if (string.IsNullOrEmpty(folderPath)) return;
        var directoryInfo = new DirectoryInfo(folderPath);
        var files = directoryInfo.GetFiles("*.tsv", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            var tableName = Path.GetFileNameWithoutExtension(file.Name);
            var targetTable = Instance.tables.Values.FirstOrDefault(table => tableName == table.GetType().Name);
            if (targetTable is IImportable importable)
            {
                Debug.Log($"Import from {targetTable.name} to {file.Name}");
                importable.Import(file.FullName);
                UnityEditor.EditorUtility.SetDirty(targetTable);
                continue;
            }
        }
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
    }
#endif
    }
}
