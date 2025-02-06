using Hagalaz.Collections;
using Hagalaz.Services.GameLogon.Store.Model;

namespace Hagalaz.Services.GameLogon.Store
{
    public class CharacterStore : ConcurrentStore<uint, CharacterSessionContext>
    {
        public bool TryAdd(CharacterSessionContext sessionContext) => base.TryAdd(sessionContext.Id, sessionContext);
        public bool TryRemove(CharacterSessionContext sessionContext) => base.TryRemove(sessionContext.Id);
    }
}