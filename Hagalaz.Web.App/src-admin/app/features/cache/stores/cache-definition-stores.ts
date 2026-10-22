import { CacheAnimationsStore } from "./cache-animations.store";
import { CacheClientMapDefinitionsStore } from "./cache-client-map-definitions.store";
import { CacheConfigDefinitionsStore } from "./cache-config-definitions.store";
import { CacheCs2Store } from "./cache-cs2.store";
import { CacheCs2IntStore } from "./cache-cs2-int.store";
import { CacheGraphicsStore } from "./cache-graphics.store";
import { CacheItemsStore } from "./cache-items.store";
import { CacheMapsStore } from "./cache-maps.store";
import { CacheNpcsStore } from "./cache-npcs.store";
import { CacheObjectsStore } from "./cache-objects.store";
import { CacheQuestsStore } from "./cache-quests.store";
import { CacheSpriteDefinitionsStore } from "./cache-sprite-definitions.store";
import { CacheVarpBitsStore } from "./cache-varp-bits.store";

export const cacheDefinitionStores = {
    items: CacheItemsStore,
    npcs: CacheNpcsStore,
    objects: CacheObjectsStore,
    animations: CacheAnimationsStore,
    maps: CacheMapsStore,
    sprites: CacheSpriteDefinitionsStore,
    quests: CacheQuestsStore,
    graphics: CacheGraphicsStore,
    "varp-bits": CacheVarpBitsStore,
    "client-map-definitions": CacheClientMapDefinitionsStore,
    "config-definitions": CacheConfigDefinitionsStore,
    cs2: CacheCs2Store,
    "cs2-int": CacheCs2IntStore,
};
