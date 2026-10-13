using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DnDManager.Data;
using DnDManager.Data.Remote;
using DnDManager.Models;

namespace DnDManager.Services
{
    /// <summary>
    /// Stato della battaglia condiviso per la campagna corrente, su Supabase. L'app lo aggiorna subito
    /// dopo ogni azione locale (ottimistico) e lo ricontrolla periodicamente (polling, non websocket)
    /// per riflettere le mosse fatte da altri PC — scelta più robusta rispetto al push in tempo reale,
    /// dati i problemi di affidabilità incontrati con il client ufficiale in questo ambiente.
    /// </summary>
    public static class RemoteBattle
    {
        public static async Task<RestBattleState> EnsureStateAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestBattleState>(
                $"battle_state?campaign_id=eq.{AppSession.CampaignId}&select=*");
            var existing = rows.FirstOrDefault();
            if (existing != null) return existing;

            var body = new
            {
                campaign_id = AppSession.CampaignId,
                cols = 24, rows = 16,
                walls_json = "[]", fog_json = "[]"
            };
            var created = await SupabaseRest.InsertAsync<RestBattleState>("battle_state", body);
            if (created != null) return created;

            rows = await SupabaseRest.GetAsync<RestBattleState>(
                $"battle_state?campaign_id=eq.{AppSession.CampaignId}&select=*");
            return rows.First();
        }

        public static async Task UpdateStateAsync(string? mapUrl, int cols, int rows, bool[,] walls, bool[,] fog)
        {
            var body = new Dictionary<string, object?>
            {
                ["map_url"] = mapUrl,
                ["cols"] = cols,
                ["rows"] = rows,
                ["walls_json"] = SerializeGrid(walls),
                ["fog_json"] = SerializeGrid(fog)
            };
            await SupabaseRest.PatchWhereAsync("battle_state", $"campaign_id=eq.{AppSession.CampaignId}", body);
        }

        public static bool[,] DeserializeGrid(string json, int rows, int cols)
        {
            var result = new bool[Math.Max(1, rows), Math.Max(1, cols)];
            try
            {
                var jagged = JsonSerializer.Deserialize<bool[][]>(json);
                if (jagged == null) return result;
                for (int y = 0; y < rows && y < jagged.Length; y++)
                    for (int x = 0; x < cols && x < jagged[y].Length; x++)
                        result[y, x] = jagged[y][x];
            }
            catch { /* griglia vuota o non valida: resta tutta a false */ }
            return result;
        }

        private static string SerializeGrid(bool[,] grid)
        {
            int rows = grid.GetLength(0), cols = grid.GetLength(1);
            var jagged = new bool[rows][];
            for (int y = 0; y < rows; y++)
            {
                jagged[y] = new bool[cols];
                for (int x = 0; x < cols; x++) jagged[y][x] = grid[y, x];
            }
            return JsonSerializer.Serialize(jagged);
        }

        // ---------------- Combattenti ----------------
        public static async Task<List<Combatant>> GetCombatantsAsync()
        {
            var rows = await SupabaseRest.GetAsync<RestBattleCombatant>(
                $"battle_combatants?campaign_id=eq.{AppSession.CampaignId}&select=*&order=sort_order");
            return rows.Select(ToLocal).ToList();
        }

        public static async Task<long> AddCombatantAsync(Combatant c)
        {
            var body = new Dictionary<string, object?>
            {
                ["campaign_id"] = AppSession.CampaignId,
                ["name"] = c.Name,
                ["initiative"] = c.Initiative,
                ["hp"] = c.Hp,
                ["is_current"] = c.IsCurrent,
                ["portrait_url"] = c.PortraitPath,
                ["character_id"] = c.CharacterId,
                ["grid_x"] = c.GridX,
                ["grid_y"] = c.GridY,
                ["sort_order"] = c.Initiative * -1
            };
            var created = await SupabaseRest.InsertAsync<RestBattleCombatant>("battle_combatants", body);
            return created?.Id ?? 0;
        }

        public static async Task UpdateCombatantAsync(Combatant c)
        {
            if (c.Id == 0) return;
            var body = new Dictionary<string, object?>
            {
                ["name"] = c.Name,
                ["initiative"] = c.Initiative,
                ["hp"] = c.Hp,
                ["is_current"] = c.IsCurrent,
                ["grid_x"] = c.GridX,
                ["grid_y"] = c.GridY,
                ["sort_order"] = c.Initiative * -1
            };
            await SupabaseRest.UpdateAsync("battle_combatants", c.Id, body);
        }

        /// <summary>Aggiorna solo la posizione (usato quando si trascina un token: più leggero/veloce).</summary>
        public static async Task UpdatePositionAsync(long combatantId, int gridX, int gridY)
        {
            if (combatantId == 0) return;
            await SupabaseRest.UpdateAsync("battle_combatants", combatantId, new { grid_x = gridX, grid_y = gridY });
        }

        public static async Task DeleteCombatantAsync(long id) => await SupabaseRest.DeleteAsync("battle_combatants", id);

        public static async Task ClearCombatantsAsync() =>
            await SupabaseRest.DeleteWhereAsync("battle_combatants", $"campaign_id=eq.{AppSession.CampaignId}");

        private static Combatant ToLocal(RestBattleCombatant r) => new()
        {
            Id = r.Id,
            Name = r.Name,
            Initiative = r.Initiative,
            Hp = r.Hp,
            IsCurrent = r.IsCurrent,
            PortraitPath = r.PortraitUrl,
            CharacterId = r.CharacterId,
            GridX = r.GridX,
            GridY = r.GridY
        };
    }
}
