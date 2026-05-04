import os

def fix_animation(path, flag_type, var_name, delay_suffix):
    with open(path, 'r') as f:
        lines = f.readlines()
    new_lines = []
    i = 0
    while i < len(lines):
        line = lines[i]
        if f'if (updateFlag.HasFlag({flag_type}.UpdateFlags.Animation))' in line or f'if ((updateFlag & UpdateFlags.Animation) != 0)' in line:
            new_lines.append(line)
            new_lines.append("            {\n")
            new_lines.append(f"                var animation = {var_name}.RenderInformation.CurrentAnimation;\n")
            new_lines.append("                for (int j = 0; j < 4; j++)\n")
            new_lines.append("                    output.WriteInt32BigEndianSmart(animation?.Id ?? 0);\n")
            new_lines.append(f"                output.WriteByte{delay_suffix}((byte)(animation?.Delay ?? 0));\n")
            new_lines.append("            }\n")
            i += 1
            # Skip the original block. Assuming it starts with { and ends with }
            brace_count = 0
            started = False
            while i < len(lines):
                if '{' in lines[i]:
                    brace_count += 1
                    started = True
                if '}' in lines[i]:
                    brace_count -= 1
                    started = True
                i += 1
                if started and brace_count == 0:
                    break
            continue
        new_lines.append(line)
        i += 1
    with open(path, 'w') as f:
        f.writelines(new_lines)

fix_animation('Hagalaz.Services.GameWorld/Network/Protocol/742/CharacterRenderMasksWriter.cs', 'Game.Abstractions.Model.Creatures.Characters', 'character', 'A')
fix_animation('Hagalaz.Services.GameWorld/Network/Protocol/742/NpcRenderMasksWriter.cs', 'UpdateFlags', 'npc', 'C')

# BodyDataRepository check
with open('Hagalaz.Services.GameWorld/Network/Protocol/742/CharacterRenderMasksWriter.cs', 'r') as f:
    c = f.read()
c = c.replace('public void WriteItemAppearance(ICharacter character, IByteBufferWriter output)\n        {',
              'public void WriteItemAppearance(ICharacter character, IByteBufferWriter output)\n        {\n            if (_bodyDataRepository == null) return;')
with open('Hagalaz.Services.GameWorld/Network/Protocol/742/CharacterRenderMasksWriter.cs', 'w') as f:
    f.write(c)
