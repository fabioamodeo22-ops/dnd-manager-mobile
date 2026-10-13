using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DnDManager.Models;

namespace DnDManager.Services
{
    public class RestManual
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("file_url")] public string FileUrl { get; set; } = "";
    }

    /// <summary>Libreria manuali condivisa (globale, come le regole del gioco: non appartiene a una campagna specifica).</summary>
    public static class RemoteManuals
    {
        public static async Task<List<Manual>> GetAllAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestManual>("manuals?select=*&order=title");
            return rows.Select(r => new Manual { Id = r.Id, Title = r.Title, FilePath = r.FileUrl }).ToList();
        }

        public static async Task AddAsync(string title, string fileUrl) =>
            await SupabaseRest.InsertRawAsync("manuals", new { title, file_url = fileUrl });

        public static async Task RenameAsync(long id, string newTitle) =>
            await SupabaseRest.UpdateAsync("manuals", id, new { title = newTitle });

        public static async Task DeleteAsync(long id) => await SupabaseRest.DeleteAsync("manuals", id);
    }
}
