using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DnDManager.Data.Remote;
using DnDManager.Models;

namespace DnDManager.Services
{
    /// <summary>
    /// Ponte tra il modello Equipment usato dall'interfaccia e le tabelle su Supabase.
    /// L'assegnazione a personaggio è molti-a-molti tramite "character_equipment": lo stesso
    /// oggetto può essere assegnato a più personaggi contemporaneamente.
    /// </summary>
    public static class RemoteEquipment
    {
        public static async Task<List<Equipment>> GetAllAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestEquipment>("equipment?select=*&order=name");
            var assigns = await SupabaseRest.GetAsync<RestCharacterEquipment>("character_equipment?select=*");
            var characters = await RemoteCharacters.GetAllAsync();
            var nameById = characters.ToDictionary(c => c.Id, c => c.Name);
            var byEquipment = assigns.GroupBy(a => a.EquipmentId)
                .ToDictionary(g => g.Key, g => g.Select(a => a.CharacterId).ToList());

            return rows.Select(r =>
            {
                var eq = ToLocal(r);
                if (byEquipment.TryGetValue(r.Id, out var charIds))
                    eq.AssignedNames = charIds.Where(nameById.ContainsKey).Select(id => nameById[id]).ToList();
                return eq;
            }).ToList();
        }

        public static async Task<long> SaveAsync(Equipment e)
        {
            var body = new Dictionary<string, object?>
            {
                ["name"] = e.Name,
                ["type"] = e.Type,
                ["cost"] = e.Cost,
                ["weight"] = e.Weight,
                ["description"] = e.Description,
                ["image_url"] = e.ImagePath
            };

            if (e.Id == 0)
            {
                var created = await SupabaseRest.InsertAsync<RestEquipment>("equipment", body);
                if (created != null) return created.Id;
                var mine = await SupabaseRest.GetAsync<RestEquipment>(
                    $"equipment?name=eq.{Uri.EscapeDataString(e.Name)}&order=id.desc&limit=1&select=id");
                return mine.FirstOrDefault()?.Id ?? 0;
            }

            await SupabaseRest.UpdateAsync("equipment", e.Id, body);
            return e.Id;
        }

        public static async Task DeleteAsync(long id) => await SupabaseRest.DeleteAsync("equipment", id);

        // ---------- assegnazioni multiple (character_equipment) ----------
        public static async Task<List<long>> GetAssignedCharacterIdsAsync(long equipmentId)
        {
            var rows = await SupabaseRest.GetAsync<RestCharacterEquipment>(
                $"character_equipment?equipment_id=eq.{equipmentId}&select=*");
            return rows.Select(r => r.CharacterId).ToList();
        }

        /// <summary>Personaggi a cui è assegnato questo oggetto, con la quantità posseduta da ciascuno.</summary>
        public static async Task<Dictionary<long, int>> GetAssignedCharacterQuantitiesAsync(long equipmentId)
        {
            var rows = await SupabaseRest.GetAsync<RestCharacterEquipment>(
                $"character_equipment?equipment_id=eq.{equipmentId}&select=*");
            return rows.ToDictionary(r => r.CharacterId, r => r.Quantity);
        }

        public static async Task<HashSet<long>> GetAssignedEquipmentIdsAsync(long characterId)
        {
            var rows = await SupabaseRest.GetAsync<RestCharacterEquipment>(
                $"character_equipment?character_id=eq.{characterId}&select=*");
            return rows.Select(r => r.EquipmentId).ToHashSet();
        }

        /// <summary>Oggetti assegnati a un personaggio, con la quantità posseduta di ciascuno.</summary>
        public static async Task<Dictionary<long, int>> GetAssignedQuantitiesAsync(long characterId)
        {
            var rows = await SupabaseRest.GetAsync<RestCharacterEquipment>(
                $"character_equipment?character_id=eq.{characterId}&select=*");
            return rows.ToDictionary(r => r.EquipmentId, r => r.Quantity);
        }

        public static async Task AssignAsync(long equipmentId, long characterId, int quantity = 1) =>
            await SupabaseRest.InsertRawAsync("character_equipment",
                new { equipment_id = equipmentId, character_id = characterId, quantity });

        public static async Task UpdateQuantityAsync(long equipmentId, long characterId, int quantity) =>
            await SupabaseRest.PatchWhereAsync("character_equipment",
                $"equipment_id=eq.{equipmentId}&character_id=eq.{characterId}", new { quantity });

        public static async Task UnassignAsync(long equipmentId, long characterId) =>
            await SupabaseRest.DeleteWhereAsync("character_equipment",
                $"equipment_id=eq.{equipmentId}&character_id=eq.{characterId}");

        private static Equipment ToLocal(RestEquipment r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            Type = r.Type,
            Cost = r.Cost,
            Weight = r.Weight,
            Description = r.Description,
            ImagePath = r.ImageUrl
        };
    }
}
