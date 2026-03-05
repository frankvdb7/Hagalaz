import { JsonPipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, inject, signal } from "@angular/core";
import { FormBuilder, ReactiveFormsModule, Validators } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatDividerModule } from "@angular/material/divider";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { MatSelectModule } from "@angular/material/select";
import { firstValueFrom } from "rxjs";
import { TypeKind } from "../services/cache.models";
import { CacheTypesService } from "../services/cache-types.service";

@Component({
    standalone: true,
    selector: "types-manager-page",
    imports: [
        ReactiveFormsModule,
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
    private readonly fb = inject(FormBuilder);
    private readonly typesService = inject(CacheTypesService);

    readonly kinds: TypeKind[] = ["items", "npcs", "objects", "sprites", "quests", "animations", "graphics", "maps", "varp-bits", "client-map-definitions", "config-definitions", "cs2", "cs2-int"];
    readonly searchableKinds: Array<"items" | "npcs" | "sprites"> = ["items", "npcs", "sprites"];

    readonly archiveSizes = signal<unknown>(null);
    readonly byIdResult = signal<unknown>(null);
    readonly rangeResult = signal<unknown>(null);
    readonly searchResult = signal<unknown>(null);
    readonly mutationResult = signal<unknown>(null);
    readonly error = signal<string | null>(null);
    readonly loading = signal(false);

    readonly byIdForm = this.fb.nonNullable.group({ kind: ["items" as TypeKind, Validators.required], id: [0, Validators.min(0)] });
    readonly rangeForm = this.fb.nonNullable.group({ kind: ["items" as TypeKind, Validators.required], startId: [0, Validators.min(0)], endIdExclusive: [10, Validators.min(1)] });
    readonly searchForm = this.fb.nonNullable.group({ kind: ["items" as "items" | "npcs" | "sprites", Validators.required], query: ["", Validators.required], offset: [0, Validators.min(0)], limit: [10, [Validators.min(1), Validators.max(200)]] });

    readonly npcMutationForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)], combatLevel: [1, Validators.min(0)] });
    readonly objectMutationForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)], name: ["", Validators.required] });
    readonly varpMutationForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)], configId: [0, Validators.min(0)], bitLength: [1, Validators.min(0)], bitOffset: [0, Validators.min(0)] });
    readonly configMutationForm = this.fb.nonNullable.group({ id: [0, Validators.min(0)], defaultValue: [0], valueType: ["i", [Validators.required, Validators.minLength(1), Validators.maxLength(1)]] });

    async loadArchiveSizes(): Promise<void> { await this.run(async () => this.archiveSizes.set(await firstValueFrom(this.typesService.getArchiveSizes()))); }
    async loadById(): Promise<void> { await this.run(async () => { const v = this.byIdForm.getRawValue(); this.byIdResult.set(await firstValueFrom(this.typesService.getById(v.kind, v.id))); }); }
    async loadRange(): Promise<void> { await this.run(async () => { const v = this.rangeForm.getRawValue(); this.rangeResult.set(await firstValueFrom(this.typesService.getRange(v.kind, v.startId, v.endIdExclusive))); }); }
    async search(): Promise<void> { await this.run(async () => { const v = this.searchForm.getRawValue(); this.searchResult.set(await firstValueFrom(this.typesService.search(v.kind, v.query, v.offset, v.limit))); }); }
    async setNpcCombatLevel(): Promise<void> { await this.run(async () => { const v = this.npcMutationForm.getRawValue(); this.mutationResult.set(await firstValueFrom(this.typesService.updateNpcCombatLevel(v.id, v.combatLevel))); }); }
    async setObjectName(): Promise<void> { await this.run(async () => { const v = this.objectMutationForm.getRawValue(); this.mutationResult.set(await firstValueFrom(this.typesService.updateObjectName(v.id, v.name))); }); }
    async setVarpBit(): Promise<void> { await this.run(async () => { const v = this.varpMutationForm.getRawValue(); this.mutationResult.set(await firstValueFrom(this.typesService.updateVarpBit(v.id, v.configId, v.bitLength, v.bitOffset))); }); }
    async setConfigDefinition(): Promise<void> { await this.run(async () => { const v = this.configMutationForm.getRawValue(); this.mutationResult.set(await firstValueFrom(this.typesService.updateConfigDefinition(v.id, v.defaultValue, v.valueType))); }); }

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
