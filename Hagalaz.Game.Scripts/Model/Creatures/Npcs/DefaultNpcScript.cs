namespace Hagalaz.Game.Scripts.Model.Creatures.Npcs
{
    /// <summary>
    /// Default script for npc.
    /// </summary>
    public class DefaultNpcScript : NpcScriptBase
    {
        public DefaultNpcScript(INpc owner, INpcService npcService, ISimplePathFinder pathFinder, IWidgetScriptActivator widgetScriptActivator)
            : base(owner, npcService, pathFinder, widgetScriptActivator)
        {
        }
    }
}
