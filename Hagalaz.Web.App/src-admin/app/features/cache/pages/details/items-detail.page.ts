import { JsonPipe } from "@angular/common";
import { Component, computed, effect, inject, untracked } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTabsModule } from "@angular/material/tabs";
import { RouterLink } from "@angular/router";
import { PortalBreadcrumbDirective } from "../../../../layout/portal-breadcrumb.directive";
import type { ItemDefinitionDto } from "../../services/cache.models";
import { CacheItemsStore } from "../../stores/cache-items.store";
import { injectCacheDefinitionId } from "../cache-definition-id";
import { DefinitionPickerComponent } from "../definition-picker.component";
import { type ItemField, ItemFieldsComponent } from "../item-fields.component";

@Component({
    selector: "cache-items-detail-page",
    imports: [
        JsonPipe,
        RouterLink,
        MatButtonModule,
        MatCardModule,
        MatChipsModule,
        MatIconModule,
        MatProgressBarModule,
        MatTabsModule,
        ItemFieldsComponent,
        DefinitionPickerComponent,
        PortalBreadcrumbDirective,
    ],
    providers: [CacheItemsStore],
    templateUrl: "./items-detail.page.html",
    styleUrl: "./items-detail.page.scss",
})
export class ItemsDetailPageComponent {
    readonly store = inject(CacheItemsStore);
    readonly id = injectCacheDefinitionId();
    readonly item = computed(() => {
        const item = this.store.byIdResult() as ItemDefinitionDto | null;
        return item?.id === this.id() ? item : null;
    });
    readonly overview = computed<ItemField[]>(() => {
        const item = this.item();
        return [
            { label: "Definition ID", value: this.value(item?.id) },
            { label: "Display name", value: item?.name || "Not supplied" },
            { label: "Cache base value", value: this.value(item?.value) },
            { label: "Stackable", value: this.value(item?.stackable) },
            { label: "Noted", value: this.value(item?.noted) },
            { label: "Members only", value: this.value(item?.membersOnly) },
        ];
    });
    readonly equipment = computed<ItemField[]>(() => [
        { label: "Equip slot", value: this.reference(this.item()?.equipSlot) },
        { label: "Equip type", value: this.reference(this.item()?.equipType) },
        { label: "Render animation ID", value: this.reference(this.item()?.renderAnimationId) },
    ]);
    readonly relationships = computed<ItemField[]>(() => {
        const item = this.item();
        return [
            this.relationship("Note item", item?.noteId),
            this.relationship("Note template", item?.noteTemplateId),
            this.relationship("Lending item", item?.lendId),
            this.relationship("Lending template", item?.lendTemplateId),
            this.relationship("Bought item", item?.boughtItemId),
            this.relationship("Bought template", item?.boughtTemplateId),
        ];
    });
    readonly visual = computed<ItemField[]>(() => [{ label: "Inventory model ID", value: this.reference(this.item()?.interfaceModelId) }]);
    readonly actions = computed(() => [
        { label: "Inventory actions", slots: this.item()?.inventoryOptions },
        { label: "Ground actions", slots: this.item()?.groundOptions },
    ]);

    constructor() {
        effect(() => {
            const id = this.id();
            if (id !== null) untracked(() => this.store.loadById(id));
        });
    }

    reload(): void {
        const id = this.id();
        if (id !== null) this.store.loadById(id);
    }

    private value(value: number | boolean | null | undefined): string {
        if (value == null) return "Not supplied";
        if (typeof value === "boolean") return value ? "Yes" : "No";
        return String(value);
    }

    private reference(value: number | null | undefined): string {
        return value == null ? "Not supplied" : value < 0 ? "Not set" : String(value);
    }

    private relationship(label: string, id: number | null | undefined): ItemField {
        return { label, value: this.reference(id), ...(id != null && id >= 0 ? { itemId: id } : {}) };
    }
}
