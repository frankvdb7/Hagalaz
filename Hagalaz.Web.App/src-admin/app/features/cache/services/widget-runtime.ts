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
export type HoverEvent = typeof pointerEvents.enter | typeof pointerEvents.exit;
const intChanged = "CS2 integer changed";
type RebindableEvent = HoverEvent | typeof intChanged;
type RuntimeCallback = { scriptId: number; args: (number | string | bigint)[]; triggers?: number[] };
type Callbacks = Record<RebindableEvent, Map<number, RuntimeCallback | null>>;
type Program = WidgetProgramBundle["programs"][number];
type Binding = WidgetProgramBundle["bindings"][number];
type ComponentChange = WidgetPreviewState["components"][number];
type DynamicChild = NonNullable<WidgetPreviewState["dynamicChildren"]>[number];
type ComponentEntry = ComponentChange | DynamicChild;
export type WidgetLayoutResolver = (id: number, changes: ReadonlyMap<number, WidgetPreviewState["components"][number]>,
    children: NonNullable<WidgetPreviewState["dynamicChildren"]>) =>
    { width: number; height: number; localX: number; localY: number;
        component?: { scrollWidth?: number; scrollHeight?: number; options?: string[] } } | undefined;

interface RuntimeDefinition {
    readonly widget: number;
    readonly bundle: WidgetProgramBundle;
    readonly programs: ReadonlyMap<number, Program>;
    readonly components: ReadonlySet<number>;
    readonly resolveLayout?: WidgetLayoutResolver;
}

interface RuntimeSnapshot {
    readonly state: WidgetPreviewState;
    readonly stringVariables: ReadonlyMap<number, string>;
    readonly varps: ReadonlyMap<number, number>;
    readonly callbacks: Callbacks;
}

interface Stacks {
    readonly ints: number[];
    readonly strings: string[];
    readonly longs: bigint[];
}

interface Frame extends Stacks {
    readonly id: number;
    readonly program: Program;
    readonly depth: number;
    readonly context: number;
    readonly il: number[];
    readonly sl: string[];
    readonly ll: bigint[];
    readonly selected: (number | undefined)[];
    readonly arrays: Int32Array[];
    pc: number;
    returned: boolean;
}

type Operation = (run: Execution, frame: Frame, n: number, operand: string) => void;

const instructionLimit = 10_000;
const maxCallDepth = 32;
const unavailableEventInput = Symbol("unavailable");

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

function clamp(value: number, max: number) {
    return Math.max(0, Math.min(max, value));
}

function cloneCallbacks(callbacks: Callbacks): Callbacks {
    return {
        [pointerEvents.enter]: new Map(callbacks[pointerEvents.enter]),
        [pointerEvents.exit]: new Map(callbacks[pointerEvents.exit]),
        [intChanged]: new Map(callbacks[intChanged]),
    };
}

function isRebindable(name: string | undefined): name is RebindableEvent {
    return name === pointerEvents.enter || name === pointerEvents.exit || name === intChanged;
}

function eventName(event: PreviewEvent): string | undefined {
    if (event.event === "open") return "Open";
    return event.event === "manual" ? event.callback : pointerEvents[event.event];
}

/** Resolves the client's reserved event-input sentinels (mouse X/Y, component context, unsupported inputs). */
function eventInt(value: number, event: PreviewEvent, context: number): number {
    if (value < -2147483647 || value > -2147483639) return value;
    const inputs: Record<number, number | undefined> = {
        [-2147483647]: event.y, [-2147483646]: event.x, [-2147483645]: context,
        [-2147483643]: -1, [-2147483642]: -1, [-2147483641]: -1,
    };
    const bound = inputs[value] ?? unavailableEventInput;
    if (bound === unavailableEventInput) throw new Error("Callback event input is unavailable.");
    return bound;
}

function bindArguments(args: WidgetCallback["args"] | RuntimeCallback["args"], event: PreviewEvent, context: number): Stacks {
    const bound: Stacks = { ints: [], strings: [], longs: [] };
    for (const argument of args) {
        const value = typeof argument === "object" ? argument.value : argument;
        const unknown = typeof argument === "object" && argument.type === "unknown";
        if (typeof value === "number" && !unknown) bound.ints.push(eventInt(value, event, context));
        else if (typeof value === "string" && value !== "event_opbase") bound.strings.push(value);
        else if (typeof value === "bigint") bound.longs.push(value);
        else throw new Error("Callback argument context is unavailable.");
    }
    return bound;
}

/** True when the callback should run, false to skip it, or a failure message for a direct event that misses its trigger. */
function triggerOutcome(callback: WidgetCallback | RuntimeCallback, name: string | undefined, event: PreviewEvent,
    triggers: ReadonlySet<number> | undefined): boolean | string {
    const required = callback.triggers;
    if (!required?.length) return true;
    if (triggers) return required.some(trigger => triggers.has(trigger));
    return event.trigger !== undefined && required.includes(event.trigger) ? true : `${name}: trigger did not match.`;
}

function declaredCallback(binding: Binding, name: string | undefined): WidgetCallback | undefined {
    return binding.callbacks?.find(entry => entry.event === name);
}

function triggeredCallback(callback: WidgetCallback | RuntimeCallback, name: string | undefined, event: PreviewEvent,
    triggers: ReadonlySet<number> | undefined): WidgetCallback | RuntimeCallback | string | undefined {
    const outcome = triggerOutcome(callback, name, event, triggers);
    return outcome === true ? callback : outcome || undefined;
}

function jump(frame: Frame, offset: number) {
    const target = frame.pc + offset + 1;
    if (target < 0 || target >= frame.program.instructions.length) throw new Error("Invalid branch target.");
    frame.pc = target - 1;
}

function contextLane(frame: Frame, n: number): number | undefined {
    const active = frame.selected[n === 1 ? 1 : 0];
    if (n === 1 && active === undefined) throw new Error("Secondary widget context is unavailable.");
    return active;
}

function laneIndex(n: number) {
    return n === 1 ? 1 : 0;
}

const arithmetic: Record<string, (a: number, b: number) => number> = {
    add: (a, b) => a + b,
    subtract: (a, b) => a - b,
    multiply: (a, b) => Math.imul(a, b),
    divide: (a, b) => {
        if (b === 0) throw new Error("Division by zero.");
        return a / b;
    },
};

const comparisons: Record<string, (a: number, b: number) => boolean> = {
    equal: (a, b) => a === b,
    notEqual: (a, b) => a !== b,
    less: (a, b) => a < b,
    lessEqual: (a, b) => a <= b,
    greater: (a, b) => a > b,
    greaterEqual: (a, b) => a >= b,
};

const layoutFields = { getWidth: "width", getHeight: "height", getX: "localX", getY: "localY", getContextY: "localY" } as const;

/** Applies one property write; returning false discards the write without recording a component change. */
type PropertySetter = (run: Execution, frame: Frame, entry: ComponentEntry, component: number) => unknown;

const propertySetters: Record<string, PropertySetter> = {
    Sprite: (_run, f, entry) => { entry.spriteId = pop(f.ints); },
    SpriteTiled: (_run, f, entry) => { entry.spriteTiled = pop(f.ints) === 1; },
    Option: (run, f, entry, component) => run.setOption(f, entry, component),
    Text: (_run, f, entry) => { entry.text = pop(f.strings); },
    Color: (_run, f, entry) => { entry.color = pop(f.ints); },
    Transparency: (_run, f, entry) => { entry.transparency = pop(f.ints); },
    Hidden: (_run, f, entry) => { entry.hidden = pop(f.ints) === 1; },
    Model: (_run, f, entry) => { entry.modelId = pop(f.ints); },
    Action: (_run, f, entry) => { entry.action = pop(f.strings); },
    Position: (_run, f, entry) => {
        const [x, y, xMode, yMode] = take(f.ints, 4);
        Object.assign(entry, { x, y, xMode: clamp(xMode, 5), yMode: clamp(yMode, 5) });
    },
    Size: (_run, f, entry) => {
        const [width, height, widthMode, heightMode] = take(f.ints, 4);
        Object.assign(entry, { width, height, widthMode: clamp(widthMode, 4), heightMode: clamp(heightMode, 4) });
    },
};

const setterOperations = [
    "setSprite", "setText", "setColor", "setPosition", "setHidden", "setSize", "setModel", "setTransparency", "setAction", "setOption",
    "setContextSprite", "setContextPosition", "setContextSize", "setContextColor", "setContextTransparency", "setContextHidden",
    "setContextText", "setContextModel", "setContextAction", "setContextSpriteTiled",
];

function setterOperation(operation: string): Operation {
    const implicit = operation.startsWith("setContext");
    const setter = propertySetters[operation.slice(implicit ? "setContext".length : "set".length)];
    return (run, frame, n) => run.setProperty(frame, n, implicit, setter);
}

function binaryOperation(fn: (a: number, b: number) => number): Operation {
    return (_run, f) => {
        const [a, b] = take(f.ints, 2);
        f.ints.push(fn(a, b));
    };
}

function branchOperation(predicate: (a: number, b: number) => boolean): Operation {
    return (_run, f, n) => {
        const [a, b] = take(f.ints, 2);
        if (predicate(a, b)) jump(f, n);
    };
}

function ownedComponentOperation(fn: (run: Execution, frame: Frame, component: number) => void): Operation {
    return (run, frame) => fn(run, frame, run.ownedComponent(pop(frame.ints)));
}

function entriesOf<T>(record: Record<string, T>, map: (value: T, key: string) => Operation): [string, Operation][] {
    return Object.entries(record).map(([key, value]) => [key, map(value, key)]);
}

const operations = new Map<string, Operation>([
    ["return", (_run, f) => { f.returned = true; }],
    ["call", (run, f, n) => run.call(f, n)],
    ["pushInt", (_run, f, n) => { f.ints.push(n); }],
    ["pushString", (_run, f, _n, operand) => { f.strings.push(operand); }],
    ["pushLong", (_run, f, _n, operand) => { f.longs.push(BigInt(operand)); }],
    ["loadInt", (_run, f, n) => { f.ints.push(local(f.il, n)); }],
    ["loadString", (_run, f, n) => { f.strings.push(local(f.sl, n)); }],
    ["loadLong", (_run, f, n) => { f.longs.push(local(f.ll, n)); }],
    ["storeInt", (_run, f, n) => { local(f.il, n); f.il[n] = pop(f.ints); }],
    ["storeString", (_run, f, n) => { local(f.sl, n); f.sl[n] = pop(f.strings); }],
    ["storeLong", (_run, f, n) => { local(f.ll, n); f.ll[n] = pop(f.longs); }],
    ["getVar", (run, f, n) => { f.ints.push(run.integers[n] ?? 0); }],
    ["setVar", (run, f, n) => run.setVar(n, pop(f.ints))],
    ["getVarp", (run, f, n) => { f.ints.push(run.varps.get(n) ?? 0); }],
    ["setVarp", (run, f, n) => { run.varps.set(n, pop(f.ints)); }],
    ["getVarbit", (run, f, n) => { f.ints.push(run.getVarbit(n)); }],
    ["setVarbit", (run, f, n) => run.setVarbit(n, pop(f.ints))],
    ["getStringVar", (run, f, n) => { f.strings.push(run.stringVariables.get(n) ?? ""); }],
    ["setStringVar", (run, f, n) => { run.stringVariables.set(n, pop(f.strings)); }],
    ["newIntArray", (_run, f, n) => {
        const slot = n >> 16;
        const length = pop(f.ints);
        if (slot < 0 || slot >= 5 || length < 0 || length > 5000) throw new Error("Invalid array allocation.");
        f.arrays[slot] = new Int32Array(length).fill((n & 65535) === 105 ? 0 : -1);
    }],
    ["loadArrayInt", (_run, f, n) => {
        const index = pop(f.ints);
        f.ints.push(f.arrays[n][arrayIndex(f.arrays[n], index)]);
    }],
    ["storeArrayInt", (_run, f, n) => {
        const value = pop(f.ints);
        const index = pop(f.ints);
        f.arrays[n][arrayIndex(f.arrays[n], index)] = value;
    }],
    ["popInt", (_run, f) => { pop(f.ints); }],
    ["popString", (_run, f) => { pop(f.strings); }],
    ["join", (_run, f, n) => {
        const parts = take(f.strings, n);
        if (parts.reduce((size, part) => size + part.length, 0) > 65_536) throw new Error("String limit exceeded.");
        f.strings.push(parts.join(""));
    }],
    ["toString", (_run, f) => { f.strings.push(String(pop(f.ints))); }],
    ...entriesOf(arithmetic, fn => binaryOperation((a, b) => fn(a, b) | 0)),
    ["jump", (_run, f, n) => jump(f, n)],
    ["min", binaryOperation(Math.min)],
    ["max", binaryOperation(Math.max)],
    ["scale", (_run, f) => {
        const [value, divisor, multiplier] = take(f.ints, 3);
        if (divisor === 0) throw new Error("Division by zero.");
        f.ints.push(Number(BigInt.asIntN(32, (BigInt(value) * BigInt(multiplier)) / BigInt(divisor))));
    }],
    ...entriesOf(comparisons, branchOperation),
    ["switch", (_run, f, n) => {
        const offset = f.program.switches[n][pop(f.ints)];
        if (offset !== undefined) jump(f, offset);
    }],
    ...entriesOf(layoutFields, (field, operation) => (run, f, n) => {
        f.ints.push(run.layoutOf(f, n, operation === "getContextY")[field]);
    }),
    ["createDynamicChild", (run, f, n) => run.createDynamicChild(f, n)],
    ["bindMouseEnter", ownedComponentOperation((run, f, component) => run.bindCallback(f, component, pointerEvents.enter))],
    ["bindMouseExit", ownedComponentOperation((run, f, component) => run.bindCallback(f, component, pointerEvents.exit))],
    ["bindIntChanged", ownedComponentOperation((run, f, component) => run.bindCallback(f, component, intChanged))],
    ["getScrollY", ownedComponentOperation((run, f, component) => { f.ints.push(run.changes.get(component)?.scrollY ?? 0); })],
    ["setScroll", ownedComponentOperation((run, f, component) => run.setScroll(f, component))],
    ["getDynamicChildCount", ownedComponentOperation((run, f, component) => { f.ints.push(run.dynamicChildCount(component)); })],
    ["clearDynamicChildren", ownedComponentOperation((run, f, component) => run.clearDynamicChildren(f, component))],
    ...setterOperations.map((operation): [string, Operation] => [operation, setterOperation(operation)]),
]);

function arrayIndex(array: Int32Array | undefined, index: number): number {
    if (!array || index < 0 || index >= array.length) throw new Error("Invalid array index.");
    return index;
}

/** One preview event's working copy of runtime state; committed by {@link WidgetRuntime} only when every script succeeds. */
class Execution {
    readonly callbacks: Callbacks;
    readonly changes: Map<number, ComponentChange>;
    readonly children: DynamicChild[];
    readonly integers: Record<string, number>;
    readonly stringVariables: Map<number, string>;
    readonly varps: Map<number, number>;
    private readonly changed = new Set<number>();
    private remaining = instructionLimit;

    constructor(private readonly definition: RuntimeDefinition, snapshot: RuntimeSnapshot) {
        this.callbacks = cloneCallbacks(snapshot.callbacks);
        this.changes = new Map(snapshot.state.components.map(entry => [entry.id, { ...entry }]));
        this.children = (snapshot.state.dynamicChildren ?? []).map(child => ({ ...child }));
        this.integers = { ...snapshot.state.integers };
        this.stringVariables = new Map(snapshot.stringVariables);
        this.varps = new Map(snapshot.varps);
    }

    /** Runs the event and every integer-change cascade it causes, returning a failure message or null. */
    runEvent(event: PreviewEvent): string | null {
        const failure = this.dispatch(eventName(event), event);
        if (failure) return failure;
        while (this.changed.size) {
            const triggers = new Set(this.changed);
            this.changed.clear();
            const cascadeFailure = this.dispatch(intChanged, { event: "manual" }, triggers);
            if (cascadeFailure) return cascadeFailure;
        }
        return null;
    }

    snapshot(): RuntimeSnapshot {
        const state: WidgetPreviewState = { components: [...this.changes.values()], integers: this.integers };
        if (this.children.length) state.dynamicChildren = this.children;
        return { state, stringVariables: this.stringVariables, varps: this.varps, callbacks: this.callbacks };
    }

    private dispatch(name: string | undefined, event: PreviewEvent, triggers?: ReadonlySet<number>): string | null {
        for (const binding of this.definition.bundle.bindings) {
            if (!triggers && event.event !== "open" && binding.component !== event.component) continue;
            const callback = this.selectCallback(binding, name, event, triggers);
            if (typeof callback === "string") return callback;
            const failure = callback ? this.invoke(binding.component, name, callback, event) : null;
            if (failure) return failure;
        }
        return null;
    }

    private selectCallback(binding: Binding, name: string | undefined, event: PreviewEvent,
        triggers: ReadonlySet<number> | undefined): WidgetCallback | RuntimeCallback | string | undefined {
        const override = isRebindable(name) ? this.callbacks[name].get(binding.component) : undefined;
        if (override === null) return undefined;
        if (!triggers && !binding.callbacks && !override) return `Component ${binding.component}: callback metadata is unavailable.`;
        const callback = override ?? declaredCallback(binding, name);
        return callback && triggeredCallback(callback, name, event, triggers);
    }

    private invoke(component: number, name: string | undefined, callback: WidgetCallback | RuntimeCallback, event: PreviewEvent): string | null {
        try {
            if (callback.scriptId === null) throw new Error("Script target is unavailable.");
            const context = (this.definition.widget << 16) | component;
            const args = bindArguments(callback.args, event, context);
            this.execute(callback.scriptId, 0, args, context, { ints: [], strings: [], longs: [] }, [], []);
            return null;
        } catch (error) {
            return `${name}, component ${component}, script ${callback.scriptId}: ${(error as Error).message}`;
        }
    }

    private execute(id: number, depth: number, args: Stacks, context: number, stacks: Stacks,
        selected: (number | undefined)[], arrays: Int32Array[]) {
        const program = this.definition.programs.get(id);
        if (!program) throw new Error(`Script ${id} is unavailable.`);
        if (depth >= maxCallDepth) throw new Error("Call depth limit exceeded.");
        const frame: Frame = {
            ...stacks, id, program, depth, context, selected, arrays, pc: 0, returned: false,
            il: Array<number>(program.locals[0]).fill(0),
            sl: Array<string>(program.locals[1]).fill("null"),
            ll: Array<bigint>(program.locals[2]).fill(0n),
        };
        if (args.ints.length > frame.il.length || args.strings.length > frame.sl.length || args.longs.length > frame.ll.length)
            throw new Error("Invalid arguments.");
        frame.il.splice(0, args.ints.length, ...args.ints);
        frame.sl.splice(0, args.strings.length, ...args.strings);
        frame.ll.splice(0, args.longs.length, ...args.longs);
        for (; frame.pc < program.instructions.length; frame.pc++) {
            this.step(frame);
            if (frame.returned) return;
        }
        throw new Error(`Script ${id}: missing return.`);
    }

    private step(frame: Frame) {
        const { operation, operand } = frame.program.instructions[frame.pc];
        try {
            if (--this.remaining < 0) throw new Error("Instruction limit exceeded.");
            const handler = operations.get(operation);
            if (!handler) throw new Error(`Unsupported operation ${operation}.`);
            handler(this, frame, Number(operand), operand);
        } catch (error) {
            throw new Error(`Script ${frame.id}, instruction ${frame.pc}: ${(error as Error).message}`);
        }
    }

    call(frame: Frame, id: number) {
        const callee = this.definition.programs.get(id);
        if (!callee) throw new Error(`Script ${id} is unavailable.`);
        const [ints, strings, longs] = callee.arguments;
        const args = { ints: take(frame.ints, ints), strings: take(frame.strings, strings), longs: take(frame.longs, longs) };
        this.execute(id, frame.depth + 1, args, frame.context, frame, frame.selected, frame.arrays);
    }

    setVar(id: number, value: number) {
        this.changed.add(id);
        this.integers[id] = value;
    }

    private varbit(id: number) {
        const bit = this.definition.bundle.varbits?.[id];
        if (!bit || bit.start < 0 || bit.end > 31 || bit.end < bit.start) throw new Error(`Varbit ${id} is unavailable.`);
        return { bit, mask: 0xffffffff >>> (31 - bit.end + bit.start), config: this.varps.get(bit.configId) ?? 0 };
    }

    getVarbit(id: number): number {
        const { bit, mask, config } = this.varbit(id);
        return ((config >>> bit.start) & mask) | 0;
    }

    setVarbit(id: number, value: number) {
        const { bit, mask, config } = this.varbit(id);
        this.varps.set(bit.configId, (config & ~(mask << bit.start)) | ((value < 0 || value > mask ? 0 : value) << bit.start));
    }

    ownedComponent(packed: number): number {
        const component = packed & 65535;
        if (packed >>> 16 !== this.definition.widget || !this.definition.components.has(component))
            throw new Error(`Widget component ${packed >>> 16}:${component} is unavailable.`);
        return component;
    }

    private resolveLayout(component: number) {
        return this.definition.resolveLayout?.(component, this.changes, this.children);
    }

    layoutOf(frame: Frame, n: number, implicit: boolean) {
        const active = implicit ? contextLane(frame, n) : undefined;
        const packed = implicit ? frame.context : pop(frame.ints);
        const component = active ?? (packed & 65535);
        const owned = packed >>> 16 === this.definition.widget && (active !== undefined || this.definition.components.has(component));
        const layout = owned ? this.resolveLayout(component) : undefined;
        if (!layout) throw new Error(`Widget component ${packed >>> 16}:${component} is unavailable.`);
        return layout;
    }

    createDynamicChild(frame: Frame, n: number) {
        const [packed, type, slot] = take(frame.ints, 3);
        const parentId = this.ownedComponent(packed);
        const occupies = (child: DynamicChild, at: number) => child.parentId === parentId && child.slot === at;
        if (type <= 0 || slot < 0 || (slot > 0 && !this.children.some(child => occupies(child, slot - 1))))
            throw new Error("Invalid dynamic child type or slot.");
        const existing = this.children.findIndex(child => occupies(child, slot));
        const child: DynamicChild = {
            id: existing < 0 ? this.children.reduce((id, entry) => Math.min(id, entry.id - 1), -2) : this.children[existing].id,
            parentId, slot, type, name: "", contentType: 0, x: 0, y: 0, width: 0, height: 0,
            xMode: 0, yMode: 0, widthMode: 0, heightMode: 0, hidden: false, blocksClicks: false,
        };
        if (existing < 0) this.children.push(child);
        else this.children[existing] = child;
        frame.selected[laneIndex(n)] = child.id;
    }

    setScroll(frame: Frame, component: number) {
        const [x, y] = take(frame.ints, 2);
        const layout = this.resolveLayout(component);
        if (!layout) throw new Error(`Widget component ${this.definition.widget}:${component} is unavailable.`);
        this.changes.set(component, {
            ...this.changes.get(component), id: component,
            scrollX: clamp(x, (layout.component?.scrollWidth || layout.width) - layout.width),
            scrollY: clamp(y, (layout.component?.scrollHeight || layout.height) - layout.height),
        });
    }

    dynamicChildCount(component: number): number {
        const slots = new Set(this.children.filter(child => child.parentId === component).map(child => child.slot));
        let count = 0;
        while (slots.has(count)) count++;
        return count;
    }

    clearDynamicChildren(frame: Frame, component: number) {
        const removed = new Set(this.children.filter(child => child.parentId === component).map(child => child.id));
        for (let lane = 0; lane < frame.selected.length; lane++) {
            if (removed.has(frame.selected[lane] as number)) frame.selected[lane] = undefined;
        }
        for (let i = this.children.length - 1; i >= 0; i--) {
            if (this.children[i].parentId === component) this.children.splice(i, 1);
        }
    }

    bindCallback(frame: Frame, component: number, event: RebindableEvent) {
        let signature = pop(frame.strings);
        let triggers: number[] | undefined;
        if (signature.endsWith("Y")) {
            const count = pop(frame.ints);
            if (count > 0) {
                triggers = take(frame.ints, count);
                signature = signature.slice(0, -1);
            }
        }
        const args: (number | string | bigint)[] = [];
        for (let i = signature.length - 1; i >= 0; i--) args[i] = popArgument(frame, signature[i]);
        const scriptId = pop(frame.ints);
        this.callbacks[event].set(component, scriptId === -1 ? null : { scriptId, args, triggers: event === intChanged ? triggers : undefined });
    }

    setProperty(frame: Frame, n: number, implicit: boolean, setter: PropertySetter) {
        const active = implicit ? contextLane(frame, n) : undefined;
        const component = this.ownedComponent(implicit ? frame.context : pop(frame.ints));
        const child = active === undefined ? undefined : this.children.find(entry => entry.id === active);
        const entry = child ?? this.changes.get(component) ?? { id: component };
        if (setter(this, frame, entry, component) === false) return;
        if (!child) this.changes.set(component, entry);
    }

    setOption(frame: Frame, entry: ComponentEntry, component: number): boolean {
        const slot = pop(frame.ints) - 1;
        const label = pop(frame.strings);
        if (slot < 0 || slot >= 10) return false;
        const options = entry.options ?? this.resolveLayout(component)?.component?.options ?? [];
        entry.options = Array.from({ length: Math.max(options.length, slot + 1) }, (_, index) => index === slot ? label : options[index] ?? "");
        return true;
    }
}

function popArgument(frame: Frame, type: string): number | string | bigint {
    if (type === "s") return pop(frame.strings);
    return type === "§" ? pop(frame.longs) : pop(frame.ints);
}

export class WidgetRuntime {
    private readonly definition: RuntimeDefinition;
    private snapshot: RuntimeSnapshot = {
        state: { components: [], integers: {} },
        stringVariables: new Map(),
        varps: new Map(),
        callbacks: { [pointerEvents.enter]: new Map(), [pointerEvents.exit]: new Map(), [intChanged]: new Map() },
    };

    constructor(widget: number, bundle: WidgetProgramBundle, resolveLayout?: WidgetLayoutResolver) {
        if (bundle.version !== 1) throw new Error("Unsupported RuneScript program version.");
        this.definition = {
            widget, bundle, resolveLayout,
            programs: new Map(bundle.programs.map(program => [program.id, program])),
            components: new Set(bundle.bindings.map(binding => binding.component)),
        };
    }

    run(event: PreviewEvent): { state?: WidgetPreviewState; message: string | null } {
        const execution = new Execution(this.definition, this.snapshot);
        const failure = execution.runEvent(event);
        if (failure) return { message: failure };
        this.snapshot = execution.snapshot();
        return { state: this.snapshot.state, message: null };
    }

    hoverTargets(event: HoverEvent): ReadonlyMap<number, boolean> {
        return new Map([...this.snapshot.callbacks[event]].map(([id, callback]) => [id, callback !== null]));
    }
}
