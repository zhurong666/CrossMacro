using System.Threading.Tasks;
using Tmds.DBus;

namespace CrossMacro.Platform.Linux.DisplayServer.Wayland.DBus;

[DBusInterface("org.kde.kwin.Scripting")]
public interface IKWinScripting : IDBusObject
{
    Task<int> loadScriptAsync(string filePath);
    Task unloadScriptAsync(string scriptName);
}
