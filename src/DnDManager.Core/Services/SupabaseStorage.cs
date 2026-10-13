using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace DnDManager.Services
{
    /// <summary>Carica file (immagini, PDF...) sullo storage condiviso di Supabase.</summary>
    public static class SupabaseStorage
    {
        private const string Bucket = "dnd-files";
        private static readonly HttpClient Http = new();

        /// <summary>Carica i byte indicati in una sottocartella del bucket, restituendo l'URL pubblico.</summary>
        public static async Task<string> UploadAsync(string folder, string fileName, byte[] bytes, string contentType)
        {
            await SupabaseService.EnsureFreshTokenAsync();
            var path = $"{folder}/{fileName}";
            var req = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseConfig.Url}/storage/v1/object/{Bucket}/{path}");
            req.Headers.Add("apikey", SupabaseConfig.AnonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", SupabaseService.CurrentAccessToken ?? SupabaseConfig.AnonKey);
            req.Headers.Add("x-upsert", "true");
            req.Content = new ByteArrayContent(bytes);
            req.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            var resp = await Http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
                throw new Exception($"{(int)resp.StatusCode}: {body}");

            return $"{SupabaseConfig.Url}/storage/v1/object/public/{Bucket}/{path}";
        }

        /// <summary>Legge un file locale e lo carica, generando un nome univoco nella sottocartella indicata.</summary>
        public static async Task<string> UploadFileAsync(string folder, string localFilePath)
        {
            var bytes = await File.ReadAllBytesAsync(localFilePath);
            var ext = Path.GetExtension(localFilePath);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            return await UploadAsync(folder, fileName, bytes, ContentTypeFor(ext));
        }

        /// <summary>Carica byte già in memoria (es. da file picker/fotocamera su mobile), con nome univoco.</summary>
        public static async Task<string> UploadBytesAsync(string folder, string originalFileName, byte[] bytes)
        {
            var ext = Path.GetExtension(originalFileName);
            var fileName = $"{Guid.NewGuid():N}{ext}";
            return await UploadAsync(folder, fileName, bytes, ContentTypeFor(ext));
        }

        public static string ContentTypeFor(string extension) => extension.ToLowerInvariant() switch
        {
            ".gif" => "image/gif",
            ".heic" => "image/heic",
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/mp4",
            ".aac" => "audio/aac",
            ".flac" => "audio/flac",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            _ => "application/octet-stream"
        };
    }
}
