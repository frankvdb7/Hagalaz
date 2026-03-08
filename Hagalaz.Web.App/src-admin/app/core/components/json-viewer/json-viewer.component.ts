import { KeyValuePipe, TitleCasePipe } from "@angular/common";
import { ChangeDetectionStrategy, Component, input } from "@angular/core";

@Component({
    selector: "admin-json-viewer",
    standalone: true,
    imports: [KeyValuePipe, TitleCasePipe],
    template: `
        <div class="json-viewer-container">
            @if (isObject(data())) {
                <div class="json-object">
                    @for (item of data() | keyvalue; track item.key) {
                        <div class="json-property">
                            <span class="json-key">{{ asString(item.key) | titlecase }}:</span>
                            @if (isObject(item.value)) {
                                @defer (on idle) {
                                    <admin-json-viewer [data]="item.value" class="nested-viewer"></admin-json-viewer>
                                } @placeholder {
                                    <span class="text-[10px] opacity-30 italic">Traversing...</span>
                                }
                            } @else {
                                <span class="json-value" [class]="getValueClass(item.value)">
                                    {{ item.value }}
                                </span>
                            }
                        </div>
                    }
                </div>
            } @else {
                <span class="json-value" [class]="getValueClass(data())">
                    {{ data() }}
                </span>
            }
        </div>
    `,
    styles: [`
        .json-viewer-container {
            font-family: 'JetBrains Mono', 'Fira Code', monospace;
            font-size: 0.8rem;
            line-height: 1.4;
        }

        .json-object {
            display: flex;
            flex-direction: column;
            gap: 0.25rem;
            padding-left: 0.5rem;
            border-left: 1px solid rgba(251, 191, 36, 0.1);
        }

        .json-property {
            display: flex;
            flex-wrap: wrap;
            gap: 0.5rem;
        }

        .json-key {
            color: rgba(251, 191, 36, 0.7);
            font-weight: 500;
        }

        .json-value {
            &.string { color: #a1b522; }
            &.number { color: #ffb300; }
            &.boolean { color: #ff5449; }
            &.null { color: #9a8f81; font-style: italic; }
        }

        .nested-viewer {
            width: 100%;
            margin-top: 0.15rem;
            margin-bottom: 0.25rem;
        }
    `],
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JsonPropertyViewerComponent {
    data = input<any>();

    isObject(val: any): boolean {
        return val !== null && typeof val === "object";
    }

    asString(val: any): string {
        return String(val);
    }

    getValueClass(val: any): string {
        if (val === null) return "null";
        const type = typeof val;
        if (type === "string") return "string";
        if (type === "number") return "number";
        if (type === "boolean") return "boolean";
        return "";
    }
}
