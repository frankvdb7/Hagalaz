import * as THREE from "three";
import type {ModelGeometry} from "./model-scene";

export interface ParticleDefinition {
    textureId: number; colorMin: number; colorMax: number;
    lifeMin: number; lifeMax: number; rateMin: number; rateMax: number;
    sizeMin: number; sizeMax: number; speedMin: number; speedMax: number;
    angles: number[]; unsupportedOpcodes: number[];
}
export interface ModelParticle {emitterId: number; face: number; definition: ParticleDefinition | null; warning?: string | null;}

/** One model's bounded particle surfaces; ModelScene owns timing and texture lifetime. */
export class ModelParticles {
    private readonly surfaces: {attachment: ModelParticle; points: THREE.Points<THREE.BufferGeometry, THREE.ShaderMaterial>; slots: {position: THREE.Vector3; velocity: THREE.Vector3; age: number; life: number; size: number; color: number[]}[]; credit: number; next: number; seed: number}[] = [];
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
            this.surfaces.push({attachment, points, slots: Array.from({length: capacity}, () => ({position: new THREE.Vector3(), velocity: new THREE.Vector3(), age: 1, life: 0, size: 0, color: [0,0,0,0]})), credit: 0, next: 0, seed: (attachment.emitterId * 97 + index + 1) >>> 0});
        }
    }

    update(delta: number, model: ModelGeometry, pixelScale: number): void {
        const dt = Math.min(Math.max(delta, 0), 0.1);
        for (const surface of this.surfaces) {
            const d = surface.attachment.definition!;
            const scale = model.particleScale ?? 1;
            const random = () => {surface.seed = (Math.imul(surface.seed, 1664525) + 1013904223) >>> 0; return surface.seed / 4294967296;};
            const sample = (min: number, max: number) => min + random() * (max - min);
            for (const slot of surface.slots) {slot.age += dt; if (slot.age < slot.life) slot.position.addScaledVector(slot.velocity, dt);}
            surface.credit += sample(d.rateMin, d.rateMax) * dt;
            const births = Math.min(Math.floor(surface.credit), surface.slots.length);
            surface.credit -= Math.floor(surface.credit);
            const face = surface.attachment.face * 3;
            if (face + 2 >= model.indices.length) continue;
            const vertex = (corner: number) => new THREE.Vector3().fromArray(model.positions, model.indices[face + corner] * 3);
            const a = vertex(0), b = vertex(1), c = vertex(2);
            const normal = b.clone().sub(a).cross(c.clone().sub(a)).normalize();
            for (let birth = 0; birth < births; birth++) {
                const slot = surface.slots[surface.next++ % surface.slots.length];
                let u = random(), v = random(); if (u + v > 1) {u = 1-u; v = 1-v;}
                slot.position.copy(a).multiplyScalar(1-u-v).addScaledVector(b, u).addScaledVector(c, v);
                // Inspectable ballistic approximation: client cone/force-field integration remains disclosed.
                const spread = Math.abs((d.angles[1] ?? 0) - (d.angles[0] ?? 0)) * Math.PI / 8192;
                slot.velocity.copy(normal).add(new THREE.Vector3(sample(-spread,spread),sample(-spread,spread),sample(-spread,spread))).normalize().multiplyScalar(sample(d.speedMin,d.speedMax) * scale);
                slot.life = sample(d.lifeMin,d.lifeMax); slot.age = 0; slot.size = sample(d.sizeMin,d.sizeMax) * scale;
                const mix = random();
                slot.color = [16,8,0,24].map(shift => (((d.colorMin >>> shift)&255) + mix * (((d.colorMax >>> shift)&255)-((d.colorMin >>> shift)&255))) / 255);
                const tint = new THREE.Color(slot.color[0],slot.color[1],slot.color[2]).convertSRGBToLinear();
                slot.color[0] = tint.r; slot.color[1] = tint.g; slot.color[2] = tint.b;
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
