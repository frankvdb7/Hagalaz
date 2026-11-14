import type { WidgetCallback, WidgetPreviewState } from "./cache-widgets.service";

export interface WidgetProgramBundle {
    version: number;
    varbits?: Record<number, { configId: number; start: number; end: number }>;
    bindings: { component: number; callbacks: WidgetCallback[] | null }[];
    programs: {
        id: number;
        arguments: [number, number, number];
        locals: [number, number, number];
        instructions: { operation: string; operand: string }[];
        switches: Record<string, number>[];
    }[];
}

export type PreviewEvent = { event: "open" | "click" | "release" | "enter" | "exit" | "manual"; component?: number; callback?: string; trigger?: number; x?: number; y?: number };
export const pointerEvents = { click: "Mouse pressed", release: "Mouse released", enter: "Mouse entered", exit: "Mouse exited" } as const;
type HoverEvent = typeof pointerEvents.enter | typeof pointerEvents.exit;
const intChanged = "CS2 integer changed";
type RuntimeCallback = { scriptId: number; args: (number | string | bigint)[]; triggers?: number[] };

function pop<T>(stack: T[]): T {
    const value = stack.pop();
    if (value === undefined) throw new Error("Operand stack underflow.");
    return value;
}

function take<T>(stack: T[], count: number): T[] {
    if (count < 0 || count > stack.length) throw new Error("Operand stack underflow.");
    return stack.splice(stack.length - count, count);
}

function local<T>(locals: T[], index: number): T {
    if (index < 0 || index >= locals.length) throw new Error("Invalid local index.");
    return locals[index];
}

export class WidgetRuntime {
    private stringVariables = new Map<number, string>();
    private varps = new Map<number, number>();
    private callbacks: Record<HoverEvent | typeof intChanged, Map<number, RuntimeCallback | null>> = {
        [pointerEvents.enter]: new Map(),
        [pointerEvents.exit]: new Map(),
        [intChanged]: new Map(),
    };
    private readonly programs;
    private readonly components;
    private state: WidgetPreviewState = { components: [], integers: {} };

    constructor(private readonly widget: number, private readonly bundle: WidgetProgramBundle,
        private readonly resolveLayout?: (id: number, changes: ReadonlyMap<number, WidgetPreviewState["components"][number]>,
            children: NonNullable<WidgetPreviewState["dynamicChildren"]>) =>
            { width: number; height: number; localX: number; localY: number;
                component?: { scrollWidth?: number; scrollHeight?: number; options?: string[] } } | undefined) {
        if (bundle.version !== 1) throw new Error("Unsupported RuneScript program version.");
        this.programs = new Map(bundle.programs.map(program => [program.id, program]));
        this.components = new Set(bundle.bindings.map(binding => binding.component));
    }

    run(event: PreviewEvent): { state?: WidgetPreviewState; message: string | null } {
        const callbacks = {
            [pointerEvents.enter]: new Map(this.callbacks[pointerEvents.enter]),
            [pointerEvents.exit]: new Map(this.callbacks[pointerEvents.exit]),
            [intChanged]: new Map(this.callbacks[intChanged]),
        };
        const changes = new Map(this.state.components.map(entry => [entry.id, { ...entry }]));
        const children = (this.state.dynamicChildren ?? []).map(child => ({ ...child }));
        const integers = { ...this.state.integers };
        const stringVariables = new Map(this.stringVariables);
        const varps = new Map(this.varps);
        const changed = new Set<number>();
        let remaining = 10_000;
        const name = event.event === "open" ? "Open" : event.event === "manual" ? event.callback : pointerEvents[event.event];
        const failure = dispatch.call(this, name, event);
        if (failure) return { message: failure };
        while (changed.size) {
            const triggers = new Set(changed);
            changed.clear();
            const failure = dispatch.call(this, intChanged, { event: "manual" }, triggers);
            if (failure) return { message: failure };
        }
        this.state = { components: [...changes.values()], integers };
        this.stringVariables = stringVariables;
        this.varps = varps;
        if (children.length) this.state.dynamicChildren = children;
        this.callbacks = callbacks;
        return { state: this.state, message: null };

        function dispatch(this: WidgetRuntime, name: string | undefined, event: PreviewEvent, triggers?: ReadonlySet<number>): string | null {
            for (const binding of this.bundle.bindings) {
                if (!triggers && event.event !== "open" && binding.component !== event.component) continue;
                const override = name === pointerEvents.enter || name === pointerEvents.exit || name === intChanged
                    ? callbacks[name].get(binding.component) : undefined;
                if (override === null) continue;
                if (!triggers && !binding.callbacks && !override) return `Component ${binding.component}: callback metadata is unavailable.`;
                const callback = override ?? binding.callbacks?.find(entry => entry.event === name);
                if (!callback) continue;
                if ("triggers" in callback && callback.triggers?.length) {
                    if (triggers && !callback.triggers.some(trigger => triggers.has(trigger))) continue;
                    if (!triggers && (event.trigger === undefined || !callback.triggers.includes(event.trigger)))
                        return `${name}: trigger did not match.`;
                }
                try {
                    if (callback.scriptId === null) throw new Error("Script target is unavailable.");
                    const ints: number[] = [], strings: string[] = [], longs: bigint[] = [];
                    const context = (this.widget << 16) | binding.component;
                    const longArgs: bigint[] = [];
                    for (const argument of callback.args) {
                        const value = typeof argument === "object" ? argument.value : argument;
                        if (typeof value === "number" && !(typeof argument === "object" && argument.type === "unknown")) {
                            const bound = value === -2147483647 ? event.y : value === -2147483646 ? event.x :
                                value === -2147483645 ? context : [-2147483643, -2147483642, -2147483641].includes(value) ? -1 :
                                value >= -2147483647 && value <= -2147483639 ? undefined : value;
                            if (bound === undefined) throw new Error("Callback event input is unavailable.");
                            ints.push(bound);
                        } else if (typeof value === "string" && value !== "event_opbase") strings.push(value);
                        else if (typeof value === "bigint") longArgs.push(value);
                        else throw new Error("Callback argument context is unavailable.");
                    }
                    execute.call(this, callback.scriptId, 0, ints.splice(0), strings.splice(0), longArgs, context, ints, strings, longs, [], []);
                } catch (error) {
                    return `${name}, component ${binding.component}, script ${callback.scriptId}: ${(error as Error).message}`;
                }
            }
            return null;
        }

        function execute(this: WidgetRuntime, id: number, depth: number, iArgs: number[], sArgs: string[], lArgs: bigint[],
            context: number, ints: number[], strings: string[], longs: bigint[], selected: (number | undefined)[],
            arrays: Int32Array[]) {
            const program = this.programs.get(id);
            if (!program) throw new Error(`Script ${id} is unavailable.`);
            if (depth >= 32) throw new Error("Call depth limit exceeded.");
            const il = Array<number>(program.locals[0]).fill(0);
            const sl = Array<string>(program.locals[1]).fill("null");
            const ll = Array<bigint>(program.locals[2]).fill(0n);
            if (iArgs.length > il.length || sArgs.length > sl.length || lArgs.length > ll.length) throw new Error("Invalid arguments.");
            iArgs.forEach((value, index) => { il[index] = value; });
            sArgs.forEach((value, index) => { sl[index] = value; });
            lArgs.forEach((value, index) => { ll[index] = value; });
            for (let pc = 0; pc < program.instructions.length; pc++) {
                const { operation, operand } = program.instructions[pc];
                const n = Number(operand);
                try {
                    if (--remaining < 0) throw new Error("Instruction limit exceeded.");
                    const jump = (offset: number) => {
                        const target = pc + offset + 1;
                        if (target < 0 || target >= program.instructions.length) throw new Error("Invalid branch target.");
                        pc = target - 1;
                    };
                    switch (operation) {
                        case "return": return;
                        case "call": {
                            const callee = this.programs.get(n);
                            if (!callee) throw new Error(`Script ${n} is unavailable.`);
                            const [i, s, l] = callee.arguments;
                            execute.call(this, n, depth + 1, take(ints, i), take(strings, s), take(longs, l), context, ints, strings, longs, selected, arrays);
                            break;
                        }
                        case "pushInt": ints.push(n); break;
                        case "pushString": strings.push(operand); break;
                        case "pushLong": longs.push(BigInt(operand)); break;
                        case "loadInt": ints.push(local(il, n)); break;
                        case "loadString": strings.push(local(sl, n)); break;
                        case "loadLong": longs.push(local(ll, n)); break;
                        case "storeInt": local(il, n); il[n] = pop(ints); break;
                        case "storeString": local(sl, n); sl[n] = pop(strings); break;
                        case "storeLong": local(ll, n); ll[n] = pop(longs); break;
                        case "getVar": ints.push(integers[n] ?? 0); break;
                        case "setVar": {
                            const value = pop(ints);
                            changed.add(n);
                            integers[n] = value;
                            break;
                        }
                        case "getVarp": ints.push(varps.get(n) ?? 0); break;
                        case "setVarp": varps.set(n, pop(ints)); break;
                        case "getVarbit":
                        case "setVarbit": {
                            const bit = this.bundle.varbits?.[n];
                            if (!bit || bit.start < 0 || bit.end > 31 || bit.end < bit.start)
                                throw new Error(`Varbit ${n} is unavailable.`);
                            const mask = 0xffffffff >>> (31 - bit.end + bit.start);
                            const config = varps.get(bit.configId) ?? 0;
                            if (operation === "getVarbit") ints.push(((config >>> bit.start) & mask) | 0);
                            else {
                                const value = pop(ints);
                                varps.set(bit.configId, (config & ~(mask << bit.start)) |
                                    ((value < 0 || value > mask ? 0 : value) << bit.start));
                            }
                            break;
                        }
                        case "getStringVar": strings.push(stringVariables.get(n) ?? ""); break;
                        case "setStringVar": stringVariables.set(n, pop(strings)); break;
                        case "newIntArray": {
                            const slot = n >> 16;
                            const length = pop(ints);
                            if (slot < 0 || slot >= 5 || length < 0 || length > 5000) throw new Error("Invalid array allocation.");
                            arrays[slot] = new Int32Array(length).fill((n & 65535) === 105 ? 0 : -1);
                            break;
                        }
                        case "loadArrayInt":
                        case "storeArrayInt": {
                            const value = operation === "storeArrayInt" ? pop(ints) : undefined;
                            const index = pop(ints);
                            const array = arrays[n];
                            if (!array || index < 0 || index >= array.length) throw new Error("Invalid array index.");
                            if (value === undefined) ints.push(array[index]);
                            else array[index] = value;
                            break;
                        }
                        case "popInt": pop(ints); break;
                        case "popString": pop(strings); break;
                        case "join": {
                            const parts = take(strings, n);
                            if (parts.reduce((size, part) => size + part.length, 0) > 65_536) throw new Error("String limit exceeded.");
                            strings.push(parts.join(""));
                            break;
                        }
                        case "toString": strings.push(String(pop(ints))); break;
                        case "add": case "subtract": case "multiply": case "divide": {
                            const [a, b] = take(ints, 2);
                            if (operation === "divide" && b === 0) throw new Error("Division by zero.");
                            ints.push((operation === "add" ? a + b : operation === "subtract" ? a - b :
                                operation === "multiply" ? Math.imul(a, b) : a / b) | 0);
                            break;
                        }
                        case "jump": jump(n); break;
                        case "min":
                        case "max": {
                            const [left, right] = take(ints, 2);
                            ints.push(operation === "min" ? Math.min(left, right) : Math.max(left, right));
                            break;
                        }
                        case "scale": {
                            const [value, divisor, multiplier] = take(ints, 3);
                            if (divisor === 0) throw new Error("Division by zero.");
                            ints.push(Number(BigInt.asIntN(32, (BigInt(value) * BigInt(multiplier)) / BigInt(divisor))));
                            break;
                        }
                        case "equal": case "notEqual": case "less": case "lessEqual": case "greater": case "greaterEqual": {
                            const [a, b] = take(ints, 2);
                            if (operation === "equal" ? a === b : operation === "notEqual" ? a !== b :
                                operation === "less" ? a < b : operation === "lessEqual" ? a <= b :
                                operation === "greater" ? a > b : a >= b) jump(n);
                            break;
                        }
                        case "switch": {
                            const offset = program.switches[n][pop(ints)];
                            if (offset !== undefined) jump(offset);
                            break;
                        }
                        case "getWidth":
                        case "getHeight":
                        case "getX":
                        case "getY":
                        case "getContextY": {
                            const implicit = operation === "getContextY";
                            const active = implicit ? selected[n === 1 ? 1 : 0] : undefined;
                            if (implicit && n === 1 && active === undefined) throw new Error("Secondary widget context is unavailable.");
                            const packed = implicit ? context : pop(ints);
                            const component = active ?? (packed & 65535);
                            const layout = packed >>> 16 === this.widget && (active !== undefined || this.components.has(component))
                                ? this.resolveLayout?.(component, changes, children) : undefined;
                            if (!layout) throw new Error(`Widget component ${packed >>> 16}:${component} is unavailable.`);
                            ints.push(operation === "getWidth" ? layout.width : operation === "getHeight" ? layout.height :
                                operation === "getX" ? layout.localX : layout.localY);
                            break;
                        }
                        case "createDynamicChild": {
                            const [packed, type, slot] = take(ints, 3);
                            const parentId = packed & 65535;
                            if (packed >>> 16 !== this.widget || !this.components.has(parentId))
                                throw new Error(`Widget component ${packed >>> 16}:${parentId} is unavailable.`);
                            if (type <= 0 || slot < 0 || (slot > 0 && !children.some(child => child.parentId === parentId && child.slot === slot - 1)))
                                throw new Error("Invalid dynamic child type or slot.");
                            const existing = children.findIndex(child => child.parentId === parentId && child.slot === slot);
                            const child = {
                                id: existing < 0 ? children.reduce((id, entry) => Math.min(id, entry.id - 1), -2) : children[existing].id,
                                parentId, slot, type, name: "", contentType: 0, x: 0, y: 0, width: 0, height: 0,
                                xMode: 0, yMode: 0, widthMode: 0, heightMode: 0, hidden: false, blocksClicks: false,
                            };
                            if (existing < 0) children.push(child);
                            else children[existing] = child;
                            selected[n === 1 ? 1 : 0] = child.id;
                            break;
                        }
                        case "bindMouseEnter":
                        case "bindMouseExit":
                        case "bindIntChanged":
                        case "getScrollY":
                        case "setScroll":
                        case "getDynamicChildCount":
                        case "clearDynamicChildren": {
                            const packed = pop(ints);
                            const component = packed & 65535;
                            if (packed >>> 16 !== this.widget || !this.components.has(component))
                                throw new Error(`Widget component ${packed >>> 16}:${component} is unavailable.`);
                            if (operation === "getScrollY") {
                                ints.push(changes.get(component)?.scrollY ?? 0);
                            } else if (operation === "setScroll") {
                                const [x, y] = take(ints, 2);
                                const layout = this.resolveLayout?.(component, changes, children);
                                if (!layout) throw new Error(`Widget component ${packed >>> 16}:${component} is unavailable.`);
                                changes.set(component, {
                                    ...changes.get(component), id: component,
                                    scrollX: clamp(x, (layout.component?.scrollWidth || layout.width) - layout.width),
                                    scrollY: clamp(y, (layout.component?.scrollHeight || layout.height) - layout.height),
                                });
                            } else if (operation === "getDynamicChildCount") {
                                const slots = new Set(children.filter(child => child.parentId === component).map(child => child.slot));
                                let count = 0;
                                while (slots.has(count)) count++;
                                ints.push(count);
                            } else if (operation === "clearDynamicChildren") {
                                for (let i = children.length - 1; i >= 0; i--) {
                                    if (children[i].parentId === component) {
                                        const id = children[i].id;
                                        for (let lane = 0; lane < selected.length; lane++) {
                                            if (selected[lane] === id) selected[lane] = undefined;
                                        }
                                        children.splice(i, 1);
                                    }
                                }
                            } else {
                                let signature = pop(strings);
                                let triggers: number[] | undefined;
                                if (signature.endsWith("Y")) {
                                    const count = pop(ints);
                                    if (count > 0) {
                                        triggers = take(ints, count);
                                        signature = signature.slice(0, -1);
                                    }
                                }
                                const args: (number | string | bigint)[] = [];
                                for (let i = signature.length - 1; i >= 0; i--) {
                                    args[i] = signature[i] === "s" ? pop(strings) : signature[i] === "§" ? pop(longs) : pop(ints);
                                }
                                const scriptId = pop(ints);
                                const eventName = operation === "bindIntChanged" ? intChanged :
                                    operation === "bindMouseEnter" ? pointerEvents.enter : pointerEvents.exit;
                                callbacks[eventName].set(component, scriptId === -1 ? null :
                                    { scriptId, args, triggers: eventName === intChanged ? triggers : undefined });
                            }
                            break;
                        }
                        case "setSprite": case "setText": case "setColor": case "setPosition": case "setHidden":
                        case "setSize": case "setModel": case "setTransparency": case "setContextSprite": case "setContextPosition": case "setContextSize":
                        case "setContextColor": case "setContextTransparency": case "setContextHidden": case "setContextText":
                        case "setContextModel": case "setContextAction": case "setAction": case "setContextSpriteTiled": case "setOption": {
                            const implicit = operation.startsWith("setContext");
                            const active = implicit ? selected[n === 1 ? 1 : 0] : undefined;
                            if (implicit && n === 1 && active === undefined) throw new Error("Secondary widget context is unavailable.");
                            const packed = implicit ? context : pop(ints);
                            const component = packed & 65535;
                            if (packed >>> 16 !== this.widget || !this.components.has(component))
                                throw new Error(`Widget component ${packed >>> 16}:${component} is unavailable.`);
                            const child = active === undefined ? undefined : children.find(entry => entry.id === active);
                            const entry = child ?? changes.get(component) ?? { id: component };
                            if (operation.endsWith("Sprite")) entry.spriteId = pop(ints);
                            else if (operation === "setContextSpriteTiled") entry.spriteTiled = pop(ints) === 1;
                            else if (operation === "setOption") {
                                const slot = pop(ints) - 1;
                                const label = pop(strings);
                                if (slot < 0 || slot >= 10) break;
                                const options = entry.options ?? this.resolveLayout?.(component, changes, children)?.component?.options ?? [];
                                entry.options = Array.from({ length: Math.max(options.length, slot + 1) },
                                    (_, index) => index === slot ? label : options[index] ?? "");
                            }
                            else if (operation.endsWith("Text")) entry.text = pop(strings);
                            else if (operation.endsWith("Color")) entry.color = pop(ints);
                            else if (operation.endsWith("Transparency")) entry.transparency = pop(ints);
                            else if (operation.endsWith("Hidden")) entry.hidden = pop(ints) === 1;
                            else if (operation.endsWith("Model")) entry.modelId = pop(ints);
                            else if (operation.endsWith("Action")) entry.action = pop(strings);
                            else {
                                const [a, b, m, n] = take(ints, 4);
                                if (operation.endsWith("Position")) Object.assign(entry, { x: a, y: b, xMode: clamp(m, 5), yMode: clamp(n, 5) });
                                else Object.assign(entry, { width: a, height: b, widthMode: clamp(m, 4), heightMode: clamp(n, 4) });
                            }
                            if (!child) changes.set(component, entry);
                            break;
                        }
                        default: throw new Error(`Unsupported operation ${operation}.`);
                    }
                } catch (error) {
                    throw new Error(`Script ${id}, instruction ${pc}: ${(error as Error).message}`);
                }
            }
            throw new Error(`Script ${id}: missing return.`);
        }
    }

    hoverTargets(event: HoverEvent): ReadonlyMap<number, boolean> {
        return new Map([...this.callbacks[event]].map(([id, callback]) => [id, callback !== null]));
    }
}

function clamp(value: number, max: number) {
    return Math.max(0, Math.min(max, value));
}
