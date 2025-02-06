using System;
using System.Numerics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raido.Server.Extensions;
using Hagalaz.Services.GameUpdate.Hubs;
using Hagalaz.Services.GameUpdate.Network;
using Hagalaz.Services.GameUpdate.Network.Protocol;
using Hagalaz.Services.GameUpdate.Services;
using Hagalaz.Services.GameUpdate;
using System.Net;
using Hagalaz.Cache;
using Hagalaz.Cache.Extensions;
using Hagalaz.ServiceDefaults;

var builder = WebApplication.CreateBuilder();

builder.AddServiceDefaults();

builder.Services.Configure<KestrelServerOptions>(builder.Configuration.GetSection("Kestrel"));
builder.Services.Configure<ServerConfig>(builder.Configuration.GetSection(ServerConfig.Key));
builder.Services.Configure<CacheOptions>(builder.Configuration.GetSection(CacheOptions.Key));
builder.Services.Configure<RsaConfig>(options =>
{
    var section = builder.Configuration.GetSection(RsaConfig.Key);
    options.PublicKey = BigInteger.Parse(section.GetValue<string>("PublicKey")!);
    options.ModulusKey = BigInteger.Parse(section.GetValue<string>("ModulusKey")!);
    options.PrivateKey = BigInteger.Parse(section.GetValue<string>("PrivateKey")!);
});

builder.Services.AddScoped<ICacheService, CacheService>();

builder.Services.AddGameCache();

builder.Services.AddRaidoServer().AddHub<FileHub>();
builder.Services.AddRaidoProtocol<FileProtocol>();

builder.WebHost.UseKestrel(options =>
{
    var tcpPort = builder.Configuration.GetValue<int>("TCP_PORT");
    if (tcpPort == 0)
    {
        throw new ArgumentNullException(nameof(tcpPort));
    }

    options.Listen(IPAddress.Loopback,
        tcpPort,
        listenOptions =>
        {
            listenOptions.UseConnectionHandler<UpdateConnectionHandler>();
            listenOptions.UseConnectionLogging();
        });

    var httpsPort = builder.Configuration.GetValue<int>("HTTPS_PORT");
    if (httpsPort == 0)
    {
        throw new ArgumentNullException(nameof(httpsPort));
    }

    options.Listen(IPAddress.Loopback,
        httpsPort,
        listenOptions =>
        {
            listenOptions.UseHttps();
        });

    var httpPort = builder.Configuration.GetValue<int>("HTTP_PORT");
    if (httpPort == 0)
    {
        throw new ArgumentNullException(nameof(httpPort));
    }

    options.Listen(IPAddress.Loopback, httpPort);
});

var app = builder.Build();

app.MapDefaultEndpoints();

await app.RunAsync();