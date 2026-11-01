import type { GetTextureMaterialDto, GetTextureHdrDto } from "../services/dtos";
import type { GetModelDto, GetAnimationSequenceDto, GetAnimationFrameDto, GetItemModelAppearanceDto } from "../services/dtos";
import { HttpClient, httpResource } from "@angular/common/http";
import { Component, computed, effect, inject, input, linkedSignal, signal, untracked } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { forkJoin, map, of, from, switchMap, Observable, catchError } from "rxjs";
import { describeMaterialEffect } from "../../../../../shared/model-viewer/material-effects";
import { NgTemplateOutlet } from "@angular/common";
import { MatButtonModule } from "@angular/material/button";
import { FormsModule } from "@angular/forms";
import { poseModel, poseFaceAlpha } from "../../../../../shared/model-viewer/model-pose";
import { RouterLink } from "@angular/router";
import { ModelViewerComponent } from "../../../../../shared/model-viewer/model-viewer.component";
import { environment } from "../../../../environments/environment";
import { prepareCacheModel } from "../../../../../shared/model-viewer/cache-model-geometry";
import { getCacheErrorMessage } from "../stores/cache-error";
import { ModelTexturesComponent } from "./model-textures.component";
import { ModelWorkspaceComponent } from "./model-workspace.component";
import { ModelAnimationsComponent } from "./model-animations.component";

import type { ModelTexture } from "../../../../../shared/model-viewer/model-scene";

type CacheTexture = ModelTexture & GetTextureMaterialDto & {hdrError?: boolean};

@Component({
    selector: "cache-model-preview",
    imports: [ModelViewerComponent, RouterLink, FormsModule, ModelTexturesComponent, ModelWorkspaceComponent, ModelAnimationsComponent, NgTemplateOutlet, MatButtonModule],
    templateUrl: "./cache-model-preview.component.html",
    host: { class: "block min-w-0" },
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
                material: this.http.get<GetTextureMaterialDto>(`${environment.cacheApiUrl}textures/${id}/material`),
            }).pipe(
                switchMap(({response, material}) => {
                    const alphaMode = material.alphaMode;
                    if (alphaMode !== 0 && alphaMode !== 1 && alphaMode !== 2) throw new Error("Unsupported texture alpha mode.");
                    const bitmap = from(createImageBitmap(response.body!, {imageOrientation: "flipY"}).then(image => {
                        if (cancelled) { image.close(); return; }
                        images.set(id, {image, ...material, alphaMode});
                    }));
                    const hdr = material.hasHdr ? this.http.get<GetTextureHdrDto>(`${environment.cacheApiUrl}textures/${id}/hdr`).pipe(
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
    readonly sequence = httpResource<GetAnimationSequenceDto>(() => this.animationId() === null ? undefined : `${environment.cacheApiUrl}types/animations/${this.animationId()}/sequence`);
    readonly sequenceValue = computed(() => this.sequence.hasValue() ? this.sequence.value() : undefined);
    readonly clip = rxResource({
        params: () => this.sequenceValue()?.frameIds,
        stream: ({ params }) => {
            const uniqueIds = [...new Set(params)];
            if (!uniqueIds.length) return of([] as GetAnimationFrameDto[]);
            return forkJoin(uniqueIds.map(id => this.http.get<GetAnimationFrameDto>(`${environment.cacheApiUrl}animation-frames/${id}`))).pipe(
                map(frames => params.map(id => frames[uniqueIds.indexOf(id)])),
            );
        },
    });
    readonly clipValue = computed(() => this.clip.hasValue() ? this.clip.value() : undefined);
    readonly frameId = computed(() => this.manualFrameId());
    readonly frame = httpResource<GetAnimationFrameDto>(() => this.frameId() === null ? undefined : `${environment.cacheApiUrl}animation-frames/${this.frameId()}`);
    readonly frameValue = computed(() => this.frame.hasValue() ? this.frame.value() : undefined);
    readonly posed = computed(() => {
        const model = this.modelValue();
        const frame = this.animationId() !== null ? this.clipValue()?.[this.frameIndex()] : this.frameValue();
        if (!model || !frame || !model.vertexSkins) return { model, error: null };
        try { return { model: { ...model, positions: poseModel(model.positions, model.vertexSkins, frame, model.coordinateScale ?? 1), faceAlpha: poseFaceAlpha(model.faceAlpha, model.faceSkins, frame) }, error: null }; }
        catch (error) { return { model, error: error instanceof Error ? error.message : "Frame could not be applied." }; }
    });
    readonly poseError = computed(() => this.posed().error);
    readonly workspace = input(false);
    readonly showLookup = computed(() => this.workspace() && !this.modelValue());
    readonly selectedTexture = linkedSignal(() => { this.modelId(); return -1; });
    readonly previewNotes = computed(() => [...(this.modelValue()?.warnings ?? []), ...this.particleWarnings(), ...this.materialWarnings()]);
    readonly sequenceErrorMessage = computed(() => this.sequence.error() ? this.errorMessage(this.sequence.error()) : null);
    readonly clipErrorMessage = computed(() => this.clip.error() ? this.errorMessage(this.clip.error()) : null);
    readonly frameErrorMessage = computed(() => this.frame.error() ? this.errorMessage(this.frame.error()) : null);
    readonly appearanceChoices = computed(() => this.appearances.hasValue() ? this.appearances.value().map(choice => ({...choice, label: choice.label + (choice.available ? "" : " (unavailable)")})) : []);
    readonly appearanceModelLabel = computed(() => this.selectedAppearance()?.modelIds.map(id => `#${id}`).join(" ") || "None for this appearance.");
    readonly currentAnimationIds = computed(() => this.modelValue()?.animationIds ?? []);
    readonly hasSkins = computed(() => !!this.modelValue()?.vertexSkins);
    readonly missingModel = computed(() => !this.definition() && this.itemId() === null && (this.modelId() ?? -1) < 0);
    readonly textureRetryAvailable = computed(() => !!this.textureData.error() || this.hasHdrError());
    readonly height = input(360);
    readonly modelId = input<number | null>(null);
    readonly itemId = input<number | null>(null);
    readonly appearance = linkedSignal(() => { this.itemId(); return "inventory"; });
    readonly appearances = httpResource<GetItemModelAppearanceDto[]>(() => {
        const id = this.itemId();
        return id === null || id < 0 ? undefined : `${environment.cacheApiUrl}types/items/${id}/model-appearances`;
    });
    readonly selectedAppearance = computed(() => this.appearances.hasValue() ? this.appearances.value().find(choice => choice.key === this.appearance()) : undefined);
    readonly definition = input<{ kind: "npcs" | "objects"; id: number; shape?: number } | null>(null);
    readonly model = httpResource<GetModelDto>(() => {
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
        return prepareCacheModel({...value, particles: value.particles ?? []}, this.textureImages());
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
