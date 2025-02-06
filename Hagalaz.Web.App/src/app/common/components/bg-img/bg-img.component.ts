import { NgOptimizedImage } from "@angular/common";
import { ChangeDetectionStrategy, Component, inject } from "@angular/core";
import { BackgroundImageService } from "@app/services/util/background-image.service";

@Component({
    selector: "app-bg-img",
    imports: [NgOptimizedImage],
    templateUrl: "./bg-img.component.html",
    styleUrl: "./bg-img.component.scss",
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class BgImgComponent {
    bgImgService = inject(BackgroundImageService);
}
