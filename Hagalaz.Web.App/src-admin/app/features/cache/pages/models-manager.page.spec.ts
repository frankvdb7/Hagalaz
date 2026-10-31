import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { Component, forwardRef, input } from "@angular/core";
import { TestBed } from "@angular/core/testing";
import { MatButtonHarness } from "@angular/material/button/testing";
import { MatInputHarness } from "@angular/material/input/testing";
import { By } from "@angular/platform-browser";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CacheModelPreviewComponent } from "./cache-model-preview.component";
import { ModelsManagerPageComponent } from "./models-manager.page";

@Component({
    selector: "cache-model-preview",
    template: "",
    providers: [{ provide: CacheModelPreviewComponent, useExisting: forwardRef(() => ModelBoundary) }],
})
class ModelBoundary {
    readonly modelId = input<number | null>(null);
    readonly model = { reload: vi.fn() };
}

describe("Models lookup", () => {
    afterEach(() => TestBed.resetTestingModule());

    async function open() {
        TestBed.overrideComponent(ModelsManagerPageComponent, {
            remove: { imports: [CacheModelPreviewComponent] },
            add: { imports: [ModelBoundary] },
        });
        const fixture = TestBed.createComponent(ModelsManagerPageComponent);
        await fixture.whenStable();
        const loader = TestbedHarnessEnvironment.loader(fixture);
        return {
            fixture,
            input: await loader.getHarness(MatInputHarness),
            button: await loader.getHarness(MatButtonHarness.with({ text: /Load model/ })),
        };
    }

    it("loads and reloads a model through the shared Material form", async () => {
        const { fixture, input, button } = await open();
        await input.setValue("42");
        await button.click();
        expect(fixture.componentInstance.selectedId()).toBe(42);
        expect(fixture.nativeElement.textContent).toContain("Model #42");
        const preview = fixture.debugElement.query(By.directive(ModelBoundary)).componentInstance as ModelBoundary;
        expect(preview.modelId()).toBe(42);
        await button.click();
        expect(preview.model.reload).toHaveBeenCalledOnce();
    });

    it("disables invalid model lookups without opening a preview", async () => {
        const { fixture, input, button } = await open();
        await input.setValue("-1");
        await input.blur();
        expect(await button.isDisabled()).toBe(true);
        expect(fixture.componentInstance.selectedId()).toBeNull();
        expect(fixture.debugElement.query(By.directive(ModelBoundary))).toBeNull();
    });
});
