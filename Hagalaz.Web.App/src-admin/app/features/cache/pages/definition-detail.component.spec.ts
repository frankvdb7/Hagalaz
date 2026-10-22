import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { provideHttpClient, withXhr } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting, type TestRequest } from "@angular/common/http/testing";
import { ApplicationRef, signal } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { MatButtonHarness } from "@angular/material/button/testing";
import { MatDialog } from "@angular/material/dialog";
import { MatDialogHarness } from "@angular/material/dialog/testing";
import { By } from "@angular/platform-browser";
import { provideRouter } from "@angular/router";
import { RouterTestingHarness } from "@angular/router/testing";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { environment } from "../../../../environments/environment";
import { routes } from "../../../app.routes";
import { AuthStore } from "../../../core/auth/auth.store";
import { cacheDefinitionStores } from "../stores/cache-definition-stores";
import { DefinitionDetailComponent } from "./definition-detail.component";
import { SpritesManagerPageComponent } from "./sprites-manager.page";

const baseUrl = `${environment.cacheApiUrl}types/`;
const stable = () => TestBed.inject(ApplicationRef).whenStable();
const kinds = Object.keys(cacheDefinitionStores) as (keyof typeof cacheDefinitionStores)[];

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
    it("refreshes sprite detail metadata and preview after a confirmed image replacement", async () => {
        const harness = await open("sprites", { id: 7, width: 32, height: 32, frameCount: 1 });
        const editor = harness.fixture.debugElement.query(By.directive(SpritesManagerPageComponent)).componentInstance as SpritesManagerPageComponent;
        const file = new File(["png"], "new-sprite.png", { type: "image/png" });
        editor.onFileSelected({ target: { files: [file] } } as unknown as Event);
        const saving = editor.uploadSpriteImage();
        TestBed.inject(MatDialog).openDialogs[0].close(true);
        await vi.waitFor(() => {
            const upload = http.expectOne((r) => r.url === `${baseUrl}sprites/7/image` && r.method === "POST");
            expect(upload.request.body.get("file")).toBe(file);
            upload.flush({ operation: "sprite-image-replaced", type: "sprite", id: 7, width: 64, height: 48, frameCount: 1 });
        });
        await saving;
        await vi.waitFor(() =>
            http.expectOne((r) => r.url === `${baseUrl}sprites/7/image` && r.method === "GET").flush(new Blob(["new-png"], { type: "image/png" }))
        );
        await vi.waitFor(() => http.expectOne(`${baseUrl}sprites/7`).flush({ id: 7, width: 64, height: 48, frameCount: 1 }));
        await stable();
        expect(editor.store.mutationResult()?.id).toBe(7);
        const inputs = Array.from((harness.routeNativeElement as HTMLElement).querySelectorAll("input"));
        expect(inputs.some((input) => input.value === "64")).toBe(true);
    });

    it("reloads the same route ID and follows ID parameter changes", async () => {
        const harness = await open("npcs");
        const detail = harness.fixture.debugElement.query(By.directive(DefinitionDetailComponent)).componentInstance as DefinitionDetailComponent;
        detail.reload();
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
        const harness = await RouterTestingHarness.create("/cache/types/items/invalid");
        http.expectNone(() => true);
        expect(harness.routeNativeElement?.textContent).toContain("valid definition ID");
        const next = harness.navigateByUrl("/cache/types/items/404");
        await vi.waitFor(() => http.expectOne(`${baseUrl}items/404`).flush({ detail: "Not found" }, { status: 404, statusText: "Not Found" }));
        await next;
        await stable();
        harness.fixture.detectChanges();
        expect(harness.routeNativeElement?.querySelector('[role="alert"]')?.textContent).toContain("Not found");
        expect(harness.routeNativeElement?.querySelector("textarea")).toBeNull();
    });
    const edits = [
        { kind: "npcs", value: "42", path: "npcs/7/combat-level", body: { combatLevel: 42 } },
        { kind: "objects", value: "Ancient gate", path: "objects/7/name", body: { name: "Ancient gate" } },
        { kind: "varp-bits", value: "12", path: "varp-bits/7", body: { configId: 12, bitLength: 2, bitOffset: 1 } },
        { kind: "config-definitions", value: "-5", path: "config-definitions/7", body: { defaultValue: -5, valueType: "i" } },
    ];
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
