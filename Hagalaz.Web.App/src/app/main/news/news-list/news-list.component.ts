import { NgOptimizedImage } from "@angular/common";
import { ChangeDetectionStrategy, Component } from "@angular/core";
import { NewsItem, newsItems } from "../news.model";
import { NewsItemComponent } from "../news-item/news-item.component";

@Component({
    selector: "app-news-list",
    imports: [NewsItemComponent],
    templateUrl: "./news-list.component.html",
    styleUrl: "./news-list.component.scss",
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class NewsListComponent {
    news: NewsItem[] = newsItems;
}
