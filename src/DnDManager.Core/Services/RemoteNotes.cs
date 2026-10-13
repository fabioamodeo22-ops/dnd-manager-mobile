using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DnDManager.Data;

namespace DnDManager.Services
{
    public class RestPersonalNote
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("campaign_id")] public long CampaignId { get; set; }
        [JsonPropertyName("user_id")] public System.Guid UserId { get; set; }
        [JsonPropertyName("content")] public string Content { get; set; } = "";
    }

    /// <summary>
    /// Note personali per la campagna corrente: private per costruzione — le regole di sicurezza sul
    /// database limitano l'accesso al solo proprietario della riga, nessuno le vede (nemmeno il Master).
    /// </summary>
    public static class RemoteNotes
    {
        public static async Task<string> GetMyNoteAsync()
        {
            if (AppSession.UserId == null) return "";
            var rows = await SupabaseRest.GetAsync<RestPersonalNote>(
                $"personal_notes?campaign_id=eq.{AppSession.CampaignId}&user_id=eq.{AppSession.UserId}&select=*");
            return rows.FirstOrDefault()?.Content ?? "";
        }

        public static async Task SaveMyNoteAsync(string content)
        {
            if (AppSession.UserId == null) return;

            var existing = await SupabaseRest.GetAsync<RestPersonalNote>(
                $"personal_notes?campaign_id=eq.{AppSession.CampaignId}&user_id=eq.{AppSession.UserId}&select=id");
            var row = existing.FirstOrDefault();

            if (row == null)
            {
                var body = new { campaign_id = AppSession.CampaignId, user_id = AppSession.UserId, content };
                await SupabaseRest.InsertRawAsync("personal_notes", body);
            }
            else
            {
                await SupabaseRest.UpdateAsync("personal_notes", row.Id, new { content });
            }
        }
    }
}
