import { describe, expect, it, vi } from "vitest";
import { decodeWidgetFont, drawWidgetText, layoutWidgetText, widgetPlainText } from "./widget-font";

function base64(bytes: Uint8Array) {
    return btoa(String.fromCharCode(...bytes));
}

function fontFixture() {
    const metrics = new Uint8Array(263);
    metrics.set([4, 0, 0, 3, 1], 258);
    metrics[2 + 65] = 3;
    metrics[2 + 66] = 2;
    metrics[2 + 32] = 2;
    const header = 261;
    const sprites = new Uint8Array(header + 256 * 8 + 7);
    const view = new DataView(sprites.buffer);
    // Each glyph has a flags byte. A adds one index pixel and one explicit alpha pixel.
    sprites.set([2, 1, 128], 65);
    sprites[header + 4] = 1;
    for (const column of [0, 1, 2, 3]) view.setUint16(header + 5 + (256 * column + 65) * 2, 1);
    view.setUint16(sprites.length - 2, 256);
    return decodeWidgetFont(base64(metrics), base64(sprites));
}

describe("widget bitmap fonts", () => {
    it("decodes glyph offsets and alpha and measures cache-formatted text", () => {
        const font = fontFixture();
        expect(font.glyphs[65]).toEqual({ x: 1, y: 1, width: 1, height: 1, alpha: Uint8Array.of(128) });
        expect(layoutWidgetText(font, "A <col=abc>B<br>A", "#ffffff", 6)).toEqual([
            { glyphs: [{ code: 65, color: "#ffffff", x: 0 }], width: 3 },
            { glyphs: [{ code: 66, color: "#000abc", x: 0 }], width: 2 },
            { glyphs: [{ code: 65, color: "#ffffff", x: 0 }], width: 3 },
        ]);
    });

    it("draws the cached glyph and its shadow at the aligned coordinates", () => {
        const font = fontFixture();
        const pixels: unknown[][] = [];
        const context = {
            clearRect: vi.fn(),
            fillStyle: "",
            globalAlpha: 1,
            fillRect(x: number, y: number, width: number, height: number) {
                pixels.push([x, y, width, height, this.fillStyle, this.globalAlpha]);
            },
        };
        drawWidgetText(context as unknown as CanvasRenderingContext2D, font, "A", "#ffcc00", 9, 10, 1, 1, 0, true);
        expect(pixels).toEqual([
            [5, 4, 1, 1, "#000000", 128 / 255],
            [4, 3, 1, 1, "#ffcc00", 128 / 255],
        ]);
    });

    it("reads text without a font the way the font prints it", () => {
        expect(widgetPlainText("<col=ff0000>Hp</col> <lt>10<gt><br>Next<img=3>")).toBe("Hp <10>\nNext");
        // A lone "<" is a character, not the start of a tag.
        expect(widgetPlainText("a < b")).toBe("a < b");
    });
});
