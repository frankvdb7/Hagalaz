using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.Skills
{
    /// <summary>
    ///     Contains skills features interface.
    /// </summary>
    public class FeaturesInterface : WidgetScript
    {
        /// <summary>
        ///     Contains menu Id.
        /// </summary>
        private int _menuID;

        /// <summary>
        ///     Contains submenu Id.
        /// </summary>
        private int _subMenuID;

        /// <summary>
        ///     Contains menu ids for skill ids.
        /// </summary>
        private static readonly int[] _menuIds = [1, 5, 2, 6, 3, 7, 4, 16, 18, 19, 15, 17, 11, 14, 13, 9, 8, 10, 20, 21, 12, 22, 23, 24, 25];

        public FeaturesInterface(ICharacterContextAccessor characterContextAccessor) : base(characterContextAccessor)
        {
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            for (var i = 0; i < 16; i++)
            {
                InterfaceInstance.AttachClickHandler(i + 9, SubmenuClick);
            }
        }

        /// <summary>
        ///     Refreshe's this interface.
        /// </summary>
        public void Refresh() => Owner.Configurations.SendStandardConfiguration(965, _menuID | (_subMenuID << 10));


        /// <summary>
        ///     Set's up this interface.
        /// </summary>
        /// <param name="skillID">Id of the skill which should be showed.</param>
        public void Setup(int skillID)
        {
            _menuID = _menuIds[skillID];
            _subMenuID = 0;
            Refresh();
        }

        /// <summary>
        ///     Happens when submenu is clicked.
        /// </summary>
        /// <returns></returns>
        private bool SubmenuClick(int componentID, ComponentClickType clickType, int extraData1, int extraData2)
        {
            if (clickType != ComponentClickType.LeftClick)
            {
                return false;
            }

            _subMenuID = componentID - 9;
            Refresh();
            return true;
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }
    }
}