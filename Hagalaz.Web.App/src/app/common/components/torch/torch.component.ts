import { ChangeDetectionStrategy, Component, OnInit, input } from "@angular/core";
import { NgOptimizedImage } from "@angular/common";

@Component({
    selector: "storm-torch",
    imports: [NgOptimizedImage],
    templateUrl: "./torch.component.html",
    styleUrls: ["./torch.component.scss"],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TorchComponent {
    readonly torchSrc = input("");
}
