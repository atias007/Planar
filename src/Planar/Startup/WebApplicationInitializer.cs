using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Planar.Common;
using Planar.Service;
using Serilog;
using System;

namespace Planar.Startup
{
    public static class WebApplicationInitializer
    {
        public static WebApplication Initialize(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseWindowsService();

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
                var file2 = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, $"AppSettings.{Global.Environment}.json");

                builder
                .AddJsonFile(file1, false, true)
                .AddJsonFile(file2, true, true)
                .AddCommandLine(args)
                .AddEnvironmentVariables();
            });

            ServiceCollectionInitializer.ConfigureServices(builder.Services);

            var app = builder.Build();
            return app;
        }

        public static void Configure(WebApplication app)
        {
            if (AppSettings.DeveloperExceptionPage || !app.Environment.IsProduction())
            {
                app.UseDeveloperExceptionPage();
            }

            if (AppSettings.SwaggerUI)
            {
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Planar"));
            }

            if (AppSettings.UseHttpsRedirect)
            {
                app.UseHttpsRedirection();
            }

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