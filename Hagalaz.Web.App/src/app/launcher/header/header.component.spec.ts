import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect, vi, afterEach } from "vitest";
import { LauncherHeaderComponent } from "./header.component";
import { LauncherService } from "@app/launcher/launcher.service";
import { NO_ERRORS_SCHEMA } from "@angular/core";
import { SvgLoader, SVG_ICON_REGISTRY_PROVIDER } from "angular-svg-icon";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";
import { of } from "rxjs";

describe("LauncherHeaderComponent", () => {
    let component: LauncherHeaderComponent;
    let fixture: ComponentFixture<LauncherHeaderComponent>;
    let httpTestingController: HttpTestingController;

    // Mock LauncherService
    const mockLauncherService = {
        api: {
            window: {
                isMaximized: vi.fn().mockResolvedValue(false),
                close: vi.fn(),
                maximize: vi.fn(),
                minimize: vi.fn(),
            },
        },
    };

    // Mock SvgLoader
    const mockSvgLoader = {
        load: (url: string, options?: any) => {
            // Return a simple SVG as mock content
            const mockSvg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><circle cx="50" cy="50" r="40" /></svg>';
            return of(mockSvg);
        },
        getSvg: (url: string, options?: any) => {
            // Return a simple SVG as mock content
            const mockSvg = '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 100 100"><circle cx="50" cy="50" r="40" /></svg>';
            return of(mockSvg);
        },
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [LauncherHeaderComponent],
            providers: [
                { provide: LauncherService, useValue: mockLauncherService },
                { provide: SvgLoader, useValue: mockSvgLoader },
                provideHttpClientTesting(),
                SVG_ICON_REGISTRY_PROVIDER,
            ],
            schemas: [NO_ERRORS_SCHEMA], // Ignore unknown elements and properties
        }).compileComponents();

        httpTestingController = TestBed.inject(HttpTestingController);

        fixture = TestBed.createComponent(LauncherHeaderComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    // Add afterEach to handle any pending HTTP requests
    afterEach(() => {
        // Verify all HTTP requests have been handled
        httpTestingController.verify();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
