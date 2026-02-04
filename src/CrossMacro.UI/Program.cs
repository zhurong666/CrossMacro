using Avalonia;
using System;
using Avalonia.Media;
using Serilog;
using CrossMacro.Core.Logging;
using CrossMacro.Infrastructure.Services;

namespace CrossMacro.UI;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        try
        {
            // Load log level from settings before logger initialization
            var logLevel = SettingsService.TryLoadLogLevelEarly();
            
            // Initialize logger with user's preferred level
            LoggerSetup.Initialize(logLevel);

            Log.Information("Starting CrossMacro application");
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch (Exception ex)
        {
            // Try to log using Serilog if available
            try { Log.Fatal(ex, "Application terminated unexpectedly"); } catch { }

            // Emergency logging to disk in case Logger failed to initialize or write
            try 
            {
                var crashLogPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "crossmacro_crash.log");
                System.IO.File.AppendAllText(crashLogPath, 
                    $"[{DateTime.Now}] CRITICAL CRASH:\n{ex}\n\n=================================\n\n");
            }
            catch { /* Total failure, nothing we can do */ }
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .With(new FontManagerOptions
            {
                DefaultFamilyName = "avares://Avalonia.Fonts.Inter/Assets#Inter",
                FontFallbacks = 
                [
                    new FontFallback { FontFamily = new FontFamily("avares://Avalonia.Fonts.Inter/Assets#Inter") }
                ]
            });
}
