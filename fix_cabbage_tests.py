import re

with open('Hagalaz.Game.Scripts.Tests/GameObjects/CabbageTests.cs', 'r') as f:
    content = f.read()

# Add field
content = re.sub(r'private IItemBuilder _itemBuilder = null!;',
                 r'private IItemBuilder _itemBuilder = null!;\n        private IMapRegionService _mapRegionService = null!;',
                 content)

# Update Initialize
content = re.sub(r'_itemBuilder = Substitute\.For<IItemBuilder>\(\);',
                 r'_itemBuilder = Substitute.For<IItemBuilder>();\n            _mapRegionService = Substitute.For<IMapRegionService>();',
                 content)
content = re.sub(r'_cabbage = new Cabbage\(_rsTaskService, _itemBuilder\);',
                 r'_cabbage = new Cabbage(_rsTaskService, _itemBuilder, _mapRegionService);',
                 content)

# Update test
content = re.sub(r'var mapRegionServiceMock = Substitute\.For<IMapRegionService>\(\);\n\s+mapRegionServiceMock\.GetOrCreateMapRegion\(123, 0, false\)\.Returns\(regionMock\);\n\s+_character\.ServiceProvider\.GetService\(typeof\(IMapRegionService\)\)\.Returns\(mapRegionServiceMock\);',
                 r'_mapRegionService.GetOrCreateMapRegion(123, 0, false).Returns(regionMock);',
                 content)

with open('Hagalaz.Game.Scripts.Tests/GameObjects/CabbageTests.cs', 'w') as f:
    f.write(content)
