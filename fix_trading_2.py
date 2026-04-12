import re

with open('Hagalaz.Game.Scripts/Characters/TradingCharacterScript.cs', 'r') as f:
    content = f.read()

# Replace properties with ! when accessed, but ONLY when it makes sense (simple access)
props = ['Target', 'SelfInterface', 'TargetInterface', 'SelfOverlay', 'TargetOverlay', 'SelfIntInputHandler', 'TargetIntInputHandler']
for prop in props:
    content = re.sub(rf'({prop})([\.\[])', rf'\1!\2', content)

# Special cases for SelfInterface and TargetInterface method calls
content = re.sub(r'(SelfInterface|TargetInterface|SelfOverlay|TargetOverlay)\.([A-Za-z]+)\(', r'\1!.\2(', content)

with open('Hagalaz.Game.Scripts/Characters/TradingCharacterScript.cs', 'w') as f:
    f.write(content)
