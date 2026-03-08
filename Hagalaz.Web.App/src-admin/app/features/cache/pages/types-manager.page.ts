import { ChangeDetectionStrategy, Component, inject, signal, computed } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { CommonModule } from "@angular/common";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDividerModule } from "@angular/material/divider";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatProgressSpinnerModule } from "@angular/material/progress-spinner";
import { MatSelectModule } from "@angular/material/select";
import { MatIconModule } from "@angular/material/icon";
import { MatTableModule } from "@angular/material/table";
import { MatTabsModule } from "@angular/material/tabs";
import { TypeKind } from "../services/cache.models";
import { CacheTypesStore } from "../services/cache-types.store";

import { RunicCardComponent } from "../../../core/components/runic-card/runic-card.component";
import { JsonPropertyViewerComponent } from "../../../core/components/json-viewer/json-viewer.component";

@Component({
    standalone: true,
    selector: "types-manager-page",
    imports: [
        CommonModule,
        ReactiveFormsModule,
        MatButtonModule,
        MatCardModule,
        MatChipsModule,
        MatDividerModule,
        MatFormFieldModule,
        MatInputModule,
        MatProgressBarModule,
        MatProgressSpinnerModule,
        MatSelectModule,
        MatIconModule,
        MatTableModule,
        MatTabsModule,
        RunicCardComponent,
        JsonPropertyViewerComponent,
    ],
    templateUrl: "./types-manager.page.html",
    styleUrl: "./types-manager.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TypesManagerPageComponent {
    private readonly fb = inject(FormBuilder);
    readonly store = inject(CacheTypesStore);

    readonly kinds: TypeKind[] = ["items", "npcs", "objects", "sprites", "quests", "animations", "graphics", "maps", "varp-bits", "client-map-definitions", "config-definitions", "cs2", "cs2-int"];
    readonly searchableKinds: Array<"items" | "npcs" | "objects" | "sprites"> = ["items", "npcs", "objects", "sprites"];

    readonly searchForm = this.fb.nonNullable.group({ 
        kind: ["items" as "items" | "npcs" | "objects" | "sprites", Validators.required], 
        query: ["", Validators.required], 
        offset: [0, Validators.min(0)], 
        limit: [20, [Validators.min(1), Validators.max(200)]] 
    });

    readonly npcMutationForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)], combatLevel: [1, Validators.min(0)] });
    readonly objectMutationForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)], name: ["", Validators.required] });
    readonly varpMutationForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)], configId: [0, Validators.min(0)], bitLength: [1, Validators.min(0)], bitOffset: [0, Validators.min(0)] });
    readonly configMutationForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)], defaultValue: [0], valueType: ["i", [Validators.required, Validators.minLength(1), Validators.maxLength(1)]] });

    readonly selectedEntity = signal<any | null>(null);
    readonly displayedColumns = ["id", "name", "actions"];

    // Map search results to table data
    readonly tableData = computed(() => {
        const result = this.store.searchResult();
        if (!result) return [];
        return result.items.map((item: any) => ({
            id: item.id ?? item.identifier ?? '?',
            name: item.name ?? item.displayName ?? item.username ?? 'Unknown Entity',
            raw: item
        }));
    });

    async loadArchiveSizes(): Promise<void> {
        this.store.loadArchiveSizes();
    }

    async search(): Promise<void> {
        const { kind, query, offset, limit } = this.searchForm.getRawValue();
        this.store.search({ kind, query, offset, limit });
    }

    inspect(entity: any): void {
        this.selectedEntity.set(entity.raw);
        
        // Auto-fill forge forms based on type and ID
        const id = entity.id;
        const kind = this.searchForm.getRawValue().kind;

        if (kind === 'npcs') this.npcMutationForm.patchValue({ id });
        if (kind === 'objects') this.objectMutationForm.patchValue({ id });
        // etc
    }

    async setNpcCombatLevel(): Promise<void> {
        const { id, combatLevel } = this.npcMutationForm.getRawValue();
        this.store.updateNpcCombatLevel({ id, combatLevel });
    }

    async setObjectName(): Promise<void> {
        const { id, name } = this.objectMutationForm.getRawValue();
        this.store.updateObjectName({ id, name });
    }

    async setVarpBit(): Promise<void> {
        const { id, configId, bitLength, bitOffset } = this.varpMutationForm.getRawValue();
        this.store.updateVarpBit({ id, configId, bitLength, bitOffset });
    }

    async setConfigDefinition(): Promise<void> {
        const { id, defaultValue, valueType } = this.configMutationForm.getRawValue();
        this.store.updateConfigDefinition({ id, defaultValue, valueType });
    }
}
