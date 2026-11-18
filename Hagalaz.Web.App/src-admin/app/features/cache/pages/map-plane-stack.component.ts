import { Component, computed, input, output } from "@angular/core";
import { MatTooltipModule } from "@angular/material/tooltip";
/** Stacked plane slabs over the scene. Each click moves a plane from visible to see-through to hidden and back; the slabs also mark the active plane. */
@Component({
    selector: "cache-map-plane-stack",
    imports: [MatTooltipModule],
    template: `
        <ol aria-label="Planes, top floor first">
            @for (plane of order; track plane) {
                <li [class]="'plane-' + plane" [class.is-active]="plane === activePlane()" [class.is-hidden]="!planes()[plane]" [class.is-ghosted]="ghostedPlanes().includes(plane)">
                    <button type="button" [attr.aria-label]="'Plane ' + plane + ', ' + state(plane) + (plane === activePlane() ? ', active' : '')" [matTooltip]="nextAction(plane) + ' plane ' + plane" matTooltipPosition="left" (click)="cycle.emit(plane)">
                        <span class="face" aria-hidden="true"></span><span class="number" aria-hidden="true">{{ plane }}</span>
                    </button>
                </li>
            }
        </ol>
        <span class="summary" role="status">{{ label() }}</span>
    `,
    styles: `
        :host { display: block; }
        ol { display: grid; margin: 0; padding: 6px 0 12px; list-style: none; }
        li { display: flex; block-size: 20px; }
        button { position: relative; inline-size: 56px; block-size: 20px; padding: 0; border: 0; background: none; cursor: pointer; }
        .face {
            position: absolute; inset: -7px 12px; border-radius: 6px; transform: rotateX(58deg) rotateZ(45deg);
            background: var(--slab); box-shadow: 0 2px 0 color-mix(in srgb, var(--slab) 60%, black), 0 3px 8px rgb(0 0 0 / 35%);
            outline: 2px solid transparent; outline-offset: 1px; transition: opacity 120ms, outline-color 120ms;
        }
        .number { position: absolute; inset: 0; display: grid; place-items: center; font: var(--mat-sys-label-small); color: var(--mat-sys-on-primary); opacity: 0; }
        .plane-3 { --slab: color-mix(in srgb, var(--mat-sys-primary) 35%, white); }
        .plane-2 { --slab: color-mix(in srgb, var(--mat-sys-primary) 70%, white); }
        .plane-1 { --slab: var(--mat-sys-primary); }
        .plane-0 { --slab: color-mix(in srgb, var(--mat-sys-primary) 60%, black); }
        .is-active .face { outline-color: var(--mat-sys-tertiary); }
        .is-active .number, button:hover .number, button:focus-visible .number { opacity: 1; }
        .is-ghosted .face { opacity: 0.55; background: radial-gradient(var(--slab) 45%, transparent 50%) 0 0 / 4px 4px; }
        .is-hidden .face { opacity: 0.4; box-shadow: none; }
        .is-hidden:not(.is-active) .face { outline: 1px dashed var(--slab); }
        button:focus-visible { outline: 2px solid var(--mat-sys-primary); outline-offset: 1px; border-radius: 6px; }
        .summary { position: absolute; inline-size: 1px; block-size: 1px; overflow: hidden; clip-path: inset(50%); white-space: nowrap; }
        @media (prefers-reduced-motion: reduce) { .face { transition: none; } }
    `,
})
export class MapPlaneStackComponent {
    readonly planes = input.required<readonly boolean[]>();
    readonly activePlane = input.required<number>();
    /** Visible planes drawn see-through. */
    readonly ghostedPlanes = input.required<readonly number[]>();
    /** Asks for the plane's next state: visible, see-through, hidden, visible again. */
    readonly cycle = output<number>();
    readonly order = [3, 2, 1, 0];
    /** Read out after changes, since the slabs only show the state visually. */
    readonly label = computed(() => {
        const active = this.activePlane(), ghosted = this.ghostedPlanes();
        const parts = [`Plane ${active} active${this.planes()[active] ? "" : " (hidden)"}`];
        if (ghosted.length) parts.push(`${planeList(ghosted)} see-through`);
        const hidden = this.planes().flatMap((visible, plane) => visible ? [] : [plane]);
        if (hidden.length) parts.push(`${planeList(hidden)} hidden`);
        return parts.join(". ");
    });

    state(plane: number): string {
        return !this.planes()[plane] ? "hidden" : this.ghostedPlanes().includes(plane) ? "see-through" : "visible";
    }

    nextAction(plane: number): string {
        return !this.planes()[plane] ? "Show" : this.ghostedPlanes().includes(plane) ? "Hide" : "See through";
    }
}

function planeList(planes: readonly number[]): string {
    return planes.length === 1 ? `Plane ${planes[0]}` : `Planes ${planes.slice(0, -1).join(", ")} and ${planes.at(-1)}`;
}
