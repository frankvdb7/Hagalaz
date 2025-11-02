using System.Collections.Generic;
using System.Threading.Tasks;
using Hagalaz.Cache;
using Hagalaz.Cache.Abstractions;
using Hagalaz.Cache.Abstractions.Logic;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Services.GameWorld.Data.Model;

namespace Hagalaz.Services.GameWorld.Data
{
    /// <summary>
    /// 
    /// </summary>
    public class QuestManager : IQuestRepository
    {
        private readonly ICacheAPI _cacheApi;
        private List<QuestDefinition> _loadedQuestDefinitions;

        /// <summary>
        /// 
        /// </summary>
        public QuestManager(ICacheAPI cacheApi) => _cacheApi = cacheApi;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public Task LoadAsync()
        {
            return Task.CompletedTask;
        }
    }
}