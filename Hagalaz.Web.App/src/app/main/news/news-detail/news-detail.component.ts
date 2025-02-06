import { Component, OnInit, ChangeDetectionStrategy, input, computed, effect, OnDestroy, signal, SecurityContext, inject } from "@angular/core";
import {
    MatCard,
    MatCardActions,
    MatCardAvatar,
    MatCardContent,
    MatCardFooter,
    MatCardHeader,
    MatCardImage,
    MatCardSubtitle,
    MatCardTitle,
} from "@angular/material/card";
import { MatChip, MatChipSet } from "@angular/material/chips";
import { NewsItem, newsItems } from "../news.model";
import { NgOptimizedImage } from "@angular/common";
import { DateAgoPipe } from "@app/common/pipes/date-ago/date-ago.pipe";
import { MatButton } from "@angular/material/button";
import { BackgroundImageService } from "@app/services/util/background-image.service";
import { CardTitleComponent } from "../../../common/components/card-title/card-title.component";
import { DomSanitizer, SafeUrl } from "@angular/platform-browser";
import { marked } from "marked";
import { RouterLinkWithHref } from "@angular/router";

@Component({
    selector: "app-news-detail",
    templateUrl: "./news-detail.component.html",
    styleUrls: ["./news-detail.component.scss"],
    imports: [MatCard, MatCardHeader, MatCardFooter, MatCardContent, DateAgoPipe, MatButton, MatChip, MatChipSet, RouterLinkWithHref, CardTitleComponent],
    host: { class: "flex flex-auto" },
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NewsDetailComponent implements OnDestroy {
    private bgImgService = inject(BackgroundImageService);
    private domSanitizer = inject(DomSanitizer);

    id = input.required<string>();
    item = computed<NewsItem>(() => {
        const id = this.id();
        return newsItems.find((item) => item.id === id) || ({} as NewsItem);
    });
    content = signal<string | null>("");

    constructor() {
        const bgImgService = this.bgImgService;

        effect(
            () => {
                bgImgService.set(this.item().image);
            },
            { allowSignalWrites: true }
        );

        effect(() => {
            const content = this.item().content;
            this.setMarkdownContent(content);
        });
    }

    async setMarkdownContent(content: string) {
        const markedContent = this.domSanitizer.sanitize(SecurityContext.HTML, await marked(content));
        this.content.set(markedContent);
    }

    sharePage() {
        const url = window.location.href;
        if (navigator.share) {
            navigator.share({
                title: this.item().title,
                url: url,
            });
        } else {
            navigator.clipboard.writeText(url);
        }
    }

    ngOnDestroy() {
        this.bgImgService.reset();
    }
}
