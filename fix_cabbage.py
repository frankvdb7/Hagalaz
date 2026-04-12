import re

with open('Hagalaz.Game.Scripts/GameObjects/Cabbage.rsobj.cs', 'r') as f:
    content = f.read()

# Add field
content = re.sub(r'private readonly IItemBuilder _itemBuilder;',
                 r'private readonly IItemBuilder _itemBuilder;\n        private readonly IMapRegionService _mapRegionService;',
                 content)

# Update constructor
content = re.sub(r'public Cabbage\(IRsTaskService rsTaskService, IItemBuilder itemBuilder\)\n        \{',
                 r'public Cabbage(IRsTaskService rsTaskService, IItemBuilder itemBuilder, IMapRegionService mapRegionService)\n        {',
                 content)
content = re.sub(r'_itemBuilder = itemBuilder;',
                 r'_itemBuilder = itemBuilder;\n            _mapRegionService = mapRegionService;',
                 content)

# Update usage
content = re.sub(r'var region = clicker\.ServiceProvider\.GetRequiredService<IMapRegionService>\(\)\n\s+\.GetOrCreateMapRegion',
                 r'var region = _mapRegionService.GetOrCreateMapRegion',
                 content)

with open('Hagalaz.Game.Scripts/GameObjects/Cabbage.rsobj.cs', 'w') as f:
    f.write(content)
