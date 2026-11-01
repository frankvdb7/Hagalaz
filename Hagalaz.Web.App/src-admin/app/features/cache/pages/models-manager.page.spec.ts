import { TestbedHarnessEnvironment } from "@angular/cdk/testing/testbed";
import { Component, forwardRef, input } from "@angular/core";
import { provideHttpClient } from "@angular/common/http";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { provideRouter } from "@angular/router";
import { TestBed } from "@angular/core/testing";
import { MatButtonHarness } from "@angular/material/button/testing";
import { MatInputHarness } from "@angular/material/input/testing";
import { By } from "@angular/platform-browser";
import { afterEach, describe, expect, it, vi } from "vitest";
import { CacheModelPreviewComponent } from "./cache-model-preview.component";
import { ModelsManagerPageComponent } from "./models-manager.page";

@Component({
    selector: "cache-model-preview",
    template: "<ng-content />",
    providers: [{ provide: CacheModelPreviewComponent, useExisting: forwardRef(() => ModelBoundary) }],
})
class ModelBoundary {
    readonly modelId = input<number | null>(null);
    readonly workspace = input(false);
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
            loader,
            input: await loader.getHarness(MatInputHarness),
            button: await loader.getHarness(MatButtonHarness.with({ text: /Load model/ })),
        };
    }

    it("loads and reloads a model through the shared Material form", async () => {
        const { fixture, input, button, loader } = await open();
        await input.setValue("42");
        await button.click();
        expect(fixture.componentInstance.selectedId()).toBe(42);
        const preview = fixture.debugElement.query(By.directive(ModelBoundary)).componentInstance as ModelBoundary;
        expect(preview.modelId()).toBe(42);
        expect(preview.workspace()).toBe(true);
        await (await loader.getHarness(MatButtonHarness.with({ text: /Load model/ }))).click();
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

    it("keeps lookup available after a failed request so another model can be loaded", async () => {
        TestBed.configureTestingModule({
            imports: [ModelsManagerPageComponent],
            providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
        });
        const http = TestBed.inject(HttpTestingController);
        const fixture = TestBed.createComponent(ModelsManagerPageComponent);
        await fixture.whenStable();
        const loader = TestbedHarnessEnvironment.loader(fixture);
        await (await loader.getHarness(MatInputHarness)).setValue("404");
        (fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();
        fixture.detectChanges(); TestBed.tick();
        http.expectOne("/rdx/cache/api/v1/cache/models/404").flush({}, {status: 404, statusText: "Not Found"});
        await fixture.whenStable();
        expect(fixture.nativeElement.textContent).toContain("Model preview is unavailable");
        await (await loader.getHarness(MatInputHarness)).setValue("666");
        (fixture.nativeElement.querySelector('button[type="submit"]') as HTMLButtonElement).click();
        fixture.detectChanges(); TestBed.tick();
        expect(fixture.componentInstance.selectedId()).toBe(666);
        http.expectOne("/rdx/cache/api/v1/cache/models/666").flush({}, {status: 404, statusText: "Not Found"});
        await fixture.whenStable();
        fixture.destroy();
        http.verify();
    });
});
