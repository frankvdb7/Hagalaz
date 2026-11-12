import type { WidgetComponent } from "../../services/widget-component";

export const widgetTypes: Record<number, { label: string; icon: string }> = {
    0: { label: "Container", icon: "folder_open" },
    3: { label: "Rectangle", icon: "crop_square" },
    4: { label: "Text", icon: "title" },
    5: { label: "Sprite", icon: "image" },
    6: { label: "Model", icon: "view_in_ar" },
    9: { label: "Line", icon: "show_chart" },
};

export function widgetType(type: number) {
    return widgetTypes[type] ?? { label: `Type ${type}`, icon: "widgets" };
}

function size(raw: number, mode: number, available: number) {
    if (mode === 1) return Math.max(0, available - raw);
    if (mode === 2) return Math.max(0, Math.floor((raw * available) / 16384));
    return Math.max(0, raw);
}

function position(raw: number, mode: number, space: number, extent: number) {
    const offset = mode >= 3 ? Math.floor((raw * extent) / 16384) : raw;
    if (mode === 1 || mode === 4) return Math.trunc(space / 2) + offset;
    if (mode === 2 || mode === 5) return space - offset;
    return offset;
}

interface WidgetBounds {
    x: number;
    y: number;
    width: number;
    height: number;
}

export interface WidgetLayer extends WidgetBounds {
    component: WidgetComponent;
    depth: number;
    ancestors: number[];
    visible: boolean;
}

export function layoutWidget(components: WidgetComponent[], width: number, height: number): WidgetLayer[] {
    const result: WidgetLayer[] = [];
    const visited = new Set<number>();
    const children = new Map<number, WidgetComponent[]>();
    const ids = new Set(components.map(({ id }) => id));
    for (const component of components) {
        const parentId = ids.has(component.parentId) ? component.parentId : -1;
        const siblings = children.get(parentId) ?? [];
        siblings.push(component);
        children.set(parentId, siblings);
    }

    function visit(component: WidgetComponent, bounds: WidgetBounds, visible: boolean, ancestors: number[]) {
        if (visited.has(component.id)) return;
        visited.add(component.id);
        const width = size(component.width, component.widthMode, bounds.width);
        const height = size(component.height, component.heightMode, bounds.height);
        const layer = {
            component,
            width,
            height,
            ancestors,
            depth: ancestors.length,
            visible: visible && !component.hidden,
            x: bounds.x + position(component.x, component.xMode, bounds.width - width, bounds.width),
            y: bounds.y + position(component.y, component.yMode, bounds.height - height, bounds.height),
        };
        result.push(layer);
        const childBounds = { ...layer, width: component.scrollWidth || width, height: component.scrollHeight || height };
        for (const child of children.get(component.id) ?? []) visit(child, childBounds, layer.visible, [...ancestors, component.id]);
    }

    const viewport = { x: 0, y: 0, width, height };
    for (const component of children.get(-1) ?? []) visit(component, viewport, true, []);
    for (const component of components) visit(component, viewport, true, []);
    return result;
}

export function widgetColor(color = 0xffffff) {
    return `#${(color & 0xffffff).toString(16).padStart(6, "0")}`;
}
