import re

with open('Hagalaz.Game.Scripts/Characters/TradingCharacterScript.cs', 'r') as f:
    content = f.read()

# Fix CloseWidget calls
content = re.sub(r'Character\.Widgets\.CloseWidget\(SelfInterface\);', r'Character.Widgets.CloseWidget(SelfInterface!);', content)
content = re.sub(r'Target!\.Widgets\.CloseWidget\(TargetInterface\);', r'Target!.Widgets.CloseWidget(TargetInterface!);', content)
content = re.sub(r'Character\.Widgets\.CloseWidget\(SelfOverlay\);', r'Character.Widgets.CloseWidget(SelfOverlay!);', content)
content = re.sub(r'Target!\.Widgets\.CloseWidget\(TargetOverlay\);', r'Target!.Widgets.CloseWidget(TargetOverlay!);', content)

# Fix loop indices and access
content = re.sub(r'SelfContainer!\[\(short\)i\]', r'SelfContainer![(short)i]!', content)
content = re.sub(r'TargetContainer!\[\(short\)i\]', r'TargetContainer![(short)i]!', content)

with open('Hagalaz.Game.Scripts/Characters/TradingCharacterScript.cs', 'w') as f:
    f.write(content)
