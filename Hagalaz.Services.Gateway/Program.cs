using Hagalaz.ServiceDefaults;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.AddDefaultHealthChecks().ConfigureOpenTelemetry();
builder.Services.AddServiceDiscovery();
builder.Configuration.AddEnvironmentVariables(EnvironmentVariables.Prefix);

// Add services to the container.
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

builder.Services.AddResponseCaching();
builder.Services.AddResponseCompression();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.MapReverseProxy();


if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.MapGet("/api/routes", (IProxyConfigProvider configProvider) => configProvider.GetConfig().Routes.ToList());
    app.MapGet("/api/clusters", (IProxyConfigProvider configProvider) => configProvider.GetConfig().Clusters.ToList());
} 
else
{
    app.UseHsts();
    app.UseResponseCaching();
    app.UseResponseCompression();
}

await app.RunAsync();