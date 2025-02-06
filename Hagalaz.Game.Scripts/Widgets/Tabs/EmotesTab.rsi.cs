using Hagalaz.Game.Abstractions.Builders.Audio;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Collections;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Maps.PathFinding;
using Hagalaz.Game.Abstractions.Model.Widgets;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common;
using Hagalaz.Game.Scripts.Model.Widgets;
using Hagalaz.Game.Scripts.Tasks;

namespace Hagalaz.Game.Scripts.Widgets.Tabs
{
    /// <summary>
    ///     Represents the emote tab.
    /// </summary>
    public class EmotesTab : WidgetScript
    {
        private readonly IAudioBuilder _soundBuilder;
        private readonly ISimplePathFinder _dumbPathFinder;
        private readonly INpcBuilder _npcBuilder;

        public EmotesTab(ICharacterContextAccessor characterContextAccessor, IAudioBuilder soundBuilder, ISimplePathFinder dumbPathFinder, INpcBuilder npcBuilder) : base(characterContextAccessor)
        {
            _soundBuilder = soundBuilder;
            _dumbPathFinder = dumbPathFinder;
            _npcBuilder = npcBuilder;
        }

        /// <summary>
        ///     Happens when interface is opened for character.
        /// </summary>
        public override void OnOpen()
        {
            InterfaceInstance.SetOptions(8, 0, 118, 6);
            InterfaceInstance.SetOptions(13, 0, 11, 2); // filtering
            Owner.Configurations.SendCs2Script(4717, [38666248, 38666247, 38666249, 3874]);

            // unlock emotes
            Owner.Configurations.SendStandardConfiguration(313, -1);
            Owner.Configurations.SendStandardConfiguration(802, -1);
            Owner.Configurations.SendStandardConfiguration(1085, 249852); // zombie hand
            Owner.Configurations.SendStandardConfiguration(1597, -1);
            Owner.Configurations.SendStandardConfiguration(1842, -1);
            Owner.Configurations.SendStandardConfiguration(1921, -893736236); // puppet master
            Owner.Configurations.SendStandardConfiguration(2033, -1);
            Owner.Configurations.SendStandardConfiguration(1958, 534); // taskmaster
            Owner.Configurations.SendStandardConfiguration(465, -1);
            Owner.Configurations.SendStandardConfiguration(1404, -1);
            Owner.Configurations.SendStandardConfiguration(1669, -1);
            Owner.Configurations.SendStandardConfiguration(2169, -1);
            Owner.Configurations.SendStandardConfiguration(2230, -1); // loyalty emotes
            Owner.Configurations.SendStandardConfiguration(2432, -1); // troubadour dance
            Owner.Configurations.SendStandardConfiguration(2405, -1); // living on borrowed time
            Owner.Configurations.SendStandardConfiguration(2458, -1); // chaotic cookery
            
            InterfaceInstance.AttachClickHandler(8, (componentID, clickType, extraData1, extraData2) =>
            {
                if (clickType != ComponentClickType.LeftClick)
                {
                    return false;
                }

                Owner.Interrupt(this);

                switch (extraData2)
                {
                    case 0:
                        Owner.QueueAnimation(Animation.Create(855));
                        break;
                    case 1:
                        Owner.QueueAnimation(Animation.Create(856));
                        break;
                    case 2:
                        Owner.QueueAnimation(Animation.Create(858));
                        break;
                    case 3:
                        Owner.QueueAnimation(Animation.Create(859));
                        break;
                    case 4:
                        Owner.QueueAnimation(Animation.Create(857));
                        break;
                    case 5:
                        Owner.QueueAnimation(Animation.Create(863));
                        break;
                    case 6:
                        Owner.QueueAnimation(Animation.Create(2113));
                        break;
                    case 7:
                        Owner.QueueAnimation(Animation.Create(862));
                        break;
                    case 8:
                        Owner.QueueAnimation(Animation.Create(864));
                        break;
                    case 9:
                        Owner.QueueAnimation(Animation.Create(861));
                        break;
                    case 10:
                        Owner.QueueAnimation(Animation.Create(2109));
                        break;
                    case 11:
                        Owner.QueueAnimation(Animation.Create(2111));
                        break;
                    case 12:
                        Owner.QueueAnimation(Animation.Create(866));
                        break;
                    case 13:
                        Owner.QueueAnimation(Animation.Create(2106));
                        break;
                    case 14:
                        Owner.QueueAnimation(Animation.Create(2107));
                        break;
                    case 15:
                        Owner.QueueAnimation(Animation.Create(2108));
                        break;
                    case 16:
                        Owner.QueueAnimation(Animation.Create(860));
                        break;
                    case 17:
                        Owner.QueueGraphic(Graphic.Create(1702));
                        Owner.QueueAnimation(Animation.Create(1374));
                        break;
                    case 18:
                        Owner.QueueAnimation(Animation.Create(2105));
                        break;
                    case 19:
                        Owner.QueueAnimation(Animation.Create(2110));
                        break;
                    case 20:
                        Owner.QueueAnimation(Animation.Create(865));
                        break;
                    case 21:
                        Owner.QueueAnimation(Animation.Create(2112));
                        break;
                    case 22:
                        Owner.QueueAnimation(Animation.Create(0x84F));
                        break;
                    case 23:
                        Owner.QueueAnimation(Animation.Create(0x850));
                        break;
                    case 24:
                        Owner.QueueAnimation(Animation.Create(1131));
                        break;
                    case 25:
                        Owner.QueueAnimation(Animation.Create(1130));
                        break;
                    case 26:
                        Owner.QueueAnimation(Animation.Create(1129));
                        break;
                    case 27:
                        Owner.QueueAnimation(Animation.Create(1128));
                        break;
                    case 28:
                        Owner.QueueAnimation(Animation.Create(4275));
                        break;
                    case 29:
                        Owner.QueueAnimation(Animation.Create(1745));
                        break;
                    case 30:
                        Owner.QueueAnimation(Animation.Create(4280));
                        break;
                    case 31:
                        Owner.QueueAnimation(Animation.Create(4276));
                        break;
                    case 32:
                        Owner.QueueAnimation(Animation.Create(3544));
                        break;
                    case 33:
                        Owner.QueueAnimation(Animation.Create(3543));
                        break;
                    case 34:
                        Owner.QueueGraphic(Graphic.Create(1244));
                        Owner.QueueAnimation(Animation.Create(7272));
                        break;
                    case 35:
                        Owner.QueueAnimation(Animation.Create(2836));
                        break;
                    case 36:
                        Owner.QueueAnimation(Animation.Create(6111));
                        break;
                    case 37: // Skillcapes
                        var cape = Owner.Equipment[EquipmentSlot.Cape];
                        if (cape == null)
                        {
                            Owner.SendChatMessage("You need to be wearing a skillcape in order to perform this emote.");
                            return true;
                        }

                        switch (cape.Id)
                        {
                            case 9747:
                            case 9748:
                            case 10639: // Attack cape
                                Owner.QueueAnimation(Animation.Create(4959));
                                Owner.QueueGraphic(Graphic.Create(823));
                                break;
                            case 9753:
                            case 9754:
                            case 10641: // Defence cape
                                Owner.QueueAnimation(Animation.Create(4961));
                                Owner.QueueGraphic(Graphic.Create(824));
                                break;
                            case 9750:
                            case 9751:
                            case 10640: // Strength cape
                                Owner.QueueAnimation(Animation.Create(4981));
                                Owner.QueueGraphic(Graphic.Create(828));
                                break;
                            case 9768:
                            case 9769:
                            case 10647: // Hitpoints cape
                                Owner.QueueAnimation(Animation.Create(14242));
                                Owner.QueueGraphic(Graphic.Create(2745));
                                break;
                            case 9756:
                            case 9757:
                            case 10642: // Ranged cape
                                Owner.QueueAnimation(Animation.Create(4973));
                                Owner.QueueGraphic(Graphic.Create(832));
                                break;
                            case 9762:
                            case 9763:
                            case 10644: // Magic cape
                                Owner.QueueAnimation(Animation.Create(4939));
                                Owner.QueueGraphic(Graphic.Create(813));
                                break;
                            case 9759:
                            case 9760:
                            case 10643: // Prayer cape
                                Owner.QueueAnimation(Animation.Create(4979));
                                Owner.QueueGraphic(Graphic.Create(829));
                                break;
                            case 9801:
                            case 9802:
                            case 10658: // Cooking cape
                                Owner.QueueAnimation(Animation.Create(4955));
                                Owner.QueueGraphic(Graphic.Create(821));
                                break;
                            case 9807:
                            case 9808:
                            case 10660: // Woodcutting cape
                                Owner.QueueAnimation(Animation.Create(4957));
                                Owner.QueueGraphic(Graphic.Create(822));
                                break;
                            case 9783:
                            case 9784:
                            case 10652: // Fletching cape
                                Owner.QueueAnimation(Animation.Create(4937));
                                Owner.QueueGraphic(Graphic.Create(812));
                                break;
                            case 9798:
                            case 9799:
                            case 10657: // Fishing cape
                                Owner.QueueAnimation(Animation.Create(4951));
                                Owner.QueueGraphic(Graphic.Create(819));
                                break;
                            case 9804:
                            case 9805:
                            case 10659: // Firemaking cape
                                Owner.QueueAnimation(Animation.Create(4975));
                                Owner.QueueGraphic(Graphic.Create(831));
                                break;
                            case 9780:
                            case 9781:
                            case 10651: // Crafting cape
                                Owner.QueueAnimation(Animation.Create(4949));
                                Owner.QueueGraphic(Graphic.Create(818));
                                break;
                            case 9795:
                            case 9796:
                            case 10656: // Smithing cape
                                Owner.QueueAnimation(Animation.Create(4943));
                                Owner.QueueGraphic(Graphic.Create(815));
                                break;
                            case 9792:
                            case 9793:
                            case 10655: // Mining cape
                                Owner.QueueAnimation(Animation.Create(4941));
                                Owner.QueueGraphic(Graphic.Create(814));
                                break;
                            case 9774:
                            case 9775:
                            case 10649: // Herblore cape
                                Owner.QueueAnimation(Animation.Create(4969));
                                Owner.QueueGraphic(Graphic.Create(835));
                                break;
                            case 9771:
                            case 9772:
                            case 10648: // Agility cape
                                Owner.QueueAnimation(Animation.Create(4977));
                                Owner.QueueGraphic(Graphic.Create(830));
                                break;
                            case 9777:
                            case 9778:
                            case 10650: // Thieving cape
                                Owner.QueueAnimation(Animation.Create(4965));
                                Owner.QueueGraphic(Graphic.Create(826));
                                break;
                            case 9786:
                            case 9787:
                            case 10653: // Slayer cape
                                Owner.QueueAnimation(Animation.Create(4967));
                                Owner.QueueGraphic(Graphic.Create(1656));
                                break;
                            case 9810:
                            case 9811:
                            case 10611: // Farming cape
                                Owner.QueueAnimation(Animation.Create(4963));
                                Owner.QueueGraphic(Graphic.Create(825));
                                break;
                            case 9765:
                            case 9766:
                            case 10645: // Runecrafting cape
                                Owner.QueueAnimation(Animation.Create(4947));
                                Owner.QueueGraphic(Graphic.Create(817));
                                break;
                            case 9789:
                            case 9790:
                            case 10654: // Construction cape
                                Owner.QueueAnimation(Animation.Create(4953));
                                Owner.QueueGraphic(Graphic.Create(820));
                                break;
                            case 12169:
                            case 12170:
                            case 12524: // Summoning cape
                                Owner.QueueAnimation(Animation.Create(8525));
                                Owner.QueueGraphic(Graphic.Create(1515));
                                break;
                            case 9948:
                            case 9949:
                            case 10646: // Hunter cape
                                Owner.QueueAnimation(Animation.Create(5158));
                                Owner.QueueGraphic(Graphic.Create(907));
                                break;
                            case 9813:
                            case 10662: // Quest cape
                                Owner.QueueAnimation(Animation.Create(4945));
                                Owner.QueueGraphic(Graphic.Create(816));
                                break;

                            case 20763: // Veteran cape
                                if (!_dumbPathFinder.CheckStep(Owner.Location, 1))
                                {
                                    Owner.SendChatMessage("There is not enough space around you to perform this emote.");
                                    return true;
                                }

                                Owner.QueueAnimation(Animation.Create(352));
                                Owner.QueueGraphic(Graphic.Create(1446));
                                break;

                            case 20765: // Classic cape
                                var random = RandomStatic.Generator.Next(0, 2);
                                Owner.QueueAnimation(Animation.Create(122));
                                Owner.QueueGraphic(Graphic.Create(random == 0 ? 1471 : 1466));
                                break;

                            case 20767: // Max cape
                                if (!_dumbPathFinder.CheckStep(Owner.Location, Owner.FaceDirection) || !_dumbPathFinder.CheckStep(Owner.Location, 2))
                                {
                                    Owner.SendChatMessage("There is not enough space around you to perform this emote.");
                                    return true;
                                }

                                var dragonNpc = _npcBuilder.Create().WithId(1224).WithLocation(Location.Create((short)(Owner.Location.X + 1), Owner.Location.Y, Owner.Location.Z, Owner.Location.Dimension)).Build();
                                Owner.QueueTask(new MaxCapeTask(Owner, dragonNpc));
                                break;

                            case 20769: // Completionist cape
                            case 20771:
                                if (!_dumbPathFinder.CheckStep(Owner.Location, 2))
                                {
                                    Owner.SendChatMessage("There is not enough space around you to perform this emote.");
                                    return true;
                                }

                                var id = cape.Id == 20769 ? (short)1830 : (short)3372;
                                Owner.QueueTask(new CompletionistCapeTask(Owner, id, () =>
                                {
                                    Owner.Configurations.SendResetCameraShake();
                                    Owner.Appearance.TurnToPlayer();
                                    Owner.QueueAnimation(Animation.Create(1175));
                                }));
                                break;

                            default:
                                Owner.SendChatMessage("You need to be wearing a skillcape in order to perform this emote.");
                                return true;
                        }

                        break;
                    case 38:
                        Owner.QueueAnimation(Animation.Create(7531));
                        break;
                    case 39:
                        Owner.QueueGraphic(Graphic.Create(1537));
                        Owner.QueueAnimation(Animation.Create(7531));
                        var musicEffect = _soundBuilder.Create().AsMusicEffect().WithId(302).Build();
                        Owner.Session.SendMessage(musicEffect.ToMessage());
                        break;
                    case 40:
                        Owner.QueueGraphic(Graphic.Create(1553));
                        Owner.QueueAnimation(Animation.Create(8770));
                        break;
                    case 41:
                        Owner.QueueGraphic(Graphic.Create(1734));
                        Owner.QueueAnimation(Animation.Create(9990));
                        break;
                    case 42:
                        Owner.QueueGraphic(Graphic.Create(1864));
                        Owner.QueueAnimation(Animation.Create(10530));
                        break;
                    case 43:
                        Owner.QueueGraphic(Graphic.Create(1973));
                        Owner.QueueAnimation(Animation.Create(11044));
                        break;
                    case 44: // Give thanks
                        Owner.QueueTask(new GiveThanksTask(Owner, () =>
                        {
                            Owner.QueueGraphic(Graphic.Create(86));
                            Owner.QueueAnimation(Animation.Create(10995));
                            Owner.Appearance.TurnToPlayer();
                        }));
                        break;
                    case 45:
                        Owner.QueueGraphic(Graphic.Create(2037));
                        Owner.QueueAnimation(Animation.Create(11542));
                        break;
                    case 46: // Dramatic point
                        break;
                    case 47: // Faint
                        break;
                    case 48:
                        Owner.QueueGraphic(Graphic.Create(2837));
                        Owner.QueueAnimation(Animation.Create(14869));
                        break;
                    case 49: // Task master
                        break;
                    case 50: // Seal of approval
                        Owner.QueueTask(new SealOfApprovalTask(Owner, () =>
                        {
                            Owner.QueueGraphic(Graphic.Create(1287));
                            Owner.QueueAnimation(Animation.Create(15105));
                            Owner.Appearance.TurnToPlayer();
                        }));
                        break;

                    // holyday emotes
                    case 63: // invoke spring
                        Owner.QueueGraphic(Graphic.Create(1391));
                        Owner.QueueAnimation(Animation.Create(15357));
                        break;

                    // loyality emotes
                    case 51: // catfight
                        Owner.QueueAnimation(Animation.Create(2252));
                        break;
                    case 52: // talk to the hand
                        Owner.QueueAnimation(Animation.Create(2416));
                        break;
                    case 53: // shake hands
                        Owner.QueueAnimation(Animation.Create(2303));
                        break;
                    case 54: // high five
                        Owner.QueueAnimation(Animation.Create(2312));
                        break;
                    case 55: // face palm
                        Owner.QueueAnimation(Animation.Create(2254));
                        break;
                    case 56: // surrender
                        Owner.QueueAnimation(Animation.Create(2360));
                        break;
                    case 57: // levitate
                        Owner.QueueAnimation(Animation.Create(2327));
                        break;
                    case 58: // muscle man pose
                        Owner.QueueAnimation(Animation.Create(2566));
                        break;
                    case 59: // rofl
                        Owner.QueueAnimation(Animation.Create(2347)); // or 2359?
                        break;
                    case 60: // breathe fire
                        Owner.QueueGraphic(Graphic.Create(358));
                        Owner.QueueAnimation(Animation.Create(2238));
                        break;
                    case 61: // storm 
                        Owner.QueueGraphic(Graphic.Create(365));
                        Owner.QueueAnimation(Animation.Create(2563));
                        break;
                    case 62: // snow
                        Owner.QueueGraphic(Graphic.Create(364));
                        Owner.QueueAnimation(Animation.Create(2417));
                        break;
                    case 64: // head in the sand
                        Owner.QueueGraphic(Graphic.Create(1761));
                        Owner.QueueAnimation(Animation.Create(12926));
                        break;
                    case 65: // hula hoop
                        Owner.QueueAnimation(Animation.Create(12928));
                        break;
                    case 66: // disappear
                        Owner.QueueGraphic(Graphic.Create(1760));
                        Owner.QueueAnimation(Animation.Create(12929));
                        break;
                    case 67: // ghost
                        Owner.QueueGraphic(Graphic.Create(1762));
                        Owner.QueueAnimation(Animation.Create(12932));
                        break;
                    case 68: // bring it
                        Owner.QueueAnimation(Animation.Create(12934));
                        break;
                    case 69: // palm first
                        Owner.QueueAnimation(Animation.Create(12931));
                        break;
                    case 71:
                        break;
                    case 93: // living on borrowed time
                        break;
                    case 94: // Troubadour dance
                        Owner.QueueAnimation(Animation.Create(15424));
                        break;
                    case 95: // Evil Laugh
                        Owner.QueueAnimation(Animation.Create((short)(Owner.Appearance.Female ? 15536 : 15535)));
                        break;
                    case 96: // Golf clap
                        Owner.QueueAnimation(Animation.Create(15520));
                        break;
                    case 97: // LOLCano
                        Owner.QueueAnimation(Animation.Create((short)(Owner.Appearance.Female ? 15532 : 15533)));
                        Owner.QueueGraphic(Graphic.Create(2191));
                        break;
                    case 98: // Infernal power
                        Owner.QueueAnimation(Animation.Create(15529));
                        Owner.QueueGraphic(Graphic.Create(2197));
                        break;
                    case 99: // Divine power
                        Owner.QueueAnimation(Animation.Create(15524));
                        Owner.QueueGraphic(Graphic.Create(2195));
                        break;
                    case 100: // You're dead
                        Owner.QueueAnimation(Animation.Create(14195));
                        break;
                    case 101: // Scream
                        Owner.QueueAnimation(Animation.Create((short)(Owner.Appearance.Female ? 15527 : 15526)));
                        break;
                    case 102: // Tornado
                        Owner.QueueAnimation(Animation.Create(15530));
                        Owner.QueueGraphic(Graphic.Create(2196));
                        break;
                    case 103: // Chaotic cookery
                        Owner.QueueAnimation(Animation.Create(15604));
                        Owner.QueueGraphic(Graphic.Create(2239));
                        break;
                    case 104: // ROFL copter
                        Owner.QueueAnimation(Animation.Create((short)(Owner.Appearance.Female ? 16374 : 16373)));
                        Owner.QueueGraphic(Graphic.Create(3010));
                        break;
                    case 105: // Nature's Might
                        Owner.QueueAnimation(Animation.Create(16376));
                        Owner.QueueGraphic(Graphic.Create(3011));
                        break;
                    case 106: // Inner Power
                        Owner.QueueAnimation(Animation.Create(16382));
                        Owner.QueueGraphic(Graphic.Create(3014));
                        break;
                    case 107: // Werewolf transformation
                        Owner.QueueAnimation(Animation.Create(16380));
                        Owner.QueueGraphic(Graphic.Create(3013));
                        Owner.QueueGraphic(Graphic.Create(3016));
                        break;
                    case 108: // Celebrate
                        Owner.QueueAnimation(Animation.Create(16913));
                        Owner.QueueGraphic(Graphic.Create(3175));
                        break;
                    case 109: // Breakdance
                        Owner.QueueAnimation(Animation.Create(17079));
                        break;
                    case 110: // Mahjarrat transformation
                        Owner.QueueAnimation(Animation.Create(17103));
                        Owner.QueueGraphic(Graphic.Create(3222));
                        break;
                    case 111: // Break wind
                        Owner.QueueAnimation(Animation.Create(17076));
                        Owner.QueueGraphic(Graphic.Create(3226));
                        break;
                    case 112: // Backflip 
                        if (!_dumbPathFinder.CheckStep(Owner.Location, Owner.FaceDirection.Reverse()))
                        {
                            Owner.SendChatMessage("There is not enough space behind you to perform this emote.");
                            return true;
                        }

                        Owner.QueueAnimation(Animation.Create(17101));
                        Owner.QueueGraphic(Graphic.Create(3221));
                        break;
                    case 113:
                        Owner.QueueAnimation(Animation.Create(17077));
                        Owner.QueueGraphic(Graphic.Create(3219));
                        break;
                    case 114:
                        Owner.QueueAnimation(Animation.Create(17080));
                        Owner.QueueGraphic(Graphic.Create(3220));
                        break;
                    case 115:
                        Owner.QueueAnimation(Animation.Create(17163));
                        break;
                    case 116:
                        Owner.QueueAnimation(Animation.Create(17166));
                        break;
                    case 117:
                        Owner.QueueAnimation(Animation.Create(Owner.Appearance.Female ? 17213 : 17212));
                        Owner.QueueGraphic(Graphic.Create(3257));
                        break;
                    case 118:
                        Owner.QueueAnimation(Animation.Create(17186));
                        Owner.QueueGraphic(Graphic.Create(3252));
                        break;
                    default:
                        Owner.SendChatMessage("Emote[" + extraData2 + "] has not been implemented yet.");
                        break;
                }

                return true;
            });
        }

        /// <summary>
        ///     Happens when interface is closed for character.
        /// </summary>
        public override void OnClose()
        {
        }
    }
}