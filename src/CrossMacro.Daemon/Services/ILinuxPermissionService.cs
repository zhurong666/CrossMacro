namespace CrossMacro.Daemon.Services;

public interface ILinuxPermissionService
{
    void ConfigureSocketPermissions(string socketPath);
}
