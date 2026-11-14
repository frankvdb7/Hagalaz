/** Accepts a whole-number edit from a validated number input, otherwise restores the input to the current value. */
export function acceptIntegerInput(input: HTMLInputElement, current: number, apply: (value: number) => void) {
    if (Number.isInteger(input.valueAsNumber) && input.validity.valid) apply(input.valueAsNumber);
    else input.value = String(current);
}
