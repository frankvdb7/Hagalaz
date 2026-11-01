import { Component, input, model, output } from "@angular/core";
import { FormsModule } from "@angular/forms";
import { MatButtonModule } from "@angular/material/button";
import { MatFormFieldModule } from "@angular/material/form-field";
import { MatInputModule } from "@angular/material/input";
import { RouterLink } from "@angular/router";
import type { GetAnimationSequenceDto } from "../services/dtos";

@Component({
    selector: "cache-model-animations",
    imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, RouterLink],
    templateUrl: "./model-animations.component.html",
    host: { class: "block" },
    styles: `mat-form-field { width: 160px; }`,
})
export class ModelAnimationsComponent {
    readonly animations = input<readonly number[]>([]);
    readonly hasSkins = input(false);
    readonly sequence = input<GetAnimationSequenceDto>();
    readonly frameIndex = input(0);
    readonly playing = input(false);
    readonly loading = input(false);
    readonly sequenceError = input<string | null>(null);
    readonly clipError = input<string | null>(null);
    readonly frameError = input<string | null>(null);
    readonly poseError = input<string | null>(null);
    readonly frameId = model(0);
    readonly playAnimation = output<number>();
    readonly inspectFrame = output<void>();
    readonly basePose = output<void>();
    readonly togglePlayback = output<void>();
    readonly nextFrame = output<void>();
    readonly retryClip = output<void>();
    readonly retryFrame = output<void>();
}
