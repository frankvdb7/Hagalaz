import { Component, inject } from "@angular/core";
import { CacheQuestsStore } from "../../stores/cache-quests.store";
import { DefinitionDetailComponent } from "../definition-detail.component";

@Component({
    selector: "cache-quests-detail-page",
    imports: [DefinitionDetailComponent],
    providers: [CacheQuestsStore],
    template: `<cache-definition-detail [store]="store" kind="quests"></cache-definition-detail>`,
})
export class QuestsDetailPageComponent {
    readonly store = inject(CacheQuestsStore);
}
