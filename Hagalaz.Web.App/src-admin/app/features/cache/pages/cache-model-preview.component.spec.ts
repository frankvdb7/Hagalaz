import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { Component, input } from "@angular/core";
import { ComponentFixture, TestBed } from "@angular/core/testing";
import { provideRouter } from "@angular/router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { ModelViewerComponent } from "../../../../../shared/model-viewer/model-viewer.component";
import type { ModelGeometry, ModelTexture } from "../../../../../shared/model-viewer/model-scene";
import { environment } from "../../../../environments/environment";
import { CacheModelPreviewComponent } from "./cache-model-preview.component";
import { CacheTextureThumbnailComponent } from "./cache-texture-thumbnail.component";

@Component({ selector: "hagalaz-model-viewer", template: "" })
class RendererBoundary {
    readonly geometry = input<ModelGeometry | null>(null);
    readonly textures = input<ReadonlyMap<number, ModelTexture>>(new Map());
}

@Component({ selector: "cache-texture-thumbnail", template: "" })
class ThumbnailBoundary { readonly id = input.required<number>(); }

const baseUrl = environment.cacheApiUrl;
const model = { id: 7, positions: [0, 0, 0], indices: [], faceColors: [], faceAlpha: [], vertexSkins: [0], warnings: [], animationIds: [66, 67] };

describe("CacheModelPreviewComponent playback", () => {
    let http: HttpTestingController;
    let fixture: ComponentFixture<CacheModelPreviewComponent>;

    beforeEach(() => {
        vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] });
        TestBed.configureTestingModule({
            imports: [CacheModelPreviewComponent],
            providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
        }).overrideComponent(CacheModelPreviewComponent, {
            remove: { imports: [ModelViewerComponent, CacheTextureThumbnailComponent] }, add: { imports: [RendererBoundary, ThumbnailBoundary] },
        });
        http = TestBed.inject(HttpTestingController);
        fixture = TestBed.createComponent(CacheModelPreviewComponent);
        fixture.componentRef.setInput("modelId", 7);
        settle();
    });
    afterEach(() => { fixture.destroy(); http.verify(); vi.useRealTimers(); vi.restoreAllMocks(); vi.unstubAllGlobals(); });

    function settle(): void { fixture.detectChanges(); TestBed.tick(); }
    function click(label: string): void {
        const button = Array.from(fixture.nativeElement.querySelectorAll("button") as NodeListOf<HTMLButtonElement>)
            .find(value => value.textContent?.trim() === label);
        expect(button, label).toBeDefined();
        button!.click(); settle();
    }
    async function loadAnimation(): Promise<void> {
        http.expectOne(`${baseUrl}models/7`).flush(model);
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.animationId()).toBe(66); }, { interval: 1 });
        http.expectOne(`${baseUrl}types/animations/66/sequence`).flush({ id: 66, frameIds: [65536, 65537, 65538], frameDurations: [2, 3, 1], loop: 2, isTweened: false });
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.sequence.hasValue()).toBe(true); }, { interval: 1 });
        for (let index = 0; index < 3; index++)
            http.expectOne(`${baseUrl}animation-frames/${65536 + index}`).flush({
                skeleton: { types: [1], groups: [[0]] },
                frame: { skeletonId: 1, transforms: [{ slot: 0, x: index + 1, y: 0, z: 0, pivotSlot: -1, tweenFlags: 0 }] },
            });
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.clip.hasValue()).toBe(true); }, { interval: 1 });
    }

    it("autoplays, pauses, steps and loops using the sequence's frame durations", async () => {
        await loadAnimation();
        const component = fixture.componentInstance;
        expect(component.playing()).toBe(true);
        expect(component.posed().model?.positions).toEqual([1, 0, 0]);
        vi.advanceTimersByTime(40); settle();
        expect(component.frameIndex()).toBe(1);
        click("Pause");
        vi.advanceTimersByTime(400); settle();
        expect(component.frameIndex()).toBe(1);
        click("Next frame");
        expect(component.frameIndex()).toBe(2);
        expect(component.posed().model?.positions).toEqual([3, 0, 0]);
        click("Play");
        vi.advanceTimersByTime(20); settle();
        expect(component.frameIndex()).toBe(1);
        expect(component.posed().model?.positions).toEqual([2, 0, 0]);
    });

    it("switches animations and resets playback when another model is selected", async () => {
        await loadAnimation();
        click("Play animation #67");
        http.expectOne(`${baseUrl}types/animations/67/sequence`).flush({ id: 67, frameIds: [131072], frameDurations: [5], loop: -1, isTweened: false });
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.sequence.value()?.id).toBe(67); }, { interval: 1 });
        http.expectOne(`${baseUrl}animation-frames/131072`).flush({ skeleton: { types: [1], groups: [[0]] }, frame: { skeletonId: 2, transforms: [{ slot: 0, x: 12, y: 0, z: 0, pivotSlot: -1, tweenFlags: 0 }] } });
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.posed().model?.positions).toEqual([12, 0, 0]); }, { interval: 1 });
        fixture.componentRef.setInput("modelId", 8); settle();
        http.expectOne(`${baseUrl}models/8`).flush({ ...model, id: 8, vertexSkins: null, animationIds: [] });
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.model.value()?.id).toBe(8); }, { interval: 1 });
        expect(fixture.componentInstance.playing()).toBe(false);
        expect(fixture.componentInstance.animationId()).toBeNull();
        expect(fixture.componentInstance.posed().model?.positions).toEqual([0, 0, 0]);
    });

    it("retries a failed model and cancels the playback timer on destruction", async () => {
        http.expectOne(`${baseUrl}models/7`).flush({}, { status: 404, statusText: "Not Found" }); settle();
        await vi.waitFor(() => { settle(); expect(fixture.nativeElement.textContent).toContain("Model preview is unavailable"); }, { interval: 1 });
        click("Retry model"); await loadAnimation();
        const advance = vi.spyOn(fixture.componentInstance, "advance");
        fixture.destroy();
        vi.advanceTimersByTime(10000);
        expect(advance).not.toHaveBeenCalled();
    });

    it("reports a missing animation frame and resumes after retrying the clip", async () => {
        http.expectOne(`${baseUrl}models/7`).flush(model);
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.animationId()).toBe(66); }, { interval: 1 });
        http.expectOne(`${baseUrl}types/animations/66/sequence`).flush({ id: 66, frameIds: [65536], frameDurations: [2], loop: -1, isTweened: false });
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.sequence.hasValue()).toBe(true); }, { interval: 1 });
        http.expectOne(`${baseUrl}animation-frames/65536`).flush({ detail: "Frame asset is missing." }, { status: 404, statusText: "Not Found" });
        await vi.waitFor(() => { settle(); expect(fixture.nativeElement.textContent).toContain("Frame asset is missing."); }, { interval: 1 });
        expect(fixture.componentInstance.posed().model?.positions).toEqual([0, 0, 0]);
        click("Retry animation");
        http.expectOne(`${baseUrl}animation-frames/65536`).flush({ skeleton: { types: [1], groups: [[0]] }, frame: { skeletonId: 1, transforms: [{ slot: 0, x: 5, y: 0, z: 0, pivotSlot: -1, tweenFlags: 0 }] } });
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.posed().model?.positions).toEqual([5, 0, 0]); }, { interval: 1 });
        expect(fixture.componentInstance.playing()).toBe(true);
    });

    async function loadTexturedModel(ids: number[], material: Record<string, unknown> = {}): Promise<void> {
        http.expectOne(`${baseUrl}models/7`).flush({
            ...model, positions: [0, 0, 0, 10, 0, 0, 0, 10, 0], indices: ids.flatMap(() => [0, 1, 2]),
            faceColors: ids.map(() => 127), faceAlpha: ids.map(() => 0), faceTextureIds: ids,
            faceTextureUvs: ids.flatMap(() => [0, 0, 1, 0, 0, 1]), vertexSkins: null, animationIds: [],
        });
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.model.hasValue()).toBe(true); }, { interval: 1 });
        for (const id of ids)
            http.expectOne(`${baseUrl}textures/${id}/material`).flush({ alphaMode: 1, colorBlend: 0, brightness: 0, effectId: 0, effectParameter: 0, scrollU: 0, scrollV: 0, repeatU: true, repeatV: true, hasHdr: false, ...material });
    }

    it("shows equipped model IDs, selects appearances and resets to inventory for another item", async () => {
        http.expectOne(`${baseUrl}models/7`).flush({...model, vertexSkins: null, animationIds: []});
        fixture.componentRef.setInput("itemId", 20139); settle();
        const choices = [
            {key: "inventory", label: "Inventory", modelIds: [100], available: true},
            {key: "male-equipped", label: "Male equipped", modelIds: [101,102,103], available: true},
            {key: "female-equipped", label: "Female equipped", modelIds: [], available: false},
            {key: "male-head", label: "Male head", modelIds: [104,105], available: true},
            {key: "female-head", label: "Female head", modelIds: [], available: false},
        ];
        http.expectOne(`${baseUrl}types/items/20139/model-appearances`).flush(choices);
        http.expectOne(`${baseUrl}types/items/20139/model`).flush({...model, vertexSkins: null, animationIds: []});
        await vi.waitFor(() => { settle(); expect(fixture.nativeElement.textContent).toContain("#100"); }, {interval: 1});
        const select = fixture.nativeElement.querySelector("select") as HTMLSelectElement;
        expect(select.options[2].disabled).toBe(true);
        select.value = "male-equipped"; select.dispatchEvent(new Event("change")); settle();
        http.expectOne(`${baseUrl}types/items/20139/model?appearance=male-equipped`).flush({...model, positions: [4,-8,12], vertexSkins: null, animationIds: []});
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.modelValue()?.positions).toEqual([4,-8,12]); }, {interval: 1});
        expect(fixture.nativeElement.textContent).toContain("#101");
        expect(fixture.nativeElement.textContent).toContain("#102");
        expect(fixture.nativeElement.textContent).toContain("#103");
        fixture.componentRef.setInput("itemId", 20140); settle();
        http.expectOne(`${baseUrl}types/items/20140/model-appearances`).flush(choices);
        http.expectOne(`${baseUrl}types/items/20140/model`).flush({...model, vertexSkins: null, animationIds: []});
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.appearance()).toBe("inventory"); expect(fixture.nativeElement.textContent).toContain("#100"); }, {interval: 1});
    });

    it("retries failed appearance metadata while retaining the inventory preview", async () => {
        http.expectOne(`${baseUrl}models/7`).flush({...model, vertexSkins: null, animationIds: []});
        fixture.componentRef.setInput("itemId", 20139); settle();
        http.expectOne(`${baseUrl}types/items/20139/model-appearances`).flush({}, {status: 500, statusText: "Error"});
        http.expectOne(`${baseUrl}types/items/20139/model`).flush({...model, vertexSkins: null, animationIds: []});
        await vi.waitFor(() => { settle(); expect(fixture.nativeElement.textContent).toContain("Appearance choices are unavailable"); }, {interval: 1});
        click("Retry appearances");
        http.expectOne(`${baseUrl}types/items/20139/model-appearances`).flush([{key: "inventory", label: "Inventory", modelIds: [123], available: true}]);
        await vi.waitFor(() => { settle(); expect(fixture.nativeElement.textContent).toContain("#123"); }, {interval: 1});
        expect(fixture.nativeElement.textContent).not.toContain("Appearance choices are unavailable");
        expect(fixture.componentInstance.modelValue()).toBeDefined();
    });

    it("closes a bitmap that finishes decoding after the model selection changes", async () => {
        let finish!: (image: ImageBitmap) => void;
        const close = vi.fn();
        vi.stubGlobal("createImageBitmap", vi.fn(() => new Promise<ImageBitmap>(resolve => { finish = resolve; })));
        await loadTexturedModel([8]);
        http.expectOne(`${baseUrl}textures/8/image`).flush(new Blob(), { headers: { "X-Texture-Alpha-Mode": "1" } });
        fixture.componentRef.setInput("modelId", 8); settle();
        http.expectOne(`${baseUrl}models/8`).flush({ ...model, id: 8, vertexSkins: null, animationIds: [] });
        finish({close} as unknown as ImageBitmap);
        await vi.waitFor(() => { settle(); expect(close).toHaveBeenCalledTimes(1); }, { interval: 1 });
    });

    it("loads emitter-only textures and displays particle fidelity warnings", async () => {
        vi.stubGlobal("createImageBitmap",vi.fn().mockResolvedValue({close:vi.fn()}));
        const definition = {textureId:744,colorMin:0x1effffff,colorMax:0x5acccccc,lifeMin:0.05,lifeMax:0.06,rateMin:296.875,rateMax:500,sizeMin:40,sizeMax:48,speedMin:800,speedMax:1200,angles:[0,64,4080,3920],unsupportedOpcodes:[10,18]};
        http.expectOne(`${baseUrl}models/7`).flush({...model,vertexSkins:null,animationIds:[],particles:[{emitterId:403,face:0,definition,warning:null}]});
        await vi.waitFor(()=>{settle();expect(fixture.componentInstance.textureIds()).toEqual([744]);},{interval:1});
        http.expectOne(`${baseUrl}textures/744/material`).flush({alphaMode:2,effectId:0,hasHdr:false,scrollU:0,scrollV:0});
        http.expectOne(`${baseUrl}textures/744/image`).flush(new Blob());
        await vi.waitFor(()=>{settle();expect(fixture.componentInstance.textureData.hasValue()).toBe(true);},{interval:1});
        expect(fixture.componentInstance.geometry()?.particles?.[0].emitterId).toBe(403);
        expect(fixture.nativeElement.textContent).toContain("Particle emitter #403");
        expect(fixture.nativeElement.textContent).toContain("Unsupported settings: 10, 18");
        expect(fixture.nativeElement.textContent).toContain("client forces, collision, prewarm");
    });

    it("releases already decoded bitmaps when another texture fails and renders the error", async () => {
        const close = vi.fn();
        vi.stubGlobal("createImageBitmap", vi.fn().mockResolvedValue({close}));
        await loadTexturedModel([8, 9]);
        http.expectOne(`${baseUrl}textures/8/image`).flush(new Blob(), { headers: { "X-Texture-Alpha-Mode": "1" } });
        await Promise.resolve();
        http.expectOne(`${baseUrl}textures/9/image`).flush(new Blob(), { status: 404, statusText: "Not Found" });
        await vi.waitFor(() => { settle(); expect(fixture.nativeElement.textContent).toContain("Some textures could not be loaded"); }, { interval: 1 });
        expect(close).toHaveBeenCalledTimes(1);
        fixture.destroy();
        expect(close).toHaveBeenCalledTimes(1);
    });

    it("cancels a pending HDR request and releases its decoded bitmap on model replacement", async () => {
        const close = vi.fn();
        vi.stubGlobal("createImageBitmap", vi.fn().mockResolvedValue({close}));
        await loadTexturedModel([8], {hasHdr: true});
        http.expectOne(`${baseUrl}textures/8/image`).flush(new Blob());
        const hdr = http.expectOne(`${baseUrl}textures/8/hdr`);
        await Promise.resolve();
        fixture.componentRef.setInput("modelId", 9); settle();
        http.expectOne(`${baseUrl}models/9`).flush({...model, id: 9, vertexSkins: null, animationIds: []});
        await vi.waitFor(() => { settle(); expect(close).toHaveBeenCalledTimes(1); }, {interval: 1});
        expect(hdr.cancelled).toBe(true);
        expect(fixture.componentInstance.textureImages().size).toBe(0);
    });

    it("identifies approximated effects and retries a missing HDR companion", async () => {
        vi.stubGlobal("createImageBitmap", vi.fn().mockResolvedValue({close: vi.fn()}));
        await loadTexturedModel([8], {effectId: 2, effectParameter: 12, scrollU: -1, hasHdr: true});
        http.expectOne(`${baseUrl}textures/8/image`).flush(new Blob());
        http.expectOne(`${baseUrl}textures/8/hdr`).flush({}, {status: 404, statusText: "Not Found"});
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.textureData.hasValue()).toBe(true); }, {interval: 1});
        expect(fixture.nativeElement.textContent).toContain("Ordinary water #2: procedural-noise approximation");
        expect(fixture.nativeElement.textContent).toContain("HDR companion unavailable; base-image fallback.");
        expect(fixture.componentInstance.textureImages().get(8)?.scrollU).toBe(-1);
        click("Retry textures");
        http.expectOne(`${baseUrl}textures/8/material`).flush({alphaMode: 1, effectId: 2, effectParameter: 12, scrollU: -1, hasHdr: true});
        http.expectOne(`${baseUrl}textures/8/image`).flush(new Blob());
        http.expectOne(`${baseUrl}textures/8/hdr`).flush({width: 1, height: 1, rgba: [2, 1, 0, 1]});
        await vi.waitFor(() => { settle(); expect(fixture.componentInstance.textureImages().get(8)?.hdr?.rgba).toEqual([2, 1, 0, 1]); }, {interval: 1});
        expect(fixture.nativeElement.textContent).not.toContain("HDR companion unavailable");
    });
});
