import { Component, inject } from "@angular/core";
import { CacheGraphicsStore } from "../../stores/cache-graphics.store";
import { DefinitionDetailComponent } from "../definition-detail.component";

@Component({
    selector: "cache-graphics-detail-page",
    imports: [DefinitionDetailComponent],
    providers: [CacheGraphicsStore],
    template: `<cache-definition-detail [store]="store" kind="graphics"></cache-definition-detail>`,
})
export class GraphicsDetailPageComponent {
    readonly store = inject(CacheGraphicsStore);
}
