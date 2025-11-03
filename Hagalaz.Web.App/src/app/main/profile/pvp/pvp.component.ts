import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { MatCard } from "@angular/material/card";
import { CardTitleComponent } from "@app/common/components/card-title/card-title.component";
import { PvpStore } from "./pvp.store";
import { DateAgoPipe } from "@app/common/pipes/date-ago/date-ago.pipe";

@Component({
    selector: "app-pvp",
    templateUrl: "./pvp.component.html",
    styleUrls: ["./pvp.component.scss"],
    imports: [MatCard, CardTitleComponent, DateAgoPipe],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class PvpComponent {
    pvpStore = inject(PvpStore);

    constructor() {
        this.pvpStore.loadPvpStats();
        this.pvpStore.loadMatchHistory();
    }

    toDate(dateString: string): Date {
        return new Date(dateString);
    }
}
