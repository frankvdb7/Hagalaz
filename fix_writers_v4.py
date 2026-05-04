import os

def patch_file(path, replacements):
    with open(path, 'r') as f:
        content = f.read()
    for old, new in replacements:
        if old not in content:
            print(f"Warning: pattern not found in {path}")
            # Try to find a slightly different version (e.g. whitespace)
            import re
            escaped_old = re.escape(old).replace(r'\ ', r'\s+')
            content = re.sub(escaped_old, new, content)
        else:
            content = content.replace(old, new)
    with open(path, 'w') as f:
        f.write(content)

# Character Writer
char_replacements = [
    (
        """            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Animation))
            {
                for (int i = 0; i < 4; i++)
                    output.WriteInt32BigEndianSmart(character.RenderInformation.CurrentAnimation.Id);
                output.WriteByteA((byte)character.RenderInformation.CurrentAnimation.Delay);
            }""",
        """            if (updateFlag.HasFlag(Game.Abstractions.Model.Creatures.Characters.UpdateFlags.Animation))
            {
                var animation = character.RenderInformation.CurrentAnimation;
                for (int i = 0; i < 4; i++)
                    output.WriteInt32BigEndianSmart(animation?.Id ?? 0);
                output.WriteByteA((byte)(animation?.Delay ?? 0));
            }"""
    ),
    (
        "public void WriteItemAppearance(ICharacter character, IByteBufferWriter output)\n        {",
        "public void WriteItemAppearance(ICharacter character, IByteBufferWriter output)\n        {\n            if (_bodyDataRepository == null) return;"
    )
]
patch_file('Hagalaz.Services.GameWorld/Network/Protocol/742/CharacterRenderMasksWriter.cs', char_replacements)

# NPC Writer
npc_replacements = [
    (
        """            if ((updateFlag & UpdateFlags.Animation) != 0)
            {
                for (int i = 0; i < 4; i++)
                    output.WriteInt32BigEndianSmart(npc.RenderInformation.CurrentAnimation.Id);
                output.WriteByteC((byte)npc.RenderInformation.CurrentAnimation.Delay);
            }""",
        """            if ((updateFlag & UpdateFlags.Animation) != 0)
            {
                var animation = npc.RenderInformation.CurrentAnimation;
                for (int i = 0; i < 4; i++)
                    output.WriteInt32BigEndianSmart(animation?.Id ?? 0);
                output.WriteByteC((byte)(animation?.Delay ?? 0));
            }"""
    )
]
patch_file('Hagalaz.Services.GameWorld/Network/Protocol/742/NpcRenderMasksWriter.cs', npc_replacements)
