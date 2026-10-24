import type { Routes } from "@angular/router";

export const integrationRoutes: Routes = [
    {
        path: "characters",
        data: {
            title: "Characters",
            eyebrow: "Adventurer records",
            description: "Character records, progression, and permissions.",
            notice: "Character administration is not connected. The existing statistics API does not provide account search or privileged editing.",
            capabilities: [
                {
                    title: "Adventurer directory",
                    icon: "group",
                    description: "Requires an authorized character search and detail contract. No sample accounts are displayed.",
                },
                {
                    title: "Progression & worlds",
                    icon: "public",
                    description: "Requires administrative progression and session APIs before levels or world assignments can be changed.",
                },
                {
                    title: "Permissions",
                    icon: "shield",
                    description: "Requires server-side authorization, confirmation, and an audit trail for privileged account changes.",
                },
            ],
        },
    },
    {
        path: "worlds",
        data: {
            title: "Realm Management",
            eyebrow: "World operations",
            description: "Game-world instances, capacity, and service health.",
            notice: "World operations are not connected. No operational commands or world status are simulated.",
            capabilities: [
                { title: "World directory", icon: "public", description: "Requires an authorized world inventory with real region and capacity information." },
                {
                    title: "Live sessions",
                    icon: "group",
                    description: "Requires live population and latency telemetry. Offline and unknown must remain distinct.",
                },
                { title: "Manage worlds", icon: "tune", description: "Requires privileged operational endpoints with confirmation and auditable results." },
            ],
        },
    },
    {
        path: "economy",
        data: {
            title: "Economy",
            eyebrow: "Gold and commerce",
            description: "Currency flow, item distribution, and player trading.",
            notice: "Economy analytics are not connected. Currency totals and transaction counts are unavailable.",
            capabilities: [
                { title: "Currency circulation", icon: "paid", description: "Requires an authoritative economy aggregate API with a reporting period." },
                { title: "Trades & transfers", icon: "swap_horiz", description: "Requires authorized trade reporting and paged transaction records." },
                { title: "Review alerts", icon: "policy", description: "Requires server-generated review signals and an audited review workflow." },
            ],
        },
    },
    {
        path: "infrastructure",
        data: {
            title: "Infrastructure",
            eyebrow: "Platform operations",
            description: "Runtime services, response times, and reliability.",
            notice: "Service telemetry is not connected. Unavailable telemetry does not mean the services are offline or healthy.",
            capabilities: [
                { title: "Service inventory", icon: "dns", description: "Requires an authorized runtime service inventory and current health observations." },
                { title: "Reliability & latency", icon: "monitoring", description: "Requires timestamped uptime, response-time, and incident metrics." },
                { title: "Incident history", icon: "warning", description: "Requires a paged operational incident feed. No incident counts are inferred." },
            ],
        },
    },
    {
        path: "audit",
        data: {
            title: "Audit Logs",
            eyebrow: "Realm accountability",
            description: "Administrative changes and operational events.",
            notice: "An audit feed is not connected. Per-page cache confirmations are not a persistent audit history.",
            capabilities: [
                {
                    title: "Administrative events",
                    icon: "history",
                    description: "Requires durable server-side actor, action, target, timestamp, and outcome records.",
                },
                { title: "Search & filters", icon: "filter_alt", description: "Requires an authorized paged audit query contract and retention policy." },
                { title: "Audit export", icon: "download", description: "Requires authorized export with privacy controls before reports can be downloaded." },
            ],
        },
    },
    {
        path: "settings",
        data: {
            title: "Settings",
            eyebrow: "Account and workspace",
            description: "Administration preferences and account security.",
            notice: "Portal settings are not connected. Use Sign out in the toolbar to end the current session.",
            capabilities: [
                {
                    title: "Account security",
                    icon: "manage_accounts",
                    description: "Requires the identity provider's supported account-management integration.",
                },
                {
                    title: "Workspace preferences",
                    icon: "settings",
                    description: "No persisted preference contract is available. This portal uses the Hagalaz dark theme.",
                },
                {
                    title: "Access management",
                    icon: "verified_user",
                    description: "Existing administrator access checks remain enforced. Role changes require a dedicated authorized API.",
                },
            ],
        },
    },
].map((route) => ({ ...route, loadComponent: () => import("./integration.page").then((m) => m.IntegrationPageComponent) }));
