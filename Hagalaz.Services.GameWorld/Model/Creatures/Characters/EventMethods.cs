using System;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Features;
using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Abstractions.Model.Events;
using Hagalaz.Game.Common.Events.Character.Packet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
