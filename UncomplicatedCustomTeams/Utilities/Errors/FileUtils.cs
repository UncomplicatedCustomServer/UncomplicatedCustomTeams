using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace UncomplicatedCustomTeams.Utilities
{
    public static class FileUtils
    {
        public static async Task<string> ReadAllTextAsync(string filePath)
        {
            using var sourceStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            using var reader = new StreamReader(sourceStream, Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }

        public static async Task WriteAllTextAsync(string filePath, string text)
        {
            byte[] encodedText = Encoding.UTF8.GetBytes(text);

            using var sourceStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
            await sourceStream.WriteAsync(encodedText, 0, encodedText.Length);
        }
    }
}