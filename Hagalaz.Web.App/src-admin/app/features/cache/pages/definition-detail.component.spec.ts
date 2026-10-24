import { Location } from "@angular/common";
import { provideLocationMocks } from "@angular/common/testing";
import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from "@angular/common/http/testing";
import { ApplicationRef, signal } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { MatButtonHarness } from "@angular/material/button/testing";
import { MatDialog } from "@angular/material/dialog";
import { MatDialogHarness } from "@angular/material/dialog/testing";
import { By } from "@angular/platform-browser";
import { provideRouter, Router } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { routes } from "../../../app.routes";
import { AuthStore } from "../../../core/auth/auth.store";
import { cacheTypeNames } from "../services/cache.models";
import { cacheDefinitionStores } from "../stores/cache-definition-stores";
import { SpriteInspectorComponent } from "./sprite-inspector.component";
import { SpritesManagerPageComponent } from "./sprites-manager.page";
import { DefinitionDetailComponent } from "./definition-detail.component";

const baseUrl = `${environment.cacheApiUrl}types/`;
const stable = () => TestBed.inject(ApplicationRef).whenStable();
const kinds = Object.keys(cacheDefinitionStores).filter((kind) => kind !== "sprites" && kind !== "items") as (keyof typeof cacheDefinitionStores)[];

describe("dedicated cache detail routes", () => {
    let http: HttpTestingController;
    beforeEach(() => {
        vi.stubGlobal(
            "URL",
            class extends URL {
                static override createObjectURL = vi.fn(() => "blob:sprite-preview");
                static override revokeObjectURL = vi.fn();
            }
        );
        TestBed.configureTestingModule({
            providers: [
                provideRouter(routes),
                provideLocationMocks(),
                provideHttpClient(withXhr()),
                provideHttpClientTesting(),
                {
                    provide: AuthStore,
                    useValue: { authenticated: signal(true), isAuthorizedAdmin: signal(true), user: signal({ preferred_username: "Administrator" }) },
                },
            ],
        });
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => {
        TestBed.inject(MatDialog).closeAll();
        http.verify();
        vi.unstubAllGlobals();
    });
    async function open(
        kind: string,
        data: Record<string, unknown> | number = {
            id: 7,
            name: "Definition",
            combatLevel: 2,
            configId: 3,
            bitLength: 2,
            bitOffset: 1,
            defaultValue: 0,
            valueType: "i",
        }
    ) {
        const opening = RouterTestingHarness.create(`/cache/types/${kind}/7`);
        await vi.waitFor(() => http.expectOne(`${baseUrl}${kind}/7`).flush(data));
        const harness = await opening;
        if (kind === "sprites") {
            await vi.waitFor(() => http.expectOne((r) => r.url === `${baseUrl}sprites/7/image`).flush(new Blob(["png"], { type: "image/png" })));
        }
        await stable();
        harness.fixture.detectChanges();
        return harness;
    }
    it.each(kinds)("loads the dedicated guarded %s page and its own definition", async (kind) => {
        const harness = await open(
            kind,
            kind === "cs2-int" ? 0 : { id: 7, name: `${kind} record`, combatLevel: 2, configId: 3, bitLength: 2, bitOffset: 1, defaultValue: 0, valueType: "i" }
        );
        const element = harness.routeNativeElement as HTMLElement;
        expect(element.querySelector(`cache-${kind}-detail-page`)).not.toBeNull();
        expect(element.querySelectorAll("mat-toolbar cache-definition-picker")).toHaveLength(1);
        expect(element.querySelector("mat-toolbar cache-definition-picker button")?.getAttribute("aria-label")).toContain(`Choose ${cacheTypeNames[kind]}.`);
        expect(element.querySelector(`a[href="/cache/types?kind=${kind}"]`)).not.toBeNull();
        expect(element.textContent).toContain(
            kind === "sprites"
                ? "Image replacement supported"
                : ["npcs", "objects", "varp-bits", "config-definitions"].includes(kind)
                  ? "Supported edits"
                  : "Read only"
        );
        expect(element.querySelector("input[readonly]")?.getAttribute("value") ?? (element.querySelector("input[readonly]") as HTMLInputElement)?.value).toBe(
            "7"
        );
        if (kind === "cs2-int") expect((element.querySelector("textarea") as HTMLTextAreaElement).value).toBe("0");
    });
    it("reloads the same route ID and follows ID parameter changes", async () => {
        const harness = await open("npcs");
        const detail = harness.fixture.debugElement.query(By.directive(DefinitionDetailComponent)).componentInstance as DefinitionDetailComponent;
        await detail.reload();
        TestBed.tick();
        http.expectOne(`${baseUrl}npcs/7`).flush({ id: 7, name: "Reloaded", combatLevel: 12 });
        await stable();
        expect((harness.routeNativeElement as HTMLElement).querySelector<HTMLInputElement>("cache-npcs-editor input")?.value).toBe("12");
        const next = harness.navigateByUrl("/cache/types/npcs/8");
        await vi.waitFor(() => http.expectOne(`${baseUrl}npcs/8`).flush({ id: 8, name: "New NPC", combatLevel: 3 }));
        await next;
        await stable();
        harness.fixture.detectChanges();
        expect(harness.routeNativeElement?.textContent).toContain("New NPC");
        expect(harness.routeNativeElement?.textContent).not.toContain("Reloaded");
    });
    it("rejects invalid route IDs and displays API errors", async () => {
        const harness = await RouterTestingHarness.create("/cache/types/npcs/invalid");
        http.expectNone(() => true);
        expect(harness.routeNativeElement?.textContent).toContain("valid definition ID");
        const next = harness.navigateByUrl("/cache/types/npcs/404");
        await vi.waitFor(() => http.expectOne(`${baseUrl}npcs/404`).flush({ detail: "Not found" }, { status: 404, statusText: "Not Found" }));
        await next;
        await stable();
        harness.fixture.detectChanges();
        expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain("Not found");
        expect(harness.routeNativeElement?.querySelector("textarea")).toBeNull();
    });

    it("replaces the breadcrumb when switching types and clears it outside details", async () => {
        const harness = await open("npcs", { id: 7, name: "Goblin", combatLevel: 2 });
        expect(harness.routeNativeElement?.querySelector("mat-toolbar cache-definition-picker")?.textContent).toContain("Goblin #7");
        const next = harness.navigateByUrl("/cache/types/objects/0");
        await vi.waitFor(() => http.expectOne(`${baseUrl}objects/0`).flush({ id: 0, name: "Gate" }));
        await next;
        await stable();
        expect(harness.routeNativeElement?.querySelectorAll("mat-toolbar cache-definition-picker")).toHaveLength(1);
        expect(harness.routeNativeElement?.querySelector("mat-toolbar cache-definition-picker button")?.getAttribute("aria-label")).toBe(
            "Choose Object. Gate #0"
        );
        expect(harness.routeNativeElement?.querySelector('mat-toolbar a[href="/cache/types?kind=objects"]')).not.toBeNull();
        await harness.navigateByUrl("/overview");
        await stable();
        expect(harness.routeNativeElement?.querySelector("mat-toolbar cache-definition-picker")).toBeNull();
    });
    const edits = [
        { kind: "npcs", value: "42", path: "npcs/7/combat-level", body: { combatLevel: 42 } },
        { kind: "objects", value: "Ancient gate", path: "objects/7/name", body: { name: "Ancient gate" } },
        { kind: "varp-bits", value: "12", path: "varp-bits/7", body: { configId: 12, bitLength: 2, bitOffset: 1 } },
        { kind: "config-definitions", value: "-5", path: "config-definitions/7", body: { defaultValue: -5, valueType: "i" } },
    ];
    function edit(harness: RouterTestingHarness, kind: string, value: string) {
        const input = harness.routeNativeElement?.querySelector(`cache-${kind}-editor input`) as HTMLInputElement;
        input.value = value;
        input.dispatchEvent(new Event("input", { bubbles: true }));
        harness.fixture.detectChanges();
        return input;
    }
    async function answerDiscard(discard: boolean) {
        await vi.waitFor(() => expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(1));
        const buttons = Array.from(document.querySelectorAll<HTMLButtonElement>("mat-dialog-actions button"));
        const button = buttons.find((button) => button.textContent?.trim() === (discard ? "Discard changes" : "Keep editing"));
        expect(button).toBeDefined();
        button?.click();
    }
    it.each(edits)("protects dirty $kind values and preserves cancelled navigation", async ({ kind, value }) => {
        const harness = await open(kind);
        const input = edit(harness, kind, value);
        const navigation = TestBed.inject(Router).navigateByUrl("/overview");
        await answerDiscard(false);
        expect(await navigation).toBe(false);
        expect(TestBed.inject(Router).url).toBe(`/cache/types/${kind}/7`);
        expect(input.value).toBe(value);
        http.expectNone(() => true);
        const leaving = TestBed.inject(Router).navigateByUrl("/overview");
        await answerDiscard(true);
        expect(await leaving).toBe(true);
        expect(TestBed.inject(Router).url).toBe("/overview");
        http.expectNone(() => true);
    });
    it("protects reused NPC IDs through the breadcrumb picker", async () => {
        const harness = await open("npcs");
        const input = edit(harness, "npcs", "99");
        const picker = harness.routeNativeElement?.querySelector("mat-toolbar cache-definition-picker button") as HTMLButtonElement;
        picker.click();
        harness.fixture.detectChanges();
        const detailPicker = harness.fixture.debugElement.query(By.directive(DefinitionDetailComponent));
        expect(detailPicker).not.toBeNull();
        const navigation = TestBed.inject(Router).navigateByUrl("/cache/types/npcs/8");
        await answerDiscard(false);
        expect(await navigation).toBe(false);
        expect(input.value).toBe("99");
        expect(TestBed.inject(Router).url).toBe("/cache/types/npcs/7");
        http.expectNone(`${baseUrl}npcs/8`);
        const leaving = TestBed.inject(Router).navigateByUrl("/cache/types/npcs/8");
        await answerDiscard(true);
        await vi.waitFor(() => http.expectOne(`${baseUrl}npcs/8`).flush({ id: 8, name: "New NPC", combatLevel: 3 }));
        expect(await leaving).toBe(true);
        await stable();
        expect(harness.routeNativeElement?.querySelector<HTMLInputElement>("cache-npcs-editor input")?.value).toBe("3");
    });
    it("cancels browser Back without losing the dirty definition", async () => {
        const harness = await open("npcs");
        const router = TestBed.inject(Router);
        router.setUpLocationChangeListener();
        const next = router.navigateByUrl("/cache/types/npcs/8");
        await vi.waitFor(() => http.expectOne(`${baseUrl}npcs/8`).flush({ id: 8, name: "New NPC", combatLevel: 3 }));
        await next;
        await stable();
        const input = edit(harness, "npcs", "99");
        TestBed.inject(Location).back();
        await answerDiscard(false);
        await vi.waitFor(() => expect(router.currentNavigation()).toBeNull());
        expect(router.url).toBe("/cache/types/npcs/8");
        expect(input.value).toBe("99");
        http.expectNone(`${baseUrl}npcs/7`);
    });
    it("protects unsaved input from the definition Reload action", async () => {
        const harness = await open("npcs");
        const input = edit(harness, "npcs", "99");
        const detail = harness.fixture.debugElement.query(By.directive(DefinitionDetailComponent)).componentInstance as DefinitionDetailComponent;
        const reload = detail.reload();
        await answerDiscard(false);
        await reload;
        expect(input.value).toBe("99");
        http.expectNone(`${baseUrl}npcs/7`);
    });
    it("reuses one discard dialog when a second navigation supersedes the first", async () => {
        const harness = await open("npcs");
        edit(harness, "npcs", "99");
        const router = TestBed.inject(Router);
        const first = router.navigateByUrl("/overview");
        await vi.waitFor(() => expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(1));
        const second = router.navigateByUrl("/cache/types/objects/7");
        await vi.waitFor(() => expect(router.currentNavigation()?.finalUrl?.toString()).toBe("/cache/types/objects/7"));
        await answerDiscard(false);
        expect(await first).toBe(false);
        expect(await second).toBe(false);
        expect(router.url).toBe("/cache/types/npcs/7");
        http.expectNone(() => true);
    });
    it("warns on native unload for edits but allows clean unload", async () => {
        const harness = await open("npcs");
        const clean = new Event("beforeunload", { cancelable: true });
        window.dispatchEvent(clean);
        expect(clean.defaultPrevented).toBe(false);
        edit(harness, "npcs", "99");
        const dirty = new Event("beforeunload", { cancelable: true });
        window.dispatchEvent(dirty);
        expect(dirty.defaultPrevented).toBe(true);
    });
    it.each(edits)("preserves failed $kind writes and blocks departure while saving", async ({ kind, value, path }) => {
        const harness = await open(kind);
        const input = edit(harness, kind, value);
        const submit = harness.routeNativeElement?.querySelector(`cache-${kind}-editor button[type=submit]`) as HTMLButtonElement;
        submit.click();
        await vi.waitFor(() => expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(1));
        TestBed.inject(MatDialog).openDialogs[0].close(true);
        let request!: TestRequest;
        await vi.waitFor(() => {
            request = http.expectOne(`${baseUrl}${path}`);
        });
        expect(await TestBed.inject(Router).navigateByUrl("/overview")).toBe(false);
        request.flush({ detail: "Update failed" }, { status: 503, statusText: "Unavailable" });
        await stable();
        expect(input.value).toBe(value);
        expect(harness.routeNativeElement?.textContent).toContain("Update failed");
        const leaving = TestBed.inject(Router).navigateByUrl("/overview");
        await answerDiscard(false);
        expect(await leaving).toBe(false);
        expect(input.value).toBe(value);
    });
    it.each(["studio", "legacy"])("protects a staged PNG in the %s sprite workflow", async (workflow) => {
        const harness =
            workflow === "studio"
                ? await open("sprites", { id: 7, width: 32, height: 32, frameCount: 1 })
                : await RouterTestingHarness.create("/cache/sprites");
        const component =
            workflow === "studio"
                ? (harness.fixture.debugElement.query(By.directive(SpriteInspectorComponent)).componentInstance as SpriteInspectorComponent)
                : (harness.fixture.debugElement.query(By.directive(SpritesManagerPageComponent)).componentInstance as SpritesManagerPageComponent);
        const file = new File(["png"], "replacement.png", { type: "image/png" });
        component.onFileSelected({ target: { files: [file], value: "" } } as unknown as Event);
        TestBed.tick();
        const leaving = TestBed.inject(Router).navigateByUrl("/overview");
        await answerDiscard(false);
        expect(await leaving).toBe(false);
        expect(component.selectedFile()).toBe(file);
        const discard = TestBed.inject(Router).navigateByUrl("/overview");
        await answerDiscard(true);
        expect(await discard).toBe(true);
        http.expectNone((request) => request.method !== "GET");
    });
    it.each(edits)("validates and confirms the dedicated $kind editor before saving", async ({ kind, value, path, body }) => {
        const harness = await open(kind);
        const element = harness.routeNativeElement as HTMLElement;
        const editor = element.querySelector(`cache-${kind}-editor`) as HTMLElement;
        const input = editor.querySelector("input") as HTMLInputElement;
        input.value = value;
        input.dispatchEvent(new Event("input", { bubbles: true }));
        harness.fixture.detectChanges();
        (editor.querySelector('button[type="submit"]') as HTMLButtonElement).click();
        await stable();
        http.expectNone(`${baseUrl}${path}`);
        const dialog = await TestbedHarnessEnvironment.documentRootLoader(harness.fixture).getHarness(MatDialogHarness);
        const confirm = await dialog.getHarness(MatButtonHarness.with({ text: "Confirm update" }));
        const clicking = confirm.click();
        let request: TestRequest | undefined;
        await vi.waitFor(() => {
            request = http.expectOne(`${baseUrl}${path}`);
        });
        expect(request?.request.body).toEqual(body);
        request?.flush({ operation: "updated", type: kind, id: 7 });
        await clicking;
        await stable();
        expect(element.querySelector('[role="status"]')?.textContent).toContain(`Saved changes to ${kind} #7`);
        expect(await TestBed.inject(Router).navigateByUrl("/overview")).toBe(true);
        expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(0);
    });
    it.each([
        { kind: "npcs", field: 0, value: "-1" },
        { kind: "objects", field: 0, value: "" },
        { kind: "varp-bits", field: 0, value: "-1" },
        { kind: "config-definitions", field: 1, value: "too long" },
    ])("blocks invalid $kind editor values without a request or dialog", async ({ kind, field, value }) => {
        const harness = await open(kind);
        const editor = (harness.routeNativeElement as HTMLElement).querySelector(`cache-${kind}-editor`) as HTMLElement;
        const input = editor.querySelectorAll("input")[field];
        input.value = value;
        input.dispatchEvent(new Event("input", { bubbles: true }));
        harness.fixture.detectChanges();
        expect((editor.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBe(true);
        expect(TestBed.inject(MatDialog).openDialogs).toHaveLength(0);
        http.expectNone(() => true);
    });

    it("blocks invalid edits and permits cancelling confirmation", async () => {
        const harness = await open("npcs");
        const editor = harness.routeNativeElement?.querySelector("cache-npcs-editor") as HTMLElement;
        const input = editor.querySelector("input") as HTMLInputElement;
        input.value = "-1";
        input.dispatchEvent(new Event("input", { bubbles: true }));
        harness.fixture.detectChanges();
        expect((editor.querySelector('button[type="submit"]') as HTMLButtonElement).disabled).toBe(true);
        http.expectNone(() => true);
        input.value = "42";
        input.dispatchEvent(new Event("input", { bubbles: true }));
        harness.fixture.detectChanges();
        (editor.querySelector('button[type="submit"]') as HTMLButtonElement).click();
        await stable();
        const dialog = await TestbedHarnessEnvironment.documentRootLoader(harness.fixture).getHarness(MatDialogHarness);
        await (await dialog.getHarness(MatButtonHarness.with({ text: "Cancel" }))).click();
        http.expectNone(() => true);
    });
});
