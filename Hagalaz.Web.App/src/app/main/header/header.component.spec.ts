import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect, vi, afterEach } from "vitest";
import { HeaderComponent } from "./header.component";
import { LoadingBarService } from "@ngx-loading-bar/core";
import { BreakpointObserver } from "@angular/cdk/layout";
import { AuthStore } from "@app/core/auth/auth.store";
import { UserStore } from "@app/core/user/user.store";
import { of } from "rxjs";
import { NO_ERRORS_SCHEMA } from "@angular/core";
import { SvgLoader, SVG_ICON_REGISTRY_PROVIDER } from "angular-svg-icon";
import { HttpTestingController, provideHttpClientTesting } from "@angular/common/http/testing";

describe("HeaderComponent", () => {
    let component: HeaderComponent;
    let fixture: ComponentFixture<HeaderComponent>;
    let httpTestingController: HttpTestingController;

    // Mock services
    const mockLoadingBarService = {
        value$: of(0),
    };

    const mockBreakpointObserver = {
        observe: vi.fn().mockReturnValue(of({ matches: false })),
    };

    const mockAuthStore = {
        authenticated: vi.fn().mockReturnValue(false),
        logout: vi.fn(),
    };

    const mockUserStore = {
        username: vi.fn().mockReturnValue("testUser"),
    };

    // More robust SvgLoader mock that handles AbortSignal
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
            imports: [HeaderComponent],
            providers: [
                { provide: LoadingBarService, useValue: mockLoadingBarService },
                { provide: BreakpointObserver, useValue: mockBreakpointObserver },
                { provide: AuthStore, useValue: mockAuthStore },
                { provide: UserStore, useValue: mockUserStore },
                { provide: SvgLoader, useValue: mockSvgLoader },
                provideHttpClientTesting(),
                SVG_ICON_REGISTRY_PROVIDER,
            ],
            schemas: [NO_ERRORS_SCHEMA], // Ignore unknown elements and properties
        }).compileComponents();

        httpTestingController = TestBed.inject(HttpTestingController);

        fixture = TestBed.createComponent(HeaderComponent);
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
