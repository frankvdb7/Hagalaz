export interface Glyph {
    x: number;
    y: number;
    width: number;
    height: number;
    alpha: Uint8Array;
}

export interface WidgetFont {
    glyphs: Glyph[];
    advances: Uint8Array;
    ascent: number;
    top: number;
    bottom: number;
    kerning(left: number, right: number): number;
}

const bytes = (data: string) => Uint8Array.from(atob(data), (value) => value.charCodeAt(0));
const cp1252 = new TextDecoder("windows-1252");
const characterCodes = new Map(Array.from({ length: 256 }, (_, code) => [cp1252.decode(Uint8Array.of(code)), code]));

export function decodeWidgetFont(metricsData: string, spriteData: string): WidgetFont {
    const metrics = bytes(metricsData);
    if (metrics[0] !== 0 || metrics.length < 263) throw new Error("Invalid widget font metrics");
    const advances = metrics.slice(2, 258);
    let offset = 258;
    let ascent = metrics[offset++];
    let kerning: WidgetFont["kerning"] = () => 0;
    if (metrics[1] === 1) {
        const lengths = metrics.slice(258, 514);
        const offsets = metrics.slice(514, 770);
        offset = 770;
        const readProfiles = () =>
            Array.from(lengths, (length) => {
                const profile = new Int8Array(length);
                let value = 0;
                for (let index = 0; index < length; index++) profile[index] = value += metrics[offset++];
                return profile;
            });
        const leading = readProfiles();
        const trailing = readProfiles();
        if (offset + 4 > metrics.length) throw new Error("Truncated widget font metrics");
        ascent = offsets[32] + lengths[32];
        kerning = (left, right) => {
            if (left === 32 || right === 32) return 0;
            const end = Math.min(offsets[left] + lengths[left], offsets[right] + lengths[right]);
            let gap = Math.min(advances[left], advances[right]);
            for (let row = Math.max(offsets[left], offsets[right]); row < end; row++) {
                gap = Math.min(gap, trailing[left][row - offsets[left]] + leading[right][row - offsets[right]]);
            }
            return -gap;
        };
    }
    return { glyphs: decodeGlyphs(bytes(spriteData)), advances, ascent, top: metrics[offset + 2], bottom: metrics[offset + 3], kerning };
}

function decodeGlyphs(data: Uint8Array): Glyph[] {
    const view = new DataView(data.buffer);
    const count = view.getUint16(data.length - 2);
    const header = data.length - count * 8 - 7;
    if (count !== 256 || header < 0) throw new Error("Invalid widget font sprites");
    const paletteStart = header - data[header + 4] * 3;
    let offset = 0;
    return Array.from({ length: count }, (_, index) => {
        const attribute = (column: number) => view.getUint16(header + 5 + (column * count + index) * 2);
        const [x, y, width, height] = [attribute(0), attribute(1), attribute(2), attribute(3)];
        const flags = data[offset++];
        const size = width * height;
        const explicitAlpha = Boolean(flags & 2);
        const start = offset + (explicitAlpha ? size : 0);
        offset = start + size;
        if (offset > paletteStart) throw new Error("Truncated widget font glyph");
        const alpha = new Uint8Array(size);
        for (let row = 0; row < height; row++) {
            for (let col = 0; col < width; col++) {
                const value = data[start + (flags & 1 ? col * height + row : row * width + col)];
                alpha[row * width + col] = explicitAlpha ? value : value ? 255 : 0;
            }
        }
        return { x, y, width, height, alpha };
    });
}

interface TextGlyph {
    code: number;
    color: string;
}

type PlacedLine = { glyphs: (TextGlyph & { x: number })[]; width: number };
const escapes: Record<string, string> = { "<lt>": "<", "<gt>": ">" };

function positionLine(font: WidgetFont, glyphs: TextGlyph[]): PlacedLine {
    let x = 0;
    let previous: number | undefined;
    return {
        glyphs: glyphs.map((glyph) => {
            if (previous !== undefined) x += font.kerning(previous, glyph.code);
            const placed = { ...glyph, x };
            x += font.advances[glyph.code];
            previous = glyph.code;
            return placed;
        }),
        width: x,
    };
}

/** The colour a `<col=rrggbb>` or `</col>` tag switches to, or undefined for any other token. */
function colorTag(token: string, base: string): string | undefined {
    if (/^<col=[a-f\d]{1,6}>$/i.test(token)) return `#${token.slice(5, -1).padStart(6, "0")}`;
    return token === "</col>" ? base : undefined;
}

/** The character a token prints, resolving `<lt>`/`<gt>` escapes; other tags print nothing. */
function printable(token: string): string | undefined {
    if (Object.hasOwn(escapes, token)) return escapes[token];
    return !token.startsWith("<") || token.length === 1 ? token : undefined;
}

/** Wraps an overflowing line at its last space or hyphen, pushing the finished part; returns the line still being filled. */
function wrapLine(font: WidgetFont, line: TextGlyph[], width: number, lines: PlacedLine[]): TextGlyph[] {
    if (positionLine(font, line).width <= width) return line;
    let space = line.length - 1;
    while (space > 0 && line[space].code !== 32 && line[space].code !== 45) space--;
    if (space <= 0) return line;
    lines.push(positionLine(font, line.slice(0, space + (line[space].code === 45 ? 1 : 0))));
    return line.slice(space + 1);
}

export function layoutWidgetText(font: WidgetFont, text: string, color: string, width: number) {
    const lines: PlacedLine[] = [];
    let line: TextGlyph[] = [];
    let currentColor = color;
    for (const token of text.match(/<[^>]*>|[\s\S]/gu) ?? []) {
        if (token === "\n" || token === "<br>") {
            lines.push(positionLine(font, line));
            line = [];
            currentColor = color;
            continue;
        }
        const tag = colorTag(token, color);
        if (tag) {
            currentColor = tag;
            continue;
        }
        const character = printable(token);
        if (character === undefined) continue;
        line.push({ code: characterCodes.get(character) ?? 63, color: currentColor });
        line = wrapLine(font, line, width, lines);
    }
    lines.push(positionLine(font, line));
    return lines;
}

/** First baseline and line pitch for the client's vertical alignments: top, centre, bottom and justified. */
function verticalLayout(font: WidgetFont, lineCount: number, height: number, vertical: number, spacing: number) {
    const lineHeight = spacing || font.ascent;
    const extra = height - font.top - font.bottom - (lineCount - 1) * lineHeight;
    if (vertical === 1 || (vertical === 3 && lineCount === 1)) return { baseline: font.top + Math.trunc(extra / 2), lineHeight };
    if (vertical === 2) return { baseline: font.top + extra, lineHeight };
    if (vertical !== 3) return { baseline: font.top, lineHeight };
    const gap = Math.max(0, Math.trunc(extra / (lineCount + 1)));
    return { baseline: font.top + gap, lineHeight: lineHeight + gap };
}

function lineStart(horizontal: number, remaining: number) {
    if (horizontal === 1) return Math.trunc(remaining / 2);
    return horizontal === 2 ? remaining : 0;
}

interface LineDraw {
    context: CanvasRenderingContext2D;
    font: WidgetFont;
    baseline: number;
    start: number;
    /** Extra width distributed across the line's spaces when justified, otherwise zero. */
    justify: number;
    spaces: number;
    shadow: boolean;
}

function drawLine({ context, font, baseline, start, justify, spaces, shadow }: LineDraw, line: PlacedLine) {
    let passedSpaces = 0;
    for (const { code, color, x } of line.glyphs) {
        if (code === 32) {
            passedSpaces++;
            continue;
        }
        const glyph = font.glyphs[code];
        const left = start + x + glyph.x + (spaces ? Math.trunc((justify * passedSpaces) / spaces) : 0);
        const top = baseline - font.ascent + glyph.y;
        if (shadow) drawGlyph(context, glyph, left + 1, top + 1, "#000000");
        drawGlyph(context, glyph, left, top, color);
    }
}

export function drawWidgetText(
    context: CanvasRenderingContext2D,
    font: WidgetFont,
    text: string,
    color: string,
    width: number,
    height: number,
    horizontal: number,
    vertical: number,
    spacing: number,
    shadow: boolean
) {
    const lines = layoutWidgetText(font, text, color, width);
    let { baseline, lineHeight } = verticalLayout(font, lines.length, height, vertical, spacing);
    context.clearRect(0, 0, width, height);
    for (const [index, line] of lines.entries()) {
        const remaining = width - line.width;
        const justified = horizontal === 3 && index < lines.length - 1;
        const spaces = justified ? line.glyphs.filter(({ code }) => code === 32).length : 0;
        drawLine({ context, font, baseline, start: lineStart(horizontal, remaining), justify: Math.max(0, remaining), spaces, shadow }, line);
        baseline += lineHeight;
    }
    context.globalAlpha = 1;
}

function drawGlyph(context: CanvasRenderingContext2D, glyph: Glyph, x: number, y: number, color: string) {
    context.fillStyle = color;
    for (let row = 0; row < glyph.height; row++) {
        for (let column = 0; column < glyph.width; column++) {
            const opacity = glyph.alpha[row * glyph.width + column];
            if (opacity) {
                context.globalAlpha = opacity / 255;
                context.fillRect(x + column, y + row, 1, 1);
            }
        }
    }
}
