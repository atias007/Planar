using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Planner.Controllers
{
    [ApiController]
    [Route("history")]
    public class HistoryController : BaseController
    {
        public HistoryController(ILogger logger, ServiceDomain bl) : base(logger)
        {
        }
    }
}