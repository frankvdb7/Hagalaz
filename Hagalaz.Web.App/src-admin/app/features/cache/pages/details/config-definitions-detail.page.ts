import { Component, inject } from "@angular/core";
import { CacheConfigDefinitionsStore } from "../../stores/cache-config-definitions.store";
import { DefinitionDetailComponent } from "../definition-detail.component";
import { ConfigDefinitionEditorComponent } from "./config-definitions-editor.component";

@Component({
    selector: "cache-config-definitions-detail-page",
    imports: [DefinitionDetailComponent, ConfigDefinitionEditorComponent],
    providers: [CacheConfigDefinitionsStore],
    template: `<cache-definition-detail [store]="store" kind="config-definitions" accessLabel="Supported edits" [excludedFields]='["defaultValue", "valueType"]'><cache-config-definitions-editor /></cache-definition-detail>`,
})
export class ConfigDefinitionsDetailPageComponent {
    readonly store = inject(CacheConfigDefinitionsStore);
}
