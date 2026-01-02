using System.Threading.Tasks;

namespace CrossMacro.Core.Services;

public interface IPermissionChecker
{
    bool IsSupported { get; }
    bool IsAccessibilityTrusted();
    bool CheckUInputAccess();
    void OpenAccessibilitySettings();
}
