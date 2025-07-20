import { ChangeDetectionStrategy, Component, OnInit } from "@angular/core";
import { MatToolbar } from "@angular/material/toolbar";

@Component({
    selector: "app-footer",
    templateUrl: "./footer.component.html",
    styleUrls: ["./footer.component.scss"],
    imports: [MatToolbar],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FooterComponent {
    get currentYear(): number {
        return new Date().getFullYear();
    }
}
