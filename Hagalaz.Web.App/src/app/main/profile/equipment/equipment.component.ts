import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { MatCard } from "@angular/material/card";
import { CardTitleComponent } from "@app/common/components/card-title/card-title.component";
import { EquipmentStore } from "./equipment.store";

@Component({
    selector: "app-equipment",
    templateUrl: "./equipment.component.html",
    styleUrls: ["./equipment.component.scss"],
    imports: [MatCard, CardTitleComponent],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: true,
})
export class EquipmentComponent {
    equipmentStore = inject(EquipmentStore);

    constructor() {
        this.equipmentStore.loadEquipment();
    }
}
