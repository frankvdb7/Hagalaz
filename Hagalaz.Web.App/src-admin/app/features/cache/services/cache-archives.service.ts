import { httpResource } from "@angular/common/http";
import { Service, type Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import type { ArchiveSizesDto } from "./cache.models";

@Service()
export class CacheArchivesService {
    archiveSizesResource(enabled: Signal<boolean>) {
        return httpResource<ArchiveSizesDto>(() => (enabled() ? `${environment.cacheApiUrl}types/archive-sizes` : undefined));
    }
}
