import { HttpClient, httpResource } from "@angular/common/http";
import { Component, computed, effect, inject, input, linkedSignal, signal, untracked } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { forkJoin, map, of, from, switchMap, Observable, catchError } from "rxjs";
import { describeMaterialEffect } from "../../../../../shared/model-viewer/material-effects";
import { FormsModule } from "@angular/forms";
import { type AnimationFrameData, poseModel, poseFaceAlpha } from "../../../../../shared/model-viewer/model-pose";
import { RouterLink } from "@angular/router";
import { ModelViewerComponent } from "../../../../../shared/model-viewer/model-viewer.component";
import { environment } from "../../../../environments/environment";
import { prepareCacheModel } from "../../../../../shared/model-viewer/cache-model-geometry";
import { getCacheErrorMessage } from "../stores/cache-error";
import { CacheTextureThumbnailComponent } from "./cache-texture-thumbnail.component";

import type {ModelParticle} from "../../../../../shared/model-viewer/model-particles";
import type { ModelTexture } from "../../../../../shared/model-viewer/model-scene";

interface ModelResponse { particles?: ModelParticle[]; id: number; positions: number[]; scale?: number[] | null; indices: number[]; faceColors: number[]; faceAlpha: number[]; faceSkins?: number[] | null; faceTextureIds?: number[] | null; faceTextureVertices?: number[] | null; faceTextureUvs?: number[] | null; vertexSkins: number[] | null; warnings: string[]; animationIds?: number[]; coordinateScale?: number; }
interface CacheMaterial { alphaMode: 0 | 1 | 2; colorBlend: number; brightness: number; effectId: number; effectParameter: number; effectParameter2?: number; scrollU: number; scrollV: number; repeatU: boolean; repeatV: boolean; hasHdr: boolean; }
type CacheTexture = ModelTexture & CacheMaterial & {hdrError?: boolean};
interface AnimationSequence { id: number; frameIds: number[]; frameDurations: number[]; loop: number; isTweened: boolean; }
interface ItemAppearance { key: string; label: string; modelIds: number[]; available: boolean; }

@Component({
    selector: "cache-model-preview",
    imports: [ModelViewerComponent, RouterLink, FormsModule, CacheTextureThumbnailComponent],
    template: `@if (appearances.hasValue()) {
            <label>Model appearance <select [ngModel]="appearance()" (ngModelChange)="appearance.set($event)">
                @for (choice of appearances.value(); track choice.key) { <option [value]="choice.key" [disabled]="!choice.available">{{choice.label}}{{choice.available ? '' : ' (unavailable)'}}</option> }
            </select></label>
            <p>Model IDs: @for (id of selectedAppearance()?.modelIds ?? []; track $index) { <span>#{{id}} </span> } @empty { <span>None for this appearance.</span> }</p>
        }
        @if (appearances.error(); as error) { <p role="alert">Appearance choices are unavailable. {{errorMessage(error)}}</p><button type="button" (click)="appearances.reload()">Retry appearances</button> }
        @if (model.isLoading()) { <p role="status">Loading model…</p> }
        @if (model.error(); as error) { <p role="alert">Model preview is unavailable. {{ errorMessage(error) }}</p><button type="button" (click)="model.reload()">Retry model</button> }
        @if (modelValue(); as value) { @for (warning of value.warnings; track warning) { <p class="supporting-text text-xs">{{warning}}</p> }<hagalaz-model-viewer [geometry]="geometry()" [textures]="textureImages()" />
            @if (value.animationIds?.length) { <h3>Associated animations</h3><ul>@for (animationId of value.animationIds; track animationId) { <li><button type="button" (click)="selectAnimation(animationId)">Play animation #{{animationId}}</button> <a [routerLink]="['/cache/types/animations', animationId]">Definition</a></li> }</ul> }
        }
        @if (!definition() && itemId() === null && (modelId() === null || modelId()! < 0)) { <p>No inventory model is available.</p> }
        @if (modelValue()?.vertexSkins) { <form (ngSubmit)="inspectFrame()"><label>Frame ID <input name="frame" type="number" min="0" [(ngModel)]="draftFrame" /></label><button type="submit">Inspect frame</button><button type="button" (click)="basePose()">Base pose</button></form> }
        @if (sequenceValue(); as sequence) { <p>Animation #{{sequence.id}}, frame {{frameIndex() + 1}} of {{sequence.frameIds.length}}. Discrete frames; tweening is not applied.</p><button type="button" (click)="playing.set(!playing())" [disabled]="!sequence.frameIds.length || !!poseError()">{{playing() ? 'Pause' : 'Play'}}</button><button type="button" (click)="advance()" [disabled]="playing()">Next frame</button> }
        @if (sequence.error(); as error) { <p role="alert">{{errorMessage(error)}}</p> }
        @if (clip.isLoading()) { <p role="status">Loading animation frames…</p> }
        @if (clip.error(); as error) { <p role="alert">{{errorMessage(error)}}</p><button type="button" (click)="clip.reload()">Retry animation</button> }
        @if (frame.error(); as error) { <p role="alert">{{errorMessage(error)}}</p> }
        @if (poseError(); as error) { <p role="alert">{{error}}</p> }
        @if (textureIds().length) { <h3>Referenced texture images</h3>@for (textureId of textureIds(); track textureId) { <cache-texture-thumbnail [id]="textureId" />@if (textureImages().get(textureId); as texture) { <span>Alpha: {{texture.alphaMode === 0 ? "opaque" : texture.alphaMode === 1 ? "cutout" : "blended"}}</span> } } }
        @for (warning of particleWarnings(); track warning) { <p class="supporting-text text-xs">{{warning}}</p> }
        @for (warning of materialWarnings(); track warning) { <p class="supporting-text text-xs">{{warning}}</p> }
        @if (textureData.error()) { <p role="alert">Some textures could not be loaded.</p> }
        @if (textureData.error() || hasHdrError()) { <button type="button" (click)="textureData.reload()">Retry textures</button> }
        <a routerLink="/cache/models">Open model browser</a>`,
})
export class CacheModelPreviewComponent {
    private readonly http = inject(HttpClient);
    readonly errorMessage = getCacheErrorMessage;
    readonly textureIds = computed(() => [...new Set([
        ...(this.modelValue()?.faceTextureIds ?? []),
        ...(this.modelValue()?.particles?.map(particle => particle.definition?.textureId ?? -1) ?? []),
    ].filter(id => id >= 0))]);
    readonly particleWarnings = computed(() => [...new Set((this.modelValue()?.particles ?? []).flatMap(particle => [
        ...(particle.warning ? [particle.warning] : []),
        ...(particle.definition ? [`Particle emitter #${particle.emitterId}: textured ballistic preview; HDR sprite brightness is bounded for inspection; client forces, collision, prewarm, bloom and advanced lifecycle curves are unverified.${particle.definition.unsupportedOpcodes.length ? ` Unsupported settings: ${particle.definition.unsupportedOpcodes.join(", ")}.` : ""}`] : []),
    ]))]);
    readonly textureData = rxResource({
        params: () => this.textureIds(),
        stream: ({params}) => !params.length ? of(new Map<number, CacheTexture>()) : new Observable<Map<number, CacheTexture>>(subscriber => {
            const images = new Map<number, CacheTexture>();
            let cancelled = false;
            const requests = forkJoin(params.map(id => forkJoin({
                response: this.http.get(`${environment.cacheApiUrl}textures/${id}/image`, {responseType: "blob", observe: "response"}),
                material: this.http.get<CacheMaterial>(`${environment.cacheApiUrl}textures/${id}/material`),
            }).pipe(
                switchMap(({response, material}) => {
                    if (material.alphaMode !== 0 && material.alphaMode !== 1 && material.alphaMode !== 2) throw new Error("Unsupported texture alpha mode.");
                    const bitmap = from(createImageBitmap(response.body!, {imageOrientation: "flipY"}).then(image => {
                        if (cancelled) { image.close(); return; }
                        images.set(id, {image, ...material});
                    }));
                    const hdr = material.hasHdr ? this.http.get<NonNullable<ModelTexture["hdr"]>>(`${environment.cacheApiUrl}textures/${id}/hdr`).pipe(
                        map(value => ({hdr: value, hdrError: false})),
                        catchError(() => of({hdr: undefined, hdrError: true})),
                    ) : of({hdr: undefined, hdrError: false});
                    return forkJoin({bitmap, hdr}).pipe(map(result => {
                        const texture = images.get(id);
                        if (texture) Object.assign(texture, result.hdr);
                    }));
                }),
            ))).subscribe({ next: () => subscriber.next(images), error: error => subscriber.error(error) });
            // Keep the resource stream open: its subscription owns these bitmaps until replacement/destruction.
            return () => {
                cancelled = true;
                requests.unsubscribe();
                for (const texture of images.values()) texture.image.close();
            };
        }),
    });
    readonly textureImages = computed(() => this.textureData.hasValue() ? this.textureData.value() : this.emptyTextures);
    readonly hasHdrError = computed(() => [...this.textureImages().values()].some(material => material.hdrError));
    readonly materialWarnings = computed(() => [...this.textureImages()].flatMap(([id, material]) => [
        ...(describeMaterialEffect(material) ? [`Texture #${id}: ${describeMaterialEffect(material)}`] : []),
        ...(material.hdrError ? [`Texture #${id}: HDR companion unavailable; base-image fallback.`] : []),
        ...(material.hdr ? [`Texture #${id}: HDR intensities use the inspection renderer's linear output; client tone mapping is unverified.`] : []),
    ]));
    private readonly emptyTextures = new Map<number, CacheTexture>();
    draftFrame = 0;
    readonly manualFrameId = signal<number | null>(null);
    readonly animationId = signal<number | null>(null);
    readonly frameIndex = signal(0);
    readonly playing = signal(false);
    readonly sequence = httpResource<AnimationSequence>(() => this.animationId() === null ? undefined : `${environment.cacheApiUrl}types/animations/${this.animationId()}/sequence`);
    readonly sequenceValue = computed(() => this.sequence.hasValue() ? this.sequence.value() : undefined);
    readonly clip = rxResource({
        params: () => this.sequenceValue()?.frameIds,
        stream: ({ params }) => {
            const uniqueIds = [...new Set(params)];
            if (!uniqueIds.length) return of([] as AnimationFrameData[]);
            return forkJoin(uniqueIds.map(id => this.http.get<AnimationFrameData>(`${environment.cacheApiUrl}animation-frames/${id}`))).pipe(
                map(frames => params.map(id => frames[uniqueIds.indexOf(id)])),
            );
        },
    });
    readonly clipValue = computed(() => this.clip.hasValue() ? this.clip.value() : undefined);
    readonly frameId = computed(() => this.manualFrameId());
    readonly frame = httpResource<AnimationFrameData>(() => this.frameId() === null ? undefined : `${environment.cacheApiUrl}animation-frames/${this.frameId()}`);
    readonly frameValue = computed(() => this.frame.hasValue() ? this.frame.value() : undefined);
    readonly posed = computed(() => {
        const model = this.modelValue();
        const frame = this.animationId() !== null ? this.clipValue()?.[this.frameIndex()] : this.frameValue();
        if (!model || !frame || !model.vertexSkins) return { model, error: null };
        try { return { model: { ...model, positions: poseModel(model.positions, model.vertexSkins, frame, model.coordinateScale ?? 1), faceAlpha: poseFaceAlpha(model.faceAlpha, model.faceSkins, frame) }, error: null }; }
        catch (error) { return { model, error: error instanceof Error ? error.message : "Frame could not be applied." }; }
    });
    readonly poseError = computed(() => this.posed().error);
    readonly modelId = input<number | null>(null);
    readonly itemId = input<number | null>(null);
    readonly appearance = linkedSignal(() => { this.itemId(); return "inventory"; });
    readonly appearances = httpResource<ItemAppearance[]>(() => {
        const id = this.itemId();
        return id === null || id < 0 ? undefined : `${environment.cacheApiUrl}types/items/${id}/model-appearances`;
    });
    readonly selectedAppearance = computed(() => this.appearances.hasValue() ? this.appearances.value().find(choice => choice.key === this.appearance()) : undefined);
    readonly definition = input<{ kind: "npcs" | "objects"; id: number; shape?: number } | null>(null);
    readonly model = httpResource<ModelResponse>(() => {
        const id = this.modelId();
        const definition = this.definition();
        if (definition && definition.id >= 0) return `${environment.cacheApiUrl}types/${definition.kind}/${definition.id}/model${definition.shape === undefined ? "" : `?shape=${definition.shape}`}`;
        const itemId = this.itemId();
        if (itemId !== null && itemId >= 0) return `${environment.cacheApiUrl}types/items/${itemId}/model${this.appearance() === "inventory" ? "" : `?appearance=${this.appearance()}`}`;
        return id === null || id < 0 ? undefined : `${environment.cacheApiUrl}models/${id}`;
    });
    readonly modelValue = computed(() => this.model.hasValue() ? this.model.value() : undefined);
    readonly geometry = computed(() => {
        const value = this.posed().model;
        if (!value) return null;
        return prepareCacheModel(value, this.textureImages());
    });

    constructor() {
        effect((cleanup) => {
            const sequence = this.sequenceValue();
            if (!this.playing() || !sequence || !this.clipValue()?.length || this.clip.isLoading() || this.poseError()) return;
            const duration = sequence.frameDurations[this.frameIndex()] ?? 1;
            const timer = setTimeout(() => this.advance(), Math.max(duration, 1) * 20);
            cleanup(() => clearTimeout(timer));
        });
        effect(() => { this.modelId(); this.itemId(); this.definition(); this.appearance(); this.basePose(); });
        effect(() => {
            const model = this.modelValue();
            if (!model) return;
            untracked(() => {
                this.basePose();
                const firstAnimation = model.animationIds?.[0];
                if (model.vertexSkins && firstAnimation !== undefined) this.selectAnimation(firstAnimation);
            });
        });
    }

    selectAnimation(id: number): void { this.frameIndex.set(0); this.manualFrameId.set(null); this.animationId.set(id); this.playing.set(true); }
    inspectFrame(): void { this.basePose(); if (Number.isSafeInteger(this.draftFrame) && this.draftFrame >= 0) this.manualFrameId.set(this.draftFrame); }
    basePose(): void { this.playing.set(false); this.animationId.set(null); this.manualFrameId.set(null); this.frameIndex.set(0); }
    advance(): void {
        const sequence = this.sequenceValue();
        if (!sequence?.frameIds.length) return;
        const next = this.frameIndex() + 1;
        if (next < sequence.frameIds.length) this.frameIndex.set(next);
        else if (sequence.loop > 0 && sequence.loop <= sequence.frameIds.length) this.frameIndex.set(sequence.frameIds.length - sequence.loop);
        else this.frameIndex.set(0);
    }
}
