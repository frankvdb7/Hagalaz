import { JsonPipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, inject, signal } from "@angular/core";
import { FormField, form, max, maxLength, min, minLength, required, validate } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatExpansionModule } from "@angular/material/expansion";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatSelectModule } from "@angular/material/select";
import { MatTabsModule } from "@angular/material/tabs";
import type { TypeKind } from "../services/cache.models";
import { CacheTypesStore } from "../stores/cache-types.store";

@Component({
    standalone: true,
    selector: "types-manager-page",
    imports: [
        FormField,
        JsonPipe,
        MatButtonModule,
        MatCardModule,
        MatExpansionModule,
        MatIconModule,
        MatTabsModule,
        MatFormFieldModule,
        MatInputModule,
        MatProgressBarModule,
        MatSelectModule,
    ],
    providers: [CacheTypesStore],
    templateUrl: "./types-manager.page.html",
    styleUrl: "./types-manager.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TypesManagerPageComponent {
    readonly store = inject(CacheTypesStore);

    readonly kinds: TypeKind[] = [
        "items",
        "npcs",
        "objects",
        "sprites",
        "quests",
        "animations",
        "graphics",
        "maps",
        "varp-bits",
        "client-map-definitions",
        "config-definitions",
        "cs2",
        "cs2-int",
    ];
    readonly searchableKinds: Array<"items" | "npcs" | "sprites"> = ["items", "npcs", "sprites"];
    readonly kindLabels: Record<TypeKind, string> = {
        items: "Items",
        npcs: "NPCs",
        objects: "Objects",
        sprites: "Sprites",
        quests: "Quests",
        animations: "Animations",
        graphics: "Graphics",
        maps: "Maps",
        "varp-bits": "Varp bits",
        "client-map-definitions": "Client map definitions",
        "config-definitions": "Config definitions",
        cs2: "CS2 scripts",
        "cs2-int": "CS2 integers",
    };

    private readonly byIdModel = signal({ kind: "items" as TypeKind, id: 0 });
    readonly byIdForm = form(this.byIdModel, (path) => {
        required(path.kind);
        required(path.id);
        min(path.id, 0);
    });
    private readonly rangeModel = signal({ kind: "items" as TypeKind, startId: 0, endIdExclusive: 10 });
    readonly rangeForm = form(this.rangeModel, (path) => {
        required(path.kind);
        required(path.startId);
        min(path.startId, 0);
        required(path.endIdExclusive);
        min(path.endIdExclusive, 1);
        validate(path.endIdExclusive, ({ value, valueOf: readField }) =>
            value() > readField(path.startId) ? undefined : { kind: "range", message: "End ID must be greater than the start ID." }
        );
    });

    private readonly searchModel = signal({ kind: "items" as "items" | "npcs" | "sprites", query: "", offset: 0, limit: 10 });
    readonly searchForm = form(this.searchModel, (path) => {
        required(path.kind);
        required(path.query);
        required(path.offset);
        min(path.offset, 0);
        required(path.limit);
        min(path.limit, 1);
        max(path.limit, 200);
    });

    private readonly npcMutationModel = signal({ id: 0, combatLevel: 1 });
    readonly npcMutationForm = form(this.npcMutationModel, (path) => {
        required(path.id);
        min(path.id, 0);
        required(path.combatLevel);
        min(path.combatLevel, 0);
    });
    private readonly objectMutationModel = signal({ id: 0, name: "" });
    readonly objectMutationForm = form(this.objectMutationModel, (path) => {
        required(path.id);
        min(path.id, 0);
        required(path.name);
    });
    private readonly varpMutationModel = signal({ id: 0, configId: 0, bitLength: 1, bitOffset: 0 });
    readonly varpMutationForm = form(this.varpMutationModel, (path) => {
        required(path.id);
        min(path.id, 0);
        required(path.configId);
        min(path.configId, 0);
        required(path.bitLength);
        min(path.bitLength, 0);
        required(path.bitOffset);
        min(path.bitOffset, 0);
    });
    private readonly configMutationModel = signal({ id: 0, defaultValue: 0, valueType: "i" });
    readonly configMutationForm = form(this.configMutationModel, (path) => {
        required(path.id);
        min(path.id, 0);
        required(path.defaultValue);
        required(path.valueType);
        minLength(path.valueType, 1);
        maxLength(path.valueType, 1);
    });

    async loadById(): Promise<void> {
        if (this.byIdForm().invalid()) return;
        const v = this.byIdModel();
        await this.store.loadById(v.kind, v.id);
    }

    async loadRange(): Promise<void> {
        if (this.rangeForm().invalid()) return;
        const v = this.rangeModel();
        await this.store.loadRange(v.kind, v.startId, v.endIdExclusive);
    }

    async search(): Promise<void> {
        if (this.searchForm().invalid()) return;
        const v = this.searchModel();
        await this.store.search(v.kind, v.query, v.offset, v.limit);
    }

    async setNpcCombatLevel(): Promise<void> {
        if (this.npcMutationForm().invalid()) return;
        const v = this.npcMutationModel();
        await this.store.setNpcCombatLevel(v.id, v.combatLevel);
    }

    async setObjectName(): Promise<void> {
        if (this.objectMutationForm().invalid()) return;
        const v = this.objectMutationModel();
        await this.store.setObjectName(v.id, v.name);
    }

    async setVarpBit(): Promise<void> {
        if (this.varpMutationForm().invalid()) return;
        const v = this.varpMutationModel();
        await this.store.setVarpBit(v.id, v.configId, v.bitLength, v.bitOffset);
    }

    async setConfigDefinition(): Promise<void> {
        if (this.configMutationForm().invalid()) return;
        const v = this.configMutationModel();
        await this.store.setConfigDefinition(v.id, v.defaultValue, v.valueType);
    }
}
