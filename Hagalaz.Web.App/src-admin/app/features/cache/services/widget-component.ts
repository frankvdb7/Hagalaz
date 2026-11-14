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

/** Legacy (pre-"if1" revision 255) components keep an extra trailing byte or click flag in several bodies. */
const legacy = 255;
type BodyDecoder = (reader: ComponentReader, component: WidgetComponent, version: number) => void;

function decodeModelCamera(reader: ComponentReader, flags: number): WidgetComponent["modelCamera"] {
    if (!(flags & 3)) return undefined;
    const modern = (flags & 3) === 2;
    return {
        flags, x: reader.short(), y: reader.short(), z: modern ? reader.short() : 0,
        pitch: reader.unsignedShort(), yaw: reader.unsignedShort(), roll: reader.unsignedShort(),
        zoom: modern ? reader.short() : reader.unsignedShort(), baseWidth: 0, baseHeight: 0,
    };
}

const bodyDecoders: Record<number, BodyDecoder> = {
    0: (reader, component, version) => {
        component.scrollWidth = reader.unsignedShort();
        component.scrollHeight = reader.unsignedShort();
        if (version === legacy) component.blocksClicks = reader.byte() === 1;
    },
    3: (reader, component) => {
        component.color = reader.int();
        component.filled = reader.byte() === 1;
        component.transparency = reader.byte();
    },
    4: (reader, component, version) => {
        component.fontId = reader.smartId();
        if (version !== legacy && version >= 2) reader.skip(1);
        component.text = reader.string();
        component.textLineHeight = reader.byte();
        component.textAlign = reader.byte();
        component.textVerticalAlign = reader.byte();
        component.textShadow = reader.byte() === 1;
        component.color = reader.int();
        component.transparency = reader.byte();
        if (version !== legacy) reader.skip(1);
    },
    5: (reader, component, version) => {
        component.spriteId = reader.int();
        component.spriteRotation = reader.unsignedShort();
        component.spriteTiled = Boolean(reader.byte() & 1);
        component.transparency = reader.byte();
        reader.skip(5);
        component.spriteFlipY = reader.byte() === 1;
        component.spriteFlipX = reader.byte() === 1;
        reader.skip(4);
        if (version !== legacy && version >= 3) reader.skip(1);
    },
    6: (reader, component) => {
        component.modelId = reader.smartId();
        const camera = decodeModelCamera(reader, reader.byte());
        component.modelAnimationId = reader.smartId();
        const baseWidth = component.widthMode !== 0 ? reader.unsignedShort() : 0;
        const baseHeight = component.heightMode !== 0 ? reader.unsignedShort() : 0;
        if (camera) component.modelCamera = { ...camera, baseWidth, baseHeight };
    },
    9: (reader, component) => {
        component.lineWidth = reader.byte();
        component.color = reader.int();
        component.lineReversed = reader.byte() === 1;
    },
};

function decodeHeader(reader: ComponentReader, id: number, version: number): WidgetComponent {
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
    component.blocksClicks = version !== legacy && Boolean(flags & 2);
    return component;
}

/** Skips the key-binding records, then reads the click action and context-menu options. */
function decodeActions(reader: ComponentReader, component: WidgetComponent) {
    reader.skip(3);
    while (reader.byte() !== 0) reader.skip(3);
    component.action = reader.string();
    const optionCount = reader.byte() & 15;
    component.options = Array.from({ length: optionCount }, () => reader.string());
}

export function decodeWidgetComponent(id: number, dataBase64: string): WidgetComponent {
    const reader = new ComponentReader(Uint8Array.from(atob(dataBase64), (byte) => byte.charCodeAt(0)));
    const version = reader.byte();
    const component = decodeHeader(reader, id, version);
    const body = bodyDecoders[component.type];
    if (!body) return component;
    body(reader, component, version);
    decodeActions(reader, component);
    return component;
}
