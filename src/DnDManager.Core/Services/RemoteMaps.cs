using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnDManager.Data;
using DnDManager.Data.Remote;
using DnDManager.Models;
using System.Text.Json.Serialization;

namespace DnDManager.Services
{
    public class RestMap
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("file_url")] public string FileUrl { get; set; } = "";
        [JsonPropertyName("campaign_id")] public long? CampaignId { get; set; }
    }

    /// <summary>Libreria mappe della campagna correntemente aperta, su Supabase.</summary>
    public static class RemoteMaps
    {
        public static async Task<List<MapItem>> GetAllAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestMap>(
                $"maps?campaign_id=eq.{AppSession.CampaignId}&select=*&order=name");
            return rows.Select(ToLocal).ToList();
        }

        public static async Task<long> SaveAsync(string name, string fileUrl)
        {
            var body = new { name, file_url = fileUrl, campaign_id = AppSession.CampaignId };
            var created = await SupabaseRest.InsertAsync<RestMap>("maps", body);
            if (created != null) return created.Id;

            var mine = await SupabaseRest.GetAsync<RestMap>(
                $"maps?campaign_id=eq.{AppSession.CampaignId}&name=eq.{Uri.EscapeDataString(name)}&order=id.desc&limit=1&select=id");
            return mine.FirstOrDefault()?.Id ?? 0;
        }

        public static async Task DeleteAsync(long id) => await SupabaseRest.DeleteAsync("maps", id);

        private static MapItem ToLocal(RestMap r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            FilePath = r.FileUrl,
            CampaignId = r.CampaignId
        };
    }
}
