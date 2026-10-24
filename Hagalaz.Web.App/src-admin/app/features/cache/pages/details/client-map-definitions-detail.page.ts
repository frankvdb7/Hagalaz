import { Component, inject } from "@angular/core";
import { CacheClientMapDefinitionsStore } from "../../stores/cache-client-map-definitions.store";
import { DefinitionDetailComponent } from "../definition-detail.component";

@Component({
    selector: "cache-client-map-definitions-detail-page",
    imports: [DefinitionDetailComponent],
    providers: [CacheClientMapDefinitionsStore],
    template: `<cache-definition-detail [store]="store" kind="client-map-definitions"></cache-definition-detail>`,
})
export class ClientMapDefinitionsDetailPageComponent {
    readonly store = inject(CacheClientMapDefinitionsStore);
}
