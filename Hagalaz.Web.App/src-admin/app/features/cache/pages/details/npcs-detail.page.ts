import { Component, inject } from "@angular/core";
import { CacheNpcsStore } from "../../stores/cache-npcs.store";
import { DefinitionDetailComponent } from "../definition-detail.component";
import { NpcEditorComponent } from "./npcs-editor.component";
import { CacheModelPreviewComponent } from "../cache-model-preview.component";
import { injectCacheDefinitionId } from "../cache-definition-id";

@Component({
    selector: "cache-npcs-detail-page",
    imports: [DefinitionDetailComponent, NpcEditorComponent, CacheModelPreviewComponent],
    providers: [CacheNpcsStore],
    template: `<cache-definition-detail [store]="store" kind="npcs" accessLabel="Supported edits" [excludedFields]='["combatLevel"]'><cache-npcs-editor /></cache-definition-detail>@if (id() !== null) { <h2>NPC model preview</h2><cache-model-preview [definition]="{ kind: 'npcs', id: id()! }" /> }`,
})
export class NpcsDetailPageComponent {
    readonly id = injectCacheDefinitionId();
    readonly store = inject(CacheNpcsStore);
}
