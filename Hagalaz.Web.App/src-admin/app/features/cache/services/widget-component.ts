export interface WidgetComponent {
    id: number;
    parentId: number;
    type: number;
    name: string;
    contentType: number;
    x: number;
    y: number;
    width: number;
    height: number;
    widthMode: number;
    heightMode: number;
    xMode: number;
    yMode: number;
    hidden: boolean;
    blocksClicks: boolean;
    scrollWidth?: number;
    scrollHeight?: number;
    scrollX?: number;
    scrollY?: number;
    spriteId?: number;
    spriteTiled?: boolean;
    spriteFlipX?: boolean;
    spriteFlipY?: boolean;
    spriteRotation?: number;
    modelId?: number;
    modelAnimationId?: number;
    modelCamera?: { flags: number; x: number; y: number; z: number; pitch: number; yaw: number; roll: number; zoom: number; baseWidth: number; baseHeight: number };
    fontId?: number;
    text?: string;
    textAlign?: number;
    textVerticalAlign?: number;
    textLineHeight?: number;
    textShadow?: boolean;
    color?: number;
    transparency?: number;
    filled?: boolean;
    lineWidth?: number;
    lineReversed?: boolean;
    action?: string;
    options?: string[];
}

const decoder = new TextDecoder("windows-1252");

class ComponentReader {
    private offset = 0;
    private readonly view: DataView;

    constructor(private readonly bytes: Uint8Array) {
        this.view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    }

    byte() {
        return this.view.getUint8(this.offset++);
    }
    signedByte() {
        return this.view.getInt8(this.offset++);
    }
    short() {
        const value = this.view.getInt16(this.offset);
        this.offset += 2;
        return value;
    }
    unsignedShort() {
        return this.short() & 0xffff;
    }
    int() {
        const value = this.view.getInt32(this.offset);
        this.offset += 4;
        return value;
    }
    skip(length: number) {
        this.offset += length;
    }

    smartId() {
        if (this.view.getInt8(this.offset) < 0) return this.int() & 0x7fffffff;
        const value = this.unsignedShort();
        return value === 32767 ? -1 : value;
    }

    string() {
        const start = this.offset;
        while (this.byte() !== 0) {
            /* Scan for the terminator. */
        }
        return decoder.decode(this.bytes.subarray(start, this.offset - 1));
    }
}

export function decodeWidgetComponent(id: number, dataBase64: string): WidgetComponent {
    const reader = new ComponentReader(Uint8Array.from(atob(dataBase64), (byte) => byte.charCodeAt(0)));
    const version = reader.byte();
    const type = reader.byte();
    const name = type & 128 ? reader.string() : "";
    const component: WidgetComponent = {
        id,
        type: type & 127,
        name,
        contentType: reader.unsignedShort(),
        x: reader.short(),
        y: reader.short(),
        width: reader.unsignedShort(),
        height: reader.unsignedShort(),
        widthMode: reader.signedByte(),
        heightMode: reader.signedByte(),
        xMode: reader.signedByte(),
        yMode: reader.signedByte(),
        parentId: reader.unsignedShort(),
        hidden: false,
        blocksClicks: false,
    };
    if (component.parentId === 65535) component.parentId = -1;
    const flags = reader.byte();
    component.hidden = Boolean(flags & 1);
    component.blocksClicks = version !== 255 && Boolean(flags & 2);

    switch (component.type) {
        case 0:
            component.scrollWidth = reader.unsignedShort();
            component.scrollHeight = reader.unsignedShort();
            if (version === 255) component.blocksClicks = reader.byte() === 1;
            break;
        case 3:
            component.color = reader.int();
            component.filled = reader.byte() === 1;
            component.transparency = reader.byte();
            break;
        case 4:
            component.fontId = reader.smartId();
            if (version !== 255 && version >= 2) reader.skip(1);
            component.text = reader.string();
            component.textLineHeight = reader.byte();
            component.textAlign = reader.byte();
            component.textVerticalAlign = reader.byte();
            component.textShadow = reader.byte() === 1;
            component.color = reader.int();
            component.transparency = reader.byte();
            if (version !== 255) reader.skip(1);
            break;
        case 5:
            component.spriteId = reader.int();
            component.spriteRotation = reader.unsignedShort();
            component.spriteTiled = Boolean(reader.byte() & 1);
            component.transparency = reader.byte();
            reader.skip(5);
            component.spriteFlipY = reader.byte() === 1;
            component.spriteFlipX = reader.byte() === 1;
            reader.skip(4);
            if (version !== 255 && version >= 3) reader.skip(1);
            break;
        case 6: {
            component.modelId = reader.smartId();
            const modelFlags = reader.byte();
            if (modelFlags & 3) {
                const modern = (modelFlags & 3) === 2;
                component.modelCamera = {
                    flags: modelFlags, x: reader.short(), y: reader.short(), z: modern ? reader.short() : 0,
                    pitch: reader.unsignedShort(), yaw: reader.unsignedShort(), roll: reader.unsignedShort(),
                    zoom: modern ? reader.short() : reader.unsignedShort(), baseWidth: 0, baseHeight: 0,
                };
            }
            component.modelAnimationId = reader.smartId();
            const baseWidth = component.widthMode !== 0 ? reader.unsignedShort() : 0;
            const baseHeight = component.heightMode !== 0 ? reader.unsignedShort() : 0;
            if (component.modelCamera) Object.assign(component.modelCamera, { baseWidth, baseHeight });
            break;
        }
        case 9:
            component.lineWidth = reader.byte();
            component.color = reader.int();
            component.lineReversed = reader.byte() === 1;
            break;
        default:
            return component;
    }
    reader.skip(3);
    while (reader.byte() !== 0) reader.skip(3);
    component.action = reader.string();
    const optionCount = reader.byte() & 15;
    component.options = Array.from({ length: optionCount }, () => reader.string());
    return component;
}
