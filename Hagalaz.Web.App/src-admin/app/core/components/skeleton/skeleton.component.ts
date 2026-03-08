import { ChangeDetectionStrategy, Component, input } from "@angular/core";
import { CommonModule } from "@angular/common";

@Component({
    selector: "admin-skeleton",
    standalone: true,
    imports: [CommonModule],
    template: `
        <div class="skeleton" 
             [style.width]="width()" 
             [style.height]="height()"
             [style.border-radius]="shape() === 'circle' ? '50%' : '4px'"
             [class.shimmer]="animate()">
        </div>
    `,
    styles: [`
        :host {
            display: inline-block;
            vertical-align: middle;
            width: 100%;
        }

        .skeleton {
            background: rgba(251, 191, 36, 0.05);
            position: relative;
            overflow: hidden;
            width: 100%;
            height: 100%;
            min-height: 1em;
        }

        .shimmer::after {
            content: "";
            position: absolute;
            top: 0;
            right: 0;
            bottom: 0;
            left: 0;
            transform: translateX(-100%);
            background: linear-gradient(
                90deg,
                transparent 0%,
                rgba(251, 191, 36, 0.08) 50%,
                transparent 100%
            );
            animation: shimmer 2s infinite;
        }

        @keyframes shimmer {
            100% {
                transform: translateX(100%);
            }
        }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkeletonComponent {
    width = input<string>("100%");
    height = input<string>("1em");
    shape = input<"rect" | "circle">("rect");
    animate = input<boolean>(true);
}
