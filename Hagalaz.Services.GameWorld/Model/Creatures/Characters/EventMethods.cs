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
using Hagalaz.Game.Abstractions.Builders.Projectile;
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
            RegisterEventHandler(new EventHappened<ConsoleCommandEvent>(e => OnCommandReceived(e.Command).Result));

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