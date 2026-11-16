import { Component, input, output } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import type { MapRenderRegion } from "../services/map-render.models";

@Component({
    selector: "cache-map-landscape-keys",
    imports: [MatButtonModule],
    template: `
        <details class="p-3" [hidden]="!regions().length">
            <summary>Landscape XTEA keys</summary>
            @for (region of regions(); track region.id) {
                <form class="mt-2" (submit)="$event.preventDefault(); keysEntered.emit({ region, value: keys.value }); keys.value = ''">
                    <label class="block">
                        Region {{ region.id }}
                        <input #keys class="w-full rounded border px-2 py-1" type="password" autocomplete="off" placeholder="Four signed integers" [disabled]="disabled()" />
                    </label>
                    <button mat-button type="submit" [disabled]="disabled()">Load landscape</button>
                </form>
            }
        </details>
    `,
})
export class MapLandscapeKeysComponent {
    readonly regions = input.required<readonly MapRenderRegion[]>();
    readonly disabled = input.required<boolean>();
    readonly keysEntered = output<{ region: MapRenderRegion; value: string }>();
}
