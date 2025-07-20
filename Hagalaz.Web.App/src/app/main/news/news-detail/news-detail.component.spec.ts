import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { NewsDetailComponent } from "./news-detail.component";
import { inputBinding, signal } from "@angular/core";

describe("NewsDetailComponent", () => {
    let component: NewsDetailComponent;
    let fixture: ComponentFixture<NewsDetailComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(NewsDetailComponent, {
            bindings: [inputBinding("id", signal("1"))],
        });
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
