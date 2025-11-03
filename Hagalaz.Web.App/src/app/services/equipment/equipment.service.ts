import { Injectable } from "@angular/core";
import { Observable, of } from "rxjs";
import { EquipmentSlot, EquipmentItem } from "./equipment.models";

@Injectable({
    providedIn: "root",
})
export class EquipmentService {
    getEquipment(): Observable<EquipmentSlot[]> {
        return of([
            { slot: "head", item: { id: 1, name: "Dragon full helm", wiki_url: "", equipment: { attack_stab: 0, attack_slash: 0, attack_crush: 0, attack_magic: -3, attack_ranged: 0, defence_stab: 30, defence_slash: 32, defence_crush: 27, defence_magic: 0, defence_ranged: 29, melee_strength: 0, ranged_strength: 0, magic_damage: 0, prayer: 0, slot: "head" } } },
            { slot: "cape", item: { id: 2, name: "Fire cape", wiki_url: "", equipment: { attack_stab: 1, attack_slash: 1, attack_crush: 1, attack_magic: 1, attack_ranged: 1, defence_stab: 11, defence_slash: 11, defence_crush: 11, defence_magic: 11, defence_ranged: 11, melee_strength: 4, ranged_strength: 0, magic_damage: 0, prayer: 2, slot: "cape" } } },
            { slot: "neck", item: { id: 3, name: "Amulet of fury", wiki_url: "", equipment: { attack_stab: 10, attack_slash: 10, attack_crush: 10, attack_magic: 10, attack_ranged: 10, defence_stab: 15, defence_slash: 15, defence_crush: 15, defence_magic: 15, defence_ranged: 15, melee_strength: 8, ranged_strength: 0, magic_damage: 0, prayer: 5, slot: "neck" } } },
            { slot: "main_hand", item: { id: 4, name: "Abyssal whip", wiki_url: "", equipment: { attack_stab: 82, attack_slash: 82, attack_crush: 0, attack_magic: 0, attack_ranged: 0, defence_stab: 0, defence_slash: 0, defence_crush: 0, defence_magic: 0, defence_ranged: 0, melee_strength: 82, ranged_strength: 0, magic_damage: 0, prayer: 0, slot: "main_hand" } } },
            { slot: "off_hand", item: { id: 5, name: "Dragon defender", wiki_url: "", equipment: { attack_stab: 25, attack_slash: 25, attack_crush: 25, attack_magic: -3, attack_ranged: -2, defence_stab: 25, defence_slash: 25, defence_crush: 25, defence_magic: -3, defence_ranged: -2, melee_strength: 6, ranged_strength: 0, magic_damage: 0, prayer: 0, slot: "off_hand" } } },
            { slot: "torso", item: { id: 6, name: "Bandos chestplate", wiki_url: "", equipment: { attack_stab: 0, attack_slash: 0, attack_crush: 0, attack_magic: -15, attack_ranged: -10, defence_stab: 82, defence_slash: 75, defence_crush: 85, defence_magic: -6, defence_ranged: 100, melee_strength: 4, ranged_strength: 0, magic_damage: 0, prayer: 1, slot: "torso" } } },
            { slot: "legs", item: { id: 7, name: "Bandos tassets", wiki_url: "", equipment: { attack_stab: 0, attack_slash: 0, attack_crush: 0, attack_magic: -21, attack_ranged: -7, defence_stab: 55, defence_slash: 48, defence_crush: 51, defence_magic: -4, defence_ranged: 64, melee_strength: 2, ranged_strength: 0, magic_damage: 0, prayer: 1, slot: "legs" } } },
            { slot: "hands", item: { id: 8, name: "Barrows gloves", wiki_url: "", equipment: { attack_stab: 12, attack_slash: 12, attack_crush: 12, attack_magic: 6, attack_ranged: 12, defence_stab: 12, defence_slash: 12, defence_crush: 12, defence_magic: 6, defence_ranged: 12, melee_strength: 12, ranged_strength: 0, magic_damage: 0, prayer: 0, slot: "hands" } } },
            { slot: "feet", item: { id: 9, name: "Dragon boots", wiki_url: "", equipment: { attack_stab: 0, attack_slash: 0, attack_crush: 0, attack_magic: -3, attack_ranged: -1, defence_stab: 12, defence_slash: 13, defence_crush: 14, defence_magic: 0, defence_ranged: 0, melee_strength: 4, ranged_strength: 0, magic_damage: 0, prayer: 0, slot: "feet" } } },
            { slot: "ring", item: { id: 10, name: "Berserker ring (i)", wiki_url: "", equipment: { attack_stab: 0, attack_slash: 0, attack_crush: 8, attack_magic: 0, attack_ranged: 0, defence_stab: 0, defence_slash: 0, defence_crush: 8, defence_magic: 0, defence_ranged: 0, melee_strength: 8, ranged_strength: 0, magic_damage: 0, prayer: 0, slot: "ring" } } },
        ]);
    }
}
