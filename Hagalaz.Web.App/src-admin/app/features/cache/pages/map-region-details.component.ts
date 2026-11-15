import { Component, input } from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import type { SceneDetails } from "./map-scene-tree";

/** The stored details of a selected scene row, for the map editor's Inspector. */
@Component({
    selector: "cache-map-region-details",
    imports: [MatIconModule],
    template: `
        @for (item of details(); track item.id) {
            <section [attr.aria-label]="item.title">
                <h3><mat-icon class="studio-icon-small" aria-hidden="true">{{ item.icon }}</mat-icon>{{ item.title }}</h3>
                <dl>
                    @for (row of item.rows; track row.label) { <div><dt>{{ row.label }}</dt><dd>{{ row.value }}</dd></div> }
                </dl>
                @for (warning of item.warnings; track warning) { <p class="supporting-text">{{ warning }}</p> }
            </section>
        }
    `,
    styles: `
        section { padding: 16px; }
        section + section { border-top: 1px solid var(--mat-sys-outline-variant); }
        h3 { display: flex; align-items: center; gap: 10px; margin: 0 0 12px; font: var(--mat-sys-label-large); letter-spacing: .07em; text-transform: uppercase; }
        h3 mat-icon { color: var(--mat-sys-primary); }
        dl { display: grid; gap: 6px; margin: 0; }
        dl div { display: flex; justify-content: space-between; gap: 8px; }
        dt { color: var(--mat-sys-on-surface-variant); }
        dd { min-width: 0; margin: 0; overflow-wrap: anywhere; text-align: end; font-variant-numeric: tabular-nums; }
        p { margin: 12px 0 0; }
    `,
})
export class MapRegionDetailsComponent {
    readonly details = input.required<readonly SceneDetails[]>();
}
