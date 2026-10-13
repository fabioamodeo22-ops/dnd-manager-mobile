using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnDManager.Data;
using DnDManager.Data.Remote;
using DnDManager.Models;

namespace DnDManager.Services
{
    /// <summary>
    /// Ponte tra il modello Character usato dall'interfaccia e la tabella "characters" su Supabase.
    /// I personaggi sono sempre legati alla campagna correntemente aperta (AppSession.CampaignId).
    /// </summary>
    public static class RemoteCharacters
    {
        public static async Task<List<Character>> GetAllAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestCharacter>(
                $"characters?campaign_id=eq.{AppSession.CampaignId}&select=*");

            // risolve owner_id -> nome utente, per mostrarlo nel campo "Proprietario"
            var ownerIds = rows.Where(r => r.OwnerId.HasValue).Select(r => r.OwnerId!.Value).Distinct().ToList();
            var owners = new Dictionary<Guid, string>();
            if (ownerIds.Count > 0)
            {
                var filter = string.Join(",", ownerIds);
                var profiles = await SupabaseRest.GetAsync<RestProfile>($"profiles?id=in.({filter})&select=*");
                foreach (var p in profiles) owners[p.Id] = p.Username;
            }

            return rows.Select(r => ToLocal(r, owners)).ToList();
        }

        public static async Task<long> SaveAsync(Character c)
        {
            var ownerId = await ResolveOwnerIdAsync(c);

            var body = new Dictionary<string, object?>
            {
                ["name"] = c.Name,
                ["race"] = c.Race,
                ["class"] = c.Class,
                ["level"] = c.Level,
                ["notes"] = c.Notes,
                ["is_npc"] = c.IsNpc,
                ["owner_id"] = ownerId,
                ["campaign_id"] = AppSession.CampaignId,
                ["str"] = c.Str,
                ["dex"] = c.Dex,
                ["con"] = c.Con,
                ["int_score"] = c.IntScore,
                ["wis"] = c.Wis,
                ["cha"] = c.Cha,
                ["proficient_saves"] = c.ProficientSaves,
                ["proficient_skills"] = c.ProficientSkills,
                ["armor_class"] = c.ArmorClass,
                ["speed"] = c.Speed,
                ["hp_max"] = c.HpMax,
                ["hp_current"] = c.HpCurrent,
                ["hp_temp"] = c.HpTemp,
                ["hit_dice"] = c.HitDice,
                ["languages"] = c.Languages,
                ["other_proficiencies"] = c.OtherProficiencies,
                ["coins_pp"] = c.CoinsPp,
                ["coins_po"] = c.CoinsPo,
                ["coins_pe"] = c.CoinsPe,
                ["coins_pa"] = c.CoinsPa,
                ["coins_pr"] = c.CoinsPr,
                ["sheet_url"] = c.SheetPath,
                ["portrait_url"] = c.PortraitPath
            };

            if (c.Id == 0)
            {
                var created = await SupabaseRest.InsertAsync<RestCharacter>("characters", body);
                if (created != null) return created.Id;

                // con "return=minimal" non torna la riga: recuperiamo l'id con una query separata
                var mine = await SupabaseRest.GetAsync<RestCharacter>(
                    $"characters?campaign_id=eq.{AppSession.CampaignId}&name=eq.{Uri.EscapeDataString(c.Name)}&order=id.desc&limit=1&select=id");
                return mine.FirstOrDefault()?.Id ?? 0;
            }

            await SupabaseRest.UpdateAsync("characters", c.Id, body);
            return c.Id;
        }

        public static async Task DeleteAsync(long id) => await SupabaseRest.DeleteAsync("characters", id);

        /// <summary>Preferisce un OwnerId già noto (es. il Giocatore che crea il proprio personaggio);
        /// altrimenti cerca di risolvere il nome utente scritto nel campo "Proprietario".</summary>
        private static async Task<Guid?> ResolveOwnerIdAsync(Character c)
        {
            if (c.OwnerId.HasValue) return c.OwnerId;
            if (string.IsNullOrWhiteSpace(c.Owner)) return null;

            var rows = await SupabaseRest.GetAsync<RestProfile>(
                $"profiles?username=eq.{Uri.EscapeDataString(c.Owner.Trim())}&select=*");
            return rows.FirstOrDefault()?.Id;
        }

        private static Character ToLocal(RestCharacter r, Dictionary<Guid, string> owners) => new()
        {
            Id = r.Id,
            Name = r.Name,
            Race = r.Race,
            Class = r.Class,
            Level = r.Level,
            Notes = r.Notes,
            SheetPath = r.SheetUrl,
            CampaignId = r.CampaignId,
            IsNpc = r.IsNpc,
            PortraitPath = r.PortraitUrl,
            OwnerId = r.OwnerId,
            Owner = r.OwnerId.HasValue && owners.TryGetValue(r.OwnerId.Value, out var u) ? u : "",
            Str = r.Str,
            Dex = r.Dex,
            Con = r.Con,
            IntScore = r.IntScore,
            Wis = r.Wis,
            Cha = r.Cha,
            ProficientSaves = r.ProficientSaves,
            ProficientSkills = r.ProficientSkills,
            ArmorClass = r.ArmorClass,
            Speed = r.Speed,
            HpMax = r.HpMax,
            HpCurrent = r.HpCurrent,
            HpTemp = r.HpTemp,
            HitDice = r.HitDice,
            Languages = r.Languages,
            OtherProficiencies = r.OtherProficiencies,
            CoinsPp = r.CoinsPp,
            CoinsPo = r.CoinsPo,
            CoinsPe = r.CoinsPe,
            CoinsPa = r.CoinsPa,
            CoinsPr = r.CoinsPr
        };
    }
}
