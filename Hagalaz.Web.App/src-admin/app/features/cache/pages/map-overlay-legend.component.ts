import { Component, input } from "@angular/core";

export interface MapLegendEntry {
    label: string;
    rgb: number;
    marker?: "edge";
}

@Component({
    selector: "cache-map-overlay-legend",
    template: `
        <section [attr.aria-label]="title()">
            <p>{{ title() }}</p>
            <ul>
                @for (entry of entries(); track entry.label) {
                    <li>
                        <span class="swatch" [class.edge]="entry.marker === 'edge'" aria-hidden="true">
                            <span [style.background-color]="color(entry.rgb)"></span>
                        </span>
                        {{ entry.label }}
                    </li>
                }
            </ul>
        </section>
    `,
    styles: `
        :host {
            display: block;
            max-inline-size: 100%;
            padding: 8px 12px;
            border: 1px solid var(--mat-sys-outline-variant);
            border-radius: 8px;
            background: var(--mat-sys-surface-container);
            color: var(--mat-sys-on-surface);
            font: var(--mat-sys-body-small);
        }
        p { margin: 0 0 4px; font-weight: 600; }
        ul { display: grid; gap: 2px; list-style: none; margin: 0; padding: 0; }
        li { display: flex; align-items: center; gap: 8px; }
        .swatch { display: flex; align-items: stretch; flex: 0 0 12px; block-size: 12px; }
        .swatch span { inline-size: 100%; border-radius: 2px; }
        .swatch.edge span { inline-size: 3px; }
    `,
})
export class MapOverlayLegendComponent {
    readonly title = input.required<string>();
    readonly entries = input.required<readonly MapLegendEntry[]>();
    protected color(rgb: number): string { return `#${rgb.toString(16).padStart(6, "0")}`; }
}
