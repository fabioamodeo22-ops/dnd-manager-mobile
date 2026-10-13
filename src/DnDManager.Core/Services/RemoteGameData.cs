using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DnDManager.Services
{
    public class RestNamedItem
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
    }

    /// <summary>Razze e classi aggiunte dagli utenti, in aggiunta a quelle base del System Reference Document.</summary>
    public static class RemoteGameData
    {
        public static async Task<List<string>> GetCustomRacesAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestNamedItem>("custom_races?select=*&order=name");
            return rows.Select(r => r.Name).ToList();
        }

        public static async Task AddCustomRaceAsync(string name) =>
            await SupabaseRest.InsertRawAsync("custom_races", new { name });

        public static async Task<List<string>> GetCustomClassesAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestNamedItem>("custom_classes?select=*&order=name");
            return rows.Select(r => r.Name).ToList();
        }

        public static async Task AddCustomClassAsync(string name) =>
            await SupabaseRest.InsertRawAsync("custom_classes", new { name });
    }
}
