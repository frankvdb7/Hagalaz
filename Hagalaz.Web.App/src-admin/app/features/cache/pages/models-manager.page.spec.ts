import type { ModelGeometry, ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import { Component, input } from "@angular/core";
import {Location} from "@angular/common";
import {provideLocationMocks} from "@angular/common/testing";
import {TestbedHarnessEnvironment} from "@angular/cdk/testing/testbed";
import {provideHttpClient} from "@angular/common/http";
import {HttpTestingController, provideHttpClientTesting} from "@angular/common/http/testing";
import {TestBed} from "@angular/core/testing";
import {MatMenuHarness} from "@angular/material/menu/testing";
import {MatInputHarness} from "@angular/material/input/testing";
import {By} from "@angular/platform-browser";
import {provideRouter, Router} from "@angular/router";
import {RouterTestingHarness} from "@angular/router/testing";
import {afterEach, beforeEach, describe, expect, it, vi} from "vitest";
import {ModelViewerComponent} from "../../../../../shared/model-viewer/model-viewer.component";
import {environment} from "../../../../environments/environment";
import {ModelsManagerPageComponent} from "./models-manager.page";
import {ModelsDetailPageComponent} from "./details/models-detail.page";
import {ModelWorkspaceComponent} from "./model-workspace.component";

@Component({ selector: "hagalaz-model-viewer", template: "" })
class RendererBoundary {
    readonly height = input(360);
    readonly geometry = input<ModelGeometry | null>(null);
    readonly textures = input<ReadonlyMap<number, ModelTexture>>(new Map());
    readonly grid = input(false);
    readonly wireframe = input(false);
    readonly showReset = input(true);
    readonly particlePlaybackSpeed = input(1);
}

const baseUrl = environment.cacheApiUrl;
const model = {id:666, positions:[0,0,0,10,0,0,0,10,0], indices:[0,1,2], faceColors:[127],faceAlpha:[0],warnings:[],animationIds:[],particles:[],coordinateScale:1};

describe("Model detail routing", () => {
    let http: HttpTestingController;
    let harness: RouterTestingHarness;
    let navigation: Promise<unknown>;
    beforeEach(() => {
        TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting(),provideLocationMocks(),provideRouter([
            {path:"cache/models",component:ModelsManagerPageComponent},
            {path:"cache/models/:id",component:ModelsDetailPageComponent},
        ])]}).overrideComponent(ModelWorkspaceComponent,{remove:{imports:[ModelViewerComponent]},add:{imports:[RendererBoundary]}});
        http = TestBed.inject(HttpTestingController);
    });
    afterEach(() => { try { http.verify(); } finally { TestBed.resetTestingModule(); } });
    async function open(url: string) {
        harness = await RouterTestingHarness.create();
        TestBed.inject(Router).setUpLocationChangeListener();
        navigation = harness.navigateByUrl(url);
        await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe(url));
        harness.detectChanges(); TestBed.tick();
        if (url === "/cache/models") {
            await vi.waitFor(() => { TestBed.tick(); http.expectOne(`${baseUrl}models/capacity`).flush({capacity:1000}); });
            await vi.waitFor(() => { TestBed.tick(); http.expectOne(request => request.url === `${baseUrl}models/range`).flush([{id:0},{id:1}]); });
            await navigation; harness.detectChanges();
        }
    }
    async function loaded(id: number) {
        await vi.waitFor(() => { TestBed.tick(); http.expectOne(`${baseUrl}models/${id}`).flush({...model,id}); });
        await navigation; harness.detectChanges(); await harness.fixture.whenStable(); harness.detectChanges();
    }
    async function lookup(id: string) {
        const loader = TestbedHarnessEnvironment.loader(harness.fixture);
        await (await loader.getHarness(MatInputHarness)).setValue(id);
        (harness.fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();
        harness.detectChanges(); TestBed.tick();
    }
    function workspace(): ModelWorkspaceComponent {
        return harness.fixture.debugElement.query(By.directive(ModelWorkspaceComponent)).componentInstance;
    }

    it("opens model details from lookup and reloads the same model without adding history", async () => {
        await open("/cache/models");
        await lookup("666");
        await loaded(666);
        const renderer = harness.fixture.debugElement.query(By.directive(RendererBoundary)).componentInstance as RendererBoundary;
        expect(renderer.geometry()).toBe(workspace().workspaceGeometry());
        expect(renderer.textures()).toBe(workspace().textureImages());
        expect(renderer.height()).toBe(430);
        expect(renderer.grid()).toBe(workspace().showGrid());
        expect(renderer.wireframe()).toBe(workspace().wireframe());
        expect(renderer.showReset()).toBe(false);
        expect(renderer.particlePlaybackSpeed()).toBe(1);

        expect(TestBed.inject(Router).url).toBe("/cache/models/666?offset=0&limit=10");
        expect(workspace().modelId()).toBe(666);
        await lookup("666"); await loaded(666);
        expect(TestBed.inject(Router).url).toBe("/cache/models/666?offset=0&limit=10");
    });

    it("browses models in the shared results table and preserves pagination in detail links", async () => {
        await open("/cache/models");
        expect(harness.routeNativeElement?.querySelector('table[aria-label="Matching models"]')?.textContent).toContain("Model #0");
        expect(harness.routeNativeElement?.textContent).toContain("Find models");
        http.expectNone(request => request.url.endsWith("/archive-sizes"));
        (harness.fixture.nativeElement.querySelector('button[aria-label="Next page"]') as HTMLButtonElement).click();
        await vi.waitFor(() => { TestBed.tick(); http.expectOne(request => request.url.endsWith("/models/range") && request.params.get("startId") === "10").flush([{id:10}]); });
        harness.detectChanges(); await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/cache/models?offset=10&limit=10");
        const menu = await TestbedHarnessEnvironment.loader(harness.fixture).getHarness(MatMenuHarness.with({triggerText:"more_horiz"}));
        await menu.open();
        const item = (await menu.getItems())[0];
        expect(await item.getText()).toContain("Inspect model");
        const link = document.querySelector<HTMLAnchorElement>('a[href="/cache/models/10?offset=10&limit=10"]');
        expect(link).not.toBeNull();
        await menu.close();
    });

    it.each(["-1","1.5","abc","9007199254740992"])("rejects invalid route ID %s without a model request", async id => {
        await open(`/cache/models/${id}`);
        expect(harness.routeNativeElement?.textContent).toContain("Enter a valid model ID");
        http.expectNone(request => request.url.includes("/models/"));
    });

    it("accepts zero as a direct model detail ID", async () => {
        await open("/cache/models/0"); await loaded(0);
        expect(workspace().modelId()).toBe(0);
    });

    it("rejects invalid model searches without navigating", async () => {
        await open("/cache/models");
        const loader = TestbedHarnessEnvironment.loader(harness.fixture);
        await (await loader.getHarness(MatInputHarness)).setValue("-1");
        (harness.fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();
        harness.detectChanges();
        expect(harness.routeNativeElement?.textContent).toContain("Enter a valid");
        expect(TestBed.inject(Router).url).toBe("/cache/models");
    });

    it.each(["textures","recolors","retextures","animations","metadata"])("restores the %s tab directly and preserves the workspace during tab navigation", async tab => {
        const names = ["model","textures","recolors","retextures","animations","metadata"];
        await open(`/cache/models/666?tab=${tab}`); await loaded(666);
        const original = workspace(), transform = original.transform();
        expect(original.tab()).toBe(names.indexOf(tab));
        await harness.navigateByUrl("/cache/models/666?tab=metadata");
        expect(workspace()).toBe(original);
        expect(original.transform()).toBe(transform);
        original.tab.set(0); harness.detectChanges(); await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/cache/models/666");
        http.expectNone(`${baseUrl}models/666`);
    });

    it("restores inspection tabs through browser history without refetching geometry", async () => {
        await open("/cache/models/666"); await loaded(666);
        const original = workspace();
        original.tab.set(1); harness.detectChanges(); await harness.fixture.whenStable();
        expect(TestBed.inject(Router).url).toBe("/cache/models/666?tab=textures");
        TestBed.inject(Location).back();
        await vi.waitFor(() => { harness.detectChanges(); expect(original.tab()).toBe(0); });
        TestBed.inject(Location).forward();
        await vi.waitFor(() => { harness.detectChanges(); expect(original.tab()).toBe(1); });
        http.expectNone(`${baseUrl}models/666`);
    });

    it("uses Model for unsupported tab values", async () => {
        await open("/cache/models/666?tab=unknown"); await loaded(666);
        expect(workspace().tab()).toBe(0);
    });

    it("recovers after failed model requests and follows ID history", async () => {
        await open("/cache/models/404");
        await vi.waitFor(() => { TestBed.tick(); http.expectOne(`${baseUrl}models/404`).flush({}, {status:404,statusText:"Not Found"}); });
        harness.detectChanges();
        expect(harness.routeNativeElement?.textContent).toContain("Model preview is unavailable");
        await lookup("666"); await loaded(666);
        const location = TestBed.inject(Location);
        location.back(); await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe("/cache/models/404"));
        await loaded(404); expect(workspace().modelId()).toBe(404);
        location.forward(); await vi.waitFor(() => expect(TestBed.inject(Router).url).toBe("/cache/models/666"));
        await loaded(666); expect(workspace().modelId()).toBe(666);
    });
});
