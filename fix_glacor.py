import sys

path = 'Hagalaz.Game.Scripts/Npcs/Elementals/Glacor.rsnpc.cs'
with open(path, 'r') as f:
    content = f.read()

old = 'glacyte.QueueTask(new RsTask(() => glacyte.Combat.SetTarget(Owner.Combat.Target!), 1));'
new = 'glacyte.QueueTask(new RsTask(() => { if (Owner.Combat.Target != null) glacyte.Combat.SetTarget(Owner.Combat.Target); }, 1));'

if old in content:
    content = content.replace(old, new)
    with open(path, 'w') as f:
        f.write(content)
    print("Fixed Glacor.rsnpc.cs")
else:
    print("Could not find line in Glacor.rsnpc.cs")
