/** Returns a value a test depends on, failing with a clear message instead of a later null dereference when it is missing. */
export function required<T>(value: T | null | undefined, description: string): T {
    if (value === null || value === undefined) throw new Error(`Expected ${description}.`);
    return value;
}
