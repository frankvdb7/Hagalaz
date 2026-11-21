using Hagalaz.Game.Abstractions.Model.GameObjects;

namespace Hagalaz.Game.Scripts.GameObjects.Altars
{
    [GameObjectScriptMetaData([17010])]
    public class LunarAltar : SwitchBookAltar<SwitchLunarBookDialogue>
    {
        protected override GameObjectClickType SwitchClick => GameObjectClickType.Option2Click;
    }
}
