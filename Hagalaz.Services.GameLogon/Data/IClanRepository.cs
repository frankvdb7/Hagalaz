using Hagalaz.Data.Entities;

namespace Hagalaz.Services.GameLogon.Data
{
    public interface IClanRepository
    {
        Clan FindByName(string clanName);
    }
}