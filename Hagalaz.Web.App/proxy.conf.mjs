export default {
    "/rdx/cache": {
        target: process.env["services__hagalaz-services-cache__https__0"] || process.env["services__hagalaz-services-cache__http__0"],
        secure: process.env["NODE_ENV"] !== "development",
        changeOrigin: true,
        logLevel: "debug",
        pathRewrite: {
            "^/rdx/cache": "",
        },
    },
    "/rdx/authorization": {
        target: process.env["services__hagalaz-services-authorization__https__0"] || process.env["services__hagalaz-services-authorization__http__0"],
        secure: process.env["NODE_ENV"] !== "development",
        changeOrigin: true,
        logLevel: "debug",
        pathRewrite: {
            "^/rdx/authorization": "",
        },
    },
    "/rdx/characters": {
        target: process.env["services__hagalaz-services-characters__https__0"] || process.env["services__hagalaz-services-characters__http__0"],
        secure: process.env["NODE_ENV"] !== "development",
        changeOrigin: true,
        logLevel: "debug",
        pathRewrite: {
            "^/rdx/characters": "",
        },
    },
};
