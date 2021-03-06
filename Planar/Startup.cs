using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Planar.Filters;
using Planar.Service;
using Planar.Service.API;
using Planar.Service.Data;
using Serilog;
using System.Net;
using AutoMapper;
using System.Reflection;

namespace Planar
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options =>
            {
                options.Filters.Add<ValidateModelStateAttribute>();
                options.Filters.Add<HttpResponseExceptionFilter>();
            })
            .AddFluentValidation(fv => fv.RegisterValidatorsFromAssembly(GetType().Assembly));

            services.AddControllers();

            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Planar", Version = "v1" });
            });

            if (AppSettings.UseHttpsRedirect)
            {
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
                    options.HttpsPort = 2610;
                });
            }

            services.AddDbContext<PlanarContext>(o => o.UseSqlServer(AppSettings.DatabaseConnectionString),
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Singleton
            );

            services.AddAutoMapper(Assembly.Load("Planar.Service"));
            services.AddTransient<DataLayer>();
            services.AddTransient<MainService>();
            services.AddScoped<GroupDomain>();
            services.AddScoped<HistoryDomain>();
            services.AddScoped<JobDomain>();
            services.AddScoped<ParametersDomain>();
            services.AddScoped<ServiceDomain>();
            services.AddScoped<MonitorDomain>();
            services.AddScoped<TraceDomain>();
            services.AddScoped<TriggerDomain>();
            services.AddScoped<UserDomain>();
            services.AddHostedService<MainService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Planar v1"));
            }

            app.UseSerilogRequestLogging();

            if (AppSettings.UseHttpsRedirect)
            {
                app.UseHttpsRedirection();
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}