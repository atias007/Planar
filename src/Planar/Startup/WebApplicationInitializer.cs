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
                EnvironmentName = AppSettings.General.Environment
            };

            var builder = WebApplication.CreateBuilder(options);

            builder.Host.UseSerilog(SerilogInitializer.Configure);

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenAnyIP(AppSettings.General.HttpPort);
                if (AppSettings.Cluster.Clustering)
                {
                    options.ListenAnyIP(AppSettings.General.HttpPort + 10000, x => x.Protocols = HttpProtocols.Http2);
                }

                if (AppSettings.General.UseHttps)
                {
                    options.ListenAnyIP(AppSettings.General.HttpsPort, opts => opts.UseHttps());
                }
            });

            Console.WriteLine("[x] Load configuration & app settings");
            var file1 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.yml");
            var file2 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, $"AppSettings.{AppSettings.General.Environment}.yml");

            builder.Configuration
                .AddYamlFile(file1, optional: false, reloadOnChange: true)
                .AddYamlFile(file2, optional: true, reloadOnChange: true)
                .AddCommandLine(args)
                .AddEnvironmentVariables();

            ServiceCollectionInitializer.ConfigureServices(builder.Services);
            builder.Host.UseWindowsService();
            var app = builder.Build();
            return app;
        }

        public static void Configure(WebApplication app)
        {
            //// app.UseHttpLogging();

            if (AppSettings.General.DeveloperExceptionPage && !app.Environment.IsProduction())
            {
                app.UseDeveloperExceptionPage();
            }

            if (AppSettings.General.SwaggerUI && !app.Environment.IsProduction())
            {
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint($"/swagger/{SwaggerInitializer.SwaggerVersion}/swagger.json", "Planar");
                    c.InjectStylesheet("/Content/theme-flattop.css");
                });
            }

            if (AppSettings.General.UseHttpsRedirect)
            {
                app.UseHttpsRedirection();
            }

            app.UseSerilogRequestLogging();

            app.UseRouting();

            //Rate limitter middleware
            app.UseRateLimiter();
            app.MapGrpcService<ClusterService>();

            // Authorization
            // ATTENTION: Always UseAuthentication should be before UseAuthorization
            if (AppSettings.Authentication.HasAuthontication)
            {
                app.UseAuthentication();
            }

            // ATTENTION: Always UseAuthentication should be before UseAuthorization
            app.UseAuthorization();
            app.MapControllers();
        }
    }
}