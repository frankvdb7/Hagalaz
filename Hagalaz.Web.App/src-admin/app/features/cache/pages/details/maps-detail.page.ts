import { Component, inject } from "@angular/core";
import { CacheMapsStore } from "../../stores/cache-maps.store";
import { DefinitionDetailComponent } from "../definition-detail.component";

@Component({
    selector: "cache-maps-detail-page",
    imports: [DefinitionDetailComponent],
    providers: [CacheMapsStore],
    template: `<cache-definition-detail [store]="store" kind="maps"></cache-definition-detail>`,
})
export class MapsDetailPageComponent {
    readonly store = inject(CacheMapsStore);
}
