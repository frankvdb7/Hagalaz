import { describe, expect, it } from "vitest";
import { pointerEvents, WidgetRuntime, type WidgetProgramBundle } from "./widget-runtime";

function program(id: number, code: [string, string][], args: [number, number, number] = [0, 0, 0]): WidgetProgramBundle["programs"][number] {
    return { id, arguments: args, locals: args, switches: [], instructions: code.map(([operation, operand]) => ({ operation, operand })) };
}

describe("Widget RuneScript runtime", () => {
    it("preserves menu slots and consumes out-of-range assignments", () => {
        const packed = "64028703";
        const baseline = ["", "Inspect", "Trade"];
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [
                { component: 30, callbacks: [{ event: "Open", scriptId: 1, args: [], triggers: [] }] },
                { component: 31, callbacks: [] },
            ],
            programs: [program(1, [
                ["pushString", "Talk"], ["pushInt", "1"], ["pushInt", packed], ["setOption", "0"],
                ["pushString", "Follow"], ["pushInt", "10"], ["pushInt", packed], ["setOption", "0"],
                ["pushString", "Target"], ["pushString", "Ignored"], ["pushInt", "11"], ["pushInt", packed], ["setOption", "0"],
                ["pushInt", packed], ["setAction", "0"], ["return", "0"],
            ])],
        }, () => ({ width: 0, height: 0, localX: 0, localY: 0, component: { options: baseline } }));
        expect(runtime.run({ event: "open" }).state?.components).toEqual([
            { id: 31, options: ["Talk", "Inspect", "Trade", "", "", "", "", "", "", "Follow"], action: "Target" },
        ]);
        expect(baseline).toEqual(["", "Inspect", "Trade"]);
    });

    it("reads and writes varbits within preview-local client configs", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1, varbits: { 7: { configId: 2, start: 4, end: 6 } },
            bindings: [{ component: 30, callbacks: [{ event: "Open", scriptId: 1, args: [], triggers: [] }] }],
            programs: [program(1, [
                ["pushInt", "159"], ["setVarp", "2"], ["getVarbit", "7"], ["setVar", "1"],
                ["pushInt", "5"], ["setVarbit", "7"], ["getVarbit", "7"], ["setVar", "2"],
                ["getVarp", "2"], ["setVar", "3"], ["return", "0"],
            ])],
        });
        expect(runtime.run({ event: "open" }).state?.integers).toEqual({ 1: 1, 2: 5, 3: 223 });
    });

    it("runs the bound integer callback only for matching writes and clamps its scroll offsets", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [{ component: 30, callbacks: [
                { event: "Open", scriptId: 1, args: [], triggers: [] },
                { event: "Mouse pressed", scriptId: 2, args: [], triggers: [] },
            ] }],
            programs: [
                program(1, [
                    ["pushInt", "3"], ["pushInt", "7"], ["pushInt", "1"], ["pushString", "Y"],
                    ["pushInt", "64028702"], ["bindIntChanged", "0"], ["pushInt", "1"], ["setVar", "8"], ["return", "0"],
                ]),
                program(2, [["pushInt", "1"], ["setVar", "7"], ["return", "0"]]),
                program(3, [
                    ["pushInt", "-10"], ["pushInt", "500"], ["pushInt", "64028702"], ["setScroll", "0"],
                    ["pushInt", "64028702"], ["getScrollY", "0"], ["setVar", "9"], ["return", "0"],
                ]),
            ],
        }, () => ({ width: 100, height: 100, localX: 0, localY: 0, component: { scrollWidth: 150, scrollHeight: 300 } }));
        expect(runtime.run({ event: "open" }).state?.components).toEqual([]);
        expect(runtime.run({ event: "click", component: 30 })).toEqual({
            state: { components: [{ id: 30, scrollX: 0, scrollY: 200 }], integers: { 7: 1, 8: 1, 9: 200 } },
            message: null,
        });
    });

    it.each([[1, true], [0, false], [2, false]])("sets sprite tiling for flag %i", (flag, tiled) => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [{ component: 30, callbacks: [{ event: "Open", scriptId: 1, args: [], triggers: [] }] }],
            programs: [program(1, [
                ["pushInt", String(flag)], ["setContextSpriteTiled", "0"], ["return", "0"],
            ])],
        });
        expect(runtime.run({ event: "open" }).state?.components).toEqual([{ id: 30, spriteTiled: tiled }]);
    });
    it("shares integer arrays with callees and honors default values and write order", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [{ component: 30, callbacks: [{ event: "Open", scriptId: 1, args: [], triggers: [] }] }],
            programs: [
                program(1, [
                    ["pushInt", "2"], ["newIntArray", "105"], ["pushInt", "1"], ["newIntArray", "65536"],
                    ["call", "2"], ["pushInt", "1"], ["loadArrayInt", "0"], ["setVar", "1"],
                    ["pushInt", "0"], ["loadArrayInt", "0"], ["setVar", "2"],
                    ["pushInt", "0"], ["loadArrayInt", "1"], ["setVar", "3"], ["return", "0"],
                ]),
                program(2, [["pushInt", "1"], ["pushInt", "42"], ["storeArrayInt", "0"], ["return", "0"]]),
            ],
        });
        expect(runtime.run({ event: "open" }).state?.integers).toEqual({ 1: 42, 2: 0, 3: -1 });
    });

    it("reads unset string variables as empty and rolls back a write after an invalid array read", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [{ component: 30, callbacks: [
                { event: "Open", scriptId: 1, args: [], triggers: [] },
                { event: "Mouse pressed", scriptId: 2, args: [], triggers: [] },
                { event: "Mouse released", scriptId: 3, args: [], triggers: [] },
            ] }],
            programs: [
                program(1, [["getStringVar", "7"], ["pushInt", "64028702"], ["setText", "0"],
                    ["pushString", "Saved"], ["setStringVar", "7"], ["return", "0"]]),
                program(2, [["pushString", "Discarded"], ["setStringVar", "7"], ["pushInt", "0"], ["loadArrayInt", "0"]]),
                program(3, [["getStringVar", "7"], ["pushInt", "64028702"], ["setText", "0"], ["return", "0"]]),
            ],
        });
        expect(runtime.run({ event: "open" }).state?.components).toEqual([{ id: 30, text: "" }]);
        expect(runtime.run({ event: "click", component: 30 }).message).toContain("Invalid array index");
        expect(runtime.run({ event: "release", component: 30 }).state?.components).toEqual([{ id: 30, text: "Saved" }]);
    });
    it("creates and sizes a child in the selected context, then clears it without changing its cached parent", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [{ component: 30, callbacks: [
                { event: "Open", scriptId: 1, args: [], triggers: [] },
                { event: "Mouse pressed", scriptId: 2, args: [], triggers: [] },
            ] }],
            programs: [
                program(1, [
                    ["pushInt", "64028702"], ["pushInt", "4"], ["pushInt", "0"], ["createDynamicChild", "1"],
                    ["pushInt", "80"], ["pushInt", "20"], ["pushInt", "0"], ["pushInt", "0"], ["setContextSize", "1"],
                    ["pushString", "Child"], ["setContextText", "1"], ["pushInt", "255"], ["setContextColor", "1"],
                    ["pushInt", "128"], ["setContextTransparency", "1"], ["pushInt", "1"], ["setContextHidden", "1"],
                    ["pushInt", "123"], ["setContextModel", "1"], ["pushString", "Inspect"], ["setContextAction", "1"],
                    ["getContextY", "1"], ["setVar", "8"], ["pushInt", "64028702"], ["getY", "0"], ["setVar", "9"],
                    ["pushString", "Open"], ["pushInt", "64028702"], ["setAction", "0"],
                    ["pushInt", "64028702"], ["getDynamicChildCount", "0"], ["setVar", "7"], ["return", "0"],
                ]),
                program(2, [["pushInt", "64028702"], ["clearDynamicChildren", "0"],
                    ["pushInt", "64028702"], ["getDynamicChildCount", "0"], ["setVar", "7"], ["return", "0"]]),
            ],
        }, (id, _changes, children) => ({
            width: 80, height: 20, localX: 0, localY: children.some(child => child.id === id) ? 12 : 25,
        }));
        const opened = runtime.run({ event: "open" });
        expect(opened.message).toBeNull();
        expect(opened.state?.dynamicChildren).toMatchObject([{
            parentId: 30, type: 4, slot: 0, width: 80, height: 20, text: "Child", color: 255, transparency: 128, hidden: true,
            modelId: 123, action: "Inspect",
        }]);
        expect(opened.state?.integers).toEqual({ 7: 1, 8: 12, 9: 25 });
        expect(opened.state?.components).toEqual([{ id: 30, action: "Open" }]);
        expect(runtime.run({ event: "click", component: 30 }).state).toEqual({ components: [{ id: 30, action: "Open" }], integers: { 7: 0, 8: 12, 9: 25 } });
    });
    it("pushes resolved width, height and parent-relative X", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [{ component: 30, callbacks: [{ event: "Open", scriptId: 1, args: [], triggers: [] }] }],
            programs: [program(1, [
                ["pushInt", "8192"], ["pushInt", "20"], ["pushInt", "2"], ["pushInt", "0"], ["setContextSize", "0"],
                ["pushInt", "64028702"], ["getWidth", "0"], ["setVar", "7"],
                ["pushInt", "64028702"], ["getHeight", "0"], ["setVar", "8"],
                ["pushInt", "64028702"], ["getX", "0"], ["setVar", "9"], ["return", "0"],
            ])],
        }, (id, changes) => {
            expect(id).toBe(30);
            expect(changes.get(id)).toMatchObject({ width: 8192, widthMode: 2 });
            return { width: 256, height: 20, localX: 12, localY: 0 };
        });
        expect(runtime.run({ event: "open" }).state?.integers).toEqual({ 7: 256, 8: 20, 9: 12 });
    });

    it("clamps values and scales with Java integer arithmetic", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [{ component: 30, callbacks: [{ event: "Open", scriptId: 1, args: [], triggers: [] }] }],
            programs: [program(1, [
                ["pushInt", "-5"], ["pushInt", "0"], ["max", "0"], ["pushInt", "-1"], ["min", "0"], ["setVar", "1"],
                ["pushInt", "-5"], ["pushInt", "2"], ["pushInt", "3"], ["scale", "0"], ["setVar", "2"],
                ["pushInt", "2147483647"], ["pushInt", "2147483647"], ["pushInt", "2147483647"],
                ["scale", "0"], ["setVar", "3"], ["return", "0"],
            ])],
        });
        expect(runtime.run({ event: "open" }).state?.integers).toEqual({ 1: -1, 2: -7, 3: 2147483647 });
    });
    it.each([
        ["enter", "bindMouseEnter", pointerEvents.enter],
        ["exit", "bindMouseExit", pointerEvents.exit],
    ] as const)("binds mouse %s with captured arguments and clears it with script ID -1", (event, operation, name) => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [{ component: 30, callbacks: [
                { event: "Open", scriptId: 1, args: [], triggers: [] },
                { event: "Mouse pressed", scriptId: 3, args: [], triggers: [] },
            ] }],
            programs: [
                program(1, [["pushInt", "2"], ["pushString", "Hover"], ["pushString", "s"],
                    ["pushInt", "64028702"], [operation, "0"], ["return", "0"]]),
                program(2, [["loadString", "0"], ["pushInt", "64028702"], ["setText", "0"], ["return", "0"]], [0, 1, 0]),
                program(3, [["pushInt", "-1"], ["pushString", ""], ["pushInt", "64028702"], [operation, "0"], ["return", "0"]]),
            ],
        });
        expect(runtime.run({ event: "open" }).message).toBeNull();
        expect(runtime.hoverTargets(name).get(30)).toBe(true);
        expect(runtime.run({ event, component: 30 }).state?.components).toEqual([{ id: 30, text: "Hover" }]);
        expect(runtime.run({ event: "click", component: 30 }).message).toBeNull();
        expect(runtime.hoverTargets(name).get(30)).toBe(false);
    });
    it("sets transparency on the addressed component during Open", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [
                { component: 30, callbacks: [{ event: "Open", scriptId: 1, args: [], triggers: [] }] },
                { component: 31, callbacks: [] },
            ],
            programs: [program(1, [
                ["pushInt", "128"], ["pushInt", "64028703"], ["setTransparency", "0"], ["return", "0"],
            ])],
        });
        expect(runtime.run({ event: "open" }).state?.components).toEqual([{ id: 31, transparency: 128 }]);
    });
    it("consumes the parent operand when clearing dynamic children and preserves cached components", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [
                { component: 30, callbacks: [{ event: "Open", scriptId: 1, args: [], triggers: [] }] },
                { component: 31, callbacks: [] },
            ],
            programs: [program(1, [
                ["pushInt", "1"], ["pushInt", "64028702"], ["clearDynamicChildren", "0"],
                ["pushInt", "64028703"], ["setHidden", "0"], ["return", "0"],
            ])],
        });
        expect(runtime.run({ event: "open" })).toEqual({
            state: { components: [{ id: 31, hidden: true }], integers: {} }, message: null,
        });
    });
    it("passes pointer coordinates to a nested call and uses the returned stack value", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [{ component: 30, callbacks: [
                { event: "Mouse pressed", scriptId: 1, args: [{ type: "int", value: -2147483646 }], triggers: [] },
            ] }],
            programs: [
                program(1, [["loadInt", "0"], ["call", "2"], ["toString", "0"], ["pushInt", "64028702"], ["setText", "0"], ["return", "0"]], [1, 0, 0]),
                program(2, [["loadInt", "0"], ["pushInt", "2"], ["multiply", "0"], ["return", "0"]], [1, 0, 0]),
            ],
        });
        expect(runtime.run({ event: "click", component: 30, x: 17 }).state?.components).toEqual([{ id: 30, text: "34" }]);
    });

    it("rolls back changes when an operation fails or execution exceeds its limit", () => {
        const runtime = new WidgetRuntime(977, {
            version: 1,
            bindings: [{ component: 30, callbacks: [
                { event: "Open", scriptId: 1, args: [], triggers: [] },
                { event: "Mouse pressed", scriptId: 2, args: [], triggers: [] },
                { event: "Mouse released", scriptId: 3, args: [], triggers: [] },
            ] }],
            programs: [
                program(1, [["pushInt", "10"], ["setContextSprite", "0"], ["return", "0"]]),
                program(2, [["pushInt", "99"], ["setContextSprite", "0"], ["unsupported_999", "0"]]),
                program(3, [["jump", "-1"]]),
            ],
        });
        runtime.run({ event: "open" });
        expect(runtime.run({ event: "click", component: 30 }).message).toContain("instruction 2");
        expect(runtime.run({ event: "release", component: 30 }).message).toContain("Instruction limit");
        expect(runtime.run({ event: "enter", component: 30 }).state?.components).toEqual([{ id: 30, spriteId: 10 }]);
    });
});
