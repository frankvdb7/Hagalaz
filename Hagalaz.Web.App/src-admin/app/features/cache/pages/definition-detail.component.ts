import { JsonPipe } from "@angular/common";
import { Component, computed, effect, inject, input, type Signal, untracked } from "@angular/core";
import { toSignal } from "@angular/core/rxjs-interop";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatChipsModule } from "@angular/material/chips";
import { MatExpansionModule } from "@angular/material/expansion";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { ActivatedRoute, RouterLink } from "@angular/router";
import { cacheTypeLabels, type TypeKind, type TypeMutationResultDto } from "../services/cache.models";

type DetailStore = {
    byIdResult: Signal<unknown>;
    definitionId: Signal<number | null>;
    loading: Signal<boolean>;
    error: Signal<string | null>;
    mutationResult: Signal<TypeMutationResultDto | null>;
    loadById(id: number): void;
};

@Component({
    selector: "cache-definition-detail",
    imports: [
        JsonPipe,
        RouterLink,
        MatButtonModule,
        MatCardModule,
        MatChipsModule,
        MatExpansionModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatProgressBarModule,
    ],
    templateUrl: "./definition-detail.component.html",
    styleUrl: "./definition-detail.component.scss",
})
export class DefinitionDetailComponent {
    readonly store = input.required<DetailStore>();
    readonly kind = input.required<TypeKind>();
    readonly excludedFields = input<string[]>([]);
    readonly accessLabel = input("Read only");
    private readonly route = inject(ActivatedRoute);
    private readonly params = toSignal(this.route.paramMap);
    readonly id = computed(() => {
        const raw = this.params()?.get("id") ?? this.route.snapshot.paramMap.get("id") ?? "";
        const id = Number(raw);
        return /^\d+$/.test(raw) && Number.isSafeInteger(id) ? id : null;
    });
    readonly kindLabels = cacheTypeLabels;
    readonly record = computed(() => {
        const value = this.store().byIdResult();
        return value !== null && typeof value === "object" ? (value as Record<string, unknown>) : {};
    });
    readonly selectedName = computed(() =>
        typeof this.record().name === "string" ? this.record().name : `${this.kindLabels[this.kind()]} #${this.id() ?? "Invalid ID"}`
    );
    readonly detailFields = computed(() =>
        Object.entries(this.record())
            .filter(([key, value]) => key !== "id" && !this.excludedFields().includes(key) && value !== null && typeof value !== "object")
            .map(([key, value]) => ({
                label: key.replace(/([A-Z])/g, " $1").replace(/^./, (letter) => letter.toUpperCase()),
                value: String(value),
                layoutClass: key === "name" ? "sm:col-span-2" : "",
            }))
    );
    readonly confirmation = computed(() => {
        const result = this.store().mutationResult();
        return !this.store().error() && result?.id === this.id() ? result : null;
    });
    constructor() {
        effect(() => {
            const id = this.id();
            const store = this.store();
            if (id !== null) untracked(() => store.loadById(id));
        });
    }
    reload(): void {
        const id = this.id();
        if (id !== null && !this.store().loading()) this.store().loadById(id);
    }
}
