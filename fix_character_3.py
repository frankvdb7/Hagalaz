import re

with open('Hagalaz.Data/Entities/Character.cs', 'r') as f:
    content = f.read()

# Add obsolete suppression for specific properties
obsolete_props = ['MinigamesBarrow', 'MinigamesDuelArena', 'MinigamesGodwar', 'MinigamesTzhaarCave', 'CharactersPermission']
for prop in obsolete_props:
    content = re.sub(rf'public virtual ([A-Za-z]+) {prop} {{ get; set; \}} = null!;',
                     rf'#pragma warning disable CS0618\n        public virtual \1 {prop} {{ get; set; }} = null!;\n#pragma warning restore CS0618',
                     content)

# Also handle ICollection version of CharactersPermission if it exists
content = re.sub(r'public virtual ICollection<CharactersPermission> CharactersPermissions \{ get; set; \} = null!;',
                 r'#pragma warning disable CS0618\n        public virtual ICollection<CharactersPermission> CharactersPermissions { get; set; } = null!;\n#pragma warning restore CS0618',
                 content)

with open('Hagalaz.Data/Entities/Character.cs', 'w') as f:
    f.write(content)
