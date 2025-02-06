import { ChangeDetectionStrategy, Component, inject, OnInit } from "@angular/core";
import { CharacterStatType } from "@app/services/character-statistics/character-statistics.models";
import { MatPaginator, PageEvent } from "@angular/material/paginator";
import { Router, RouterLink } from "@angular/router";
import { MatError, MatFormField, MatLabel } from "@angular/material/form-field";
import { MatCard, MatCardActions, MatCardContent, MatCardHeader, MatCardTitle } from "@angular/material/card";
import { ErrorPipe } from "@app/common/pipes/error/error.pipe";
import { DecimalPipe, LowerCasePipe } from "@angular/common";
import { MatTableModule } from "@angular/material/table";
import { MatOption, MatSelect, MatSelectTrigger } from "@angular/material/select";
import { LoadingComponent } from "@app/common/components/loading/loading.component";
import { HighscoresStore } from "./highscores.store";

@Component({
    selector: "app-highscores",
    templateUrl: "./highscores.component.html",
    styleUrls: ["./highscores.component.scss"],
    imports: [
        MatTableModule,
        LoadingComponent,
        MatSelect,
        MatSelectTrigger,
        ErrorPipe,
        LowerCasePipe,
        DecimalPipe,
        MatPaginator,
        RouterLink,
        MatError,
        MatFormField,
        MatOption,
        MatCard,
        MatCardActions,
        MatCardContent,
        MatCardHeader,
        MatCardTitle,
        MatLabel,
    ],
    providers: [HighscoresStore],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class HighscoresComponent {
    private router = inject(Router);
    highscoresStore = inject(HighscoresStore);

    displayedColumns: string[] = ["rank", "name", "level", "experience"];

    public statNames: string[] = Object.keys(CharacterStatType);

    getStatIcon(stat: CharacterStatType | string) {
        if (!stat) {
            return "";
        }
        return `./assets/icons/${stat.toLowerCase()}-icon.webp`;
    }

    async onPageChanged(event: PageEvent) {
        await this.router.navigate([], {
            queryParams: { page: event.pageIndex + 1, limit: event.pageSize },
            queryParamsHandling: "merge",
        });
    }

    async onStatTypeChanged(statType: CharacterStatType) {
        await this.router.navigate([], {
            queryParams: { skill: statType },
            queryParamsHandling: "merge",
        });
    }
}
