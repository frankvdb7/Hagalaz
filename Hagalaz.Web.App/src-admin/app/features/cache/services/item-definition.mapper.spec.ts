import { CacheItemsStore } from "../stores/cache-items.store";
import { describe, expect, expectTypeOf, it } from "vitest";
import type { GetItemDefinitionDto, UpdateItemDefinitionDto, GetItemTypeDto, SearchDefinitionsDto } from "./dtos";
import { toUpdateItemDefinitionDto } from "./item-definition.mapper";

const definition: GetItemDefinitionDto = {
    id: 7,
    extraData: { 451: 42, 452: "text" },
    interfaceModelId: 7,
    name: "Fixture",
    modelZoom: 7,
    modelRotation1: 7,
    modelRotation2: 7,
    modelOffset1: 7,
    modelOffset2: 7,
    stackableType: 7,
    value: 7,
    membersOnly: true,
    maleWornModelId1: 7,
    maleWornModelId2: 7,
    femaleWornModelId1: 7,
    femaleWornModelId2: 7,
    groundOptions: [null, null, "Take", null, null],
    inventoryOptions: ["Wield", null, null, null, "Drop"],
    originalModelColors: null,
    modifiedModelColors: null,
    originalTextureColors: null,
    modifiedTextureColors: null,
    unknownArray1: null,
    questIDs: null,
    unnoted: true,
    maleWornModelId3: 7,
    femaleWornModelId3: 7,
    multiStackSize: 7,
    maleHeadModel: 7,
    femaleHeadModel: 7,
    maleHeadModel2: 7,
    femaleHeadModel2: 7,
    zan2D: 7,
    unknownInt6: 7,
    noteId: 7,
    noteTemplateId: 7,
    stackIds: null,
    stackAmounts: null,
    scaleX: 7,
    scaleY: 7,
    scaleZ: 7,
    ambient: 7,
    contrast: 7,
    teamId: 7,
    lendId: 7,
    lendTemplateId: 7,
    maleWearXOffset: 7,
    maleWearYOffset: 7,
    maleWearZOffset: 7,
    femaleWearXOffset: 7,
    femaleWearYOffset: 7,
    femaleWearZOffset: 7,
    unknownInt18: 7,
    unknownInt19: 7,
    unknownInt20: 7,
    unknownInt21: 7,
    unknownInt22: 7,
    unknownInt23: 7,
    unknownInt24: 7,
    unknownInt25: 7,
    pickSizeShift: 7,
    boughtItemId: 7,
    boughtTemplateId: 7,
    equipSlot: 7,
    equipType: 7,
    someEquipInt: 7,
    noted: true,
    stackable: true,
    hasWearModel: true,
    degradeType: 7,
    renderAnimationId: 7,
    hasDestroyOption: true,
};

describe("item definition update contract", () => {
    it("retains every stored field and excludes all response-only fields", () => {
        const update: UpdateItemDefinitionDto = toUpdateItemDefinitionDto(definition);
        const { noted, stackable, hasWearModel, degradeType, renderAnimationId, hasDestroyOption, ...stored } = definition;
        expect(update).toEqual(stored);
        expect(update.extraData).toEqual({ 451: 42, 452: "text" });
        expect(update).not.toHaveProperty("stackable");
        expect(update).not.toHaveProperty("noted");
        expect(update).not.toHaveProperty("renderAnimationId");
    });
    it("retains complete resource types through definition, range and retained search state", () => {
        type Store = InstanceType<typeof CacheItemsStore>;
        expectTypeOf<ReturnType<Store["byIdResult"]>>().toEqualTypeOf<GetItemDefinitionDto | null>();
        expectTypeOf<ReturnType<Store["rangeResult"]>>().toEqualTypeOf<GetItemTypeDto[] | null>();
        expectTypeOf<ReturnType<Store["searchResult"]>>().toEqualTypeOf<SearchDefinitionsDto<GetItemTypeDto> | null>();
    });

});
