import {Component} from "@angular/core";
import {TypesManagerPageComponent} from "./types-manager.page";

@Component({
    selector: "cache-models-manager-page",
    imports: [TypesManagerPageComponent],
    template: `<types-manager-page [models]="true" />`,
})
export class ModelsManagerPageComponent {}
