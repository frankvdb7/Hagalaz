import { Component, ChangeDetectionStrategy } from "@angular/core";

@Component({
    selector: "app-loading",
    templateUrl: "./loading.component.html",
    styleUrls: ["./loading.component.scss"],
    standalone: true,
    host: {
        class: "absolute inset-0 z-50 animate-pulse backdrop-blur-sm",
        "[@fade]": "true",
    },
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoadingComponent {}
