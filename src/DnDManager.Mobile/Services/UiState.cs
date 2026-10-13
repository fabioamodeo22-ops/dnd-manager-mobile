namespace DnDManager.Mobile.Services;

/// <summary>Stato condiviso dell'interfaccia: titolo della barra in alto e messaggi "toast".</summary>
public class UiState
{
    public string Title { get; private set; } = "Grimorio del Master";
    public string? Toast { get; private set; }
    public bool ToastIsError { get; private set; }

    public event Action? Changed;

    public void SetTitle(string title)
    {
        if (Title == title) return;
        Title = title;
        Changed?.Invoke();
    }

    public async void ShowToast(string message, bool isError = false)
    {
        Toast = message;
        ToastIsError = isError;
        Changed?.Invoke();
        await Task.Delay(isError ? 4500 : 2200);
        if (Toast == message)
        {
            Toast = null;
            Changed?.Invoke();
        }
    }

    public void Error(Exception ex) => ShowToast(Friendly(ex), isError: true);

    /// <summary>Messaggi d'errore comprensibili per gli errori più comuni di rete/permessi.</summary>
    public static string Friendly(Exception ex)
    {
        var msg = ex.Message;
        if (ex is HttpRequestException || ex is TaskCanceledException)
            return "Connessione assente o lenta. Riprova.";
        if (msg.StartsWith("401")) return "Sessione scaduta: esci e accedi di nuovo.";
        if (msg.StartsWith("403") || msg.Contains("row-level security", StringComparison.OrdinalIgnoreCase))
            return "Non hai i permessi per questa operazione.";
        return msg.Length > 220 ? msg[..220] + "…" : msg;
    }
}
