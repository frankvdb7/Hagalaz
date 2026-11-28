
namespace Hagalaz.Services.GameWorld.Providers
{
    public interface IClientPermissionProvider
    {
        int GetClientPermission(IList<string> roles);
    }
}