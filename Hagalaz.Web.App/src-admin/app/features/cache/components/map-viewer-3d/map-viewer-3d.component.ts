import { 
    ChangeDetectionStrategy, 
    Component, 
    ElementRef, 
    afterNextRender, 
    inject, 
    input, 
    OnDestroy, 
    viewChild, 
    effect,
    signal
} from "@angular/core";
import { MatIconModule } from "@angular/material/icon";
import { MatButtonModule } from "@angular/material/button";
import { MatTooltipModule } from "@angular/material/tooltip";
import { MatButtonToggleModule } from "@angular/material/button-toggle";
import * as THREE from "three";
import { OrbitControls } from "three/examples/jsm/controls/OrbitControls.js";
import { MapTypeDto } from "../../services/cache.models";

@Component({
    selector: "admin-map-viewer-3d",
    standalone: true,
    imports: [MatIconModule, MatButtonModule, MatTooltipModule, MatButtonToggleModule],
    template: `
        <div class="relative w-full h-full min-h-[500px] rounded-xl overflow-hidden border border-storm-gold/10 bg-black/40 shadow-inner group/viewer">
            <canvas #canvas class="w-full h-full block cursor-move"></canvas>
            
            <!-- Top Left: Status & Projection Info -->
            <div class="absolute top-4 left-4 flex flex-col gap-2 pointer-events-none">
                <div class="px-3 py-1.5 rounded bg-black/60 backdrop-blur border border-storm-gold/20 text-[10px] font-mono text-storm-gold uppercase tracking-widest pointer-events-auto">
                    3D Projection Engine
                </div>
                @if (mapData()) {
                    <div class="px-3 py-1 rounded bg-black/40 backdrop-blur border border-white/5 text-[9px] font-mono text-storm-text/60 pointer-events-auto">
                        ID: #{{ mapData()?.id }} | Objects: {{ mapData()?.objectCount }}
                    </div>
                }
                @if (!sceneReady()) {
                    <div class="px-3 py-1 rounded bg-rose-500/20 text-rose-300 text-[9px] font-bold uppercase animate-pulse pointer-events-auto">
                        Initializing Core...
                    </div>
                }
            </div>

            <!-- Top Right: Visual Controls Overlay -->
            <div class="absolute top-4 right-4 flex flex-col gap-2 opacity-0 group-hover/viewer:opacity-100 transition-opacity duration-300">
                <div class="p-2 rounded-xl bg-black/60 backdrop-blur border border-storm-gold/10 flex flex-col gap-2 shadow-2xl">
                    <!-- Plane Selector -->
                    <div class="flex flex-col gap-1 px-1 mb-1">
                        <span class="text-[8px] font-black uppercase text-storm-gold/40 tracking-tighter">Render Plane</span>
                        <mat-button-toggle-group [value]="currentPlane()" (change)="currentPlane.set($event.value)" class="runic-toggle-group">
                            <mat-button-toggle [value]="0">0</mat-button-toggle>
                            <mat-button-toggle [value]="1">1</mat-button-toggle>
                            <mat-button-toggle [value]="2">2</mat-button-toggle>
                            <mat-button-toggle [value]="3">3</mat-button-toggle>
                            <mat-button-toggle [value]="-1" matTooltip="All Planes">A</mat-button-toggle>
                        </mat-button-toggle-group>
                    </div>

                    <div class="h-px bg-white/5 mx-1"></div>

                    <!-- Feature Toggles -->
                    <div class="grid grid-cols-3 gap-1">
                        <button mat-icon-button [class.active-toggle]="showTerrain()" (click)="showTerrain.set(!showTerrain())" 
                                matTooltip="Toggle Terrain" class="runic-viewer-btn">
                            <mat-icon class="!text-sm">{{ showTerrain() ? 'landscape' : 'grid_off' }}</mat-icon>
                        </button>
                        <button mat-icon-button [class.active-toggle]="showObjects()" (click)="showObjects.set(!showObjects())" 
                                matTooltip="Toggle Objects" class="runic-viewer-btn">
                            <mat-icon class="!text-sm">{{ showObjects() ? 'view_in_ar' : 'layers_clear' }}</mat-icon>
                        </button>
                        <button mat-icon-button [class.active-toggle]="showWireframe()" (click)="showWireframe.set(!showWireframe())" 
                                matTooltip="Toggle Wireframe" class="runic-viewer-btn">
                            <mat-icon class="!text-sm">grid_view</mat-icon>
                        </button>
                    </div>
                </div>
            </div>

            <div class="absolute bottom-4 right-4 text-[9px] font-mono text-storm-text/20 uppercase tracking-tighter pointer-events-none">
                Orbit: Left | Pan: Right | Zoom: Scroll
            </div>
        </div>
    `,
    styles: [`
        :host { display: block; width: 100%; height: 100%; }
        
        .runic-viewer-btn {
            --mdc-icon-button-state-layer-size: 32px;
            width: 32px !important;
            height: 32px !important;
            padding: 0 !important;
            color: rgba(247, 232, 180, 0.3) !important;
            transition: all 0.2s ease;
            
            &:hover { color: #fbbf24 !important; background: rgba(251, 191, 36, 0.05) !important; }
            &.active-toggle { color: #fbbf24 !important; }
        }

        .runic-toggle-group {
            border: none !important;
            background: transparent !important;
            height: 24px;
            
            ::ng-deep .mat-button-toggle {
                background-color: transparent;
                color: rgba(247, 232, 180, 0.4);
                border: 1px solid rgba(251, 191, 36, 0.1);
                font-family: var(--font-mono);
                font-size: 9px;
                line-height: 22px;
                
                &:first-child { border-radius: 4px 0 0 4px; }
                &:last-child { border-radius: 0 4px 4px 0; }
                
                &.mat-button-toggle-checked {
                    background-color: rgba(251, 191, 36, 0.15);
                    color: #fbbf24;
                    border-color: rgba(251, 191, 36, 0.4);
                }
            }
        }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MapViewer3dComponent implements OnDestroy {
    mapData = input<MapTypeDto | null>(null);
    terrainFlags = input<Int8Array | null>(null);
    terrainHeights = input<Int16Array | null>(null);
    canvas = viewChild<ElementRef<HTMLCanvasElement>>("canvas");

    sceneReady = signal(false);
    currentPlane = signal<number>(0);
    showTerrain = signal(true);
    showObjects = signal(true);
    showWireframe = signal(false);

    private renderer?: THREE.WebGLRenderer;
    private scene?: THREE.Scene;
    private camera?: THREE.PerspectiveCamera;
    private controls?: OrbitControls;
    private frameId?: number;
    
    private objectsGroup = new THREE.Group();
    private terrainGroup = new THREE.Group();

    constructor() {
        afterNextRender(() => {
            try {
                this.initThree();
                this.sceneReady.set(true);
                this.animate();
                
                const data = this.mapData();
                if (data) {
                    this.renderScene(data, this.terrainFlags(), this.terrainHeights());
                }
            } catch (err) {
                console.error("Failed to initialize 3D Engine:", err);
            }
        });

        effect(() => {
            const data = this.mapData();
            const flags = this.terrainFlags();
            const heights = this.terrainHeights();
            
            // Track dependencies for re-render
            this.currentPlane();
            this.showTerrain();
            this.showObjects();
            this.showWireframe();
            
            if (this.sceneReady()) {
                this.renderScene(data, flags, heights);
            }
        });
    }

    ngOnDestroy() {
        if (this.frameId) {
            cancelAnimationFrame(this.frameId);
        }
        window.removeEventListener("resize", this.onResize);
        this.renderer?.dispose();
        this.disposeGroup(this.objectsGroup);
        this.disposeGroup(this.terrainGroup);
    }

    private disposeGroup(group: THREE.Group) {
        group.traverse((child) => {
            if (child instanceof THREE.Mesh) {
                child.geometry.dispose();
                if (Array.isArray(child.material)) {
                    child.material.forEach(m => m.dispose());
                } else {
                    child.material.dispose();
                }
            }
        });
        group.clear();
    }

    private initThree() {
        const canvasEl = this.canvas()?.nativeElement;
        if (!canvasEl) throw new Error("Canvas element not found");

        this.scene = new THREE.Scene();
        this.scene.background = new THREE.Color(0x0b0704);
        this.scene.fog = new THREE.FogExp2(0x0b0704, 0.001);

        const rect = canvasEl.getBoundingClientRect();
        this.camera = new THREE.PerspectiveCamera(60, rect.width / rect.height, 0.1, 20000);
        this.camera.position.set(128, 128, 128);
        this.camera.lookAt(32, 0, 32);

        this.renderer = new THREE.WebGLRenderer({
            canvas: canvasEl,
            antialias: true,
            alpha: true
        });
        this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
        this.renderer.setSize(rect.width, rect.height);

        this.controls = new OrbitControls(this.camera, canvasEl);
        this.controls.enableDamping = true;
        this.controls.dampingFactor = 0.05;
        this.controls.target.set(32, 0, 32);

        const ambientLight = new THREE.AmbientLight(0xffffff, 0.6);
        this.scene.add(ambientLight);

        const mainLight = new THREE.DirectionalLight(0xfbbf24, 1.2);
        mainLight.position.set(100, 200, 100);
        this.scene.add(mainLight);

        const fillLight = new THREE.DirectionalLight(0x3b82f6, 0.4);
        fillLight.position.set(-100, 50, -100);
        this.scene.add(fillLight);

        this.scene.add(this.terrainGroup);
        this.scene.add(this.objectsGroup);

        window.addEventListener("resize", this.onResize);
    }

    private onResize = () => {
        const canvasEl = this.canvas()?.nativeElement;
        if (!canvasEl || !this.camera || !this.renderer) return;
        const rect = canvasEl.parentElement?.getBoundingClientRect();
        if (!rect) return;
        this.camera.aspect = rect.width / rect.height;
        this.camera.updateProjectionMatrix();
        this.renderer.setSize(rect.width, rect.height);
    };

    private animate() {
        this.frameId = requestAnimationFrame(() => this.animate());
        this.controls?.update();
        if (this.scene && this.camera && this.renderer) {
            this.renderer.render(this.scene, this.camera);
        }
    }

    private renderScene(data: MapTypeDto | null, flags: Int8Array | null, heights: Int16Array | null) {
        if (!data || !this.scene) return;

        this.disposeGroup(this.objectsGroup);
        this.disposeGroup(this.terrainGroup);

        const planeWidth = data.terrainWidth || 64;
        const planeHeight = data.terrainHeight || 64;
        const planes = data.terrainPlanes || 4;
        const activePlane = this.currentPlane();
        
        this.terrainGroup.visible = this.showTerrain();
        this.objectsGroup.visible = this.showObjects();

        for (let p = 0; p < planes; p++) {
            if (activePlane !== -1 && p !== activePlane) continue;

            const geometry = new THREE.PlaneGeometry(planeWidth, planeHeight, planeWidth - 1, planeHeight - 1);
            geometry.rotateX(-Math.PI / 2);

            const posAttr = geometry.attributes['position'];
            const vertices = posAttr.array as Float32Array;
            const colors = [];
            
            for (let i = 0; i < vertices.length; i += 3) {
                const vx = Math.min(Math.max(Math.round(vertices[i] + planeWidth / 2), 0), planeWidth - 1);
                const vy = Math.min(Math.max(Math.round(planeHeight / 2 - vertices[i + 2]), 0), planeHeight - 1);
                
                const tileIdx = (p * planeWidth * planeHeight) + (vx * planeHeight) + vy;
                const heightValue = heights ? (heights[tileIdx] || 0) : 0;
                const flagValue = flags ? (flags[tileIdx] || 0) : 0;

                vertices[i + 1] = (heightValue * -0.05) + (p * 8); 

                const color = new THREE.Color();
                if (p > 0 && activePlane === -1) color.setHex(0x3b82f6);
                else if (flagValue & 0x1) color.setHex(0x1e40af);
                else if (flagValue & 0x4) color.setHex(0xa8a29e);
                else if (heightValue < -10) color.setHex(0x14532d);
                else if (heightValue > 15) color.setHex(0x78350f);
                else color.setHex(0x166534);
                
                const n = (Math.random() * 0.05);
                color.r += n; color.g += n; color.b += n;
                colors.push(color.r, color.g, color.b);
            }
            posAttr.needsUpdate = true;
            geometry.setAttribute('color', new THREE.Float32BufferAttribute(colors, 3));
            geometry.computeVertexNormals();

            const material = new THREE.MeshPhongMaterial({
                vertexColors: true,
                wireframe: this.showWireframe(),
                transparent: true,
                opacity: p === activePlane || activePlane === -1 ? 1.0 : 0.2,
                side: THREE.DoubleSide
            });

            const terrainMesh = new THREE.Mesh(geometry, material);
            terrainMesh.position.set(planeWidth / 2, 0, planeHeight / 2);
            this.terrainGroup.add(terrainMesh);

            if (p === activePlane || activePlane === -1) {
                const grid = new THREE.GridHelper(planeWidth, planeWidth, 0xfbbf24, 0x1d160c);
                grid.position.set(planeWidth / 2, (p * 8) + 0.05, planeHeight / 2);
                grid.material.opacity = 0.05;
                grid.material.transparent = true;
                this.terrainGroup.add(grid);
            }
        }

        const boxGeometry = new THREE.BoxGeometry(0.6, 1.2, 0.6);
        const boxMaterial = new THREE.MeshPhongMaterial({ 
            color: 0xfbbf24,
            emissive: 0xfbbf24,
            emissiveIntensity: 0.5
        });

        data.objects.forEach(obj => {
            if (activePlane !== -1 && obj.z !== activePlane) return;

            const mesh = new THREE.Mesh(boxGeometry, boxMaterial);
            const tileIdx = (obj.z * planeWidth * planeHeight) + (obj.x * planeHeight) + obj.y;
            const heightValue = heights ? (heights[tileIdx] || 0) : 0;
            const yPos = (heightValue * -0.05) + (obj.z * 8) + 0.6;

            mesh.position.set(obj.x + 0.5, yPos, obj.y + 0.5);
            mesh.rotation.y = (obj.rotation * Math.PI) / 2;
            this.objectsGroup.add(mesh);
        });
    }
}
