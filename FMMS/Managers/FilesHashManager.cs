using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FMMS.Managers
{
    public static class FilesHashManager
    {
        public static string GetSha256Hash(string filePath)
        {
            // Check if the file exists
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("File not found.", filePath);
            }

            using SHA256 sha256 = SHA256.Create();
            using FileStream fileStream = File.OpenRead(filePath);
            // ComputeHash reads the file stream and returns the hash as a byte array
            byte[] hashBytes = sha256.ComputeHash(fileStream);

            // Convert the byte array to a hexadecimal string
            StringBuilder sb = new();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        public static async Task<string> GetSha256HashAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Файл не найден.", filePath);
            }

            // Открываем файл для асинхронного чтения
            await using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);

            // Асинхронно вычисляем хеш
            byte[] hashBytes = await SHA256.HashDataAsync(fileStream);

            // Преобразуем массив байтов в шестнадцатеричную строку
            StringBuilder sb = new(hashBytes.Length * 2);
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }

        public static async Task<string> GetSha256HashAsync(Stream stream)
        {
            // Убедимся, что поток находится в начале (если возможно)
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            // Асинхронно вычисляем хеш из потока
            byte[] hashBytes = await SHA256.HashDataAsync(stream);

            // Преобразуем массив байтов в шестнадцатеричную строку
            StringBuilder sb = new(hashBytes.Length * 2);
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
