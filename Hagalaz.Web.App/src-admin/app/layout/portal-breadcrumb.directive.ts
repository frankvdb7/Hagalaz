import { afterNextRender, DestroyRef, Directive, inject, TemplateRef } from "@angular/core";
import { PortalBreadcrumb } from "./portal-breadcrumb";

@Directive({ selector: "ng-template[portalBreadcrumb]" })
export class PortalBreadcrumbDirective {
    constructor() {
        const slot = inject(PortalBreadcrumb, { optional: true });
        const template = inject(TemplateRef<unknown>);
        if (!slot) return;
        afterNextRender(() => slot.template.set(template));
        inject(DestroyRef).onDestroy(() => {
            if (slot.template() === template) slot.template.set(null);
        });
    }
}
