using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DnDManager.Data;
using DnDManager.Data.Remote;
using DnDManager.Models;

namespace DnDManager.Services
{
    public class RestSharedFile
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("campaign_id")] public long CampaignId { get; set; }
        [JsonPropertyName("owner_id")] public Guid OwnerId { get; set; }
        [JsonPropertyName("file_name")] public string FileName { get; set; } = "";
        [JsonPropertyName("file_url")] public string FileUrl { get; set; } = "";
        [JsonPropertyName("file_type")] public string FileType { get; set; } = "";
    }

    /// <summary>
    /// File caricati dagli utenti per la campagna corrente (musica, immagini, PDF, documenti...).
    /// Le regole di sicurezza sul database fanno sì che questa stessa query restituisca automaticamente
    /// "tutti i file" per il Master e "solo i propri" per un Giocatore — non serve filtrare lato client.
    /// </summary>
    public static class RemoteSharedFiles
    {
        public static async Task<List<SharedFile>> GetAllAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestSharedFile>(
                $"shared_files?campaign_id=eq.{AppSession.CampaignId}&select=*&order=file_name");

            var ownerIds = rows.Select(r => r.OwnerId).Distinct().ToList();
            var owners = new Dictionary<Guid, string>();
            if (ownerIds.Count > 0)
            {
                var filter = string.Join(",", ownerIds);
                var profiles = await SupabaseRest.GetAsync<RestProfile>($"profiles?id=in.({filter})&select=*");
                foreach (var p in profiles) owners[p.Id] = p.Username;
            }

            return rows.Select(r => new SharedFile
            {
                Id = r.Id,
                OwnerId = r.OwnerId,
                OwnerUsername = owners.TryGetValue(r.OwnerId, out var u) ? u : "",
                FileName = r.FileName,
                FileUrl = r.FileUrl,
                FileType = string.IsNullOrEmpty(r.FileType) ? ClassifyType(r.FileName) : r.FileType
            }).ToList();
        }

        public static async Task UploadAsync(string localPath)
        {
            if (AppSession.UserId == null) throw new InvalidOperationException("Sessione non valida.");
            var url = await SupabaseStorage.UploadFileAsync("shared-files", localPath);
            var body = new
            {
                campaign_id = AppSession.CampaignId,
                owner_id = AppSession.UserId,
                file_name = Path.GetFileName(localPath),
                file_url = url,
                file_type = ClassifyType(localPath)
            };
            await SupabaseRest.InsertRawAsync("shared_files", body);
        }

        /// <summary>Variante per mobile: il file arriva già come byte (file picker), non come percorso locale.</summary>
        public static async Task UploadBytesAsync(string fileName, byte[] bytes)
        {
            if (AppSession.UserId == null) throw new InvalidOperationException("Sessione non valida.");
            var url = await SupabaseStorage.UploadBytesAsync("shared-files", fileName, bytes);
            var body = new
            {
                campaign_id = AppSession.CampaignId,
                owner_id = AppSession.UserId,
                file_name = Path.GetFileName(fileName),
                file_url = url,
                file_type = ClassifyType(fileName)
            };
            await SupabaseRest.InsertRawAsync("shared_files", body);
        }

        public static async Task DeleteAsync(long id) => await SupabaseRest.DeleteAsync("shared_files", id);

        public static string ClassifyType(string pathOrName) => Path.GetExtension(pathOrName).ToLowerInvariant() switch
        {
            ".mp3" or ".wav" or ".ogg" or ".m4a" or ".flac" or ".wma" or ".aac" => "audio",
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp" => "image",
            ".pdf" => "pdf",
            ".doc" or ".docx" => "word",
            _ => "file"
        };
    }
}
