import re

def cleanup_file(filepath):
    with open(filepath, 'r') as f:
        content = f.read()

    # Remove ! after null checks for Target
    # if (Target != null) { Target! -> Target
    content = re.sub(r'if \((Target) != null\)\s*\{\s*\1!', r'if (\1 != null)\n            {\n                \1', content)

    # Remove ! on indexer result followed by null check
    # Container![i]! == null -> Container![i] == null
    content = re.sub(r'(\w+Container!\[[^\]]+\])! == null', r'\1 == null', content)
    content = re.sub(r'(\w+Container!\[[^\]]+\])! != null', r'\1 != null', content)

    # Remove ! when accessing member after null check
    # if (Prop != null && Prop!.IsOpened) -> if (Prop != null && Prop.IsOpened)
    props = ['SelfInterface', 'TargetInterface', 'SelfOverlay', 'TargetOverlay', 'SelfContainer', 'TargetContainer']
    for prop in props:
        content = re.sub(rf'if \({prop} != null && {prop}!\.', rf'if ({prop} != null && {prop}.', content)

    with open(filepath, 'w') as f:
        f.write(content)

cleanup_file('Hagalaz.Game.Scripts/Characters/TradingCharacterScript.cs')
cleanup_file('Hagalaz.Game.Scripts/Minigames/DuelArena/DuelArenaScript.cs')
