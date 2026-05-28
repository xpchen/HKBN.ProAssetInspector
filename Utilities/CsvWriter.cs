using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HKBN.ProAssetInspector.Utilities
{
    public static class CsvWriter
    {
        public static void WriteUtf8Bom(string path, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
        {
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            writer.WriteLine(string.Join(",", headers.Select(Escape)));
            foreach (var row in rows)
                writer.WriteLine(string.Join(",", row.Select(Escape)));
        }

        public static string Escape(string value)
        {
            if (value == null)
                return string.Empty;

            var needsQuotes = value.Contains(',') || value.Contains('"');
            if (!needsQuotes)
                return value;

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
