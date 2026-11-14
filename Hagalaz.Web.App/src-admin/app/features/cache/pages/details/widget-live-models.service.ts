import { HttpClient } from "@angular/common/http";
import { DestroyRef, inject, Injectable, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { animationFrames, catchError, distinctUntilChanged, forkJoin, from, map, type Observable, of, shareReplay, switchMap } from "rxjs";
import { animationFrameDelay } from "../../../../../../shared/model-viewer/animation-tween";
import { environment } from "../../../../../environments/environment";
import type { GetAnimationFrameDto, GetAnimationSequenceDto, GetModelDto, GetTextureMaterialDto } from "../../services/dtos";
import type { WidgetModelAsset, WidgetModelDraw, WidgetModelScene, WidgetTexture } from "./widget-model-scene";

@Injectable()
export class WidgetLiveModelsService {
    private readonly http = inject(HttpClient);
    private readonly destroy = inject(DestroyRef);
    private readonly models = new Map<number, Observable<WidgetModelAsset | null>>();
    private readonly textures = new Map<number, Observable<WidgetTexture | null>>();
    private readonly bitmaps = new Set<ImageBitmap>();
    private readonly frames = new Map<number, Observable<GetAnimationFrameDto | null>>();
    private readonly animations = new Map<number, Observable<WidgetModelDraw["pose"]>>();
    private readonly queue = new Map<HTMLCanvasElement, WidgetModelDraw>();
    private scene?: WidgetModelScene;
    private frame?: number;
    private starting = false;
    readonly available = signal<boolean | null>(null);

    constructor() {
        this.destroy.onDestroy(() => {
            if (this.frame !== undefined) cancelAnimationFrame(this.frame);
            this.scene?.dispose();
            for (const bitmap of this.bitmaps) bitmap.close();
        });
    }

    async start() {
        if (this.starting) return;
        this.starting = true;
        if (typeof WebGL2RenderingContext === "undefined") {
            this.available.set(false);
            return;
        }
        try {
            const { WidgetModelScene } = await import("./widget-model-scene");
            if (this.destroy.destroyed) return;
            this.scene = new WidgetModelScene();
            this.available.set(true);
        } catch {
            this.available.set(false);
        }
    }

    enqueue(target: HTMLCanvasElement, draw: WidgetModelDraw) {
        this.queue.set(target, draw);
        this.frame ??= requestAnimationFrame(() => {
            this.frame = undefined;
            try {
                // The renderer consumes this batch synchronously.
                this.scene?.draw(this.queue);
            } catch {
                this.scene?.dispose();
                this.scene = undefined;
                this.available.set(false);
            } finally {
                this.queue.clear();
            }
        });
    }

    remove(target: HTMLCanvasElement) {
        this.queue.delete(target);
    }

    model(id: number) {
        let asset = this.models.get(id);
        if (!asset) {
            asset = this.http.get<GetModelDto>(`${environment.cacheApiUrl}models/${id}`).pipe(
                switchMap((model) => {
                    const ids = [...new Set((model.faceTextureIds ?? []).filter((value) => value >= 0))];
                    return ids.length
                        ? forkJoin(ids.map((texture) => this.texture(texture))).pipe(
                              map((values) => {
                                  const textures = new Map<number, WidgetTexture>();
                                  values.forEach((value, index) => {
                                      if (value) textures.set(ids[index], value);
                                  });
                                  return { model, textures };
                              })
                          )
                        : of({ model, textures: new Map<number, WidgetTexture>() });
                }),
                catchError(() => of(null)),
                takeUntilDestroyed(this.destroy),
                shareReplay({ bufferSize: 1, refCount: true })
            );
            this.models.set(id, asset);
        }
        return asset;
    }

    private texture(id: number) {
        let texture = this.textures.get(id);
        if (!texture) {
            const base = `${environment.cacheApiUrl}textures/${id}`;
            texture = forkJoin({
                material: this.http.get<GetTextureMaterialDto>(`${base}/material`),
                blob: this.http.get(`${base}/image?sourceAlpha=true`, { responseType: "blob" }),
            }).pipe(
                switchMap(({ material, blob }) =>
                    from(
                        createImageBitmap(blob, { imageOrientation: "flipY", premultiplyAlpha: "none" }).then((image) => {
                            if (this.destroy.destroyed) {
                                image.close();
                                return null;
                            }
                            this.bitmaps.add(image);
                            return { image, material };
                        })
                    )
                ),
                catchError(() => of(null)),
                takeUntilDestroyed(this.destroy),
                shareReplay({ bufferSize: 1, refCount: false })
            );
            this.textures.set(id, texture);
        }
        return texture;
    }

    animation(id: number) {
        let animation = this.animations.get(id);
        if (!animation) {
            animation = this.http.get<GetAnimationSequenceDto>(`${environment.cacheApiUrl}types/animations/${id}/sequence`).pipe(
                switchMap(({ frameIds, frameDurations, loop }) => {
                    if (!frameIds.length) return of(undefined);
                    let duration = 0;
                    const ends = frameIds.map((_, index) => (duration += animationFrameDelay(frameDurations, index) * 20));
                    const loopStart = loop > 0 && loop <= frameIds.length ? frameIds.length - loop : 0;
                    const start = ends[loopStart - 1] ?? 0;
                    return animationFrames().pipe(
                        map(({ elapsed }) => {
                            const time = elapsed < duration ? elapsed : start + ((elapsed - duration) % (duration - start));
                            return frameIds[ends.findIndex((end) => time < end)];
                        }),
                        distinctUntilChanged(),
                        switchMap((id) => this.animationFrame(id).pipe(map((frame) => (frame ? { id, frame } : undefined))))
                    );
                }),
                catchError(() => of(undefined)),
                takeUntilDestroyed(this.destroy),
                shareReplay({ bufferSize: 1, refCount: true })
            );
            this.animations.set(id, animation);
        }
        return animation;
    }

    private animationFrame(id: number) {
        let frame = this.frames.get(id);
        if (!frame) {
            frame = this.http.get<GetAnimationFrameDto>(`${environment.cacheApiUrl}animation-frames/${id}`).pipe(
                catchError(() => of(null)),
                takeUntilDestroyed(this.destroy),
                shareReplay({ bufferSize: 1, refCount: false })
            );
            this.frames.set(id, frame);
        }
        return frame;
    }
}
