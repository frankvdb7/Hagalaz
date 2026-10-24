import { A11yModule } from "@angular/cdk/a11y";
import { CdkOverlayOrigin, OverlayModule } from "@angular/cdk/overlay";
import { Component, computed, effect, inject, input, type Signal, signal, untracked, viewChild } from "@angular/core";
import { FormField, form } from "@angular/forms/signals";
import { MatAutocompleteModule } from "@angular/material/autocomplete";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatIconModule } from "@angular/material/icon";
import { MatInputModule } from "@angular/material/input";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { Router } from "@angular/router";
import { cacheTypeNames, type DefinitionSummaryDto, type SearchResultDto, type TypeKind } from "../services/cache.models";

export interface DefinitionPickerStore {
    byIdResult: Signal<unknown>;
    definitionId: Signal<number | null>;
    searchResult?: Signal<SearchResultDto<DefinitionSummaryDto> | null>;
    searchLoading?: Signal<boolean>;
    searchError?: Signal<string | null>;
    search?(query: string, offset: number, limit: number): void;
}

@Component({
    selector: "cache-definition-picker",
    imports: [
        A11yModule,
        OverlayModule,
        FormField,
        MatAutocompleteModule,
        MatButtonModule,
        MatCardModule,
        MatFormFieldModule,
        MatIconModule,
        MatInputModule,
        MatProgressBarModule,
    ],
    templateUrl: "./definition-picker.component.html",
    styleUrl: "./definition-picker.component.scss",
})
export class DefinitionPickerComponent {
    readonly store = input.required<DefinitionPickerStore>();
    readonly kind = input.required<TypeKind>();
    readonly id = input<number | null>(null);
    private readonly router = inject(Router);
    private readonly origin = viewChild(CdkOverlayOrigin);
    readonly opened = signal(false);
    private readonly model = signal({ query: "" });
    readonly searchForm = form(this.model);
    readonly query = computed(() => this.model().query.trim());
    readonly typeName = computed(() => cacheTypeNames[this.kind()]);
    readonly canSearch = computed(() => (this.kind() === "items" || this.kind() === "npcs") && !!this.store().search);
    readonly inputLabel = computed(() => `${this.typeName()} ${this.canSearch() ? "name or ID" : "ID"}`);
    readonly prompt = computed(() => (this.canSearch() ? "Type a name or ID" : "Enter an exact ID"));
    readonly help = computed(() => (this.canSearch() ? "Search by name, or enter an exact ID." : "Enter an exact ID to open this definition."));
    readonly caption = computed(() => {
        const record = this.store().definitionId() === this.id() ? this.store().byIdResult() : null;
        const name = record !== null && typeof record === "object" && "name" in record && typeof record.name === "string" ? record.name : null;
        return `${name || this.typeName()} #${this.id() ?? "?"}`;
    });
    readonly directId = computed(() => {
        const query = this.query();
        const id = Number(query);
        return /^\d+$/.test(query) && Number.isSafeInteger(id) ? id : null;
    });
    readonly results = computed(() => (this.store().searchResult?.()?.query === this.query() ? (this.store().searchResult?.()?.items ?? []) : []));
    readonly pending = signal(false);
    readonly loading = computed(() => this.pending() || (this.store().searchLoading?.() ?? false));
    readonly error = computed(() => (this.canSearch() && !this.pending() && this.query() && this.directId() === null ? this.store().searchError?.() : null));

    constructor() {
        effect((onCleanup) => {
            const query = this.query();
            if (!this.opened() || !this.canSearch() || !query || this.directId() !== null) {
                this.pending.set(false);
                return;
            }
            this.pending.set(true);
            const timer = setTimeout(() => {
                this.pending.set(false);
                untracked(() => this.store().search?.(query, 0, 10));
            }, 250);
            onCleanup(() => clearTimeout(timer));
        });
    }

    open(): void {
        this.model.set({ query: "" });
        this.opened.set(true);
    }

    close(): void {
        this.opened.set(false);
        this.origin()?.elementRef.nativeElement.focus();
    }

    choose(value: string | number): void {
        const id = Number(value);
        this.close();
        void this.router.navigate([`/cache/types/${this.kind()}`, id], { queryParamsHandling: "preserve" });
    }

    retry(): void {
        this.store().search?.(this.query(), 0, 10);
    }
}
