import { type ComponentFixture, TestBed } from "@angular/core/testing";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MapLoadProgress } from "../services/map-render.models";
import { MapLoadStatusComponent, mapLoadPercent, mapLoadStage } from "./map-load-status.component";
import { required } from "../../../../testing/required";

const progress = (values: Partial<MapLoadProgress> = {}): MapLoadProgress => ({ regionsSettled: 1, regionsTotal: 1, appearancesSettled: 0, appearancesTotal: 0, texturesSettled: 0, texturesTotal: 0, animationsSettled: 0, animationsTotal: 0, waterSettled: 0, waterTotal: 0, failures: 0, ...values });

describe("map load stage", () => {
    it.each([
        [{ regionsSettled: 0 }, "Loading region", null],
        [{ regionsSettled: 2, regionsTotal: 9 }, "Loading regions", "2 of 9"],
        [{ appearancesSettled: 3, appearancesTotal: 8 }, "Loading objects", "3 of 8"],
        [{ appearancesSettled: 8, appearancesTotal: 8, texturesSettled: 1, texturesTotal: 4 }, "Loading textures", "1 of 4"],
        [{ appearancesSettled: 8, appearancesTotal: 8, texturesSettled: 1, texturesTotal: 4, animationsSettled: 2, animationsTotal: 5 }, "Loading animations", "2 of 5"],
        [{ appearancesSettled: 8, appearancesTotal: 8, texturesSettled: 1, texturesTotal: 4, waterSettled: 1, waterTotal: 3 }, "Loading water", "1 of 3"],
        [{}, "Finishing scene", null],
    ])("describes %j", (values, label, detail) => {
        expect(mapLoadStage(progress(values))).toEqual({ label, detail });
    });

    it("measures settled work over known work", () => {
        expect(mapLoadPercent(progress({ regionsSettled: 0, regionsTotal: 0 }))).toBe(0);
        expect(mapLoadPercent(progress({ appearancesSettled: 1, appearancesTotal: 3 }))).toBe(50);
        expect(mapLoadPercent(progress({ animationsSettled: 0, animationsTotal: 1 }))).toBe(50);
        expect(mapLoadPercent(progress({ waterSettled: 0, waterTotal: 1 }))).toBe(50);
    });
});

describe("MapLoadStatusComponent", () => {
    let fixture: ComponentFixture<MapLoadStatusComponent>;
    let element: HTMLElement;
    const set = (inputs: Partial<{ loading: boolean; hasRegions: boolean; hasGeometry: boolean; progress: MapLoadProgress; issues: readonly string[]; activity: string | null }>) => {
        for (const [name, value] of Object.entries(inputs)) fixture.componentRef.setInput(name, value);
        fixture.detectChanges();
    };
    const click = (label: string) => {
        const button = [...element.querySelectorAll("button")].find(candidate => candidate.textContent?.includes(label) || candidate.getAttribute("aria-label") === label);
        if (!button) throw new Error(`No ${label} button`);
        button.click();
        fixture.detectChanges();
    };
    const announcement = () => element.querySelector('[role="status"]')?.textContent;

    beforeEach(() => {
        fixture = TestBed.createComponent(MapLoadStatusComponent);
        element = fixture.nativeElement;
        fixture.componentRef.setInput("progress", progress());
    });

    it("centers the first load until a region arrives", () => {
        set({ loading: true, progress: progress({ regionsSettled: 1, regionsTotal: 4 }) });
        expect(fixture.componentInstance.view()).toBe("first-load");
        expect(element.querySelector("mat-spinner")).not.toBeNull();
        expect(element.textContent).toContain("Loading regions");
        expect(element.textContent).toContain("1 of 4");
    });

    it("shows a delayed, non-interactive indicator once a scene is visible", () => {
        set({ loading: true, hasRegions: true, hasGeometry: true, progress: progress({ appearancesSettled: 2, appearancesTotal: 3 }) });
        const panel = element.querySelector(".load-panel");
        expect(panel?.classList).toContain("is-delayed");
        expect(panel?.classList).not.toContain("is-interactive");
        expect(element.textContent).toContain("2 / 3");
        expect(element.querySelector("mat-progress-bar")?.getAttribute("aria-valuenow")).toBe("75");
    });

    it("shows overall progress and each parallel operation's own progress, marking finished ones complete", () => {
        set({ loading: true, hasRegions: true, hasGeometry: true, progress: progress({
            appearancesSettled: 800, appearancesTotal: 1600, animationsSettled: 17, animationsTotal: 17,
            waterSettled: 1, waterTotal: 4, texturesTotal: 10,
        }) });
        const panel = required(element.querySelector("cache-map-load-panel"), "the load panel");
        expect(panel.textContent).toContain("1 of 4 done");
        const rows = [...panel.querySelectorAll("li")].map(row => [
            row.querySelector(".operation-label")?.textContent, row.querySelector(".operation-value")?.textContent, row.classList.contains("is-done"),
            row.querySelector("mat-progress-bar")?.getAttribute("aria-valuenow"),
        ]);
        expect(rows).toEqual([["Objects", "800 / 1,600", false, "50"], ["Animations", "17 / 17", true, "100"], ["Water", "1 / 4", false, "25"], ["Textures", "0 / 10", false, "0"]]);
        set({ activity: "Saving changes" });
        expect(element.querySelector("cache-map-load-panel")).toBeNull();
        expect(element.textContent).toContain("Saving changes");
    });

    it("keeps progress moving forward as totals grow and restarts for a new load", () => {
        set({ loading: true, hasRegions: true, progress: progress({ appearancesSettled: 3, appearancesTotal: 3 }) });
        expect(fixture.componentInstance.percent()).toBe(100);
        set({ progress: progress({ appearancesSettled: 3, appearancesTotal: 3, texturesTotal: 8 }) });
        expect(fixture.componentInstance.percent()).toBe(100);

        set({ loading: false });
        expect(fixture.componentInstance.percent()).toBe(100);
        set({ loading: true, progress: progress({ appearancesTotal: 3 }) });
        expect(fixture.componentInstance.percent()).toBe(25);

        set({ progress: progress({ regionsSettled: 0, regionsTotal: 2 }) });
        expect(fixture.componentInstance.percent()).toBe(0);
    });

    it("restarts progress after a finished load whose panel was hidden, but not when visibility lowers the counts", () => {
        set({ loading: true, hasRegions: true, hasGeometry: true, progress: progress({ appearancesSettled: 9, appearancesTotal: 10 }) });
        // Hiding a plane drops the settled counts within the same load.
        set({ progress: progress({ appearancesSettled: 2, appearancesTotal: 4 }) });
        expect(fixture.componentInstance.percent()).toBe(90);
        set({ loading: false });
        expect(element.querySelector("cache-map-load-panel")).toBeNull();
        set({ loading: true, progress: progress({ appearancesSettled: 1, appearancesTotal: 10 }) });
        expect(element.querySelector("mat-progress-bar")?.getAttribute("aria-valuenow")).toBe("18");
    });

    it("describes a cache write with an indeterminate bar", () => {
        set({ hasRegions: true, hasGeometry: true, activity: "Saving changes" });
        expect(element.textContent).toContain("Saving changes");
        expect(element.querySelector("mat-progress-bar")?.getAttribute("mode")).toBe("indeterminate");
        expect(announcement()).toBe("Saving changes");
    });

    it("does not change the announcement as counts change", () => {
        set({ loading: true, hasRegions: true, progress: progress({ appearancesTotal: 5 }) });
        const first = announcement();
        set({ progress: progress({ appearancesSettled: 4, appearancesTotal: 5 }) });
        expect(announcement()).toBe(first);
    });

    it("explains a failed region and retries", () => {
        const retried = vi.fn();
        fixture.componentInstance.retry.subscribe(retried);
        set({ progress: progress({ failures: 1 }), issues: ["Region 5: unavailable"] });
        expect(element.textContent).toContain("Region could not be loaded");
        expect(element.textContent).toContain("Region 5: unavailable");
        click("Retry");
        expect(retried).toHaveBeenCalledOnce();
    });

    it("explains an empty scene with its availability notes", () => {
        set({ issues: ["Region 5: terrain absent."] });
        expect(fixture.componentInstance.view()).toBe("empty");
        expect(element.textContent).toContain("No renderable tiles or objects");
        expect(element.textContent).toContain("Region 5: terrain absent.");
    });

    it("keeps read failures visible with details until dismissed", () => {
        set({ hasRegions: true, hasGeometry: true, progress: progress({ failures: 2 }), issues: ["Texture 9: missing"] });
        expect(element.textContent).toContain("2 items could not be loaded");
        expect(element.textContent).not.toContain("Texture 9: missing");

        click("Details");
        expect(element.textContent).toContain("Texture 9: missing");

        click("Dismiss load issues");
        expect(fixture.componentInstance.view()).toBe("none");
        expect(announcement()).toBe("Map scene ready.");

        set({ progress: progress({ failures: 3 }) });
        expect(fixture.componentInstance.view()).toBe("issues");
    });
});
