import { ChangeDetectionStrategy, Component } from "@angular/core";
import { MatCard, MatCardContent } from "@angular/material/card";
import { CardTitleComponent } from "../../../common/components/card-title/card-title.component";

@Component({
    selector: "app-privacy-policy-page",
    imports: [MatCardContent, MatCard, CardTitleComponent],
    templateUrl: "./privacy-policy-page.component.html",
    styleUrl: "./privacy-policy-page.component.css",
    host: { class: "flex flex-auto" },
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PrivacyPolicyPageComponent {}
