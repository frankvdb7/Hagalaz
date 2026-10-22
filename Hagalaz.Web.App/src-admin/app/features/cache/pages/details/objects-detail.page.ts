import { Component, inject } from "@angular/core";
import { CacheObjectsStore } from "../../stores/cache-objects.store";
import { DefinitionDetailComponent } from "../definition-detail.component";
import { ObjectEditorComponent } from "./objects-editor.component";

@Component({
    selector: "cache-objects-detail-page",
    imports: [DefinitionDetailComponent, ObjectEditorComponent],
    providers: [CacheObjectsStore],
    template: `<cache-definition-detail [store]="store" kind="objects" accessLabel="Supported edits" [excludedFields]='["name"]'><cache-objects-editor /></cache-definition-detail>`,
})
export class ObjectsDetailPageComponent {
    readonly store = inject(CacheObjectsStore);
}
