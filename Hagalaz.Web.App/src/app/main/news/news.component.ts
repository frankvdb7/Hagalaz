import { ChangeDetectionStrategy, Component, OnInit } from "@angular/core";
import { NewsItem, newsItems } from "@app/main/news/news.model";
import { RouterLink } from "@angular/router";
import { NewsItemComponent } from "./news-item/news-item.component";
import { MatAnchor } from "@angular/material/button";

@Component({
    selector: "app-news",
    templateUrl: "./news.component.html",
    styleUrls: ["./news.component.scss"],
    imports: [RouterLink, NewsItemComponent, MatAnchor],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class NewsComponent {
    news: NewsItem[] = newsItems.slice(0, 5);
}
