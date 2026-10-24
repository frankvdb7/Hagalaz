import { Component, inject } from "@angular/core";
import { CacheAnimationsStore } from "../../stores/cache-animations.store";
import { DefinitionDetailComponent } from "../definition-detail.component";

@Component({
    selector: "cache-animations-detail-page",
    imports: [DefinitionDetailComponent],
    providers: [CacheAnimationsStore],
    template: `<cache-definition-detail [store]="store" kind="animations"></cache-definition-detail>`,
})
export class AnimationsDetailPageComponent {
    readonly store = inject(CacheAnimationsStore);
}
