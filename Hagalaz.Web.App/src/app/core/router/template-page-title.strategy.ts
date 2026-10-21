import { inject, Service } from "@angular/core";
import { Title } from "@angular/platform-browser";
import { type RouterStateSnapshot, TitleStrategy } from "@angular/router";

@Service()
export class TemplatePageTitleStrategy extends TitleStrategy {
    private readonly title = inject(Title);

    override updateTitle(routerState: RouterStateSnapshot) {
        const title = this.buildTitle(routerState);
        if (title !== undefined) {
            this.title.setTitle(`${title} | Hagalaz`);
        }
    }
}
