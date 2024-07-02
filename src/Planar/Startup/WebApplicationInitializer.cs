using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Planar.Common;
using Serilog;
using System;
using Prometheus;

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
                    options.ListenAnyIP(AppSettings.General.HttpsPort, opts =>
                    {
                        if (AppSettings.General.CertificateFile == null && AppSettings.General.CertificatePassword == null)
                        {
                            opts.UseHttps();
                        }
                        else if (AppSettings.General.CertificateFile != null && AppSettings.General.CertificatePassword == null)
                        {
                            opts.UseHttps(AppSettings.General.CertificateFile);
                        }
                        else
                        {
                            opts.UseHttps(AppSettings.General.CertificateFile, AppSettings.General.CertificatePassword);
                        }
                        opts.UseHttps();
                    });
                }
            });

            Console.WriteLine("[x] Load configuration & app settings");
            var file = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Settings, "AppSettings.yml");
            using var stream = YmlFileReader.ReadStreamAsync(file).Result;
            builder.Configuration
                .AddYamlStream(stream)
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

            // Capture metrics about all received HTTP requests.
            app.UseHttpMetrics();

            // Capture metrics about received gRPC requests.
            if (AppSettings.Cluster.Clustering)
            {
                app.UseGrpcMetrics();
            }

            //Rate limitter middleware
            app.UseRateLimiter();

            if (AppSettings.Cluster.Clustering)
            {
                app.MapGrpcService<ClusterService>();
            }

            // ****************************************************************
            // ATTENTION: dont change the order of the following middlewares
            // ****************************************************************

            app.UseRouting();
            app.MapMetrics();

            // Authorization
            // ATTENTION: Always UseAuthentication should be before UseAuthorization
            if (AppSettings.Authentication.HasAuthontication)
            {
                app.UseAuthentication();
            }

            // ATTENTION: Always UseAuthentication should be before UseAuthorization
            app.UseAuthorization();
            app.MapControllers();

            // ****************************************************************
            // ATTENTION: dont add middlewares after --> app.MapControllers()
            // ****************************************************************
        }
    }
}