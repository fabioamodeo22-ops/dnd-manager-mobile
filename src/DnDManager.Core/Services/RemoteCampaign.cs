using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnDManager.Data;
using DnDManager.Data.Remote;
using DnDManager.Models;

namespace DnDManager.Services
{
    /// <summary>Dettagli della campagna correntemente aperta (AppSession.CampaignId) su Supabase.</summary>
    public static class RemoteCampaign
    {
        public static async Task<Campaign?> GetCurrentAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestCampaign>($"campaigns?id=eq.{AppSession.CampaignId}&select=*");
            var r = rows.FirstOrDefault();
            return r == null ? null : new Campaign { Id = r.Id, Name = r.Name, Description = r.Description, Notes = r.Notes, Story = r.Story };
        }

        public static async Task SaveAsync(Campaign c)
        {
            var body = new { name = c.Name, description = c.Description, notes = c.Notes, story = c.Story };
            await SupabaseRest.UpdateAsync("campaigns", c.Id, body);
        }
    }

    /// <summary>Missioni della campagna correntemente aperta.</summary>
    public static class RemoteQuests
    {
        public static async Task<List<Quest>> GetAllAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestQuest>(
                $"quests?campaign_id=eq.{AppSession.CampaignId}&select=*&order=id");
            return rows.Select(ToLocal).ToList();
        }

        public static async Task<long> SaveAsync(Quest q)
        {
            var body = new Dictionary<string, object?>
            {
                ["campaign_id"] = AppSession.CampaignId,
                ["title"] = q.Title,
                ["description"] = q.Description,
                ["status"] = q.Status,
                ["is_locked"] = q.IsLocked,
                ["linked_quest_id"] = q.LinkedQuestId
            };

            if (q.Id == 0)
            {
                var created = await SupabaseRest.InsertAsync<RestQuest>("quests", body);
                if (created != null) return created.Id;
                var mine = await SupabaseRest.GetAsync<RestQuest>(
                    $"quests?campaign_id=eq.{AppSession.CampaignId}&title=eq.{Uri.EscapeDataString(q.Title)}&order=id.desc&limit=1&select=id");
                return mine.FirstOrDefault()?.Id ?? 0;
            }

            await SupabaseRest.UpdateAsync("quests", q.Id, body);
            return q.Id;
        }

        public static async Task DeleteAsync(long id) => await SupabaseRest.DeleteAsync("quests", id);

        /// <summary>Blocca (nasconde ai Giocatori) o recupera una missione.</summary>
        public static async Task SetLockedAsync(long id, bool locked) =>
            await SupabaseRest.UpdateAsync("quests", id, new { is_locked = locked });

        // ---------- Note riservate al Master (tabella separata, permessi propri) ----------
        public static async Task<string> GetMasterNoteAsync(long questId)
        {
            var rows = await SupabaseRest.GetAsync<RestQuestNote>($"quest_master_notes?quest_id=eq.{questId}&select=*");
            return rows.FirstOrDefault()?.Notes ?? "";
        }

        public static async Task SaveMasterNoteAsync(long questId, string notes)
        {
            var existing = await SupabaseRest.GetAsync<RestQuestNote>($"quest_master_notes?quest_id=eq.{questId}&select=quest_id");
            if (existing.Count == 0)
                await SupabaseRest.InsertRawAsync("quest_master_notes", new { quest_id = questId, notes });
            else
                await SupabaseRest.PatchWhereAsync("quest_master_notes", $"quest_id=eq.{questId}", new { notes });
        }

        private static Quest ToLocal(RestQuest r) => new()
        {
            Id = r.Id,
            CampaignId = r.CampaignId,
            Title = r.Title,
            Description = r.Description,
            Status = r.Status,
            IsLocked = r.IsLocked,
            LinkedQuestId = r.LinkedQuestId
        };
    }
}
