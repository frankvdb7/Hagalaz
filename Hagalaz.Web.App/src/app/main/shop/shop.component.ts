import { ChangeDetectionStrategy, Component, inject, OnInit } from "@angular/core";
import { MatCardModule } from "@angular/material/card";
import { CardTitleComponent } from "@app/common/components/card-title/card-title.component";
import { ShopStore } from "./shop.store";
import { MatButtonModule } from "@angular/material/button";
import { UserStore } from "@app/core/user/user.store";
import { CosmeticItem } from "@app/services/shop/shop.models";

@Component({
    selector: "app-shop",
    templateUrl: "./shop.component.html",
    styleUrls: ["./shop.component.scss"],
    imports: [MatCardModule, CardTitleComponent, MatButtonModule],
    changeDetection: ChangeDetectionStrategy.OnPush,
    standalone: true,
})
export class ShopComponent implements OnInit {
    shopStore = inject(ShopStore);
    userStore = inject(UserStore);

    ngOnInit(): void {
        this.shopStore.loadShopItems();
    }

    buyItem(item: CosmeticItem): void {
        this.shopStore.purchaseItem(item);
    }
}
