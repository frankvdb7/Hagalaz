import { Component, computed, input } from "@angular/core";
import type { MapObjectTransform } from "./map-scene-geometry";
import type { HoveredTileReadout } from "./map-viewport.component";

/** Selected object and hovered tile readout overlaid on the map viewport. */
@Component({
    selector: "div[cacheMapSelectionStatus]",
    template: `
        @if (transform(); as selected) {
            <div role="status">
                <strong>1 object selected</strong><br />Tile {{ selected.x }}, {{ selected.y }} · Plane {{ selected.plane }}
            </div>
        }
        @if (tile(); as hovered) {
            <div class="tile-readout">
                @if (transform()) { <strong>Hovered tile</strong><br /> }
                Tile {{ hovered.x }}, {{ hovered.y }} · Plane {{ hovered.plane }}{{ collision() }}
            </div>
        }
    `,
})
export class MapSelectionStatusComponent {
    readonly transform = input.required<MapObjectTransform | null>();
    readonly tile = input.required<HoveredTileReadout | null>();
    /** Cache collision for the hovered tile, prefixed with its separator, or empty when the tile has none. */
    readonly collision = computed(() => {
        const collision = this.tile()?.collision;
        if (!collision) return "";
        const bits = collision.bits.length ? collision.bits.join(", ") : "No collision bits";
        return ` · Collision plane ${collision.plane ?? "none"} · ${bits} · Cache only`;
    });
}
