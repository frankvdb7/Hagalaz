import * as THREE from "three";
import type {ModelGeometry} from "./model-scene";

export interface ParticleDefinition {
    textureId: number; colorMin: number; colorMax: number;
    lifeMin: number; lifeMax: number; rateMin: number; rateMax: number;
    sizeMin: number; sizeMax: number; speedMin: number; speedMax: number;
    angles: number[]; unsupportedOpcodes: number[];
    localEffectorIds?: number[] | null;
    speedDrag?: { mode: number; strength: number } | null;
}
export interface ModelParticle {emitterId: number; face: number; definition: ParticleDefinition | null; warning?: string | null;}

export interface ParticleEffectorDefinition {
    x: number; y: number; z: number;
    coneAngle: number; falloff: number; attenuation: number; scope: number;
    directDisplacement: boolean; radial: boolean; reverse: boolean;
}
export interface ModelEffector { effectorId: number; vertex: number; definition: ParticleEffectorDefinition | null; }
export interface PosedEffector { effectorId: number; position: [number, number, number]; definition: ParticleEffectorDefinition | null; }

interface ParticleSlot {
    position: THREE.Vector3; velocity: THREE.Vector3; speed: number;
    direction?: [number, number, number];
    age: number; life: number; size: number; color: number[];
}

interface LocalEffector {
    definition: ParticleEffectorDefinition;
    x: number; y: number; z: number;
    magnitude: number; rangeSquared: number; cone: number;
}

interface ParticleMotion {
    scale: number;
    effectors: readonly LocalEffector[];
    drag?: { mode: number; strength: number; center: [number, number, number] };
}

function localEffectors(model: ModelGeometry, ids: readonly number[] | null | undefined, scale: number): LocalEffector[] {
    if (!ids?.length || !model.effectors?.length) return [];
    return model.effectors.slice(0, 8).flatMap(({ effectorId, position, definition }) => {
        if (definition?.scope !== 0 || !ids.includes(effectorId)) return [];
        const magnitude = Math.trunc(Math.hypot(definition.x, definition.y, definition.z));
        const range = Math.trunc((magnitude << 3) / definition.attenuation);
        return [{
            definition, x: position[0] / scale, y: -position[1] / scale, z: position[2] / scale,
            magnitude: definition.reverse ? -magnitude : magnitude,
            rangeSquared: [2147483647, range * range, range][definition.falloff],
            cone: Math.trunc(16384 * Math.cos(definition.coneAngle * 8 * 3.834951969714103e-4)),
        }];
    });
}

/** Class325_Sub3_Sub1_Sub1 applies local forces in cache coordinates per millisecond. */
function applyLocalEffectors(slot: ParticleSlot, effectors: readonly LocalEffector[], scale: number, dt: number): void {
    const x = Math.floor(slot.position.x / scale);
    const y = Math.floor(-slot.position.y / scale);
    const z = Math.floor(slot.position.z / scale);
    const milliseconds = dt * 1000;
    let forceX = 0, forceY = 0, forceZ = 0;
    for (const effector of effectors) {
        const force = sampleEffector(effector, x, y, z);
        if (!force) continue;
        if (effector.definition.directDisplacement) {
            const unit = scale / 4096;
            slot.position.x = Math.trunc(slot.position.x / unit + force[0] * milliseconds) * unit;
            slot.position.y = -Math.trunc(-slot.position.y / unit + force[1] * milliseconds) * unit;
            slot.position.z = Math.trunc(slot.position.z / unit + force[2] * milliseconds) * unit;
        } else {
            forceX += force[0];
            forceY += force[1];
            forceZ += force[2];
        }
    }
    if (slot.speed && (forceX || forceY || forceZ)) {
        bendParticle(slot, forceX * milliseconds, forceY * milliseconds, forceZ * milliseconds, scale);
    }
}

function bendParticle(slot: ParticleSlot, x: number, y: number, z: number, scale: number): void {
    const units = 32768 / slot.speed;
    const direction = slot.direction ?? [slot.velocity.x * units, -slot.velocity.y * units, slot.velocity.z * units];
    x += direction[0];
    y += direction[1];
    z += direction[2];
    // The client keeps the direction in signed shorts and doubles speed when it halves the direction.
    const speedUnit = scale * 1000 / 262144;
    let speed = Math.round(slot.speed / speedUnit);
    while (Math.max(Math.abs(x), Math.abs(y), Math.abs(z)) > 32767) {
        x /= 2; y /= 2; z /= 2;
        speed <<= 1;
    }
    direction[0] = Math.trunc(x);
    direction[1] = Math.trunc(y);
    direction[2] = Math.trunc(z);
    slot.direction = direction;
    slot.speed = speed * speedUnit;
    const velocityUnit = scale * 1000 / 4096;
    const velocity = (axis: number) => Math.floor(direction[axis] * (speed << 2) / 8388608) * velocityUnit;
    slot.velocity.set(velocity(0), -velocity(1), velocity(2));
}

function sampleEffector({ definition, magnitude, rangeSquared, cone, x, y, z }: LocalEffector, px: number, py: number, pz: number): [number, number, number] | undefined {
    const dx = px - x, dy = py - y, dz = pz - z;
    const squared = dx * dx + dy * dy + dz * dz;
    if (squared > rangeSquared || magnitude === 0) return;
    const distance = Math.sqrt(squared) || 1;
    const cosine = (dx * definition.x + dy * definition.y + dz * definition.z) * 65535 / (magnitude * distance);
    if (cosine < cone) return;
    if (definition.radial) {
        const force = magnitude / distance;
        return [dx * force, dy * force, dz * force];
    }
    const distanceUnit = distance / 16;
    const falloff = definition.falloff === 0 ? 0 : (definition.falloff === 1 ? distanceUnit : distanceUnit * distanceUnit) * definition.attenuation;
    return [definition.x - falloff, definition.y - falloff, definition.z - falloff];
}

function advanceParticles(slots: readonly ParticleSlot[], motion: ParticleMotion, dt: number): void {
    if (dt <= 0) return;
    for (const slot of slots) advanceParticle(slot, motion, dt);
}

function advanceParticle(slot: ParticleSlot, { effectors, scale, drag }: ParticleMotion, dt: number): void {
    slot.age += dt;
    if (slot.age >= slot.life) return;
    if (drag) applySpeedDrag(slot, drag, scale, dt);
    if (effectors.length) applyLocalEffectors(slot, effectors, scale, dt);
    slot.position.addScaledVector(slot.velocity, dt);
}

/** Class325_Sub3_Sub1_Sub1 damps raw speed using distance to the current emitter centroid. */
function applySpeedDrag(slot: ParticleSlot, drag: NonNullable<ParticleMotion["drag"]>, scale: number, dt: number): void {
    const unit = scale * 1000 / 262144;
    const speed = Math.round(slot.speed / unit);
    if (!speed) return;
    const dx = Math.floor(slot.position.x / scale) - drag.center[0];
    const dy = Math.floor(-slot.position.y / scale) - drag.center[1];
    const dz = Math.floor(slot.position.z / scale) - drag.center[2];
    const squared = (dx * dx + dy * dy + dz * dz) | 0;
    const distance = drag.mode === 1 ? Math.trunc(Math.sqrt(squared)) >> 2 : squared;
    const divisor = drag.mode === 1 ? 262144 : 268435456;
    const next = (speed - Math.floor(speed * drag.strength * distance * Math.round(dt * 1000) / divisor)) | 0;
    slot.velocity.multiplyScalar(next / speed);
    slot.speed = next * unit;
}

/** Class406 staggers new particles in 8-bit fixed-point milliseconds across the update. */
function advanceBirth(slot: ParticleSlot, motion: ParticleMotion, dt: number, birth: number, births: number): void {
    if ((!motion.effectors.length && !motion.drag) || birth === 0) return;
    const spacing = Math.trunc((Math.floor(dt * 1000) << 8) / births);
    const age = birth * spacing;
    if (age > 256) advanceParticle(slot, motion, (age >> 8) / 1000);
}

function particleMotion(model: ModelGeometry, definition: ParticleDefinition, scale: number, a: THREE.Vector3, b: THREE.Vector3, c: THREE.Vector3): ParticleMotion {
    const motion: ParticleMotion = { scale, effectors: localEffectors(model, definition.localEffectorIds, scale) };
    if (definition.speedDrag) {
        const divisor = 3 * scale;
        motion.drag = { ...definition.speedDrag, center: [
            Math.trunc((a.x + b.x + c.x) / divisor),
            Math.trunc(-(a.y + b.y + c.y) / divisor),
            Math.trunc((a.z + b.z + c.z) / divisor),
        ] };
    }
    return motion;
}

/** One model's bounded particle surfaces; ModelScene owns timing and texture lifetime. */
export class ModelParticles {
    private readonly surfaces: {attachment: ModelParticle; points: THREE.Points<THREE.BufferGeometry, THREE.ShaderMaterial>; slots: ParticleSlot[]; credit: number; next: number; seed: number}[] = [];
    constructor(private readonly scene: THREE.Scene, particles: readonly ModelParticle[], textures: ReadonlyMap<number, THREE.Texture>) {
        for (const [index, attachment] of particles.slice(0,4096).entries()) {
            const d = attachment.definition;
            if (!d || d.lifeMax <= 0 || d.rateMax <= 0) continue;
            const capacity = Math.max(1, Math.min(Math.floor(4096 / particles.length), Math.ceil(d.rateMax * d.lifeMax) + 1));
            const geometry = new THREE.BufferGeometry();
            geometry.setAttribute("position", new THREE.Float32BufferAttribute(new Float32Array(capacity * 3), 3));
            geometry.setAttribute("particleColor", new THREE.Float32BufferAttribute(new Float32Array(capacity * 4), 4));
            geometry.setAttribute("particleSize", new THREE.Float32BufferAttribute(new Float32Array(capacity), 1));
            const texture = textures.get(d.textureId);
            const material = new THREE.ShaderMaterial({
                uniforms: {sprite: {value: texture ?? null}, textured: {value: !!texture}, normalizeHdr: {value: texture?.type === THREE.FloatType}, pixelScale: {value: 1}},
                vertexShader: `attribute vec4 particleColor; attribute float particleSize; varying vec4 tint; uniform float pixelScale;
                    void main() { tint = particleColor; vec4 view = modelViewMatrix * vec4(position, 1.0); gl_Position = projectionMatrix * view; gl_PointSize = max(0.0, particleSize * pixelScale / max(0.01, -view.z)); }`,
                fragmentShader: `uniform sampler2D sprite; uniform bool textured; uniform bool normalizeHdr; varying vec4 tint;
                    void main() { vec2 uv = vec2(gl_PointCoord.x, 1.0-gl_PointCoord.y); float falloff = max(0.0, 1.0-length(gl_PointCoord-0.5)*2.0);
                    vec4 color = textured ? texture2D(sprite, uv) : vec4(1.0,1.0,1.0,falloff*falloff);
                    if (normalizeHdr) color.rgb /= max(1.0, max(color.r, max(color.g, color.b)));
                    gl_FragColor = color * tint;
                    #include <tonemapping_fragment>
                    #include <colorspace_fragment>
                    }`,
                transparent: true, depthWrite: false, depthTest: true,
            });
            const points = new THREE.Points(geometry, material); points.frustumCulled = false;
            this.scene.add(points);
            this.surfaces.push({attachment, points, slots: Array.from({length: capacity}, () => ({position: new THREE.Vector3(), velocity: new THREE.Vector3(), speed: 0, age: 1, life: 0, size: 0, color: [0,0,0,0]})), credit: 0, next: 0, seed: (attachment.emitterId * 97 + index + 1) >>> 0});
        }
    }

    update(delta: number, model: ModelGeometry, pixelScale: number): void {
        const dt = Math.min(Math.max(delta, 0), 0.1);
        for (const surface of this.surfaces) {
            const d = surface.attachment.definition!;
            const scale = model.particleScale ?? 1;
            const random = () => {surface.seed = (Math.imul(surface.seed, 1664525) + 1013904223) >>> 0; return surface.seed / 4294967296;};
            const sample = (min: number, max: number) => min + random() * (max - min);
            surface.credit += sample(d.rateMin, d.rateMax) * dt;
            const births = Math.min(Math.floor(surface.credit), surface.slots.length);
            surface.credit -= Math.floor(surface.credit);
            const face = surface.attachment.face * 3;
            if (face + 2 >= model.indices.length) continue;
            const vertex = (corner: number) => new THREE.Vector3().fromArray(model.positions, model.indices[face + corner] * 3);
            const a = vertex(0), b = vertex(1), c = vertex(2);
            const motion = particleMotion(model, d, scale, a, b, c);
            advanceParticles(surface.slots, motion, dt);
            const normal = b.clone().sub(a).cross(c.clone().sub(a)).normalize();
            for (let birth = 0; birth < births; birth++) {
                const slot = surface.slots[surface.next++ % surface.slots.length];
                let u = random(), v = random(); if (u + v > 1) {u = 1-u; v = 1-v;}
                slot.position.copy(a).multiplyScalar(1-u-v).addScaledVector(b, u).addScaledVector(c, v);
                if (motion.drag) quantizeBirthPosition(slot.position, scale);
                // Inspectable ballistic approximation: client cone/force-field integration remains disclosed.
                const spread = Math.abs((d.angles[1] ?? 0) - (d.angles[0] ?? 0)) * Math.PI / 8192;
                slot.velocity.copy(normal).add(new THREE.Vector3(sample(-spread,spread),sample(-spread,spread),sample(-spread,spread))).normalize();
                slot.speed = sample(d.speedMin,d.speedMax) * scale;
                slot.direction = undefined;
                slot.velocity.multiplyScalar(slot.speed);
                slot.life = sample(d.lifeMin,d.lifeMax); slot.age = 0; slot.size = sample(d.sizeMin,d.sizeMax) * scale;
                const mix = random();
                slot.color = [16,8,0,24].map(shift => (((d.colorMin >>> shift)&255) + mix * (((d.colorMax >>> shift)&255)-((d.colorMin >>> shift)&255))) / 255);
                const tint = new THREE.Color(slot.color[0],slot.color[1],slot.color[2]).convertSRGBToLinear();
                slot.color[0] = tint.r; slot.color[1] = tint.g; slot.color[2] = tint.b;
                advanceBirth(slot, motion, dt, birth, births);
            }
            const positions = surface.points.geometry.getAttribute("position"), colors = surface.points.geometry.getAttribute("particleColor"), sizes = surface.points.geometry.getAttribute("particleSize");
            surface.slots.forEach((slot, index) => {
                positions.setXYZ(index, slot.position.x, slot.position.y, slot.position.z);
                const alive = slot.age < slot.life;
                colors.setXYZW(index, slot.color[0],slot.color[1],slot.color[2],alive ? slot.color[3] * (1-slot.age/slot.life) : 0);
                sizes.setX(index, alive ? slot.size : 0);
            });
            positions.needsUpdate = colors.needsUpdate = sizes.needsUpdate = true;
            surface.points.material.uniforms.pixelScale.value = pixelScale;
        }
    }

    dispose(): void {
        for (const {points} of this.surfaces) {this.scene.remove(points); points.geometry.dispose(); points.material.dispose();}
        this.surfaces.length = 0;
    }
}

function quantizeBirthPosition(position: THREE.Vector3, scale: number): void {
    position.set(Math.trunc(position.x / scale) * scale, Math.trunc(position.y / scale) * scale, Math.trunc(position.z / scale) * scale);
}
