using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Planner.API.Common;
using Planner.Service;
using Planner.Service.API;
using Planner.Service.Data;
using Serilog;
using System.Net;

namespace Planner
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
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "Planner", Version = "v1" });
            });

            if (AppSettings.UseHttpsRedirect)
            {
                services.AddHttpsRedirection(options =>
                {
                    options.RedirectStatusCode = (int)HttpStatusCode.TemporaryRedirect;
                    options.HttpsPort = 2610;
                });
            }

            services.AddDbContext<PlannerContext>(o => o.UseSqlServer(AppSettings.DatabaseConnectionString),
                contextLifetime: ServiceLifetime.Transient,
                optionsLifetime: ServiceLifetime.Singleton
            );
            services.AddTransient<DataLayer>();
            services.AddTransient<DeamonBL>();
            services.AddTransient<MainService>();
            services.AddScoped<ServiceDomain>();
            services.AddScoped<IPlannerCommand, DeamonService>();
            services.AddHostedService<MainService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Planner v1"));
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