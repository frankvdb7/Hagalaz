using System;
using System.Text;
using System.Threading.Tasks;
using Hagalaz.Game.Abstractions.Services;
using Hagalaz.Game.Abstractions.Services.Model;
using Hagalaz.Game.Abstractions.Store;

namespace Hagalaz.Services.GameWorld.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class GameMessageService : IGameMessageService
    {
        private readonly ICharacterStore _characterStore;

        /// <summary>
        /// The world specific text.
        /// </summary>
        private const string WorldSpecificText = "<img=3><col=FF8D00>News: ";

        /// <summary>
        /// The world wide text.
        /// </summary>
        private const string WorldWideText = "<img=3><col=FF0000>News: ";

        /// <summary>
        /// The game text.
        /// </summary>
        private const string GameText = "<img=3><col=FF8500>News: ";

        /// <summary>
        /// The friend text.
        /// </summary>
        private const string FriendsText = "<img=3><col=008C00>News: ";

        /// <summary>
        /// The end text.
        /// </summary>
        private const string EndText = "</col>";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="masterConnectionAdapter"></param>
        /// <param name="characterStore"></param>
        public GameMessageService(ICharacterStore characterStore)
        {
            _characterStore = characterStore;
        }


        /// <summary>
        /// Formats the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        private static string FormatMessage(string message, GameMessageType type)
        {
            var builder = new StringBuilder();
            switch (type)
            {
                case GameMessageType.Friends:
                    builder.Append(FriendsText);
                    break;
                case GameMessageType.Game:
                    builder.Append(GameText);
                    break;
                case GameMessageType.WorldSpecific:
                    builder.Append(WorldSpecificText);
                    break;
                case GameMessageType.WorldWide:
                    builder.Append(WorldWideText);
                    break;
                default:
                    throw new NotSupportedException(nameof(type));
            }

            return builder.Append(message).Append(EndText).ToString();
        }

        /// <summary>
        /// Announces the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="type">The type.</param>
        /// <param name="announcerDisplayName">Display name of the announcer.</param>
        public async Task MessageAsync(string message, GameMessageType type, string? announcerDisplayName = null)
        {
            var text = FormatMessage(message, type);
            await foreach (var character in _characterStore.FindAllAsync())
            {
                switch (type)
                {
                    case GameMessageType.WorldSpecific:
                        var friendText = FormatMessage(message, GameMessageType.Friends);
                        // TODO
                        //character.SendMessage(announcerDisplayName != null && character.ContactList.ContainsFriend(announcerDisplayName) ? friendText : text);

                        // send it to any friends not in this world.
                        // TODO
                        //await _masterConnectionAdapter.SendDataAsync(new AddGameMessageRequestPacketComposer(message, GameMessageType.Friends, announcerDisplayName).Serialize());
                        break;
                    case GameMessageType.WorldWide:
                        character.SendChatMessage(text);

                        // send it to anyone not in this world
                        // TODO
                        //await _masterConnectionAdapter.SendDataAsync(new AddGameMessageRequestPacketComposer(message, type).Serialize());
                        break;
                    case GameMessageType.Game:
                        character.SendChatMessage(text);
                        break;
                    case GameMessageType.Friends:
                        // TODO
                        /*if (character.ContactList.ContainsFriend(announcerDisplayName))
                        {
                            character.SendMessage(text);
                        }*/

                        // TODO
                        //await _masterConnectionAdapter.SendDataAsync(new AddGameMessageRequestPacketComposer(message, type, announcerDisplayName).Serialize());
                        break;
                    default:
                        throw new NotSupportedException(nameof(type));
                }
            }
        }
    }
}