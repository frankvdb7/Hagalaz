using System;
using System.Linq;
using Hagalaz.Game.Abstractions.Builders.Item;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Common.Tasks;
using Hagalaz.Game.Scripts.Areas.Edgeville.Npcs.Sage;
using Hagalaz.Game.Scripts.Characters;
using Hagalaz.Game.Scripts.Model.Widgets;

namespace Hagalaz.Game.Scripts.Widgets.CharacterDesign
{
    /// <summary>
    ///     Represents account design interface.
    /// </summary>
    public class DesignInterface : WidgetScript
    {
        /// <summary>
        ///     Contains selected style Id.
        /// </summary>
        private int _styleID;

        /// <summary>
        ///     Contains selected substyle Id.
        /// </summary>
        private int _substyleID;

        /// <summary>
        ///     Contains selected advanced selector.
        /// </summary>
        private int _advancedSelectorID;

        /// <summary>
        ///     Contains Id of the step.
        ///     First step is body style choice.
        ///     Second step is head style choice.
        ///     Third step is name choice.
        /// </summary>
        private int _step;

        /// <summary>
        ///     Contains step button unEquipHandler.
        /// </summary>
        private OnComponentClick _stepButtonHandler;

        /// <summary>
        ///     Contains style button unEquipHandler.
        /// </summary>
        private OnComponentClick _styleButtonHandler;

        /// <summary>
        ///     Contains gender button unEquipHandler.
        /// </summary>
        private OnComponentClick _genderButtonHandler;

        /// <summary>
        ///     Contains substyle button unEquipHandler.
        /// </summary>
        private OnComponentClick _substyleButtonHandler;

        /// <summary>
        ///     Contains confirm button unEquipHandler.
        /// </summary>
        private OnComponentClick _confirmClick;

        /// <summary>
        ///     Contains name confirm button unEquipHandler.
        /// </summary>
        private OnComponentClick _nameConfirmClick;

        /// <summary>
        ///     Contains suggestion button unEquipHandler.
        /// </summary>
        private OnComponentClick _suggestionClick;

        /// <summary>
        ///     Contains skinChooser button unEquipHandler.
        /// </summary>
        private OnComponentClick _skinChooserClick;

        /// <summary>
        ///     Contains designerChooser button unEquipHandler.
        /// </summary>
        private OnComponentClick _designerChooserClick;

        /// <summary>
        ///     Contains designerPallete button unEquipHandler.
        /// </summary>
        private OnComponentClick _designerPalleteClick;

        /// <summary>
        ///     Contains designerPanel button unEquipHandler.
        /// </summary>
        private OnComponentClick _designerPanelClick;

        /// <summary>
        ///     Contains designerRandomizerclick button unEquipHandler.
        /// </summary>
        private OnComponentClick _designerRandomizerClick;

        /// <summary>
        ///     Contains suggestion table.
        /// </summary>
        private static readonly string[] _suggestionTable =
        [
            "pkgod", "sun", "moon", "darkness", "light", "power", "magic", "pwnage"
        ];

        /// <summary>
        ///     Contains name set callback.
        /// </summary>
        private EventHappened? _nameSetCallback;

        /// <summary>
        ///     Contains last name entered.
        /// </summary>
        private string _lastName;

        private readonly IGameMessageService _gameMessageService;
        private readonly ICharacterCreateInfoRepository _characterCreateInfoRepository;
        private readonly IItemBuilder _itemBuilder;

        public DesignInterface(
            ICharacterContextAccessor characterContextAccessor, IGameMessageService gameMessageService,
            ICharacterCreateInfoRepository characterCreateInfoRepository, IItemBuilder itemBuilder)
            : base(characterContextAccessor)
        {
            _gameMessageService = gameMessageService;
            _characterCreateInfoRepository = characterCreateInfoRepository;
            _itemBuilder = itemBuilder;
        }

        /// <summary>
        ///     Get's called when Script instance is created.
        /// </summary>
        protected override void Initialize()
        {
            _stepButtonHandler = StepButtonClicked;
            _styleButtonHandler = StyleButtonClicked;
            _genderButtonHandler = GenderButtonClicked;
            _substyleButtonHandler = SubStyleButtonClicked;
            _confirmClick = ConfirmButtonClicked;
            _nameConfirmClick = AccountConfirmButtonClicked;
            _suggestionClick = SuggestionButtonClicked;
            _skinChooserClick = SkinChooserClicked;
            _designerChooserClick = DesignerChooserClicked;
            _designerPalleteClick = DesignerPalleteClicked;
            _designerPanelClick = DesignerPanelClicked;
            _designerRandomizerClick = ColorRandomizerButtonClicked;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            SetupInterface();
            SetupHandlers();
            Randomize();
            RefreshGender();
            Refresh();
        }

        /// <summary>
        ///     Set's handlers up.
        /// </summary>
        public void SetupHandlers()
        {
            InterfaceInstance.AttachClickHandler(139, _stepButtonHandler); // continue
            InterfaceInstance.AttachClickHandler(137, _stepButtonHandler); // back
            for (var i = 68; i < 75; i++)
            {
                InterfaceInstance.AttachClickHandler(i, _styleButtonHandler);
            }

            for (var i = 103; i < 106; i++)
            {
                InterfaceInstance.AttachClickHandler(i, _substyleButtonHandler);
            }

            /*for (int i = 158; i < 161; i++)
                this.interfaceInstance.AttachClickHandler((short)i, this.suggestionClick);*/
            for (var i = 116; i < 122; i++)
            {
                InterfaceInstance.AttachClickHandler(i, _designerChooserClick);
            }

            InterfaceInstance.AttachClickHandler(123, _designerRandomizerClick);
            InterfaceInstance.AttachClickHandler(128, _designerPanelClick);
            InterfaceInstance.AttachClickHandler(132, _designerPalleteClick);
            /*this.interfaceInstance.AttachClickHandler(165, this.suggestionClick);*/
            InterfaceInstance.AttachClickHandler(62, _genderButtonHandler); // male
            InterfaceInstance.AttachClickHandler(63, _genderButtonHandler); // female
            InterfaceInstance.AttachClickHandler(138, _confirmClick); // confirm
            InterfaceInstance.AttachClickHandler(182, _nameConfirmClick); // name confirm
            InterfaceInstance.AttachClickHandler(65, _skinChooserClick); // skin chooser.
        }

        /// <summary>
        ///     Set's interface up.
        /// </summary>
        public void SetupInterface()
        {
            InterfaceInstance.SetOptions(128, 0, 33, 2);
            InterfaceInstance.SetOptions(132, 0, 204, 2);
            InterfaceInstance.SetOptions(65, 0, 11, 2);
        }

        /// <summary>
        ///     Randomizes colors and body parts.
        /// </summary>
        public void Randomize()
        {
            var female = Owner.Appearance.Female;
            Owner.Appearance.Female = !female;
            Owner.Appearance.SetColor(ColorType.HairColor, HairColours[RandomStatic.Generator.Next(HairColours.Length)]);
            Owner.Appearance.SetColor(ColorType.TorsoColor, ClothColours[RandomStatic.Generator.Next(ClothColours.Length)]);
            Owner.Appearance.SetColor(ColorType.LegColor, ClothColours[RandomStatic.Generator.Next(ClothColours.Length)]);
            Owner.Appearance.SetColor(ColorType.FeetColor, FeetColours[RandomStatic.Generator.Next(FeetColours.Length)]);
            Owner.Appearance.SetColor(ColorType.SkinColor, SkinColours[RandomStatic.Generator.Next(SkinColours.Length)]);
            var hairLook = female
                ? FemaleHairLooks[RandomStatic.Generator.Next(FemaleHairLooks.Length)]
                : MaleHairLooks[RandomStatic.Generator.Next(MaleHairLooks.Length)];
            Owner.Appearance.SetLook(LookType.HairLook, hairLook);
            Owner.Appearance.SetLook(LookType.BeardLook,
                Owner.Appearance.Female ? 9 : MaleFacialLooks[RandomStatic.Generator.Next(MaleFacialLooks.Length)]);
            var torsoIndex = RandomStatic.Generator.Next(MaleTorsoLooks.GetLength(0));
            var torso = female ? FemaleTorsoLooks[torsoIndex, 0] : MaleTorsoLooks[torsoIndex, 0];
            Owner.Appearance.SetLook(LookType.TorsoLook, torso == -1 ? 0 : torso);
            var arms = female ? FemaleTorsoLooks[torsoIndex, 1] : MaleTorsoLooks[torsoIndex, 1];
            if (arms != -1)
            {
                Owner.Appearance.SetLook(LookType.ArmsLook, arms);
            }

            var wrists = female ? FemaleTorsoLooks[torsoIndex, 2] : MaleTorsoLooks[torsoIndex, 2];
            Owner.Appearance.SetLook(LookType.WristLook, wrists == -1 ? 0 : wrists);
            var legs = female
                ? FemaleLegLooks[RandomStatic.Generator.Next(FemaleLegLooks.Length)]
                : MaleLegLooks[RandomStatic.Generator.Next(MaleLegLooks.Length)];
            Owner.Appearance.SetLook(LookType.LegsLook, legs);
            var feet = female
                ? FemaleFeetLooks[RandomStatic.Generator.Next(FemaleFeetLooks.Length)]
                : MaleFeetLooks[RandomStatic.Generator.Next(MaleFeetLooks.Length)];
            Owner.Appearance.SetLook(LookType.FeetLook, feet);
        }

        /// <summary>
        ///     Randomizes head only.
        /// </summary>
        public void RandomizeHead()
        {
            var hairLooks = Owner.Appearance.Female ? FemaleHairLooks : MaleHairLooks;
            var faceLooks = Owner.Appearance.Female
                ?
                [
                    9
                ]
                : MaleFacialLooks;
            Owner.Appearance.SetLook(LookType.HairLook, hairLooks[RandomStatic.Generator.Next(hairLooks.Length)]);
            Owner.Appearance.SetLook(LookType.BeardLook, faceLooks[RandomStatic.Generator.Next(faceLooks.Length)]);
            Owner.Appearance.SetColor(ColorType.HairColor, HairColours[RandomStatic.Generator.Next(HairColours.Length)]);
        }

        /// <summary>
        ///     Refreshe's this interface configurations.
        /// </summary>
        public void Refresh()
        {
            Owner.Configurations.SendStandardConfiguration(1158, _styleID);
            Owner.Configurations.SendGlobalCs2Int(1008, Owner.Appearance.GetDrawnBodyPart(BodyPart.HairPart));
            Owner.Configurations.SendGlobalCs2Int(1009, Owner.Appearance.GetDrawnBodyPart(BodyPart.BeardPart));
            Owner.Configurations.SendGlobalCs2Int(1010, Owner.Appearance.GetDrawnBodyPart(BodyPart.ChestPart));
            Owner.Configurations.SendGlobalCs2Int(1011, Owner.Appearance.GetDrawnBodyPart(BodyPart.ArmsPart));
            Owner.Configurations.SendGlobalCs2Int(1012, Owner.Appearance.GetDrawnBodyPart(BodyPart.HandsPart));
            Owner.Configurations.SendGlobalCs2Int(1013, Owner.Appearance.GetDrawnBodyPart(BodyPart.LegsPart));
            Owner.Configurations.SendGlobalCs2Int(1014, Owner.Appearance.GetDrawnBodyPart(BodyPart.FeetPart));
            Owner.Configurations.SendGlobalCs2Int(1015, Owner.Appearance.GetColor(ColorType.HairColor));
            Owner.Configurations.SendGlobalCs2Int(1016, Owner.Appearance.GetColor(ColorType.TorsoColor));
            Owner.Configurations.SendGlobalCs2Int(1017, Owner.Appearance.GetColor(ColorType.LegColor));
            Owner.Configurations.SendGlobalCs2Int(1018, Owner.Appearance.GetColor(ColorType.FeetColor));
            Owner.Configurations.SendGlobalCs2Int(1019, Owner.Appearance.GetColor(ColorType.SkinColor));
            Owner.Appearance.DrawCharacter();
            DrawAccessories();
        }

        /// <summary>
        ///     Refreshes the gender.
        /// </summary>
        public void RefreshGender() => Owner.Configurations.SendStandardConfiguration(1363, Owner.Appearance.Female ? 12345 : 8249);

        /// <summary>
        ///     Draw's accessories.
        /// </summary>
        private void DrawAccessories()
        {
            if (_styleID <= 0)
            {
                return;
            }

            var amulets = Owner.Appearance.Female ? FemaleAmulets : MaleAmulets;
            var weapons = Owner.Appearance.Female ? FemaleWeapons : MaleWeapons;
            var shields = Owner.Appearance.Female ? FemaleShields : MaleShields;

            if (amulets[_styleID - 1] != -1)
            {
                Owner.Appearance.DrawItem(BodyPart.AmuletPart, amulets[_styleID - 1]);
            }

            if (weapons[_styleID - 1] != -1)
            {
                Owner.Appearance.DrawItem(BodyPart.WeaponPart, weapons[_styleID - 1]);
            }

            if (shields[_styleID - 1] != -1)
            {
                Owner.Appearance.DrawItem(BodyPart.ShieldPart, shields[_styleID - 1]);
            }
        }

        /// <summary>
        ///     Happens when style button is clicked.
        /// </summary>
        /// <param name="componentID">Id of the step changing button.</param>
        /// <param name="buttonClickType">Type of the cilck.</param>
        /// <param name="info1">Extra info 1.</param>
        /// <param name="info2">Extra info 2.</param>
        /// <returns></returns>
        public bool StyleButtonClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (_step != 0 || buttonClickType != ComponentClickType.LeftClick)
            {
                return false;
            }

            SetStyle(componentID - 68 + 1);
            return true;
        }

        /// <summary>
        ///     Set's style of the character.
        /// </summary>
        /// <param name="styleID">Id of the style.</param>
        public void SetStyle(int styleID)
        {
            _styleID = styleID;
            SetSubStyle(styleID > 0 ? 1 : 0);
        }

        /// <summary>
        ///     Set's substyle of the character.
        /// </summary>
        /// <param name="substyleID"></param>
        public void SetSubStyle(int substyleID)
        {
            _substyleID = substyleID;

            if (Owner.Appearance.Female)
            {
                ApplySubStyleFemale();
            }
            else
            {
                ApplySubStyleMale();
            }

            Refresh();
        }

        /// <summary>
        ///     Applies male substyle.
        /// </summary>
        private void ApplySubStyleMale()
        {
            if (_styleID <= 0 || _substyleID <= 0)
            {
                return;
            }

            var dd = MaleDefaultDesigns[_styleID - 1];
            var dsd = dd.SubDesigns[_substyleID - 1];
            for (var i = 0; i < dsd.Looks.GetLength(0); i++)
            {
                var value = dsd.Looks[i, 1];
                if (value != -1)
                {
                    Owner.Appearance.SetLook((LookType)dsd.Looks[i, 0], value);
                }
            }

            for (var i = 0; i < dsd.Colours.GetLength(0); i++)
            {
                Owner.Appearance.SetColor((ColorType)dsd.Colours[i, 0], dsd.Colours[i, 1]);
            }
        }

        /// <summary>
        ///     Applies female substyle.
        /// </summary>
        private void ApplySubStyleFemale()
        {
            if (_styleID <= 0 || _substyleID <= 0)
            {
                return;
            }

            var dd = FemaleDefaultDesigns[_styleID - 1];
            var dsd = dd.SubDesigns[_substyleID - 1];
            for (var i = 0; i < dsd.Looks.GetLength(0); i++)
            {
                var value = dsd.Looks[i, 1];
                if (value != -1)
                {
                    Owner.Appearance.SetLook((LookType)dsd.Looks[i, 0], value);
                }
            }

            for (var i = 0; i < dsd.Colours.GetLength(0); i++)
            {
                Owner.Appearance.SetColor((ColorType)dsd.Colours[i, 0], dsd.Colours[i, 1]);
            }
        }

        /// <summary>
        ///     Happens when skin chooser is clicked.
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="buttonClickType"></param>
        /// <param name="info1"></param>
        /// <param name="info2"></param>
        /// <returns></returns>
        public bool SkinChooserClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step != 0)
            {
                return false;
            }

            if (info2 < 0 || info2 >= SkinColours.Length)
            {
                return false;
            }

            Owner.Appearance.SetColor(ColorType.SkinColor, SkinColours[info2]);
            Refresh();
            return true;
        }


        /// <summary>
        ///     Happens when designer chooser is clicked.
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="buttonClickType"></param>
        /// <param name="info1"></param>
        /// <param name="info2"></param>
        /// <returns></returns>
        public bool DesignerChooserClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step != 1)
            {
                return false;
            }

            _advancedSelectorID = componentID - 116;
            return true;
        }


        /// <summary>
        ///     Happens when designer pallete is clicked.
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="buttonClickType"></param>
        /// <param name="info1"></param>
        /// <param name="info2"></param>
        /// <returns></returns>
        public bool DesignerPalleteClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step != 1 || info2 < 0)
            {
                return false;
            }

            if (_advancedSelectorID == 0)
            {
                if (info2 >= SkinColours.Length)
                {
                    return false;
                }

                Owner.Appearance.SetColor(ColorType.SkinColor, SkinColours[info2]);
            }
            else if (_advancedSelectorID == 1)
            {
                if (info2 >= HairColours.Length)
                {
                    return false;
                }

                Owner.Appearance.SetColor(ColorType.HairColor, HairColours[info2]);
            }
            else if (_advancedSelectorID == 2)
            {
                if (info2 >= ClothColours.Length)
                {
                    return false;
                }

                Owner.Appearance.SetColor(ColorType.TorsoColor, ClothColours[info2]);
            }
            else if (_advancedSelectorID == 3)
            {
                if (info2 >= ClothColours.Length)
                {
                    return false;
                }

                Owner.Appearance.SetColor(ColorType.LegColor, ClothColours[info2]);
            }
            else if (_advancedSelectorID == 4)
            {
                if (info2 >= FeetColours.Length)
                {
                    return false;
                }

                Owner.Appearance.SetColor(ColorType.FeetColor, FeetColours[info2]);
            }
            else if (_advancedSelectorID == 5)
            {
                if (Owner.Appearance.Female || info2 >= HairColours.Length)
                {
                    return false;
                }

                Owner.Appearance.SetColor(ColorType.HairColor, HairColours[info2]);
            }
            else
            {
                return false;
            }

            Refresh();
            return true;
        }

        /// <summary>
        ///     Happens when designer pallete is clicked.
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="buttonClickType"></param>
        /// <param name="info1"></param>
        /// <param name="info2"></param>
        /// <returns></returns>
        public bool DesignerPanelClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step != 1 || info2 < 0)
            {
                return false;
            }

            if (_advancedSelectorID == 0)
            {
                return false;
            }

            if (_advancedSelectorID == 1)
            {
                var d = Owner.Appearance.Female ? FemaleHairLooks : MaleHairLooks;
                if (info2 >= d.Length)
                {
                    return false;
                }

                Owner.Appearance.SetLook(LookType.HairLook, d[info2]);
            }
            else if (_advancedSelectorID == 2)
            {
                var d = Owner.Appearance.Female ? FemaleTorsoLooks : MaleTorsoLooks;
                if (info2 >= d.GetLength(0))
                {
                    return false;
                }

                int torsoIndex = info2;
                Owner.Appearance.SetLook(LookType.TorsoLook, d[torsoIndex, 0] == -1 ? 0 : d[torsoIndex, 0]);
                var arms = MaleTorsoLooks[torsoIndex, 1];
                if (arms != -1)
                {
                    Owner.Appearance.SetLook(LookType.ArmsLook, arms);
                }

                Owner.Appearance.SetLook(LookType.WristLook, d[torsoIndex, 2] == -1 ? 0 : d[torsoIndex, 2]);
            }
            else if (_advancedSelectorID == 3)
            {
                var d = Owner.Appearance.Female ? FemaleLegLooks : MaleLegLooks;
                if (info2 >= d.Length)
                {
                    return false;
                }

                Owner.Appearance.SetLook(LookType.LegsLook, d[info2]);
            }
            else if (_advancedSelectorID == 4)
            {
                var d = Owner.Appearance.Female ? FemaleFeetLooks : MaleFeetLooks;
                if (info2 >= d.Length)
                {
                    return false;
                }

                Owner.Appearance.SetLook(LookType.FeetLook, d[info2]);
            }
            else if (_advancedSelectorID == 5)
            {
                if (Owner.Appearance.Female || info2 >= MaleFacialLooks.Length)
                {
                    return false;
                }

                Owner.Appearance.SetLook(LookType.BeardLook, MaleFacialLooks[info2]);
            }
            else
            {
                return false;
            }

            Refresh();
            return true;
        }

        /// <summary>
        ///     Happens when step button is clicked.
        /// </summary>
        /// <param name="componentID">Id of the step changing button.</param>
        /// <param name="buttonClickType">Type of the cilck.</param>
        /// <param name="info1">Extra info 1.</param>
        /// <param name="info2">Extra info 2.</param>
        /// <returns></returns>
        public bool StepButtonClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step == 2)
            {
                return false;
            }

            if (_step == 0 && componentID == 137)
            {
                return false;
            }

            if (_step == 1 && componentID == 139)
            {
                return false;
            }

            _step = componentID == 137 ? 0 : 1;
            InterfaceInstance.SetVisible(138, _step == 1);
            Refresh();
            return true;
        }

        /// <summary>
        ///     Happens when gender button is clicked.
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="buttonClickType"></param>
        /// <param name="info1"></param>
        /// <param name="info2"></param>
        /// <returns></returns>
        public bool GenderButtonClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step != 0)
            {
                return false;
            }

            Owner.Appearance.Female = componentID != 62;
            if (componentID != 62)
            {
                ApplySubStyleFemale();
            }
            else
            {
                ApplySubStyleMale();
            }

            RefreshGender();
            RandomizeHead();
            Refresh();
            return true;
        }


        /// <summary>
        ///     Happens when substyle button is clicked.
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="buttonClickType"></param>
        /// <param name="info1"></param>
        /// <param name="info2"></param>
        /// <returns></returns>
        public bool SubStyleButtonClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step != 0)
            {
                return false;
            }

            SetSubStyle(componentID - 103 + 1);
            return true;
        }

        /// <summary>
        ///     Happens when confirm button is clicked.
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="buttonClickType"></param>
        /// <param name="info1"></param>
        /// <param name="info2"></param>
        /// <returns></returns>
        public bool ConfirmButtonClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step != 1)
            {
                return false;
            }

            if (Owner.Name.Length == 0 || Owner.Name.StartsWith("#") || Owner.DisplayName.Length == 0 || Owner.DisplayName.StartsWith("#"))
            {
                Refresh();
                PrepareNameChooser();
            }
            else
            {
                End();
            }

            return true;
        }

        /// <summary>
        ///     Happens when color randomizer is clicked.
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="buttonClickType"></param>
        /// <param name="info1"></param>
        /// <param name="info2"></param>
        /// <returns></returns>
        public bool ColorRandomizerButtonClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step != 1)
            {
                return false;
            }

            Owner.Appearance.SetColor(ColorType.TorsoColor, ClothColours[RandomStatic.Generator.Next(ClothColours.Length)]);
            Owner.Appearance.SetColor(ColorType.LegColor, ClothColours[RandomStatic.Generator.Next(ClothColours.Length)]);
            Owner.Appearance.SetColor(ColorType.FeetColor, FeetColours[RandomStatic.Generator.Next(FeetColours.Length)]);
            Refresh();
            return true;
        }


        /// <summary>
        ///     Enable's acc name input.
        /// </summary>
        public void PrepareNameChooser()
        {
            _step = 2;
            /*this.interfaceInstance.SetVisible(90, true);
            this.interfaceInstance.SetVisible(156, false);
            this.interfaceInstance.SetVisible(164, false);
            this.interfaceInstance.SetVisible(35, false);
            this.interfaceInstance.SetVisible(115, false);
            this.interfaceInstance.SetVisible(117, false);
            this.interfaceInstance.SetVisible(116, false);
            this.interfaceInstance.SetVisible(89, false);*/
            //this.interfaceInstance.DrawString(154, "Please enter your desired character name.");
            Owner.Configurations.SendCs2Script(3943, []); // enable the input
        }

        /// <summary>
        ///     Happens when account name confirm button is clicked.
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="buttonClickType"></param>
        /// <param name="info1"></param>
        /// <param name="info2"></param>
        /// <returns></returns>
        public bool AccountConfirmButtonClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step != 2)
            {
                return false;
            }

            AccountNameConfirm();
            return true;
        }


        /// <summary>
        ///     Enable's acc name confirm.
        /// </summary>
        public void AccountNameConfirm()
        {
            _step = 3;
            Owner.Widgets.StringInputHandler = NameInputHandler;
            Owner.Configurations.SendCs2Script(3945,
            [
                0, 1, 1
            ]); // send the packet.
        }

        /// <summary>
        ///     Method for listening for name input.
        /// </summary>
        /// <param name="name"></param>
        public void NameInputHandler(string name)
        {
            Owner.Widgets.StringInputHandler = null;
            _lastName = name;
            if (_step != 3)
            {
                return;
            }

            if (name.Length <= 0)
            {
                OnBadNameEntered(name, null);
                return;
            }

            SendSetName(name);
        }

        /// <summary>
        ///     Send's set name packet.
        /// </summary>
        /// <param name="name"></param>
        private void SendSetName(string name)
        {
            _step = 4;
            _nameSetCallback = Owner.RegisterEventHandler<NameSetFinishedEvent>(NameSetFinished);
            //var adapter = ServiceLocator.Current.GetInstance<IMasterConnectionAdapter>();
            //adapter.SendPacketAsync(new SetCharacterDetailsRequestPacketComposer(Owner.Session.Id,
            //        new CharacterDetailsDto
            //        {
            //            DisplayName = name
            //        }))
            //    .Wait();
        }

        /// <summary>
        ///     Get's called when name set is finished and we can get response.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public bool NameSetFinished(Event e)
        {
            if (_nameSetCallback == null)
            {
                return false;
            }

            if (!Owner.UnregisterEventHandler<NameSetFinishedEvent>(_nameSetCallback))
            {
                return false;
            }

            _nameSetCallback = null;
            var nfe = (NameSetFinishedEvent)e;
            if (nfe.Success)
            {
                End();
            }
            else
            {
                OnBadNameEntered(_lastName, nfe.ReturnMessage);
            }

            return false;
        }

        /// <summary>
        ///     Executed when given name is not available.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="returnMessage">The return message.</param>
        private void OnBadNameEntered(string name, string? returnMessage)
        {
            if (returnMessage == null)
            {
                InterfaceInstance.DrawString(170, "'" + name + "' is not available.");
            }
            else
            {
                InterfaceInstance.DrawString(170, returnMessage);
            }

            SetSuggestions(GenerateSuggestions());
            _step = 2;
        }

        /// <summary>
        ///     Set's suggestions.
        /// </summary>
        /// <param name="sugg"></param>
        private void SetSuggestions(string[] sugg)
        {
            InterfaceInstance.DrawString(158, "<u=ebe0bc>" + sugg[0]);
            Owner.Configurations.SendGlobalCs2String(331, sugg[0]);
            InterfaceInstance.DrawString(159, "<u=ebe0bc>" + sugg[1]);
            Owner.Configurations.SendGlobalCs2String(332, sugg[1]);
            InterfaceInstance.DrawString(160, "<u=ebe0bc>" + sugg[2]);
            Owner.Configurations.SendGlobalCs2String(333, sugg[2]);
            InterfaceInstance.DrawString(161, "<u=ebe0bc>");
            Owner.Configurations.SendGlobalCs2String(334, "");
            InterfaceInstance.DrawString(162, "<u=ebe0bc>");
            Owner.Configurations.SendGlobalCs2String(335, "");
            InterfaceInstance.DrawString(163, "<u=ebe0bc>");
            Owner.Configurations.SendGlobalCs2String(336, "");
            Owner.Configurations.SendCs2Script(3946,
            [
                sugg[0], sugg[1], sugg[2], "", "", "", 1
            ]);
            Owner.Configurations.SendCs2Script(3948,
            [
                100
            ]);
            //this.interfaceInstance.SetVisible(156, true);
            //this.interfaceInstance.SetVisible(155, false);
            Owner.Configurations.SendCs2Script(3945,
            [
                4, 1, 1
            ]); // set the inputbox data to cs2string 334
            Owner.Configurations.SendCs2Script(3943, []); // enable input
        }

        /// <summary>
        ///     Generate's String[3] with 3 name suggestions.
        /// </summary>
        /// <returns></returns>
        private static string[] GenerateSuggestions()
        {
            var sugg = new string[3];
            for (var i = 0; i < sugg.Length; i++)
            {
                var randomWord = _suggestionTable[RandomStatic.Generator.Next(_suggestionTable.Length)];
                var randomNumber = RandomStatic.Generator.Next(9999);
                sugg[i] = randomWord + randomNumber;
                if (sugg[i].Length > 12)
                {
                    var buff = sugg[i].ToCharArray();
                    var rBuff = new char[RandomStatic.Generator.Next(12)];
                    for (var a = 0; a < rBuff.Length; a++)
                    {
                        rBuff[a] = buff[a];
                    }

                    sugg[i] = new string(rBuff);
                }
            }

            return sugg;
        }


        /// <summary>
        ///     Happens when suggestion button is clicked.
        /// </summary>
        /// <param name="componentID"></param>
        /// <param name="buttonClickType"></param>
        /// <param name="info1"></param>
        /// <param name="info2"></param>
        /// <returns></returns>
        public bool SuggestionButtonClicked(int componentID, ComponentClickType buttonClickType, int info1, int info2)
        {
            if (buttonClickType != ComponentClickType.LeftClick || _step != 2)
            {
                return false;
            }

            if (componentID >= 158 && componentID <= 160)
            {
                Owner.Configurations.SendCs2Script(3945,
                [
                    componentID - 158 + 1, 1, 1
                ]);
                return true;
            }

            SetSuggestions(GenerateSuggestions());
            return true;
        }


        /// <summary>
        ///     End's character design.
        /// </summary>
        public void End()
        {
            Owner.Appearance.DrawCharacter(); // redraw , so accessories are removed
            Owner.Appearance.Refresh();
            Owner.Widgets.CloseAll();
            Owner.GetScript<WidgetsCharacterScript>()?.OpenMainGameFrame();
            InitiateWelcome();
        }

        /// <summary>
        ///     Initiates the welcome.
        /// </summary>
        public void InitiateWelcome()
        {
            if (Owner.HasReceivedWelcome)
            {
                return;
            }

            Owner.SendChatMessage("<col=FF0000>You have received your starter pack! We hope you enjoy Hagalaz!</col>");

            _gameMessageService.MessageAsync("A new hero has joined Hagalaz! Give " + Owner.DisplayName + " a warm welcome!", GameMessageType.Game);

            foreach (var item in _characterCreateInfoRepository.FindAllContainerItems())
            {
                var it = _itemBuilder.Create().WithId(item.Id).WithCount(item.Count).Build();

                switch (item.Type)
                {
                    case ItemContainerType.Inventory: Owner.Inventory.Add(it); break;
                    case ItemContainerType.Bank: Owner.Bank.Add(it); break;
                    case ItemContainerType.MoneyPouch: Owner.MoneyPouch.Add(it); break;
                }
            }

            var sss = Owner.Viewport.VisibleCreatures.OfType<INpc>().FirstOrDefault(npc => npc.Name.Contains("sage", StringComparison.OrdinalIgnoreCase));
            if (sss == null)
            {
                return;
            }

            var task = new CreatureReachTask(Owner,
                sss,
                success =>
                {
                    if (success)
                    {
                        var ssSageStarterDialogue = Owner.ServiceProvider.GetRequiredService<SageStarterDialogue>();
                        Owner.Widgets.OpenChatboxOverlay((short)DialogueInterfaces.Send2TextChatRight,
                            0,
                            ssSageStarterDialogue,
                            false,
                            sss);
                    }
                },
                typeof(ICharacter)); // Prevent any interruption that would have prevented the task from executing.
            Owner.QueueTask(task);
        }


        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose() { }

        /// <summary>
        ///     Contains all skin colours.
        /// </summary>
        public static readonly int[] SkinColours =
        [
            9, 8, 7, 0, 1, 2, 3, 4, 5, 6, 10, 11
        ];

        /// <summary>
        ///     Contains all hair colours.
        /// </summary>
        public static readonly int[] HairColours =
        [
            20, 19, 10, 18, 4, 5, 15, 7, 0, 6, 21, 9, 22, 17, 8, 16, 24, 11, 23, 3, 2, 1, 14, 13, 12
        ];

        /// <summary>
        ///     Contains all clothes colours.
        /// </summary>
        public static readonly int[] ClothColours =
        [
            32,
            101,
            48,
            56,
            165,
            103,
            167,
            106,
            54,
            198,
            199,
            200,
            225,
            35,
            39,
            53,
            42,
            46,
            29,
            91,
            57,
            90,
            34,
            102,
            104,
            105,
            107,
            173,
            137,
            201,
            204,
            211,
            197,
            108,
            217,
            220,
            221,
            226,
            227,
            215,
            222,
            166,
            212,
            174,
            175,
            169,
            144,
            135,
            136,
            133,
            123,
            119,
            192,
            194,
            117,
            115,
            111,
            141,
            45,
            49,
            84,
            77,
            118,
            88,
            85,
            138,
            51,
            92,
            112,
            145,
            179,
            143,
            149,
            151,
            153,
            44,
            154,
            155,
            86,
            89,
            72,
            66,
            33,
            206,
            109,
            110,
            114,
            116,
            184,
            170,
            120,
            113,
            150,
            205,
            210,
            207,
            209,
            193,
            152,
            156,
            183,
            161,
            159,
            160,
            73,
            75,
            181,
            185,
            208,
            74,
            36,
            37,
            43,
            50,
            58,
            55,
            139,
            148,
            147,
            64,
            69,
            70,
            71,
            68,
            93,
            94,
            95,
            124,
            182,
            96,
            97,
            219,
            63,
            228,
            79,
            82,
            98,
            99,
            100,
            125,
            126,
            127,
            40,
            128,
            129,
            188,
            130,
            131,
            186,
            132,
            164,
            157,
            180,
            187,
            31,
            162,
            168,
            52,
            163,
            158,
            196,
            59,
            60,
            87,
            78,
            61,
            76,
            80,
            171,
            172,
            176,
            177,
            178,
            38,
            41,
            47,
            62,
            65,
            67,
            81,
            83,
            121,
            122,
            134,
            140,
            142,
            146,
            189,
            190,
            191,
            195,
            202,
            203,
            213,
            214,
            216,
            218,
            223,
            224
        ];

        /// <summary>
        ///     Contains all feet colours.
        /// </summary>
        public static readonly int[] FeetColours =
        [
            55,
            54,
            14,
            120,
            194,
            53,
            11,
            154,
            0,
            1,
            2,
            3,
            4,
            5,
            9,
            78,
            25,
            33,
            142,
            80,
            144,
            83,
            31,
            175,
            176,
            177,
            12,
            16,
            30,
            19,
            23,
            6,
            68,
            34,
            67,
            11,
            79,
            81,
            82,
            84,
            150,
            114,
            178,
            181,
            188,
            174,
            85,
            194,
            197,
            198,
            203,
            204,
            192,
            199,
            143,
            189,
            151,
            152,
            146,
            121,
            112,
            113,
            110,
            100,
            96,
            169,
            171,
            94,
            92,
            88,
            118,
            22,
            26,
            61,
            95,
            65,
            62,
            115,
            28,
            69,
            89,
            122,
            156,
            126,
            128,
            130,
            21,
            131,
            132,
            63,
            66,
            49,
            43,
            10,
            183,
            86,
            87,
            91,
            93,
            161,
            147,
            97,
            90,
            127,
            182,
            187,
            184,
            186,
            170,
            129,
            133,
            160,
            138,
            136,
            137,
            50,
            52,
            158,
            162,
            185,
            51,
            13,
            20,
            27,
            35,
            32,
            116,
            125,
            124,
            41,
            46,
            47,
            48,
            45,
            70,
            71,
            72,
            101,
            159,
            73,
            74,
            196,
            40,
            205,
            56,
            59,
            75,
            76,
            77,
            102,
            103,
            104,
            17,
            105,
            106,
            165,
            107,
            108,
            163,
            109,
            141,
            134,
            157,
            164,
            8,
            139,
            145,
            29,
            140,
            135,
            173,
            36,
            37,
            64,
            38,
            57,
            148,
            149,
            153,
            155,
            15,
            18,
            24,
            39,
            42,
            44,
            58,
            60,
            98,
            99,
            117,
            119,
            123,
            166,
            167,
            168,
            172,
            179,
            180,
            190,
            191,
            193,
            195,
            200,
            201
        ];

        /// <summary>
        ///     Contains all male hair looks.
        /// </summary>
        public static readonly int[] MaleHairLooks =
        [
            5, 6, 93, 96, 92, 268, 265, 264, 267, 315, 94, 263, 312, 313, 311, 314, 261, 310, 1, 0, 97, 95, 262, 316, 309, 3, 91, 4
        ];

        /// <summary>
        ///     Contains all male torso looks.
        /// </summary>
        public static readonly int[,] MaleTorsoLooks =
        {
            {
                457, 588, 364
            },
            {
                445, -1, 366
            },
            {
                459, 591, 367
            },
            {
                460, 592, 368
            },
            {
                461, 593, 369
            },
            {
                462, 594, 370
            },
            {
                452, -1, 371
            },
            {
                463, 596, 372
            },
            {
                464, 597, 373
            },
            {
                446, -1, 374
            },
            {
                465, 599, 375
            },
            {
                466, 600, 376
            },
            {
                467, 601, 377
            },
            {
                451, -1, 378
            },
            {
                468, 603, 379
            },
            {
                453, -1, 380
            },
            {
                454, -1, 381
            },
            {
                455, -1, 382
            },
            {
                469, 607, 383
            },
            {
                470, 608, 384
            },
            {
                450, -1, 385
            },
            {
                458, 589, 365
            },
            {
                447, -1, 386
            },
            {
                448, -1, 387
            },
            {
                449, -1, 388
            },
            {
                471, 613, 389
            },
            {
                443, -1, 390
            },
            {
                472, 615, 391
            },
            {
                473, 616, 392
            },
            {
                444, -1, 393
            },
            {
                474, 618, 394
            },
            {
                456, -1, 9
            }
        };

        /// <summary>
        ///     Contains all male leg looks.
        /// </summary>
        public static readonly int[] MaleLegLooks =
        [
            620,
            622,
            623,
            624,
            625,
            626,
            627,
            628,
            629,
            630,
            631,
            632,
            633,
            634,
            635,
            636,
            637,
            638,
            639,
            640,
            641,
            621,
            642,
            643,
            644,
            645,
            646,
            647,
            648,
            649,
            650,
            651
        ];

        /// <summary>
        ///     Contains all male feet looks.
        /// </summary>
        public static readonly int[] MaleFeetLooks =
        [
            427, 428, 429, 430, 431, 432, 433, 434, 435, 436, 437, 438, 439, 440, 441, 442, 42, 43
        ];

        /// <summary>
        ///     Contains all male face looks.
        /// </summary>
        public static readonly int[] MaleFacialLooks =
        [
            14, 13, 98, 308, 305, 307, 10, 15, 16, 100, 12, 11, 102, 306, 99, 101, 104, 17
        ];

        /// <summary>
        ///     Contains all female hair looks.
        /// </summary>
        public static readonly int[] FemaleHairLooks =
        [
            141,
            361,
            272,
            273,
            359,
            274,
            353,
            277,
            280,
            360,
            356,
            269,
            358,
            270,
            275,
            357,
            145,
            271,
            354,
            355,
            45,
            52,
            49,
            47,
            48,
            46,
            143,
            362,
            144,
            279,
            142,
            146,
            278,
            135
        ];

        /// <summary>
        ///     Contains all female torso looks.
        /// </summary>
        public static readonly int[,] FemaleTorsoLooks =
        {
            {
                565, 395, 507
            },
            {
                567, 397, 509
            },
            {
                568, 398, 510
            },
            {
                569, 399, 511
            },
            {
                570, 400, 512
            },
            {
                571, 401, 513
            },
            {
                561, -1, 514
            },
            {
                572, 403, 515
            },
            {
                573, 404, 516
            },
            {
                574, 405, 517
            },
            {
                575, 406, 518
            },
            {
                576, 407, 519
            },
            {
                577, 408, 520
            },
            {
                560, -1, 521
            },
            {
                578, 410, 522
            },
            {
                562, -1, 523
            },
            {
                563, -1, 524
            },
            {
                564, -1, 525
            },
            {
                579, 414, 526
            },
            {
                559, -1, 527
            },
            {
                580, 416, 528
            },
            {
                566, 396, 508
            },
            {
                581, 417, 529
            },
            {
                582, 418, 530
            },
            {
                557, -1, 531
            },
            {
                583, 420, 532
            },
            {
                584, 421, 533
            },
            {
                585, 422, 534
            },
            {
                586, 423, 535
            },
            {
                556, -1, 536
            },
            {
                587, 425, 537
            },
            {
                558, -1, 538
            }
        };

        /// <summary>
        ///     Contains all female leg looks.
        /// </summary>
        public static readonly int[] FemaleLegLooks =
        [
            475,
            477,
            478,
            479,
            480,
            481,
            482,
            483,
            484,
            485,
            486,
            487,
            488,
            489,
            490,
            491,
            492,
            493,
            494,
            495,
            496,
            476,
            497,
            498,
            499,
            500,
            501,
            502,
            503,
            504,
            505,
            506
        ];

        /// <summary>
        ///     Contains all female feet looks.
        /// </summary>
        public static readonly int[] FemaleFeetLooks =
        [
            539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 555, 79, 80
        ];

        /// <summary>
        ///     Contains male default designs.
        /// </summary>
        public static readonly DefaultDesign[] MaleDefaultDesigns =
        [
            new DefaultDesign( // ADVENTURER
            [
                new DefaultSubDesign(new[,]
                    {
                        // ADVENTURER #1
                        {
                            2, 473
                        },
                        {
                            3, 616
                        },
                        {
                            4, 392
                        },
                        {
                            5, 648
                        },
                        {
                            6, 441
                        }
                    },
                    new[,]
                    {
                        {
                            1, 37
                        },
                        {
                            2, 213
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign(new[,]
                    {
                        // ADVENTURER #2
                        {
                            2, 443
                        },
                        {
                            3, -1
                        },
                        {
                            4, 390
                        },
                        {
                            5, 646
                        },
                        {
                            6, 440
                        }
                    },
                    new[,]
                    {
                        {
                            1, 37
                        },
                        {
                            2, 213
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign(new[,]
                    {
                        // ADVENTURER #3
                        {
                            2, 474
                        },
                        {
                            3, 618
                        },
                        {
                            4, 394
                        },
                        {
                            5, 650
                        },
                        {
                            6, 441
                        }
                    },
                    new[,]
                    {
                        {
                            1, 37
                        },
                        {
                            2, 213
                        },
                        {
                            3, 4
                        }
                    })
            ]),
            new DefaultDesign( // WARRIOR
            [
                new DefaultSubDesign(new[,]
                    {
                        // WARRIOR #1
                        {
                            2, 453
                        },
                        {
                            3, -1
                        },
                        {
                            4, 380
                        },
                        {
                            5, 636
                        },
                        {
                            6, 429
                        }
                    },
                    new[,]
                    {
                        {
                            1, 197
                        },
                        {
                            2, 202
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // WARRIOR #2
                    new[,]
                    {
                        {
                            2, 454
                        },
                        {
                            3, -1
                        },
                        {
                            4, 381
                        },
                        {
                            5, 637
                        },
                        {
                            6, 429
                        }
                    },
                    new[,]
                    {
                        {
                            1, 197
                        },
                        {
                            2, 202
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // WARROR #3
                    new[,]
                    {
                        {
                            2, 455
                        },
                        {
                            3, -1
                        },
                        {
                            4, 382
                        },
                        {
                            5, 638
                        },
                        {
                            6, 429
                        }
                    },
                    new[,]
                    {
                        {
                            1, 197
                        },
                        {
                            2, 202
                        },
                        {
                            3, 4
                        }
                    })
            ]),
            new DefaultDesign( // MAGE
            [
                new DefaultSubDesign( // MAGE #1
                    new[,]
                    {
                        {
                            2, 447
                        },
                        {
                            3, -1
                        },
                        {
                            4, 386
                        },
                        {
                            5, 642
                        },
                        {
                            6, 429
                        }
                    },
                    new[,]
                    {
                        {
                            1, 125
                        },
                        {
                            2, 125
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // MAGE #2
                    new[,]
                    {
                        {
                            2, 448
                        },
                        {
                            3, -1
                        },
                        {
                            4, 387
                        },
                        {
                            5, 643
                        },
                        {
                            6, 429
                        }
                    },
                    new[,]
                    {
                        {
                            1, 125
                        },
                        {
                            2, 125
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // MAGE #3
                    new[,]
                    {
                        {
                            2, 449
                        },
                        {
                            3, -1
                        },
                        {
                            4, 388
                        },
                        {
                            5, 644
                        },
                        {
                            6, 429
                        }
                    },
                    new[,]
                    {
                        {
                            1, 125
                        },
                        {
                            2, 125
                        },
                        {
                            3, 4
                        }
                    })
            ]),
            new DefaultDesign( // RANGER
            [
                new DefaultSubDesign( // RANGER #1
                    new[,]
                    {
                        {
                            2, 469
                        },
                        {
                            3, 607
                        },
                        {
                            4, 383
                        },
                        {
                            5, 639
                        },
                        {
                            6, 431
                        }
                    },
                    new[,]
                    {
                        {
                            1, 149
                        },
                        {
                            2, 150
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // RANGER #2
                    new[,]
                    {
                        {
                            2, 470
                        },
                        {
                            3, 608
                        },
                        {
                            4, 384
                        },
                        {
                            5, 640
                        },
                        {
                            6, 429
                        }
                    },
                    new[,]
                    {
                        {
                            1, 149
                        },
                        {
                            2, 150
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // RANGER #3
                    new[,]
                    {
                        {
                            2, 450
                        },
                        {
                            3, -1
                        },
                        {
                            4, 385
                        },
                        {
                            5, 641
                        },
                        {
                            6, 429
                        }
                    },
                    new[,]
                    {
                        {
                            1, 149
                        },
                        {
                            2, 150
                        },
                        {
                            3, 4
                        }
                    })
            ]),
            new DefaultDesign( // THIEF
            [
                new DefaultSubDesign( // THIEF #1
                    new[,]
                    {
                        {
                            2, 452
                        },
                        {
                            3, -1
                        },
                        {
                            4, 371
                        },
                        {
                            5, 627
                        },
                        {
                            6, 434
                        }
                    },
                    new[,]
                    {
                        {
                            1, 189
                        },
                        {
                            2, 189
                        },
                        {
                            3, 39
                        }
                    })
            ]),
            new DefaultDesign( // CRAFTER
            [
                new DefaultSubDesign( // CRAFTER #1
                    new[,]
                    {
                        {
                            2, 461
                        },
                        {
                            3, 593
                        },
                        {
                            4, 369
                        },
                        {
                            5, 625
                        },
                        {
                            6, 432
                        }
                    },
                    new[,]
                    {
                        {
                            1, 77
                        },
                        {
                            2, 78
                        },
                        {
                            3, 4
                        }
                    })
            ]),
            new DefaultDesign( // HERBLORIST
            [
                new DefaultSubDesign( // HERBLORIST #1
                    new[,]
                    {
                        {
                            2, 446
                        },
                        {
                            3, -1
                        },
                        {
                            4, 374
                        },
                        {
                            5, 630
                        },
                        {
                            6, 429
                        }
                    },
                    new[,]
                    {
                        {
                            1, 109
                        },
                        {
                            2, 109
                        },
                        {
                            3, 4
                        }
                    })
            ])
        ];

        /// <summary>
        ///     Contains female default designs.
        /// </summary>
        public static readonly DefaultDesign[] FemaleDefaultDesigns =
        [
            new DefaultDesign( // ADVENTURER
            [
                new DefaultSubDesign( // ADVENTURER #1
                    new[,]
                    {
                        {
                            2, 586
                        },
                        {
                            3, 423
                        },
                        {
                            4, 535
                        },
                        {
                            5, 503
                        },
                        {
                            6, 553
                        }
                    },
                    new[,]
                    {
                        {
                            1, 60
                        },
                        {
                            2, 60
                        },
                        {
                            3, 155
                        }
                    }),
                new DefaultSubDesign( // ADVENTURER #2
                    new[,]
                    {
                        {
                            2, 584
                        },
                        {
                            3, 421
                        },
                        {
                            4, 533
                        },
                        {
                            5, 501
                        },
                        {
                            6, 553
                        }
                    },
                    new[,]
                    {
                        {
                            1, 52
                        },
                        {
                            2, 221
                        },
                        {
                            3, 155
                        }
                    }),
                new DefaultSubDesign( // ADVENTURER #3
                    new[,]
                    {
                        {
                            2, 585
                        },
                        {
                            3, 422
                        },
                        {
                            4, 534
                        },
                        {
                            5, 502
                        },
                        {
                            6, 554
                        }
                    },
                    new[,]
                    {
                        {
                            1, 60
                        },
                        {
                            2, 60
                        },
                        {
                            3, 155
                        }
                    })
            ]),
            new DefaultDesign( // WARRIOR
            [
                new DefaultSubDesign( // WARRIOR #1
                    new[,]
                    {
                        {
                            2, 562
                        },
                        {
                            3, -1
                        },
                        {
                            4, 523
                        },
                        {
                            5, 491
                        },
                        {
                            6, 551
                        }
                    },
                    new[,]
                    {
                        {
                            1, 204
                        },
                        {
                            2, 201
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // WARRIOR #2
                    new[,]
                    {
                        {
                            2, 563
                        },
                        {
                            3, -1
                        },
                        {
                            4, 524
                        },
                        {
                            5, 492
                        },
                        {
                            6, 551
                        }
                    },
                    new[,]
                    {
                        {
                            1, 204
                        },
                        {
                            2, 201
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // WARRIOR #3
                    new[,]
                    {
                        {
                            2, 564
                        },
                        {
                            3, -1
                        },
                        {
                            4, 525
                        },
                        {
                            5, 493
                        },
                        {
                            6, 551
                        }
                    },
                    new[,]
                    {
                        {
                            1, 204
                        },
                        {
                            2, 201
                        },
                        {
                            3, 4
                        }
                    })
            ]),
            new DefaultDesign( // MAGE
            [
                new DefaultSubDesign( // MAGE #1
                    new[,]
                    {
                        {
                            2, 581
                        },
                        {
                            3, 417
                        },
                        {
                            4, 529
                        },
                        {
                            5, 497
                        },
                        {
                            6, 551
                        }
                    },
                    new[,]
                    {
                        {
                            1, 132
                        },
                        {
                            2, 132
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // MAGE #2
                    new[,]
                    {
                        {
                            2, 582
                        },
                        {
                            3, 418
                        },
                        {
                            4, 530
                        },
                        {
                            5, 498
                        },
                        {
                            6, 551
                        }
                    },
                    new[,]
                    {
                        {
                            1, 132
                        },
                        {
                            2, 128
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // MAGE #3
                    new[,]
                    {
                        {
                            2, 557
                        },
                        {
                            3, -1
                        },
                        {
                            4, 531
                        },
                        {
                            5, 499
                        },
                        {
                            6, 551
                        }
                    },
                    new[,]
                    {
                        {
                            1, 132
                        },
                        {
                            2, 132
                        },
                        {
                            3, 4
                        }
                    })
            ]),
            new DefaultDesign( // RANGER
            [
                new DefaultSubDesign( // RANGER #1
                    new[,]
                    {
                        {
                            2, 579
                        },
                        {
                            3, 414
                        },
                        {
                            4, 526
                        },
                        {
                            5, 494
                        },
                        {
                            6, 551
                        }
                    },
                    new[,]
                    {
                        {
                            1, 156
                        },
                        {
                            2, 156
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // RANGER #2
                    new[,]
                    {
                        {
                            2, 559
                        },
                        {
                            3, -1
                        },
                        {
                            4, 527
                        },
                        {
                            5, 495
                        },
                        {
                            6, 551
                        }
                    },
                    new[,]
                    {
                        {
                            1, 149
                        },
                        {
                            2, 150
                        },
                        {
                            3, 4
                        }
                    }),
                new DefaultSubDesign( // RANGER #3
                    new[,]
                    {
                        {
                            2, 580
                        },
                        {
                            3, 416
                        },
                        {
                            4, 528
                        },
                        {
                            5, 496
                        },
                        {
                            6, 552
                        }
                    },
                    new[,]
                    {
                        {
                            1, 149
                        },
                        {
                            2, 150
                        },
                        {
                            3, 4
                        }
                    })
            ]),
            new DefaultDesign( // THIEF
            [
                new DefaultSubDesign( // THIEF #1
                    new[,]
                    {
                        {
                            2, 561
                        },
                        {
                            3, -1
                        },
                        {
                            4, 514
                        },
                        {
                            5, 482
                        },
                        {
                            6, 545
                        }
                    },
                    new[,]
                    {
                        {
                            1, 189
                        },
                        {
                            2, 189
                        },
                        {
                            3, 39
                        }
                    })
            ]),
            new DefaultDesign( // CRAFTER
            [
                new DefaultSubDesign( // CRAFTER #1
                    new[,]
                    {
                        {
                            2, 570
                        },
                        {
                            3, 400
                        },
                        {
                            4, 512
                        },
                        {
                            5, 480
                        },
                        {
                            6, 543
                        }
                    },
                    new[,]
                    {
                        {
                            1, 77
                        },
                        {
                            2, 78
                        },
                        {
                            3, 4
                        }
                    })
            ]),
            new DefaultDesign( // HERBLORIST
            [
                new DefaultSubDesign( // HERBLORIST #1
                    new[,]
                    {
                        {
                            2, 574
                        },
                        {
                            3, 405
                        },
                        {
                            4, 517
                        },
                        {
                            5, 485
                        },
                        {
                            6, 548
                        }
                    },
                    new[,]
                    {
                        {
                            1, 109
                        },
                        {
                            2, 109
                        },
                        {
                            3, 4
                        }
                    })
            ])
        ];

        /// <summary>
        ///     Contains MALE style amulets.
        /// </summary>
        public static readonly int[] MaleAmulets =
        [
            19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713
        ];

        /// <summary>
        ///     Contains FEMALE style amulets.
        /// </summary>
        public static readonly int[] FemaleAmulets =
        [
            19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713, 19713
        ];

        /// <summary>
        ///     Contains MALE style weapons.
        /// </summary>
        public static readonly int[] MaleWeapons =
        [
            -1, 19730, 19714, 19726, -1, 19716, 19734, 19723, 19715, 19731, 19732, 19725, 19724, 19717, 19729, 19716, 19719, 19720, 19734, 19733
        ];

        /// <summary>
        ///     Contains MALE style shields.
        /// </summary>
        public static readonly int[] MaleShields =
        [
            -1, -1, -1, 19727, -1, -1, 19721, 19722, -1, -1, -1, -1, -1, -1, -1, -1, 19718, 19721, -1, -1
        ];

        /// <summary>
        ///     Contains FEMALE style weapons.
        /// </summary>
        public static readonly int[] FemaleWeapons =
        [
            -1, 19730, 19714, 19726, 19728, -1, -1, 19723, 19715, 19731, 19732, 19725, 19724, 19717, 19729, 19716, 19719, 19720, 19734, 19733
        ];

        /// <summary>
        ///     Contains FEMALE style shields.
        /// </summary>
        public static readonly int[] FemaleShields =
        [
            -1, -1, -1, 19727, -1, -1, -1, 19722, -1, -1, -1, -1, -1, -1, -1, -1, 19718, 19721, -1, -1
        ];
    }


    /// <summary>
    ///     Design class.
    /// </summary>
    public class DefaultDesign
    {
        /// <summary>
        ///     Contains sub designs.
        /// </summary>
        public DefaultSubDesign[] SubDesigns { get; }

        /// <summary>
        ///     Construct's new default design instance.
        /// </summary>
        /// <param name="subDesigns">The sub designs.</param>
        public DefaultDesign(DefaultSubDesign[] subDesigns) => SubDesigns = subDesigns;
    }

    /// <summary>
    ///     Subdesign class.
    /// </summary>
    public class DefaultSubDesign
    {
        /// <summary>
        ///     Contains body looks.
        /// </summary>
        public int[,] Looks { get; }

        /// <summary>
        ///     Contains colors.
        /// </summary>
        public int[,] Colours { get; }

        /// <summary>
        ///     Construct's new DefaultSubDesign instance.
        /// </summary>
        /// <param name="looks">The looks.</param>
        /// <param name="colours">The colours.</param>
        public DefaultSubDesign(int[,] looks, int[,] colours)
        {
            Looks = looks;
            Colours = colours;
        }
    }
}