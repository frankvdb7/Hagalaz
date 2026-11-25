using Hagalaz.Game.Scripts.Model.Creatures.Npcs;

namespace Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.SkillCapeDialogue
{
    /// <summary>
    ///     An NPC who sells the skill cape of one skill: clicking it with Option 1 opens the skill cape dialogue.
    /// </summary>
    public abstract class SkillCapeMasterScript(
        INpc owner,
        INpcService npcService,
        ISimplePathFinder pathFinder,
        IWidgetScriptActivator widgetScriptActivator,
        int skillId) : DialogueNpcScript<SkillCapeDialogue>(owner, npcService, pathFinder, widgetScriptActivator)
    {
        /// <inheritdoc />
        protected override void Configure(SkillCapeDialogue dialogue) => dialogue.SkillID = skillId;
    }
}
