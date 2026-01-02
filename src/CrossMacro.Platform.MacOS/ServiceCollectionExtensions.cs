using CrossMacro.Core.Services;
using CrossMacro.Platform.MacOS.Services;
using Microsoft.Extensions.DependencyInjection;

using System.Runtime.Versioning;

namespace CrossMacro.Platform.MacOS;

public static class ServiceCollectionExtensions
{
    [SupportedOSPlatform("macos")]
    public static IServiceCollection AddMacOSServices(this IServiceCollection services)
    {
        services.AddTransient<IInputCapture, MacOSInputCapture>();
        services.AddTransient<IInputSimulator, MacOSInputSimulator>();
        services.AddSingleton<IMousePositionProvider, MacOSMousePositionProvider>();
        services.AddSingleton<IPermissionChecker, MacOSPermissionCheckerService>();
        return services;
    }
}
