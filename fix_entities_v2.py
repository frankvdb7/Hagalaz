import re

with open('Hagalaz.Data/Entities/Character.cs', 'r') as f:
    lines = f.readlines()

new_lines = []
obsolete_names = ['MinigamesBarrow', 'MinigamesDuelArena', 'MinigamesGodwar', 'MinigamesTzhaarCave', 'CharactersPermissions', 'CharactersPermission']

in_obsolete_block = False
for line in lines:
    if '#pragma warning' in line:
        continue

    # Check if this is an obsolete property
    is_obsolete = any(name in line for name in obsolete_names) and 'public virtual' in line

    if is_obsolete:
        if not in_obsolete_block:
            new_lines.append('        #pragma warning disable CS0618\n')
            in_obsolete_block = True
    else:
        if in_obsolete_block:
            new_lines.append('        #pragma warning restore CS0618\n')
            in_obsolete_block = False

    # Fix indentation for properties
    if 'public virtual' in line:
        line = '        ' + line.strip() + '\n'
    elif 'public string' in line or 'public DateTime' in line or 'public short' in line or 'public byte' in line:
        line = '        ' + line.strip() + '\n'

    new_lines.append(line)

content = "".join(new_lines)
with open('Hagalaz.Data/Entities/Character.cs', 'w') as f:
    f.write(content)
