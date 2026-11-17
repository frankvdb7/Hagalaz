import { Component, model, viewChild } from "@angular/core";
import { type MatMenu, MatMenuModule } from "@angular/material/menu";

/** Submenu for choosing the active plane; the page's Layers menu opens it. */
@Component({
    selector: "cache-map-plane-menus",
    imports: [MatMenuModule],
    template: `
        <mat-menu #activePlaneMenu="matMenu">
            @for (plane of planes; track plane) {
                <button mat-menu-item type="button" role="menuitemradio" [attr.aria-checked]="activePlane() === plane" (click)="activePlane.set(plane)">Plane {{ plane }}</button>
            }
        </mat-menu>
    `,
})
export class MapPlaneMenusComponent {
    readonly activePlane = model.required<number>();
    readonly activePlaneMenu = viewChild.required<MatMenu>("activePlaneMenu");
    readonly planes = [0, 1, 2, 3];
}
