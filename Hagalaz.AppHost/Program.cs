using Aspire.Hosting;
using Hagalaz.AppHost;

var builder = DistributedApplication.CreateBuilder(args);

AppHostConfiguration.Configure(builder);

builder.Build().Run();
