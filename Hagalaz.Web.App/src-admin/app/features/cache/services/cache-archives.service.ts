import { httpResource } from "@angular/common/http";
import { Service, type Signal } from "@angular/core";
import { environment } from "../../../../environments/environment";
import type { GetArchiveSizesDto } from "./cache.models";

@Service()
export class CacheArchivesService {
    archiveSizesResource(enabled: Signal<boolean>) {
        return httpResource<GetArchiveSizesDto>(() => (enabled() ? `${environment.cacheApiUrl}types/archive-sizes` : undefined));
    }
}
