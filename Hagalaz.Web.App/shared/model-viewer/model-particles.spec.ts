import * as THREE from "three";
import {describe,expect,it,vi} from "vitest";
import {ModelParticles, type ParticleDefinition} from "./model-particles";
import {prepareCacheModel} from "./cache-model-geometry";
import {poseModel} from "./model-pose";
const definition: ParticleDefinition = {textureId:744,colorMin:0xffffcc22,colorMax:0xffffcc22,lifeMin:0.05,lifeMax:0.06,rateMin:500,rateMax:500,sizeMin:40,sizeMax:48,speedMin:0,speedMax:0,angles:[0,0,0,0],unsupportedOpcodes:[]};
const attachment = {emitterId:403,face:1,definition};
const model = {positions:[0,0,0,1,0,0,0,1,0, 100,0,0,101,0,0,100,1,0],indices:[0,1,2,3,4,5],faceColors:[127,20287],faceAlpha:[0,255],particles:[attachment]};

describe("model-attached particles",()=>{
    it("derives the falling launch direction from the cached constant force", () => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 403, face: 0, definition: {
            ...definition, rateMin: 50, rateMax: 50, speedMin: 1000, speedMax: 1000,
            constantEffectors: [{ x: 0, y: 984, z: 0, coneAngle: 0, falloff: 0, attenuation: 1,
                scope: 2, directDisplacement: false, radial: false, reverse: false }],
        } };
        const particles = new ModelParticles(scene, [emitter], new Map());
        try {
            const geometry = { positions: [0, 0, 0, 1, 0, 0, 0, 1, 0], indices: [0, 1, 2] };
            const positions = (scene.children[0] as THREE.Points).geometry.getAttribute("position");
            particles.update(0.02, geometry, 200);
            const birthY = positions.getY(0);
            particles.update(0.02, geometry, 200);
            expect(positions.getY(0) - birthY).toBeLessThan(-20);
            expect(positions.getZ(0)).toBe(0);
        } finally {
            particles.dispose();
        }
    });

    it("wraps the client launch vector when a component exceeds signed-short range", () => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 403, face: 0, definition: {
            ...definition, rateMin: 50, rateMax: 50, speedMin: 1000, speedMax: 1000, angles: [0, 0, 4096, 4096],
        } };
        const particles = new ModelParticles(scene, [emitter], new Map());
        try {
            const geometry = { positions: [0, 0, 0, 1, 0, 0, 0, 1, 0], indices: [0, 1, 2] };
            const positions = (scene.children[0] as THREE.Points).geometry.getAttribute("position");
            particles.update(0.02, geometry, 200);
            const birthX = positions.getX(0);
            particles.update(0.02, geometry, 200);
            expect(positions.getX(0) - birthX).toBeCloseTo(-20);
        } finally {
            particles.dispose();
        }
    });

    it("updates tint and opacity on separate lifetime windows", () => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 403, face: 0, definition: {
            ...definition, rateMin: 50, rateMax: 50, lifeMin: 0.1, lifeMax: 0.1,
            colorMin: 0xff000000, colorMax: 0xff000000,
            colorOverLife: { targetArgb: 0x00ffffff, rgbLifetimePercent: 40, alphaLifetimePercent: 80 },
        } };
        const particles = new ModelParticles(scene, [emitter], new Map());
        try {
            const geometry = { positions: [0, 0, 0, 1, 0, 0, 0, 1, 0], indices: [0, 1, 2] };
            const colors = (scene.children[0] as THREE.Points).geometry.getAttribute("particleColor");
            particles.update(0.02, geometry, 200);
            expect(colors.getX(0)).toBe(0);
            expect(colors.getW(0)).toBe(1);
            particles.update(0.02, geometry, 200);
            expect(colors.getX(0)).toBeCloseTo(new THREE.Color().setRGB(127 / 255, 0, 0).convertSRGBToLinear().r);
            expect(colors.getW(0)).toBeCloseTo(191 / 255);
            particles.update(0.02, geometry, 200);
            const tint = colors.getX(0);
            expect(tint).toBeGreaterThan(0.98);
            particles.update(0.02, geometry, 200);
            expect(colors.getX(0)).toBe(tint);
            expect(colors.getW(0)).toBeCloseTo(64 / 255);
        } finally {
            particles.dispose();
        }
    });

    it("ramps travel speed from rest and holds it after the configured window", () => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 403, face: 0, definition: {
            ...definition, rateMin: 50, rateMax: 50, lifeMin: 0.1, lifeMax: 0.1,
            speedOverLife: { rawTarget: 262144, lifetimePercent: 40 },
        } };
        const particles = new ModelParticles(scene, [emitter], new Map());
        try {
            const geometry = { positions: [0, 0, 0, 1, 0, 0, 0, 1, 0], indices: [0, 1, 2] };
            const positions = (scene.children[0] as THREE.Points).geometry.getAttribute("position");
            particles.update(0.02, geometry, 200);
            expect(positions.getZ(0)).toBe(0);
            particles.update(0.02, geometry, 200);
            expect(positions.getZ(0)).toBeCloseTo(10, 1);
            particles.update(0.02, geometry, 200);
            expect(positions.getZ(0)).toBeCloseTo(30, 1);
            particles.update(0.02, geometry, 200);
            expect(positions.getZ(0)).toBeCloseTo(50, 1);
        } finally {
            particles.dispose();
        }
    });

    it.each([undefined, [20287]])("uses the cache normal with cape emitter 403 after geometry preparation (%j)", faceColors => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 403, face: 0, definition: {
            ...definition, rateMin: 50, rateMax: 50, speedMin: 1000, speedMax: 1000,
            angles: [0, 64, 4080, 3920],
        } };
        const particles = new ModelParticles(scene, [emitter], new Map());
        try {
            const geometry = prepareCacheModel({ positions: [0, 0, 0, 1, 0, 0, 0, 1, 0], indices: [0, 1, 2], faceColors });
            const positions = (scene.children[0] as THREE.Points).geometry.getAttribute("position");
            particles.update(0.02, geometry, 200);
            const startX = positions.getX(0);
            particles.update(0.02, geometry, 200);
            expect(positions.getX(0) - startX).toBeGreaterThan(19);
            expect(Math.abs(positions.getZ(0))).toBeLessThan(1);
        } finally {
            particles.dispose();
        }
    });

    it("shrinks cape sprites over their lifetime without changing the birth rate", () => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 403, face: 0, definition: {
            ...definition, sizeMin: 44, sizeMax: 44, lifeMin: 0.06, lifeMax: 0.06,
            rateMin: 50, rateMax: 50, sizeOverLife: { target: 4, lifetimePercent: 100 },
        } };
        const particles = new ModelParticles(scene, [emitter], new Map());
        try {
            const geometry = { positions: Array<number>(9).fill(0), indices: [0, 1, 2] };
            const sizes = (scene.children[0] as THREE.Points).geometry.getAttribute("particleSize");
            particles.update(0.02, geometry, 200);
            expect(sizes.getX(0)).toBe(44);
            expect(sizes.getX(1)).toBe(0);
            particles.update(0.02, geometry, 200);
            expect(sizes.getX(0)).toBeCloseTo(30.669921875);
            expect(sizes.getX(1)).toBe(44);
        } finally {
            particles.dispose();
        }
    });

    it.each([[true, -80], [false, -900 / 4096]] as const)("applies a constant force without an attachment (direct=%s)", (directDisplacement, expected) => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 403, face: 0, definition: {
            ...definition, lifeMin: 1, lifeMax: 1, rateMin: 50, rateMax: 50, speedMin: 1, speedMax: 1,
            constantEffectors: [{ x: 0, y: 16384, z: 0, coneAngle: 0, falloff: 2, attenuation: 1, scope: 2,
                directDisplacement, radial: true, reverse: true }],
        } };
        const geometry = { positions: Array<number>(9).fill(0), indices: [0, 1, 2] };
        const particles = new ModelParticles(scene, [emitter], new Map());
        try {
            particles.update(0.02, geometry, 200);
            particles.update(0.02, geometry, 200);
            const positions = (scene.children[0] as THREE.Points).geometry.getAttribute("position");
            expect(positions.getY(0)).toBeCloseTo(expected);
        } finally {
            particles.dispose();
        }
    });

    it.each([
        [1, 1000 - 500 * 6 * 125 * 50 / 262144],
        [2, 1000 - 500 * 6 * 250000 * 50 / 268435456],
    ])("damps the spray away from the emitter centroid with drag mode %i", (mode, expected) => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 9, face: 0, definition: {
            ...definition, lifeMin: 1, lifeMax: 1, rateMin: 20, rateMax: 20,
            speedMin: 10000, speedMax: 10000, speedDrag: { mode, strength: 6 },
        } };
        const geometry = { positions: [0, 0, 0, 0, 0.5, 0, 0, 0, 0.5], indices: [0, 1, 2] };
        const particles = new ModelParticles(scene, [emitter], new Map());
        try {
            const positions = (scene.children[0] as THREE.Points).geometry.getAttribute("position");
            particles.update(0.05, geometry, 200);
            particles.update(0.05, geometry, 200);
            expect(positions.getX(0)).toBe(500);
            particles.update(0.05, geometry, 200);
            expect(positions.getX(0)).toBeCloseTo(expected, 3);
        } finally {
            particles.dispose();
        }
    });

    it("shows inward motion from the radial effector even when a render step exceeds particle lifetime", () => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 394, face: 0, definition: {
            ...definition, lifeMin: 0.04, lifeMax: 0.045, rateMin: 500, rateMax: 500,
            speedMin: 16000, speedMax: 20000, localEffectorIds: [17],
        } };
        // Model 57543, emitter face 2866 and its Class64 anchor.
        const source = {
            positions: [-202, -1581, -1536, 202, -1581, -1536, 0, -1581, -1472, 0, -971, -256],
            indices: [0, 1, 2], faceColors: [127],
            effectors: [{ effectorId: 17, vertex: 3, definition: {
                x: 81920, y: 81920, z: 81920, coneAngle: 1023, falloff: 1, attenuation: 618, scope: 0,
                directDisplacement: true, radial: true, reverse: true,
            } }],
        };
        const particles = new ModelParticles(scene, [emitter], new Map());
        try {
            const geometry = prepareCacheModel(source);
            particles.update(0.05, geometry, 200);
            const points = (scene.children[0] as THREE.Points).geometry;
            const positions = points.getAttribute("position");
            const colors = points.getAttribute("particleColor");
            const anchor = new THREE.Vector3(0, 971, -256);
            const activeDistances: number[] = [];
            for (let i = 0; i < positions.count; i++) {
                if (colors.getW(i) > 0) activeDistances.push(new THREE.Vector3().fromBufferAttribute(positions, i).distanceTo(anchor));
            }
            expect(Math.min(...activeDistances)).toBeLessThan(800);
            expect(Math.max(...activeDistances)).toBeGreaterThan(1300);
        } finally {
            particles.dispose();
        }
    });

    it("moves live particles only while the animated effector vertex puts them in its cone", () => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 7, face: 0, definition: { ...definition, lifeMin: 1, lifeMax: 1, rateMin: 50, rateMax: 50, localEffectorIds: [1] } };
        const force = { x: 16384, y: 0, z: 0, coneAngle: 0, falloff: 0, attenuation: 1, scope: 0, directDisplacement: true, radial: false, reverse: false };
        const source = {
            positions: [0, 0, 0, 0, 0, 8, 0, 8, 0, -8, 0, 0, -8, 0, 0],
            indices: [0, 1, 2], faceColors: [127],
            effectors: [{ effectorId: 1, vertex: 3, definition: force }, { effectorId: 2, vertex: 4, definition: force }],
        };
        const particles = new ModelParticles(scene, [emitter], new Map());
        const geometry = prepareCacheModel(source);
        const positions = (scene.children[0] as THREE.Points).geometry.getAttribute("position");
        particles.update(0.02, geometry, 200);
        expect(positions.getX(0)).toBe(0);
        particles.update(0.02, geometry, 200);
        // Client fixed-point displacement: 16384 * 20 ms / 4096 = 80 cache units.
        expect(positions.getX(0)).toBe(80);
        const posed = poseModel(source.positions, [0, 0, 0, 1, 0], {
            skeleton: { types: [1], groups: [[1]] },
            frame: { skeletonId: 1, transforms: [{ slot: 0, x: 208, y: 0, z: 0, pivotSlot: -1, tweenFlags: 0 }] },
        });
        particles.update(0.02, prepareCacheModel({ ...source, positions: posed }), 200);
        expect(positions.getX(0)).toBe(80);
        particles.dispose();
    });

    it.each([
        { directDisplacement: true, radial: true, reverse: true, falloff: 0, x: -16384, expected: -80 },
        { directDisplacement: false, radial: false, reverse: false, falloff: 0, x: 16384, expected: 800 / 4096 },
        { directDisplacement: true, radial: false, reverse: false, falloff: 1, x: 1, expected: 10 / 4096 },
        { directDisplacement: true, radial: false, reverse: false, falloff: 2, x: 1, expected: 0 },
    ])("uses client force units and falloff $falloff for radial=$radial, direct=$directDisplacement", ({ directDisplacement, radial, reverse, falloff, x, expected }) => {
        const scene = new THREE.Scene();
        const emitter = { emitterId: 7, face: 0, definition: { ...definition, lifeMin: 1, lifeMax: 1, rateMin: 50, rateMax: 50, speedMin: 1, speedMax: 1, localEffectorIds: [1] } };
        const force = { x, y: 0, z: 0, coneAngle: 0, falloff, attenuation: 1, scope: 0, directDisplacement, radial, reverse };
        const geometry = prepareCacheModel({
            positions: [0, 0, 0, 0, 0, 0, 0, 0, 0, -8, 0, 0], indices: [0, 1, 2], faceColors: [127],
            effectors: [{ effectorId: 1, vertex: 3, definition: force }],
        });
        const particles = new ModelParticles(scene, [emitter], new Map());
        particles.update(0.02, geometry, 200);
        particles.update(0.02, geometry, 200);
        const positions = (scene.children[0] as THREE.Points).geometry.getAttribute("position");
        expect(positions.getX(0)).toBeCloseTo(expected);
        particles.dispose();
    });

    it("spawns on the selected expanded face and uses its current posed coordinates",()=>{
        const scene = new THREE.Scene(), texture = new THREE.Texture();
        const particles=new ModelParticles(scene,[attachment],new Map([[744,texture]]));
        const geometry = prepareCacheModel(model);
        expect(geometry.opacities?.slice(3)).toEqual([0,0,0]);
        particles.update(0.016,geometry,200);
        const points=scene.children[0] as THREE.Points<THREE.BufferGeometry,THREE.ShaderMaterial>;
        expect(points.material.uniforms.sprite.value).toBe(texture);
        expect(points.material.uniforms.textured.value).toBe(true);
        const colors=points.geometry.getAttribute("particleColor"), positions=points.geometry.getAttribute("position");
        expect(colors.getW(0)).toBeGreaterThan(0);
        expect(positions.getX(0)).toBeGreaterThanOrEqual(100);
        expect(positions.getX(0)).toBeLessThanOrEqual(101);
        const posed=prepareCacheModel({...model,positions:model.positions.map((value,index)=>index%3===0?value+1000:value)});
        particles.update(0.1,posed,400);
        for(let i=0;i<positions.count;i++) if(colors.getW(i)>0) expect(positions.getX(i)).toBeGreaterThanOrEqual(1100);
        expect(points.material.uniforms.pixelScale.value).toBe(400);
        particles.dispose();
    });
    it("preserves source tint through linear rendering and normalizes only HDR sprite brightness",()=>{
        const scene=new THREE.Scene();
        const texture=new THREE.DataTexture(new Float32Array([16,16,16,0.25]),1,1,THREE.RGBAFormat,THREE.FloatType);
        const particles=new ModelParticles(scene,[attachment],new Map([[744,texture]]));
        particles.update(0.016,prepareCacheModel(model),200);
        const points=scene.children[0] as THREE.Points<THREE.BufferGeometry,THREE.ShaderMaterial>;
        const colors=points.geometry.getAttribute("particleColor");
        const displayed=new THREE.Color(colors.getX(0),colors.getY(0),colors.getZ(0)).convertLinearToSRGB();
        expect([displayed.r,displayed.g,displayed.b].map(channel=>Math.round(channel*255))).toEqual([255,204,34]);
        expect(colors.getW(0)).toBe(1);
        expect(points.material.uniforms.normalizeHdr.value).toBe(true);
        expect(Array.from(texture.image.data)).toEqual([16,16,16,0.25]);
        particles.dispose();
        const ordinary=new ModelParticles(scene,[attachment],new Map([[744,new THREE.Texture()]]));
        expect((scene.children[0] as THREE.Points<THREE.BufferGeometry,THREE.ShaderMaterial>).material.uniforms.normalizeHdr.value).toBe(false);
        ordinary.dispose();
    });
    it("bounds capacity and disposes surfaces once without disposing borrowed textures",()=>{
        const scene=new THREE.Scene(), texture=new THREE.Texture();
        const releaseTexture=vi.spyOn(texture,"dispose");
        const particles=new ModelParticles(scene,[{...attachment,definition:{...definition,rateMax:1000000,lifeMax:1000}}],new Map([[744,texture]]));
        const points=scene.children[0] as THREE.Points<THREE.BufferGeometry,THREE.ShaderMaterial>;
        const geometry=vi.spyOn(points.geometry,"dispose"),material=vi.spyOn(points.material,"dispose");
        expect(points.geometry.getAttribute("position").count).toBeLessThanOrEqual(4096);
        particles.dispose(); particles.dispose();
        expect(scene.children).toHaveLength(0);expect(geometry).toHaveBeenCalledTimes(1);expect(material).toHaveBeenCalledTimes(1);expect(releaseTexture).not.toHaveBeenCalled();
    });
    it("does not fabricate unavailable emitters and has a visible-sprite fallback for missing textures",()=>{
        const scene=new THREE.Scene();const missing=new ModelParticles(scene,[{...attachment,definition:null}],new Map());
        expect(scene.children).toHaveLength(0);missing.dispose();
        const particles=new ModelParticles(scene,[attachment],new Map());
        const points=scene.children[0] as THREE.Points<THREE.BufferGeometry,THREE.ShaderMaterial>;
        expect(points.material.uniforms.textured.value).toBe(false);particles.dispose();
    });
});
