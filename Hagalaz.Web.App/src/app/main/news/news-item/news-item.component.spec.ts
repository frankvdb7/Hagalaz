import { ComponentFixture, TestBed } from "@angular/core/testing";
import { it, describe, beforeEach, expect } from "vitest";
import { NewsItemComponent } from "./news-item.component";
import { inputBinding, signal } from "@angular/core";
import { newsItems } from "@app/main/news/news.model";

describe("NewsItemComponent", () => {
    let component: NewsItemComponent;
    let fixture: ComponentFixture<NewsItemComponent>;

    beforeEach(() => {
        fixture = TestBed.createComponent(NewsItemComponent, {
            bindings: [inputBinding("newsItem", signal(newsItems[0]))],
        });
        component = fixture.componentInstance;
        fixture.detectChanges();
    });

    it("should create", () => {
        expect(component).toBeTruthy();
    });
});
