import { ComponentFixture, TestBed } from "@angular/core/testing";

import { LauncherFooterComponent } from "./footer.component";

describe("LauncherFooterComponent", () => {
    let component: LauncherFooterComponent;
    let fixture: ComponentFixture<LauncherFooterComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
            declarations: [LauncherFooterComponent],
        }).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(LauncherFooterComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
