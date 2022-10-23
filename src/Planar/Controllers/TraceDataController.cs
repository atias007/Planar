using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Planar.Service.API;
using System;

namespace Planar.Controllers
{
    public class TraceDataController : ControllerBase
    {
        private readonly TraceDomain _businessLayer;

        public TraceDataController(IServiceProvider serviceProvider)
        {
            _businessLayer = serviceProvider.GetRequiredService<TraceDomain>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            var result = _businessLayer.GetTraceData();
            return Ok(result);
        }

        [EnableQuery]
        public Service.Model.Trace Get([FromODataUri] int key)
        {
            var result = _businessLayer.GetTrace(key);
            return result;
        }
    }
}