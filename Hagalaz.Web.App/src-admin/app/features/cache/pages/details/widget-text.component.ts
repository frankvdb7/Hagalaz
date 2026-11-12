import { afterRenderEffect, Component, computed, type ElementRef, inject, input, viewChild } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { CacheWidgetFontsService } from "../../services/cache-widget-fonts.service";
import { drawWidgetText } from "../../services/widget-font";
import { widgetColor, type WidgetLayer } from "./widget-layout";

@Component({
    selector: "cache-widget-text",
    template: `
        @if (bitmap()) {
            <canvas #canvas [width]="layer().width" [height]="layer().height" aria-hidden="true"></canvas>
        } @else {
            <span [attr.data-align]="layer().component.textAlign" [attr.data-valign]="layer().component.textVerticalAlign">{{ plainText() }}</span>
        }
    `,
    styles: `
        :host, canvas { display: block; width: 100%; height: 100%; image-rendering: pixelated; }
        span { display: flex; width: 100%; height: 100%; white-space: pre-wrap; font-size: 12px; }
        span[data-align="1"] { justify-content: center; text-align: center; }
        span[data-align="2"] { justify-content: end; text-align: end; }
        span[data-valign="1"] { align-items: center; }
        span[data-valign="2"] { align-items: end; }
    `,
})
export class WidgetTextComponent {
    private readonly fonts = inject(CacheWidgetFontsService);
    readonly layer = input.required<WidgetLayer>();
    private readonly canvas = viewChild<ElementRef<HTMLCanvasElement>>("canvas");
    private readonly font = rxResource({
        params: () => {
            const { fontId, text } = this.layer().component;
            return text && fontId !== undefined && fontId >= 0 ? fontId : undefined;
        },
        stream: ({ params }) => this.fonts.get(params),
    });
    readonly bitmap = computed(() => {
        const { width, height } = this.layer();
        return width > 0 && height > 0 && width * height <= 4_194_304 && this.font.hasValue() ? this.font.value() : null;
    });
    readonly plainText = computed(() => (this.layer().component.text ?? "").replace(/<br>/gi, "\n").replace(/<[^>]+>/g, ""));

    constructor() {
        afterRenderEffect(() => {
            const font = this.bitmap();
            const context = this.canvas()?.nativeElement.getContext("2d");
            if (!font || !context) return;
            const { component, width, height } = this.layer();
            drawWidgetText(
                context,
                font,
                component.text ?? "",
                widgetColor(component.color),
                width,
                height,
                component.textAlign ?? 0,
                component.textVerticalAlign ?? 0,
                component.textLineHeight ?? 0,
                component.textShadow ?? false
            );
        });
    }
}
