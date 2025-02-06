import { Component, OnInit, ChangeDetectionStrategy } from "@angular/core";

@Component({
    selector: "app-card-title",
    templateUrl: "./card-title.component.html",
    styleUrls: ["./card-title.component.scss"],
    standalone: true,
    host: {
        class: "block text-center text-bold filter drop-shadow-md",
    },
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CardTitleComponent {}
