using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnDManager.Data.Remote;
using DnDManager.Models;

namespace DnDManager.Services
{
    /// <summary>
    /// Ponte tra il modello Spell usato dall'interfaccia e le tabelle su Supabase.
    /// L'assegnazione a personaggio è molti-a-molti tramite "character_spells": lo stesso
    /// incantesimo/trucchetto può essere assegnato a più personaggi contemporaneamente.
    /// </summary>
    public static class RemoteSpells
    {
        public static async Task<List<Spell>> GetAllAsync(bool cantripsOnly)
        {
            var rows = await SupabaseRest.GetAsync<RestSpell>("spells?select=*&order=level,name");
            var assigns = await SupabaseRest.GetAsync<RestCharacterSpell>("character_spells?select=*");
            var characters = await RemoteCharacters.GetAllAsync();
            var nameById = characters.ToDictionary(c => c.Id, c => c.Name);
            var bySpell = assigns.GroupBy(a => a.SpellId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.CharacterId).ToList());

            return rows.Where(r => (r.Level == 0) == cantripsOnly).Select(r =>
            {
                var sp = ToLocal(r);
                if (bySpell.TryGetValue(r.Id, out var charIds))
                    sp.AssignedNames = charIds.Where(nameById.ContainsKey).Select(id => nameById[id]).ToList();
                return sp;
            }).ToList();
        }

        public static async Task<long> SaveAsync(Spell s)
        {
            var body = new Dictionary<string, object?>
            {
                ["name"] = s.Name,
                ["level"] = s.Level,
                ["school"] = s.School,
                ["classes"] = s.Classes,
                ["components"] = s.Components,
                ["casting_time"] = s.CastingTime,
                ["range"] = s.Range,
                ["duration"] = s.Duration,
                ["description"] = s.Description,
                ["image_url"] = s.ImagePath
            };

            if (s.Id == 0)
            {
                var created = await SupabaseRest.InsertAsync<RestSpell>("spells", body);
                if (created != null) return created.Id;
                var mine = await SupabaseRest.GetAsync<RestSpell>(
                    $"spells?name=eq.{Uri.EscapeDataString(s.Name)}&order=id.desc&limit=1&select=id");
                return mine.FirstOrDefault()?.Id ?? 0;
            }

            await SupabaseRest.UpdateAsync("spells", s.Id, body);
            return s.Id;
        }

        public static async Task DeleteAsync(long id) => await SupabaseRest.DeleteAsync("spells", id);

        // ---------- assegnazioni multiple (character_spells) ----------
        public static async Task<List<long>> GetAssignedCharacterIdsAsync(long spellId)
        {
            var rows = await SupabaseRest.GetAsync<RestCharacterSpell>(
                $"character_spells?spell_id=eq.{spellId}&select=*");
            return rows.Select(r => r.CharacterId).ToList();
        }

        public static async Task<HashSet<long>> GetAssignedSpellIdsAsync(long characterId)
        {
            var rows = await SupabaseRest.GetAsync<RestCharacterSpell>(
                $"character_spells?character_id=eq.{characterId}&select=*");
            return rows.Select(r => r.SpellId).ToHashSet();
        }

        public static async Task AssignAsync(long spellId, long characterId) =>
            await SupabaseRest.InsertRawAsync("character_spells",
                new { spell_id = spellId, character_id = characterId });

        public static async Task UnassignAsync(long spellId, long characterId) =>
            await SupabaseRest.DeleteWhereAsync("character_spells",
                $"spell_id=eq.{spellId}&character_id=eq.{characterId}");

        private static Spell ToLocal(RestSpell r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            Level = r.Level,
            School = r.School,
            Classes = r.Classes,
            Components = r.Components,
            CastingTime = r.CastingTime,
            Range = r.Range,
            Duration = r.Duration,
            Description = r.Description,
            ImagePath = r.ImageUrl
        };
    }
}
