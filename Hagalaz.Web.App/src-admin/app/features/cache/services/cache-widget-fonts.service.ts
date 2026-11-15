import { HttpClient } from "@angular/common/http";
import { inject, Injectable } from "@angular/core";
import { catchError, forkJoin, map, type Observable, of, shareReplay } from "rxjs";
import { environment } from "../../../../environments/environment";
import { decodeWidgetFont, type WidgetFont } from "./widget-font";

@Injectable()
export class CacheWidgetFontsService {
    private readonly http = inject(HttpClient);
    private readonly fonts = new Map<number, Observable<WidgetFont | null>>();

    get(id: number) {
        let font = this.fonts.get(id);
        if (!font) {
            const container = (index: number) => this.http.get<{ dataBase64: string }>(`${environment.cacheApiUrl}indexes/${index}/files/${id}/container`);
            font = forkJoin([container(13), container(8)]).pipe(
                map(([metrics, glyphs]) => decodeWidgetFont(metrics.dataBase64, glyphs.dataBase64)),
                catchError(() => {
                    this.fonts.delete(id);
                    return of(null);
                }),
                shareReplay({ bufferSize: 1, refCount: true })
            );
            this.fonts.set(id, font);
        }
        return font;
    }
}
