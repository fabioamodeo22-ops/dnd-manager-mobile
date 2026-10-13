using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DnDManager.Services
{
    /// <summary>
    /// Chiamate dirette all'API REST di Supabase (PostgREST) tramite HttpClient. Stessa API pubblica
    /// della versione WPF; in più rinnova il token prima di ogni richiesta e ritenta una volta su 401.
    /// </summary>
    public static class SupabaseRest
    {
        private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private static HttpRequestMessage NewRequest(HttpMethod method, string pathAndQuery)
        {
            var req = new HttpRequestMessage(method, $"{SupabaseConfig.Url}/rest/v1/{pathAndQuery}");
            req.Headers.Add("apikey", SupabaseConfig.AnonKey);
            var token = SupabaseService.CurrentAccessToken;
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? SupabaseConfig.AnonKey);
            return req;
        }

        /// <summary>Esegue la richiesta (costruita da <paramref name="build"/>) e restituisce il corpo; eccezione se non 2xx.</summary>
        private static async Task<string> SendAsync(Func<HttpRequestMessage> build)
        {
            await SupabaseService.EnsureFreshTokenAsync();
            using var resp = await Http.SendAsync(build());
            var body = await resp.Content.ReadAsStringAsync();

            if (resp.StatusCode == HttpStatusCode.Unauthorized && SupabaseService.IsSignedIn)
            {
                // token scaduto nel frattempo (es. telefono in standby): rinnova e riprova una volta
                var restored = await SupabaseService.ForceRefreshAsync();
                if (restored)
                {
                    using var retry = await Http.SendAsync(build());
                    var retryBody = await retry.Content.ReadAsStringAsync();
                    if (!retry.IsSuccessStatusCode) throw new Exception($"{(int)retry.StatusCode}: {retryBody}");
                    return retryBody;
                }
            }

            if (!resp.IsSuccessStatusCode)
                throw new Exception($"{(int)resp.StatusCode}: {body}");
            return body;
        }

        private static StringContent JsonContent(object body) =>
            new(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        /// <summary>Chiama una funzione RPC di Supabase, restituendo il corpo grezzo della risposta.</summary>
        public static Task<string> RpcRawAsync(string functionName, object body) =>
            SendAsync(() =>
            {
                var req = NewRequest(HttpMethod.Post, $"rpc/{functionName}");
                req.Content = JsonContent(body);
                return req;
            });

        /// <summary>GET su una tabella. Esempio di query: "campaign_members?user_id=eq.xxxx&amp;select=*"</summary>
        public static Task<string> GetRawAsync(string tableAndQuery) =>
            SendAsync(() => NewRequest(HttpMethod.Get, tableAndQuery));

        public static async Task<List<T>> GetAsync<T>(string tableAndQuery)
        {
            var body = await GetRawAsync(tableAndQuery);
            return JsonSerializer.Deserialize<List<T>>(body, JsonOptions) ?? new();
        }

        /// <summary>INSERT su una tabella. Non chiede la riga di ritorno (evita falsi errori RLS sulla riga appena creata).</summary>
        public static Task<string> InsertRawAsync(string table, object body) =>
            SendAsync(() =>
            {
                var req = NewRequest(HttpMethod.Post, table);
                req.Headers.Add("Prefer", "return=minimal");
                req.Content = JsonContent(body);
                return req;
            });

        public static async Task<T?> InsertAsync<T>(string table, object body)
        {
            var respBody = await InsertRawAsync(table, body);
            if (string.IsNullOrWhiteSpace(respBody)) return default;
            var list = JsonSerializer.Deserialize<List<T>>(respBody, JsonOptions);
            return list != null && list.Count > 0 ? list[0] : default;
        }

        /// <summary>PATCH con un filtro libero (es. "campaign_id=eq.3").</summary>
        public static Task PatchWhereAsync(string table, string filterQuery, object body) =>
            SendAsync(() =>
            {
                var req = NewRequest(HttpMethod.Patch, $"{table}?{filterQuery}");
                req.Headers.Add("Prefer", "return=minimal");
                req.Content = JsonContent(body);
                return req;
            });

        /// <summary>UPDATE di una riga tramite il suo id (colonna "id").</summary>
        public static Task UpdateAsync(string table, long id, object body) => PatchWhereAsync(table, $"id=eq.{id}", body);

        /// <summary>DELETE con un filtro libero (es. "equipment_id=eq.3&amp;character_id=eq.7").</summary>
        public static Task DeleteWhereAsync(string table, string filterQuery) =>
            SendAsync(() =>
            {
                var req = NewRequest(HttpMethod.Delete, $"{table}?{filterQuery}");
                req.Headers.Add("Prefer", "return=minimal");
                return req;
            });

        /// <summary>DELETE di una riga tramite il suo id (colonna "id").</summary>
        public static Task DeleteAsync(string table, long id) => DeleteWhereAsync(table, $"id=eq.{id}");
    }
}
