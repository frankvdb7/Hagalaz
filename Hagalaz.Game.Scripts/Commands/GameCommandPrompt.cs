using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Authorization;
using Hagalaz.Game.Abstractions.Features;
using Hagalaz.Game.Abstractions.Model;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Store;
using Hagalaz.Game.Abstractions.Tasks;
using Hagalaz.Game.Common.Events.Character;
using Hagalaz.Game.Scripts.Util;
using Hagalaz.Game.Scripts.Widgets.CharacterName;
using Microsoft.Extensions.Logging;

namespace Hagalaz.Game.Scripts.Commands
{
    /// <summary>
    ///     Console commands management for in-game commands..
    /// </summary>
    public class GameCommandPrompt : IGameCommandPrompt
    {
        /// <summary>
        ///     A collection if dictionaries available to this command prompt.
        /// </summary>
        private readonly Dictionary<string, IGameCommand> _commands = new(StringComparer.InvariantCultureIgnoreCase);

        /// <summary>
        ///     Construct the commands.
        /// </summary>
        /// <remarks>
        ///     arguments[0] = Hagalaz.Game.Model.Characters.Character character
        ///     arguments[1] = string[] arguments
        ///     arguments[2] = string command (unedited)
        /// </remarks>
        public GameCommandPrompt(IServiceProvider serviceProvider, ILogger<GameCommandPrompt> logger)
        {
            var commands = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<IEnumerable<IGameCommand>>();
            var cmd = new List<IGameCommand>
            {
                new GenericGameCommand
                {
                    Name = "currentregion",
                    CommandFunc = async (character, arguments) =>
                    {
                        character.SendChatMessage("Current Region Id: " + character.Region.Id, ChatMessageType.ConsoleText);
                        return true;
                    },
                    Permission = Permission.SystemAdministrator
                },
                new GenericGameCommand
                {
                    Name = "listcommands",
                    CommandFunc = async (character, arguments) =>
                    {
                        foreach (var command in _commands.Values.Where(command => character.Permissions.HasAtLeastXPermission(command.Permission)))
                        {
                            character.SendChatMessage(command.Name, ChatMessageType.ConsoleText);
                        }

                        return true;
                    },
                    Permission = Permission.Standard
                },
                new GenericGameCommand
                {
                    Name = "logconsole",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        var writer = new ServerTextWriter(text =>
                        {
                            if (character == null || character.IsDestroyed)
                            {
                                Console.Out.Close(); // close the text writer
                                Console.SetOut(new StreamWriter(Console.OpenStandardOutput())); // reset the default
                                return;
                            }

                            character.SendChatMessage(text, ChatMessageType.ConsoleText);
                        });
                        Console.SetOut(writer);
                        return true;
                    },
                    Permission = Permission.SystemAdministrator
                },
                new GenericGameCommand
                {
                    Name = "played",
                    CommandFunc = async (character, arguments) =>
                    {
                        var currentPlayTime = character.Statistics.PlayTime.Add(DateTime.Now - character.LastLogin);
                        var sb = new StringBuilder("Play time: ");
                        sb.Append(currentPlayTime.Days > 0 ? currentPlayTime.Days + " Day(s) " : "");
                        sb.Append(currentPlayTime.Hours > 0 ? currentPlayTime.Hours + " Hour(s) " : "");
                        sb.Append(currentPlayTime.Minutes > 0 ? currentPlayTime.Minutes + " Minute(s) " : "");
                        sb.Append(currentPlayTime.Seconds > 0 ? currentPlayTime.Seconds + " Second(s) " : "");
                        character.SendChatMessage(sb.ToString());
                        return true;
                    },
                    Permission = Permission.Standard
                },
                new GenericGameCommand
                {
                    Name = "testurl",
                    CommandFunc = async (character, arguments) =>
                    {
                        //await character.Session.SendPacketAsync(new OpenUrlPacketComposer("http://www.github.com/"));
                        return true;
                    },
                    Permission = Permission.SystemAdministrator
                },
                new GenericGameCommand
                {
                    Name = "testpriority",
                    CommandFunc = async (character, arguments) =>
                    {
                        //await character.Session.SendPacketAsync(new CharacterRenderPriorityPacketComposer(false));
                        return true;
                    },
                    Permission = Permission.SystemAdministrator
                },
                new GenericGameCommand
                {
                    Name = "getnpcdef",
                    CommandFunc = async (character, arguments) =>
                    {
                        var definitionRepository = character.ServiceProvider.GetRequiredService<INpcService>();
                        var npcID = int.Parse(arguments[0]);
                        var defString = definitionRepository.FindNpcDefinitionById(npcID).ToString()?.Split(',');
                        if (defString != null)
                        {
                            foreach (var info in defString)
                            {
                                character.SendChatMessage(info, ChatMessageType.ConsoleText);
                            }
                        }

                        return true;
                    },
                    Permission = Permission.GameAdministrator
                },
                new GenericGameCommand
                {
                    Name = "getobjectdef",
                    CommandFunc = async (character, arguments) =>
                    {
                        var objectID = int.Parse(arguments[0]);
                        var definitionRepository = character.ServiceProvider.GetRequiredService<IGameObjectService>();
                        var defString = definitionRepository.FindGameObjectDefinitionById(objectID)?.ToString()?.Split(',');
                        if (defString != null)
                        {
                            foreach (var info in defString)
                            {
                                character.SendChatMessage(info, ChatMessageType.ConsoleText);
                            }
                        }

                        return true;
                    },
                    Permission = Permission.GameAdministrator
                },
                new GenericGameCommand
                {
                    Name = "loopgfx",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        var gfx = int.Parse(arguments[1]);
                        RsTickTask task = null!;
                        character.QueueTask(task = new RsTickTask(async () =>
                        {
                            character.QueueGraphic(Graphic.Create((short)gfx));
                            character.SendChatMessage(" gfx - " + gfx, ChatMessageType.ConsoleText);
                            gfx++;
                            if (character.Movement.Moving)
                            {
                                task.Cancel();
                            }
                        }));
                        return true;
                    },
                    Permission = Permission.SystemAdministrator
                },
                new GenericGameCommand
                {
                    Name = "coords",
                    CommandFunc = async (character, arguments) =>
                    {
                        character.SendChatMessage(character.Location.ToString()!, ChatMessageType.ConsoleText);
                        return true;
                    },
                    Permission = Permission.GameAdministrator
                },
                new GenericGameCommand
                {
                    Name = "mcoords",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        var mapX = -1;
                        var mapY = -1;
                        character.Viewport.GetLocalPosition(character.Location, ref mapX, ref mapY);
                        character.SendChatMessage("x=" + mapX + ",y=" + mapY);
                        return true;
                    },
                    Permission = Permission.GameAdministrator
                },
                new GenericGameCommand
                {
                    Name = "players",
                    CommandFunc = async (character, arguments) =>
                    {
                        var store = character.ServiceProvider.GetRequiredService<ICharacterStore>();
                        var players = await store.CountAsync();
                        character.SendChatMessage($"There are {players} players currently on this world.");
                        var count = await store.CountAsync();
                        character.SendChatMessage("There are " + count + " players currently on this lobby.");
                        return true;
                    },
                    Permission = Permission.Standard
                },
                new GenericGameCommand
                {
                    Name = "empty",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        character.Inventory.Clear(true);
                        return true;
                    },
                    Permission = Permission.GameAdministrator
                },
                new GenericGameCommand
                {
                    Name = "master",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        for (byte i = 0; i < StatisticsConstants.SkillsCount; i++)
                        {
                            character.Statistics.AddExperience(i, 20000000D);
                        }

                        character.Statistics.Normalise();
                        character.SendChatMessage("All your skills have been mastered.");
                        return true;
                    },
                    Permission = Permission.GameAdministrator
                },
                new GenericGameCommand
                {
                    Name = "pure",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        for (byte i = 0; i < StatisticsConstants.SkillsCount; i++)
                        {
                            if (i != StatisticsConstants.Summoning || i != StatisticsConstants.Defence)
                            {
                                character.Statistics.AddExperience(i, 20000000D);
                            }
                        }

                        character.Statistics.Normalise();
                        character.SendChatMessage("All your skills have been pured.");
                        return true;
                    },
                    Permission = Permission.GameAdministrator
                },
                new GenericGameCommand
                {
                    Name = "reset",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        for (byte i = 0; i < StatisticsConstants.SkillsCount; i++)
                        {
                            character.Statistics.SetSkillLevel(i, 1);
                            character.Statistics.SetSkillExperience(i, 0D);
                        }

                        character.Statistics.Normalise();
                        character.SendChatMessage("All your skills have been reset.");
                        return true;
                    },
                    Permission = Permission.GameAdministrator
                },
                new GenericGameCommand
                {
                    Name = "level",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        if (character.Permissions.HasAtLeastXPermission(Permission.GameAdministrator))
                        {
                            var skillID = byte.Parse(arguments[0]);
                            var experience = double.Parse(arguments[1]);
                            if (skillID >= StatisticsConstants.SkillsCount)
                            {
                                character.SendChatMessage("Wrong Skill Id.");
                                return true;
                            }

                            if (experience < 0 || experience > StatisticsConstants.MaximumExperience)
                            {
                                character.SendChatMessage("Wrong experience amount.");
                                return true;
                            }

                            character.Statistics.SetSkillLevel(skillID, 1);
                            character.Statistics.SetSkillExperience(skillID, experience);
                            return true;
                        }

                        return false;
                    },
                    Permission = Permission.GameAdministrator
                },
                new GenericGameCommand
                {
                    Name = "teletome",
                    CommandFunc = async (character, arguments) =>
                    {
                        var name = string.Join(" ", arguments);
                        var repository = character.ServiceProvider.GetRequiredService<ICharacterStore>();
                        var c = await repository.FindAllAsync()
                            .Where(ch => string.Compare(ch.DisplayName, name, StringComparison.OrdinalIgnoreCase) == 0)
                            .SingleOrDefaultAsync();
                        if (c != null)
                        {
                            c.Movement.Teleport(Teleport.Create(character.Location.Clone()));
                            c.SendChatMessage("You've been teleported to " + character.DisplayName + ".");
                        }
                        else
                        {
                            character.SendChatMessage("Character \"" + name + "\" could not be found.");
                        }

                        return true;
                    },
                    Permission = Permission.GameModerator
                },
                new GenericGameCommand
                {
                    Name = "teleto",
                    CommandFunc = async (character, arguments) =>
                    {
                        var name = string.Join(" ", arguments);

                        var repository = character.ServiceProvider.GetRequiredService<ICharacterStore>();
                        var c = await repository.FindAllAsync()
                            .Where(ch => string.Compare(ch.DisplayName, name, StringComparison.OrdinalIgnoreCase) == 0)
                            .FirstOrDefaultAsync();
                        if (c != null)
                        {
                            character.Movement.Teleport(Teleport.Create(c.Location.Clone()));
                            character.SendChatMessage("You teleported to " + c.DisplayName + ".");
                        }
                        else
                        {
                            character.SendChatMessage("Character \"" + name + "\" could not be found.");
                        }

                        return true;
                    },
                    Permission = Permission.GameModerator
                },
                new GenericGameCommand
                {
                    Name = "mute",
                    CommandFunc = async (character, arguments) =>
                    {
                        var name = string.Join(" ", arguments);
                        // TODO
                        //IMasterConnectionAdapter adapter = ServiceLocator.Current.GetInstance<IMasterConnectionAdapter>();
                        //await adapter.SendDataAsync(new DoGameCommandRequestPacketComposer(character.Session.Id, name, SubmitOffenceType.Mute).Serialize());
                        return true;
                    },
                    Permission = Permission.GameModerator
                },
                new GenericGameCommand
                {
                    Name = "unmute",
                    CommandFunc = async (character, arguments) =>
                    {
                        if (!character.Permissions.HasAtLeastXPermission(Permission.GameModerator))
                        {
                            return false;
                        }

                        var name = string.Join(" ", arguments);
                        // TODO
                        //var adapter = ServiceLocator.Current.GetInstance<IMasterConnectionAdapter>();
                        //await adapter.SendDataAsync(new DoGameCommandRequestPacketComposer(character.Session.Id, name, SubmitOffenceType.UnMute).Serialize());
                        return true;
                    },
                    Permission = Permission.GameModerator
                },
                new GenericGameCommand
                {
                    Name = "ipban",
                    CommandFunc = async (character, arguments) =>
                    {
                        var cmsd = string.Join(" ", arguments);
                        // TODO
                        //var adapter = ServiceLocator.Current.GetInstance<IMasterConnectionAdapter>();
                        //await adapter.SendDataAsync(new DoGameCommandRequestPacketComposer(character.Session.Id, cmsd, SubmitOffenceType.IpBan).Serialize());
                        return true;
                    },
                    Permission = Permission.GameModerator
                },
                new GenericGameCommand
                {
                    Name = "unipban",
                    CommandFunc = async (character, arguments) =>
                    {
                        if (!character.Permissions.HasAtLeastXPermission(Permission.GameModerator))
                        {
                            return false;
                        }

                        var cfmd = string.Join(" ", arguments);
                        // TODO
                        //var adapter = ServiceLocator.Current.GetInstance<IMasterConnectionAdapter>();
                        //await adapter.SendDataAsync(new DoGameCommandRequestPacketComposer(character.Session.Id, cfmd, SubmitOffenceType.UnIpBan).Serialize());
                        return true;
                    },
                    Permission = Permission.GameModerator
                },
                new GenericGameCommand
                {
                    Name = "ban",
                    CommandFunc = async (character, arguments) =>
                    {
                        var name = string.Join(" ", arguments);
                        // TODO
                        //var adapter = ServiceLocator.Current.GetInstance<IMasterConnectionAdapter>();
                        //await adapter.SendDataAsync(new DoGameCommandRequestPacketComposer(character.Session.Id, name, SubmitOffenceType.Ban).Serialize());
                        return true;
                    },
                    Permission = Permission.GameModerator
                },
                new GenericGameCommand
                {
                    Name = "unban",
                    CommandFunc = async (character, arguments) =>
                    {
                        var name = string.Join(" ", arguments);
                        // TODO
                        //var adapter = ServiceLocator.Current.GetInstance<IMasterConnectionAdapter>();
                        //await adapter.SendDataAsync(new DoGameCommandRequestPacketComposer(character.Session.Id, name, SubmitOffenceType.UnBan).Serialize());
                        return true;
                    },
                    Permission = Permission.GameModerator
                },
                new GenericGameCommand
                {
                    Name = "kick",
                    CommandFunc = async (character, arguments) =>
                    {
                        var name = string.Join(" ", arguments);
                        // TODO
                        //IMasterConnectionAdapter adapter = ServiceLocator.Current.GetInstance<IMasterConnectionAdapter>();
                        //await adapter.SendDataAsync(new DoGameCommandRequestPacketComposer(character.Session.Id, name, SubmitOffenceType.Kick).Serialize());
                        return true;
                    },
                    Permission = Permission.GameModerator
                },
                new GenericGameCommand
                {
                    Name = "changename",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        var script = character.ServiceProvider.GetRequiredService<NameInterface>();
                        character.Widgets.OpenWidget(890, 0, script, true);
                        return false;
                    },
                    Permission = Permission.Standard
                },
                new GenericGameCommand
                {
                    Name = "displaytitle",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        var displayTitle = byte.Parse(arguments[0]);
                        character.Appearance.DisplayTitle = (DisplayTitle)displayTitle;
                        return true;
                    },
                    Permission = Permission.SystemAdministrator
                },
                new GenericGameCommand
                {
                    Name = "isopen",
                    CommandFunc = async (character, arguments) =>
                    {
                        var interfaceID = short.Parse(arguments[0]);
                        var inter = character.Widgets.GetOpenWidget(interfaceID);
                        character.SendChatMessage("InterfaceID:" + interfaceID + " is " + (inter == null ? "closed" : "open"));
                        return true;
                    },
                    Permission = Permission.SystemAdministrator
                },
                new GenericGameCommand
                {
                    Name = "tzhaar",
                    CommandFunc = async (character, arguments) =>
                    {
                        var regionManager = character.ServiceProvider.GetRequiredService<IMapRegionService>();
                        if (!regionManager.TryCreateDimension(out var dimension))
                        {
                            return false;
                        }

                        var source = Location.Create(4416, 5120, 0, 0);
                        var destination = Location.Create(640, 640, 0, dimension.Id);
                        regionManager.CreateDynamicRegion(source, destination);
                        return true;
                    },
                    Permission = Permission.SystemAdministrator
                },
                new GenericGameCommand
                {
                    Name = "minimapclick",
                    CommandFunc = async (character, arguments) =>
                    {
                        await Task.CompletedTask;
                        EventHappened? happ = null!;
                        happ = character.RegisterEventHandler(new EventHappened<WalkAllowEvent>(e =>
                        {
                            if (e.Minimap)
                            {
                                character.Movement.Teleport(Teleport.Create(e.TargetLocation));
                                return true;
                            }

                            //character.UnregisterEventHandler<WalkAllowEvent>(happ);
                            return false;
                        }));
                        return true;
                    },
                    Permission = Permission.GameAdministrator
                }
            };

            foreach (var command in cmd.Union(commands))
            {
                try
                {
                    _commands.TryAdd(command.Name, command);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed loading command {Name}", command.Name);
                }
            }
        }

        /// <summary>
        ///     Handles a given execute.
        /// </summary>
        /// <param name="command">The name of the command to execute.</param>
        /// <param name="character">The character.</param>
        /// <param name="args">The arguments.</param>
        /// <returns>
        ///     Command has been handled
        /// </returns>
        public async ValueTask<bool> ExecuteAsync(string command, ICharacter character, string[] args)
        {
            if (!_commands.TryGetValue(command, out var cmd) || !character.Permissions.HasAtLeastXPermission(cmd.Permission))
            {
                return false;
            }

            var eventArgs = new GameCommandArgs(character, args);
            await cmd.Execute(eventArgs);
            return eventArgs.Handled;
        }
    }
}