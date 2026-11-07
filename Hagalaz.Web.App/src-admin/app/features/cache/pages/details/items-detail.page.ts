import { MatSliderModule } from "@angular/material/slider";
import { MatInputModule } from "@angular/material/input";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatExpansionModule } from "@angular/material/expansion";
import { MatChipsModule } from "@angular/material/chips";
import { MatCardModule } from "@angular/material/card";
import { ClipboardModule } from "@angular/cdk/clipboard";
import { FormsModule } from "@angular/forms";
import { MatDialog } from "@angular/material/dialog";
import { MatSlideToggleModule } from "@angular/material/slide-toggle";
import { MatSelectModule } from "@angular/material/select";
import { firstValueFrom } from "rxjs";
import { CachePendingChangesService } from "../../cache-pending-changes.service";
import { CacheWriteDialogComponent } from "../cache-write-dialog";
import { ItemSpriteThumbnailComponent } from "../item-sprite-thumbnail.component";
import { JsonPipe } from "@angular/common";
import { Component, computed, DestroyRef, effect, inject, linkedSignal, signal, untracked } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatIconModule } from "@angular/material/icon";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatTabsModule } from "@angular/material/tabs";
import { Router, RouterLink } from "@angular/router";
import { PortalBreadcrumbDirective } from "../../../../layout/portal-breadcrumb.directive";
import type { GetItemDefinitionDto } from "../../services/cache.models";
import { CacheItemsStore } from "../../stores/cache-items.store";
import { injectCacheDefinitionId } from "../cache-definition-id";
import { DefinitionPickerComponent } from "../definition-picker.component";
import { type ItemField, ItemFieldsComponent } from "../item-fields.component";
import { CacheModelPreviewComponent } from "../cache-model-preview.component";

@Component({
    selector: "cache-items-detail-page",
    imports: [
        JsonPipe,
        FormsModule,
        ClipboardModule,
        MatSlideToggleModule,
        ItemSpriteThumbnailComponent,
        RouterLink,
        MatCardModule,
        MatChipsModule,
        MatExpansionModule,
        MatFormFieldModule,
        MatInputModule,
        MatSliderModule,
        MatSelectModule,

        MatButtonModule,
        MatIconModule,
        MatProgressBarModule,
        MatTabsModule,
        ItemFieldsComponent,
        DefinitionPickerComponent,
        PortalBreadcrumbDirective,
        CacheModelPreviewComponent,
    ],
    providers: [CacheItemsStore],
    templateUrl: "./items-detail.page.html",
    host: { class: "block min-w-0" },
    styleUrl: "./items-detail.page.scss",
})
export class ItemsDetailPageComponent {
    readonly store = inject(CacheItemsStore);
    private readonly dialog = inject(MatDialog);
    private readonly router = inject(Router);
    private readonly pending = inject(CachePendingChangesService);
    readonly id = injectCacheDefinitionId();
    readonly item = computed(() => {
        const item = this.store.byIdResult();
        return item?.id === this.id() ? item : null;
    });
    readonly selectedTab = signal(0);
    readonly spriteZoom = signal(1);
    readonly previewSize = computed(() => Math.round(88 * this.spriteZoom()));
    readonly previewQuantity = signal(1);
    readonly previewOutline = signal<0 | 1 | 2>(1);
    readonly previewShadow = signal(true);
    readonly previewShadowColour = signal("#302020");
    readonly previewShadowParameter = computed(() => (this.previewShadow() ? this.previewShadowColour().slice(1) : "none"));
    readonly draft = linkedSignal<GetItemDefinitionDto | null>(() => (this.item() ? structuredClone(this.item()!) : null));
    readonly dirty = computed(() => JSON.stringify(this.draft()) !== JSON.stringify(this.item()));
    readonly valid = computed(() => {
        const draft = this.draft();
        return (
            !!draft &&
            !!draft.name?.trim() &&
            draft.name.length <= 50 &&
            [...draft.name].every((character) => character.charCodeAt(0) >= 32 && character.charCodeAt(0) <= 255) &&
            Number.isInteger(draft.value) &&
            draft.value! >= 0 &&
            draft.value! <= 2147483647
        );
    });
    readonly canSave = computed(() => this.dirty() && this.valid() && !this.store.loading());

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
    readonly visual = computed<ItemField[]>(() => [
        { label: "Inventory model ID", value: this.reference(this.item()?.interfaceModelId) },
        { label: "Model scale X", value: this.value(this.item()?.scaleX) },
        { label: "Model scale Y", value: this.value(this.item()?.scaleY) },
        { label: "Model scale Z", value: this.value(this.item()?.scaleZ) },
        { label: "Model rotation X", value: this.value(this.item()?.modelRotation1) },
        { label: "Model rotation Y", value: this.value(this.item()?.modelRotation2) },
        { label: "Model rotation Z", value: this.value(this.item()?.zan2D) },
        { label: "Model offset X", value: this.value(this.item()?.modelOffset1) },
        { label: "Model offset Y", value: this.value(this.item()?.modelOffset2) },
    ]);
    readonly actions = computed(() => [
        { label: "Inventory actions", slots: this.item()?.inventoryOptions },
        { label: "Ground actions", slots: this.item()?.groundOptions },
    ]);

    constructor() {
        this.pending.register({ dirty: this.dirty, saving: this.store.saving }, inject(DestroyRef));
        effect(() => {
            const result = this.store.mutationResult();
            if (result?.operation === "item-duplicated" && !this.store.saving() && result.id !== this.id())
                void this.router.navigate(["/cache/types/items", result.id], { queryParamsHandling: "preserve" });
        });
        effect(() => {
            const id = this.id();
            if (id !== null) untracked(() => this.store.loadById(id));
        });
    }

    setPreviewQuantity(value: string): void {
        const quantity = Number(value);
        if (Number.isSafeInteger(quantity) && quantity >= -1 && quantity <= 2147483647) this.previewQuantity.set(quantity);
    }

    updateDraft(changes: Partial<GetItemDefinitionDto>): void {
        this.draft.update((current) => (current ? { ...current, ...changes } : null));
    }

    async save(): Promise<void> {
        if (!this.canSave()) return;
        const draft = structuredClone(this.draft()!);
        if (await this.confirm(`Save the complete definition for item #${draft.id}?`)) this.store.updateItem(draft);
    }

    async duplicate(): Promise<void> {
        if (!this.item() || this.store.loading() || this.dirty()) return;
        if (await this.confirm(`Duplicate persisted item #${this.item()!.id} into a new definition?`)) this.store.duplicateItem(this.item()!.id);
    }

    resetDraft(): void {
        this.draft.set(this.item() ? structuredClone(this.item()!) : null);
    }

    reload(): void {
        const load = () => {
            const id = this.id();
            if (id !== null) this.store.loadById(id);
        };
        if (this.dirty())
            void this.pending.canLeave().then((leave) => {
                if (leave) load();
            });
        else load();
    }

    private async confirm(description: string): Promise<boolean> {
        return (await firstValueFrom(this.dialog.open(CacheWriteDialogComponent, { data: description, maxWidth: "32rem" }).afterClosed())) === true;
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
