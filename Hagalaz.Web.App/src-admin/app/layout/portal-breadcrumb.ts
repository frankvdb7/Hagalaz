import { Service, signal, type TemplateRef } from "@angular/core";

/** The routed page owns the template and its data; the toolbar only renders it. */
@Service()
export class PortalBreadcrumb {
    readonly template = signal<TemplateRef<unknown> | null>(null);
}
