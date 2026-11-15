import type { ParticleEmitterDto } from "./particle-emitter-dto";

export interface ModelParticleDto {
    emitterId: number;
    face: number;
    definition: ParticleEmitterDto | null;
    warning: string | null;
}
