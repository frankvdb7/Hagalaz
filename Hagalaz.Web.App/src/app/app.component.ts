import { ChangeDetectionStrategy, Component } from "@angular/core";
import { RouterOutlet } from "@angular/router";

@Component({
    selector: "app-root",
    templateUrl: "./app.component.html",
    styleUrls: ["./app.component.scss"],
    host: {
        class: "flex flex-col h-full",
    },
    imports: [RouterOutlet],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class AppComponent {}
