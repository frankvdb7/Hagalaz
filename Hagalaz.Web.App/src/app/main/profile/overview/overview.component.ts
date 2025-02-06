import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { MatCard } from "@angular/material/card";
import { CardTitleComponent } from "@app/common/components/card-title/card-title.component";
import { UserStore } from "@app/core/user/user.store";

@Component({
    selector: "app-overview",
    templateUrl: "./overview.component.html",
    styleUrls: ["./overview.component.scss"],
    imports: [MatCard, CardTitleComponent],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class OverviewComponent {
    store = inject(UserStore);
}
