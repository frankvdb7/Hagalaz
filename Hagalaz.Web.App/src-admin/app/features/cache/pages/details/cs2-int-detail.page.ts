import { Component, inject } from "@angular/core";
import { CacheCs2IntStore } from "../../stores/cache-cs2-int.store";
import { DefinitionDetailComponent } from "../definition-detail.component";

@Component({
    selector: "cache-cs2-int-detail-page",
    imports: [DefinitionDetailComponent],
    providers: [CacheCs2IntStore],
    template: `<cache-definition-detail [store]="store" kind="cs2-int"></cache-definition-detail>`,
})
export class Cs2IntDetailPageComponent {
    readonly store = inject(CacheCs2IntStore);
}
