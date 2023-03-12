using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Planar.Common;
using Serilog;
using System;

namespace Planar.Startup
{
    public static class WebApplicationInitializer
    {
        public static WebApplication Initialize(string[] args)
        {
            var options = new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default,
                EnvironmentName = AppSettings.Environment
            };

            var builder = WebApplication.CreateBuilder(options);

            builder.Host.UseSerilog(SerilogInitializer.Configure);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(AppSettings.HttpPort);
                if (AppSettings.Clustering)
                {
                    options.ListenAnyIP(AppSettings.HttpPort + 10000, x => x.Protocols = HttpProtocols.Http2);
                }

                if (AppSettings.UseHttps)
                {
                    options.ListenAnyIP(AppSettings.HttpsPort, opts => opts.UseHttps());
                }
            });

            builder.WebHost.ConfigureAppConfiguration(builder =>
            {
                Console.WriteLine("[x] Load configuration & app settings");
                var file1 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.json");
                var file2 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, $"AppSettings.{AppSettings.Environment}.json");

                builder
                .AddJsonFile(file1, optional: false, reloadOnChange: true)
                .AddJsonFile(file2, optional: true, reloadOnChange: true)
                .AddCommandLine(args)
                .AddEnvironmentVariables();
            });

            ServiceCollectionInitializer.ConfigureServices(builder.Services);
            builder.Host.UseWindowsService();
            var app = builder.Build();
            return app;
        }

        public static void Configure(WebApplication app)
        {
            //// app.UseHttpLogging();

            if (AppSettings.DeveloperExceptionPage || !app.Environment.IsProduction())
            {
                app.UseDeveloperExceptionPage();
            }

            if (AppSettings.SwaggerUI)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint($"/swagger/{SwaggerInitializer.SwaggerVersion}/swagger.json", "Planar");
                    c.InjectStylesheet("/Content/theme-flattop.css");
                });
            }

            if (AppSettings.UseHttpsRedirect)
            {
                app.UseHttpsRedirection();
            }

            app.UseSerilogRequestLogging();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                endpoints.MapGrpcService<ClusterService>();
            });
        }
    }
}