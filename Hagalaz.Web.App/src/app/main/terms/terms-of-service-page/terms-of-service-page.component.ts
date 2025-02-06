import { ChangeDetectionStrategy, Component } from "@angular/core";
import { CardTitleComponent } from "../../../common/components/card-title/card-title.component";
import { MatCard, MatCardContent } from "@angular/material/card";

@Component({
    selector: "app-terms-of-service-page",
    imports: [CardTitleComponent, MatCard, MatCardContent],
    templateUrl: "./terms-of-service-page.component.html",
    styleUrl: "./terms-of-service-page.component.css",
    host: { class: "flex flex-auto" },
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TermsOfServicePageComponent {}
