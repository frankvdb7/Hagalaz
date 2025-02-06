using System.Collections.Generic;

namespace Hagalaz.Services.GameWorld.Services.Model
{
    public record WorldInfoCacheDto(int Checksum, IList<WorldLocationInfo> LocationInfos, IList<WorldInfo> WorldInfos);
}
