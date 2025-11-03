import { Injectable } from "@angular/core";
import { Observable, of } from "rxjs";
import { CosmeticItem } from "./shop.models";

@Injectable({
    providedIn: "root",
})
export class ShopService {
    getShopItems(): Observable<CosmeticItem[]> {
        return of([
            {
                id: "santa-hat",
                name: "Santa Hat",
                description: "A festive red and white hat.",
                price: 5000,
                imageUrl: "https://oldschool.runescape.wiki/images/Santa_hat.png?127d6",
            },
            {
                id: "party-hat-blue",
                name: "Blue Party Hat",
                description: "A rare and highly sought-after blue party hat.",
                price: 100000,
                imageUrl: "https://oldschool.runescape.wiki/images/Blue_partyhat.png?127d6",
            },
            {
                id: "halloween-mask",
                name: "Halloween Mask",
                description: "A spooky mask for the Halloween season.",
                price: 7500,
                imageUrl: "https://oldschool.runescape.wiki/images/Halloween_mask.png?127d6",
            },
            {
                id: "bunny-ears",
                name: "Bunny Ears",
                description: "Cute bunny ears for a playful look.",
                price: 3000,
                imageUrl: "https://oldschool.runescape.wiki/images/Bunny_ears.png?127d6",
            },
            {
                id: "scythe",
                name: "Scythe",
                description: "A grim scythe, often associated with the Grim Reaper.",
                price: 15000,
                imageUrl: "https://oldschool.runescape.wiki/images/Scythe.png?127d6",
            },
            {
                id: "yo-yo",
                name: "Yo-yo",
                description: "A classic toy, perfect for showing off.",
                price: 2000,
                imageUrl: "https://oldschool.runescape.wiki/images/Yo-yo.png?127d6",
            },
            {
                id: "dragon-full-helm",
                name: "Dragon Full Helm",
                description: "A powerful and stylish dragon full helm.",
                price: 25000,
                imageUrl: "https://oldschool.runescape.wiki/images/Dragon_full_helm.png?127d6",
            },
            {
                id: "fire-cape",
                name: "Fire Cape",
                description: "A prestigious cape obtained from the Fight Caves.",
                price: 30000,
                imageUrl: "https://oldschool.runescape.wiki/images/Fire_cape.png?127d6",
            },
            {
                id: "gnome-scarf",
                name: "Gnome Scarf",
                description: "A fashionable scarf worn by gnomes.",
                price: 1000,
                imageUrl: "https://oldschool.runescape.wiki/images/Gnome_scarf.png?127d6",
            },
            {
                id: "gnome-goggles",
                name: "Gnome Goggles",
                description: "Stylish goggles for the adventurous gnome.",
                price: 1200,
                imageUrl: "https://oldschool.runescape.wiki/images/Gnome_goggles.png?127d6",
            },
        ]);
    }
}
