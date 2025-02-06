using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.CapeDesign
{
    /// <summary>
    ///     Contains the cape designer script.
    /// </summary>
    public class CustomizeInterfaceScript : WidgetScript
    {
        public CustomizeInterfaceScript(
            ICharacterContextAccessor characterContextAccessor, IItemService itemService, IItemPartFactory itemPartFactory) : base(characterContextAccessor)
        {
            _itemRepository = itemService;
            _itemPartFactory = itemPartFactory;
        }

        /// <summary>
        ///     Id of the item that this interface script will customize.
        /// </summary>
        public IItem ToCustomize { get; set; }

        /// <summary>
        ///     The item manager
        /// </summary>
        private readonly IItemService _itemRepository;

        private readonly IItemPartFactory _itemPartFactory;

        /// <summary>
        ///     Called when [close].
        /// </summary>
        public override void OnClose() { }

        /// <summary>
        ///     Called when [open].
        /// </summary>
        public override void OnOpen() => Setup();

        /// <summary>
        ///     Setups this instance.
        /// </summary>
        public void Setup()
        {
            InterfaceInstance.DrawModel(55,
                Owner.Appearance.Female
                    ? _itemRepository.FindItemDefinitionById(ToCustomize.Id).FemaleWornModelId1
                    : _itemRepository.FindItemDefinitionById(ToCustomize.Id).MaleWornModelId1);

            Refresh();

            InterfaceInstance.AttachClickHandler(58,
                (componentID, type, extraInfo1, extraInfo2) =>
                {
                    if (type == ComponentClickType.LeftClick)
                    {
                        var capePart = _itemPartFactory.Create(ToCustomize.Id);
                        Owner.Appearance.SetItemPart(ToCustomize.Id, capePart);
                        var hatPart = _itemPartFactory.Create(ToCustomize.Id + 1);
                        Owner.Appearance.SetItemPart(ToCustomize.Id + 1, hatPart);
                        Refresh();
                        return true;
                    }

                    return false;
                });

            InterfaceInstance.AttachClickHandler(34,
                (componentID, type, extraInfo1, extraInfo2) =>
                {
                    if (type == ComponentClickType.LeftClick)
                    {
                        var colorScript = Owner.ServiceProvider.GetRequiredService<ColorInterfaceScript>();
                        colorScript.ToCustomize = ToCustomize;
                        colorScript.Part = 0;
                        Owner.Widgets.OpenWidget(19, 0, colorScript, false);
                        Owner.Configurations.SendStandardConfiguration(2174, Owner.Appearance.GetOrAddItemPart(ToCustomize.Id).ModelColors[0]);
                        return true;
                    }

                    return false;
                });

            InterfaceInstance.AttachClickHandler(71,
                (componentID, type, extraInfo1, extraInfo2) =>
                {
                    if (type == ComponentClickType.LeftClick)
                    {
                        var colorScript = Owner.ServiceProvider.GetRequiredService<ColorInterfaceScript>();
                        colorScript.ToCustomize = ToCustomize;
                        colorScript.Part = 1;
                        Owner.Widgets.OpenWidget(19, 0, colorScript, false);
                        Owner.Configurations.SendStandardConfiguration(2174, Owner.Appearance.GetOrAddItemPart(ToCustomize.Id).ModelColors[1]);
                        return true;
                    }

                    return false;
                });

            InterfaceInstance.AttachClickHandler(83,
                (componentID, type, extraInfo1, extraInfo2) =>
                {
                    if (type == ComponentClickType.LeftClick)
                    {
                        var colorScript = Owner.ServiceProvider.GetRequiredService<ColorInterfaceScript>();
                        colorScript.ToCustomize = ToCustomize;
                        colorScript.Part = 2;
                        Owner.Widgets.OpenWidget(19, 0, colorScript, false);
                        Owner.Configurations.SendStandardConfiguration(2174, Owner.Appearance.GetOrAddItemPart(ToCustomize.Id).ModelColors[2]);
                        return true;
                    }

                    return false;
                });

            InterfaceInstance.AttachClickHandler(95,
                (componentID, type, extraInfo1, extraInfo2) =>
                {
                    if (type == ComponentClickType.LeftClick)
                    {
                        var colorScript = Owner.ServiceProvider.GetRequiredService<ColorInterfaceScript>();
                        colorScript.ToCustomize = ToCustomize;
                        colorScript.Part = 3;
                        Owner.Widgets.OpenWidget(19, 0, colorScript, false);
                        Owner.Configurations.SendStandardConfiguration(2174, Owner.Appearance.GetOrAddItemPart(ToCustomize.Id).ModelColors[3]);
                        return true;
                    }

                    return false;
                });

            OnComponentClick finish = (componentID, type, extraInfo1, extraInfo2) =>
            {
                if (type == ComponentClickType.LeftClick)
                {
                    Owner.Appearance.DrawCharacter();
                    Owner.Appearance.Refresh();
                    Owner.Widgets.CloseWidget(InterfaceInstance);
                    return true;
                }

                return false;
            };
            InterfaceInstance.AttachClickHandler(114, finish);
            InterfaceInstance.AttachClickHandler(142, finish);
        }

        /// <summary>
        ///     Refreshes this instance.
        /// </summary>
        public void Refresh()
        {
            var capeColors = Owner.Appearance.GetOrAddItemPart(ToCustomize.Id).ModelColors;

            Owner.Configurations.SendStandardConfiguration(2172, capeColors[1] << 16 | capeColors[0]);
            //int colors2 = capeColors[2];
            Owner.Configurations.SendStandardConfiguration(2173, capeColors[3] << 16 | capeColors[2]);
        }
    }
}