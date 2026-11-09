/** Revision-742 Class47 volumes, in x-fastest texture order. */
export type WaterVolume = "ridged" | "lake" | "normal";
export const waterVolumeSize = 128;
export const waterVolumeSlices = 16;

const size = waterVolumeSize;
const slices = waterVolumeSlices;
const sliceArea = size * size;
const unit = 4096;
const f32 = Math.fround;

function javaRandom(seed: number): () => number {
    const multiplier = 0x5deece66dn;
    const mask = (1n << 48n) - 1n;
    let state = (BigInt(seed) ^ multiplier) & mask;
    return () => {
        state = (state * multiplier + 11n) & mask;
        return Number(state >> 16n) | 0;
    };
}

function fixedPermutation(seed: number): Uint8Array {
    const next = javaRandom(seed);
    const table = new Uint8Array(512);
    for (let index = 0; index < 255; index++) table[index] = index;
    for (let index = 255; index > 0; index--) {
        let swap: number;
        if ((index & (index - 1)) === 0) {
            swap = Math.floor((next() >>> 0) * index / 0x100000000);
        } else {
            // Class9.method423 uses a signed rejection threshold and Class144.method1667.
            const limit = (-0x80000000 - (0x100000000 % index)) | 0;
            let value: number;
            do value = next(); while (value >= limit);
            swap = ((value >> 31) & (index - 1)) + (value + (value >>> 31)) % index;
        }
        const value = table[swap];
        table[swap] = table[index];
        table[index] = table[index + 256] = value;
    }
    return table;
}

const fadeFixed = Int32Array.from({ length: unit }, (_, value) => {
    const cube = value * (value * value >> 12) >> 12;
    return (40960 + ((value * 6 - 61440) * value >> 12)) * cube >> 12;
});

function mixFixed(from: number, to: number, weight: number): number {
    return from + (weight * (to - from) >> 12);
}

function fixedGradient(hash: number, x: number, y: number, z: number): number {
    const index = hash & 15;
    const first = index < 8 ? x : y;
    const second = index < 4 ? y : index === 12 || index === 14 ? x : z;
    return (index & 1 ? -first : first) + (index & 2 ? -second : second);
}

function fixedNoise(table: Uint8Array, x: number, y: number, z: number, periodX: number, periodY: number, periodZ: number): number {
    const nextX = ((x >> 12) + 1) % periodX & 255;
    const nextY = ((y >> 12) + 1) % periodY & 255;
    const nextZ = ((z >> 12) + 1) % periodZ & 255;
    const cellX = x >> 12 & 255;
    const cellY = y >> 12 & 255;
    const cellZ = z >> 12 & 255;
    x &= 4095;
    y &= 4095;
    z &= 4095;
    const xWeight = fadeFixed[x];
    const yWeight = fadeFixed[y];
    const zWeight = fadeFixed[z];
    const firstZ = table[cellZ];
    const secondZ = table[nextZ];
    const firstY = table[cellY + firstZ];
    const secondY = table[nextY + firstZ];
    const thirdY = table[cellY + secondZ];
    const fourthY = table[nextY + secondZ];
    const near = mixFixed(
        mixFixed(fixedGradient(table[cellX + firstY], x, y, z), fixedGradient(table[nextX + firstY], x - unit, y, z), xWeight),
        mixFixed(fixedGradient(table[cellX + secondY], x, y - unit, z), fixedGradient(table[nextX + secondY], x - unit, y - unit, z), xWeight),
        yWeight,
    );
    const far = mixFixed(
        mixFixed(fixedGradient(table[cellX + thirdY], x, y, z - unit), fixedGradient(table[nextX + thirdY], x - unit, y, z - unit), xWeight),
        mixFixed(fixedGradient(table[cellX + fourthY], x, y - unit, z - unit), fixedGradient(table[nextX + fourthY], x - unit, y - unit, z - unit), xWeight),
        yWeight,
    );
    return mixFixed(near, far, zWeight);
}

const ridgeGain = Math.trunc(0.5 ** -f32(0.1) * unit);
const ridgeOffset = Math.trunc(f32(0.55) * unit);
const lakeWeights = Array.from({ length: 5 }, (_, octave) => Math.trunc(f32(0.45) ** octave * unit));

function ridgedVoxel(table: Uint8Array, x: number, y: number, z: number): number {
    let sum = 0;
    let previousRidge = unit;
    let amplitude = unit;
    for (let octave = 0; octave < 5; octave++) {
        const period = 8 << octave;
        const periodZ = 2 << octave;
        const noise = fixedNoise(table, x * period, y * period, z * periodZ, period, period, periodZ);
        const distance = ridgeOffset - Math.abs(noise);
        const ridge = distance * distance >> 12;
        previousRidge = ridge * Math.min(unit, previousRidge * 3) >> 12;
        sum += previousRidge * amplitude >> 12;
        amplitude = amplitude * ridgeGain >> 12;
    }
    return 3 * Math.min(255, sum >> 4) >> 5;
}

function lakeVoxel(table: Uint8Array, x: number, y: number, z: number): number {
    let sum = 0;
    for (let octave = 0; octave < 5; octave++) {
        const periodX = 16 << octave;
        const period = 2 << octave;
        const noise = fixedNoise(table, x * periodX, y * period, z * period, periodX, period, period);
        sum += noise * lakeWeights[octave] >> 12;
    }
    return 127 + (Math.min(4095, Math.abs(sum)) >> 5);
}

function* fixedVolume(kind: "ridged" | "lake"): Generator<void, Uint8Array> {
    const table = fixedPermutation(kind === "ridged" ? 8 : 12);
    const voxel = kind === "ridged" ? ridgedVoxel : lakeVoxel;
    const data = new Uint8Array(sliceArea * slices * 2);
    let offset = 0;
    for (let z = 0; z < slices; z++) {
        for (let y = 0; y < size; y++) {
            for (let x = 0; x < size; x++) {
                const value = voxel(table, x * 32, y * 32, z * 256);
                data[offset++] = value;
                data[offset++] = value;
            }
        }
        yield;
    }
    return data;
}

function floatPermutation(): Uint8Array {
    const next = javaRandom(419684);
    const table = Uint8Array.from({ length: 512 }, (_, index) => index & 255);
    for (let index = 0; index < 256; index++) {
        const swap = next() & 255;
        const value = table[swap];
        table[swap] = table[swap + 256] = table[index];
        table[index] = table[index + 256] = value;
    }
    return table;
}

function fadeFloat(value: number): number {
    return f32(f32(f32(value * value) * value) * f32(10 + f32(f32(f32(value * 6) - 15) * value)));
}

function mixFloat(from: number, to: number, weight: number): number {
    return f32(from + f32(f32(to - from) * weight));
}

function floatGradient(hash: number, x: number, y: number, z: number): number {
    const third = f32(0.333333);
    return f32(f32(f32((hash & 1 ? third : -third) * x) + f32((hash & 2 ? third : -third) * y)) + f32((hash & 4 ? third : -third) * z));
}

function* heightVolume(): Generator<void, Uint8Array> {
    const table = floatPermutation();
    const data = new Int8Array(sliceArea * slices);
    for (let z = 0; z < slices; z++) {
        let amplitude = 0.5;
        for (let octave = 0; octave < 8; octave++) {
            const period = 4 << octave;
            const mask = (period - 1) & 255;
            const cellZ = z << octave;
            const firstZ = table[cellZ & ((16 << octave) - 1 & 255)];
            // Class335_Sub1 interpolates with zFraction = 0 at every octave for this volume.
            const scale = f32(amplitude * 127);
            for (let y = 0; y < size; y++) {
                const sampleY = y * period / size;
                const cellY = Math.floor(sampleY);
                const fy = sampleY - cellY;
                const yw = fadeFloat(fy);
                const firstY = table[(cellY & mask) + firstZ];
                const secondY = table[((cellY + 1) & mask) + firstZ];
                for (let x = 0; x < size; x++) {
                    const sampleX = x * period / size;
                    const cellX = Math.floor(sampleX);
                    const fx = sampleX - cellX;
                    const xw = fadeFloat(fx);
                    const firstX = cellX & mask;
                    const secondX = (cellX + 1) & mask;
                    const near = mixFloat(
                        floatGradient(table[firstX + firstY], 1 - fx, 1 - fy, 1),
                        floatGradient(table[secondX + firstY], fx, 1 - fy, 1),
                        xw,
                    );
                    const far = mixFloat(
                        floatGradient(table[firstX + secondY], 1 - fx, fy, 1),
                        floatGradient(table[secondX + secondY], fx, fy, 1),
                        xw,
                    );
                    const offset = z * sliceArea + y * size + x;
                    data[offset] = f32(data[offset] + f32(scale * mixFloat(near, far, yw)));
                }
            }
            amplitude = f32(amplitude * f32(0.6));
        }
        yield;
    }
    const heights = new Uint8Array(data.buffer);
    for (let i = 0; i < heights.length; i++) heights[i] += 127;
    return heights;
}

function* normalVolume(): Generator<void, Uint8Array> {
    const height = yield* heightVolume();
    const data = new Uint8Array(height.length * 4);
    let output = 0;
    for (let z = 0; z < slices; z++) {
        const current = z * sliceArea;
        const previous = ((z + slices - 1) % slices) * sliceArea;
        // Class59 samples x/y at texel centres and z at slice boundaries with GL_LINEAR and repeat.
        const sample = (x: number, y: number) => {
            const index = (y & (size - 1)) * size + (x & (size - 1));
            return height[current + index] + height[previous + index];
        };
        for (let y = 0; y < size; y++) {
            for (let x = 0; x < size; x++) {
                const dx = (sample(x - 1, y) - sample(x + 1, y)) / 510;
                const dy = (sample(x, y - 1) - sample(x, y + 1)) / 510;
                const reciprocalRelief = 0.5;
                const scale = 0.5 / Math.hypot(dx, dy, reciprocalRelief);
                data[output++] = Math.round((0.5 + dx * scale) * 255);
                data[output++] = Math.round((0.5 + dy * scale) * 255);
                data[output++] = Math.round((0.5 + reciprocalRelief * scale) * 255);
                data[output++] = (sample(x, y) + 1) >> 1;
            }
        }
        yield;
    }
    return data;
}

const volumes = new Map<WaterVolume, Promise<Uint8Array>>();

/** Share CPU data across scenes and yield between slices so generation can coexist with rendering. */
export function waterVolume(kind: WaterVolume): Promise<Uint8Array> {
    let volume = volumes.get(kind);
    if (!volume) {
        volume = generateVolume(kind);
        volumes.set(kind, volume);
    }
    return volume;
}

async function generateVolume(kind: WaterVolume): Promise<Uint8Array> {
    const generator = kind === "normal" ? normalVolume() : fixedVolume(kind);
    let slice = generator.next();
    while (!slice.done) {
        await new Promise<void>(resolve => setTimeout(resolve, 0));
        slice = generator.next();
    }
    return slice.value;
}

function turbulenceGradient(hash: number, x: number, y: number): number {
    switch (hash & 3) {
        case 0: return y + x;
        case 1: return y - x;
        case 2: return x - y;
        default: return -x - y;
    }
}

let turbulence: Float32Array | undefined;

/** Class49_Sub4 reads at most 255 values (offset 127 + 64 cells of 2), so the first two rows of its table suffice. */
export const waterTurbulenceRows = 2;

/** Class49_Sub4 packs both table results per cell. LongNode ignores its offset argument. */
export function waterTurbulence(): Float32Array {
    if (turbulence) return turbulence;
    const result = new Float32Array(waterTurbulenceRows * 64 * 2);
    const table = fixedPermutation(0);
    const persistence = Math.trunc(f32(0.4) * unit) / unit;
    for (let octave = 0; octave < 3; octave++) {
        const period = 4 << octave;
        const weight = Math.trunc(persistence ** octave * unit);
        let offset = 0;
        for (let y = 0; y < waterTurbulenceRows; y++) {
            const py = y * 16 * period;
            const cellY = py >> 12;
            const fy = py & 4095;
            const firstY = table[cellY];
            const secondY = table[(cellY + 1) % period];
            for (let x = 0; x < 64; x++) {
                const px = x * 64 * period;
                const cellX = px >> 12;
                const nextX = (cellX + 1) % period;
                const fx = px & 4095;
                const near = mixFixed(turbulenceGradient(table[cellX + firstY], fx, fy), turbulenceGradient(table[nextX + firstY], fx - unit, fy), fadeFixed[fx]);
                const far = mixFixed(turbulenceGradient(table[cellX + secondY], fx, fy - unit), turbulenceGradient(table[nextX + secondY], fx - unit, fy - unit), fadeFixed[fx]);
                const value = (mixFixed(near, far, fadeFixed[fy]) * weight >> 12) / unit;
                result[offset++] += value;
                result[offset++] += value;
            }
        }
    }
    turbulence = result;
    return result;
}
