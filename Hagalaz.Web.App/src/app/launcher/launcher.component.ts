import { Component, ChangeDetectionStrategy } from "@angular/core";
import { RouterOutlet } from "@angular/router";
import { BgImgComponent } from "../common/components/bg-img/bg-img.component";
import { ScrollBarDirective } from "@app/common/directives/scroll-bar.directive";

@Component({
    selector: "app-launcher",
    templateUrl: "./launcher.component.html",
    styleUrls: ["./launcher.component.scss"],
    imports: [RouterOutlet, BgImgComponent, ScrollBarDirective],
    host: {
        class: "flex flex-col flex-auto min-h-0",
    },
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LauncherComponent {}
