export function getCacheErrorMessage(error: unknown): string {
    const response = error as { error?: { detail?: string; title?: string }; message?: string };
    const problem = response.error ?? {};
    return [problem.detail, problem.title, response.message].find((message) => typeof message === "string") ?? "Request failed.";
}
