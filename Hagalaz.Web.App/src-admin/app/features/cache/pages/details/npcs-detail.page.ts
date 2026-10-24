import { Component, inject } from "@angular/core";
import { CacheNpcsStore } from "../../stores/cache-npcs.store";
import { DefinitionDetailComponent } from "../definition-detail.component";
import { NpcEditorComponent } from "./npcs-editor.component";

@Component({
    selector: "cache-npcs-detail-page",
    imports: [DefinitionDetailComponent, NpcEditorComponent],
    providers: [CacheNpcsStore],
    template: `<cache-definition-detail [store]="store" kind="npcs" accessLabel="Supported edits" [excludedFields]='["combatLevel"]'><cache-npcs-editor /></cache-definition-detail>`,
})
export class NpcsDetailPageComponent {
    readonly store = inject(CacheNpcsStore);
}
