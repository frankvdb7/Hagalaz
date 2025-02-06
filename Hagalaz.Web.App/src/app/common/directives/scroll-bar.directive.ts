import { Directive, ElementRef, inject, input, Renderer2 } from "@angular/core";
import { OverlayScrollbars, PartialOptions } from "overlayscrollbars";

@Directive({
    selector: "[appScrollBar]",
    standalone: true,
})
export class ScrollBarDirective {
    private elementRef = inject(ElementRef<HTMLElement>);
    private renderer = inject(Renderer2);
    private instance: OverlayScrollbars | undefined;

    options = input<PartialOptions>({}, { alias: "appScrollBarOptions" });

    ngAfterViewInit(): void {
        this.renderer.setAttribute(this.elementRef.nativeElement, "data-overlayscrollbars-initialize", "");
        this.instance = OverlayScrollbars(this.elementRef.nativeElement, this.options());
    }

    ngOnDestroy(): void {
        this.instance?.destroy();
    }
}
