import { ChangeDetectionStrategy, Component } from "@angular/core";
import { RouterOutlet } from "@angular/router";

@Component({
    selector: "admin-root",
    standalone: true,
    imports: [RouterOutlet],
    templateUrl: "./app.component.html",
    styleUrl: "./app.component.scss",
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AdminAppComponent {}
