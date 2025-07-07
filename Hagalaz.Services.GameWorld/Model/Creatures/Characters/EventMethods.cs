using System;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hagalaz.Collections.Extensions;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Builders.GameObject;
using Hagalaz.Game.Abstractions.Builders.GroundItem;
using Hagalaz.Game.Abstractions.Builders.Movement;
using Hagalaz.Game.Abstractions.Builders.Npc;
using Hagalaz.Game.Abstractions.Features;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Combat;
using Hagalaz.Game.Abstractions.Model.Creatures;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Creatures.Npcs;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Model.GameObjects;
using Hagalaz.Game.Abstractions.Model.Maps;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common;
using Hagalaz.Game.Common.Events.Character.Packet;
using Hagalaz.Game.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Hagalaz.DependencyInjection.Extensions;
using Hagalaz.Game.Extensions;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    /// <summary>
    /// Contains methods for managing character events.
    /// </summary>
    public partial class Character : Creature
    {
        /// <summary>
        /// Get's called when character class is created.
        /// </summary>
        private void RegisterEventHandlers() =>
            RegisterEventHandler(new EventHappened<ConsoleCommandEvent>((e) =>
            {
                var ce = e;
                if (!Permissions.HasAtLeastXPermission(Permission.GameAdministrator)) return OnCommandReceived(ce.Command).Result;

                //return this.OnCommandReceived(ce.Command);
                if (ce.Command.Equals("refill"))
                {
                    Statistics.Normalise();
                    return true;
                }
                else if (ce.Command.Equals("infinity"))
                {
                    QueueTask(new RsTickTask(Statistics.Normalise));
                    return true;
                }
                else if (ce.Command.StartsWith("setskull"))
                {
                    var cmd = ce.Command.Split(' ');
                    var icon = short.Parse(cmd[1]);
                    Appearance.SkullIcon = (SkullIcon)icon;
                }
                else if (ce.Command.StartsWith("proj"))
                {
                    var cmd = ce.Command.Split(' ');
                    var creatures = Viewport.VisibleCreatures.ToArray();
                    ICreature? target = creatures.FirstOrDefault(creature => creature != this);

                    if (target == null) return false;

                    if (!int.TryParse(cmd[1], out var gfxID))
                    {
                        return false;
                    }

                    var projectile = new Projectile(gfxID);
                    projectile.SetSenderData(this, 11, false);
                    projectile.SetReceiverData(target, 30);
                    projectile.SetFlyingProperties(30, 30, 20, 0, false);
                    projectile.Display();
                }
                else if (ce.Command.StartsWith("addexp"))
                {
                    var cmd = ce.Command.Split(' ');
                    var skillID = byte.Parse(cmd[1]);
                    var exp = double.Parse(cmd[2]);
                    Statistics.AddExperience(skillID, exp);
                }
                else if (ce.Command.StartsWith("mappos"))
                {
                    var cmd = ce.Command.Split(' ');
                    int absX = int.Parse(cmd[1]);
                    int absY = int.Parse(cmd[2]);
                    var mapX = -1;
                    var mapY = -1;
                    Viewport.GetLocalPosition(Game.Abstractions.Model.Location.Create(absX, absY, Location.Z, Location.Dimension), ref mapX, ref mapY);
                    SendChatMessage("X:" + mapX + " Y:" + mapY);
                }
                else if (ce.Command.StartsWith("defence"))
                {
                    Statistics.Bonuses.SetBonus(BonusType.DefenceStab, 2000);
                    Statistics.Bonuses.SetBonus(BonusType.DefenceRanged, 2000);
                    Statistics.Bonuses.SetBonus(BonusType.DefenceMagic, 2000);
                    Statistics.Bonuses.SetBonus(BonusType.DefenceCrush, 2000);
                    Statistics.Bonuses.SetBonus(BonusType.DefenceSummoning, 2000);
                    Statistics.Bonuses.SetBonus(BonusType.DefenceSlash, 2000);
                    SendChatMessage("Be carefull with that...");
                }
                else if (ce.Command.StartsWith("power"))
                {
                    Statistics.Bonuses.SetBonus(BonusType.Strength, 2000);
                    Statistics.Bonuses.SetBonus(BonusType.RangedStrength, 2000);
                    Statistics.Bonuses.SetBonus(BonusType.MagicDamage, 2000);
                    SendChatMessage("Be carefull with that...");
                }
                else if (ce.Command.StartsWith("interfacesettings"))
                {
                    var cmd = ce.Command.Split(' ');
                    var interfaceID = int.Parse(cmd[1]);
                    var childID = int.Parse(cmd[2]);
                    int min = ushort.Parse(cmd[3]);
                    int max = ushort.Parse(cmd[4]);
                    var value = int.Parse(cmd[5]);
                    var inter = Widgets.GetOpenWidget((short)interfaceID);
                    inter?.SetOptions((short)childID, (ushort)min, (ushort)max, value);
                    return false;
                }
                else if (ce.Command.StartsWith("spawnobject"))
                {
                    var goBuilder = ServiceProvider.GetRequiredService<IGameObjectBuilder>();
                    var cmd = ce.Command.Split(' ');
                    var gameObject = goBuilder.Create()
                        .WithId(int.Parse(cmd[1]))
                        .WithLocation(Location)
                        .WithShape((ShapeType)int.Parse(cmd[2]))
                        .WithRotation(int.Parse(cmd[3]))
                        .Build();
                    Region.Add(gameObject);
                    return false;
                }
                else if (ce.Command.StartsWith("spawnnpc"))
                {
                    var builder = ServiceProvider.GetRequiredService<INpcBuilder>();
                    try
                    {
                        builder
                            .Create()
                            .WithId(int.Parse(ce.Command.Split(' ')[1]))
                            .WithLocation(Location)
                            .Spawn();
                        SendChatMessage("Done :)", ChatMessageType.ConsoleText);
                    }
                    catch (Exception)
                    {
                        SendChatMessage("Fail :(", ChatMessageType.ConsoleText);
                    }

                    return false;
                }
                else if (ce.Command.StartsWith("savenpcs"))
                {
                    DataRow[] rows = null;

                    // Get all spawns.
                    //var database = ServiceLocator.Current.GetInstance<ISqlDatabaseManager>();
                    //using (var client = database.GetClient())
                    //{
                    //    var table = client.ReadDataTable("SELECT * FROM npc_spawns;");
                    //    if (table != null)
                    //        rows = table.Select();
                    //}

                    //var highest = 0;

                    //foreach (DataRow row in rows)
                    //{
                    //    int num = Convert.ToInt16(row[0]);
                    //    if (num >= highest)
                    //        highest = num;
                    //}

                    //highest += 1;

                    //var repository = ServiceLocator.Current.GetInstance<INpcStore>();
                    //using (var client = database.GetClient())
                    //{
                    //    foreach (var n in repository.FindAllAsync().ToEnumerable().Where(n => n.Statistics.CombatLevel == 1337))
                    //    {
                    //        client.AddParameter("idx", highest);
                    //        client.AddParameter("id", n.Definition.Id);
                    //        client.AddParameter("ox", n.Bounds.DefaultLocation.X);
                    //        client.AddParameter("oy", n.Bounds.DefaultLocation.Y);
                    //        client.AddParameter("oz", n.Bounds.DefaultLocation.Z);
                    //        client.AddParameter("mx", n.Bounds.MinimumLocation.X);
                    //        client.AddParameter("my", n.Bounds.MinimumLocation.Y);
                    //        client.AddParameter("mz", n.Bounds.MinimumLocation.Z);
                    //        client.AddParameter("max", n.Bounds.MaximumLocation.X);
                    //        client.AddParameter("may", n.Bounds.MaximumLocation.Y);
                    //        client.AddParameter("maz", n.Bounds.MaximumLocation.Z);
                    //        client.ExecuteUpdate("INSERT INTO npc_spawns (spawn_id,npc_id,coord_x,coord_y,coord_z,min_coord_x,min_coord_y,min_coord_z,max_coord_x,max_coord_y,max_coord_z) VALUES "
                    //                             + "(@idx,@id,@ox,@oy,@oz,@mx,@my,@mz,@max,@may,@maz)");
                    //        highest++;
                    //    }
                    //}
                    //SendChatMessage("Done :)", ChatMessageType.ConsoleText);
                    return false;
                }
                else if (ce.Command.StartsWith("killeveryone"))
                {
                    Speak("TROLLSTRIKE!!!");
                    Viewport.VisibleCreatures.OfType<INpc>()
                        .ForEach(npc =>
                        {
                            Combat.PerformAttack(new AttackParams()
                            {
                                Damage = 1337, MaxDamage = 1337, DamageType = DamageType.StandardMagic, Target = npc
                            });
                        });
                }
                else if (ce.Command.StartsWith("forcemovement"))
                {
                    var builder = ServiceProvider.GetRequiredService<IMovementBuilder>();
                    var mov = builder.Create()
                        .WithStart(Location)
                        .WithEnd(Game.Abstractions.Model.Location.Create(Location.X + RandomStatic.Generator.Next(5),
                            Location.Y + RandomStatic.Generator.Next(5),
                            Location.Z,
                            Location.Dimension))
                        .WithEndSpeed(40)
                        .Build();
                    Movement.Teleport(Teleport.Create(mov.EndLocation));
                    QueueForceMovement(mov);
                }
                else if (ce.Command.StartsWith("spawnitem"))
                {
                    var cmd = ce.Command.Split(' ');
                    var itemID = int.Parse(cmd[1]);
                    var itemAmount = int.Parse(cmd[2]);
                    var amountOfTicks = int.Parse(cmd[3]);
                    var groundItemBuilder = ServiceProvider.GetRequiredService<IGroundItemBuilder>();
                    groundItemBuilder.Create()
                        .WithItem(builder => builder.Create().WithId(itemID).WithCount(itemAmount))
                        .WithLocation(Location)
                        .WithTicks(amountOfTicks)
                        .Spawn();
                }
                else if (ce.Command.StartsWith("config"))
                {
                    var cmd = ce.Command.Split(' ');
                    Configurations.SendStandardConfiguration((short)int.Parse(cmd[1]), int.Parse(cmd[2]));
                }
                else if (ce.Command.StartsWith("cs2config"))
                {
                    var cmd = ce.Command.Split(' ');
                    Configurations.SendGlobalCs2Int((short)int.Parse(cmd[1]), int.Parse(cmd[2]));
                }
                else if (ce.Command.StartsWith("bitconfig"))
                {
                    var cmd = ce.Command.Split(' ');
                    Configurations.SendBitConfiguration((short)int.Parse(cmd[1]), int.Parse(cmd[2]));
                }
                else if (ce.Command.StartsWith("stringconfig"))
                {
                    var cmd = ce.Command.Split(' ');
                    var value = cmd.Length >= 2 ? cmd[2] : string.Empty;
                    Configurations.SendGlobalCs2String(short.Parse(cmd[1]), value);
                }
                else if (ce.Command.StartsWith("togglecomponent"))
                {
                    var cmd = ce.Command.Split(' ');
                    var inter = Widgets.GetOpenWidget(short.Parse(cmd[1]));
                    if (inter == null) return false;
                    inter.SetVisible(short.Parse(cmd[2]), bool.Parse(cmd[3]));
                }
                else if (ce.Command.StartsWith("drawstring"))
                {
                    var cmd = ce.Command.Split(' ');
                    var inter = Widgets.GetOpenWidget(short.Parse(cmd[1]));
                    if (inter == null) return false;
                    var bld = new StringBuilder();
                    for (int i = 3; i < cmd.Length; i++) bld.Append(cmd[i] + (i + 1 >= cmd.Length ? "" : " "));
                    inter.DrawString(short.Parse(cmd[2]), bld.ToString());
                }
                else if (ce.Command.StartsWith("drawitem"))
                {
                    var cmd = ce.Command.Split(' ');
                    var inter = Widgets.GetOpenWidget(short.Parse(cmd[1]));
                    if (inter == null) return false;
                    inter.DrawItem(short.Parse(cmd[2]), short.Parse(cmd[3]), int.Parse(cmd[4]));
                }
                else if (ce.Command.StartsWith("animatecomponent"))
                {
                    var cmd = ce.Command.Split(' ');
                    var inter = Widgets.GetOpenWidget(short.Parse(cmd[1]));
                    if (inter == null) return false;
                    inter.SetAnimation(short.Parse(cmd[2]), short.Parse(cmd[3]));
                }
                else if (ce.Command.StartsWith("drawmodel"))
                {
                    var cmd = ce.Command.Split(' ');
                    var inter = Widgets.GetOpenWidget(short.Parse(cmd[1]));
                    if (inter == null) return false;
                    inter.DrawModel(short.Parse(cmd[2]), int.Parse(cmd[3]));
                }
                else if (ce.Command.StartsWith("drawcharacter"))
                {
                    var cmd = ce.Command.Split(' ');
                    var inter = Widgets.GetOpenWidget(short.Parse(cmd[1]));
                    if (inter == null) return false;
                    inter.DrawCharacterHead(short.Parse(cmd[2]));
                }
                else if (ce.Command.StartsWith("setcolor"))
                {
                    var cmd = ce.Command.Split(' ');
                    var part = int.Parse(cmd[1]);
                    var val = int.Parse(cmd[2]);
                    try
                    {
                        Appearance.SetColor((ColorType)part, val);
                    }
                    catch (Exception ex)
                    {
                        SendChatMessage("Something went wrong: " + ex.Message);
                    }
                }
                else if (ce.Command.StartsWith("runcs2"))
                {
                    var cmd = ce.Command.Split(' ');
                    var types = cmd[1].ToCharArray();
                    var scriptID = int.Parse(cmd[2]);
                    var p = new object[cmd.Length - 3];
                    var t = 0;
                    for (int i = 3; i < cmd.Length; i++)
                    {
                        if (types[t++] == 's')
                            p[i - 3] = cmd[i];
                        else
                            p[i - 3] = int.Parse(cmd[i]);
                    }

                    Configurations.SendCs2Script(scriptID, p);
                }
                else if (ce.Command.StartsWith("emptycs2"))
                {
                    var cmd = ce.Command.Split(' ');
                    var scriptID = int.Parse(cmd[1]);
                    Configurations.SendCs2Script(scriptID, []);
                }
                else if (ce.Command.StartsWith("tonpc"))
                {
                    var id = short.Parse(ce.Command.Split(' ')[1]);
                    Appearance.TurnToNpc(id);
                }
                else if (ce.Command.StartsWith("toplayer"))
                {
                    Appearance.TurnToPlayer();
                }
                else if (ce.Command.Equals("setcollision"))
                {
                    var regionManager = ServiceLocator.Current.GetInstance<IMapRegionService>();
                    regionManager.FlagCollision(Location, CollisionFlag.ObjectTile);
                    regionManager.FlagCollision(Location, CollisionFlag.ObjectBlock);
                    regionManager.FlagCollision(Location, CollisionFlag.ObjectAllowRange);
                }
                else if (ce.Command.Equals("getcollision"))
                {
                    var regionManager = ServiceLocator.Current.GetInstance<IMapRegionService>();
                    var mapRegion = regionManager.GetOrCreateMapRegion(Location.RegionId, Location.Dimension, true);
                    SendChatMessage("Collision:" + mapRegion.GetCollision(Location.X - Location.RegionX * 64, Location.Y - Location.RegionY * 64, Location.Z),
                        ChatMessageType.ConsoleText);
                }
                else if (ce.Command.StartsWith("level"))
                {
                    var cmd = ce.Command.Split(' ');
                    var skillID = byte.Parse(cmd[1]);
                    var level = byte.Parse(cmd[2]);
                    Statistics.SetSkillExperience(skillID, StatisticsHelpers.ExperienceForLevel(level));
                }
                else
                {
                    return OnCommandReceived(ce.Command).Result;
                }

                return false;
            }));

        /// <summary>
        /// This method is called when ConsoleCommandEvent is caught by our handler.
        /// </summary>
        /// <param name="commandAndArgs">Command which was received.</param>
        private async Task<bool> OnCommandReceived(string commandAndArgs)
        {
            try
            {
                var commands = ServiceProvider.GetRequiredService<IGameCommandPrompt>();
                var (command, arguments) = ParseCommandAndArguments(commandAndArgs);
                return await commands.ExecuteAsync(command, this, arguments);
            }
            catch (Exception ex)
            {
                var logger = ServiceProvider.GetRequiredService<ILogger<ICharacter>>();
                logger.LogError(ex, "Error while handling character command: {Command}", commandAndArgs);
                return false;
            }
        }

        private static (string command, string[] arguments) ParseCommandAndArguments(string commandAndArgs)
        {
            var commandSpan = commandAndArgs.AsSpan();
            var startIndex = commandSpan.IndexOf(' ');
            var command = startIndex > 0 ? commandSpan[..startIndex] : commandSpan;
            var args = startIndex > 0 ? commandSpan[(startIndex + 1)..].ToString().Split(' ') : [];
            return (command.ToString(), args);
        }
    }
}