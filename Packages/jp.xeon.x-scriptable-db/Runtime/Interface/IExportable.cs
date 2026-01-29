using System.Text;

namespace Xeon.XScriptableDB
{
    public interface IExportable
    {
        void Export(string filePath, Encoding encoding = null);
    }
}
