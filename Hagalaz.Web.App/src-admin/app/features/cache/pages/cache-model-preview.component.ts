import type { GetTextureMaterialDto, GetTextureHdrDto } from "../services/dtos";
import type { GetModelDto, GetAnimationSequenceDto, GetAnimationFrameDto, GetItemModelAppearanceDto } from "../services/dtos";
import { HttpClient, httpResource } from "@angular/common/http";
import { Component, computed, effect, inject, input, linkedSignal, model, signal, untracked } from "@angular/core";
import { rxResource } from "@angular/core/rxjs-interop";
import { forkJoin, map, of, from, switchMap, Observable, catchError } from "rxjs";
import { describeMaterialEffect } from "../../../../../shared/model-viewer/material-effects";
import { NgTemplateOutlet } from "@angular/common";
import { MatButtonModule } from "@angular/material/button";
import { FormsModule } from "@angular/forms";
import { poseBillboards, poseFaceAlpha, poseFaceColors, poseModel, unsupportedTransformTypes } from "../../../../../shared/model-viewer/model-pose";
import { animationFrameDelay, animationTickMs, nextAnimationFrame, tweenAnimationFrame } from "../../../../../shared/model-viewer/animation-tween";
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
        ...(this.modelValue()?.billboards?.map(billboard => billboard.definition?.textureId ?? -1) ?? []),
    ].filter(id => id >= 0))]);
    readonly particleWarnings = computed(() => [...new Set((this.modelValue()?.particles ?? []).flatMap(particle => [
        ...(particle.warning ? [particle.warning] : []),
        ...(particle.definition ? [`Particle emitter #${particle.emitterId}: textured preview with model-local forces; HDR sprite brightness is bounded for inspection. Global forces, collision, prewarm, bloom and advanced lifecycle curves remain unverified.${particle.definition.unsupportedOpcodes.length ? ` Unsupported settings: ${particle.definition.unsupportedOpcodes.join(", ")}.` : ""}`] : []),
    ]))]);
    readonly textureData = rxResource({
        params: () => this.textureIds(),
        stream: ({params}) => !params.length ? of(new Map<number, CacheTexture>()) : new Observable<Map<number, CacheTexture>>(subscriber => {
            const images = new Map<number, CacheTexture>();
            let cancelled = false;
            const requests = forkJoin(params.map(id => forkJoin({
                response: this.http.get(`${environment.cacheApiUrl}textures/${id}/image?sourceAlpha=true`, {responseType: "blob", observe: "response"}),
                material: this.http.get<GetTextureMaterialDto>(`${environment.cacheApiUrl}textures/${id}/material`),
            }).pipe(
                switchMap(({response, material}) => {
                    const alphaMode = material.alphaMode;
                    if (alphaMode !== 0 && alphaMode !== 1 && alphaMode !== 2) throw new Error("Unsupported texture alpha mode.");
                    if (!response.body) throw new Error("Texture image is missing.");
                    const bitmap = from(createImageBitmap(response.body, {imageOrientation: "flipY", premultiplyAlpha: "none"}).then(image => {
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
    private readonly frameTick = signal(0);
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
    private readonly activeFrame = computed(() => {
        if (this.animationId() === null) return this.frameValue();
        const clip = this.clipValue();
        const index = this.frameIndex();
        const frame = clip?.[index];
        const sequence = this.sequenceValue();
        if (!frame || !clip || !sequence?.isTweened) return frame;
        const next = clip[nextAnimationFrame(index, clip.length, sequence.loop)];
        return tweenAnimationFrame(frame, next, this.frameTick(), animationFrameDelay(sequence.frameDurations, index));
    });
    readonly posed = computed(() => {
        const model = this.modelValue();
        const frame = this.activeFrame();
        if (!model || !frame) return { model, error: null };
        try {
            return { model: {
                ...model,
                positions: model.vertexSkins ? poseModel(model.positions, model.vertexSkins, frame, model.coordinateScale ?? 1) : model.positions,
                faceAlpha: poseFaceAlpha(model.faceAlpha, model.faceSkins, frame),
                faceColors: poseFaceColors(model.faceColors, model.faceSkins, frame),
                billboards: model.billboards && poseBillboards(model.billboards, frame),
            }, error: null };
        }
        catch (error) { return { model, error: error instanceof Error ? error.message : "Frame could not be applied." }; }
    });
    readonly poseError = computed(() => this.posed().error);
    readonly poseWarning = computed(() => {
        const frame = this.frameValue();
        const frames = this.animationId() !== null ? this.clipValue() ?? [] : frame ? [frame] : [];
        const types = new Set(frames.flatMap(unsupportedTransformTypes));
        return types.size ? `Unsupported animation transform types: ${[...types].join(", ")}.` : null;
    });
    readonly workspace = input(false);
    readonly inspectionTab = model(0);
    readonly showLookup = computed(() => this.workspace() && !this.modelValue());
    readonly selectedTexture = linkedSignal(() => { this.modelId(); return -1; });
    readonly previewNotes = computed(() => [
        ...(this.modelValue()?.warnings ?? []),
        ...(this.modelValue()?.billboards ?? []).flatMap(billboard => billboard.warning ? [billboard.warning] : []),
        ...(this.modelValue()?.effectors ?? []).flatMap(effector => effector.warning ? [effector.warning] : []),
        ...this.particleWarnings(), ...this.materialWarnings(),
    ]);
    readonly sequenceErrorMessage = computed(() => this.sequence.error() ? this.errorMessage(this.sequence.error()) : null);
    readonly clipErrorMessage = computed(() => this.clip.error() ? this.errorMessage(this.clip.error()) : null);
    readonly frameErrorMessage = computed(() => this.frame.error() ? this.errorMessage(this.frame.error()) : null);
    readonly appearanceChoices = computed(() => this.appearances.hasValue() ? this.appearances.value().map(choice => ({...choice, label: choice.label + (choice.available ? "" : " (unavailable)")})) : []);
    readonly appearanceModelLabel = computed(() => this.selectedAppearance()?.modelIds.map(id => `#${id}`).join(" ") || "None for this appearance.");
    readonly currentAnimationIds = computed(() => this.modelValue()?.animationIds ?? []);
    readonly hasSkins = computed(() => !!(this.modelValue()?.vertexSkins || this.modelValue()?.faceSkins || this.modelValue()?.billboards?.some(billboard => billboard.group >= 0)));
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
            const duration = animationFrameDelay(sequence.frameDurations, this.frameIndex());
            const tick = this.frameTick();
            const timer = sequence.isTweened
                ? setTimeout(() => this.advanceTween(tick, duration), animationTickMs)
                : setTimeout(() => this.advance(), duration * animationTickMs);
            cleanup(() => clearTimeout(timer));
        });
        effect(() => { this.modelId(); this.itemId(); this.definition(); this.appearance(); this.basePose(); });
        effect(() => {
            const model = this.modelValue();
            if (!model) return;
            untracked(() => {
                this.basePose();
                const firstAnimation = model.animationIds?.[0];
                if (this.hasSkins() && firstAnimation !== undefined) this.selectAnimation(firstAnimation);
            });
        });
    }

    selectAnimation(id: number): void { this.frameIndex.set(0); this.frameTick.set(0); this.manualFrameId.set(null); this.animationId.set(id); this.playing.set(true); }
    inspectFrame(): void { this.basePose(); if (Number.isSafeInteger(this.draftFrame) && this.draftFrame >= 0) this.manualFrameId.set(this.draftFrame); }
    basePose(): void { this.playing.set(false); this.animationId.set(null); this.manualFrameId.set(null); this.frameIndex.set(0); this.frameTick.set(0); }
    advance(): void {
        const sequence = this.sequenceValue();
        if (!sequence?.frameIds.length) return;
        this.frameIndex.update(index => nextAnimationFrame(index, sequence.frameIds.length, sequence.loop));
        this.frameTick.set(0);
    }

    private advanceTween(tick: number, duration: number): void {
        if (tick >= duration) {
            this.advance();
            this.frameTick.set(1);
        } else this.frameTick.set(tick + 1);
    }
}
