import { ComponentFixture, TestBed } from "@angular/core/testing";

import { LauncherHeaderComponent } from "./header.component";

describe("LauncherHeaderComponent", () => {
    let component: LauncherHeaderComponent;
    let fixture: ComponentFixture<LauncherHeaderComponent>;

    beforeEach(async () => {
        await TestBed.configureTestingModule({
    imports: [LauncherHeaderComponent],
}).compileComponents();
    });

    beforeEach(() => {
        fixture = TestBed.createComponent(LauncherHeaderComponent);
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
