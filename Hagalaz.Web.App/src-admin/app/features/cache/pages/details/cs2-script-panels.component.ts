import { JsonPipe } from "@angular/common";
import { Component, computed, input } from "@angular/core";
import { MatCardModule } from "@angular/material/card";
import { RouterLink } from "@angular/router";
import type { GetCs2ScriptDto } from "../../services/dtos";

/** Script counts and switch tables. */
@Component({
    selector: "cache-cs2-metadata",
    imports: [MatCardModule],
    template: `
        <mat-card appearance="outlined">
            <mat-card-header><mat-card-title>Metadata</mat-card-title></mat-card-header>
            <mat-card-content class="pt-4">
                <dl class="script-metadata">
                    <div><dt>Script ID</dt><dd>{{ script().id }}</dd></div>
                    <div><dt>Instructions</dt><dd>{{ script().opcodeCount }}</dd></div>
                    <div><dt>Switch tables</dt><dd>{{ script().switchTablesCount }}</dd></div>
                </dl>
                <table class="script-table mt-4" aria-label="Arguments and locals by type">
                    <thead><tr><th scope="col">Type</th><th scope="col">Arguments</th><th scope="col">Locals</th></tr></thead>
                    <tbody>
                        <tr><th scope="row">Int</th><td>{{ script().intArgsCount }}</td><td>{{ script().intLocalsCount }}</td></tr>
                        <tr><th scope="row">String</th><td>{{ script().stringArgsCount }}</td><td>{{ script().stringLocalsCount }}</td></tr>
                        <tr><th scope="row">Long</th><td>{{ script().longArgsCount }}</td><td>{{ script().longLocalsCount }}</td></tr>
                    </tbody>
                </table>
                @if (script().switchTables.length) {
                    <h3 class="mt-5 mb-2">Switch tables</h3>
                    @for (cases of script().switchTables; track $index; let tableIndex = $index) {
                        <details>
                            <summary>Table {{ tableIndex }} ({{ cases.length }} cases)</summary>
                            <table class="script-table" [attr.aria-label]="'Switch table ' + tableIndex">
                                <thead><tr><th scope="col">Key</th><th scope="col">Offset</th></tr></thead>
                                <tbody>
                                    @for (entry of cases; track entry.key) { <tr><td>{{ entry.key }}</td><td>{{ entry.offset }}</td></tr> }
                                </tbody>
                            </table>
                        </details>
                    }
                }
            </mat-card-content>
        </mat-card>
    `,
    styleUrls: ["./cs2-script.scss"],
    styles: `
        :host { display: block; }
        .script-metadata > div { display: flex; justify-content: space-between; gap: 1rem; }
    `,
})
export class Cs2MetadataComponent {
    readonly script = input.required<GetCs2ScriptDto>();
}

/** Decompiled source, with links to the widget components it references. */
@Component({
    selector: "cache-cs2-source",
    imports: [MatCardModule, RouterLink],
    template: `
        <mat-card appearance="outlined">
            <mat-card-header><mat-card-title>Source</mat-card-title></mat-card-header>
            <mat-card-content class="pt-4">
                @if (partial()) { <p class="mb-2">Partial source. Unresolved instructions are shown by opcode.</p> }
                @if (lines().length) {
                    <div class="source-scroll" role="region" aria-label="CS2 source" tabindex="0">
                        @for (line of lines(); track $index) {
                            <div class="source-line" [class.raw]="line.raw">
                                <span class="source-index">{{ line.number }}</span>
                                <div>
                                    <code>{{ line.text }}</code>
                                    @if (line.widget; as widget) {
                                        <a class="source-reference" [routerLink]="['/cache/widgets', widget.id]" [queryParams]="{ component: widget.component }">
                                            Open widget {{ widget.id }}:{{ widget.component }}
                                        </a>
                                    }
                                </div>
                            </div>
                        }
                    </div>
                } @else {
                    <p>This script has no instructions.</p>
                }
            </mat-card-content>
        </mat-card>
    `,
    styleUrls: ["./cs2-script.scss"],
    styles: `
        :host { display: block; }
        .source-line {
            display: grid;
            grid-template-columns: 5ch minmax(0, 1fr);
            gap: 0.5rem;
            padding-block: 0.25rem;
            font-family: monospace;
        }
        .source-line code { white-space: pre-wrap; overflow-wrap: anywhere; }
        .source-index, .source-line.raw { color: var(--mat-sys-on-surface-variant); }
        .source-reference { display: inline-block; margin-inline-start: 0.75rem; font-family: var(--mat-sys-body-small-font); }
    `,
})
export class Cs2SourceComponent {
    readonly script = input.required<GetCs2ScriptDto>();
    /** Source lines; labels have no instruction index of their own. */
    /** Older script responses omit decompiled source. */
    private readonly source = computed(() => this.script().source ?? []);
    readonly lines = computed(() => this.source().map(line => ({ ...line, number: line.label ? "" : line.index })));
    readonly partial = computed(() => this.source().some(line => line.raw));
}

/** Executable RuneScript and the instruction-level disassembly. */
@Component({
    selector: "cache-cs2-disassembly",
    imports: [MatCardModule],
    template: `
        <mat-card appearance="outlined" class="min-w-0">
            <mat-card-header><mat-card-title>Disassembly</mat-card-title></mat-card-header>
            <mat-card-content class="pt-4">
                @if (executableSource() !== null) {
                    <details class="mb-4">
                        <summary>Executable RuneScript</summary>
                        <pre class="source-scroll">{{ executableSource() }}</pre>
                    </details>
                }
                @if (script().instructions.length) {
                    <div class="instruction-scroll" role="region" aria-label="Script instructions" tabindex="0">
                        <table class="script-table instructions" aria-label="Disassembly">
                            <thead><tr><th scope="col">Index</th><th scope="col">Opcode</th><th scope="col">Type</th><th scope="col">Operand</th></tr></thead>
                            <tbody>
                                @for (instruction of script().instructions; track instruction.index) {
                                    <tr>
                                        <td>{{ instruction.index }}</td>
                                        <td>{{ instruction.opcode }}</td>
                                        <td>{{ instruction.operandKind }}</td>
                                        <td>{{ operandText(instruction) }}</td>
                                    </tr>
                                }
                            </tbody>
                        </table>
                    </div>
                } @else {
                    <p>This script has no instructions.</p>
                }
            </mat-card-content>
        </mat-card>
    `,
    styleUrls: ["./cs2-script.scss"],
    styles: `
        :host { display: block; min-width: 0; }
        .instruction-scroll { max-block-size: 65vh; overflow: auto; }
        .instructions { font-family: monospace; }
        .instructions th { position: sticky; top: 0; background: var(--mat-sys-surface); }
        .instructions td:last-child { width: 100%; overflow-wrap: anywhere; white-space: pre-wrap; }
    `,
})
export class Cs2DisassemblyComponent {
    private readonly json = new JsonPipe();
    readonly script = input.required<GetCs2ScriptDto>();
    readonly executableSource = computed(() => {
        const source = this.script().executableSource;
        return source?.length ? source.join("\n") : null;
    });

    /** String operands are shown quoted and escaped, as JSON. */
    operandText(instruction: GetCs2ScriptDto["instructions"][number]): string {
        return instruction.operandKind === "string" ? this.json.transform(instruction.operand) : instruction.operand;
    }
}
