using Microsoft.Extensions.DependencyInjection;

namespace CrossMacro.UI.DependencyInjection;

/// <summary>
/// Platform-specific service registration interface.
/// Each platform implements this to register its specific services.
/// </summary>
public interface IPlatformServiceRegistrar
{
    /// <summary>
    /// Register platform-specific services (Input capture, simulator, position provider, etc.)
    /// </summary>
    void RegisterPlatformServices(IServiceCollection services);
}
