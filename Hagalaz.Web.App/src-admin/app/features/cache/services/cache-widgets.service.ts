import { HttpClient, httpResource } from "@angular/common/http";
import { computed, inject, Injectable } from "@angular/core";
import type { WidgetComponent } from "./widget-component";
import type { WidgetProgramBundle } from "./widget-runtime";
import { environment } from "../../../../environments/environment";

interface WidgetReferenceEntry {
    fileId: number;
    children: { childId: number; index: number }[];
}

export interface WidgetCallback {
    event: string;
    scriptId: number | null;
    args: ({ type: "int" | "unknown"; value: number } | { type: "string"; value: string })[];
    triggers: number[];
}

export type PreviewFields = "hidden" | "text" | "action" | "options" | "spriteId" | "spriteTiled" | "color" | "transparency" | "modelId" | "x" | "y" | "xMode" | "yMode" | "width" | "height" | "widthMode" | "heightMode" | "scrollX" | "scrollY";
export interface WidgetPreviewState {
    components: (Pick<WidgetComponent, "id"> & Partial<Pick<WidgetComponent, PreviewFields>>)[];
    dynamicChildren?: (WidgetComponent & { slot: number })[];
    integers: Record<string, number>;
}

interface WidgetArchive {
    entries: { subFileId: number; dataBase64: string; callbacks: WidgetCallback[] | null }[];
}

@Injectable()
export class CacheWidgetsService {
    private readonly http = inject(HttpClient);

    loadProgram(id: number) {
        return this.http.get<WidgetProgramBundle>(`${environment.cacheApiUrl}widgets/${id}/program`);
    }
    // The game client stores widget groups and their component files in index 3.
    readonly referenceTable = httpResource<{ entries: WidgetReferenceEntry[] }>(() => `${environment.cacheApiUrl}indexes/3/reference-table`);
    readonly widgets = computed(() =>
        this.referenceTable.hasValue() ? [...this.referenceTable.value().entries].sort((a, b) => a.fileId - b.fileId) : []
    );

    archive(widgetId: () => number | undefined) {
        return httpResource<WidgetArchive>(() => {
            const id = widgetId();
            return id === undefined ? undefined : `${environment.cacheApiUrl}widgets/${id}/components`;
        });
    }
}
