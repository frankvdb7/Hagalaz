import re

with open('Hagalaz.Data/Entities/Character.cs', 'r') as f:
    content = f.read()

# Update collection property declarations to initialize with null! if they are virtual.
# ICollection<T> properties.
content = re.sub(r'public virtual ICollection<([A-Za-z]+)> ([A-Za-z]+) \{ get; set; \}',
                 r'public virtual ICollection<\1> \2 { get; set; } = null!;', content)

# Special cases
content = re.sub(r'public virtual ICollection<Clan> Clans \{ get; set; \}', r'public virtual ICollection<Clan> Clans { get; set; } = null!;', content)
content = re.sub(r'public virtual ICollection<Aspnetrole> Roles \{ get; set; \}', r'public virtual ICollection<Aspnetrole> Roles { get; set; } = null!;', content)

# For the second constructor, add a targeted suppression for the whole constructor if it's too many fields.
# But let's see if initializing properties helps.
content = re.sub(r'public Character\(string userName\) : base\(userName\)\s+\{',
                 r'#pragma warning disable CS8618\n        public Character(string userName) : base(userName)\n#pragma warning restore CS8618\n        {',
                 content)

with open('Hagalaz.Data/Entities/Character.cs', 'w') as f:
    f.write(content)
