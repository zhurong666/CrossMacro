using System;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using CrossMacro.Core.Models;
using CrossMacro.Core.Services;
using Serilog;
using CrossMacro.Infrastructure.Serialization;

namespace CrossMacro.Infrastructure.Services;

public class HotkeyConfigurationService : IHotkeyConfigurationService
{
    private readonly string _configPath;

    public HotkeyConfigurationService() : this(null)
    {
    }

    public HotkeyConfigurationService(string? configRootPath)
    {
        if (string.IsNullOrEmpty(configRootPath))
        {
            var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            
            if (string.IsNullOrEmpty(xdgConfigHome))
            {
                xdgConfigHome = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
            }
            configRootPath = Path.Combine(xdgConfigHome, "crossmacro");
        }

        if (!Directory.Exists(configRootPath))
        {
            Directory.CreateDirectory(configRootPath);
        }

        _configPath = Path.Combine(configRootPath, "hotkeys.json");
    }

    public HotkeySettings Load()
    {
        try
        {
            if (File.Exists(_configPath))
            {
                var json = File.ReadAllText(_configPath);
                var settings = JsonSerializer.Deserialize(json, CrossMacroJsonContext.Default.HotkeySettings);
                if (settings != null)
                {
                    Log.Information("Loaded hotkey configuration from {Path}", _configPath);
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load hotkey configuration from {Path}", _configPath);
        }

        Log.Information("Using default hotkey configuration");
        return new HotkeySettings();
    }

    public async Task<HotkeySettings> LoadAsync()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                Log.Information("Using default hotkey configuration");
                return new HotkeySettings();
            }

            var json = await File.ReadAllTextAsync(_configPath);
            var settings = JsonSerializer.Deserialize(json, CrossMacroJsonContext.Default.HotkeySettings);
            if (settings != null)
            {
                Log.Information("Loaded hotkey configuration from {Path}", _configPath);
                return settings;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load hotkey configuration from {Path}", _configPath);
        }

        Log.Information("Using default hotkey configuration");
        return new HotkeySettings();
    }

    public void Save(HotkeySettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, CrossMacroJsonContext.Default.HotkeySettings);
            File.WriteAllText(_configPath, json);
            Log.Information("Saved hotkey configuration to {Path}", _configPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save hotkey configuration to {Path}", _configPath);
        }
    }
}
