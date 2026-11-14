import { Component, computed, input, output, signal } from "@angular/core";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { MatListModule } from "@angular/material/list";
import type { MapFloor } from "../services/map-render.models";
import { MapFloorSwatchComponent } from "./map-floor-swatch.component";

/** Chooses an underlay or overlay from the cache's floor catalog. */
@Component({
    selector: "cache-map-floor-picker",
    imports: [MapFloorSwatchComponent, MatFormFieldModule, MatInputModule, MatListModule],
    template: `
        <mat-form-field appearance="outline" subscriptSizing="dynamic" class="studio-compact-inspector">
            <mat-label>Find a floor by ID</mat-label>
            <input matInput inputmode="numeric" [value]="query()" (input)="query.set($any($event.target).value)" />
        </mat-form-field>
        <mat-action-list class="floor-list map-asset-rows map-scroll-area">
            @for (floor of visibleFloors(); track floor.id) {
                <button mat-list-item type="button" [activated]="floor.id === chosen()" [attr.aria-pressed]="floor.id === chosen()" (click)="chosenChange.emit(floor.id)">
                    <cache-map-floor-swatch matListItemAvatar aria-hidden="true" [floor]="floor" />
                    <span matListItemTitle class="floor-label">
                        <span>{{ floor.overlay ? 'Overlay' : 'Underlay' }} {{ floor.id }}</span>
                        @if (floor.textureId >= 0) { <span class="floor-texture">Texture {{ floor.textureId }}</span> }
                    </span>
                </button>
            }
        </mat-action-list>
        @if (message(); as text) { <p class="supporting-text" [attr.role]="error() ? 'alert' : 'status'">{{ text }}</p> }
    `,
    styles: `
        :host { display: grid; gap: 8px; }
        .floor-list { max-height: 320px; padding: 0; overflow: hidden auto; }
        .floor-list cache-map-floor-swatch { margin: 0 12px 0 4px; }
        .floor-label { display: grid; min-width: 0; }
        .floor-texture { color: var(--mat-sys-on-surface-variant); font: var(--mat-sys-body-small); }
    `,
})
export class MapFloorPickerComponent {
    readonly floors = input<readonly MapFloor[]>([]);
    readonly overlay = input(false);
    readonly chosen = input<number | null>(null);
    readonly loading = input(false);
    readonly error = input<string | null>(null);
    readonly chosenChange = output<number>();
    readonly query = signal("");
    readonly visibleFloors = computed(() => {
        const query = this.query().trim();
        return this.floors().filter(floor => floor.overlay === this.overlay() && (!query || String(floor.id).includes(query)));
    });
    readonly message = computed(() => {
        if (this.error()) return this.error();
        if (this.loading()) return "Loading floors…";
        return this.visibleFloors().length ? null : "No matching floors.";
    });
}
