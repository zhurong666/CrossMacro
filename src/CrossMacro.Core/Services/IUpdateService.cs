using System;
using System.Threading.Tasks;

namespace CrossMacro.Core.Services;

public interface IUpdateService
{
    Task<UpdateCheckResult> CheckForUpdatesAsync();
}

public class UpdateCheckResult
{
    public bool HasUpdate { get; set; }
    public string LatestVersion { get; set; } = string.Empty;
    public string ReleaseUrl { get; set; } = string.Empty;
}
