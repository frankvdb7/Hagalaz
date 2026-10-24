import { Component, inject } from "@angular/core";
import { CacheCs2Store } from "../../stores/cache-cs2.store";
import { DefinitionDetailComponent } from "../definition-detail.component";

@Component({
    selector: "cache-cs2-detail-page",
    imports: [DefinitionDetailComponent],
    providers: [CacheCs2Store],
    template: `<cache-definition-detail [store]="store" kind="cs2"></cache-definition-detail>`,
})
export class Cs2DetailPageComponent {
    readonly store = inject(CacheCs2Store);
}
