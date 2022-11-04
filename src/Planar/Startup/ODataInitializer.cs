using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace Planar.Startup
{
    public static class ODataInitializer
    {
        public static void RegisterOData(IMvcBuilder builder)
        {
            builder.AddOData(option => option
                    .Select()
                    .Filter()
                    .Count()
                    .OrderBy()
                    .SetMaxTop(50)
                    .AddRouteComponents("odata", GetEdmModel()));
        }

        public static IEdmModel GetEdmModel()
        {
            var modelBuilder = new ODataConventionModelBuilder();
            var customers = modelBuilder.EntitySet<Service.Model.Trace>("TraceData");
            customers.EntityType.Page(50, 50);
            customers.EntityType.OrderBy("Id", "TimeStamp");

            var history = modelBuilder.EntitySet<Service.Model.JobInstanceLog>("HistoryData");
            history.EntityType.Page(50, 50);
            history.EntityType.OrderBy("Id", "StartDate", "EndDate", "Duration", "EffectedRows");

            return modelBuilder.GetEdmModel();
        }
    }
}