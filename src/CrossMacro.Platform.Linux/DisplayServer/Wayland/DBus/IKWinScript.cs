using System.Threading.Tasks;
using Tmds.DBus;

namespace CrossMacro.Platform.Linux.DisplayServer.Wayland.DBus;

[DBusInterface("org.kde.kwin.Script")]
public interface IKWinScript : IDBusObject
{
    Task runAsync();
    Task stopAsync();
}
