using Microsoft.Extensions.Logging;

using MudBlazor;
using MudBlazor.Services;

using ProjectDoxen.Manager;

namespace ProjectDoxen;


public static class MauiProgram
{
  public static MauiApp CreateMauiApp()
  {

    var builder = MauiApp.CreateBuilder();
    builder
      .UseMauiApp<App>()
      .ConfigureFonts(fonts =>
      {
        fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
      });

    builder.Services.AddMauiBlazorWebView();
    builder.Services.AddMudServices();
    builder.Services.AddSingleton<CryptoService>();
    builder.Services.AddSingleton<SimpleCredentialManager>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

    return builder.Build();
  }
}
