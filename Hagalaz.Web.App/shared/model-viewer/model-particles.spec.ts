import * as THREE from "three";
import {describe,expect,it,vi} from "vitest";
import {ModelParticles, type ParticleDefinition} from "./model-particles";
import {prepareCacheModel} from "./cache-model-geometry";
const definition: ParticleDefinition = {textureId:744,colorMin:0xffffcc22,colorMax:0xffffcc22,lifeMin:0.05,lifeMax:0.06,rateMin:500,rateMax:500,sizeMin:40,sizeMax:48,speedMin:0,speedMax:0,angles:[0,0,0,0],unsupportedOpcodes:[]};
const attachment = {emitterId:403,face:1,definition};
const model = {positions:[0,0,0,1,0,0,0,1,0, 100,0,0,101,0,0,100,1,0],indices:[0,1,2,3,4,5],faceColors:[127,20287],faceAlpha:[0,255],particles:[attachment]};

describe("model-attached particles",()=>{
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
