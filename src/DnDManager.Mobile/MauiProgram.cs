using DnDManager.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace DnDManager.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddSingleton<SessionStore>();
        builder.Services.AddSingleton<UiState>();
        builder.Services.AddSingleton<DeviceActions>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
