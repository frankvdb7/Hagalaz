import { Component, inject } from "@angular/core";
import { CacheItemsStore } from "../../stores/cache-items.store";
import { DefinitionDetailComponent } from "../definition-detail.component";

@Component({
    selector: "cache-items-detail-page",
    imports: [DefinitionDetailComponent],
    providers: [CacheItemsStore],
    template: `<cache-definition-detail [store]="store" kind="items"></cache-definition-detail>`,
})
export class ItemsDetailPageComponent {
    readonly store = inject(CacheItemsStore);
}
