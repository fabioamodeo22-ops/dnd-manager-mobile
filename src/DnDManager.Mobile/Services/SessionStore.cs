using System.Text.Json;
using DnDManager.Data;
using DnDManager.Services;

namespace DnDManager.Mobile.Services;

/// <summary>
/// Persiste la sessione Supabase nel portachiavi del telefono (Keychain su iOS, Keystore su Android)
/// così l'utente resta collegato tra un avvio e l'altro; ricorda anche l'ultima campagna aperta.
/// </summary>
public class SessionStore
{
    private const string SessionKey = "supabase_session";
    private const string LastCampaignKey = "last_campaign";

    public SessionStore()
    {
        SupabaseService.SessionChanged += OnSessionChanged;
    }

    private static async void OnSessionChanged(AuthSession? session)
    {
        try
        {
            if (session == null) SecureStorage.Default.Remove(SessionKey);
            else await SecureStorage.Default.SetAsync(SessionKey, JsonSerializer.Serialize(session));
        }
        catch { /* il portachiavi può non essere disponibile (es. emulatore senza lock screen): si rifà il login */ }
    }

    /// <summary>Prova a ripristinare la sessione salvata. True se l'utente risulta di nuovo autenticato.</summary>
    public async Task<bool> TryRestoreAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(SessionKey);
            if (string.IsNullOrEmpty(json)) return false;
            var saved = JsonSerializer.Deserialize<AuthSession>(json);
            if (saved == null || string.IsNullOrEmpty(saved.RefreshToken)) return false;

            if (!await SupabaseService.RestoreAsync(saved)) return false;
            await LoadIdentityAsync();
            return true;
        }
        catch { return false; }
    }

    /// <summary>Valorizza AppSession dopo login/ripristino (id, email, username).</summary>
    public async Task LoadIdentityAsync(string? knownUsername = null)
    {
        AppSession.UserId = SupabaseService.CurrentUserId;
        AppSession.Email = SupabaseService.CurrentEmail ?? "";
        AppSession.UserName = knownUsername ?? await RemoteCampaigns.GetMyUsernameAsync() ?? AppSession.Email;
    }

    public void RememberCampaign(CampaignMembership m) =>
        Preferences.Default.Set(LastCampaignKey, $"{m.CampaignId}|{m.Role}|{m.Name}");

    public CampaignMembership? LastCampaign()
    {
        var raw = Preferences.Default.Get(LastCampaignKey, "");
        var parts = raw.Split('|', 3);
        if (parts.Length != 3 || !long.TryParse(parts[0], out var id)) return null;
        return new CampaignMembership { CampaignId = id, Role = parts[1], Name = parts[2] };
    }

    public void ForgetCampaign() => Preferences.Default.Remove(LastCampaignKey);

    public static void EnterCampaign(CampaignMembership m)
    {
        AppSession.CampaignId = m.CampaignId;
        AppSession.CampaignName = m.Name;
        AppSession.IsMaster = m.IsMaster;
    }
}
