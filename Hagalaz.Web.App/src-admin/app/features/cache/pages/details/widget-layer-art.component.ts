import { Component, computed, input } from "@angular/core";
import { widgetColor, type WidgetLayer } from "./widget-layout";
import { WidgetModelComponent } from "./widget-model.component";
import { WidgetSpriteComponent } from "./widget-sprite.component";
import { WidgetTextComponent } from "./widget-text.component";

/** Draws one visible widget layer by component type, tinted and faded by the component's colour and transparency. */
@Component({
    selector: "span[cacheWidgetLayerArt]",
    imports: [WidgetModelComponent, WidgetSpriteComponent, WidgetTextComponent],
    template: `
        @switch (layer().component.type) {
            @case (3) { <span class="widget-rectangle" [class.filled]="layer().component.filled"></span> }
            @case (4) { <cache-widget-text [layer]="layer()" /> }
            @case (5) {
                @if (sprite(); as value) { <cache-widget-sprite [id]="value.id" [tiled]="value.tiled" [style.transform]="value.transform" /> }
            }
            @case (6) { <cache-widget-model [layer]="layer()" /> }
            @case (9) {
                <span class="widget-line" [style.width.px]="line().length" [style.height.px]="line().width" [style.top.px]="line().top"
                    [style.transform]="line().transform"></span>
            }
        }
    `,
    styles: `
        :host { position: absolute; inset: 0; display: block; overflow: hidden; pointer-events: none; }
        .widget-rectangle { display: block; width: 100%; height: 100%; border: 1px solid currentColor; }
        .widget-rectangle.filled, .widget-line { background: currentColor; }
        .widget-line { position: absolute; left: 0; transform-origin: left center; }
    `,
    host: { class: "layer-appearance", "[style.color]": "color()", "[style.opacity]": "opacity()" },
})
export class WidgetLayerArtComponent {
    readonly layer = input.required<WidgetLayer>({ alias: "cacheWidgetLayerArt" });
    readonly color = computed(() => widgetColor(this.layer().component.color));
    readonly opacity = computed(() => 1 - (this.layer().component.transparency ?? 0) / 256);

    readonly sprite = computed(() => {
        const { spriteId, spriteTiled, spriteRotation, spriteFlipX, spriteFlipY } = this.layer().component;
        if (spriteId === undefined || spriteId < 0) return null;
        const rotation = ((spriteRotation ?? 0) * 360) / 65536;
        return {
            id: spriteId,
            tiled: spriteTiled ?? false,
            transform: `rotate(${rotation}deg) scale(${spriteFlipX ? -1 : 1}, ${spriteFlipY ? -1 : 1})`,
        };
    });

    readonly line = computed(() => {
        const { width, height, component } = this.layer();
        const rise = component.lineReversed ? -height : height;
        return {
            length: Math.hypot(width, height),
            width: component.lineWidth,
            top: component.lineReversed ? height : 0,
            transform: `rotate(${Math.atan2(rise, width)}rad)`,
        };
    });
}
