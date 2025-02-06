import { Component, ChangeDetectionStrategy, inject } from "@angular/core";
import { MatButtonModule, MatFabButton, MatIconButton } from "@angular/material/button";
import { MatIcon } from "@angular/material/icon";
import { MatToolbar } from "@angular/material/toolbar";
import { LauncherService } from "@app/launcher/launcher.service";

@Component({
    selector: "app-launcher-footer",
    templateUrl: "./footer.component.html",
    styleUrls: ["./footer.component.scss"],
    imports: [MatIcon, MatToolbar, MatIconButton, MatButtonModule, MatFabButton],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class LauncherFooterComponent {
    private launcherService = inject(LauncherService);

    launchClient() {
        this.launcherService.api.client.launch();
    }
}
