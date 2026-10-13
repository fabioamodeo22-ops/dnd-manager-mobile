namespace DnDManager.Mobile.Services;

/// <summary>File scelto dall'utente, già letto in memoria.</summary>
public record PickedFile(string FileName, byte[] Bytes);

/// <summary>Funzioni native del telefono usate dalle pagine Blazor: dialoghi, apertura file/URL, scelta file.</summary>
public class DeviceActions
{
    private static Page? CurrentPage => Application.Current?.Windows.FirstOrDefault()?.Page;

    public Task<bool> ConfirmAsync(string title, string message, string accept = "Sì", string cancel = "Annulla") =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = CurrentPage;
            if (page == null) return false;
            return await page.DisplayAlertAsync(title, message, accept, cancel);
        });

    public Task AlertAsync(string title, string message) =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = CurrentPage;
            if (page != null) await page.DisplayAlertAsync(title, message, "OK");
        });

    public Task<string?> PromptAsync(string title, string message, string initial = "") =>
        MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = CurrentPage;
            if (page == null) return null;
            return await page.DisplayPromptAsync(title, message, "OK", "Annulla", initialValue: initial);
        });

    /// <summary>Apre un URL (PDF, audio, documento, pagina web) con l'app predefinita del telefono.</summary>
    public async Task OpenUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        try
        {
            if (!await Launcher.Default.TryOpenAsync(new Uri(url)))
                await Browser.Default.OpenAsync(new Uri(url), BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception ex)
        {
            await AlertAsync("Impossibile aprire", ex.Message);
        }
    }

    public Task<PickedFile?> PickImageAsync() => PickAsync(PickOptions.Images);

    public Task<PickedFile?> PickAnyFileAsync() => PickAsync(PickOptions.Default);

    private static async Task<PickedFile?> PickAsync(PickOptions options)
    {
        var result = await MainThread.InvokeOnMainThreadAsync(() => FilePicker.Default.PickAsync(options));
        if (result == null) return null;

        await using var stream = await result.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return new PickedFile(result.FileName, ms.ToArray());
    }
}
