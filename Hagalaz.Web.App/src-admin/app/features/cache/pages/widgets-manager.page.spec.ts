import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { Location } from "@angular/common";
import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { provideLocationMocks } from "@angular/common/testing";
import { TestBed } from "@angular/core/testing";
import { MatPaginatorHarness } from "@angular/material/paginator/testing";
import { MatCheckboxHarness } from "@angular/material/checkbox/testing";
import { MatSelectHarness } from "@angular/material/select/testing";
import { MatTabGroupHarness } from "@angular/material/tabs/testing";
import { Component } from "@angular/core";
import { provideRouter, Router } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { firstValueFrom, take, toArray } from "rxjs";
import { Vector3 } from "three";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { routes } from "../../../app.routes";
import type { WidgetCallback } from "../services/cache-widgets.service";
import { WidgetLiveModelsService } from "./details/widget-live-models.service";
import { groupWidgetModelDraws, widgetModelCamera, type WidgetModelDraw } from "./details/widget-model-scene";

const widgetRoute = routes
    .find((route) => route.path === "")
    ?.children?.find((route) => route.path === "cache")
    ?.children?.find((route) => route.path === "widgets");
const referenceTableUrl = `${environment.cacheApiUrl}indexes/3/reference-table`;
const entries = Array.from({ length: 26 }, (_, index) => ({
    fileId: (25 - index) * 2,
    children: [
        { childId: 9, index: 1 },
        { childId: 2, index: 0 },
    ],
}));
const archiveUrl = (id: number) => `${environment.cacheApiUrl}widgets/${id}/components`;

function component(type: number, parent: number, data: number[]) {
    const header = [255, type, 0, 0, 0, 10, 0, 15, 0, 80, 0, 40, 0, 0, 0, 0, parent >> 8, parent & 255, 0];
    return btoa(String.fromCharCode(...header, ...data, 0, 0, 0, 0, 0, 0));
}

const archive = {
    entries: [
        { subFileId: 0, dataBase64: component(0, 65535, [0, 0, 0, 0, 0]), callbacks: null as WidgetCallback[] | null },
        { subFileId: 1, dataBase64: component(3, 2, [0, 0, 128, 255, 1, 0]), callbacks: null as WidgetCallback[] | null },
    ],
};

@Component({ template: "" })
class Cs2RouteBoundary {}

describe("Widget browser", () => {
    beforeEach(() => {
        if (!widgetRoute) throw new Error("Widgets route is missing");
        TestBed.configureTestingModule({
            providers: [
                provideHttpClient(), provideHttpClientTesting(), provideLocationMocks(),
                provideRouter([{ path: "cache", children: [widgetRoute, { path: "types/cs2/:id", component: Cs2RouteBoundary }] }]),
            ],
        });
    });

    afterEach(() => {
        vi.restoreAllMocks();
        TestBed.inject(HttpTestingController).verify();
    });

    async function open(url: string) {
        const harness = await RouterTestingHarness.create();
        const navigation = harness.navigateByUrl(url);
        await vi.waitFor(() => {
            TestBed.tick();
            TestBed.inject(HttpTestingController).expectOne(referenceTableUrl).flush({ entries });
        });
        await navigation;
        harness.detectChanges();
        return harness;
    }

    async function loadArchive(id: number, harness: RouterTestingHarness, fixture = archive) {
        await vi.waitFor(() => {
            TestBed.tick();
            TestBed.inject(HttpTestingController).expectOne(archiveUrl(id)).flush(fixture);
        });
        harness.detectChanges();
    }

    it("paginates actual widget IDs and restores the page after inspecting a widget", async () => {
        const harness = await open("/cache/widgets");
        const root = harness.fixture.nativeElement as HTMLElement;
        expect(root.querySelectorAll("tbody tr")).toHaveLength(10);
        expect(root.querySelector('a[aria-label="Inspect widget 1"]')).toBeNull();
        root.querySelector<HTMLButtonElement>('button[aria-label="Last page"]')?.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(root.querySelectorAll("tbody tr")).toHaveLength(6);
        });

        root.querySelector<HTMLAnchorElement>('a[aria-label="Inspect widget 50"]')?.click();
        await loadArchive(50, harness);
        expect(TestBed.inject(Router).url).toBe("/cache/widgets/50?page=2");
        expect(Array.from(root.querySelectorAll(".tree-label-text"), (label) => label.textContent?.trim())).toEqual(["Container (2)", "Rectangle (9)"]);
        TestBed.inject(HttpTestingController).expectNone(referenceTableUrl);

        root.querySelector<HTMLAnchorElement>('a[href="/cache/widgets?page=2"]')?.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(root.querySelector('a[aria-label="Inspect widget 50"]')).not.toBeNull();
        });
        expect(root.querySelectorAll("tbody tr")).toHaveLength(6);
    });

    it("shows a missing-widget message for a direct URL to a gap in the cache", async () => {
        const harness = await open("/cache/widgets/1");
        expect(harness.fixture.nativeElement.textContent).toContain("Widget 1 not found.");
    });

    it("applies open and click effects and preserves them after a filtered manual callback", async () => {
        const harness = await open("/cache/widgets/0?component=9");
        await loadArchive(0, harness, {
            entries: [archive.entries[0], { ...archive.entries[1], callbacks: [
                { event: "CS2 integer changed", scriptId: 279, args: [], triggers: [1971] },
                { event: "Mouse pressed", scriptId: 279, args: [], triggers: [] },
                { event: "Mouse released", scriptId: 279, args: [], triggers: [] },
            ] }],
        });
        const root = harness.fixture.nativeElement as HTMLElement;
        const http = TestBed.inject(HttpTestingController);
        const url = `${environment.cacheApiUrl}widgets/0/program`;
        const button = (label: string) => root.querySelector<HTMLButtonElement>(`.preview-toolbar button[aria-label="${label}"]`)!;
        expect(root.querySelector(".preview-toolbar > button:last-child")).toBe(button("Play preview"));
        button("Play preview").click();
        const opening = http.expectOne(url);
        expect(opening.request.method).toBe("GET");
        const callback = (event: string, scriptId: number, triggers: number[] = []) => ({ event, scriptId, args: [], triggers });
        const program = (id: number, x: number) => ({
            id, arguments: [0, 0, 0], locals: [0, 0, 0], switches: [],
            instructions: [x, 15, 0, 0].map(value => ({ operation: "pushInt", operand: String(value) }))
                .concat({ operation: "setContextPosition", operand: "0" }, { operation: "return", operand: "0" }),
        });
        opening.flush({ version: 1, bindings: [{ component: 9, callbacks: [
            callback("Open", 1), callback("Mouse pressed", 2), callback("Mouse released", 3), callback("CS2 integer changed", 4, [1971]),
        ] }], programs: [program(1, 20), program(2, 25), program(3, 30), program(4, 35)] });
        harness.detectChanges();
        const layer = () => root.querySelector<HTMLButtonElement>('[aria-label="Activate Rectangle (9)"]')!;
        expect(layer().classList.contains("inspect-layer")).toBe(false);
        expect(layer().classList.contains("selected")).toBe(false);
        expect(layer().style.left).toBe("30px");
        layer().dispatchEvent(new MouseEvent("pointerdown", { button: 0 }));
        harness.detectChanges();
        expect(layer().style.left).toBe("35px");
        layer().dispatchEvent(new MouseEvent("pointerup", { button: 0 }));
        harness.detectChanges();
        expect(layer().style.left).toBe("40px");
        http.expectNone(request => request.method === "POST");
        const inspector = await TestbedHarnessEnvironment.loader(harness.fixture).getHarness(MatTabGroupHarness.with({ selector: ".inspector-panel mat-tab-group" }));
        root.querySelector<HTMLButtonElement>('[data-tree-component="9"] .tree-label')!.click();
        harness.detectChanges();
        await inspector.selectTab({ label: "Logic" });
        await vi.waitFor(() => expect(root.querySelector('input[aria-label="Trigger ID"]')).not.toBeNull());
        const trigger = root.querySelector<HTMLInputElement>('input[aria-label="Trigger ID"]')!;
        trigger.value = "7";
        trigger.form!.dispatchEvent(new Event("submit", { cancelable: true }));
        harness.detectChanges();
        expect(root.textContent).toContain("trigger did not match.");
        expect(layer().style.left).toBe("40px");
        trigger.value = "1971";
        trigger.form!.dispatchEvent(new Event("submit", { cancelable: true }));
        harness.detectChanges();
        expect(layer().style.left).toBe("45px");
        http.expectNone(request => request.method === "POST");
        button("Stop preview").click();
        harness.detectChanges();
        const inspectionLayer = root.querySelector<HTMLButtonElement>('[aria-label="Select Rectangle (9)"]');
        expect(inspectionLayer?.style.left).toBe("20px");
        expect(inspectionLayer?.classList.contains("inspect-layer")).toBe(true);
        button("Play preview").click();
        const departed = http.expectOne(url);
        await harness.navigateByUrl("/cache/widgets/2");
        await loadArchive(2, harness);
        expect(departed.cancelled).toBe(true);
    });

    it("selects linked components and restores their highlighting on browser Back", async () => {
        const harness = await open("/cache/widgets/0?component=9");
        await loadArchive(0, harness);
        const root = harness.fixture.nativeElement as HTMLElement;
        expect(root.querySelector('button[aria-label="Select Rectangle (9)"]')?.getAttribute("aria-pressed")).toBe("true");
        expect(root.querySelector('[data-tree-component="9"].selected')).not.toBeNull();

        await harness.navigateByUrl("/cache/widgets/0?component=2");
        expect(root.querySelector('[data-tree-component="2"].selected')).not.toBeNull();
        await harness.navigateByUrl("/cache/widgets/0?component=111");
        expect(root.textContent).toContain("Component 111 not found.");
        expect(root.querySelector(".tree-row.selected")).toBeNull();
        expect(root.querySelector('a[href="/cache/widgets"]')).not.toBeNull();
        TestBed.inject(HttpTestingController).expectNone(archiveUrl(0));

        TestBed.inject(Router).setUpLocationChangeListener();
        TestBed.inject(Location).back();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(TestBed.inject(Router).url).toBe("/cache/widgets/0?component=2");
            expect(root.querySelector('[data-tree-component="2"].selected')).not.toBeNull();
        });
    });

    it.each([false, true])("inspects callbacks and follows the script link (complete second tail: %s)", async (complete) => {
        const harness = await open("/cache/widgets/0");
        await loadArchive(0, harness, {
            entries: [
                { ...archive.entries[0], callbacks: [
                    { event: "Open", scriptId: 4213, args: [{ type: "int", value: -2147483645 }], triggers: [] },
                    { event: "Context menu clicked", scriptId: 5125, args: [], triggers: [] },
                ] },
                { ...archive.entries[1], callbacks: complete ? [] : null },
            ],
        });
        const root = harness.fixture.nativeElement as HTMLElement;
        const select = (id: number) => {
            root.querySelector<HTMLButtonElement>(`[data-tree-component="${id}"] .tree-label`)!.click();
            harness.detectChanges();
        };
        select(2);
        const inspector = await TestbedHarnessEnvironment.loader(harness.fixture).getHarness(MatTabGroupHarness.with({ selector: ".inspector-panel mat-tab-group" }));
        await inspector.selectTab({ label: "Logic" });
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(root.querySelector('[aria-label="CS2 callbacks"]')).not.toBeNull();
        });
        const section = root.querySelector('[aria-label="CS2 callbacks"]')!;
        expect([...section.querySelectorAll("a")].map((link) => link.textContent?.trim())).toEqual(["Script 4213", "Script 5125"]);
        expect(section.textContent).toContain("-2147483645");
        select(9);
        expect(root.querySelector('[aria-label="CS2 callbacks"]')?.textContent).toContain(
            complete ? "No cached callbacks." : "Callback metadata is unavailable."
        );
        select(2);
        root.querySelector<HTMLAnchorElement>('a[href="/cache/types/cs2/4213"]')?.click();
        await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe("/cache/types/cs2/4213"));
    });

    it("lets users change items per page and preserves that choice through widget details", async () => {
        const harness = await open("/cache/widgets");
        const paginator = await TestbedHarnessEnvironment.loader(harness.fixture).getHarness(MatPaginatorHarness);
        await paginator.setPageSize(25);
        const root = harness.fixture.nativeElement as HTMLElement;
        expect(root.querySelectorAll("tbody tr")).toHaveLength(25);
        root.querySelector<HTMLAnchorElement>('a[aria-label="Inspect widget 0"]')?.click();
        await loadArchive(0, harness);
        expect(TestBed.inject(Router).url).toBe("/cache/widgets/0?limit=25");
        root.querySelector<HTMLAnchorElement>('a[href="/cache/widgets?limit=25"]')?.click();
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(root.querySelectorAll("tbody tr")).toHaveLength(25);
        });
        expect(await paginator.getPageSize()).toBe(25);
    });

    it("draws repeated model components with one request using their cached widget camera", async () => {
        const harness = await open("/cache/widgets");
        const root = harness.fixture.nativeElement as HTMLElement;
        const model = component(6, 65535, [20, 116, 1, 255, 233, 255, 243, 1, 154, 7, 254, 0, 0, 3, 113, 127, 255]);
        root.querySelector<HTMLAnchorElement>('a[aria-label="Inspect widget 0"]')?.click();
        await loadArchive(0, harness, {
            entries: [
                { subFileId: 0, dataBase64: model, callbacks: null },
                { subFileId: 1, dataBase64: model, callbacks: null },
            ],
        });
        const http = TestBed.inject(HttpTestingController);
        const modelUrl = `${environment.cacheApiUrl}models/5236/widget-image`;
        await vi.waitFor(() => {
            TestBed.tick();
            const request = http.expectOne(({ url }) => url.startsWith(modelUrl));
            const query = new URL(request.request.url, "https://cache.test").searchParams;
            expect([query.get("width"), query.get("height"), query.get("x"), query.get("pitch"), query.get("zoom")]).toEqual(["80", "40", "-23", "410", "881"]);
            request.flush(new Blob([], { type: "image/png" }));
        });
        harness.detectChanges();
        expect(root.querySelectorAll("cache-widget-model img")).toHaveLength(2);
        root.querySelector<HTMLButtonElement>('[aria-label="Select Model (9)"]')?.click();
        harness.detectChanges();
        expect(root.querySelector('a[href="/cache/models/5236"]')?.textContent).toBe("5236");
    });

    it("draws duplicate live models in one batch and updates the edited camera without refetching", async () => {
        vi.spyOn(WidgetLiveModelsService.prototype, "start").mockImplementation(async function (this: WidgetLiveModelsService) {
            this.available.set(true);
        });
        const enqueue = vi.spyOn(WidgetLiveModelsService.prototype, "enqueue").mockImplementation(() => {});
        const harness = await open("/cache/widgets");
        const root = harness.fixture.nativeElement as HTMLElement;
        const model = component(6, 65535, [20, 116, 1, 255, 233, 255, 243, 1, 154, 7, 254, 0, 0, 3, 113, 127, 255]);
        root.querySelector<HTMLAnchorElement>('a[aria-label="Inspect widget 0"]')?.click();
        await loadArchive(0, harness, {
            entries: [
                { subFileId: 0, dataBase64: model, callbacks: null },
                { subFileId: 1, dataBase64: model, callbacks: null },
            ],
        });
        const http = TestBed.inject(HttpTestingController);
        const path = `${environment.cacheApiUrl}models/5236`;
        await vi.waitFor(() => {
            TestBed.tick();
            http.expectOne(path).flush({ id: 5236, positions: [], indices: [], faceTextureIds: [] });
        });
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(enqueue).toHaveBeenCalledTimes(2);
        });
        const queued = new Map<HTMLCanvasElement, WidgetModelDraw>(enqueue.mock.calls);
        const groups = [...groupWidgetModelDraws(queued)];
        expect(groups.map(({ targets }) => targets.length)).toEqual([2]);

        root.querySelector<HTMLButtonElement>('[aria-label="Select Model (9)"]')?.click();
        harness.detectChanges();
        const pitch = [...root.querySelectorAll("mat-form-field")]
            .find((field) => field.querySelector("mat-label")?.textContent?.trim() === "Pitch")
            ?.querySelector("input");
        if (!pitch) throw new Error("Pitch input is missing");
        pitch.value = "512";
        pitch.dispatchEvent(new Event("input"));
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(enqueue.mock.calls.slice(2).map(([, draw]) => draw.camera?.pitch)).toEqual([512]);
        });
        http.expectNone(path);
        http.expectNone(({ url }) => url.includes("widget-image"));
    });

    it("projects a legacy widget model with the cached offset and focal length", () => {
        const { matrix, view } = widgetModelCamera(
            { flags: 1, x: -23, y: -13, z: 0, pitch: 0, yaw: 0, roll: 0, zoom: 881, baseWidth: 0, baseHeight: 0 },
            486,
            331
        );
        const projected = new Vector3(128, 0, 0).applyMatrix4(matrix).applyMatrix4(view.projectionMatrix);
        expect((projected.x + 1) * 243).toBeCloseTo(220 + (512 * 128) / (881 * 4));
        expect(((1 - projected.y) * 331) / 2).toBeCloseTo(152);
    });

    it("shares the cached animation and advances model poses for both subscribers", async () => {
        const live = TestBed.runInInjectionContext(() => new WidgetLiveModelsService());
        const http = TestBed.inject(HttpTestingController);
        const play = () => firstValueFrom(live.animation(16).pipe(take(2), toArray()));
        const first = play();
        const second = play();
        http.expectOne(`${environment.cacheApiUrl}types/animations/16/sequence`).flush({
            frameIds: [101, 102],
            frameDurations: [10, 10],
            loop: 2,
        });
        for (const id of [101, 102]) {
            await vi.waitFor(() => http.expectOne(`${environment.cacheApiUrl}animation-frames/${id}`).flush({ frame: { skeletonId: 1 } }));
        }
        expect((await first).map((pose) => pose?.id)).toEqual([101, 102]);
        expect(await second).toEqual(await first);
    });

    it("selects a sparse component from the preview and applies local layout edits", async () => {
        const harness = await open("/cache/widgets");
        const root = harness.fixture.nativeElement as HTMLElement;
        root.querySelector<HTMLAnchorElement>('a[aria-label="Inspect widget 0"]')?.click();
        await loadArchive(0, harness);
        const rectangle = root.querySelector<HTMLButtonElement>('[aria-label="Select Rectangle (9)"]');
        expect(rectangle?.style.left).toBe("20px");
        rectangle?.click();
        harness.detectChanges();
        expect(rectangle?.getAttribute("aria-pressed")).toBe("true");
        const loader = TestbedHarnessEnvironment.loader(harness.fixture);
        expect(root.querySelector('[aria-label="Preview mode"]')?.hasAttribute("hideSingleSelectionIndicator")).toBe(true);
        const zoom = await loader.getHarness(MatSelectHarness.with({ selector: ".preview-zoom mat-select" }));
        expect(await zoom.getValueText()).toBe("Fit");
        await zoom.clickOptions({ text: "150%" });
        harness.detectChanges();
        expect(root.querySelector<HTMLElement>(".preview-art")?.style.transform).toBe("scale(1.5)");
        const x = [...root.querySelectorAll(".layout-fields mat-form-field")]
            .find((field) => field.querySelector("mat-label")?.textContent?.trim() === "X")
            ?.querySelector("input");
        if (!x) throw new Error("X input is missing");
        x.value = "32";
        x.dispatchEvent(new Event("change"));
        harness.detectChanges();
        expect(rectangle?.style.left).toBe("42px");
        root.querySelector<HTMLButtonElement>('[aria-label="Reset preview and local edits"]')?.click();
        harness.detectChanges();
        expect(rectangle?.style.left).toBe("20px");
        expect(await zoom.getValueText()).toBe("Fit");
    });

    it("switches inspector tabs and updates preview visibility with Material controls", async () => {
        const harness = await open("/cache/widgets");
        const root = harness.fixture.nativeElement as HTMLElement;
        root.querySelector<HTMLAnchorElement>('a[aria-label="Inspect widget 0"]')?.click();
        await loadArchive(0, harness);
        const rectangle = root.querySelector<HTMLButtonElement>('[aria-label="Select Rectangle (9)"]');
        rectangle?.click();
        harness.detectChanges();

        const loader = TestbedHarnessEnvironment.loader(harness.fixture);
        const visible = await loader.getHarness(MatCheckboxHarness.with({ label: "Visible in preview" }));
        await visible.uncheck();
        expect(rectangle?.querySelector(".widget-rectangle")).toBeNull();
        const inspector = await loader.getHarness(MatTabGroupHarness.with({ selector: ".inspector-panel mat-tab-group" }));
        await inspector.selectTab({ label: "Logic" });
        await vi.waitFor(() => {
            harness.detectChanges();
            expect(root.textContent).toContain("Component actions");
        });
        await inspector.selectTab({ label: "Properties" });
        await vi.waitFor(async () => {
            harness.detectChanges();
            const restored = await loader.getHarness(MatCheckboxHarness.with({ label: "Visible in preview" }));
            expect(await restored.isChecked()).toBe(false);
        });
    });
});
