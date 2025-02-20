using Hagalaz.AppHost;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddMySql("mysql")
    .WithEnvironment("MYSQL_DATABASE", "hagalaz-db")
    .WithBindMount("../Data", "/docker-entrypoint-initdb.d")
    .WithPhpMyAdmin()
    .AddDatabase("hagalaz-db");

var messaging = builder.AddRabbitMQ("messaging")
    .WithImage("masstransit/rabbitmq");

var cache = builder.AddRedis("cache")
    .WithRedisInsight();

var gameWorldService = builder.AddProject<Projects.Hagalaz_Services_GameWorld>("hagalaz-services-gameworld", launchProfileName: "tcp")
    .WaitFor(messaging)
    .WaitFor(database)
    .WaitFor(cache)
    .WithReference(database)
    .WithReference(messaging)
    .WithReference(cache)
    .WithEnvironment("HAGALAZ_Hagalaz.Cache__Path", "../Cache")
    .WithEndpoint(port: 443, scheme: "tcp", env: "TCP_PORT")
    .WithHttpsEndpoint(port: 7010, env: "HTTPS_PORT")
    .WithHttpEndpoint(port: 5010, env: "HTTP_PORT")
    .WithHttpsHealthCheck("/health");

var authService = builder.AddProject<Projects.Hagalaz_Services_Authorization>("hagalaz-services-authorization")
    .WaitFor(messaging)
    .WaitFor(database)
    .WithReference(database)
    .WithReference(messaging)
    .WithScalarDocs()
    .WithHttpsHealthCheck("/health");

var contactsService = builder.AddProject<Projects.Hagalaz_Services_Contacts>("hagalaz-services-contacts")
    .WaitFor(messaging)
    .WaitFor(database)
    .WithReference(authService)
    .WithReference(database)
    .WithReference(messaging)
    .WithScalarDocs()
    .WithHttpsHealthCheck("/health");

var charactersService = builder.AddProject<Projects.Hagalaz_Services_Characters>("hagalaz-services-characters")
    .WaitFor(messaging)
    .WaitFor(database)
    .WithReference(authService)
    .WithReference(database)
    .WithReference(messaging)
    .WithScalarDocs()
    .WithHttpsHealthCheck("/health");

var gameUpdate = builder.AddProject<Projects.Hagalaz_Services_GameUpdate>("hagalaz-services-gameupdate", launchProfileName: "tcp")
    .WithEnvironment("HAGALAZ_Hagalaz.Cache__Path", "../Cache")
    .WithEndpoint(port: 43594, scheme: "tcp", env: "TCP_PORT")
    .WithHttpsEndpoint(port: 7005, env: "HTTPS_PORT")
    .WithHttpEndpoint(port: 5008, env: "HTTP_PORT")
    .WithHttpsHealthCheck("/health");

var webApp = builder.AddNpmApp("hagalaz-web-app", "../Hagalaz.Web.App", "start:aspire")
    .WithReference(authService)
    .WithReference(contactsService)
    .WithReference(charactersService)
    .WithHttpsEndpoint(targetPort: 4400, env: "PORT")
    .WithExternalHttpEndpoints()
    .PublishAsDockerFile();

var gateway = builder.AddProject<Projects.Hagalaz_Services_Gateway>("hagalaz-services-gateway")
    .WithReference(gameWorldService)
    .WithReference(authService)
    .WithReference(contactsService)
    .WithReference(charactersService)
    .WithReference(gameUpdate)
    .WithReference(webApp)
    .WithExternalHttpEndpoints()
    .WithHttpsHealthCheck("/health");

if (builder.Environment.IsDevelopment() && builder.Configuration["DOTNET_LAUNCH_PROFILE"] == "https")
{
    // Disable TLS certificate validation in development, see https://github.com/dotnet/aspire/issues/3324 for more details.
    webApp.WithEnvironment("NODE_TLS_REJECT_UNAUTHORIZED", "0");
}

builder.Build().Run();