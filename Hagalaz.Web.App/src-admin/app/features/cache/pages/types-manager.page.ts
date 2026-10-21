import { JsonPipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, inject, signal } from "@angular/core";
import { form, FormField, max, maxLength, min, minLength, required } from "@angular/forms/signals";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDividerModule } from "@angular/material/divider";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatSelectModule } from "@angular/material/select";
import { firstValueFrom } from "rxjs";
import type { TypeKind } from "../services/cache.models";
import { CacheTypesService } from "../services/cache-types.service";

@Component({
    standalone: true,
    selector: "types-manager-page",
    imports: [
        FormField,
        JsonPipe,
        MatButtonModule,
        MatCardModule,
        MatChipsModule,
        MatDividerModule,
        MatFormFieldModule,
        MatInputModule,
        MatProgressBarModule,
        MatSelectModule,
    ],
    templateUrl: "./types-manager.page.html",
    styleUrl: "./types-manager.page.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TypesManagerPageComponent {
    private readonly typesService = inject(CacheTypesService);

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

    readonly archiveSizes = signal<unknown>(null);
    readonly byIdResult = signal<unknown>(null);
    readonly rangeResult = signal<unknown>(null);
    readonly searchResult = signal<unknown>(null);
    readonly mutationResult = signal<unknown>(null);
    readonly error = signal<string | null>(null);
    readonly loading = signal(false);

    private readonly byIdModel = signal({ kind: "items" as TypeKind, id: 0 });
    readonly byIdForm = form(this.byIdModel, (path) => {
        required(path.kind);
        min(path.id, 0);
    });
    private readonly rangeModel = signal({ kind: "items" as TypeKind, startId: 0, endIdExclusive: 10 });
    readonly rangeForm = form(this.rangeModel, (path) => {
        required(path.kind);
        min(path.startId, 0);
        min(path.endIdExclusive, 1);
    });

    private readonly searchModel = signal({ kind: "items" as "items" | "npcs" | "sprites", query: "", offset: 0, limit: 10 });
    readonly searchForm = form(this.searchModel, (path) => {
        required(path.kind);
        required(path.query);
        min(path.offset, 0);
        min(path.limit, 1);
        max(path.limit, 200);
    });

    private readonly npcMutationModel = signal({ id: 0, combatLevel: 1 });
    readonly npcMutationForm = form(this.npcMutationModel, (path) => {
        min(path.id, 0);
        min(path.combatLevel, 0);
    });
    private readonly objectMutationModel = signal({ id: 0, name: "" });
    readonly objectMutationForm = form(this.objectMutationModel, (path) => {
        min(path.id, 0);
        required(path.name);
    });
    private readonly varpMutationModel = signal({ id: 0, configId: 0, bitLength: 1, bitOffset: 0 });
    readonly varpMutationForm = form(this.varpMutationModel, (path) => {
        min(path.id, 0);
        min(path.configId, 0);
        min(path.bitLength, 0);
        min(path.bitOffset, 0);
    });
    private readonly configMutationModel = signal({ id: 0, defaultValue: 0, valueType: "i" });
    readonly configMutationForm = form(this.configMutationModel, (path) => {
        min(path.id, 0);
        required(path.valueType);
        minLength(path.valueType, 1);
        maxLength(path.valueType, 1);
    });

    async loadArchiveSizes(): Promise<void> {
        await this.run(async () => this.archiveSizes.set(await firstValueFrom(this.typesService.getArchiveSizes())));
    }
    async loadById(): Promise<void> {
        await this.run(async () => {
            const v = this.byIdModel();
            this.byIdResult.set(await firstValueFrom(this.typesService.getById(v.kind, v.id)));
        });
    }
    async loadRange(): Promise<void> {
        await this.run(async () => {
            const v = this.rangeModel();
            this.rangeResult.set(await firstValueFrom(this.typesService.getRange(v.kind, v.startId, v.endIdExclusive)));
        });
    }
    async search(): Promise<void> {
        await this.run(async () => {
            const v = this.searchModel();
            this.searchResult.set(await firstValueFrom(this.typesService.search(v.kind, v.query, v.offset, v.limit)));
        });
    }
    async setNpcCombatLevel(): Promise<void> {
        await this.run(async () => {
            const v = this.npcMutationModel();
            this.mutationResult.set(await firstValueFrom(this.typesService.updateNpcCombatLevel(v.id, v.combatLevel)));
        });
    }
    async setObjectName(): Promise<void> {
        await this.run(async () => {
            const v = this.objectMutationModel();
            this.mutationResult.set(await firstValueFrom(this.typesService.updateObjectName(v.id, v.name)));
        });
    }
    async setVarpBit(): Promise<void> {
        await this.run(async () => {
            const v = this.varpMutationModel();
            this.mutationResult.set(await firstValueFrom(this.typesService.updateVarpBit(v.id, v.configId, v.bitLength, v.bitOffset)));
        });
    }
    async setConfigDefinition(): Promise<void> {
        await this.run(async () => {
            const v = this.configMutationModel();
            this.mutationResult.set(await firstValueFrom(this.typesService.updateConfigDefinition(v.id, v.defaultValue, v.valueType)));
        });
    }

    private async run(action: () => Promise<void>): Promise<void> {
        this.error.set(null);
        this.loading.set(true);
        try {
            await action();
        } catch (error: unknown) {
            const response = error as { error?: { detail?: string; title?: string }; message?: string };
            this.error.set(response.error?.detail ?? response.error?.title ?? response.message ?? "Request failed.");
        } finally {
            this.loading.set(false);
        }
    }
}
