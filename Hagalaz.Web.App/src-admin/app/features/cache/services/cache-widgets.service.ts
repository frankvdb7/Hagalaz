import { httpResource } from "@angular/common/http";
import { computed, Injectable } from "@angular/core";
import { environment } from "../../../../environments/environment";

interface WidgetReferenceEntry {
    fileId: number;
    children: { childId: number; index: number }[];
}

interface WidgetArchive {
    entries: { subFileId: number; dataBase64: string }[];
}

@Injectable()
export class CacheWidgetsService {
    // The game client stores widget groups and their component files in index 3.
    readonly referenceTable = httpResource<{ entries: WidgetReferenceEntry[] }>(() => `${environment.cacheApiUrl}indexes/3/reference-table`);
    readonly widgets = computed(() =>
        this.referenceTable.hasValue() ? [...this.referenceTable.value().entries].sort((a, b) => a.fileId - b.fileId) : []
    );

    archive(widgetId: () => number | undefined) {
        return httpResource<WidgetArchive>(() => {
            const id = widgetId();
            return id === undefined ? undefined : `${environment.cacheApiUrl}indexes/3/files/${id}/archive`;
        });
    }
}
