using System.Collections.Generic;
using System.Linq;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Providers;
using Hagalaz.Game.Common.Events;
using Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Armadyl;
using Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Bandos;
using Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Saradomin;
using Hagalaz.Game.Scripts.Minigames.Godwars.NPCs.Zamorak;
using Hagalaz.Game.Scripts.Model.Creatures.Characters;

namespace Hagalaz.Game.Scripts.Minigames.Godwars
{
    /// <summary>
    /// </summary>
    public static class GodwarsManager
    {
        /// <summary>
        ///     The critters.
        /// </summary>
        private static readonly Dictionary<int, GodwarsChamber> _critters = new Dictionary<int, GodwarsChamber>();

        /// <summary>
        /// </summary>
        private struct GodwarsChamber
        {
            /// <summary>
            ///     The general.
            /// </summary>
            private INpc _general;

            /// <summary>
            ///     The body guards.
            /// </summary>
            private readonly List<INpc> _bodyGuards;

            /// <summary>
            ///     Gets the body guards.
            /// </summary>
            /// <value>
            ///     The body guards.
            /// </value>
            public IEnumerable<INpc> BodyGuards => _bodyGuards;

            /// <summary>
            ///     Initializes a new instance of the <see cref="GodwarsChamber" /> struct.
            /// </summary>
            /// <param name="general">The general.</param>
            /// <param name="bodyGuards">The body guards.</param>
            public GodwarsChamber(INpc general, List<INpc> bodyGuards)
            {
                _general = general;
                _bodyGuards = bodyGuards;
            }

            /// <summary>
            ///     Sets the general.
            /// </summary>
            /// <param name="npc">The NPC.</param>
            public void SetGeneral(INpc npc) => _general = npc;

            /// <summary>
            ///     Adds the body guard.
            /// </summary>
            /// <param name="npc">The NPC.</param>
            public void AddBodyGuard(INpc npc)
            {
                if (_bodyGuards.Contains(npc))
                {
                    return;
                }

                _bodyGuards.Add(npc);
            }

            /// <summary>
            ///     Removes the body guard.
            /// </summary>
            /// <param name="npc">The NPC.</param>
            public void RemoveBodyGuard(INpc npc)
            {
                if (!_bodyGuards.Contains(npc))
                {
                    return;
                }

                _bodyGuards.Remove(npc);
            }

            /// <summary>
            ///     Determines whether this instance [can body guard respawn].
            /// </summary>
            /// <returns></returns>
            public bool CanBodyGuardRespawn() => _general != null && !_general.Combat.IsDead;
        }

        /// <summary>
        ///     Gets the body guards.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        /// <returns></returns>
        public static IEnumerable<INpc> GetBodyGuards(INpc npc)
        {
            if (!_critters.ContainsKey(npc.Area.Id))
            {
                yield break;
            }

            foreach (var guard in _critters[npc.Area.Id].BodyGuards)
            {
                yield return guard;
            }
        }

        /// <summary>
        ///     Sets the general.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        /// <param name="remove">if set to <c>true</c> [remove].</param>
        public static void SetGeneral(INpc npc, bool remove = false)
        {
            if (remove)
            {
                if (_critters.ContainsKey(npc.Area.Id))
                {
                    _critters.Remove(npc.Area.Id);
                }

                return;
            }

            if (!_critters.ContainsKey(npc.Area.Id))
            {
                _critters.Add(npc.Area.Id, new GodwarsChamber(npc, []));
            }
            else
            {
                _critters[npc.Area.Id].SetGeneral(npc);
            }
        }

        /// <summary>
        ///     Adds the body guard.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        public static void AddBodyGuard(INpc npc)
        {
            if (!_critters.ContainsKey(npc.Area.Id))
            {
                _critters.Add(npc.Area.Id, new GodwarsChamber(null, [npc]));
            }
            else
            {
                _critters[npc.Area.Id].AddBodyGuard(npc);
            }
        }

        /// <summary>
        ///     Removes the body guard.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        public static void RemoveBodyGuard(INpc npc)
        {
            if (!_critters.ContainsKey(npc.Area.Id))
            {
                return;
            }

            _critters[npc.Area.Id].RemoveBodyGuard(npc);
        }

        /// <summary>
        ///     Determines whether this instance [can body guard respawn] the specified NPC.
        /// </summary>
        /// <param name="npc">The NPC.</param>
        /// <returns></returns>
        public static bool CanBodyGuardRespawn(INpc npc)
        {
            if (!_critters.ContainsKey(npc.Area.Id))
            {
                return false;
            }

            return _critters[npc.Area.Id].CanBodyGuardRespawn();
        }
    }

    /// <summary>
    ///     Contains a script for godwars.
    /// </summary>
    public class GodwarsScript : CharacterScriptBase
    {
        /// <summary>
        ///     Contains the kill handler.
        /// </summary>
        private EventHappened? _killHandler;

        public GodwarsScript(ICharacterContextAccessor contextAccessor) : base(contextAccessor)
        {
        }

        /// <summary>
        ///     Gets the armadyl kills.
        /// </summary>
        public int ArmadylKills { get; private set; }

        /// <summary>
        ///     Gets the bandos kills.
        /// </summary>
        public int BandosKills { get; private set; }

        /// <summary>
        ///     Gets the saradomin kills.
        /// </summary>
        public int SaradominKills { get; private set; }

        /// <summary>
        ///     Gets the zamorak kills.
        /// </summary>
        public int ZamorakKills { get; private set; }

        /// <summary>
        ///     Gets the zaros kills.
        /// </summary>
        public int ZarosKills { get; private set; }

        /// <summary>
        ///     Initializes this instance.
        /// </summary>
        protected override void Initialize()
        {
            var dto = Character.Profile.GetObject(GodwarsConstants.ProfileMinigamesGodwarsKillCount, new GodwarsDto());
            ArmadylKills = dto.ArmadylKills;
            BandosKills = dto.BandosKills;
            SaradominKills = dto.SaradominKills;
            ZamorakKills = dto.ZamorakKills;
            ZarosKills = dto.ZarosKills;

            DrawAllKills();

            _killHandler = Character.RegisterEventHandler(new EventHappened<CreatureKillEvent>(e =>
            {
                if (e.Victim is not INpc npc)
                {
                    return false; // The event is not handled.
                }

                var compositeID = npc.Appearance.CompositeID;
                if (GodwarsConstants.ArmadylNpCs.Contains(compositeID) || npc.HasScript<ArmadylFaction>())
                {
                    ArmadylKills++;
                    UpdateGodwarsProfile();
                    DrawArmadylKills();
                    return true; // The event is handled.
                }

                if (GodwarsConstants.BandosNpCs.Contains(compositeID) || npc.HasScript<BandosFaction>())
                {
                    BandosKills++;
                    UpdateGodwarsProfile();
                    DrawBandosKills();
                    return true; // The event is handled.
                }

                if (GodwarsConstants.SaradominNpCs.Contains(compositeID) || npc.HasScript<SaradominFaction>())
                {
                    SaradominKills++;
                    UpdateGodwarsProfile();
                    DrawSaradominKills();
                    return true; // The event is handled.
                }

                if (GodwarsConstants.ZamorakNpCs.Contains(compositeID) || npc.HasScript<ZamorakFaction>())
                {
                    ZamorakKills++;
                    UpdateGodwarsProfile();
                    DrawZamorakKills();
                    return true; // The event is handled.
                }

                return false; // The event is not handled.
            }));
        }

        private void UpdateGodwarsProfile() =>
            Character.Profile.SetObject(GodwarsConstants.ProfileMinigamesGodwarsKillCount, new GodwarsDto
            {
                ArmadylKills = ArmadylKills,
                BandosKills = BandosKills,
                SaradominKills = SaradominKills,
                ZamorakKills = ZamorakKills,
                ZarosKills = ZarosKills
            });

        /// <summary>
        ///     Resets the armadyl kills.
        /// </summary>
        public void ResetArmadylKills()
        {
            ArmadylKills = 0;
            UpdateGodwarsProfile();
            DrawArmadylKills();
        }

        /// <summary>
        ///     Resets the bandos kills.
        /// </summary>
        public void ResetBandosKills()
        {
            BandosKills = 0;
            UpdateGodwarsProfile();
            DrawBandosKills();
        }

        /// <summary>
        ///     Resets the saradomin kills.
        /// </summary>
        public void ResetSaradominKills()
        {
            SaradominKills = 0;
            UpdateGodwarsProfile();
            DrawSaradominKills();
        }

        /// <summary>
        ///     Resets the zamorak kills.
        /// </summary>
        public void ResetZamorakKills()
        {
            ZamorakKills = 0; 
            UpdateGodwarsProfile();
            DrawZamorakKills();
        }

        /// <summary>
        ///     Resets the zaros kills.
        /// </summary>
        public void ResetZarosKills()
        {
            ZarosKills = 0;
            UpdateGodwarsProfile();
            DrawZarosKills();
        }

        /// <summary>
        ///     Draws all kills.
        /// </summary>
        public void DrawAllKills()
        {
            DrawArmadylKills();
            DrawBandosKills();
            DrawSaradominKills();
            DrawZamorakKills();
            DrawZarosKills();
        }

        /// <summary>
        ///     Draws the armadyl kills.
        /// </summary>
        public void DrawArmadylKills() => Character.Configurations.SendBitConfiguration(3939, ArmadylKills);

        /// <summary>
        ///     Draws the armadyl kills.
        /// </summary>
        public void DrawBandosKills() => Character.Configurations.SendBitConfiguration(3941, BandosKills);

        /// <summary>
        ///     Draws the armadyl kills.
        /// </summary>
        public void DrawSaradominKills() => Character.Configurations.SendBitConfiguration(3938, SaradominKills);

        /// <summary>
        ///     Draws the armadyl kills.
        /// </summary>
        public void DrawZamorakKills() => Character.Configurations.SendBitConfiguration(3942, ZamorakKills);

        /// <summary>
        ///     Draws the zaros kills.
        /// </summary>
        public void DrawZarosKills() => Character.Configurations.SendBitConfiguration(8725, ZarosKills);

        /// <summary>
        ///     Unloads this instance.
        /// </summary>
        private void Unload()
        {
            if (_killHandler != null)
            {
                Character.UnregisterEventHandler<CreatureKillEvent>(_killHandler);
            }
        }

        /// <summary>
        ///     Get's called when character is destroyed permanently.
        ///     By default, this method does nothing.
        /// </summary>
        public override void OnDestroy() => Unload();

        /// <summary>
        ///     Called when this script is removed from the character.
        /// </summary>
        public override void OnRemove()
        {
            Unload();

            Character.Profile.SetObject(GodwarsConstants.ProfileMinigamesGodwarsKillCount, new GodwarsDto());

            Character.SendChatMessage("The power of all those you slew in the dungeon drains from your body.");
        }
    }
}