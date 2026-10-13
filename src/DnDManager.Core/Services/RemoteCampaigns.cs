using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnDManager.Data;
using DnDManager.Data.Remote;

namespace DnDManager.Services
{
    /// <summary>Una campagna a cui l'utente partecipa, con il suo ruolo (master/player).</summary>
    public class CampaignMembership
    {
        public long CampaignId { get; set; }
        public string Name { get; set; } = "";
        public string Role { get; set; } = "player";
        public bool IsMaster => Role == "master";
    }

    /// <summary>Elenco campagne dell'utente e creazione di nuove campagne (stessa logica di CampaignSelectWindow).</summary>
    public static class RemoteCampaigns
    {
        public static async Task<List<CampaignMembership>> GetMineAsync()
        {
            var uid = AppSession.UserId ?? throw new InvalidOperationException("Sessione non valida, effettua di nuovo l'accesso.");
            var members = await SupabaseRest.GetAsync<RestCampaignMember>($"campaign_members?user_id=eq.{uid}&select=*");
            if (members.Count == 0) return new();

            var ids = string.Join(",", members.Select(m => m.CampaignId).Distinct());
            var camps = await SupabaseRest.GetAsync<RestCampaign>($"campaigns?id=in.({ids})&select=*");
            var byId = camps.ToDictionary(c => c.Id);

            return members
                .Where(m => byId.ContainsKey(m.CampaignId))
                .Select(m => new CampaignMembership { CampaignId = m.CampaignId, Name = byId[m.CampaignId].Name, Role = m.Role })
                .OrderBy(m => m.Name)
                .ToList();
        }

        /// <summary>Crea una campagna: il trigger sul database rende il creatore Master della campagna.</summary>
        public static async Task CreateAsync(string name)
        {
            var uid = AppSession.UserId ?? throw new InvalidOperationException("Sessione non valida, effettua di nuovo l'accesso.");
            await SupabaseRest.InsertRawAsync("campaigns", new { name, created_by = uid });
        }

        /// <summary>Recupera lo username dal profilo dell'utente autenticato.</summary>
        public static async Task<string?> GetMyUsernameAsync()
        {
            try
            {
                if (AppSession.UserId == null) return null;
                var rows = await SupabaseRest.GetAsync<RestProfile>($"profiles?id=eq.{AppSession.UserId}&select=*");
                return rows.FirstOrDefault()?.Username;
            }
            catch { return null; }
        }
    }
}
