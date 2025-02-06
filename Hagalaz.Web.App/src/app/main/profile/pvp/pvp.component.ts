import { ChangeDetectionStrategy, Component, OnInit } from "@angular/core";
import { MatCard } from "@angular/material/card";
import { CardTitleComponent } from "@app/common/components/card-title/card-title.component";

@Component({
    selector: "app-pvp",
    templateUrl: "./pvp.component.html",
    styleUrls: ["./pvp.component.scss"],
    imports: [MatCard, CardTitleComponent],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PvpComponent implements OnInit {
    constructor() {}

    ngOnInit() {}
}
