export interface EquipmentSlot {
    slot: string;
    item: EquipmentItem | null;
}

export interface EquipmentItem {
    id: number;
    name: string;
    wiki_url: string;
    equipment: {
        attack_stab: number;
        attack_slash: number;
        attack_crush: number;
        attack_magic: number;
        attack_ranged: number;
        defence_stab: number;
        defence_slash: number;
        defence_crush: number;
        defence_magic: number;
        defence_ranged: number;
        melee_strength: number;
        ranged_strength: number;
        magic_damage: number;
        prayer: number;
        slot: string;
    };
}
