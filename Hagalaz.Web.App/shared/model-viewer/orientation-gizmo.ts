import * as THREE from "three";

export const axisColors = { x: "#ff4242", up: "#72df43", north: "#45a8ff" } as const;

/** Named as RuneScape map coordinates, where Z is the plane and Y runs north-south. */
const axes = [
    { label: "X", color: axisColors.x, direction: [1, 0, 0] },
    { label: "Z", color: axisColors.up, direction: [0, 1, 0] },
    { label: "Y", color: axisColors.north, direction: [0, 0, 1] },
] as const;

export class OrientationGizmo {
    private readonly root = new THREE.Group();
    private readonly camera = new THREE.OrthographicCamera(-1.7, 1.7, 1.7, -1.7, 0, 10);
    private readonly viewport = new THREE.Vector4();
    private readonly cube = new THREE.Mesh(
        new THREE.BoxGeometry(0.45, 0.45, 0.45),
        [0x777d86, 0x777d86, 0x9ba2ac, 0x9ba2ac, 0x5f6773, 0x5f6773].map(color => new THREE.MeshBasicMaterial({ color })),
    );
    private readonly arrows: THREE.ArrowHelper[] = [];
    private readonly labels: THREE.Sprite[] = [];

    constructor(private readonly sceneCamera: THREE.Camera) {
        this.camera.position.z = 5;
        this.root.add(this.cube);
        for (const { label, color, direction } of axes) {
            const axis = new THREE.Vector3(...direction);
            const arrow = new THREE.ArrowHelper(axis, new THREE.Vector3(), 1, color, 0.23, 0.23);
            this.arrows.push(arrow);
            this.root.add(arrow);

            const canvas = document.createElement("canvas");
            canvas.width = canvas.height = 64;
            const context = canvas.getContext("2d");
            if (!context) continue;
            context.font = "bold 50px sans-serif";
            context.textAlign = "center";
            context.textBaseline = "middle";
            context.strokeStyle = "#101722";
            context.lineWidth = 5;
            context.strokeText(label, 32, 33);
            context.fillStyle = color;
            context.fillText(label, 32, 33);
            const map = new THREE.CanvasTexture(canvas);
            map.colorSpace = THREE.SRGBColorSpace;
            const sprite = new THREE.Sprite(new THREE.SpriteMaterial({ map, depthTest: false }));
            sprite.position.copy(axis).multiplyScalar(1.36);
            sprite.scale.setScalar(0.46);
            this.labels.push(sprite);
            this.root.add(sprite);
        }
    }

    render(renderer: THREE.WebGLRenderer): void {
        this.root.quaternion.copy(this.sceneCamera.quaternion).invert();
        renderer.getViewport(this.viewport);
        const { x, y, z: width, w: height } = this.viewport;
        const size = Math.min(110, width, height);
        renderer.clearDepth();
        renderer.setViewport(x + width - size, y + height - size, size, size);
        try {
            renderer.render(this.root, this.camera);
        } finally {
            renderer.setViewport(this.viewport);
        }
    }

    dispose(): void {
        this.cube.geometry.dispose();
        for (const material of this.cube.material) material.dispose();
        for (const arrow of this.arrows) arrow.dispose();
        for (const label of this.labels) {
            label.material.map?.dispose();
            label.material.dispose();
        }
    }
}
