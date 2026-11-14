import { Component, computed, input, model } from "@angular/core";

/** Test boundary for `cache-item-sprite-thumbnail`; records the bound inputs without loading sprite data. */
@Component({ selector: "cache-item-sprite-thumbnail", template: "" })
export class ItemSpriteBoundary {
    readonly id = input.required<number>();
    readonly name = input("");
    readonly size = input(44);
    readonly revision = input<unknown>();
    readonly quantity = input<number>();
    readonly outline = input<number>();
    readonly shadow = input<string>();
    /** Snapshot of every bound input, for asserting the parent's complete binding. */
    readonly bound = computed(() => ({
        id: this.id(),
        name: this.name(),
        size: this.size(),
        revision: this.revision(),
        quantity: this.quantity(),
        outline: this.outline(),
        shadow: this.shadow(),
    }));
}

/** Test boundary for `cache-model-preview`; records the bound inputs without rendering WebGL. */
@Component({ selector: "cache-model-preview", template: "" })
export class ModelPreviewBoundary {
    readonly workspace = input(false);
    readonly inspectionTab = model(0);
    readonly definition = input<unknown>();
    readonly modelId = input<number | null>(null);
    readonly itemId = input<number | null>(null);
    readonly height = input(360);
    /** Snapshot of every bound input, for asserting the parent's complete binding. */
    readonly bound = computed(() => ({
        workspace: this.workspace(),
        inspectionTab: this.inspectionTab(),
        definition: this.definition(),
        modelId: this.modelId(),
        itemId: this.itemId(),
        height: this.height(),
    }));
}
