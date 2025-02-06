using Hagalaz.Services.JagGrab.Network;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hagalaz.Services.JagGrab
{
    /// <summary>
    /// 
    /// </summary>
    public class Startup
    {
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="env">The env.</param>
        public Startup(IConfiguration configuration) => Configuration = configuration;

        /// <summary>
        /// Configures the services.
        /// </summary>
        /// <param name="services">The services.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<JagGrabConfig>(Configuration.GetSection("Configuration"));

            services.AddSingleton<RequestHandler>();
            //services.AddSingleton<IConnectionAdapter, ConnectionHandler>();

            //services.AddTransient<Request>();
        }

        /// <summary>
        /// Configures the specified application.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="connection">The connection.</param>
        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpsRedirection();
        }
    }
}
