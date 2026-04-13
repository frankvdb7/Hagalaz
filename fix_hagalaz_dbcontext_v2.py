import re

with open('Hagalaz.Data/HagalazDbContext.cs', 'r') as f:
    lines = f.readlines()

new_lines = []
obsolete_sets = ['CharactersPermission', 'CharactersPreference', 'CharactersSlayerTask', 'MinigamesBarrow', 'MinigamesDuelArena', 'MinigamesGodwar', 'MinigamesTzhaarCave']

in_obsolete_block = False
for line in lines:
    if '#pragma warning' in line:
        continue

    # Check if this is an obsolete DbSet property
    is_obsolete = any(name in line for name in obsolete_sets) and 'public virtual DbSet<' in line

    if is_obsolete:
        if not in_obsolete_block:
            new_lines.append('        #pragma warning disable CS0618\n')
            in_obsolete_block = True
    else:
        if in_obsolete_block:
            new_lines.append('        #pragma warning restore CS0618\n')
            in_obsolete_block = False

    # Fix indentation for properties
    if 'public virtual DbSet<' in line:
        line = '        ' + line.strip() + '\n'

    new_lines.append(line)

content = "".join(new_lines)

with open('Hagalaz.Data/HagalazDbContext.cs', 'w') as f:
    f.write(content)
