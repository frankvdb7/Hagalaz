import { NgOptimizedImage } from "@angular/common";
import { ChangeDetectionStrategy, Component, input } from "@angular/core";
import { MatCard } from "@angular/material/card";
import { MatRipple } from "@angular/material/core";
import { RouterLink } from "@angular/router";
import { DateAgoPipe } from "@app/common/pipes/date-ago/date-ago.pipe";
import { NewsItem } from "@app/main/news/news.model";

@Component({
    selector: "app-news-item",
    templateUrl: "./news-item.component.html",
    styleUrls: ["./news-item.component.scss"],
    imports: [MatCard, RouterLink, DateAgoPipe, MatRipple, NgOptimizedImage],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class NewsItemComponent {
    readonly newsItem = input.required<NewsItem>();
}
