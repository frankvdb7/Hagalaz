using Hagalaz.Game.Abstractions.Model.Creatures.Characters;
using Hagalaz.Game.Messages.Protocol;

namespace Hagalaz.Services.GameWorld.Model.Creatures.Characters
{
    public partial class Character : Creature
    {
        public void OnCharacterClicked(CharacterClickType clickType, bool forceRun, ICharacter target) =>
            _optionHandlers[(int)clickType - 1]?.Invoke(target, forceRun);

        public void SendChatMessage(
            string message, ChatMessageType type = ChatMessageType.ChatboxText, string? displayName = null, string? previousDisplayName = null) =>
            Session.SendMessage(new ChatMessage
            {
                Type = type, DisplayName = displayName, PreviousDisplayName = previousDisplayName, Text = message
            });
    }
}