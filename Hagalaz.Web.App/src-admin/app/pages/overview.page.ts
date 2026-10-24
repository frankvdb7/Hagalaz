import { NgOptimizedImage } from "@angular/common";
import { Component } from "@angular/core";
import { MatButtonModule } from "@angular/material/button";
import { MatCardModule } from "@angular/material/card";
import { MatIconModule } from "@angular/material/icon";
import { RouterLink } from "@angular/router";
import { ArchiveCountCardComponent } from "./archive-count-card";

@Component({
    selector: "admin-overview",
    imports: [NgOptimizedImage, RouterLink, MatButtonModule, MatCardModule, MatIconModule, ArchiveCountCardComponent],
    templateUrl: "./overview.page.html",
    styleUrl: "./overview.page.scss",
})
export class OverviewPageComponent {
    readonly metrics = [
        { label: "Adventurers online", icon: "group", accent: "accent-cyan" },
        { label: "Active worlds", icon: "public", accent: "accent-emerald" },
        { label: "Average latency", icon: "bolt", accent: "accent-violet" },
    ];
    readonly actions = [
        { label: "Browse definitions", path: "/cache/types", icon: "database", accent: "" },
        { label: "Edit sprites", path: "/cache/types", queryParams: { kind: "sprites" }, icon: "image", accent: "accent-violet" },
        { label: "Explore worlds", path: "/worlds", icon: "public", accent: "accent-emerald" },
        { label: "Audit capabilities", path: "/audit", icon: "history", accent: "accent-cyan" },
    ];
}
