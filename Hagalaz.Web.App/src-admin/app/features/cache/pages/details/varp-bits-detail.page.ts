import { Component, inject } from "@angular/core";
import { CacheVarpBitsStore } from "../../stores/cache-varp-bits.store";
import { DefinitionDetailComponent } from "../definition-detail.component";
import { VarpBitEditorComponent } from "./varp-bits-editor.component";

@Component({
    selector: "cache-varp-bits-detail-page",
    imports: [DefinitionDetailComponent, VarpBitEditorComponent],
    providers: [CacheVarpBitsStore],
    template: `<cache-definition-detail [store]="store" kind="varp-bits" accessLabel="Supported edits" [excludedFields]='["configId", "bitLength", "bitOffset"]'><cache-varp-bits-editor /></cache-definition-detail>`,
})
export class VarpBitsDetailPageComponent {
    readonly store = inject(CacheVarpBitsStore);
}
