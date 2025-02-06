using Hagalaz.Game.Abstractions.Model.Quests;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Services.GameWorld.Services.Model;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Services.GameWorld.Providers
{
    /// <summary>
    /// 
    /// </summary>
    public class QuestScriptProvider : IQuestScriptProvider
    {
        private readonly ILogger<QuestScriptProvider> _logger;
        private string[] _loadedQuestScripts;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        public QuestScriptProvider(ILogger<QuestScriptProvider> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Loads the quest scripts.
        /// </summary>
        public void LoadQuestScripts()
        {
            _loadedQuestScripts = new string[short.MaxValue];

            //            var questScripts = _scriptsActivator.CreateInstance<QuestScript>().ToList();
            //            foreach (var script in questScripts)
            //            {
            //                var questID = script.GetQuestId();
            //                if (_loadedQuestScripts[questID] != null)
            //                    _logger.LogWarning("Duplicate Quest Script [" + questID + "," + script.GetType().FullName + "]");
            //                _loadedQuestScripts[questID] = script.GetType().FullName;
            //            }

            //_logger.LogInformation("Loaded " + questScripts.Count + " quest scripts!");
        }

        /// <summary>
        /// Make's questscript for specific quest.
        /// </summary>
        /// <param name="quest">The quest.</param>
        /// <returns>
        /// QuestScript.
        /// </returns>
        public IQuestScript? MakeQuestScript(Quest quest)
        {
            IQuestScript sc;
            //            if (quest.Definition.Id <= 0 || quest.Definition.Id >= _loadedQuestScripts.Length || _loadedQuestScripts[quest.Definition.Id] == null)
            //                sc = new DefaultQuestScript(quest.Definition.Id);
            //            else
            //                sc = _scriptsActivator.CreateInstance<QuestScript>(_loadedQuestScripts[quest.Definition.Id]);
            //sc.Initialize(quest, quest.Owner);
            return null;
        }
    }
}