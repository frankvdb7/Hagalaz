import { Config } from "tailwindcss";

const colors = [
    "primary",
    "on-primary",
    "primary-container",
    "on-primary-container",
    "secondary",
    "on-secondary",
    "surface",
    "on-surface",
    "background",
    "on-background",
    "error",
    "on-error",
    "outline",
    "outline-variant",
    "surface-dim",
    "surface-bright",
    "surface-container",
    "surface-container-high",
    "surface-container-highest",
    "surface-container-low",
];

function makeColorTheme(colorVariableBase: string, colors: string[]) {
    const colorTheme: { [key: string]: string } = {};
    for (let i = 0; i < colors.length; i++) {
        colorTheme[colors[i]] = `var(${colorVariableBase}-${typeof colors[i] === "string" ? colors[i].toLowerCase() : colors[i]})`;
    }
    return colorTheme;
}

const colorObj = makeColorTheme("--sys", colors);

export default {
    prefix: "",
    important: true,
    content: ["./src/**/*.{html,scss,ts}"],
    theme: {
        screens: {
            xs: { max: "599.98px" },
            sm: { min: "600px" },
            md: { min: "905px" },
            lg: { min: "1240px" },
            xl: { min: "1440px" },
        },
        container: {
            screens: {
                xs: "100%",
                sm: "100%",
                md: "840px",
                lg: "100%",
                xl: "1040px",
            },
        },
        extend: {
            backgroundImage: {
                "gradient-radial": "radial-gradient(var(--tw-gradient-stops))",
                "gradient-eclipse-top": "radial-gradient(ellipse at top, var(--tw-gradient-stops))",
            },
            transitionProperty: {
                "max-width": "max-width",
            },
            colors: {
                ...colorObj,
            },
            borderRadius: {
                DEFAULT: "0.75rem",
            },
            minHeight: (util) => ({
                ...util.theme("spacing"),
            }),
        },
    },
    variants: {
        extend: {},
    },
    plugins: [],
} satisfies Config;
