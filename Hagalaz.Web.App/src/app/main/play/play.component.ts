import { ChangeDetectionStrategy, Component, OnInit } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { CardTitleComponent } from "@app/common/components/card-title/card-title.component";

@Component({
    selector: "app-play",
    templateUrl: "./play.component.html",
    styleUrls: ["./play.component.scss"],
    imports: [MatButtonModule, MatCardModule, CardTitleComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    host: { class: "flex flex-auto flex-col items-center justify-center" }
})
export class PlayComponent implements OnInit {
    constructor() {}

    ngOnInit() {}
}
