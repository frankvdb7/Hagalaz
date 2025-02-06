import { ChangeDetectionStrategy, Component, OnInit } from "@angular/core";

@Component({
    selector: "app-play",
    templateUrl: "./play.component.html",
    styleUrls: ["./play.component.scss"],
    imports: [],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PlayComponent implements OnInit {
    constructor() {}

    ngOnInit() {}
}
