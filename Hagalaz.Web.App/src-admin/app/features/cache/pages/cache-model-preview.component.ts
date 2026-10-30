import { HttpClient, httpResource } from "@angular/common/http";
import { Component, computed, effect, inject, input, signal, untracked } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { forkJoin, map, of, from, switchMap } from "rxjs";
import { FormsModule } from "@angular/forms";
import { type AnimationFrameData, poseModel, poseFaceAlpha } from "../../../../../shared/model-viewer/model-pose";
import { RouterLink } from "@angular/router";
import { ModelViewerComponent } from "../../../../../shared/model-viewer/model-viewer.component";
import { environment } from "../../../../environments/environment";
import { prepareCacheModel } from "../../../../../shared/model-viewer/cache-model-geometry";
import { getCacheErrorMessage } from "../stores/cache-error";
import { CacheTextureThumbnailComponent } from "./cache-texture-thumbnail.component";

import type { ModelTexture } from "../../../../../shared/model-viewer/model-scene";

interface ModelResponse { id: number; positions: number[]; scale?: number[] | null; indices: number[]; faceColors: number[]; faceAlpha: number[]; faceSkins?: number[] | null; faceTextureIds?: number[] | null; faceTextureVertices?: number[] | null; faceTextureUvs?: number[] | null; vertexSkins: number[] | null; warnings: string[]; animationIds?: number[]; coordinateScale?: number; }
interface AnimationSequence { id: number; frameIds: number[]; frameDurations: number[]; loop: number; isTweened: boolean; }

@Component({
    selector: "cache-model-preview",
    imports: [ModelViewerComponent, RouterLink, FormsModule, CacheTextureThumbnailComponent],
    template: `@if (model.isLoading()) { <p role="status">Loading model…</p> }
        @if (model.error(); as error) { <p role="alert">Model preview is unavailable. {{ errorMessage(error) }}</p><button type="button" (click)="model.reload()">Retry model</button> }
        @if (model.value(); as value) { @for (warning of value.warnings; track warning) { <p class="supporting-text text-xs">{{warning}}</p> }<hagalaz-model-viewer [geometry]="geometry()" [textures]="textureImages()" />
            @if (value.animationIds?.length) { <h3>Associated animations</h3><ul>@for (animationId of value.animationIds; track animationId) { <li><button type="button" (click)="selectAnimation(animationId)">Play animation #{{animationId}}</button> <a [routerLink]="['/cache/types/animations', animationId]">Definition</a></li> }</ul> }
        }
        @if (!definition() && (modelId() === null || modelId()! < 0)) { <p>No inventory model is available.</p> }
        @if (model.value()?.vertexSkins) { <form (ngSubmit)="inspectFrame()"><label>Frame ID <input name="frame" type="number" min="0" [(ngModel)]="draftFrame" /></label><button type="submit">Inspect frame</button><button type="button" (click)="basePose()">Base pose</button></form> }
        @if (sequence.value(); as sequence) { <p>Animation #{{sequence.id}}, frame {{frameIndex() + 1}} of {{sequence.frameIds.length}}. Discrete frames; tweening is not applied.</p><button type="button" (click)="playing.set(!playing())" [disabled]="!sequence.frameIds.length || !!poseError()">{{playing() ? 'Pause' : 'Play'}}</button><button type="button" (click)="advance()" [disabled]="playing()">Next frame</button> }
        @if (sequence.error(); as error) { <p role="alert">{{errorMessage(error)}}</p> }
        @if (clip.isLoading()) { <p role="status">Loading animation frames…</p> }
        @if (clip.error(); as error) { <p role="alert">{{errorMessage(error)}}</p><button type="button" (click)="clip.reload()">Retry animation</button> }
        @if (frame.error(); as error) { <p role="alert">{{errorMessage(error)}}</p> }
        @if (poseError(); as error) { <p role="alert">{{error}}</p> }
        @if (textureIds().length) { <h3>Referenced texture images</h3>@for (textureId of textureIds(); track textureId) { <cache-texture-thumbnail [id]="textureId" />@if (textureImages().get(textureId); as texture) { <span>Alpha: {{texture.alphaMode === 0 ? "opaque" : texture.alphaMode === 1 ? "cutout" : "blended"}}</span> } } }
        @if (textureData.error()) { <p role="alert">Some textures could not be loaded.</p> }
        <a routerLink="/cache/models">Open model browser</a>`,
})
export class CacheModelPreviewComponent {
    private readonly http = inject(HttpClient);
    readonly errorMessage = getCacheErrorMessage;
    readonly textureIds = computed(() => [...new Set(this.model.value()?.faceTextureIds?.filter(id => id >= 0) ?? [])]);
    readonly textureData = rxResource({
        params: () => this.textureIds(),
        stream: ({params}) => params.length ? forkJoin(params.map(id => this.http.get(`${environment.cacheApiUrl}textures/${id}/image`, {responseType: "blob", observe: "response"}).pipe(
            switchMap(response => {
                const mode = Number(response.headers.get("X-Texture-Alpha-Mode"));
                if (mode !== 0 && mode !== 1 && mode !== 2) throw new Error("Unsupported texture alpha mode.");
                return from(createImageBitmap(response.body!, {imageOrientation: "flipY"})).pipe(map(image => [id, {image, alphaMode: mode, colorBlend: Number(response.headers.get("X-Texture-Color-Blend")), brightness: Number(response.headers.get("X-Texture-Brightness"))} as ModelTexture] as const));
            }),
        ))).pipe(map(entries => new Map(entries))) : of(new Map<number, ModelTexture>()),
    });
    readonly textureImages = computed(() => this.textureData.value() ?? this.emptyTextures);
    private readonly emptyTextures = new Map<number, ModelTexture>();
    draftFrame = 0;
    readonly manualFrameId = signal<number | null>(null);
    readonly animationId = signal<number | null>(null);
    readonly frameIndex = signal(0);
    readonly playing = signal(false);
    readonly sequence = httpResource<AnimationSequence>(() => this.animationId() === null ? undefined : `${environment.cacheApiUrl}types/animations/${this.animationId()}/sequence`);
    readonly clip = rxResource({
        params: () => this.sequence.value()?.frameIds,
        stream: ({ params }) => {
            const uniqueIds = [...new Set(params)];
            if (!uniqueIds.length) return of([] as AnimationFrameData[]);
            return forkJoin(uniqueIds.map(id => this.http.get<AnimationFrameData>(`${environment.cacheApiUrl}animation-frames/${id}`))).pipe(
                map(frames => params.map(id => frames[uniqueIds.indexOf(id)])),
            );
        },
    });
    readonly frameId = computed(() => this.manualFrameId());
    readonly frame = httpResource<AnimationFrameData>(() => this.frameId() === null ? undefined : `${environment.cacheApiUrl}animation-frames/${this.frameId()}`);
    readonly posed = computed(() => {
        const model = this.model.value();
        const frame = this.animationId() !== null ? this.clip.value()?.[this.frameIndex()] : this.frame.value();
        if (!model || !frame || !model.vertexSkins) return { model, error: null };
        try { return { model: { ...model, positions: poseModel(model.positions, model.vertexSkins, frame, model.coordinateScale ?? 1), faceAlpha: poseFaceAlpha(model.faceAlpha, model.faceSkins, frame) }, error: null }; }
        catch (error) { return { model, error: error instanceof Error ? error.message : "Frame could not be applied." }; }
    });
    readonly poseError = computed(() => this.posed().error);
    readonly modelId = input<number | null>(null);
    readonly itemId = input<number | null>(null);
    readonly definition = input<{ kind: "npcs" | "objects"; id: number; shape?: number } | null>(null);
    readonly model = httpResource<ModelResponse>(() => {
        const id = this.modelId();
        const definition = this.definition();
        if (definition && definition.id >= 0) return `${environment.cacheApiUrl}types/${definition.kind}/${definition.id}/model${definition.shape === undefined ? "" : `?shape=${definition.shape}`}`;
        const itemId = this.itemId();
        if (itemId !== null && itemId >= 0 && id !== null && id >= 0) return `${environment.cacheApiUrl}types/items/${itemId}/model`;
        return id === null || id < 0 ? undefined : `${environment.cacheApiUrl}models/${id}`;
    });
    readonly geometry = computed(() => {
        const value = this.posed().model;
        if (!value) return null;
        return prepareCacheModel(value, this.textureImages());
    });

    constructor() {
        effect(cleanup => {
            const images = this.textureData.value();
            cleanup(() => { for (const image of images?.values() ?? []) image.image.close(); });
        });
        effect((cleanup) => {
            const sequence = this.sequence.value();
            if (!this.playing() || !sequence || !this.clip.value()?.length || this.clip.isLoading() || this.poseError()) return;
            const duration = sequence.frameDurations[this.frameIndex()] ?? 1;
            const timer = setTimeout(() => this.advance(), Math.max(duration, 1) * 20);
            cleanup(() => clearTimeout(timer));
        });
        effect(() => { this.modelId(); this.itemId(); this.definition(); this.basePose(); });
        effect(() => {
            const model = this.model.value();
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
        const sequence = this.sequence.value();
        if (!sequence?.frameIds.length) return;
        const next = this.frameIndex() + 1;
        if (next < sequence.frameIds.length) this.frameIndex.set(next);
        else if (sequence.loop > 0 && sequence.loop <= sequence.frameIds.length) this.frameIndex.set(sequence.frameIds.length - sequence.loop);
        else this.frameIndex.set(0);
    }
}
