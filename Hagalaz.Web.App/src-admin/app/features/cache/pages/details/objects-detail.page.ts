import { Component, inject } from "@angular/core";
import { CacheObjectsStore } from "../../stores/cache-objects.store";
import { DefinitionDetailComponent } from "../definition-detail.component";
import { ObjectEditorComponent } from "./objects-editor.component";
import { CacheModelPreviewComponent } from "../cache-model-preview.component";
import { injectCacheDefinitionId } from "../cache-definition-id";
import { FormsModule } from "@angular/forms";

@Component({
    selector: "cache-objects-detail-page",
    imports: [DefinitionDetailComponent, ObjectEditorComponent, CacheModelPreviewComponent, FormsModule],
    providers: [CacheObjectsStore],
    template: `<cache-definition-detail [store]="store" kind="objects" accessLabel="Supported edits" [excludedFields]='["name"]'><cache-objects-editor /></cache-definition-detail>@if (id() !== null) { <h2>Object model preview</h2><label>Shape <input type="number" min="0" max="22" [(ngModel)]="shape" placeholder="First defined shape" /></label><cache-model-preview [definition]="{ kind: 'objects', id: id()!, shape: shape ?? undefined }" /> }`,
})
export class ObjectsDetailPageComponent {
    readonly id = injectCacheDefinitionId();
    shape: number | null = null;
    readonly store = inject(CacheObjectsStore);
}
