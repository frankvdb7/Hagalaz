import re

with open('Hagalaz.Data/Entities/Character.cs', 'r') as f:
    content = f.read()

# targeted suppression for constructor
content = re.sub(r'public Character\(string userName\)\n\s+: base\(userName\)\n\s+\{',
                 r'#pragma warning disable CS8618\n        public Character(string userName) : base(userName)\n#pragma warning restore CS8618\n        {',
                 content)

# targeted suppression for obsolete property DisplayName if it was marked as such
# actually the warning is about CharactersPermission
content = re.sub(r'public string DisplayName \{ get; set; \} = null!;',
                 r'public string DisplayName { get; set; } = null!;', content)

# suppressing obsolete for CharactersPermission in non-virtual property if it exists
content = re.sub(r'public virtual CharactersPermission CharactersPermission \{ get; set; \} = null!;',
                 r'#pragma warning disable CS0618\n        public virtual CharactersPermission CharactersPermission { get; set; } = null!;\n#pragma warning restore CS0618',
                 content)

with open('Hagalaz.Data/Entities/Character.cs', 'w') as f:
    f.write(content)
