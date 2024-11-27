using AnimeList.Models;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text;

namespace AnimeList
{
    public static class Utils
    {
        public static readonly string addonPrefix = ":";
        public static readonly string anilistPrefix = "anilist" + addonPrefix;
        public static readonly string torrentioPrefix = "tt" + addonPrefix;
        public static readonly string kitsuPrefix = "kitsu" + addonPrefix;

        public static bool IsTokenExpired(DateTime? expirationDate)
        {
            return DateTime.UtcNow >= (expirationDate ?? DateTime.UtcNow).AddMinutes(-5);
        }

        public static Meta ExpiredMeta() 
        {
            return new Meta { id = $"{anilistPrefix}token-expired", name = "Token expired, re-install addon" };
        }

        public static List<Meta> ExpiredMetas()
        {
            return [ExpiredMeta()];
        }

        public static string CompressString(string text)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            using var memoryStream = new MemoryStream();
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gzipStream.Write(buffer, 0, buffer.Length);
            }
            memoryStream.Position = 0;
            byte[] compressed = new byte[memoryStream.Length];
            memoryStream.Read(compressed, 0, compressed.Length);

            byte[] gzBuffer = new byte[compressed.Length + 4];
            Buffer.BlockCopy(compressed, 0, gzBuffer, 4, compressed.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gzBuffer, 0, 4);
            return Convert.ToBase64String(gzBuffer);
        }

        public static string DecompressString(string compressedText)
        {
            byte[] gzBuffer = Convert.FromBase64String(compressedText);
            using var memoryStream = new MemoryStream();
            int dataLength = BitConverter.ToInt32(gzBuffer, 0);
            memoryStream.Write(gzBuffer, 4, gzBuffer.Length - 4);

            byte[] buffer = new byte[dataLength];
            memoryStream.Position = 0;

            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
            {
                gzipStream.Read(buffer, 0, buffer.Length);
            }
            return Encoding.UTF8.GetString(buffer);
        }

        public static string GetListTypeString(ListType list, TokenData tokenData)
        {
            return (tokenData?.anime_service ?? AnimeService.Kitsu) == AnimeService.Kitsu
                ? list.ToString().ToLower()
                : list.ToString().ToUpper();
        }
    }
}
