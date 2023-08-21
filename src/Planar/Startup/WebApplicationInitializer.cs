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

            Console.WriteLine("[x] Load configuration & app settings");
            var file1 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.yml");
            var file2 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, $"AppSettings.{AppSettings.Environment}.yml");

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

            if (AppSettings.DeveloperExceptionPage && !app.Environment.IsProduction())
            {
                app.UseDeveloperExceptionPage();
            }

            if (AppSettings.SwaggerUI && !app.Environment.IsProduction())
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

            // Authorization
            if (AppSettings.HasAuthontication)
            {
                app.UseAuthentication();
            }

            app.UseAuthorization();
            app.MapControllers();

            //Rate limitter middleware
            app.UseRateLimiter();
            app.MapGrpcService<ClusterService>();
        }
    }
}