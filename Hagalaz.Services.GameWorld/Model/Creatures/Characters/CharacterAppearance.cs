using System;
using System.Collections.Generic;
using Hagalaz.Game.Extensions;
using System.Linq;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Data;
using Hagalaz.Game.Abstractions.Factories;
using Hagalaz.Game.Abstractions.Logic.Dehydrations;
using Hagalaz.Game.Abstractions.Logic.Hydrations;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Items;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Services.GameWorld.Logic.Characters.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Class which represents character appearance.
    /// </summary>
    public class CharacterAppearance : ICharacterAppearance, IHydratable<HydratedAppearanceDto>, IDehydratable<HydratedAppearanceDto>,
        IHydratable<HydratedItemAppearanceCollectionDto>, IDehydratable<HydratedItemAppearanceCollectionDto>
    {
        /// <summary>
        /// Contains owner of this class.
        /// </summary>
        private readonly ICharacter _owner;

        /// <summary>
        /// Contains character body parts.
        /// </summary>
        private readonly int[] _drawnBodyParts;

        /// <summary>
        /// Contains character colours.
        /// </summary>
        private readonly int[] _colors;

        /// <summary>
        /// Contains character look.
        /// </summary>
        private readonly int[] _look;

        /// <summary>
        /// Contains the characters item appearances.
        /// </summary>
        private readonly IItemPart?[] _drawnItemParts;

        /// <summary>
        /// The all item parts
        /// </summary>
        private readonly Dictionary<int, IItemPart> _allItemParts = new();

        /// <summary>
        /// The NPC definitionRepository
        /// </summary>
        private readonly INpcService _npcDefinitionRepository;

        /// <summary>
        /// 
        /// </summary>
        private readonly ICharacterNpcScriptProvider _characterNpcScriptProvider;

        /// <summary>
        /// 
        /// </summary>
        private readonly IBodyDataRepository _bodyDataRepository;

        /// <summary>
        /// Contains boolean if character is visible.
        /// </summary>
        private bool _visible = true;

        /// <summary>
        /// Contains character render Id.
        /// </summary>
        private int _renderId;

        /// <summary>
        /// Contains this character prayer icon.
        /// </summary>
        private PrayerIcon _prayerIcon = PrayerIcon.None;

        /// <summary>
        /// Contains this character skull icon.
        /// </summary>
        private SkullIcon _skullIcon = SkullIcon.None;

        /// <summary>
        /// Contains the display title.
        /// </summary>
        private DisplayTitle _displayTitle = DisplayTitle.None;

        /// <summary>
        /// Contains character gender.
        /// </summary>
        private bool _female;

        /// <summary>
        /// Contains character render Id.
        /// </summary>
        public int RenderId
        {
            get => _renderId;
            set
            {
                if (_renderId != value)
                {
                    _renderId = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Contains boolean if character is visible.
        /// </summary>
        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible != value)
                {
                    _visible = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Contains Id of the npc this character is turned to 
        /// or -1 if character is not NPC.
        /// </summary>
        public int NpcId { get; private set; }

        /// <summary>
        /// Contains PlayerNPCScript , if NPCID is -1 then 
        /// PNPCScript == null.
        /// </summary>
        public ICharacterNpcScript PnpcScript { get; private set; }

        /// <summary>
        /// Contains this character prayer icon.
        /// </summary>
        public PrayerIcon PrayerIcon
        {
            get => _prayerIcon;
            set
            {
                if (_prayerIcon != value)
                {
                    _prayerIcon = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Contains this character skull icon.
        /// </summary>
        public SkullIcon SkullIcon
        {
            get => _skullIcon;
            set
            {
                if (_skullIcon != value)
                {
                    _skullIcon = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Contains character gender.
        /// </summary>
        public bool Female
        {
            get => _female;
            set
            {
                if (_female != value)
                {
                    _female = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Contains the display title.
        /// </summary>
        public DisplayTitle DisplayTitle
        {
            get => _displayTitle;
            set
            {
                if (_displayTitle != value)
                {
                    _displayTitle = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Contains size of this character.
        /// </summary>
        public int Size { get; private set; }

        /// <summary>
        /// Construct's new appearance class.
        /// </summary>
        /// <param name="owner"></param>
        public CharacterAppearance(ICharacter owner)
        {
            _npcDefinitionRepository = owner.ServiceProvider.GetRequiredService<INpcService>();
            _bodyDataRepository = owner.ServiceProvider.GetRequiredService<IBodyDataRepository>();
            _characterNpcScriptProvider = owner.ServiceProvider.GetRequiredService<ICharacterNpcScriptProvider>();
            _owner = owner;
            Size = 1;
            NpcId = -1;
            _drawnBodyParts = new int[_bodyDataRepository.BodySlotCount];
            _drawnItemParts = new IItemPart[_bodyDataRepository.BodySlotCount];
            _colors = new int[10];
            _look = new int[7];
        }

        /// <summary>
        /// Turn's character into NPC.
        /// </summary>
        /// <param name="npcId">Id of the npc to which this player should be turned to.</param>
        /// <param name="pnpcScript">The PNPC script.</param>
        public void TurnToNpc(int npcId, ICharacterNpcScript? pnpcScript = null)
        {
            NpcId = npcId;
            Size = _npcDefinitionRepository.FindNpcDefinitionById(npcId).Size;
            PnpcScript = pnpcScript ?? (ICharacterNpcScript)_owner.ServiceProvider.GetRequiredService(_characterNpcScriptProvider.GetCharacterNpcScriptTypeById(npcId));
            PnpcScript.Initialize(_owner);
            PnpcScript.OnTurnToNpc();
            DrawCharacter();
        }

        /// <summary>
        /// Turn's character back into player.
        /// </summary>
        public void TurnToPlayer()
        {
            if (!this.IsTransformedToNpc())
                return;
            var script = PnpcScript;
            PnpcScript = null!;
            NpcId = -1;
            script?.OnTurnToPlayer();
            Size = 1;
            DrawCharacter();
        }

        /// <summary>
        /// Resets the render Id.
        /// </summary>
        public void ResetRenderID()
        {
            var weapon = _owner.Equipment[EquipmentSlot.Weapon];
            RenderId = weapon != null ? weapon.ItemDefinition.RenderAnimationId != -1 ? weapon.ItemDefinition.RenderAnimationId : 1426 : 1426;
        }

        /// <summary>
        /// Draw's item on character's body part,
        /// throws exception if that body part is not accessible.
        /// </summary>
        /// <param name="part">Part of the body to draw.</param>
        /// <param name="itemId">Id of the item to draw.</param>
        /// <exception cref="Exception">
        /// Inaccessible part!
        /// </exception>
        public void DrawItem(BodyPart part, int itemId)
        {
            if (_bodyDataRepository.IsDisabledSlot(part))
                throw new ArgumentException("Inaccessible part!", nameof(part));
            _drawnBodyParts[(int)part] = itemId + 0x4000;
            if (_allItemParts.TryGetValue(itemId, out var value))
            {
                _drawnItemParts[(int)part] = value;
            }
            Refresh();
        }

        /// <summary>
        /// Draws the item on body.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="itemPart">The item appearance.</param>
        /// <exception cref="Exception">
        /// Inaccessible part!
        /// </exception>
        public void DrawItem(BodyPart part, IItemPart itemPart)
        {
            if (_bodyDataRepository.IsDisabledSlot(part))
                throw new Exception("Inaccessible part!");
            _drawnBodyParts[(int)part] = itemPart.ItemId + 0x4000;
            _drawnItemParts[(int)part] = itemPart;
            Refresh();
        }

        /// <summary>
        /// Draw's clothes on character's body part,
        /// throws exception if that body part is not accessible.
        /// </summary>
        /// <param name="part">Part of the body to draw.</param>
        /// <param name="clothesID">Clothes data.</param>
        public void DrawCloth(BodyPart part, int clothesID)
        {
            if (_bodyDataRepository.IsDisabledSlot(part))
                throw new Exception("Inaccessible part!");
            _drawnBodyParts[(int)part] = clothesID + 0x100;
            _drawnItemParts[(int)part] = null;
            Refresh();
        }

        public IItemPart? GetDrawnItemPart(BodyPart part) => _drawnItemParts[(int)part];

        /// <summary>
        /// Clear's drawing on given body part.
        /// </summary>
        /// <param name="part">Part which should be cleared.</param>
        public void ClearDrawnBodyPart(BodyPart part)
        {
            _drawnBodyParts[(int)part] = 0;
            _drawnItemParts[(int)part] = null;
            Refresh();
        }

        /// <summary>
        /// Get's body part.
        /// </summary>
        /// <param name="part">Part of the body to get.</param>
        /// <returns></returns>
        public int GetDrawnBodyPart(BodyPart part) => _drawnBodyParts[(int)part];

        public IItemPart GetOrAddItemPart(int itemId)
        {
            if (_allItemParts.TryGetValue(itemId, out var value))
            {
                return value;
            }

            value = _owner.ServiceProvider.GetRequiredService<IItemPartFactory>().Create(itemId);
            _allItemParts.Add(itemId, value);
            return value;
        }

        public void SetItemPart(int itemId, IItemPart itemPart) => _allItemParts[itemPart.ItemId] = itemPart;

        /// <summary>
        /// Get's character current colorID for given type. 
        /// Throws exception if type is wrong.
        /// </summary>
        /// <param name="type">Type of the color.</param>
        /// <returns></returns>
        public int GetColor(ColorType type)
        {
            if ((int)type < 0 || (int)type >= 10)
                throw new Exception("Wrong color type!");
            return _colors[(int)type];
        }

        /// <summary>
        /// Set's character color.
        /// Throws exception if type is wrong.
        /// </summary>
        /// <param name="type">Type of the color.</param>
        /// <param name="value">Color value.</param>
        public void SetColor(ColorType type, int value)
        {
            if ((int)type < 0 || (int)type >= 10)
                throw new Exception("Wrong color type!");
            _colors[(int)type] = value;
            Refresh();
        }

        /// <summary>
        /// Refreshe's character appereance.
        /// </summary>
        public void Refresh()
        {
            _owner.RenderInformation.ScheduleFlagUpdate(UpdateFlags.Appearance);
            _owner.RenderInformation.ScheduleItemAppearanceUpdate();
        }

        /// <summary>
        /// Set's character look.
        /// </summary>
        /// <param name="type">Type of the look to set.</param>
        /// <param name="look">Look value which should be set.</param>
        public void SetLook(LookType type, int look)
        {
            if ((int)type < 0 || (int)type >= _look.Length)
                throw new Exception("Wrong look type.");
            _look[(int)type] = look;
        }

        /// <summary>
        /// Get's character specific type look.
        /// </summary>
        /// <param name="type">Type of the look to get.</param>
        /// <returns>Look type or throws exception if type is invalid.</returns>
        public int GetLook(LookType type)
        {
            if ((int)type < 0 || (int)type >= _look.Length)
                throw new Exception("Wrong look type.");
            return _look[(int)type];
        }

        /// <summary>
        /// (RE)Draws character,
        /// Draws all worn items and clotches.
        /// </summary>
        public void DrawCharacter()
        {
            if (NpcId != -1)
            {
                RenderId = _npcDefinitionRepository.FindNpcDefinitionById(NpcId).RenderId;
                return;
            }

            var chest = _owner.Equipment[EquipmentSlot.Chest];
            var shield = _owner.Equipment[EquipmentSlot.Shield];
            var legs = _owner.Equipment[EquipmentSlot.Legs];
            var hat = _owner.Equipment[EquipmentSlot.Hat];
            var hands = _owner.Equipment[EquipmentSlot.Hands];
            var feet = _owner.Equipment[EquipmentSlot.Feet];
            var aura = _owner.Equipment[EquipmentSlot.Aura];
            var cape = _owner.Equipment[EquipmentSlot.Cape];
            var amulet = _owner.Equipment[EquipmentSlot.Amulet];
            var weapon = _owner.Equipment[EquipmentSlot.Weapon];

            if (hat != null)
                DrawItem(BodyPart.HatPart, hat.Id);
            else
                ClearDrawnBodyPart(BodyPart.HatPart);

            if (cape != null)
                DrawItem(BodyPart.CapePart, cape.Id);
            else
                ClearDrawnBodyPart(BodyPart.CapePart);

            if (amulet != null)
                DrawItem(BodyPart.AmuletPart, amulet.Id);
            else
                ClearDrawnBodyPart(BodyPart.AmuletPart);

            if (weapon != null)
                DrawItem(BodyPart.WeaponPart, weapon.Id);
            else
                ClearDrawnBodyPart(BodyPart.WeaponPart);

            if (chest != null)
                DrawItem(BodyPart.ChestPart, chest.Id);
            else
                DrawCloth(BodyPart.ChestPart, GetLook(LookType.TorsoLook));

            if (shield != null)
                DrawItem(BodyPart.ShieldPart, shield.Id);
            else
                ClearDrawnBodyPart(BodyPart.ShieldPart);

            if (chest != null && chest.EquipmentDefinition.Type == EquipmentType.FullBody)
                ClearDrawnBodyPart(BodyPart.ArmsPart);
            else
                DrawCloth(BodyPart.ArmsPart, GetLook(LookType.ArmsLook));

            if (legs != null)
                DrawItem(BodyPart.LegsPart, legs.Id);
            else
                DrawCloth(BodyPart.LegsPart, GetLook(LookType.LegsLook));

            if (hat != null && (hat.EquipmentDefinition.Type == EquipmentType.FullMask || hat.EquipmentDefinition.Type == EquipmentType.FullHat))
                ClearDrawnBodyPart(BodyPart.HairPart);
            else
                DrawCloth(BodyPart.HairPart, GetLook(LookType.HairLook));

            if (hands != null)
                DrawItem(BodyPart.HandsPart, hands.Id);
            else
                DrawCloth(BodyPart.HandsPart, GetLook(LookType.WristLook));

            if (feet != null)
                DrawItem(BodyPart.FeetPart, feet.Id);
            else
                DrawCloth(BodyPart.FeetPart, GetLook(LookType.FeetLook));

            if (hat != null && hat.EquipmentDefinition.Type == EquipmentType.FullMask)
                ClearDrawnBodyPart(BodyPart.BeardPart);
            else
                DrawCloth(BodyPart.BeardPart, GetLook(LookType.BeardLook));

            if (aura != null)
                DrawItem(BodyPart.AuraPart, aura.Id);
            else
                ClearDrawnBodyPart(BodyPart.AuraPart);

            RenderId = weapon != null ? weapon.ItemDefinition.RenderAnimationId != -1 ? weapon.ItemDefinition.RenderAnimationId : 1426 : 1426;
        }

        public void Hydrate(HydratedAppearanceDto hydration)
        {
            _female = hydration.Gender == 1;
            SetColor(ColorType.TorsoColor, hydration.TorsoColor);
            SetColor(ColorType.SkinColor, hydration.SkinColor);
            SetColor(ColorType.HairColor, hydration.HairColor);
            SetColor(ColorType.LegColor, hydration.LegColor);
            SetColor(ColorType.FeetColor, hydration.FeetColor);
            SetLook(LookType.TorsoLook, hydration.TorsoLook);
            SetLook(LookType.FeetLook, hydration.FeetLook);
            SetLook(LookType.LegsLook, hydration.LegsLook);
            SetLook(LookType.HairLook, hydration.HairLook);
            SetLook(LookType.WristLook, hydration.WristLook);
            SetLook(LookType.ArmsLook, hydration.ArmsLook);
            DisplayTitle = hydration.DisplayTitle;
        }

        public HydratedAppearanceDto Dehydrate() => new()
        {
            Gender = _female ? 1 : 0,
            HairColor = GetColor(ColorType.HairColor),
            TorsoColor = GetColor(ColorType.TorsoColor),
            LegColor = GetColor(ColorType.LegColor),
            FeetColor = GetColor(ColorType.FeetColor),
            SkinColor = GetColor(ColorType.SkinColor),
            HairLook = GetLook(LookType.HairLook),
            BeardLook = GetLook(LookType.BeardLook),
            TorsoLook = GetLook(LookType.TorsoLook),
            ArmsLook = GetLook(LookType.ArmsLook),
            WristLook = GetLook(LookType.WristLook),
            LegsLook = GetLook(LookType.LegsLook),
            FeetLook = GetLook(LookType.FeetLook),
            DisplayTitle = _displayTitle
        };

        public void Hydrate(HydratedItemAppearanceCollectionDto hydration)
        {
            var itemPartFactory = _owner.ServiceProvider.GetRequiredService<IItemPartFactory>();
            foreach (var item in hydration.Appearances)
            {
                var itemPart = itemPartFactory.Create(item.Id);
                if (itemPart is IHydratable<HydratedItemAppearanceDto> itemPartHydratable)
                {
                    itemPartHydratable.Hydrate(item);
                }
                _allItemParts.Add(item.Id, itemPart);
            }
        }

        HydratedItemAppearanceCollectionDto IDehydratable<HydratedItemAppearanceCollectionDto>.Dehydrate() => new()
        {
            Appearances = _allItemParts.Values.Select(item =>
            {
                if (item is IDehydratable<HydratedItemAppearanceDto> itemDehydratable)
                {
                    return itemDehydratable.Dehydrate();
                }
                return new HydratedItemAppearanceDto
                {
                    Id = item.ItemId,
                    MaleModels = item.MaleModels,
                    FemaleModels = item.FemaleModels,
                    ModelColors = item.ModelColors,
                    TextureColors = item.TextureColors
                };
            }).ToList()
        };
    }
}