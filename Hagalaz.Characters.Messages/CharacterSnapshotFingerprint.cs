using System.Security.Cryptography;
using System.Text.Json;

namespace Hagalaz.Characters.Messages;

public static class CharacterSnapshotFingerprint
{
    public static string Compute(ICharacterPersistenceMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);

        var content = new
        {
            message.MasterId,
            message.Appearance,
            message.Details,
            message.Statistics,
            message.ItemCollection,
            message.Familiar,
            message.Music,
            message.Farming,
            message.Slayer,
            message.Notes,
            message.Profile,
            message.ItemAppearanceCollection,
            message.State
        };

        return Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(content)));
    }
}
