import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect, vi } from "vitest";
import { HighscoresComponent } from "./highscores.component";
import { Router, provideRouter } from "@angular/router";
import { HighscoresStore } from "./highscores.store";
import { NO_ERRORS_SCHEMA, importProvidersFrom } from "@angular/core";
import { RouterTestingModule } from "@angular/router/testing";

describe("HighscoresComponent", () => {
    let component: HighscoresComponent;
    let fixture: ComponentFixture<HighscoresComponent>;

    // Mock services
    const mockRouter = {
        navigate: vi.fn().mockResolvedValue(true)
    };

    const mockHighscoresStore = {
        // Add any properties or methods used in the component
    };

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            imports: [
                HighscoresComponent,
                RouterTestingModule.withRoutes([])
            ],
            providers: [
                { provide: HighscoresStore, useValue: mockHighscoresStore }
            ],
            schemas: [NO_ERRORS_SCHEMA] // Ignore unknown elements and properties
        }).compileComponents();

        fixture = TestBed.createComponent(HighscoresComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
