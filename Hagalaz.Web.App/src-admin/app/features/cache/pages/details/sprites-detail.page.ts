import { Component, inject } from "@angular/core";
import { CacheSpriteDefinitionsStore } from "../../stores/cache-sprite-definitions.store";
import { DefinitionDetailComponent } from "../definition-detail.component";
import { SpritesManagerPageComponent } from "../sprites-manager.page";

@Component({
    selector: "cache-sprites-detail-page",
    imports: [DefinitionDetailComponent, SpritesManagerPageComponent],
    providers: [CacheSpriteDefinitionsStore],
    template: `<cache-definition-detail #detail [store]="store" kind="sprites" accessLabel="Image replacement supported">
        @if (store.definitionId() === detail.id() && detail.id() !== null) {
            <sprites-manager-page [spriteId]="detail.id()" (imageReplaced)="store.loadById($event.id)" />
        }
    </cache-definition-detail>`,
})
export class SpriteDefinitionsDetailPageComponent {
    readonly store = inject(CacheSpriteDefinitionsStore);
}
