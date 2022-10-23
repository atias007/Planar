using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.Extensions.DependencyInjection;
using Planar.Service.API;
using System;

namespace Planar.Controllers
{
    public class HistoryDataController : ControllerBase
    {
        private readonly HistoryDomain _businessLayer;

        public HistoryDataController(IServiceProvider serviceProvider)
        {
            _businessLayer = serviceProvider.GetRequiredService<HistoryDomain>();
        }

        [EnableQuery]
        public IActionResult Get()
        {
            var result = _businessLayer.GetHistoryData();
            return Ok(result);
        }

        [EnableQuery]
        public Service.Model.JobInstanceLog Get([FromODataUri] int key)
        {
            var result = _businessLayer.GetHistory(key);
            return result;
        }
    }
}