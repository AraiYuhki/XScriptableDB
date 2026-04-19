using System;
using System.Text;

namespace Xeon.XScriptableDB.Editor
{
    /// <summary>
    /// ストリーミングインポートの設定。
    /// </summary>
    public class StreamingImportSettings
    {
        /// <summary>チャンクサイズ（一度に処理する行数）</summary>
        public int ChunkSize { get; set; } = 1000;

        /// <summary>エンコーディング</summary>
        public Encoding Encoding { get; set; } = Encoding.UTF8;

        /// <summary>区切り文字</summary>
        public char Delimiter { get; set; } = ',';

        /// <summary>ヘッダー行が存在するかどうか</summary>
        public bool HasHeader { get; set; } = true;

        /// <summary>エラー時に続行するかどうか</summary>
        public bool ContinueOnError { get; set; } = true;

        /// <summary>最大エラー数（これを超えると処理が停止します）</summary>
        public int MaxErrors { get; set; } = 100;

        /// <summary>進捗コールバック</summary>
        public Action<StreamingImportProgress> OnProgress { get; set; }
    }
}