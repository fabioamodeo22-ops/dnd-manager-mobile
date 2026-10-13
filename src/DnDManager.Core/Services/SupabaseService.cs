using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DnDManager.Services
{
    /// <summary>Sessione Supabase serializzabile (salvata nel SecureStorage del telefono per restare loggati).</summary>
    public class AuthSession
    {
        [JsonPropertyName("access_token")] public string AccessToken { get; set; } = "";
        [JsonPropertyName("refresh_token")] public string RefreshToken { get; set; } = "";
        [JsonPropertyName("expires_at")] public long ExpiresAt { get; set; }
        [JsonPropertyName("user_id")] public string UserId { get; set; } = "";
        [JsonPropertyName("email")] public string Email { get; set; } = "";
    }

    /// <summary>
    /// Autenticazione Supabase (GoTrue) tramite chiamate HTTP dirette, senza SDK di terze parti:
    /// stessa API pubblica della versione WPF (SignInAsync, SignUpAsync, CurrentAccessToken...), più
    /// il rinnovo automatico del token e il ripristino della sessione all'avvio dell'app.
    /// </summary>
    public static class SupabaseService
    {
        private static readonly HttpClient Http = new();
        private static readonly SemaphoreSlim RefreshLock = new(1, 1);
        private static AuthSession? _session;

        /// <summary>Notifica ogni cambio di sessione (login, refresh, logout) per poterla persistere. Null = logout.</summary>
        public static event Action<AuthSession?>? SessionChanged;

        public static string? CurrentAccessToken => _session?.AccessToken;
        public static Guid? CurrentUserId => Guid.TryParse(_session?.UserId, out var id) ? id : null;
        public static string? CurrentEmail => _session?.Email;
        public static bool IsSignedIn => _session != null;

        /// <summary>Mantenuto per compatibilità con il codice WPF: qui non c'è nulla da inizializzare.</summary>
        public static Task EnsureInitializedAsync() => Task.CompletedTask;

        public static async Task<(bool Success, string? Error)> SignUpAsync(string email, string password, string username)
        {
            try
            {
                var body = new { email, password, data = new { username } };
                using var resp = await PostAuthAsync("signup", body);
                var json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return (false, ExtractError(json));

                // Se il progetto richiede la conferma email, la risposta non contiene un access_token
                using var doc = JsonDocument.Parse(json);
                if (!doc.RootElement.TryGetProperty("access_token", out _))
                    return (false, "Registrazione inviata. Se il progetto richiede la conferma email, controlla la posta e poi torna qui ad Accedere.");

                SetSession(ParseSession(doc.RootElement));
                return (true, null);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public static async Task<(bool Success, string? Error)> SignInAsync(string email, string password)
        {
            try
            {
                using var resp = await PostAuthAsync("token?grant_type=password", new { email, password });
                var json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                    return (false, resp.StatusCode == System.Net.HttpStatusCode.BadRequest
                        ? "Credenziali non valide, oppure l'email non è ancora stata confermata."
                        : ExtractError(json));

                using var doc = JsonDocument.Parse(json);
                SetSession(ParseSession(doc.RootElement));
                return (true, null);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        /// <summary>Ripristina una sessione salvata: la rinnova subito col refresh token (che potrebbe essere scaduto).</summary>
        public static async Task<bool> RestoreAsync(AuthSession saved)
        {
            _session = saved;
            var ok = await RefreshAsync(force: true);
            if (!ok) { _session = null; SessionChanged?.Invoke(null); }
            return ok;
        }

        /// <summary>Rinnova il token se scade entro un minuto. Chiamato prima di ogni richiesta.</summary>
        public static Task EnsureFreshTokenAsync() => RefreshAsync(force: false);

        /// <summary>Rinnova il token in ogni caso (usato dopo una risposta 401).</summary>
        public static Task<bool> ForceRefreshAsync() => RefreshAsync(force: true);

        private static async Task<bool> RefreshAsync(bool force)
        {
            if (_session == null) return false;
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            if (!force && _session.ExpiresAt - now > 60) return true;

            await RefreshLock.WaitAsync();
            try
            {
                if (_session == null) return false;
                now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (!force && _session.ExpiresAt - now > 60) return true; // già rinnovato da un'altra chiamata

                using var resp = await PostAuthAsync("token?grant_type=refresh_token", new { refresh_token = _session.RefreshToken });
                var json = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return false;

                using var doc = JsonDocument.Parse(json);
                SetSession(ParseSession(doc.RootElement));
                return true;
            }
            catch { return false; }
            finally { RefreshLock.Release(); }
        }

        public static void SignOut()
        {
            var token = _session?.AccessToken;
            _session = null;
            SessionChanged?.Invoke(null);
            if (string.IsNullOrEmpty(token)) return;

            // logout lato server "best effort", senza bloccare l'interfaccia
            _ = Task.Run(async () =>
            {
                try
                {
                    var req = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseConfig.Url}/auth/v1/logout");
                    req.Headers.Add("apikey", SupabaseConfig.AnonKey);
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    using var r = await Http.SendAsync(req);
                }
                catch { /* ignora */ }
            });
        }

        /// <summary>
        /// Elimina definitivamente l'account (richiesto da App Store e Google Play per le app con registrazione).
        /// Usa la funzione SQL "delete_my_account" (vedi supabase/delete_my_account.sql nel repository).
        /// </summary>
        public static async Task<(bool Success, string? Error)> DeleteAccountAsync()
        {
            try
            {
                await SupabaseRest.RpcRawAsync("delete_my_account", new { });
                _session = null;
                SessionChanged?.Invoke(null);
                return (true, null);
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        /// <summary>Traduce un nome utente nella sua email, per permettere l'accesso senza dover ricordare l'email.</summary>
        public static async Task<string?> ResolveEmailByUsernameAsync(string username)
        {
            try
            {
                var raw = await SupabaseRest.RpcRawAsync("get_email_by_username", new { uname = username });
                return JsonSerializer.Deserialize<string?>(raw);
            }
            catch { return null; }
        }

        // ------------------------------------------------------------------

        private static async Task<HttpResponseMessage> PostAuthAsync(string pathAndQuery, object body)
        {
            var req = new HttpRequestMessage(HttpMethod.Post, $"{SupabaseConfig.Url}/auth/v1/{pathAndQuery}");
            req.Headers.Add("apikey", SupabaseConfig.AnonKey);
            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
            return await Http.SendAsync(req);
        }

        private static AuthSession ParseSession(JsonElement root)
        {
            var s = new AuthSession
            {
                AccessToken = root.GetProperty("access_token").GetString() ?? "",
                RefreshToken = root.TryGetProperty("refresh_token", out var rt) ? rt.GetString() ?? "" : "",
            };

            if (root.TryGetProperty("expires_at", out var ea) && ea.ValueKind == JsonValueKind.Number)
                s.ExpiresAt = ea.GetInt64();
            else if (root.TryGetProperty("expires_in", out var ei) && ei.ValueKind == JsonValueKind.Number)
                s.ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + ei.GetInt64();
            else
                s.ExpiresAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 3600;

            if (root.TryGetProperty("user", out var user) && user.ValueKind == JsonValueKind.Object)
            {
                s.UserId = user.TryGetProperty("id", out var id) ? id.GetString() ?? "" : "";
                s.Email = user.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";
            }
            return s;
        }

        private static void SetSession(AuthSession s)
        {
            _session = s;
            SessionChanged?.Invoke(s);
        }

        private static string ExtractError(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                foreach (var key in new[] { "error_description", "msg", "message", "error" })
                    if (doc.RootElement.TryGetProperty(key, out var v) && v.ValueKind == JsonValueKind.String)
                        return v.GetString() ?? json;
            }
            catch { /* non JSON */ }
            return string.IsNullOrWhiteSpace(json) ? "Operazione non riuscita." : json;
        }
    }
}
