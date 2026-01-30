using Hagalaz.ServiceDefaults;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.AddServerHeader = false;
});

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

app.Use(async (context, next) =>
{
    context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy", "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
    context.Response.Headers.Append("X-DNS-Prefetch-Control", "off");
    context.Response.Headers.Append("X-Permitted-Cross-Domain-Policies", "none");
    context.Response.Headers.Append("X-Download-Options", "noopen");
    await next();
});

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
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
        await next();
    });
    app.UseResponseCaching();
    app.UseResponseCompression();
}

await app.RunAsync();