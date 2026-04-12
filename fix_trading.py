import re

with open('Hagalaz.Game.Scripts/Characters/TradingCharacterScript.cs', 'r') as f:
    lines = f.readlines()

new_lines = []
for line in lines:
    # Replace SelfContainer and TargetContainer with ! when accessed
    line = re.sub(r'(SelfContainer|TargetContainer)([\.\[])', r'\1!\2', line)
    # Special case for passed as argument
    line = re.sub(r', (SelfContainer|TargetContainer),', r', \1!,', line)
    line = re.sub(r', (SelfContainer|TargetContainer)\)', r', \1!)', line)
    new_lines.append(line)

with open('Hagalaz.Game.Scripts/Characters/TradingCharacterScript.cs', 'w') as f:
    f.writelines(new_lines)
