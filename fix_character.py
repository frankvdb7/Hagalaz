import re

with open('Hagalaz.Data/Entities/Character.cs', 'r') as f:
    content = f.read()

# Remove the pragmas
content = re.sub(r'#pragma warning disable CS8618\s+', '', content)
content = re.sub(r'#pragma warning restore CS8618\s+', '', content)

# Redo the second constructor suppression (targeted)
content = re.sub(r'public Character\(string userName\)\s+: base\(userName\)\s+\{',
                 r'#pragma warning disable CS8618\n        public Character(string userName) : base(userName)\n#pragma warning restore CS8618\n        {',
                 content)

# Refactor properties in constructor to initialize properly
# We will do this by ensuring all collection navigation properties are initialized.
# The user wants targeted suppression if initialization is not possible.
# Actually, the user suggested "initialize properties with null! (as done for the virtual properties below)".
# In Character(), they are already initialized with new HashSet<T>().
# But the non-virtual ones like DisplayName, RegisterIp are NOT.

# Update DisplayName and RegisterIp to null! in the field declarations if not already
content = re.sub(r'public string DisplayName \{ get; set; \} = null!;', r'public string DisplayName { get; set; } = null!;', content)
content = re.sub(r'public string RegisterIp \{ get; set; \} = null!;', r'public string RegisterIp { get; set; } = null!;', content)

# For virtual navigation properties that are NOT collections:
# CharacterProfile, CharactersFamiliar, etc. are initialized with null! in my previous edit.
# Let's check them.

with open('Hagalaz.Data/Entities/Character.cs', 'w') as f:
    f.write(content)
