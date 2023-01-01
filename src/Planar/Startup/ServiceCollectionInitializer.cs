using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Planar.Filters;
using Planar.Service;
using Planar.Service.API;
using Planar.Service.Data;
using Planar.Service.General;
using Planar.Service.Monitor;
using System;
using System.Net;
using System.Reflection;

namespace Planar.Startup
{
    public static class ServiceCollectionInitializer
    {
        public static void ConfigureServices(IServiceCollection services)
        {
            Console.WriteLine("[x] Configure services");

            services.AddMvc(options =>
            {
                options.Filters.Add<ValidateModelStateAttribute>();
                options.Filters.Add<HttpResponseExceptionFilter>();
            });

            services.AddFluentValidationAutoValidation();
            services.AddValidatorsFromAssemblies(new[] { typeof(Program).Assembly, typeof(MainService).Assembly });

            var mvcBuilder = services.AddControllers();
            ODataInitializer.RegisterOData(mvcBuilder);

            if (AppSettings.SwaggerUI)
            {
                services.AddSwaggerGen(SwaggerInitializer.InitializeSwagger);
            }

            if (AppSettings.UseHttpsRedirect)
            {
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
                    options.HttpsPort = AppSettings.HttpsPort;
                });
            }

            services.AddDbContext<PlanarContext>(o => o.UseSqlServer(
                    AppSettings.DatabaseConnectionString,
                    options => options.EnableRetryOnFailure(10, TimeSpan.FromSeconds(10), null))
            );

            services.AddQuartzService();
            services.AddAutoMapper(Assembly.Load($"{nameof(Planar)}.{nameof(Service)}"));
            services.AddTransient<MainService>();
            services.AddScoped<GroupDomain>();
            services.AddScoped<HistoryDomain>();
            services.AddScoped<JobDomain>();
            services.AddScoped<ConfigDomain>();
            services.AddScoped<ServiceDomain>();
            services.AddScoped<MonitorDomain>();
            services.AddScoped<TraceDomain>();
            services.AddScoped<TriggerDomain>();
            services.AddScoped<UserDomain>();
            services.AddScoped<ClusterDomain>();
            services.AddScoped<ClusterUtil>();
            services.AddScoped<MonitorUtil>();
            services.AddPlanarServices();
            services.AddGrpc();
        }
    }
}