import { Component, input } from "@angular/core";
import { RouterLink } from "@angular/router";

export interface ItemField {
    label: string;
    value: string;
    itemId?: number;
}

@Component({
    selector: "cache-item-fields",
    imports: [RouterLink],
    template: `<dl>@for (field of fields(); track field.label) {
        <div><dt>{{ field.label }}</dt><dd>
            @if (field.itemId !== undefined) { <a href="/cache/types/items/{{ field.itemId }}" [routerLink]="['/cache/types/items', field.itemId]" queryParamsHandling="preserve" aria-label="{{ field.label }} #{{ field.itemId }}">{{ field.value }}</a> }
            @else { {{ field.value }} }
        </dd></div>
    }</dl>`,
    styles: `
        :host { display: block; min-width: 0; }
        dl > div { display: flex; justify-content: space-between; gap: 16px; padding: 14px 0; border-bottom: 1px solid var(--mat-sys-outline-variant); }
        dt { color: var(--mat-sys-on-surface-variant); font: var(--mat-sys-body-small); }
        dd { margin: 0; text-align: right; overflow-wrap: anywhere; font: var(--mat-sys-label-large); }
        a { color: var(--mat-sys-primary); text-decoration: underline; text-underline-offset: 4px; }
    `,
})
export class ItemFieldsComponent {
    readonly fields = input.required<ItemField[]>();
}
